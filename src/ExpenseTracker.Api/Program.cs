using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

//DB - schema is owned by ExpenseTracker.Db (DACPAC); EF is used for queries only
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()));

//CORS - which frontends may call this API (Azure overrides via Cors__AllowedOrigins__0)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

//Auth0 - validate JWT access tokens
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
        options.Audience = builder.Configuration["Auth0:Audience"];
    });

//Blob Storage - no account key: developer login locally, Managed Identity in Azure
TokenCredential credential = builder.Environment.IsDevelopment()
    ? new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        TenantId = builder.Configuration["Azure:TenantId"],
        ExcludeManagedIdentityCredential = true
    })
    : new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
builder.Services.AddSingleton(new BlobServiceClient(
    new Uri($"https://{builder.Configuration["Storage:AccountName"]}.blob.core.windows.net"), credential));
builder.Services.AddSingleton<BlobFileStore>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
