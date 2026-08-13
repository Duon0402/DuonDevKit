using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DuonDevKit.Validation.DependencyInjection
{
    /// <summary>Discovers and registers <see cref="IValidator{T}"/> implementations.</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Scans <paramref name="assemblies"/> for concrete classes implementing <see cref="IValidator{T}"/>
        /// and registers them — see <see cref="AddDuonDevKitValidators(IServiceCollection, IEnumerable{Type})"/>
        /// for the registration behavior. Register once at startup, e.g.
        /// <c>services.AddDuonDevKitValidators(typeof(Program).Assembly)</c>.
        /// </summary>
        /// <exception cref="ArgumentException">No assembly was provided.</exception>
        public static IServiceCollection AddDuonDevKitValidators(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (assemblies.Length == 0)
                throw new ArgumentException("At least one assembly must be provided.", nameof(assemblies));

            return services.AddDuonDevKitValidators(assemblies.SelectMany(GetLoadableTypes));
        }

        /// <summary>
        /// Registers whichever of <paramref name="candidateTypes"/> are concrete classes implementing
        /// <see cref="IValidator{T}"/> (e.g. a <c>FluentValidation.AbstractValidator&lt;T&gt;</c> subclass)
        /// against their closed <c>IValidator&lt;T&gt;</c>, as <see cref="ServiceLifetime.Scoped"/>. Use
        /// this overload directly (instead of scanning a whole assembly) to register from an explicit,
        /// pre-filtered type list.
        /// </summary>
        /// <exception cref="InvalidOperationException">Two types implement the same closed <c>IValidator&lt;T&gt;</c> — whether both come from <paramref name="candidateTypes"/> or one was already registered by an earlier call — since which one would apply is otherwise undefined (type scan order isn't guaranteed).</exception>
        public static IServiceCollection AddDuonDevKitValidators(this IServiceCollection services, IEnumerable<Type> candidateTypes)
        {
            foreach (var type in candidateTypes)
            {
                // IsGenericTypeDefinition excludes an unbound generic validator (e.g. BaseValidator<T>) —
                // it can't be registered as-is and would otherwise only fail later, at BuildServiceProvider().
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                    continue;

                foreach (var implementedInterface in type.GetInterfaces())
                {
                    if (!implementedInterface.IsGenericType)
                        continue;

                    if (implementedInterface.GetGenericTypeDefinition() != typeof(IValidator<>))
                        continue;

                    var existing = services.FirstOrDefault(d => d.ServiceType == implementedInterface);
                    if (existing is not null)
                    {
                        throw new InvalidOperationException(
                            $"Both '{existing.ImplementationType?.FullName}' and '{type.FullName}' implement '{implementedInterface}'. " +
                            "Only one validator may be registered per validated type.");
                    }

                    services.AddScoped(implementedInterface, type);
                }
            }

            return services;
        }

        /// <summary>Like <see cref="Assembly.GetTypes"/>, but falls back to whatever types did load instead of throwing when some type in the assembly can't be (e.g. a missing dependency).</summary>
        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t is not null)!;
            }
        }
    }
}
