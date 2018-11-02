using System.Data.Common;
using System.Data.SqlClient;

namespace HealthChecksSample
{
    public class SqlConnectionHealthCheck : DbConnectionHealthCheck
    {
        private static readonly string DefaultTestQuery = "Select 1";

        public SqlConnectionHealthCheck(string connectionString)
            : this(connectionString, testQuery: DefaultTestQuery)
        {
        }

        public SqlConnectionHealthCheck(string connectionString, string testQuery)
            : base(SqlClientFactory.Instance, connectionString, testQuery ?? DefaultTestQuery)
        {
        }
    }
}
