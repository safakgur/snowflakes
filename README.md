# Snowflakes ![Logo][logo]

[![CI][wf-ci-badge]][wf-ci]
[![NuGet][nuget-badge]][nuget]

Snowflake IDs are 64-bit, sortable values that can be generated in a distributed system, ensuring uniqueness
without a central authority. The format was [originally created by Twitter (now X)][twitter-announcement] and adopted
by others like [Sony][sonyflake], [Discord][discord-snowflakes], [Instagram][instagram-sharding-and-ids], and more.

This .NET library lets you create customized snowflakes by configuring the components:
timestamp, sequence, and instance ID.

## Layout

Snowflakes are 64-bit IDs, but only 63 bits are used for one to fit in a signed integer.

There are three standard components that make up a snowflake:

1. **Timestamp:** Time passed since a set epoch. Both the epoch and the unit (precision) are adjustable.
2. **Instance ID:** Identifies the process creating the snowflake. Also known as Machine ID or Shard ID.
3. **Sequence number:** Increments when multiple snowflakes are created in the same time unit.

When you define the snowflake format for a system, consider the following:

* **System lifetime:** Bigger timestamp length, later epoch, and lower precision mean higher lifetime.
* **Instance count:** Bigger instance ID length means there can be more instances generating snowflakes.
* **Generation rate:** Bigger sequence number length means the instance can generate more snowflakes per timestamp.
* **Ordering:** Components specified earlier occupy higher bits, meaning they are prioritized during sorting.

## How to Use

### Configuring Snowflakes

The following examples show how you can define your own snowflake format.

#### X's Implementation

41-bit timestamp in units of 1 ms elapsed since 2010-11-04T01:42:54.657Z  
10-bit instance ID  
12-bit sequence number

```csharp
var epoch = DateTimeOffset.FromUnixTimeMilliseconds(1288834974657); // X's epoch
var instanceId = 0; // e.g., ordinal index of a K8 pod
var snowflakeGen = new SnowflakeGeneratorBuilder()
    .AddTimestamp(41, epoch, TimeSpan.TicksPerMillisecond)
    .AddConstant(10, instanceId)
    .AddSequenceForTimestamp(12)
    .Build();
```

#### Sony's Implementation (Sonyflake)

39-bit timestamp in units of 10 ms from a specified epoch  
8-bit sequence number  
16-bit instance ID

```csharp
var epoch = new DateTimeOffset(2024, 8, 30, 0, 0, 0, TimeSpan.Zero); // e.g., when your system came online
var instanceId = 0; // e.g., ordinal index of a K8 pod
var snowflakeGen = new SnowflakeGeneratorBuilder()
    .AddTimestamp(39, epoch, TimeSpan.TicksPerMillisecond * 10) // 10 ms increments
    .AddSequenceForTimestamp(8)
    .AddConstant(16, instanceId)
    .Build();
```

#### Comparison of the Implementations

From Sonyflake's README:

* \[Sonyflake's\] lifetime (174 years) is longer than that of \[X's\] Snowflake (69 years)  
* \[Sonyflake\] can work in more distributed machines (2^16) than \[X's\] Snowflake (2^10)  
* \[Sonyflake\] can generate 2^8 IDs per 10 msec at most in a single machine/thread (slower than \[X's\] Snowflake)

Note that X Snowflake and Sonyflake components are also placed in different orders, which means:

* X Snowflake will be sorted by timestamp -> instance ID -> sequence number
* Sonyflake will be sorted by timestamp -> sequence number -> instance ID


### Generating Snowflakes

Once you have a generator, you can generate snowflakes using its `NewSnowflake` method.

```csharp
var snowflake1 = snowflakeGen.NewSnowflake();
var snowflake2 = snowflakeGen.NewSnowflake();
// ...
```

Try to keep snowflakes as `Int64` values and use the appropriate integer type when persisting them to a database.
This will ensure they are stored efficiently and remain sortable.

### Encoding Snowflakes

You can use `SnowflakeEncoder` to encode snowflakes to custom-base strings.
This will make them shorter, which can be useful when they are used in URIs.

```csharp
// There are base 36, 62, and 64 encoders, all URI-safe.
var encoder = SnowflakeEncoder.Base62;

var snowflake = snowflakeGen.NewSnowflake(); // 139611368062976
var encodedSnowflake = encoder.Encode(snowflake); // "ddw3cbIG"
var decodedSnowflake = encoder.Decode(encodedSnowflake); // 139611368062976
```

Make sure you decode the values back to `Int64` before sorting or persisting them.

### Dependency Injection

The `SnowflakeGenerator` instance must be shared for the generated snowflakes to be unique.
When using a DI container, a generator needs to be registered as a singleton.

```csharp
services.AddSingleton(static serviceProvider =>
{
    // A fixed epoch, specific to the system that will use the generated snowflakes.
    // Do not change this value when you have snowflakes in the wild.
    // The epoch must be earlier than the current time at the time of snowflake generation.
    var epoch = new DateTimeOffset(2024, 8, 30, 0, 0, 0, TimeSpan.Zero);

    // Assuming a time provider is registered.
    // Feel free to omit it when setting up the timestamp component (Default: TimeProvider.System).
    var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();

    // Assuming there is an options class that can provide information about the current instance.
    // Feel free to obtain the instance ID by different means.
    var programOpts = serviceProvider.GetRequiredService<IOptions<ProgramOptions>>().Value;
    var instanceId = programOpts.InstanceId;

    // The generator below uses the Sonyflake configuration.
    return new SnowflakeGeneratorBuilder()
        .AddTimestamp(39, epoch, TimeSpan.TicksPerMillisecond * 10, timeProvider)
        .AddSequenceForTimestamp(8)
        .AddConstant(16, instanceIndex)
        .Build();
});
```

Consider [keyed registrations][dotnet-di-keyed] if your application needs to generate multiple types of snowflakes.

Once registered, you can inject and use the generator like any other dependency.

```csharp
public class FooService(SnowflakeGenerator snowflakeGen)
{
    public Foo CreateFoo() => new()
    {
        Id = snowflakeGen.NewSnowflake()
        // ...
    };
}
```

### String Instance IDs

There may be cases where you don't have access to an integer instance ID. Using Azure App Services, for example,
we have no good way of obtaining an integer "index" of the instance, but we do have access to a string
(exposed via the `WEBSITE_INSTANCE_ID` environment variable) that uniquely identifies it.

For scenarios like this, `AddConstant` has a hashing overload that can create a reasonably high-cardinality value.
Below is an alternative service registration example showing the use of this overload.

```csharp
services.AddSingleton(static serviceProvider =>
{
    var epoch = new DateTimeOffset(2024, 8, 30, 0, 0, 0, TimeSpan.Zero);

    // This time, we have a string instance ID, which means we need to hash it to get a value that
    // fits in the number of bits we have available.
    var programOpts = serviceProvider.GetRequiredService<IOptions<ProgramOptions>>().Value;
    var instanceId = programOpts.InstanceId;

    // Pick a good algorithm for the hash. Speed is not a concern as this will only run once on
    // application startup.
    using var instanceIdHashAlg = SHA512.Create();

    // The generator below uses the Sonyflake configuration.
    return new SnowflakeGeneratorBuilder()
        .AddTimestamp(39, epoch, TimeSpan.TicksPerMillisecond * 10, timeProvider)
        .AddSequenceForTimestamp(8)
        .AddConstant(16, instanceId, instanceIdHashAlg) // ...passing the hash algorithm.
        .Build();
});
```

**Important:** Uniqueness can't be guaranteed with this approach due to the possibility of hash collisions.
The likelihood of collisions will depend on the chosen component length and hash algorithm.

If you can't supply a unique, integer instance ID that fits the component, you need to design your
system to be able to tolerate the occasional duplicate snowflake.

[logo]: https://raw.githubusercontent.com/safakgur/snowflakes/main/media/logo-28.png "Logo"
[wf-ci]: https://github.com/safakgur/snowflakes/actions/workflows/ci.yml "CI Workflow"
[wf-ci-badge]: https://github.com/safakgur/snowflakes/actions/workflows/ci.yml/badge.svg?event=push "CI Badge"
[nuget]: https://www.nuget.org/packages/Snowflakes/ "NuGet Gallery"
[nuget-badge]: https://img.shields.io/nuget/v/Snowflakes.svg?style=flat "NuGet Badge"

[twitter-announcement]: https://blog.twitter.com/2010/announcing-snowflake "Announcing Snowflake"
[sonyflake]: https://github.com/sony/sonyflake "Sonyflake"
[discord-snowflakes]: https://discord.com/developers/docs/reference#snowflakes "Discord Developer Portal"
[instagram-sharding-and-ids]: https://instagram-engineering.com/sharding-ids-at-instagram-1cf5a71e5a5c "Sharding & IDs at Instagram"

[dotnet-di-keyed]: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#keyed-services ".NET Dependency Injection"
