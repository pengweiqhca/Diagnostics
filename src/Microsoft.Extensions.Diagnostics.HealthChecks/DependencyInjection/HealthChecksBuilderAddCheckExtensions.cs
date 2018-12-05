// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;

namespace Autofac
{
    /// <summary>
    /// Provides basic extension methods for registering <see cref="IHealthCheck"/> instances in an <see cref="IHealthChecksBuilder"/>.
    /// </summary>
    public static class HealthChecksBuilderAddCheckExtensions
    {
        /// <summary>
        /// Adds a new health check with the specified name and implementation.
        /// </summary>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
        /// <param name="name">The name of the health check.</param>
        /// <param name="instance">An <see cref="IHealthCheck"/> instance.</param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> that should be reported when the health check reports a failure. If the provided value
        /// is <c>null</c>, then <see cref="HealthStatus.Unhealthy"/> will be reported.
        /// </param>
        /// <param name="tags">A list of tags that can be used to filter health checks.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/>.</returns>
        public static IHealthChecksBuilder AddCheck(
            this IHealthChecksBuilder builder,
            string name,
            IHealthCheck instance,
            HealthStatus? failureStatus = null,
            IEnumerable<string> tags = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return builder.Add(new HealthCheckRegistration(name, instance, failureStatus, tags));
        }
    }
}
