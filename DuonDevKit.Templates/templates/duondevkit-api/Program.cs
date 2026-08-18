#if (auth)
using System.Security.Claims;
#endif
using DuonDevKit.AspNetCore;
#if (dapper)
using DuonDevKit.Dapper;
using DuonDevKit.Dapper.DependencyInjection;
#endif
using DuonDevKit.EntityFrameworkCore;
using DuonDevKit.EntityFrameworkCore.Auditing;
using DuonDevKit.EntityFrameworkCore.DependencyInjection;
using DuonDevKit.EntityFrameworkCore.Extensions;
using DuonDevKit.EntityFrameworkCore.Repositories;
#if (auth)
using DuonDevKit.Jwt;
using DuonDevKit.Jwt.DependencyInjection;
#endif
#if (validation)
using DuonDevKit.Validation;
using DuonDevKit.Validation.DependencyInjection;
using FluentValidation;
#else
using DuonDevKit.AspNetCore.Validation;
using System.ComponentModel.DataAnnotations;
#endif
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=app.db")
        .AddDuonDevKitAuditing(sp));
builder.Services.AddDuonDevKitEntityFrameworkCore<AppDbContext>();
#if (dapper)
builder.Services.AddDuonDevKitDapper<AppDbContext>();
#endif
#if (auth)
builder.Services.AddDuonDevKitJwt(new JwtSettings
{
    // Falls back to a development-only key so the app runs immediately after scaffolding. Set a
    // real one before shipping: `dotnet user-secrets set "Jwt:SigningKey" "<32+ byte value>"` for
    // local dev (this project already has a UserSecretsId), or an environment variable /
    // your host's secret store (e.g. Azure Key Vault, AWS Secrets Manager) in production —
    // never commit a real signing key to source control.
    SigningKey = builder.Configuration["Jwt:SigningKey"] ?? "development-only-signing-key-change-this-before-production",
    Issuer = "DuonDevKit.ApiTemplate",
    Audience = "DuonDevKit.ApiTemplate",
});
builder.Services.AddAuthorization();
#endif
#if (validation)
builder.Services.AddDuonDevKitValidators(typeof(Program).Assembly);
#endif
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseDuonDevKitExceptionHandling();
#if (auth)
app.UseAuthentication();
app.UseAuthorization();
#endif

if (app.Environment.IsDevelopment())
{
    // GET /openapi/v1.json — pair with a UI of your choice (e.g. `dotnet add package
    // Scalar.AspNetCore` + `app.MapScalarApiReference()`, or Swagger UI) if you want one browsable.
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Applies Migrations/ (already included) instead of EnsureCreated, so schema changes going
    // forward are tracked normally: `dotnet ef migrations add <Name>` after editing the model.
    await db.Database.MigrateAsync();
}

#if (auth)
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

#endif
app.MapGet("/todos", async (IRepository<TodoItem> todos, CancellationToken ct) =>
    (await todos.ListAsync(ct: ct)).ToApiResult())
#if (auth)
    .RequireAuthorization()
#endif
    ;

var createTodo = app.MapPost("/todos", async (
    CreateTodoRequest request,
    IRepository<TodoItem> todos,
    IUnitOfWork unitOfWork,
#if (validation)
    IValidator<CreateTodoRequest> validator,
#endif
    CancellationToken ct) =>
{
#if (validation)
    var validated = validator.ValidateToResult(request);
    if (validated.IsFailure)
        return validated.ToApiResult();

#endif
    var todo = new TodoItem { Id = Guid.NewGuid().ToString("N"), Title = request.Title };
    var added = await todos.AddAsync(todo, ct);
    if (added.IsFailure)
        return added.ToApiResult();

    var saved = await unitOfWork.SaveChangesAsync(ct);
    return saved.IsFailure ? saved.ToApiResult() : Results.Created($"/todos/{todo.Id}", todo);
})
#if (auth)
    .RequireAuthorization()
#endif
    ;
#if (!validation)
createTodo.WithDuonDevKitValidation<CreateTodoRequest>();
#endif

#if (dapper)
app.MapGet("/todos/summary", async (IDapperQueries dapper, CancellationToken ct) =>
    (await dapper.QueryAsync<TodoSummary>("SELECT IsDone, COUNT(*) AS Count FROM Todos GROUP BY IsDone", ct: ct)).ToApiResult())
#if (auth)
    .RequireAuthorization()
#endif
    ;

#endif
app.Run();

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();
#if (auth)
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
#endif

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
#if (auth)
        modelBuilder.ConfigureDuonDevKitRefreshTokens();
#endif
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

#if (validation)
public sealed record CreateTodoRequest(string Title);

public sealed class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
    }
}
#else
public sealed record CreateTodoRequest([Required, MaxLength(200)] string Title);
#endif
#if (auth)
public sealed record RefreshRequest(string RefreshToken);
#endif
#if (dapper)
public sealed class TodoSummary
{
    public bool IsDone { get; init; }
    public int Count { get; init; }
}
#endif

