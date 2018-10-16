// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;

#if NET45
using System.Collections.Generic;
namespace Autofac
#else
namespace Microsoft.Extensions.DependencyInjection
#endif
{
    internal class HealthChecksBuilder : IHealthChecksBuilder
    {
#if NET45
        private ICollection<HealthCheckRegistration> _registrations = new List<HealthCheckRegistration>();
        public ICollection<HealthCheckRegistration> Build() => new List<HealthCheckRegistration>(_registrations);
#else
        public HealthChecksBuilder(IServiceCollection services)
        {
            Services = services;
        }
        public IServiceCollection Services { get; }
#endif
        public IHealthChecksBuilder Add(HealthCheckRegistration registration)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }
#if NET45
            _registrations.Add(registration);
#else
            Services.Configure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Add(registration);
            });
#endif
            return this;
        }
    }
}
