using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PhotoToPdf.Api.Data;
using PhotoToPdf.Api.Dtos;
using PhotoToPdf.Api.Models;
using PhotoToPdf.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Port=5432;Database=phototopdf;Username=postgres;Password=postgres"));

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPdfService, PdfService>();

var frontendOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
[
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "https://photo-to-pdf-xi.vercel.app"
];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(frontendOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "SuperStrongPasswordForLocalDevelopment123!");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "PhotoToPdfApp",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "PhotoToPdfClient",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", message = "PhotoToPdf API ishlayapti." }));

app.MapPost("/api/auth/register", async (AppDbContext db, IJwtTokenService jwtTokenService, RegisterRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.FullName) ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Barcha maydonlar to'ldirilishi kerak." });
    }

    var normalizedEmail = request.Email.Trim();
    if (await db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail.ToLower()))
    {
        return Results.Conflict(new { message = "Bu email ro'yxatdan o'tgan." });
    }

    var user = new AppUser
    {
        FullName = request.FullName.Trim(),
        Email = normalizedEmail,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    var token = jwtTokenService.GenerateToken(user);
    return Results.Ok(new AuthResponse(token, user.FullName, user.Email));
});

app.MapPost("/api/auth/login", async (AppDbContext db, IJwtTokenService jwtTokenService, LoginRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Email va parol kerak." });
    }

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower());
    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }

    var token = jwtTokenService.GenerateToken(user);
    return Results.Ok(new AuthResponse(token, user.FullName, user.Email));
});

app.MapGet("/api/auth/me", [Authorize] async (ClaimsPrincipal user, AppDbContext db) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var appUser = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

    if (appUser is null) return Results.NotFound();
    return Results.Ok(new { id = appUser.Id, fullName = appUser.FullName, email = appUser.Email, createdAt = appUser.CreatedAt });
});

app.MapGet("/api/history", [Authorize] async (ClaimsPrincipal user, AppDbContext db) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var history = await db.UserHistory
        .Where(h => h.UserId == userId)
        .OrderByDescending(h => h.CreatedAt)
        .Select(h => new
        {
            id = h.Id,
            operation = h.Operation,
            sourceFileName = h.SourceFileName,
            outputFileNames = h.OutputFileNames,
            fileSizeBytes = h.FileSizeBytes,
            createdAt = h.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(history);
});

app.MapPost("/api/convert/photo-to-pdf", [Authorize] async (HttpContext httpContext, AppDbContext db, IPdfService pdfService) =>
{
    if (!httpContext.Request.HasFormContentType)
    {
        return Results.BadRequest(new { message = "Form-data format kerak." });
    }

    var form = await httpContext.Request.ReadFormAsync();
    var files = form.Files.Where(f => f.Length > 0).ToList();
    if (!files.Any())
    {
        return Results.BadRequest(new { message = "Hech qanday rasm tanlanmadi." });
    }

    var pdfBytes = pdfService.ConvertImagesToPdf(files);
    var userId = Guid.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    db.UserHistory.Add(new UserHistoryItem
    {
        UserId = userId,
        Operation = "PhotoToPdf",
        SourceFileName = string.Join(", ", files.Select(f => Path.GetFileName(f.FileName))),
        OutputFileNames = "converted-file.pdf",
        FileSizeBytes = pdfBytes.LongLength,
        CreatedAt = DateTime.UtcNow
    });
    await db.SaveChangesAsync();

    return Results.File(pdfBytes, "application/pdf", $"converted-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
});

app.MapPost("/api/convert/pdf-to-images", [Authorize] async (HttpContext httpContext, AppDbContext db, IPdfService pdfService) =>
{
    if (!httpContext.Request.HasFormContentType)
    {
        return Results.BadRequest(new { message = "Form-data format kerak." });
    }

    var form = await httpContext.Request.ReadFormAsync();
    var file = form.Files.FirstOrDefault(f => f.Length > 0);
    if (file is null)
    {
        return Results.BadRequest(new { message = "PDF fayli tanlanmadi." });
    }

    using var stream = file.OpenReadStream();
    var zipBytes = pdfService.ConvertPdfToImages(stream);

    var userId = Guid.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    db.UserHistory.Add(new UserHistoryItem
    {
        UserId = userId,
        Operation = "PdfToImages",
        SourceFileName = file.FileName,
        OutputFileNames = "pdf-export.zip",
        FileSizeBytes = zipBytes.LongLength,
        CreatedAt = DateTime.UtcNow
    });
    await db.SaveChangesAsync();

    return Results.File(zipBytes, "application/zip", $"pdf-export-{DateTime.UtcNow:yyyyMMddHHmmss}.zip");
});

app.Run();

