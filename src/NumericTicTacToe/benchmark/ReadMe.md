# NumTic Benchmark Suite

This project contains comprehensive performance benchmarks for the Numeric Tic-Tac-Toe game implementation using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Overview

The benchmark suite is designed to:
- ✅ **Validate performance assumptions** from our analysis and optimizations
- ✅ **Guide future optimization decisions** with concrete data
- ✅ **Track performance regressions** over time
- ✅ **Compare alternative implementations** objectively

## Benchmark Categories

### 1. GameStateBenchmarks
Tests core game state operations that represent the primary performance bottlenecks:

**Core Operations:**
- `CreateCopy_FreshGame` - Measures allocation cost of copying fresh game states
- `CreateCopy_MidGame` - Measures allocation cost with some tokens used
- `CreateCopy_NearEnd` - Measures allocation cost with most tokens used
- `ApplyMove` - Measures move application performance
- `UndoMove` - Measures move rollback performance
- `ApplyUndoPattern` - Tests mutation-based pattern vs copying

**Winner Detection:**
- `ScanForWinner_NoWinner` - Winner scanning with no winner present
- `ScanForWinner_HasWinner` - Winner scanning with a winner

**Board Operations:**
- `IsEmptyPosition` - Position validation performance
- `GetBoardPosition` - Coordinate to index conversion
- `GetBoardCoordinates` - Index to coordinate conversion

### 2. TokenManagementBenchmarks
Tests token-related operations, including our Span optimization:

**Token Enumeration:**
- `HashSetEnumeration` - Direct HashSet<byte> enumeration (baseline)
- `SpanEnumeration` - Stack-allocated Span<byte> enumeration (optimized)

**Token Operations:**
- `ContainsToken` - HashSet membership testing
- `AddRemoveToken` - HashSet modification operations
- `CreateTokenCopy` - HashSet copying performance

### 3. BotPlayerBenchmarks
Tests AI player performance across different scenarios:

**Bot Difficulty:**
- `EasyBot_EarlyGame` - Easy difficulty at game start (max tokens)
- `EasyBot_MidGame` - Easy difficulty mid-game (fewer tokens)
- `HardBot_EarlyGame` - Hard difficulty at game start
- `HardBot_MidGame` - Hard difficulty mid-game

**Allocation Patterns:**
- `SimulateConcurrentCopying` - Tests the current concurrent evaluation pattern

## Running Benchmarks

### Full Benchmark Suite
```bash
cd benchmark
dotnet run -c Release
```

### Quick Verification (Short Run)
```bash
dotnet run -c Release -- --quick-test
```

### Individual Benchmark Categories
```bash
# Only GameState benchmarks
dotnet run -c Release --filter "*GameState*"

# Only optimization comparisons  
dotnet run -c Release --filter "*Token*"
```

## Expected Results

Based on our performance analysis, these benchmarks should validate:

### ✅ **CreateCopy Allocation Costs**
- **Fresh Game**: ~270 bytes per copy (4-5 tokens available)
- **Mid Game**: ~200 bytes per copy (2-3 tokens available) 
- **Near End**: ~100 bytes per copy (1-2 tokens available)

### ✅ **Span Enumeration Optimization**
- **SpanEnumeration**: 0 bytes allocated per operation
- **HashSetEnumeration**: ~24 bytes allocated per operation
- **Performance**: Should be 2-3x faster for span approach

### ✅ **BotPlayer Performance Scaling**
- **Early game**: Higher allocation due to more available moves
- **Mid/End game**: Lower allocation as move options decrease
- **Hard vs Easy**: Higher allocation but better move quality

## Interpreting Results

### Memory Allocation
- **Gen0 collections**: Should be minimal for optimized paths
- **Allocated bytes**: Lower is better, validates optimization effectiveness
- **Allocation ratio**: Compares optimized vs baseline approaches

### Performance Metrics
- **Mean time**: Average execution time per operation
- **Error/StdDev**: Measurement consistency (lower is better)
- **Ratio**: Performance comparison to baseline (lower is better for optimizations)

### Key Validation Points

**✅ Concurrency vs Allocation Trade-off**
BotPlayer benchmarks should confirm that ~1KB allocation per move calculation is justified by 3-4x concurrency speedup.

**✅ Micro-optimization Impact**
Token benchmarks should show measurable but appropriately-scaled improvements from our optimizations.

**✅ Scale Appropriateness**
Results should confirm that optimization efforts are proportional to real-world impact.

## Continuous Monitoring

Run these benchmarks:
- ✅ **Before major refactoring** to establish baseline
- ✅ **After optimization changes** to validate improvements
- ✅ **During regular development** to catch regressions
- ✅ **Before releases** to ensure performance standards

## Benchmark Infrastructure

- **Runtime**: .NET 9.0 with latest JIT optimizations
- **Configuration**: Release mode with full optimizations enabled
- **Memory Diagnostics**: Enabled to track allocations and GC pressure
- **Statistics**: 99.9% confidence intervals for reliable measurements
- **Warm-up**: Automatic JIT warm-up for consistent results

This benchmark suite provides the empirical foundation for all performance decisions in the NumTic project.
