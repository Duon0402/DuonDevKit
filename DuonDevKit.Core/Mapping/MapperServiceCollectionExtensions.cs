using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DuonDevKit.Core.Mapping
{
    /// <summary>Discovers and registers <see cref="IMapper{TSource, TDestination}"/>/<see cref="IUpdateMapper{TSource, TDestination}"/> implementations.</summary>
    public static class MapperServiceCollectionExtensions
    {
        /// <summary>
        /// Scans <paramref name="assemblies"/> for concrete classes implementing
        /// <see cref="IMapper{TSource, TDestination}"/> and/or <see cref="IUpdateMapper{TSource, TDestination}"/>
        /// and registers them — see <see cref="AddDuonDevKitMappers(IServiceCollection, IEnumerable{Type})"/>
        /// for the registration behavior. Register once at startup, e.g.
        /// <c>services.AddDuonDevKitMappers(typeof(Program).Assembly)</c>.
        /// </summary>
        public static IServiceCollection AddDuonDevKitMappers(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (assemblies.Length == 0)
                throw new ArgumentException("At least one assembly must be provided.", nameof(assemblies));

            return services.AddDuonDevKitMappers(assemblies.SelectMany(GetLoadableTypes));
        }

        /// <summary>
        /// Registers whichever of <paramref name="candidateTypes"/> are concrete classes implementing
        /// <see cref="IMapper{TSource, TDestination}"/> and/or <see cref="IUpdateMapper{TSource, TDestination}"/>
        /// against their interface, and registers <see cref="IObjectMapper"/> as an <see cref="ObjectMapper"/>
        /// resolving them. Use this overload directly (instead of scanning whole assemblies) to register from
        /// an explicit, pre-filtered type list.
        /// </summary>
        /// <exception cref="InvalidOperationException">Two types implement the same closed <see cref="IMapper{TSource, TDestination}"/>/<see cref="IUpdateMapper{TSource, TDestination}"/> — whether both come from <paramref name="candidateTypes"/> or one was already registered by an earlier call — since which one would apply is otherwise undefined (type scan order isn't guaranteed).</exception>
        public static IServiceCollection AddDuonDevKitMappers(this IServiceCollection services, IEnumerable<Type> candidateTypes)
        {
            foreach (var type in candidateTypes)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                foreach (var implementedInterface in type.GetInterfaces())
                {
                    if (!implementedInterface.IsGenericType)
                        continue;

                    var definition = implementedInterface.GetGenericTypeDefinition();
                    if (definition != typeof(IMapper<,>) && definition != typeof(IUpdateMapper<,>))
                        continue;

                    var existing = services.FirstOrDefault(d => d.ServiceType == implementedInterface);
                    if (existing is not null)
                    {
                        throw new InvalidOperationException(
                            $"Both '{existing.ImplementationType?.FullName}' and '{type.FullName}' implement '{implementedInterface}'. " +
                            "Only one mapper may be registered per type pair.");
                    }

                    services.AddScoped(implementedInterface, type);
                }
            }

            services.TryAddScoped<IObjectMapper, ObjectMapper>();

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
