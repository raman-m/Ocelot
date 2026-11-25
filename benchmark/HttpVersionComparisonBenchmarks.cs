using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace Ocelot.Benchmarks;

/// <summary>
/// Benchmark comparing HTTP/1.1 vs HTTP/2 performance in Ocelot.
/// This benchmark measures latency, throughput, and connection overhead
/// when using different HTTP versions for downstream communication.
/// </summary>
[Config(typeof(HttpVersionComparisonBenchmarks))]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(warmupCount: 3, targetCount: 5)]
public sealed class HttpVersionComparisonBenchmarks : ManualConfig, IDisposable
{
    private readonly BenchmarkSteps steps = new();
    private static readonly string TestPayload = GenerateTestPayload(1024); // 1KB payload

    public void Dispose() => steps.Dispose();

    public HttpVersionComparisonBenchmarks()
    {
        AddColumn(StatisticColumn.AllStatistics);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddValidator(BaselineValidator.FailOnError);
    }

    [GlobalSetup]
    public void SetUp()
    {
        var port = PortFinder.GetRandomPort();
        
        // Setup downstream service
        var route = steps.GivenDefaultRoute(port);
        var configuration = steps.GivenConfiguration(route);
        
        steps.GivenThereIsAServiceRunningOnKestrel(port, "/", 200,
            options => options.Protocols = HttpProtocols.Http1AndHttp2,
            nameof(HttpVersionComparisonBenchmarks));
        
        steps.GivenThereIsAConfiguration(configuration);
    }

    /// <summary>
    /// Baseline: Ocelot with HTTP/1.1
    /// </summary>
    [Benchmark(Baseline = true)]
    [Arguments(1, 100)]  // 1 concurrent request, 100 iterations
    [Arguments(10, 100)] // 10 concurrent requests, 100 iterations
    [Arguments(50, 50)]  // 50 concurrent requests, 50 iterations
    public async Task OcelotHttp11(int concurrentRequests, int iterations)
    {
        var config = CreateHttpVersionConfig("1.1");
        await RunOcelotGateway(config, concurrentRequests, iterations);
    }

    /// <summary>
    /// Test: Ocelot with HTTP/2
    /// </summary>
    [Benchmark]
    [Arguments(1, 100)]
    [Arguments(10, 100)]
    [Arguments(50, 50)]
    public async Task OcelotHttp2(int concurrentRequests, int iterations)
    {
        var config = CreateHttpVersionConfig("2.0");
        await RunOcelotGateway(config, concurrentRequests, iterations);
    }

    /// <summary>
    /// Test: HTTP/2 with RequestVersionOrLower policy
    /// This policy allows the gateway to negotiate the best version
    /// </summary>
    [Benchmark]
    [Arguments(1, 100)]
    [Arguments(10, 100)]
    [Arguments(50, 50)]
    public async Task OcelotHttp2WithNegotiation(int concurrentRequests, int iterations)
    {
        var config = CreateHttpVersionConfigWithPolicy("2.0", "RequestVersionOrLower");
        await RunOcelotGateway(config, concurrentRequests, iterations);
    }

    /// <summary>
    /// Test: HTTP/2 with Downgrade policy
    /// This policy allows downgrading to HTTP/1.1 if needed
    /// </summary>
    [Benchmark]
    [Arguments(1, 100)]
    [Arguments(10, 100)]
    [Arguments(50, 50)]
    public async Task OcelotHttp2WithDowngrade(int concurrentRequests, int iterations)
    {
        var config = CreateHttpVersionConfigWithPolicy("2.0", "AcceptDowngrade");
        await RunOcelotGateway(config, concurrentRequests, iterations);
    }

    private async Task RunOcelotGateway(string configuration, int concurrentRequests, int iterations)
    {
        steps.GivenThereIsAConfiguration(configuration);
        
        steps.GivenOcelotIsRunning(host => host
            .ConfigureLogging((hosting, logging) => 
                logging.AddConfiguration(hosting.Configuration.GetSection("Logging"))));

        try
        {
            var tasks = new Task[concurrentRequests];
            for (int i = 0; i < iterations; i++)
            {
                for (int j = 0; j < concurrentRequests; j++)
                {
                    tasks[j] = SendRequestAsync();
                }
                await Task.WhenAll(tasks);
            }
        }
        finally
        {
            steps.Dispose();
        }
    }

    private async Task SendRequestAsync()
    {
        using var client = new HttpClient();
        using var content = new StringContent(TestPayload, Encoding.UTF8, "application/json");
        try
        {
            await client.PostAsync("http://localhost:5000/", content);
        }
        catch
        {
            // Swallow exceptions for benchmarking
        }
    }

    private string CreateHttpVersionConfig(string version)
    {
        return $$"""
{
  "Routes": [{
    "DownstreamPathTemplate": "/",
    "DownstreamScheme": "https",
    "DownstreamHostAndPorts": [{
      "Host": "localhost",
      "Port": 5000
    }],
    "UpstreamPathTemplate": "/",
    "UpstreamHttpMethod": ["POST", "GET"],
    "DownstreamHttpVersion": "{{version}}"
  }],
  "GlobalConfiguration": {}
}
""";
    }

    private string CreateHttpVersionConfigWithPolicy(string version, string policy)
    {
        return $$"""
{
  "Routes": [{
    "DownstreamPathTemplate": "/",
    "DownstreamScheme": "https",
    "DownstreamHostAndPorts": [{
      "Host": "localhost",
      "Port": 5000
    }],
    "UpstreamPathTemplate": "/",
    "UpstreamHttpMethod": ["POST", "GET"],
    "DownstreamHttpVersion": "{{version}}",
    "DownstreamHttpVersionPolicy": "{{policy}}"
  }],
  "GlobalConfiguration": {}
}
""";
    }

    private static string GenerateTestPayload(int sizeInBytes)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"id\": \"test-123\",");
        sb.Append("\"timestamp\": \"2024-11-25T12:00:00Z\",");
        sb.Append("\"data\": \"");
        
        // Fill with repeated data to reach desired size
        var targetSize = sizeInBytes - 100; // Account for JSON structure
        while (sb.Length < targetSize)
        {
            sb.Append("Lorem ipsum dolor sit amet, consectetur adipiscing elit. ");
        }
        
        sb.Append("\"}");
        return sb.ToString();
    }
}
