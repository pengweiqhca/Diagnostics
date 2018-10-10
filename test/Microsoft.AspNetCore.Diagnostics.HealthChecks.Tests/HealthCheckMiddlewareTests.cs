// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.HealthChecks
{
    public class HealthCheckMiddlewareTests
    {
        [Fact] // Matches based on '.Map'
        public async Task IgnoresRequestThatDoesNotMatchPath()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/frob");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact] // Matches based on '.Map'
        public async Task MatchIsCaseInsensitive()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/HEALTH");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ReturnsPlainTextStatus()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task StatusCodeIs200IfNoChecks()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task StatusCodeIs200IfAllChecksHealthy()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddCheck("Foo", () => HealthCheckResult.Passed("A-ok!"))
                        .AddCheck("Bar", () => HealthCheckResult.Passed("A-ok!"))
                        .AddCheck("Baz", () => HealthCheckResult.Passed("A-ok!"));
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task StatusCodeIs200IfCheckIsDegraded()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddCheck("Foo", () => HealthCheckResult.Passed("A-ok!"))
                        .AddCheck("Bar", () => HealthCheckResult.Failed("Not so great."), failureStatus: HealthStatus.Degraded)
                        .AddCheck("Baz", () => HealthCheckResult.Passed("A-ok!"));
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal("Degraded", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task StatusCodeIs503IfCheckIsUnhealthy()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddAsyncCheck("Foo", () => Task.FromResult(HealthCheckResult.Passed("A-ok!")))
                        .AddAsyncCheck("Bar", () => Task.FromResult(HealthCheckResult.Failed("Pretty bad.")))
                        .AddAsyncCheck("Baz", () => Task.FromResult(HealthCheckResult.Passed("A-ok!")));
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task StatusCodeIs500IfCheckIsFailed()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddAsyncCheck("Foo", () => Task.FromResult(HealthCheckResult.Passed("A-ok!")))
                        .AddAsyncCheck("Bar", () => throw null)
                        .AddAsyncCheck("Baz", () => Task.FromResult(HealthCheckResult.Passed("A-ok!")));
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal("Failed", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CanUseCustomWriter()
        {
            var expectedJson = JsonConvert.SerializeObject(new
            {
                status = "Unhealthy",
            });

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        ResponseWriter = (c, r) =>
                        {
                            var json = JsonConvert.SerializeObject(new { status = r.Status.ToString(), });
                            c.Response.ContentType = "application/json";
                            return c.Response.WriteAsync(json);
                        },
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddAsyncCheck("Foo", () => Task.FromResult(HealthCheckResult.Passed("A-ok!")))
                        .AddAsyncCheck("Bar", () => Task.FromResult(HealthCheckResult.Failed("Pretty bad.")))
                        .AddAsyncCheck("Baz", () => Task.FromResult(HealthCheckResult.Passed("A-ok!")));
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.Equal("application/json", response.Content.Headers.ContentType.ToString());

            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedJson, result);
        }

        [Fact]
        public async Task NoResponseWriterReturnsEmptyBody()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        ResponseWriter = null,
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddAsyncCheck("Foo", () => Task.FromResult(HealthCheckResult.Passed("A-ok!")))
                        .AddAsyncCheck("Bar", () => Task.FromResult(HealthCheckResult.Failed("Pretty bad.")))
                        .AddAsyncCheck("Baz", () => Task.FromResult(HealthCheckResult.Passed("A-ok!")));
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CanSetCustomStatusCodes()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        ResultStatusCodes =
                        {
                            [HealthStatus.Healthy] = 201,
                        }
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task SetsCacheHeaders()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health");
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
            Assert.Equal("no-store, no-cache", response.Headers.CacheControl.ToString());
            Assert.Equal("no-cache", response.Headers.Pragma.ToString());
            Assert.Equal(new string[] { "Thu, 01 Jan 1970 00:00:00 GMT" }, response.Content.Headers.GetValues(HeaderNames.Expires));
        }

        [Fact]
        public async Task CanSuppressCacheHeaders()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        SuppressCacheHeaders = true,
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
            Assert.Null(response.Headers.CacheControl);
            Assert.Empty(response.Headers.Pragma.ToString());
            Assert.False(response.Content.Headers.Contains(HeaderNames.Expires));
        }

        [Fact]
        public async Task CanFilterChecks()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        Predicate = (check) => check.Name == "Foo" || check.Name == "Baz",
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddAsyncCheck("Foo", () => Task.FromResult(HealthCheckResult.Passed("A-ok!")))
                        // Will get filtered out
                        .AddAsyncCheck("Bar", () => Task.FromResult(HealthCheckResult.Failed("A-ok!")))
                        .AddAsyncCheck("Baz", () => Task.FromResult(HealthCheckResult.Passed("A-ok!")));
                });
            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CanListenWithoutPath_AcceptsRequest()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHealthChecks(default);
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("http://localhost:5001/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CanListenOnPort_AcceptsRequest_OnSpecifiedPort()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Use(next => async (context) =>
                    {
                        // Need to fake setting the connection info. TestServer doesn't
                        // do that, because it doesn't have a connection.
                        context.Connection.LocalPort = context.Request.Host.Port.Value;
                        await next(context);
                    });

                    app.UseHealthChecks("/health", port: 5001);
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("http://localhost:5001/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CanListenOnPortWithoutPath_AcceptsRequest_OnSpecifiedPort()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Use(next => async (context) =>
                    {
                        // Need to fake setting the connection info. TestServer doesn't
                        // do that, because it doesn't have a connection.
                        context.Connection.LocalPort = context.Request.Host.Port.Value;
                        await next(context);
                    });

                    app.UseHealthChecks(default, port: 5001);
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("http://localhost:5001/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CanListenOnPort_RejectsRequest_OnOtherPort()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Use(next => async (context) =>
                    {
                        // Need to fake setting the connection info. TestServer doesn't
                        // do that, because it doesn't have a connection.
                        context.Connection.LocalPort = context.Request.Host.Port.Value;
                        await next(context);
                    });

                    app.UseHealthChecks("/health", port: 5001);
                })
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks();
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("http://localhost:5000/health");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }


    }
}
