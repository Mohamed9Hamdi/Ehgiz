# Ehgiz API Reference (Frontend Integration Guide)

This document describes the complete backend surface of the Ehgiz peer-to-peer tool rental platform. Use it as the source of truth when reviewing or building the Angular frontend: every endpoint, request/response shape, enum value, auth rule, and real-time event the client can rely on is listed here.

The backend is an ASP.NET Core (net10.0) Clean Architecture solution:

- `Ehgiz.API` — controllers, SignalR hubs, middleware, DI wiring
- `Ehgiz.Application` — services, DTOs, interfaces, AI integration
- `Ehgiz.DAL` — EF Core entities, enums, repositories, migrations (SQL Server)

## Table of contents

- [Conventions](#conventions)
- [Response envelope](#response-envelope)
- [Authentication and session model](#authentication-and-session-model)
- [Error handling](#error-handling)
- [Rate limiting](#rate-limiting)
- [CORS](#cors)
- [Enums (shared vocabularies)](#enums)
- [Endpoints](#endpoints)
  - [Auth and profile](#auth-and-profile)
  - [Tools](#tools)
  - [Bookings and handovers](#bookings-and-handovers)
  - [Payments](#payments)
  - [Wallet](#wallet)
  - [Messages](#messages)
  - [Notifications](#notifications)
  - [Reviews](#reviews)
  - [Saved searches](#saved-searches)
  - [AI assistant](#ai-assistant)
  - [Settings (public)](#settings-public)
  - [Admin](#admin)
- [SignalR real-time hubs](#signalr-real-time-hubs)
- [Media / image handling](#media--image-handling)
- [Frontend review checklist](#frontend-review-checklist)

## Conventions

- Base URL: whatever the API is hosted on. Local dev default is `https://localhost:<port>`; the Angular dev server is expected at `http://localhost:4200`.
- All routes below are relative to the API host and are prefixed with `/api` (SignalR hubs are under `/hubs`).
- All timestamps are UTC ISO-8601 (`DateTime` serialized by System.Text.Json, e.g. `2026-07-06T12:34:56Z`).
- Money is `decimal`. Default currency is `usd` (top-ups are USD-only).
- IDs are integers (`int`), not GUIDs.
- Enums are serialized as their **string name** in most response DTOs (e.g. `"Pending"`, `"Active"`) but a few request DTOs bind enums by name too (see each enum's note). Do not send numeric enum values from the client — send the string name.
- Property casing in JSON is camelCase (default ASP.NET Core serialization). C# `PricePerDay` becomes `pricePerDay` on the wire.

## Response envelope

Almost every JSON response is wrapped in a generic envelope, `ApiResponse<T>`:

```json
{
  "succeeded": true,
  "message": "Success",
  "data": { },
  "errors": []
}
```

- `succeeded` (bool) — high-level success flag. Prefer checking the HTTP status code; `succeeded` mirrors it.
- `message` (string) — human-readable message, often meant to be surfaced to the user (e.g. "Booking accepted.").
- `data` (T | null) — the payload. On failures this is usually absent/null.
- `errors` (string[]) — list of validation or domain error strings on failure; empty on success.

Exceptions to the envelope:

- `204 No Content` responses have **no body** (logout, delete, mark-as-read, cancel/accept/reject flows that return NoContent).
- The Stripe webhook (`POST /api/payments/webhook`) returns a bare `200 OK` / `400` with no envelope.
- SignalR hub messages are raw payloads, not wrapped.

Paged collections use `PagedResult<T>` as the `data`:

```json
{
  "items": [ ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 42,
  "totalPages": 5
}
```

## Authentication and session model

Auth is JWT Bearer + an HTTP-only refresh cookie.

- **Access token**: short-lived JWT returned in the response body from `login` / `refresh`. Send it as `Authorization: Bearer <token>` on every authenticated request. Lifetime is configured by `Jwt.AccessTokenMins` (currently 60 minutes).
- **Refresh token**: opaque token stored in an **HTTP-only, Secure, SameSite=Strict cookie** named `X-Refresh-Token`, scoped to path `/api/auth`. The client never reads or sends it manually — the browser attaches it automatically to `/api/auth/refresh` and `/api/auth/logout`. Because it is HttpOnly and path-scoped, JS cannot access it, and it is only sent to those two endpoints.
  - This means the Angular `HttpClient` **must send `withCredentials: true`** on `refresh` and `logout` (and login sets the cookie, so login also needs credentials enabled). CORS is configured with `AllowCredentials`, so the frontend origin must be explicitly allow-listed (no wildcard).
- **Refresh flow**: when a request returns `401` with message "Invalid or expired access token.", call `POST /api/auth/refresh` (no body, credentials included). It rotates the refresh cookie and returns a fresh access token. If refresh itself returns `401`, the session is dead — clear state and route to login.
- **Login response** payload (`LoginResponseDTO`):

```json
{ "accessToken": "…", "expiresAt": "2026-07-06T13:34:56Z", "roles": ["user"] }
```

  Roles come from the token; use them for client-side route guards. Roles are `"user"` and `"admin"` (lowercase).

- **Email confirmation is required to sign in** (`RequireConfirmedEmail = true`). A freshly registered user cannot log in until they verify their email via the code flow.
- **Lockout**: 5 failed login attempts locks the account for 5 minutes.
- Unauthenticated requests to `[Authorize]` endpoints get `401` with `ApiResponse.Fail("Unauthorized." | "Invalid or expired access token.")`.
- Authorization failures (wrong owner, not a participant) surface as `403` with a domain message.

### SignalR auth

Hubs are `[Authorize]`. Browsers can't set Authorization headers on the WebSocket handshake, so the token is passed as a query-string param: `?access_token=<jwt>`. The server reads `access_token` only for paths starting with `/hubs`. Configure the Angular SignalR client with `accessTokenFactory: () => token`.

## Error handling

A global exception middleware maps exceptions to status codes and returns the standard `ApiResponse.Fail` envelope:

| Exception thrown in service layer | HTTP status | Meaning for the client |
|---|---|---|
| `KeyNotFoundException` | 404 | Resource not found |
| `UnauthorizedAccessException` | 403 | Authenticated but not allowed (e.g. not the owner) |
| `InvalidOperationException`, `ValidationException`, `ArgumentException` | 400 | Bad request / domain rule violation |
| anything else | 500 | "An unexpected error occurred." (details are logged server-side only) |

Model validation failures (data annotations on request DTOs) return `400` — either the framework's default problem-details or, for endpoints that check `ModelState` manually (e.g. AI assistant), an `ApiResponse.Fail` with the messages in `errors`.

## Rate limiting

A per-IP fixed-window limiter named `auth-codes` guards the one-time-code endpoints: `verify-email`, `resend-verification`, `forgot-password`, `resend-reset-code`, `reset-password`. Limit is **6 requests / 15 minutes per IP**. On rejection the client gets `429` with `ApiResponse.Fail("Too many requests. Please try again later.")`. There are additional per-email limits inside the service layer. Design the UI to handle 429 gracefully (disable resend buttons with a countdown).

Max request body size is **10 MB** (affects image uploads).

## CORS

Policy name `Angular`. Allowed origins come from config `Frontend.AllowedOrigins` (defaults to `http://localhost:4200`). `AllowAnyHeader`, `AllowAnyMethod`, `AllowCredentials`. The frontend origin must be listed exactly (credentials mode forbids `*`).

## Enums

Send/expect the **string name**. Numeric values are the DB storage values (useful only if you need stable ordering).

**BookingStatus**: `Pending`(1), `Accepted`(2), `Rejected`(3), `DeliveryHandover`(4), `Active`(5), `ReturnHandover`(6), `Completed`(7), `Disputed`(8), `Cancelled`(9)

**EscrowStatus**: `Pending`(1), `Held`(2), `Released`(3), `Refunded`(4)

**PaymentStatus**: `Pending`(1), `Completed`(2), `Failed`(3), `Refunded`(4)

**PaymentMethod**: `CreditCard`(1), `DebitCard`(2), `Wallet`(3), `BankTransfer`(4)

**HandoverType**: `Delivery`(1), `Return`(2)

**IssueReportStatus**: `Open`(1), `InReview`(2), `Resolved`(3), `Closed`(4), `Rejected`(5)

**MessageStatus**: `Sent`(1), `Delivered`(2), `Read`(3)

**ToolCondition**: `New`(1), `Good`(2), `Fair`(3), `Poor`(4)

**WalletTransactionType**: `TopUp`(1), `BookingDebit`(2), `EarningCredit`(3), `InsuranceRefund`(4), `BookingRefund`(5), `Withdrawal`(6), `LateFeeDebit`(7), `LateFeeCredit`(8), `PartialRefund`(9), `DisputeCredit`(10), `AdminReversal`(11)

**NotificationType**: `Booking`(1), `Payment`(2), `Message`(3), `Review`(4), `IssueReport`(5), `System`(6), `HandoverPending`(7), `HandoverAccepted`(8), `HandoverDisputed`(9), `DisputeResolved`(10), `SavedSearchMatch`(11)

## Endpoints

Legend: 🔓 = anonymous allowed, 🔒 = requires Bearer token, 👑 = requires `admin` role.

### Auth and profile

Base: `/api/auth`

| Method | Path | Auth | Body | Notes |
|---|---|---|---|---|
| POST | `/register` | 🔓 | `multipart/form-data` `RegisterRequestDTO` | Sends verification email. Returns `201` on success. |
| POST | `/login` | 🔓 | `LoginRequestDTO` | Sets refresh cookie, returns `LoginResponseDTO`. `401` if bad creds / unconfirmed. |
| POST | `/refresh` | 🔓 (cookie) | none | Reads refresh cookie, rotates it, returns new access token. `401` if invalid. |
| POST | `/logout` | 🔒 | none | Revokes refresh token, clears cookie. `204`. |
| POST | `/verify-email` | 🔓 (rate-limited) | `VerifyEmailRequestDTO` | Confirms email with code. |
| POST | `/resend-verification` | 🔓 (rate-limited) | `ResendVerificationRequestDTO` | |
| POST | `/forgot-password` | 🔓 (rate-limited) | `ForgotPasswordRequestDTO` | Emails a reset code. Response is neutral (does not reveal if account exists). |
| POST | `/resend-reset-code` | 🔓 (rate-limited) | `ResendResetCodeRequestDTO` | |
| POST | `/reset-password` | 🔓 (rate-limited) | `ResetPasswordRequestDTO` | |
| GET | `/me` | 🔒 | — | Returns `UserProfileDTO`. |
| PUT | `/me` | 🔒 | `UpdateProfileDTO` | Updates text fields. Returns updated `UserProfileDTO`. |
| POST | `/me/profile-image` | 🔒 | `IFormFile image` (form field named `image`) | Replaces avatar. Returns `UserProfileDTO`. |
| POST | `/me/national-id` | 🔒 | `IFormFile image` | Uploads national ID image. |
| DELETE | `/me/profile-image` | 🔒 | — | Removes avatar. Returns `UserProfileDTO`. |

Request DTOs:

- `RegisterRequestDTO` (multipart): `fullName` (req, ≤150), `email` (req, email, ≤256), `phoneNumber` (req, phone, ≤30), `city` (req, ≤100), `password` (req, 8–128), `profileImage` (optional file), `nationalIdImage` (optional file).
- `LoginRequestDTO`: `email` (req, email), `password` (req).
- `VerifyEmailRequestDTO`: `email`, `code` (≤10).
- `ResetPasswordRequestDTO`: `email`, `code` (≤10), `newPassword` (8–128).
- `ForgotPasswordRequestDTO` / `ResendResetCodeRequestDTO` / `ResendVerificationRequestDTO`: `email`.
- `UpdateProfileDTO`: `fullName?`, `phoneNumber?`, `address?`, `city?` (all optional; nulls left unchanged).

Response DTOs:

- `UserProfileDTO`: `id`, `email`, `fullName`, `phoneNumber?`, `profileImageUrl?`, `nationalIdImageUrl?`, `address?`, `city?`, `createdAt`, `isActive`, `roles: string[]`.
- `LoginResponseDTO`: `accessToken`, `expiresAt`, `roles: string[]`.

Password policy (enforced server-side; mirror it in client validation): min length 8, requires a digit, requires a lowercase letter. Uppercase not required.

### Tools

Base: `/api/tools`

| Method | Path | Auth | Body / Query | Returns |
|---|---|---|---|---|
| GET | `/` | 🔓 | `ToolFilterDto` (query) | `PagedResult<ToolDto>` |
| GET | `/{id}` | 🔓 | — | `ToolDto` |
| GET | `/my` | 🔒 | — | `List<ToolDto>` (caller's tools) |
| POST | `/` | 🔒 | `CreateToolDto` (JSON) | `201` `ToolDto` |
| PUT | `/{id}` | 🔒 (owner) | `UpdateToolDto` (JSON) | `ToolDto` |
| DELETE | `/{id}` | 🔒 (owner) | — | `204` |
| POST | `/{id}/images` | 🔒 (owner) | `multipart/form-data` `List<IFormFile> images` | `ToolImagesUploadedDto` |
| DELETE | `/images/{imageId}` | 🔒 (owner) | — | `204` |
| PUT | `/images/{imageId}/primary` | 🔒 (owner) | — | `204` |
| POST | `/suggest-from-images` | 🔒 | `multipart/form-data` `List<IFormFile> images` | `ToolSuggestionDto` (AI-generated draft) |
| POST | `/search-by-photo` | 🔓 | `multipart/form-data` `images` + `?page&pageSize` | `PhotoSearchResultDto` |

`ToolFilterDto` query params: `categoryId?`, `condition?` (ToolCondition name), `location?`, `minPrice?`, `maxPrice?`, `isAvailable?`, `searchTerm?`, `nearLat?`, `nearLng?`, `radiusKm?`, `page`(default 1), `pageSize`(default 10). Geo search: pass `nearLat`+`nearLng`+`radiusKm` to filter/sort by distance; results then carry `distanceKm`.

`CreateToolDto` / `UpdateToolDto` (JSON): `categoryId`, `name` (req, ≤200), `description?` (≤5000), `pricePerDay` (0.01–1,000,000), `insurancePrice` (0–1,000,000), `condition?` (ToolCondition name), `location?` (≤200), `latitude?` (-90..90), `longitude?` (-180..180). `UpdateToolDto` additionally has `isAvailable` (bool). Note: creating a tool and uploading its images are **two separate calls** — create first, then POST images to `/{id}/images`.

`ToolDto`: `id`, `name`, `description?`, `pricePerDay`, `insurancePrice`, `condition?`, `location?`, `latitude?`, `longitude?`, `distanceKm?`, `isAvailable`, `createdAt`, `ownerId`, `ownerName`, `ownerProfileImageUrl?`, `categoryId`, `categoryName`, `imageUrls: string[]`, `images: ToolImageDto[]`.

`ToolImageDto`: `id`, `imageUrl`, `isPrimary`.

`ToolImagesUploadedDto`: `toolId`, `imageUrls: string[]`.

`ToolSuggestionDto`: `name`, `description`, `condition` (ToolCondition), `categoryId`, `categoryName?`. Use it to pre-fill the create-tool form.

`PhotoSearchResultDto`: `identifiedObject`, `brand?`, `model?`, `searchKeywords: string[]`, `matchingTools: PagedResult<ToolDto>`.

### Bookings and handovers

Base: `/api/bookings` — all endpoints 🔒 except availability.

| Method | Path | Auth | Body | Returns / effect |
|---|---|---|---|---|
| POST | `/` | 🔒 | `CreateBookingRequest` | `201` `CreateBookingResponse` (funds held from wallet) |
| GET | `/my` | 🔒 | — | `BookingCardDto[]` (as renter) |
| GET | `/received` | 🔒 | — | `BookingCardDto[]` (as owner) |
| GET | `/{id}` | 🔒 (participant) | — | `BookingDto` (full detail) |
| PUT | `/{id}/cancel` | 🔒 | — | Cancels, refunds renter wallet |
| PUT | `/{id}/accept` | 🔒 (owner) | — | Owner accepts pending booking |
| PUT | `/{id}/reject` | 🔒 (owner) | — | Owner rejects, refunds renter |
| POST | `/{id}/handover/delivery` | 🔒 (owner) | `multipart` `SubmitHandoverRequest` | Owner submits delivery proof |
| PUT | `/{id}/handover/delivery/respond` | 🔒 (renter) | `RespondHandoverRequest` | Renter accepts/disputes delivery |
| POST | `/{id}/handover/return` | 🔒 (renter) | `multipart` `SubmitHandoverRequest` | Renter submits return proof |
| PUT | `/{id}/handover/return/respond` | 🔒 (owner) | `RespondHandoverRequest` | Owner accepts/disputes return |
| POST | `/{id}/report-issue` | 🔒 (participant) | `ReportIssueRequest` | Moves booking to Disputed |
| GET | `/tool/{toolId}/availability?year&month` | 🔓 | — | `ToolAvailabilityDto` |

Booking lifecycle (status transitions): `Pending` → owner `accept` → `Accepted` → owner submits delivery → `DeliveryHandover` → renter accepts → `Active` → renter submits return → `ReturnHandover` → owner accepts → `Completed`. Branches: owner `reject` → `Rejected`; either party `cancel` → `Cancelled`; a disputed handover or `report-issue` → `Disputed` (admin resolves).

`CreateBookingRequest`: `toolId`, `startDate`, `endDate`.

`CreateBookingResponse`: `bookingId`, `rentalCost`, `insuranceAmount`, `platformFee`, `totalCharged`, `currency`. Booking payment is taken from the renter's **wallet balance** (top up first); insurance is held in escrow and refunded on clean return.

`SubmitHandoverRequest` (multipart): `notes?` (string), `images?` (`List<IFormFile>`, field name `images`).

`RespondHandoverRequest` (JSON): `accept` (bool), `notes?`. `accept:false` disputes the handover.

`ReportIssueRequest` (JSON): `title`, `description`.

`BookingCardDto` (list view): `id`, `toolId`, `toolName`, `toolImageUrl?`, `otherPartyId`, `otherPartyName`, `otherPartyImageUrl?`, `startDate`, `endDate`, `days`, `totalPrice`, `status`, `createdAt`, `deliveryHandover?: HandoverSummaryDto`, `returnHandover?: HandoverSummaryDto`, `allowedActions: string[]`.

`HandoverSummaryDto`: `id`, `isSubmitted`, `isAccepted?`, `submittedAt?`, `respondedAt?`, `imageCount`.

`BookingDto` (detail): `id`, `toolId`, `toolName`, `toolImageUrl?`, `ownerId`, `ownerName`, `ownerProfileImageUrl?`, `renterId`, `renterName`, `renterProfileImageUrl?`, `startDate`, `endDate`, `days`, `rentalCost`, `insurancePrice`, `totalPrice`, `status`, `paymentStatus?`, `escrowStatus?`, `createdAt`, `adminResolutionNotes?`, `handovers?: HandoverDto[]`, `allowedActions: string[]`, `hasReview`.

`HandoverDto`: `id`, `bookingId`, `type` (HandoverType name), `submittedByName`, `submitterNotes?`, `submittedAt`, `respondedByName?`, `responderNotes?`, `isAccepted?`, `respondedAt?`, `images?: HandoverImageDto[]`.

`HandoverImageDto`: `id`, `imageUrl`, `caption?`.

`ToolAvailabilityDto`: `toolId`, `year`, `month`, `bookedRanges: BookedDateRange[]` where each is `{ bookingId, startDate, endDate, status }`. Use it to disable dates in the date picker.

**`allowedActions`** is the server's authoritative list of what the current user may do to this booking right now (e.g. `"accept"`, `"reject"`, `"cancel"`, `"submitDeliveryHandover"`, `"respondDeliveryHandover"`, `"submitReturnHandover"`, `"respondReturnHandover"`, `"reportIssue"`, `"review"`). Drive button visibility off this array rather than re-deriving rules on the client.

### Payments

Base: `/api/payments`

| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `/webhook` | 🔓 | Stripe-only. Raw body + `Stripe-Signature` header. Not called by the frontend. |
| GET | `/booking/{bookingId}` | 🔒 | Returns `PaymentDto` or `404` if none. |

`PaymentDto`: `id`, `bookingId`, `amount`, `paymentMethod?`, `paymentStatus?`, `escrowStatus?`, `paidAt?`, `stripePaymentIntentId?`.

### Wallet

Base: `/api/wallet` — all 🔒 except the Stripe redirect returns.

| Method | Path | Auth | Body/Query | Returns |
|---|---|---|---|---|
| GET | `/` | 🔒 | — | `WalletDto` |
| GET | `/transactions` | 🔒 | — | `WalletTransactionDto[]` |
| GET | `/earnings?months=12` | 🔒 | — | `MonthlyEarningsDto[]` |
| POST | `/topup` | 🔒 | `TopUpRequest` | `TopUpResponse` (Stripe embedded checkout) |
| POST | `/withdraw` | 🔒 | `WithdrawalRequest` | message only |
| GET | `/connect/onboard` | 🔒 | — | `ConnectOnboardingResponse` (Stripe Connect URL) |
| GET | `/connect/return` | 🔓 | — | Stripe redirect landing (onboarding done) |
| GET | `/connect/refresh` | 🔓 | — | Stripe redirect landing (link expired) |

`WalletDto`: `id`, `balance`, `heldBalance`, `totalBalance`. `heldBalance` is escrow (in-flight bookings); `balance` is spendable.

`TopUpRequest`: `amount`, `currency` (default `"usd"`; top-ups are USD-only). `TopUpResponse`: `clientSecret`, `amount`, `currency`. Render with Stripe embedded checkout — `stripe.initEmbeddedCheckout({ clientSecret })`. After success the browser is redirected to `<frontendBaseUrl>/wallet/topup/return`, so that route must exist in Angular.

`WithdrawalRequest`: `amount`. Requires a completed Stripe Connect onboarding first (`/connect/onboard` → redirect user to `onboardingUrl`).

`ConnectOnboardingResponse`: `onboardingUrl`.

`WalletTransactionDto`: `id`, `amount`, `type` (WalletTransactionType name), `description?`, `reference?`, `createdAt`.

`MonthlyEarningsDto`: `month` (e.g. `"2026-07"`), `gross`, `fees`, `net`. Good for an earnings chart.

### Messages

Base: `/api/messages` — all 🔒. Pairs with the Chat hub for real-time delivery.

| Method | Path | Body/Query | Returns |
|---|---|---|---|
| POST | `/conversations` | `StartConversationDto { recipientId }` | `ConversationDto` (gets or creates the 1:1 conversation) |
| GET | `/conversations` | — | `ConversationDto[]` (inbox, with unread counts) |
| GET | `/conversations/{conversationId}?page=1&pageSize=30` | — | `MessageDto[]` (paged history) |
| POST | `/conversations/{conversationId}` | `SendMessageDto { content }` | `201` `MessageDto` |
| PUT | `/conversations/{conversationId}/read` | — | `204` (marks incoming messages read) |

`ConversationDto`: `id`, `otherUserId`, `otherUserName`, `otherUserAvatarUrl?`, `updatedAt`, `lastMessage?: MessageDto`, `unreadCount`.

`MessageDto`: `id`, `conversationId`, `senderId`, `senderName`, `senderAvatarUrl?`, `content?`, `status` (MessageStatus name), `createdAt`, `deliveredAt?`, `readAt?`.

`SendMessageDto`: `content` (1–2000 chars).

Sending via REST also pushes the message to the recipient over SignalR (`ReceiveMessage`). Marking read pushes `MessagesRead` to the sender.

### Notifications

Base: `/api/notifications` — all 🔒. Pairs with the Notification hub.

| Method | Path | Returns / effect |
|---|---|---|
| GET | `/` | `NotificationDto[]` (all) |
| GET | `/unread` | `NotificationDto[]` (unread only) |
| GET | `/unread/count` | `{ count: number }` |
| PUT | `/{id}/read` | `204` mark one read |
| PUT | `/read-all` | `204` mark all read |
| DELETE | `/{id}` | `204` delete one |

`NotificationDto`: `id`, `userId`, `title`, `message`, `type` (NotificationType name), `isRead`, `url?`, `createdAt`. `url` is a client-relative deep link to navigate to when the notification is clicked.

### Reviews

Base: `/api/reviews`

| Method | Path | Auth | Body | Returns |
|---|---|---|---|---|
| GET | `/tool/{toolId}` | 🔓 | — | `ReviewDto[]` |
| GET | `/tool/{toolId}/rating` | 🔓 | — | `{ toolId, averageRating }` |
| GET | `/{id}` | 🔓 | — | `ReviewDto` |
| POST | `/` | 🔒 | `CreateReviewDto` | `201` `ReviewDto` |
| DELETE | `/{id}` | 🔒 (author) | — | `204` |

`CreateReviewDto`: `bookingId`, `rating` (int, 1–5 expected), `comment?`. A review is tied to a completed booking; `BookingDto.hasReview` tells you whether the renter has already reviewed.

`ReviewDto`: `id`, `bookingId`, `rating`, `comment?`, `createdAt`, `toolId`, `toolName`, `renterName`, `renterProfileImageUrl?`.

### Saved searches

Base: `/api/saved-searches` — all 🔒. When a newly listed tool matches a saved search, the owner of the search gets a `SavedSearchMatch` notification.

| Method | Path | Body | Returns |
|---|---|---|---|
| POST | `/` | `CreateSavedSearchDto` | `201` `SavedSearchDto` |
| GET | `/` | — | `SavedSearchDto[]` |
| DELETE | `/{id}` | — | message only |

`CreateSavedSearchDto`: `searchTerm?`, `categoryId?`, `location?`, `minPrice?`, `maxPrice?`, `condition?` (ToolCondition name).

`SavedSearchDto`: `id`, `searchTerm?`, `categoryId?`, `categoryName?`, `location?`, `minPrice?`, `maxPrice?`, `condition?`, `createdAt`.

### AI assistant

Base: `/api/ai` — 🔒.

| Method | Path | Body | Returns |
|---|---|---|---|
| POST | `/assistant` | `ToolAssistantRequestDto { question }` | `ToolAssistantResponseDto` |

`ToolAssistantRequestDto`: `question` (req, 5–1000 chars). `ToolAssistantResponseDto`: `answer` (string), `recommendedTools: ToolRecommendationDto[]`.

`ToolRecommendationDto`: `id`, `name`, `description?`, `pricePerDay`, `categoryName`, `location?`, `imageUrls: string[]`.

If the AI key is unconfigured the endpoint returns `503` with a "not configured" message — handle it gracefully.

### Settings (public)

| Method | Path | Auth | Returns |
|---|---|---|---|
| GET | `/api/settings/platform-fee` | 🔓 | `{ feePercent: number }` |

Use this to show renters/owners the platform commission before they list or book.

### Admin

Base: `/api/admin` — **all endpoints require the `admin` role** (`403` otherwise).

Dashboard:
- GET `/dashboard` → `AdminDashboardStatsDto` (`totalUsers`, `activeUsers`, `totalListings`, `activeListings`, `totalBookings`, `activeBookings`, `disputedBookings`, `openIssueReports`, `totalCategories`, `totalRevenue`, `pendingEscrow`).

Disputes:
- GET `/disputes` → `BookingDto[]`
- GET `/disputes/{bookingId}` → `DisputeDetailsDto { booking, issues: IssueReportDto[], handovers: HandoverDto[] }`
- PUT `/disputes/{bookingId}/favor-owner` — body `ResolveDisputeRequest { resolutionNotes? }`
- PUT `/disputes/{bookingId}/favor-renter` — body `ResolveDisputeRequest`
- PUT `/disputes/{bookingId}/partial-refund` — body `PartialRefundRequest { refundPercentage, resolutionNotes? }`
- PUT `/disputes/{bookingId}/force-complete` — body `ResolveDisputeRequest`
- PUT `/disputes/{bookingId}/force-cancel` — body `ResolveDisputeRequest`

Issue reports:
- GET `/issue-reports` → `IssueReportDto[]`
- GET `/issue-reports/{id}` → `IssueReportDto`
- PUT `/issue-reports/{id}/status` — body `UpdateIssueStatusRequest { status }` (IssueReportStatus name)

`IssueReportDto`: `id`, `reporterName`, `title?`, `description?`, `status`, `createdAt`.

Users:
- GET `/users` → `AdminUserDetailsDto[]`
- GET `/users/{id}` → `AdminUserDetailsDto`
- PUT `/users/{id}/active` — body `SetUserActiveRequest { isActive }`
- PUT `/users/{id}/role` — body `SetUserRoleRequest { role }` (`"user"` | `"admin"`)
- DELETE `/users/{id}`

`AdminUserDetailsDto`: `id`, `fullName`, `email`, `phoneNumber?`, `profileImageUrl?`, `nationalIdImageUrl?`, `address?`, `city?`, `isActive`, `emailConfirmed`, `role`, `createdAt`, `totalListings`, `totalBookings`, `stripeCustomerId?`, `stripeAccountId?`.

Listings:
- GET `/listings` → `AdminListingDto[]`
- GET `/listings/{id}` → `AdminListingDetailsDto`
- PUT `/listings/{id}/availability` — body `SetListingAvailabilityRequest { isAvailable }`
- DELETE `/listings/{id}`

Categories:
- GET `/categories` → `AdminCategoryDto[]` (`id`, `name`, `description?`, `imageUrl?`, `isActive`, `toolCount`)
- POST `/categories` — body `CreateCategoryRequest { name, description?, imageUrl? }` → `201`
- PUT `/categories/{id}` — body `UpdateCategoryRequest { name?, description?, imageUrl?, isActive? }`
- DELETE `/categories/{id}`

Wallets & transactions:
- GET `/wallets` → `AdminWalletDto[]` (`id`, `userId`, `userFullName`, `userEmail`, `balance`, `heldBalance`, `updatedAt`)
- GET `/transactions` → `AdminWalletTransactionDto[]` (`id`, `walletId`, `userId`, `userFullName`, `amount`, `type`, `description?`, `reference?`, `createdAt`)
- POST `/transactions/{id}/rollback` — body `RollbackTransactionRequest { reason }` → `RollbackTransactionResultDto`

Platform settings:
- GET `/settings/platform-fee` → `{ feePercent }`
- PUT `/settings/platform-fee` — body `UpdatePlatformFeeRequest { feePercent }` (0–100)

Note: there is no public "list categories" endpoint outside admin in this controller set — the frontend consumes categories via the admin categories endpoint (admin) or embedded in tool DTOs. Verify how the public catalog fetches categories when reviewing the catalog UI.

## SignalR real-time hubs

Two hubs. Connect with the JWT in the query string (`?access_token=`), same token as REST. Each user is auto-joined to a per-user group on connect, so the client only needs to `.on(...)` handlers — no manual group joins.

### Notification hub — `/hubs/notifications`

Server → client events:
- `ReceiveNotification` — payload is a `NotificationDto`. Fired when a new notification is created for this user. Increment the unread badge and prepend to the list.
- `NotificationsReadStateChanged` — no payload. Fired when read state changes (single or read-all). Re-fetch the unread count / list.

The hub also tracks online presence server-side (`UserConnection` rows) on connect/disconnect. No client action needed.

### Chat hub — `/hubs/chat`

Client → server methods (invoke):
- `StartTyping(conversationId: number)`
- `StopTyping(conversationId: number)`

Server → client events:
- `ReceiveMessage` — payload is a `MessageDto`. Fired to the recipient when a message is sent (the REST POST triggers it). Append to the open conversation and bump the inbox.
- `MessagesRead` — payload `{ conversationId }`. Fired to the **sender** when the recipient marks the conversation read. Update your sent-message ticks to "read".
- `UserTyping` — payload `{ conversationId, userId, isTyping }`. Fired to the other participant on start/stop typing. Show/hide the typing indicator.

Group naming (internal, for reference): notifications use `user_{id}`; chat uses `chat_user_{id}`.

## Media / image handling

Images (avatars, national IDs, tool photos, handover photos, category images) are uploaded as `multipart/form-data` and stored on **Cloudinary**. All `*ImageUrl` / `imageUrls` fields returned by the API are **absolute HTTPS URLs** — bind them directly to `<img src>`, no base-URL prefixing needed.

Upload rules for the client:
- Use `FormData`; do not set `Content-Type` manually (let the browser set the multipart boundary).
- Total request body limit is 10 MB.
- Multi-image endpoints expect the form field named `images` (repeated). Single-image endpoints (avatar, national ID) expect a field named `image`.
- Register is a single multipart request that can include `profileImage` and `nationalIdImage` alongside the text fields.

## Frontend review checklist

Use these when reviewing the Angular app against this backend:

1. **Auth interceptor**: attaches `Authorization: Bearer`; on `401` "expired" it calls `/api/auth/refresh` (with `withCredentials`) once and retries; on refresh failure it logs out. Login/refresh/logout requests must send credentials so the refresh cookie flows.
2. **Enum handling**: the client sends and reads enum **names**, not numbers. Check dropdowns for condition, status filters, role, issue status.
3. **Response unwrapping**: a shared operator/interceptor unwraps `ApiResponse<T>.data`; `204` endpoints are handled as empty. Paged endpoints read `data.items` + pagination fields.
4. **Booking actions**: buttons are driven by `allowedActions`, not client-side status guards. Handover submit uses multipart; respond uses JSON.
5. **Wallet/Stripe**: top-up uses embedded checkout with `clientSecret`; a `/wallet/topup/return` route exists; withdraw is gated behind Connect onboarding.
6. **Real-time**: both hubs connect with `accessTokenFactory`; handlers exist for `ReceiveNotification`, `NotificationsReadStateChanged`, `ReceiveMessage`, `MessagesRead`, `UserTyping`; typing calls `StartTyping`/`StopTyping`.
7. **Images**: bound directly from absolute URLs; uploads use `FormData` with correct field names and respect the 10 MB cap.
8. **Rate-limited flows**: verify/resend/reset screens handle `429` with a cooldown; forgot-password UX stays neutral (no "account not found" leak).
9. **Admin guard**: admin routes require the `admin` role from the token; non-admins get `403`.
10. **Email confirmation gate**: post-register flow routes to the verify-email screen; login errors distinguish unconfirmed accounts where the API allows.
