# Snowflakes

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
   This is also referred to as Machine ID and Shard ID.

3. **Sequence number** - Incremented when multiple identifiers are generated at the
   same unit of time on the same instance.

## Example Configurations

X's implementation uses,

* 41-bit timestamp in units of 1 ms from the epoch, 1288834974657 (in Unix time milliseconds)
* 10-bit instance ID
* 12-bit sequence number

Here's how to configure this using Snowflakes:

```csharp
var epoch = DateTimeOffset.FromUnixTimeMilliseconds(1288834974657); // X's epoch
var instanceId = 0; // e.g., ordinal index of a K8 pod
var snowflakeGen = new SnowflakeGenerator([
    new TimestampSnowflakeComponent(41, epoch, TimeSpan.TicksPerMillisecond),
    new ConstantSnowflakeComponent(10, instanceId),
    new SequenceSnowflakeComponent(12, refComponentIndex: 0)]);

long snowflake = snowflakeGen.NewSnowflake();
```

Sony's Sonyflake produces,

* 39-bit timestamp in units of 10 ms from a specified epoch
* 8-bit sequence number
* 16-bit instance ID

Here's how to configure this using Snowflakes:

```csharp
var epoch = new DateTimeOffset(2024, 8, 30, 0, 0, 0, TimeSpan.Zero); // e.g., when your system came online
var instanceId = 0; // e.g., ordinal index of a K8 pod
var snowflakeGen = new SnowflakeGenerator([
    new TimestampSnowflakeComponent(39, epoch, TimeSpan.TicksPerMillisecond * 10), // 10 ms increments
    new SequenceSnowflakeComponent(8, refComponentIndex: 0),
    new ConstantSnowflakeComponent(16, instanceId)]);

long snowflake = snowflakeGen.NewSnowflake();
```

Note that not only X and Sonyflake components have their lengths different, they're also placed in
different orders with Sonyflake having the sequence number before the instance ID, which means they
are sorted differently.

Quoting the comparison from Sonyflake's README:

> The lifetime (174 years) is longer than that of Snowflake (69 years)
> It can work in more distributed machines (2^16) than Snowflake (2^10)
> It can generate 2^8 IDs per 10 msec at most in a single machine/thread (slower than Snowflake)

## Recommendations

* Think about the lifetime of your system, the maximum number of instances that will generate
  snowflakes, and how many snowflakes they will need to generate in your specified unit of time.
  Avoid copying what another company does without fully understanding its implications.

* Register `SnowflakeGenerator` instances as singletons. The generator is thread-safe and using a
  single instance per instance ID is the only way to ensure getting unique snowflakes.


* Keep snowflakes as `Int64` values and use the appropriate integer type when persisting them to a
  database. This will ensure they are stored efficiently and remain sortable.

* You can use `SnowflakeEncoder` to encode snowflakes to custom-base strings. This will make them
  shorter, which may be useful when they are used in URIs. Make sure you decode them back to `Int64`
  before sorting or persisting them.

* If you don't have access to an integer instance ID, you can use the hashing constructor of
  `ConstantSnowflakeComponent` to get a reasonably safe value. Using Azure App Services is one
  example scenario where you don't have access to an integer "index" of the instance, but you do
  have access to a string (`WEBSITE_INSTANCE_ID` environment variable) that uniquely identifies it.

* When constructing a `TimestampSnowflakeComponent`, supply it with a `TimeProvider` to help with
  testing. The time provider is optional and will default to `TimeProvider.System`.

### Code Samples

#### Generator registration using an integer instance ID

```csharp
services.AddSingleton(static serviceProvider =>
{
    // Assuming there is an options class that can provide information about the current instance.
    // Feel free to obtain the instance ID in another way.
    var programOpts = serviceProvider.GetRequiredService<ProgramOptions>();

    // Assuming a time provider is registered.
    // Feel free to omit it when constructing the timestamp component to default to TimeProvider.System.
    var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();

    // A fixed epoch, specific to the system that will use the generated snowflakes.
    // Do not change this value when you have snowflakes in the wild.
    var epoch = new DateTimeOffset(2024, 8, 30, 0, 0, 0, TimeSpan.Zero);

    // The generator below uses the Sonyflake configuration.
    return new SnowflakeGenerator([
        new TimestampSnowflakeComponent(39, epoch, TimeSpan.TicksPerMillisecond * 10, timeProvider),
        new SequenceSnowflakeComponent(8, refComponentIndex: 0),
        new ConstantSnowflakeComponent(16, programOpts.InstanceIndex)]);
});
```

#### Generator registration using a string instance ID

```csharp
services.AddSingleton(static serviceProvider =>
{
    // Assuming there is an options class that can provide information about the current instance.
    // Feel free to obtain the instance ID in another way.
    var programOpts = serviceProvider.GetRequiredService<ProgramOptions>();

    // Assuming a time provider is registered.
    // Feel free to omit it when constructing the timestamp component to default to TimeProvider.System.
    var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();

    // A fixed epoch, specific to the system that will use the generated snowflakes.
    // Do not change this value when you have snowflakes in the wild.
    var epoch = new DateTimeOffset(2024, 8, 30, 0, 0, 0, TimeSpan.Zero);

    // Pick a good algorithm for the hash. Speed is not a concern as this will only run once on
    // application startup. You don't need to find an algorithm that produces few enough bits to
    // fit the component either, as the component will trim the hash to fit.
    using var instanceIdHashAlg = SHA512.Create();

    // The generator below uses the Sonyflake configuration.
    return new SnowflakeGenerator([
        new TimestampSnowflakeComponent(39, epoch, TimeSpan.TicksPerMillisecond * 10, timeProvider),
        new SequenceSnowflakeComponent(8, refComponentIndex: 0),
        new ConstantSnowflakeComponent(16, programOpts.InstanceId, instanceIdHashAlg)]);
});
```

#### Generator usage

```csharp
public class FooService(SnowflakeGenerator snowflakeGen)
{
    public Foo CreateFoo()
    {
        var id = snowflakeGen.NewSnowflake();

        var foo = new Foo { Id = id };

        // ...

        return foo;
    }
}
```

#### Encoder usage

```csharp
var snowflake = snowflakeGen.NewSnowflake(); // 139611368062976
var encodedSnowflake = SnowflakeEncoder.Base62.Encode(snowflake); // "ddw3cbIG"
var decodedSnowflake = SnowflakeEncoder.Base62.Decode(encodedSnowflake); // 139611368062976
```

[twitter-announcement]: https://blog.twitter.com/2010/announcing-snowflake "Announcing Snowflake @ Twitter Engineering"
[sonyflake]: https://github.com/sony/sonyflake "Sonyflake"
[discord-snowflakes]: https://discord.com/developers/docs/reference#snowflakes
[instagram-sharding-and-ids]: https://instagram-engineering.com/sharding-ids-at-instagram-1cf5a71e5a5c "Sharding & IDs at Instagram"