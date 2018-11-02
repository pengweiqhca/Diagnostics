// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecksSample
{
    public class DbConnectionHealthCheck : IHealthCheck
    {
        public DbConnectionHealthCheck(DbProviderFactory factory, string connectionString)
            : this(factory, connectionString, testQuery: null)
        {
        }

        public DbConnectionHealthCheck(DbProviderFactory factory, string connectionString, string testQuery)
        {
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            TestQuery = testQuery;
        }

        protected DbProviderFactory Factory { get; }

        protected string ConnectionString { get; }

        // This sample supports specifying a query to run as a boolean test of whether the database
        // is responding. It is important to choose a query that will return quickly or you risk
        // overloading the database.
        //
        // In most cases this is not necessary, but if you find it necessary, choose a simple query such as 'SELECT 1'.
        protected string TestQuery { get; }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var connection = Factory.CreateConnection())
            {
                try
                {
                    connection.ConnectionString = ConnectionString;
                    await connection.OpenAsync(cancellationToken);

                    if (TestQuery != null)
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = TestQuery;

                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                catch (DbException ex)
                {
                    return HealthCheckResult.Failed(exception: ex);
                }
            }

            return HealthCheckResult.Passed();
        }
    }
}
