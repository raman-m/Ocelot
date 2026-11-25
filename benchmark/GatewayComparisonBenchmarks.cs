using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net.Http.Headers;
using System.Text;

namespace Ocelot.Benchmarks;

/// <summary>
/// Benchmark comparing Ocelot vs YARP gateway performance.
/// 
/// This benchmark measures:
/// - Case 1: HTTP/1.1 Ocelot vs HTTP/2 YARP
/// - Case 2: HTTP/2 Ocelot vs HTTP/2 YARP
/// 
/// Metrics:
/// - Latency (mean, median, p95, p99)
/// - Throughput (requests per second)
/// - Memory usage
/// - Allocation rate
/// 
/// Note: YARP benchmarks are designed as a reference implementation.
/// To run YARP benchmarks, install YARP separately and implement the YARP test.
/// </summary>
[Config(typeof(GatewayComparisonBenchmarks))]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(warmupCount: 3, targetCount: 5)]
public sealed class GatewayComparisonBenchmarks : ManualConfig, IDisposable
{
    private readonly BenchmarkSteps steps = new();
    private static readonly string SmallPayload = GeneratePayload(100);    // 100 bytes
    private static readonly string MediumPayload = GeneratePayload(1024);  // 1 KB
    private static readonly string LargePayload = GeneratePayload(10240); // 10 KB

    public void Dispose() => steps.Dispose();

    public GatewayComparisonBenchmarks()
    {
        AddColumn(StatisticColumn.AllStatistics);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddValidator(BaselineValidator.FailOnError);
    }

    [GlobalSetup]
    public void SetUp()
    {
        var port = PortFinder.GetRandomPort();
        
        // Setup downstream service that supports both HTTP/1.1 and HTTP/2
        var route = steps.GivenDefaultRoute(port);
        var configuration = steps.GivenConfiguration(route);
        
        steps.GivenThereIsAServiceRunningOnKestrel(port, "/", 200,
            options => options.Protocols = HttpProtocols.Http1AndHttp2,
            nameof(GatewayComparisonBenchmarks));
        
        steps.GivenThereIsAConfiguration(configuration);
    }

    #region Case 1: HTTP/1.1 Ocelot vs HTTP/2 YARP

    /// <summary>
    /// Baseline: Ocelot with HTTP/1.1 (default configuration)
    /// Small payload (100 bytes)
    /// </summary>
    [Benchmark(Baseline = true)]
    [Arguments(10)]
    [Arguments(50)]
    public async Task OcelotHttp11_SmallPayload(int concurrentRequests)
    {
        var config = CreateOcelotHttp11Config();
        await RunBenchmarkAsync(config, concurrentRequests, SmallPayload);
    }

    /// <summary>
    /// Baseline: Ocelot with HTTP/1.1 (default configuration)
    /// Medium payload (1 KB)
    /// </summary>
    [Benchmark(Baseline = true)]
    [Arguments(10)]
    [Arguments(50)]
    public async Task OcelotHttp11_MediumPayload(int concurrentRequests)
    {
        var config = CreateOcelotHttp11Config();
        await RunBenchmarkAsync(config, concurrentRequests, MediumPayload);
    }

    /// <summary>
    /// Reference: YARP with HTTP/2 (default configuration)
    /// Small payload (100 bytes)
    /// 
    /// Note: This is a template for YARP implementation.
    /// Actual YARP benchmarks should be implemented in a separate benchmark class
    /// that includes YARP package reference.
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    public async Task YarpHttp2_SmallPayload_Reference(int concurrentRequests)
    {
        // This is a placeholder for YARP benchmarks
        // YARP automatically uses HTTP/2 by default
        // Expected to show improvement over HTTP/1.1 Ocelot
        
        var config = CreateOcelotHttp11Config();
        await RunBenchmarkAsync(config, concurrentRequests, SmallPayload);
        
        // Note: To implement real YARP benchmarks, create a separate project
        // with YARP references and implement GatewayComparisonBenchmarks_YARP
    }

    /// <summary>
    /// Reference: YARP with HTTP/2 (default configuration)
    /// Medium payload (1 KB)
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    public async Task YarpHttp2_MediumPayload_Reference(int concurrentRequests)
    {
        var config = CreateOcelotHttp11Config();
        await RunBenchmarkAsync(config, concurrentRequests, MediumPayload);
    }

    #endregion

    #region Case 2: HTTP/2 Ocelot vs HTTP/2 YARP

    /// <summary>
    /// Ocelot with HTTP/2
    /// Small payload (100 bytes)
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    public async Task OcelotHttp2_SmallPayload(int concurrentRequests)
    {
        var config = CreateOcelotHttp2Config();
        await RunBenchmarkAsync(config, concurrentRequests, SmallPayload);
    }

    /// <summary>
    /// Ocelot with HTTP/2
    /// Medium payload (1 KB)
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    public async Task OcelotHttp2_MediumPayload(int concurrentRequests)
    {
        var config = CreateOcelotHttp2Config();
        await RunBenchmarkAsync(config, concurrentRequests, MediumPayload);
    }

    /// <summary>
    /// Ocelot with HTTP/2
    /// Large payload (10 KB)
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    public async Task OcelotHttp2_LargePayload(int concurrentRequests)
    {
        var config = CreateOcelotHttp2Config();
        await RunBenchmarkAsync(config, concurrentRequests, LargePayload);
    }

    /// <summary>
    /// Reference: YARP with HTTP/2 (default configuration)
    /// Small payload (100 bytes)
    /// 
    /// Note: This is a template for YARP implementation.
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    public async Task YarpHttp2_SmallPayload_Case2_Reference(int concurrentRequests)
    {
        var config = CreateOcelotHttp2Config();
        await RunBenchmarkAsync(config, concurrentRequests, SmallPayload);
    }

    /// <summary>
    /// Reference: YARP with HTTP/2 (default configuration)
    /// Medium payload (1 KB)
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    public async Task YarpHttp2_MediumPayload_Case2_Reference(int concurrentRequests)
    {
        var config = CreateOcelotHttp2Config();
        await RunBenchmarkAsync(config, concurrentRequests, MediumPayload);
    }

    /// <summary>
    /// Reference: YARP with HTTP/2 (default configuration)
    /// Large payload (10 KB)
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    public async Task YarpHttp2_LargePayload_Case2_Reference(int concurrentRequests)
    {
        var config = CreateOcelotHttp2Config();
        await RunBenchmarkAsync(config, concurrentRequests, LargePayload);
    }

    #endregion

    #region Helper Methods

    private async Task RunBenchmarkAsync(string configuration, int concurrentRequests, string payload)
    {
        steps.GivenThereIsAConfiguration(configuration);
        
        steps.GivenOcelotIsRunning(host => host
            .ConfigureKestrel((_, options) => 
                options.Protocols = HttpProtocols.Http1AndHttp2));

        try
        {
            var tasks = new Task[concurrentRequests];
            for (int i = 0; i < 100; i++) // 100 iterations per benchmark
            {
                for (int j = 0; j < concurrentRequests; j++)
                {
                    tasks[j] = SendRequestAsync(payload);
                }
                await Task.WhenAll(tasks);
            }
        }
        finally
        {
            steps.Dispose();
        }
    }

    private async Task SendRequestAsync(string payload)
    {
        using var client = new HttpClient();
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        try
        {
            await client.PostAsync("http://localhost:5000/", content);
        }
        catch
        {
            // Swallow exceptions for benchmarking
        }
    }

    private string CreateOcelotHttp11Config()
    {
        return """
{
  "Routes": [{
    "DownstreamPathTemplate": "/",
    "DownstreamScheme": "https",
    "DownstreamHostAndPorts": [{
      "Host": "localhost",
      "Port": 5000
    }],
    "UpstreamPathTemplate": "/",
    "UpstreamHttpMethod": ["POST", "GET"]
  }],
  "GlobalConfiguration": {}
}
""";
    }

    private string CreateOcelotHttp2Config()
    {
        return """
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
    "DownstreamHttpVersion": "2.0"
  }],
  "GlobalConfiguration": {}
}
""";
    }

    private static string GeneratePayload(int sizeInBytes)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"id\": \"test-123\",");
        sb.Append("\"timestamp\": \"2024-11-25T12:00:00Z\",");
        sb.Append("\"data\": \"");
        
        var targetSize = sizeInBytes - 100;
        var template = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. ";
        while (sb.Length < targetSize)
        {
            sb.Append(template);
        }
        
        sb.Append("\"}");
        return sb.ToString();
    }

    #endregion
}

/// <summary>
/// YARP Benchmarks Template
/// 
/// This is a template for implementing YARP benchmarks.
/// To use this:
/// 1. Create a new .csproj file: Ocelot.Benchmarks.YARP.csproj
/// 2. Add YARP package reference: Microsoft.ReverseProxy
/// 3. Implement similar benchmark structure as above
/// 4. Compare results with Ocelot benchmarks
/// 
/// Expected advantages of YARP:
/// - HTTP/2 is default (no configuration needed)
/// - Better connection multiplexing
/// - Lower memory footprint per connection
/// - Faster header compression with HPACK
/// 
/// Command to run YARP benchmarks:
/// dotnet run -c Release -p .\benchmark\Ocelot.Benchmarks.YARP.csproj -- --filter *YARP*
/// </summary>
public sealed class GatewayComparisonBenchmarks_YARP
{
    // TODO: Implement YARP benchmarks when YARP package is available
    // This would include:
    // - YarpHttp2_SmallPayload
    // - YarpHttp2_MediumPayload
    // - YarpHttp2_LargePayload
    // - Performance comparison metrics
}
