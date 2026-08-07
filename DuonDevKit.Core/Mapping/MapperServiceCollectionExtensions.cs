using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

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
        /// <exception cref="InvalidOperationException">Two types in <paramref name="candidateTypes"/> implement the same closed <see cref="IMapper{TSource, TDestination}"/>/<see cref="IUpdateMapper{TSource, TDestination}"/> — which one would apply is otherwise undefined, since type scan order isn't guaranteed.</exception>
        public static IServiceCollection AddDuonDevKitMappers(this IServiceCollection services, IEnumerable<Type> candidateTypes)
        {
            var registeredBy = new Dictionary<Type, Type>();

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

                    if (registeredBy.TryGetValue(implementedInterface, out var existing))
                    {
                        throw new InvalidOperationException(
                            $"Both '{existing.FullName}' and '{type.FullName}' implement '{implementedInterface}'. " +
                            "Only one mapper may be registered per type pair.");
                    }

                    registeredBy[implementedInterface] = type;
                    services.AddScoped(implementedInterface, type);
                }
            }

            services.AddScoped<IObjectMapper, ObjectMapper>();

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
