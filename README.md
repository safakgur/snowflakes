# Snowflakes ![Logo][logo]

[![CI][wf-ci-badge]][wf-ci]
[![NuGet][nuget-badge]][nuget]

Snowflake IDs, also known as snowflakes, are 64-bit, sortable identifiers generated in a distributed
system that provide uniqueness across time and space without requiring a central authority.
The format was [originally created by Twitter (now X)][twitter-announcement] and adopted by others
like [Sony][sonyflake], [Discord][discord-snowflakes], [Instagram][instagram-sharding-and-ids],
and more.

This library allows you to define your own snowflake variation by creating a generator where the
order and lengths of the components (such as timestamp, sequence, and instance ID) are fully
customizable. Once configured, the generator can be called to produce new snowflakes.

## Format

Snowflakes are 64-bit identifiers, but only 63 bits are used for one to fit in a signed integer.

There are three standard components that make up a snowflake:

1. **Timestamp** - A unit of time elapsed since an epoch to provide uniqueness in time.  
   Both the unit (precision) and the epoch are configurable.

2. **Instance ID** - Represents the current process to provide uniqueness in space.  
   This is also referred to as Machine ID or Shard ID.

3. **Sequence number** - Incremented when multiple identifiers are generated at the
   same unit of time.

## Example Configurations

### X's Implementation

* 41-bit timestamp in units of 1 ms from the epoch, 1288834974657 (in Unix time milliseconds)
* 10-bit instance ID
* 12-bit sequence number

Here's how to configure this using Snowflakes:

```csharp
var epoch = DateTimeOffset.FromUnixTimeMilliseconds(1288834974657); // X's epoch
var instanceId = 0; // e.g., ordinal index of a K8 pod
var snowflakeGen = new SnowflakeGeneratorBuilder()
    .AddTimestamp(41, epoch, TimeSpan.TicksPerMillisecond)
    .AddConstant(10, instanceId)
    .AddSequenceForTimestamp(12)
    .Build();

long snowflake = snowflakeGen.NewSnowflake();
```

### Sonyflake (Sony's Implementation)

* 39-bit timestamp in units of 10 ms from a specified epoch
* 8-bit sequence number
* 16-bit instance ID

Here's how to configure this using Snowflakes:

```csharp
var epoch = new DateTimeOffset(2024, 8, 30, 0, 0, 0, TimeSpan.Zero); // e.g., when your system came online
var instanceId = 0; // e.g., ordinal index of a K8 pod
var snowflakeGen = new SnowflakeGeneratorBuilder()
    .AddTimestamp(39, epoch, TimeSpan.TicksPerMillisecond * 10) // 10 ms increments
    .AddSequenceForTimestamp(8)
    .AddConstant(16, instanceId)
    .Build();

long snowflake = snowflakeGen.NewSnowflake();
```

---

Note that not only X and Sonyflake components have their lengths different, they're also placed in
different orders with Sonyflake having the sequence number before the instance ID, which means they
are sorted differently.

Quoting the comparison from Sonyflake's README:

> The lifetime (174 years) is longer than that of Snowflake (69 years)
> It can work in more distributed machines (2^16) than Snowflake (2^10)
> It can generate 2^8 IDs per 10 msec at most in a single machine/thread (slower than Snowflake)

## Recommendations

* Before adding another dependency to your system, think about whether you will actually benefit
  from using snowflakes as opposed to UUIDs or DB-generated identifiers. Do you need your IDs to
  be generated by distributed instances, to be sortable, to be short? Make sure it adds more value
  than the complication it introduces.

* When defining your own snowflake format, think about the lifetime of your system, the maximum
  number of instances that will generate snowflakes, and how many snowflakes they will need to
  generate in a unit of time. Avoid copying another company's configuration without fully
  understanding its implications.

* Register `SnowflakeGenerator` instances as singletons. The generator is thread-safe and using a
  single instance per instance ID is the only way to ensure getting unique snowflakes.

* Keep snowflakes as `Int64` values and use the appropriate integer type when persisting them to a
  database. This will ensure they are stored efficiently and remain sortable.

* You can use `SnowflakeEncoder` to encode snowflakes to custom-base strings. This will make them
  shorter, which may be useful when they are used in URIs. Make sure you decode them back to `Int64`
  before sorting or persisting them.

* If you don't have access to an integer instance ID, you can use the hashing `AddConstant` overload
  to get a reasonably high-cardinality value.

  * Using Azure App Services is one example scenario where you don't have access to an integer
    "index" of the instance, but you do have access to a string (via the `WEBSITE_INSTANCE_ID`
    environment variable) that uniquely identifies it.

* When setting up the timestamp component, consider supplying it with a `TimeProvider` as it can
  help with testing. The time provider is optional and will default to `TimeProvider.System`.

## Code Samples

### Generator registration using an integer instance ID

```csharp
services.AddSingleton(static serviceProvider =>
{
    // Assuming there is an options class that can provide information about the current instance.
    // Feel free to obtain the instance ID in a different way.
    var programOpts = serviceProvider.GetRequiredService<IOptions<ProgramOptions>>().Value;
    var instanceIndex = programOpts.InstanceIndex;

    // Assuming a time provider is registered.
    // Feel free to omit it when setting up the timestamp component to default to TimeProvider.System.
    var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();

    // A fixed epoch, specific to the system that will use the generated snowflakes.
    // Do not change this value when you have snowflakes in the wild.
    // The epoch must be smaller than the current time at the time of snowflake generation.
    var epoch = new DateTimeOffset(2024, 8, 30, 0, 0, 0, TimeSpan.Zero);

    // The generator below uses the Sonyflake configuration.
    return new SnowflakeGeneratorBuilder()
        .AddTimestamp(39, epoch, TimeSpan.TicksPerMillisecond * 10, timeProvider)
        .AddSequenceForTimestamp(8)
        .AddConstant(16, instanceIndex)
        .Build();
});
```

### Generator registration using a string instance ID

```csharp
services.AddSingleton(static serviceProvider =>
{
    // Assuming there is an options class that can provide information about the current instance.
    // Feel free to obtain the instance ID in a different way.
    var programOpts = serviceProvider.GetRequiredService<IOptions<ProgramOptions>>().Value;
    var instanceId = programOpts.InstanceId;

    // Assuming a time provider is registered.
    // Feel free to omit it when setting up the timestamp component to default to TimeProvider.System.
    var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();

    // A fixed epoch, specific to the system that will use the generated snowflakes.
    // Do not change this value when you have snowflakes in the wild.
    // The epoch must be smaller than the current time at the time of snowflake generation.
    var epoch = new DateTimeOffset(2024, 8, 30, 0, 0, 0, TimeSpan.Zero);

    // Pick a good algorithm for the hash. Speed is not a concern as this will only run once on
    // application startup. You don't need to find an algorithm that produces few enough bits to
    // fit the component either, as the component will truncate the hash to fit.
    using var instanceIdHashAlg = SHA512.Create();

    // The generator below uses the Sonyflake configuration.
    return new SnowflakeGeneratorBuilder()
        .AddTimestamp(39, epoch, TimeSpan.TicksPerMillisecond * 10, timeProvider)
        .AddSequenceForTimestamp(8)
        .AddConstant(16, instanceId, instanceIdHashAlg)
        .Build();
});
```

### Generator usage

```csharp
// Assuming the generator is registered and can be injected.
// Feel free to obtain it in a different way, but make sure all consumers use the same instance.
public class FooService(SnowflakeGenerator snowflakeGen)
{
    public Foo CreateFoo()
    {
        // NewSnowflake returns an Int64.
        var foo = new Foo { Id = snowflakeGen.NewSnowflake() };

        // ...

        return foo;
    }
}
```

### Encoder usage

```csharp
var encoder = SnowflakeEncoder.Base62; // There are base 36 and 64 encoders as well, all URI-safe.

var snowflake = snowflakeGen.NewSnowflake(); // 139611368062976
var encodedSnowflake = encoder.Encode(snowflake); // "ddw3cbIG"
var decodedSnowflake = encoder.Decode(encodedSnowflake); // 139611368062976
```

[logo]: https://raw.githubusercontent.com/safakgur/snowflakes/main/media/logo-28.png "Logo"
[wf-ci]: https://github.com/safakgur/snowflakes/actions/workflows/ci.yml
[wf-ci-badge]: https://github.com/safakgur/snowflakes/actions/workflows/ci.yml/badge.svg?event=push
[nuget]: https://www.nuget.org/packages/Snowflakes/
[nuget-badge]: https://img.shields.io/nuget/v/Snowflakes.svg?style=flat

[twitter-announcement]: https://blog.twitter.com/2010/announcing-snowflake "Announcing Snowflake @ Twitter Engineering"
[sonyflake]: https://github.com/sony/sonyflake "Sonyflake"
[discord-snowflakes]: https://discord.com/developers/docs/reference#snowflakes
[instagram-sharding-and-ids]: https://instagram-engineering.com/sharding-ids-at-instagram-1cf5a71e5a5c "Sharding & IDs at Instagram"
