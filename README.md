# Snowflakes ![Logo][logo]

[![CI][wf-ci-badge]][wf-ci]
[![CodeQL Advanced][wf-codeql-badge]][wf-codeql]
[![NuGet][nuget-badge]][nuget]

Snowflake IDs are typically 64-bit, unique, sortable identifiers that can be generated in a distributed system
without a central authority. The format was [originally created by Twitter (now X)][twitter-announcement]
and adopted by others like [Sony][sonyflake], [Discord][discord-snowflakes], [Instagram][instagram-sharding-and-ids], and more.

This .NET library lets you create customized snowflakes by configuring the components:
Timestamp, Sequence, and Instance ID, using any integer type.

## Layout

Traditional snowflakes are 64-bit IDs, where only 63 bits are used for one to fit in a signed
integer. The library defaults to `long`/`Int64` snowflakes for maximum compability, but allows
other integer types to be used when desired. There are examples of this later in this document.

There are three standard components that make up a snowflake:

1. **Timestamp:** Time passed since a set epoch. Both the epoch and the unit (precision) are adjustable.
2. **Instance ID:** Identifies the process creating the snowflake. Also known as Machine ID or Shard ID.
3. **Sequence number:** Increments when multiple snowflakes are created in the same time unit.

When you define the snowflake format for a system, consider the following:

* **System lifetime:** Bigger timestamp length, later epoch, and lower precision mean higher lifetime.
* **Instance count:** Bigger instance ID length means there can be more instances generating snowflakes.
* **Generation rate:** Bigger sequence length means an instance can generate more snowflakes per time.
* **Ordering:** Components specified earlier occupy higher bits, meaning they are prioritized during sorting.

## How to Use

### Configuring Snowflakes

The following examples show how you can define your own snowflake format.

#### X's Implementation

41-bit timestamp in milliseconds elapsed since X's epoch  
10-bit instance ID  
12-bit sequence number

```csharp
// X's epoch - 2010-11-04T01:42:54.657Z
var epoch = DateTimeOffset.FromUnixTimeMilliseconds(1288834974657);

// Set the instance ID, e.g., the ordinal index of a K8 pod.
var instanceId = 0;

// Create the generator.
var snowflakeGen = SnowflakeGenerator.CreateBuilder()
    .AddTimestamp(41, epoch, TimeSpan.TicksPerMillisecond)
    .AddConstant(10, instanceId)
    .AddSequenceForTimestamp(12)
    .Build();
```

#### Sony's Implementation (Sonyflake)

39-bit timestamp in units of 10 ms elapsed since a custom epoch  
8-bit sequence number  
16-bit instance ID

```csharp
// Choose an epoch, e.g., when your system came online. Epoch can't be in the future.
var epoch = new DateTimeOffset(2024, 8, 30, 0, 0, 0, TimeSpan.Zero);

// Set the instance ID, e.g., the ordinal index of a K8 pod.
var instanceId = 0;

// Create the generator.
var snowflakeGen = SnowflakeGenerator.CreateBuilder()
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

If you decide to use snowflakes, make sure you configure the generator based on _your system_'s requirements.
Do not blindly copy one of the above configurations.

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
var encoder = SnowflakeEncoder.Base62Ordinal;

var snowflake = snowflakeGen.NewSnowflake(); // 139611368062976
var encodedSnowflake = encoder.Encode(snowflake); // "ddw3cbIG"
var decodedSnowflake = encoder.Decode(encodedSnowflake); // 139611368062976
```

Make sure you decode the values back to `Int64` before sorting or persisting them.

### Dependency Injection

The `SnowflakeGenerator` instance must be shared for the generated snowflakes to be unique,
so when using a DI container, a generator needs to be registered as a singleton.

```csharp
services.AddSingleton(static serviceProvider =>
{
    // A fixed epoch, specific to the system that will use the generated snowflakes.
    // Do not change this value once you have snowflakes in the wild.
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
    return SnowflakeGenerator.CreateBuilder()
        .AddTimestamp(39, epoch, TimeSpan.TicksPerMillisecond * 10, timeProvider)
        .AddSequenceForTimestamp(8)
        .AddConstant(16, instanceId)
        .Build();
});
```

Consider [keyed registrations][dotnet-di-keyed] if your application needs to generate multiple types of snowflakes.

Once registered, you can inject and use the generator like any other dependency.

```csharp
public class FooService(SnowflakeGenerator<long> snowflakeGen)
{
    public Foo CreateFoo() => new()
    {
        Id = snowflakeGen.NewSnowflake()
        // ...
    };
}
```

### Advanced

#### Custom-Size Snowflakes

Snowflakes are traditionally 64-bit integers, signed in most implementations but unsigned in others
(like Discord's). While this library defaults to `long`/`Int64` for maximum compatibility, it also
allows you to use any integer type that satisfies the constraint,
`where T : struct, IBinaryInteger<T>, IMinMaxValue<T>`. All common integer types in .NET (other than
`BigInteger`, which doesn't implement `IMinMaxValue<T>`) fall into this group.

You can specify a custom type by using the generic `SnowflakeGenerator.Create*` overloads.

```csharp
// No generic type argument - defaults to a builder to generate 64-bit signed snowflakes.
var snowflakeGenBuilder64 = SnowflakeGenerator.CreateBuilder();

// Same as above, but the type is specified explicitly.
var snowflakeGenBuilder64Explicit = SnowflakeGenerator.CreateBuilder<long>();

// Same size, but unsigned, so we get to use one extra bit.
var snowflakeGenBuilderU64 = SnowflakeGenerator.CreateBuilder<ulong>();

// Builders to generate 32-bit signed and unsigned snowflakes - half the default size.
var snowflakeGenBuilder32 = SnowflakeGenerator.CreateBuilder<int>();
var snowflakeGenBuilderU32 = SnowflakeGenerator.CreateBuilder<uint>();

// Builders to generate 128-bit signed and unsigned snowflakes - double the default size, as big as UUIDs.
var snowflakeGenBuilder128 = SnowflakeGenerator.CreateBuilder<Int128>();
var snowflakeGenBuilderU128 = SnowflakeGenerator.CreateBuilder<UInt128>();

// + sbyte, byte, short, ushort, and any other binary integer implementation you can find...
```

#### Blocking Timestamp Generation

Standard snowflakes use a sequence number to prevent collisions when multiple snowflakes are
generated in the same time unit. This library provides an additional snowflake component, called
`BlockingTimestampSnowflakeComponent`, that provides an alternative to using sequence numbers.

With blocking timestamps, calls to `NewSnowflake` will block the thread until the next timestamp
unit is available, ensuring that no two snowflakes are generated with the same timestamp.
This approach eliminates the need for a sequence component, simplifying the ID generation process.
However, it can potentially slow down ID generation, especially under high load, as threads may
frequently need to wait for the next timestamp unit to become available. On the upside, not having
sequence numbers means more bits are available for the timestamp, allowing for smaller units.

```csharp
var snowflakeGen = SnowflakeGenerator.CreateBuilder()
    .AddBlockingTimestamp(44, epoch, TimeSpan.TicksPerMillisecond / 2)
    .AddConstant(19, instanceId)
    .Build();
```

The example above sets the timestamp to be 44 bits with half-millisecond precision, meaning it will
have a 278-year lifetime and allow two snowflakes to be generated every millisecond.

#### Custom Components

The library already offers the usual timestamp, instance ID, and sequence number components,
but it also allows you to create your own components by subclassing `SnowflakeComponent`.

Below is an example custom component that provides random bits.

```csharp
public sealed class RandomSnowflakeComponent<T> : SnowflakeComponent<T>
    where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
{
    public RandomSnowflakeComponent(int lengthInBits) : base(lengthInBits)
    {
        AllowTruncation = true;
    }

    public override T CalculateValue(SnowflakeGenerationContext<T> ctx)
    {
        Span<byte> buffer = stackalloc byte[MaxLengthInBytes];
        while (true)
        {
            RandomNumberGenerator.Fill(buffer);
            if (T.TryReadLittleEndian(buffer, IsUnsigned, out var value))
                return value;
        }
    }
}
```

You can configure a snowflake generator to use any `SnowflakeComponent` implementation.

```csharp
var snowflakgeGen = SnowflakeGenerator.CreateBuilder()
    .AddTimestamp(30, epoch)
    .Add(new RandomSnowflakeComponent<long>(33)) // Here we add our custom component
    .Build();

// High 30 bits have milliseconds elapsed since `epoch` while low 33 bits are random.
// Similar to a version 7 UUID, albeit with a smaller range.
var snowflake = snowflakgeGen.NewSnowflake();
```

If you're feeling fancy, you can also write an extension method for `SnowflakeGeneratorBuilder`.

```csharp
public static class SnowflakeGeneratorBuilderExtensions
{
    public static SnowflakeGeneratorBuilder<T> AddRandom<T>(
        this SnowflakeGeneratorBuilder<T> builder, int lengthInBits)
        where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Add(new RandomSnowflakeComponent<T>(lengthInBits));
    }
}
```

The extension we created above allows usage like `AddRandom(33)`.

```csharp
var snowflakgeGen = SnowflakeGenerator.CreateBuilder()
    .AddTimestamp(30, epoch)
    .AddRandom(33) // Extension method
    .Build();
```

#### Snowflake Generation in Tests

You can't mock `SnowflakeGenerator` itself as it is sealed, but you can easily create instances
of it that return constant test data.

```csharp
var testSnowflakeGen = SnowflakeGenerator.CreateBuilder()
    .AddConstant(63, 123L)
    .Build();

// `testSnowflakeGen.NewSnowflake()` will always return 123.
```

If you want your test snowflake generation to be dynamic, you can mock (or inherit from) `SnowflakeComponent`.

```csharp
var random = new Random();

// NSubstitute example
var testComponent = Substitute.For<SnowflakeComponent<long>>(31);
testComponent
    .CalculateValue(Arg.Any<SnowflakeGenerationContext<long>>())
    .Returns(call =>
    {
        // Context can be used to access other components of the generator.
        // var ctx = call.Arg<SnowflakeGenerationContext<long>>();
        // var timestampLastValue = ctx.Components[0].LastValue;

        // Return any value for tests.
        return random.Next();
    });

// Assuming we have a test time provider
var testEpoch = TestTimeProvider.Instance.GetUtcNow().AddDays(-10);
var testSnowflakeGen = SnowflakeGenerator.CreateBuilder()
    .AddTimestamp(32, testEpoch)
    .Add(testComponent)
    .Build();
```

## Support

If you need any help, please feel free to [create an issue with the "question" label][issues-ask].

## Contributing

Thank you for your interest in contributing to Snowflakes!

Please see the [CONTRIBUTING.md](CONTRIBUTING.md) for more information.

For security bugs and vulnerabilities, please see [SECURITY.md](SECURITY.md).

[logo]: https://raw.githubusercontent.com/safakgur/snowflakes/main/media/logo-28.png "Logo"

[wf-ci]: https://github.com/safakgur/snowflakes/actions/workflows/ci.yml "CI Workflow"
[wf-ci-badge]: https://github.com/safakgur/snowflakes/actions/workflows/ci.yml/badge.svg?event=push "CI Badge"

[wf-codeql]: https://github.com/safakgur/snowflakes/actions/workflows/codeql.yml
[wf-codeql-badge]: https://github.com/safakgur/snowflakes/actions/workflows/codeql.yml/badge.svg?branch=main&event=push

[nuget]: https://www.nuget.org/packages/Snowflakes/ "NuGet Gallery"
[nuget-badge]: https://img.shields.io/nuget/v/Snowflakes.svg?style=flat "NuGet Badge"

[twitter-announcement]: https://blog.twitter.com/2010/announcing-snowflake "Announcing Snowflake"
[sonyflake]: https://github.com/sony/sonyflake "Sonyflake"
[discord-snowflakes]: https://discord.com/developers/docs/reference#snowflakes "Discord Developer Portal"
[instagram-sharding-and-ids]: https://instagram-engineering.com/sharding-ids-at-instagram-1cf5a71e5a5c "Sharding & IDs at Instagram"

[dotnet-di-keyed]: https://learn.microsoft.com/dotnet/core/extensions/dependency-injection#keyed-services ".NET Dependency Injection"

[issues-ask]: https://github.com/safakgur/snowflakes/issues/new?labels=question
