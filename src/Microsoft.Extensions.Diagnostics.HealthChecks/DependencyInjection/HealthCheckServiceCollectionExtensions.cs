// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
#if NET45
namespace Autofac
#else
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace Microsoft.Extensions.DependencyInjection
#endif
{
    /// <summary>
    /// Provides extension methods for registering <see cref="HealthCheckService"/> in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class HealthCheckServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="HealthCheckService"/> to the container, using the provided delegate to register
        /// health checks.
        /// </summary>
        /// <remarks>
        /// This operation is idempotent - multiple invocations will still only result in a single
        /// <see cref="HealthCheckService"/> instance in the <see cref="IServiceCollection"/>. It can be invoked
        /// multiple times in order to get access to the <see cref="IHealthChecksBuilder"/> in multiple places.
        /// </remarks>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the <see cref="HealthCheckService"/> to.</param>
        /// <returns>An instance of <see cref="IHealthChecksBuilder"/> from which health checks can be registered.</returns>
#if NET45
        public static IHealthChecksBuilder AddHealthChecks(this ContainerBuilder services)
        {
            services.RegisterType<DefaultHealthCheckService>().As<HealthCheckService>().SingleInstance();
            return new HealthChecksBuilder();
#else
        public static IHealthChecksBuilder AddHealthChecks(this IServiceCollection services)
        {
            services.TryAddSingleton<HealthCheckService, DefaultHealthCheckService>();
            return new HealthChecksBuilder(services);
#endif
        }
    }
}
