// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
#if NET45
namespace Autofac
#else
namespace Microsoft.Extensions.DependencyInjection
#endif
{
    /// <summary>
    /// A builder used to register health checks.
    /// </summary>
    public interface IHealthChecksBuilder
    {
        /// <summary>
        /// Adds a <see cref="HealthCheckRegistration"/> for a health check.
        /// </summary>
        /// <param name="registration">The <see cref="HealthCheckRegistration"/>.</param>
        IHealthChecksBuilder Add(HealthCheckRegistration registration);
#if !NET45
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> into which <see cref="IHealthCheck"/> instances should be registered.
        /// </summary>
        IServiceCollection Services { get; }
#endif
    }
}
