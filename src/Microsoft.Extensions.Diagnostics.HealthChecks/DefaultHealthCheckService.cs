// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Autofac;
using Autofac.Core.Lifetime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IServiceScopeFactory = Autofac.ILifetimeScope;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    internal class DefaultHealthCheckService : HealthCheckService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HealthCheckServiceOptions _options;

        public DefaultHealthCheckService(
            IServiceScopeFactory scopeFactory,
            HealthCheckServiceOptions options)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            // We're specifically going out of our way to do this at startup time. We want to make sure you
            // get any kind of health-check related error as early as possible. Waiting until someone
            // actually tries to **run** health checks would be real baaaaad.
            ValidateRegistrations(_options.Registrations);
        }
        public override async Task<HealthReport> CheckHealthAsync(
            Func<HealthCheckRegistration, bool> predicate,
            CancellationToken cancellationToken = default)
        {
            var registrations = _options.Registrations;

            using (var scope = _scopeFactory.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag))
            {
                var provider = new AutofacServiceProvider(scope);

                var context = new HealthCheckContext();
                var entries = new Dictionary<string, HealthReportEntry>(StringComparer.OrdinalIgnoreCase);

                var totalTime = ValueStopwatch.StartNew();

                foreach (var registration in registrations)
                {
                    if (predicate != null && !predicate(registration))
                    {
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    var healthCheck = registration.Factory(provider);

                        var stopwatch = ValueStopwatch.StartNew();
                        context.Registration = registration;

                        HealthReportEntry entry;
                        try
                        {
                            var result = await healthCheck.CheckHealthAsync(context, cancellationToken);
                            var duration = stopwatch.GetElapsedTime();

                            entry = new HealthReportEntry(
                                status: result.Status,
                                description: result.Description,
                                duration: duration,
                                exception: result.Exception,
                                data: result.Data);

                        }

                        // Allow cancellation to propagate.
                        catch (Exception ex) when (ex as OperationCanceledException == null)
                        {
                            var duration = stopwatch.GetElapsedTime();
                            entry = new HealthReportEntry(
                                status: HealthStatus.Unhealthy,
                                description: ex.Message,
                                duration: duration,
                                exception: ex,
                                data: null);

                        }

                        entries[registration.Name] = entry;
                }

                var totalElapsedTime = totalTime.GetElapsedTime();
                var report = new HealthReport(entries, totalElapsedTime);
                return report;
            }
        }

        private static void ValidateRegistrations(IEnumerable<HealthCheckRegistration> registrations)
        {
            // Scan the list for duplicate names to provide a better error if there are duplicates.
            var duplicateNames = registrations
                .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateNames.Count > 0)
            {
                throw new ArgumentException($"Duplicate health checks were registered with the name(s): {string.Join(", ", duplicateNames)}", nameof(registrations));
            }
        }

        internal class HealthCheckDataLogValue : IReadOnlyList<KeyValuePair<string, object>>
        {
            private readonly string _name;
            private readonly List<KeyValuePair<string, object>> _values;

            private string _formatted;

            public HealthCheckDataLogValue(string name, IReadOnlyDictionary<string, object> values)
            {
                _name = name;
                _values = values.ToList();

                // We add the name as a kvp so that you can filter by health check name in the logs.
                // This is the same parameter name used in the other logs.
                _values.Add(new KeyValuePair<string, object>("HealthCheckName", name));
            }

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    if (index < 0 || index >= Count)
                    {
                        throw new IndexOutOfRangeException(nameof(index));
                    }

                    return _values[index];
                }
            }

            public int Count => _values.Count;

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return _values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _values.GetEnumerator();
            }

            public override string ToString()
            {
                if (_formatted == null)
                {
                    var builder = new StringBuilder();
                    builder.AppendLine($"Health check data for {_name}:");

                    var values = _values;
                    for (var i = 0; i < values.Count; i++)
                    {
                        var kvp = values[i];
                        builder.Append("    ");
                        builder.Append(kvp.Key);
                        builder.Append(": ");

                        builder.AppendLine(kvp.Value?.ToString());
                    }

                    _formatted = builder.ToString();
                }

                return _formatted;
            }
        }

        internal class AutofacServiceProvider : IServiceProvider
        {
            private readonly ILifetimeScope _scope;

            public AutofacServiceProvider(ILifetimeScope scope) => _scope = scope;

            public object GetService(Type serviceType) => _scope.ResolveOptional(serviceType);
        }
    }
}
