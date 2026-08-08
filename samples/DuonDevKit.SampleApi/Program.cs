using System.Security.Claims;
using DuonDevKit.AspNetCore;
using DuonDevKit.Dapper;
using DuonDevKit.Dapper.DependencyInjection;
using DuonDevKit.EntityFrameworkCore;
using DuonDevKit.EntityFrameworkCore.Auditing;
using DuonDevKit.EntityFrameworkCore.DependencyInjection;
using DuonDevKit.EntityFrameworkCore.Extensions;
using DuonDevKit.EntityFrameworkCore.Repositories;
using DuonDevKit.Jwt;
using DuonDevKit.Jwt.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")));
builder.Services.AddDbContext<SampleDbContext>((sp, options) =>
    options.UseSqlite("Data Source=duondevkit-sample.db").AddDuonDevKitAuditing(sp));
builder.Services.AddDuonDevKitEntityFrameworkCore<SampleDbContext>();
builder.Services.AddDuonDevKitDapper<SampleDbContext>();
builder.Services.AddDuonDevKitJwt(new JwtSettings
{
    // Development-only key. Load this from user secrets or a secret store in production.
    SigningKey = "development-only-signing-key-change-this-before-production-2026",
    Issuer = "DuonDevKit.SampleApi",
    Audience = "DuonDevKit.SampleApi",
});
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseDuonDevKitExceptionHandling();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapPost("/auth/demo", async (IJwtTokenGenerator tokens, IRefreshTokenService refreshTokens, CancellationToken ct) =>
{
    const string userId = "demo-user";
    var refreshResult = await refreshTokens.IssueAsync(userId, ct);
    return refreshResult.Map(refreshToken => new
    {
        accessToken = tokens.GenerateAccessToken([new Claim(ClaimTypes.NameIdentifier, userId)]),
        refreshToken,
    }).ToApiResult();
});

app.MapPost("/auth/refresh", async (RefreshRequest request, IJwtTokenGenerator tokens, IRefreshTokenService refreshTokens, CancellationToken ct) =>
{
    var rotation = await refreshTokens.RotateAsync(request.RefreshToken, ct);
    return rotation.Map(value => new
    {
        accessToken = tokens.GenerateAccessToken([new Claim(ClaimTypes.NameIdentifier, value.UserId)]),
        refreshToken = value.NewRefreshToken,
    }).ToApiResult();
});

app.MapGet("/todos", async (IRepository<TodoItem> todos, CancellationToken ct) =>
    (await todos.ListAsync(ct: ct)).ToApiResult()).RequireAuthorization();

app.MapPost("/todos", async (CreateTodoRequest request, IRepository<TodoItem> todos, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    var todo = new TodoItem { Id = Guid.NewGuid().ToString("N"), Title = request.Title };
    var added = await todos.AddAsync(todo, ct);
    if (added.IsFailure)
        return added.ToApiResult();

    var saved = await unitOfWork.SaveChangesAsync(ct);
    return saved.IsFailure ? saved.ToApiResult() : Results.Created($"/todos/{todo.Id}", todo);
}).RequireAuthorization();

app.MapGet("/todos/summary", async (IDapperQueries dapper, CancellationToken ct) =>
    (await dapper.QueryAsync<TodoSummary>("SELECT IsDone, COUNT(*) AS Count FROM Todos GROUP BY IsDone", ct: ct)).ToApiResult())
    .RequireAuthorization();

app.Run();

public sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureDuonDevKitRefreshTokens();
        modelBuilder.ApplySoftDeleteQueryFilter();
    }
}

public sealed class TodoItem : BaseEntity, ICanCreate, ICanUpdate, ISoftDelete
{
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

public sealed record CreateTodoRequest(string Title);
public sealed record RefreshRequest(string RefreshToken);
public sealed class TodoSummary
{
    public bool IsDone { get; init; }
    public int Count { get; init; }
}
