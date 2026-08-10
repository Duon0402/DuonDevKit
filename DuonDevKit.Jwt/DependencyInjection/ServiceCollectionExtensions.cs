using System.Text;
using DuonDevKit.EntityFrameworkCore.Auditing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace DuonDevKit.Jwt.DependencyInjection
{
    /// <summary>Registers JWT issuance/validation and refresh-token handling.</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <paramref name="settings"/>, <see cref="IJwtTokenGenerator"/>, <see cref="IRefreshTokenService"/>
        /// (backed by <c>IRepository&lt;RefreshToken, string&gt;</c>/<c>IUnitOfWork</c> — call this after
        /// <c>AddDuonDevKitEntityFrameworkCore&lt;TContext&gt;</c>), <see cref="HttpContextCurrentUserProvider"/>
        /// as <see cref="ICurrentUserProvider"/> (taking over from <c>AddDuonDevKitEntityFrameworkCore</c>'s
        /// <c>NullCurrentUserProvider</c> fallback specifically, but never from an app-supplied one — whether
        /// registered before or after this call), and the <see cref="JwtBearerDefaults.AuthenticationScheme"/>
        /// authentication handler validating tokens against the same <paramref name="settings"/>.
        /// </summary>
        public static IServiceCollection AddDuonDevKitJwt(this IServiceCollection services, JwtSettings settings)
        {
            var keyLength = Encoding.UTF8.GetByteCount(settings.SigningKey);
            if (keyLength < 32)
                throw new ArgumentException($"JwtSettings.SigningKey must be at least 32 bytes (256 bits) of entropy for HMAC-SHA256; got {keyLength} bytes.", nameof(settings));

            services.AddSingleton(settings);
            services.AddHttpContextAccessor();

            var existingCurrentUserProvider = services.LastOrDefault(d => d.ServiceType == typeof(ICurrentUserProvider));
            if (existingCurrentUserProvider is null || existingCurrentUserProvider.ImplementationType == typeof(NullCurrentUserProvider))
                services.Replace(ServiceDescriptor.Scoped<ICurrentUserProvider, HttpContextCurrentUserProvider>());

            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = settings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = settings.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                    };
                });

            return services;
        }
    }
}
