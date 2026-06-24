using Ehgiz.API.Extensions;
using Ehgiz.Application;
using Ehgiz.DAL;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicy();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDalServices();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddSignalRServices();
builder.Services.AddControllers();
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 10 * 1024 * 1024);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.BufferBody = true);
builder.Services.AddSwaggerWithAuth();

var app = builder.Build();

await app.UseSwaggerInDevelopmentAsync();
app.UseApplicationMiddleware();
app.MapControllers();
app.MapApplicationHubs();

app.Run();
