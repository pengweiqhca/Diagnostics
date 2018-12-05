// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using IServiceCollection = Autofac.ContainerBuilder;

namespace Autofac
{
    internal class HealthChecksBuilder : IHealthChecksBuilder
    {
        private ICollection<HealthCheckRegistration> _registrations = new List<HealthCheckRegistration>();
        public ICollection<HealthCheckRegistration> Build() => new List<HealthCheckRegistration>(_registrations);

        public HealthChecksBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }

        public IHealthChecksBuilder Add(HealthCheckRegistration registration)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            _registrations.Add(registration);

            return this;
        }
    }
}
