# Audit.NET.Polly

**Resilience wrapper Data Provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)**

Allows to define [Polly](https://www.pollydocs.org/index.html) resilience strategies to any [Data Provider](https://github.com/thepirat000/Audit.NET?tab=readme-ov-file#data-providers) within Audit.NET.

## Install

**NuGet Package** 

To install the package, run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.Polly
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.Polly.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.Polly/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.Polly.svg)](https://www.nuget.org/packages/Audit.NET.Polly/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration

To set the Polly data provider globally, call the `UsePolly()` method on the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):

```c#
Audit.Core.Configuration.Setup()
    .UsePolly(polly => polly
        .DataProvider(...)
        .WithResilience(r => r...)
```

For instance, to establish a retry policy for a [RavenDB data provider](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.RavenDB#readme), 
ensuring that insert/replace operations are retried no more than twice in the event of a RavenException:

```c#
var ravenDbProvider = new RavenDbDataProvider(r => r
    .WithSettings(s => s
        .Urls("http://127.0.0.1:8080")
        .Database(_ => "Audit")));

Audit.Core.Configuration.Setup()
    .UsePolly(p => p
        .DataProvider(ravenDbProvider)
        .WithResilience(resilience => resilience
            .AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<RavenException>(),
                Delay = TimeSpan.FromSeconds(1),
                MaxRetryAttempts = 2
            })));
```

## Extension for Fallback

To facilitate the [fallback](https://www.pollydocs.org/strategies/fallback.html) to a different Data Provider you can use the `FallbackToDataProvider()` extension method in the FallbackAction.

For instance, to establish a fallback policy for a [RavenDB data provider](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.RavenDB#readme),
ensuring that in the event of a RavenException, the Audit Events will be written to a file using a [File data provider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/Providers/FileDataProvider.cs) as a fallback:

```c#
var ravenDbProvider = new RavenDbDataProvider(...);

var fallbakDbProvider = new FileDataProvider(...);

Audit.Core.Configuration.Setup()
    .UsePolly(p => p
        .DataProvider(ravenDbProvider)
        .WithResilience(resilience => resilience
            .AddFallback(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<RavenException>(),
                FallbackAction = args => args.FallbackToDataProvider(fallbakDbProvider)
            })));
```   

## Extension for hedging

To facilitate the [hedging strategy](https://www.pollydocs.org/strategies/hedging.html) you can use the `FallbackToDataProvider()` extension method in the ActionGenerator.

For instance, to establish a hedging policy for a data provider so in case of exceptions, the Audit Events will be written to a different data provider using a hedging strategy:

```c#
var primaryDataProvider = new SqlDataProvider(sqlConfig => sqlConfig...);
var secondaryDataProvider = new FileDataProvider(fileConfig => fileConfig...);

Audit.Core.Configuration.Setup()
    .UsePolly(p => p
        .DataProvider(primaryDataProvider)
        .WithResilience(r => r
            .AddHedging(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<SqlException>(),
                MaxHedgedAttempts = 2,
                Delay = TimeSpan.Zero,
                ActionGenerator = args => args.FallbackToDataProvider(secondaryDataProvider)
            }));
```

## Settings

- `DataProvider`: The primary data provider to use.
- `WithResilience`: The resilience strategy to apply to the primary data provider. It can be a single policy or a collection of policies.

## Resilience Strategies

The following are some of the available resilience policies to apply to the primary data provider. 

- `AddRetry`: Adds a [retry](https://www.pollydocs.org/strategies/retry.html) policy to the primary data provider. 
- `AddFallback`: Adds a [fallback](https://www.pollydocs.org/strategies/fallback.html) policy to the primary data provider. 
- `AddCircuitBreaker`: Adds a [circuit breaker](https://www.pollydocs.org/strategies/circuit-breaker.html) policy to the primary data provider. 
- `AddTimeout`: Adds a [timeout](https://www.pollydocs.org/strategies/timeout.html) policy to the primary data provider. 
- `AddHedging`: Adds a [hedging](https://www.pollydocs.org/strategies/hedging.html) policy to the primary data provider. 

Please refer to [Polly documentation](https://www.pollydocs.org/strategies/index.html) for a complete list.
