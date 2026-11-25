# Ocelot Benchmarks

This folder contains performance benchmarks for the Ocelot API Gateway using [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet).

## Overview

Benchmarks measure Ocelot's performance across different configurations, middleware, and use cases. These benchmarks help identify performance bottlenecks and validate optimization efforts.

## Available Benchmarks

### Existing Benchmarks

| Benchmark | Purpose | Metrics |
|-----------|---------|---------|
| `PayloadBenchmarks` | Payload processing performance | Latency, Memory, Throughput |
| `ResponseBenchmarks` | Response handling | Latency, Memory |
| `UrlPathToUrlPathTemplateMatcherBenchmarks` | Route matching | Latency, Allocations |
| `DownstreamRouteFinderMiddlewareBenchmarks` | Route finding middleware | Latency |
| `ExceptionHandlerMiddlewareBenchmarks` | Exception handling | Latency |
| `AllTheThingsBenchmarks` | Full gateway pipeline | End-to-end latency |
| `SerilogBenchmarks` | Serilog logging performance | Latency, Memory |
| `MsLoggerBenchmarks` | Microsoft.Extensions.Logging performance | Latency, Memory |

### HTTP/2 Benchmarks (New)

#### Test 1: HTTP Version Comparison

**File**: `HttpVersionComparisonBenchmarks.cs`

**Purpose**: Measure the performance difference between HTTP/1.1 and HTTP/2 in Ocelot.

**Test Cases**:
- `OcelotHttp11` - Baseline with HTTP/1.1
- `OcelotHttp2` - HTTP/2 with explicit version configuration
- `OcelotHttp2WithNegotiation` - HTTP/2 with `RequestVersionOrLower` policy (allows downgrade)
- `OcelotHttp2WithDowngrade` - HTTP/2 with `AcceptDowngrade` policy

**Concurrency Levels**:
- 1 concurrent request (baseline latency)
- 10 concurrent requests (moderate load)
- 50 concurrent requests (high load)

**Metrics Measured**:
- Mean latency
- Median latency
- P95, P99 latency
- Memory allocations
- Garbage collection stats

**Expected Results**:
- HTTP/2 should show better performance under high concurrency
- Connection reuse benefits visible at 50 concurrent requests
- Memory efficiency improvements with HTTP/2 multiplexing

#### Test 2: Gateway Comparison (Ocelot vs YARP)

**File**: `GatewayComparisonBenchmarks.cs`

**Purpose**: Compare Ocelot and YARP gateway performance across different HTTP versions and payload sizes.

**Test Cases**:

**Case 1: HTTP/1.1 Ocelot vs HTTP/2 YARP**
- `OcelotHttp11_SmallPayload` (100 bytes)
- `OcelotHttp11_MediumPayload` (1 KB)
- `YarpHttp2_SmallPayload_Reference`
- `YarpHttp2_MediumPayload_Reference`

**Case 2: HTTP/2 Ocelot vs HTTP/2 YARP**
- `OcelotHttp2_SmallPayload` (100 bytes)
- `OcelotHttp2_MediumPayload` (1 KB)
- `OcelotHttp2_LargePayload` (10 KB)
- `YarpHttp2_SmallPayload_Case2_Reference`
- `YarpHttp2_MediumPayload_Case2_Reference`
- `YarpHttp2_LargePayload_Case2_Reference`

**Concurrency Levels**:
- 10 concurrent requests (baseline)
- 50 concurrent requests (stress)

**Payload Sizes**:
- Small: 100 bytes (REST API style)
- Medium: 1 KB (typical API responses)
- Large: 10 KB (file transfer)

**Expected Results**:
- HTTP/2 Ocelot should outperform HTTP/1.1 Ocelot
- YARP HTTP/2 should perform similarly or better than Ocelot HTTP/2
- Improvements more pronounced at high concurrency
- Payload size affects throughput but not connection overhead

## Running Benchmarks

### Prerequisites

```bash
# .NET 8.0, 9.0, or 10.0 SDK
dotnet --version

# Optional: BenchmarkDotNet tools
dotnet tool install -g BenchmarkDotNet.Tool
```

### Run All Benchmarks

```bash
# Release configuration (required for accurate results)
dotnet run -c Release -p .\benchmark\Ocelot.Benchmarks.csproj
```

### Run Specific Benchmark

```bash
# Run only HTTP version comparison
dotnet run -c Release -p .\benchmark\Ocelot.Benchmarks.csproj -- --filter *HttpVersionComparison*

# Run only gateway comparison
dotnet run -c Release -p .\benchmark\Ocelot.Benchmarks.csproj -- --filter *GatewayComparison*

# Run specific test
dotnet run -c Release -p .\benchmark\Ocelot.Benchmarks.csproj -- --filter *OcelotHttp2*
```

### Run with Custom Options

```bash
# Run with more iterations (better accuracy, longer time)
dotnet run -c Release -p .\benchmark\Ocelot.Benchmarks.csproj -- --warmupCount=5 --targetCount=10

# Run specific job
dotnet run -c Release -p .\benchmark\Ocelot.Benchmarks.csproj -- --filter *HttpVersionComparison* --job=short

# Export results to JSON
dotnet run -c Release -p .\benchmark\Ocelot.Benchmarks.csproj -- --exportJson=results.json
```

## Understanding Results

### Key Metrics

| Metric | Meaning | Lower is Better |
|--------|---------|-----------------|
| Mean | Average execution time | ✅ Yes |
| Median | Middle value execution time | ✅ Yes |
| P95 | 95th percentile latency | ✅ Yes |
| P99 | 99th percentile latency | ✅ Yes |
| StdDev | Standard deviation (consistency) | ✅ Yes |
| Allocated | Memory allocated per operation | ✅ Yes |
| Gen0 | GC Gen0 collections | ✅ Yes |

### Interpreting HTTP/2 Benchmarks

**Case 1: HTTP Version Comparison**
- Compare `OcelotHttp11` (baseline) with `OcelotHttp2`
- Look for improvements in latency and allocations
- Improvements should increase with concurrency
- Monitor for memory efficiency gains

**Case 2: Gateway Comparison**
- Compare Ocelot HTTP/1.1 baseline with YARP HTTP/2 reference
- Compare Ocelot HTTP/2 with YARP HTTP/2
- Payload size should not dramatically affect latency
- Concurrency differences show connection multiplexing benefits

### Example Results Interpretation

```
// Good HTTP/2 improvement
OcelotHttp11:            Mean=50.0ms, Allocated=5.2KB
OcelotHttp2:             Mean=35.0ms, Allocated=4.1KB
Improvement:             30% latency, 21% memory

// Connection multiplexing benefit
Concurrency=10:          ~1 connection per concurrent user
Concurrency=50:          ~1 connection total (HTTP/2 multiplexing)
Result:                  Lower memory, better throughput
```

## Performance Considerations

### What These Benchmarks Test

✅ **Gateway forwarding performance**
- Request processing latency
- Response handling
- Middleware execution time
- Memory efficiency

✅ **HTTP version impact**
- Protocol overhead
- Connection reuse
- Header compression
- Multiplexing benefits

✅ **Payload size impact**
- Large payload handling
- Memory allocations
- Throughput at scale

### What These Benchmarks DON'T Test

❌ **Network latency** - All communication is local
❌ **TLS/SSL overhead** - Configuration doesn't use real TLS
❌ **Real-world workloads** - Simplified request patterns
❌ **Service mesh complexity** - Single downstream service
❌ **Authentication/Authorization** - Not included
❌ **Rate limiting/Throttling** - Not included

## Development Guide

### Adding New HTTP/2 Benchmarks

1. **Create new benchmark class**:

```csharp
[Config(typeof(MyBenchmark))]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public sealed class MyBenchmark : ManualConfig
{
    [GlobalSetup]
    public void Setup() { }

    [Benchmark(Baseline = true)]
    public async Task Baseline() { }

    [Benchmark]
    public async Task TestCase() { }
}
```

2. **Add to Program.cs**:

```csharp
var switcher = new BenchmarkSwitcher(new[] {
    typeof(MyBenchmark),
    // ... other benchmarks
});
```

3. **Run and analyze**:

```bash
dotnet run -c Release -p .\benchmark\Ocelot.Benchmarks.csproj -- --filter *MyBenchmark*
```

### Best Practices

1. **Always run in Release mode** - Debug mode has different performance characteristics
2. **Use warm-up iterations** - BenchmarkDotNet includes this by default
3. **Multiple runs** - Results vary, run multiple times
4. **Control variables** - Change one thing at a time
5. **Monitor GC** - Excessive allocations indicate problems
6. **Document assumptions** - Explain what each benchmark measures

## CI/CD Integration

These benchmarks should be run:

- **Before major releases** - Ensure no performance regressions
- **During optimization work** - Measure improvements
- **In cloud environment** - Consistent hardware
- **With multiple .NET versions** - Test across targets (8.0, 9.0, 10.0)

### Example GitHub Actions Workflow

```yaml
name: Benchmarks

on:
  pull_request:
    paths:
      - 'src/**'
      - 'benchmark/**'

jobs:
  benchmark:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - run: dotnet run -c Release -p ./benchmark/Ocelot.Benchmarks.csproj
      - uses: actions/upload-artifact@v3
        with:
          name: benchmark-results
          path: BenchmarkDotNet.Artifacts/
```

## References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [HTTP/2 RFC 7540](https://tools.ietf.org/html/rfc7540)
- [HTTP/2 Header Compression RFC 7541](https://tools.ietf.org/html/rfc7541)
- [Ocelot Documentation](https://ocelot.readthedocs.io/)
- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)

## Troubleshooting

### Benchmark Won't Start

```bash
# Check .NET version
dotnet --version

# Verify project builds
dotnet build -c Release -p .\benchmark\Ocelot.Benchmarks.csproj

# Check for port conflicts (default 5000, 5001)
netstat -ano | findstr :5000
```

### Results Show High Variance

- Increase warmup count
- Close background applications
- Run multiple times
- Use statistical analysis tools

### Out of Memory Errors

- Reduce concurrent requests in benchmark
- Increase `--warmupCount` to initialize memory
- Run on 64-bit system with sufficient RAM

## Contributing

When adding new benchmarks:

1. Create descriptive class and method names
2. Document the purpose and expected results
3. Include comments explaining test scenarios
4. Add to this README with explanation
5. Test locally before submitting PR

## License

Same as Ocelot - See [LICENSE.md](../LICENSE.md)

---

**Last Updated**: 2024-11-25
**Maintainers**: Ocelot Contributors
