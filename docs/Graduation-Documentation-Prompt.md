# Ehgiz — Claude Graduation Documentation Prompt

Use this file to generate your **ITI-style graduation report** for Ehgiz in Claude.

---

## Step-by-step

1. **Fill placeholders** in [Section 2](#2-master-prompt-copy-into-claude) below (`[TEAM_MEMBER_NAMES]`, etc.).
2. **Open Claude** (Opus or Sonnet recommended for long documents).
3. **Attach:**
   - `CRM Exception Flow.pdf` (style template)
   - Swagger UI screenshots (all controller groups)
   - Database ER diagram screenshot
4. **Paste** the entire Master Prompt (Section 2).
5. If the response truncates, reply: `Continue from Chapter X`.
6. Use [Section 3](#3-follow-up-prompts) to refine chapters.
7. Export to Word/Google Docs and apply ITI title-page formatting like the CRM PDF.

---

## 1. Placeholders — fill before sending

| Placeholder | Example | Your value |
|-------------|---------|------------|
| `[TEAM_MEMBER_NAMES]` | Ahmad Hassan, Sara Mohamed, Omar Ali | |
| `[ITI_BRANCH]` | Fayoum branch | |
| `[PROGRAM_TRACK]` | Intensive .NET Full Stack Track | |
| `[YEAR]` | 2026 | |
| `[SUPERVISOR_NAME]` | Dr. … (optional in Acknowledgments) | |

---

## 2. Master Prompt (copy into Claude)

```markdown
# ROLE
You are an expert technical writer and .NET full-stack architect preparing an **ITI graduation project report** in **English**. Your output must closely follow the structure, academic tone, chapter depth, and formatting style of the attached reference document **"CRM Exception Flow"** — but the subject is our project **Ehgiz**, not CRM.

# INPUTS YOU HAVE
1. **Template PDF** — "CRM Exception Flow" (~57 pages). Mirror its table of contents, chapter numbering, writing style (formal, third-person, problem-solution narrative), and section granularity. Do NOT copy CRM-specific content; adapt every section to Ehgiz.
2. **Swagger UI screenshots** — Authoritative source for API endpoint names, HTTP methods, route groups, and auth requirements. Reference in Chapter 4 as **Figure: Swagger API Documentation**. Describe each controller group you can read from the screenshots.
3. **Database ER diagram screenshot** — Authoritative source for entities, relationships, and cardinality. Reference in Chapter 3 as **Figure: Entity Relationship Diagram**. Describe every major entity and relationship visible.

# PROJECT: EHGIZ

## Elevator pitch
**Ehgiz** is a peer-to-peer **tool rental marketplace**. Owners list tools; renters browse, book, pay via Stripe/wallet, chat in real time, and receive AI-assisted tool recommendations. Graduation full-stack project: **ASP.NET Core (.NET 10) backend** + **Angular SPA frontend** + **SQL Server**.

## Problem domain (replace CRM narrative with this)
- **Problem:** DIY and home-repair users struggle to find the right rental tools at fair prices; owners lack a trusted platform for listings, bookings, payments, and communication.
- **Gap:** Informal rentals and generic classified ads lack booking lifecycle, escrow, reviews, real-time chat, and intelligent discovery.
- **Solution:** Ehgiz integrates tool listings, booking workflow, payments, messaging, notifications, reviews, saved searches, and an AI tool assistant in one platform.

## Tech stack

### Backend (Clean Architecture)
- **Ehgiz.API** — Controllers, SignalR hubs, middleware, Swagger, extension-based startup
- **Ehgiz.Application** — Services, DTOs, interfaces, Mapster mappings, AI agent, seed data
- **Ehgiz.DAL** — EF Core entities, repositories, Unit of Work, migrations
- **Ehgiz.Tests** — xUnit + SQLite in-memory fixtures

| Concern | Technology |
|---------|------------|
| Framework | ASP.NET Core Web API (.NET 10) |
| Database | SQL Server + EF Core 10 |
| Auth | ASP.NET Core Identity, JWT Bearer, HTTP-only refresh cookie |
| Real-time | SignalR (`/hubs/chat`, `/hubs/notifications`) |
| Payments | Stripe.net (intents + webhooks) |
| Email | SendGrid (verification & password-reset OTP) |
| Media | Cloudinary |
| AI | Microsoft.Extensions.AI + GitHub Models (`openai/gpt-4o-mini`) |
| Mapping | Mapster |
| Logging | Serilog |
| API docs | Swashbuckle (Swagger) |

**Patterns:** Repository + Unit of Work, `ApiResponse<T>` envelope, DTO mapping, extension-method DI.

**Auth details:**
- Access token: JWT in `Authorization: Bearer`, ~60 min lifetime
- Refresh token: HTTP-only cookie `X-Refresh-Token`, path `/api/auth`, rotated on refresh
- Roles: `user`, `admin` (lowercase in JWT claims)
- Email confirmation required before login; 6-digit OTP for verify and password reset

### Frontend (Angular — separate repository)
- Standalone components, signals, HttpClient
- Auth interceptor (Bearer token) + error interceptor (401 → clear session)
- `withCredentials: true` on login/refresh/logout for refresh cookie
- SignalR: `accessTokenFactory` for hub connections
- Key routes: `/browse`, `/tools`, `/bookings`, `/wallet`, `/messages`, `/notifications`, `/reviews`, `/saved-searches`, `/ai/rag-search`, `/admin`, `/dashboard`

## Core modules

| Module | Capabilities |
|--------|--------------|
| **Authentication** | Register (multipart + profile image), login, refresh, logout, verify-email, resend-verification, forgot-password, reset-password, `/api/auth/me` |
| **Tools & Categories** | CRUD, multi-image upload (Cloudinary), geo + text + category + price search, condition filters |
| **Bookings** | Full lifecycle with handovers and issue reports |
| **Payments & Wallet** | Stripe payment intents, webhook, wallet balance, USD top-up, transactions, platform fee (default 10%) |
| **Messaging** | Conversations, message history, real-time via SignalR |
| **Notifications** | List, mark read, real-time push via SignalR |
| **Reviews** | Ratings/comments tied to completed bookings |
| **Saved Searches** | Save filter criteria; notify when matching tool listed |
| **AI** | Image validation, photo-based search, listing suggestions, conversational tool assistant |
| **Admin** | Dashboard stats, users, tools, categories, disputes, issue reports, platform fee |
| **Settings** | Public platform settings (e.g. fee percent) |

## Booking lifecycle (for flowcharts)
`Pending` → `Accepted` / `Rejected` → `DeliveryHandover` → `Active` → `ReturnHandover` → `Completed`  
Branches: `Cancelled`, `Disputed` (admin resolution: favor-owner, favor-renter, force-cancel, split, close)

## Database entities (cross-check with ER diagram)
ApplicationUser, RefreshToken, EmailVerificationCode, PasswordResetCode, Category, Tool, ToolImage, Booking, Handover, HandoverImage, IssueReport, Payment, Wallet, WalletTransaction, PlatformRevenueLedger, Conversation, Message, Notification, Review, SavedSearch, SystemSetting, UserConnection

**Relationships (high level):**
- User owns Tools; Tool belongs to Category; Tool has many ToolImages
- Booking links Renter + Tool; Booking has Handovers and Payments
- Conversation links two users; Message belongs to Conversation
- Review ties User + Tool + Booking
- Wallet belongs to User; WalletTransaction belongs to Wallet

## API surface (verify against Swagger screenshots)

| Controller group | Base route |
|------------------|------------|
| Auth | `/api/auth` |
| Tools | `/api/tools` |
| Categories | `/api/categories` |
| Bookings | `/api/bookings` |
| Payments | `/api/payments` |
| Wallet | `/api/wallet` |
| Messages | `/api/messages` |
| Notifications | `/api/notifications` |
| Reviews | `/api/reviews` |
| Saved Searches | `/api/saved-searches` |
| AI | `/api/ai` |
| Settings | `/api/settings` |
| Admin | `/api/admin` |
| Health | `/health` |
| SignalR | `/hubs/chat`, `/hubs/notifications` |

All JSON responses use `ApiResponse<T>`: `{ succeeded, message, data, errors }`.

## AI Tool Assistant (v1 — for Chapter 3–4)
- Endpoint: `POST /api/ai/assistant` — `[Authorize]`
- Request: `{ "question": "..." }` (5–1000 chars)
- Response: `{ "answer": "...", "recommendedTools": [{ id, name, description, pricePerDay, categoryName, location, imageUrls }] }`
- Backend uses **function calling** (`SearchAvailableTools`, `ListCategories`, `GetToolById`) against SQL Server catalog
- **Stateless:** no chat history stored in database; each request is independent
- LLM: GitHub Models endpoint `https://models.github.ai/inference`, model `openai/gpt-4o-mini`
- Typical latency: 3–10 seconds

## User roles
- **user:** Browse, list tools, book, pay, chat, review, AI assistant, saved searches
- **admin:** Platform administration, dispute/issue resolution, category management, fee config

## Testing
- `Ehgiz.Tests`: xUnit service tests with SQLite-backed EF Core
- Covers: auth, tokens, tools, bookings, wallet, reviews, saved searches, messaging, notifications, profiles
- Manual API testing via Swagger (Development environment)
- Do NOT invent precise benchmark numbers unless provided; use qualitative evaluation for graduation scope

## Real implementation challenges (use in Chapter 4.3)
1. **JWT + refresh cookie + CORS:** HttpOnly cookie scoped to `/api/auth`; Angular must use `withCredentials`; CORS must allow explicit origin (not `*`)
2. **SignalR authentication:** Browsers cannot set Authorization on WebSocket; token passed as `?access_token=` query param for `/hubs` paths
3. **Email OTP flows:** Registration blocked until verified; rate limiting on code endpoints (6 req / 15 min per IP)
4. **Stripe webhooks:** Raw body signature verification; idempotent payment state updates
5. **SQL Server `text` column:** Tool `Description` stored as `text` — EF `Contains` fails; AI search filters in memory after projection
6. **AI external dependency:** GitHub Models API required; returns 503 if `AI__ApiKey` not configured

---

# OUTPUT REQUIREMENTS

## Document structure (match CRM template TOC)
1. **Title page** — Project: **Ehgiz** | Subtitle: *Peer-to-Peer Tool Rental Marketplace with AI-Assisted Discovery*
2. **Abstract** (~250–400 words) + **Keywords**
3. **Acknowledgments**
4. **Table of Contents**
5. **Chapter 1: Introduction** — 1.1 Introduction, 1.2 Background and Motivation, 1.3 Importance of the Problem, 1.4 Problem Statement, 1.5 Objectives (1 main + 5–6 specific), 1.6 Overview of the Proposed Solution
6. **Chapter 2: Literature Review / Related Work** — P2P rental marketplaces, sharing economy platforms, marketplace architecture patterns, AI in product discovery; gaps Ehgiz addresses
7. **Chapter 3: Proposed System** — Architecture methodology (DDD + layered), include figures for:
   - System architecture (3-tier: Angular → API → SQL Server)
   - **ER Diagram** (from screenshot)
   - Authentication & refresh flow (sequence diagram)
   - Booking lifecycle (state diagram)
   - Payment / wallet / platform fee flow
   - AI assistant flow (sequence diagram)
   - RBAC (user vs admin)
   - SignalR messaging flow
8. **Chapter 4: Implementation** — 4.1 Technologies & tools, 4.2 Key modules (backend + frontend), 4.3 Challenges faced and resolutions (use the 6 challenges above), reference **Swagger screenshots**
9. **Chapter 5: Testing & Evaluation** — Unit/integration (xUnit), Swagger manual testing, test scenarios per module, performance discussion (CRUD vs AI latency)
10. **Chapter 6: Results & Discussion** — Objectives achievement, strengths, limitations (stateless AI chat, external service dependencies, web-only)
11. **Chapter 7: Conclusion & Future Work** — Contributions; future: persisted AI chat, vector RAG, mobile app, multi-currency, analytics dashboard
12. **References** — Academic sources + official docs (Microsoft, Angular, Stripe, EF Core, GitHub Models)

## Formatting rules
- Formal academic English, third person
- Number figures: **Figure 3.1: Entity Relationship Diagram**, etc.
- For attached screenshots: describe content in prose + "*(see attached screenshot)*"
- Include diagrams (mermaid or ASCII) for flows not covered by screenshots
- Target length: **40–55 pages** equivalent to CRM template
- Do NOT copy CRM paragraphs verbatim — mirror structure and tone only

## What NOT to include
- CRM, exception management, n8n, Salesforce, vector DB RAG (not implemented)
- Persisted AI chat history (not in v1)
- Fabricated performance metrics or user-study scores

## Authors
[TEAM_MEMBER_NAMES]
Information Technology Institute — [ITI_BRANCH]
[PROGRAM_TRACK], [YEAR]

Begin with the Title Page and Abstract. Produce the full document; if too long, stop at a chapter boundary and wait for "continue".
```

---

## 3. Follow-up prompts

Paste these in the **same Claude thread** after the first draft:

### Expand implementation challenges
```
Expand Chapter 4.3 "Challenges faced and how they were resolved" with these six Ehgiz-specific items. For each: describe the problem, the technical root cause, and the solution implemented. Use academic tone.

1. JWT access token + HTTP-only refresh cookie with CORS and Angular withCredentials
2. SignalR hub auth via access_token query string
3. Email verification and password-reset OTP with rate limiting
4. Stripe webhook signature verification and payment state machine
5. SQL Server text column breaking EF Contains on tool description search (AI assistant)
6. GitHub Models AI integration with function calling and 503 fallback when API key missing
```

### Swagger figure captions
```
Review the Swagger UI screenshots I attached. For Chapter 4 and Appendix:
1. Add a numbered figure caption for each screenshot
2. List every API controller group and approximate endpoint count per group
3. Note which endpoints require [Authorize] vs AllowAnonymous vs Admin role
```

### ER diagram narrative
```
Using the ER diagram screenshot, write Section 3.1 database design prose:
- List every entity/table visible
- Describe cardinalities (one-to-many, many-to-many)
- Explain how Booking, Payment, Wallet, and Handover relate
- Note Identity tables (AspNetUsers, roles) if visible
```

### Abstract polish
```
Rewrite the Abstract to maximum 250 words. Include keywords:
P2P marketplace, tool rental, ASP.NET Core, Angular, Entity Framework Core, SQL Server, JWT, SignalR, Stripe, AI assistant, GitHub Models
```

### API appendix
```
Generate Appendix A: Complete API Endpoint Reference as a table with columns:
Controller | Method | Route | Auth | Brief Description

Use Swagger screenshots as source. Group by controller.
```

### Booking lifecycle diagram
```
Generate a mermaid stateDiagram-v2 for the Ehgiz booking lifecycle including:
Pending, Accepted, Rejected, DeliveryHandover, Active, ReturnHandover, Completed, Cancelled, Disputed
Include admin dispute resolution paths.
```

---

## 4. Screenshot checklist

Before sending to Claude, ensure you have:

- [ ] Swagger — full API list (scroll capture or multiple shots)
- [ ] Swagger — Auth section expanded (login, register, refresh)
- [ ] Swagger — AI section (`POST /api/ai/assistant`)
- [ ] Swagger — Admin section
- [ ] DB ER diagram — full schema (all tables + relationships)

---

## 5. Verified project facts (do not let Claude invent)

| Fact | Value |
|------|-------|
| Backend framework | ASP.NET Core .NET 10 |
| Frontend | Angular (standalone components) |
| Database | SQL Server |
| Roles | `user`, `admin` |
| AI chat persistence | **None** (client-side UI only) |
| AI model | `openai/gpt-4o-mini` via GitHub Models |
| Platform fee default | 10% |
| Refresh cookie name | `X-Refresh-Token` |
| SignalR hubs | `/hubs/chat`, `/hubs/notifications` |

**Source docs in repo:** `README.md`, `docs/API-Reference.md`

---

## 6. Suggested document title page

```
Information Technology Institute
[ITI_BRANCH] intensive program
for .NET full stack track

Ehgiz
Peer-to-Peer Tool Rental Marketplace
with AI-Assisted Discovery

By
[TEAM_MEMBER_NAMES]

[YEAR]
```
