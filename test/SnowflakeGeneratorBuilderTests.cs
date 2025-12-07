using System.Security.Cryptography;
using NSubstitute;
using Snowflakes.Components;
using Snowflakes.Resources;

namespace Snowflakes.Tests;

public sealed class SnowflakeGeneratorBuilderTests
{
    private readonly SnowflakeGeneratorBuilder<long> _builder = new();

    [Fact]
    public void Add_throws_when_component_is_null()
    {
        Assert.Throws<ArgumentNullException>("component", () => _builder.Add(null!));
    }

    [Fact]
    public void Add_throws_when_component_is_already_added()
    {
        var component = new ConstantSnowflakeComponent<long>(lengthInBits: 1, value: 1);

        _builder.Add(component);

        Assert.Throws<ArgumentException>("component", () => _builder.Add(component));
    }

    [Fact]
    public void Add_throws_when_component_is_too_big()
    {
        var component63 = new ConstantSnowflakeComponent<long>(lengthInBits: 63, value: 1);
        var component1 = new ConstantSnowflakeComponent<long>(lengthInBits: 1, value: 1);

        _builder.Add(component63);

        Assert.Throws<ArgumentException>("component", () => _builder.Add(component1));
    }

    [Fact]
    public void Add_returns_current_instance()
    {
        var component = new ConstantSnowflakeComponent<long>(lengthInBits: 1, value: 1);

        var result = _builder.Add(component);

        Assert.Same(_builder, result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddTimestamp_and_AddBlockingTimestamp_create_components_with_correct_properties(
        bool addBlockingTimestamp)
    {
        var random = new Random();

        var lengthInBits = random.Next(1, 10);
        var epoch = DateTimeOffset.FromUnixTimeMilliseconds(random.Next(1, 10));
        var ticksPerUnit = random.Next(1, 10);
        var now = new DateTimeOffset(2024, 08, 31, 18, 18, 0, TimeSpan.Zero);

        var testTimeProvider = Substitute.For<TimeProvider>();
        testTimeProvider.GetUtcNow().Returns(now);

        if (addBlockingTimestamp)
            _builder.AddBlockingTimestamp(lengthInBits, epoch, ticksPerUnit, testTimeProvider);
        else
            _builder.AddTimestamp(lengthInBits, epoch, ticksPerUnit, testTimeProvider);

        var component = _builder.Build().Components[0];
        var tsComponent = addBlockingTimestamp
            ? Assert.IsType<BlockingTimestampSnowflakeComponent<long>>(component)
            : Assert.IsType<TimestampSnowflakeComponent<long>>(component);

        Assert.Equal(lengthInBits, tsComponent.LengthInBits);
        Assert.Equal(epoch, tsComponent.Epoch);
        Assert.Equal(ticksPerUnit, tsComponent.TicksPerUnit);
        Assert.Equal(testTimeProvider, tsComponent.TimeProvider);
    }

    [Fact]
    public void AddConstant_value_creates_component_with_correct_properties()
    {
        var random = new Random();

        var lengthInBits = random.Next(1, 10);
        var value = random.Next(1, 10);

        var component = _builder
            .AddConstant(lengthInBits, value)
            .Build().Components[0];

        var constComponent = Assert.IsType<ConstantSnowflakeComponent<long>>(component);

        Assert.Equal(lengthInBits, constComponent.LengthInBits);
        Assert.Equal(value, constComponent.Value);
    }

    [Theory]
    [InlineData("MD5", 2353163291832495564L)]
    [InlineData("SHA256", 8069623936395563335L)]
    [Obsolete(DeprecationMessages.HashedConstantComponent)]
    public void AddConstant_valueToHash_creates_component_with_correct_properties(
        string algName, long expectedValue)
    {
        var lengthInBits = 63;
        var valueToHash = "test value";
        using HashAlgorithm hashAlg = algName switch
        {
            "MD5" => MD5.Create(),
            "SHA256" => SHA256.Create(),
            _ => throw new NotImplementedException()
        };

        var component = _builder
            .AddConstant(lengthInBits, valueToHash, hashAlg)
            .Build().Components[0];

        var constComponent = Assert.IsType<ConstantSnowflakeComponent<long>>(component);

        Assert.Equal(lengthInBits, constComponent.LengthInBits);
        Assert.Equal(expectedValue, constComponent.Value);
    }

    [Fact]
    public void AddSequenceForTimestamp_throws_when_no_timestamp_component_found()
    {
        _builder.AddConstant(lengthInBits: 1, value: 1);

        Assert.Throws<InvalidOperationException>(() =>
            _builder.AddSequenceForTimestamp(lengthInBits: 1));
    }

    [Fact]
    public void AddSequenceForTimestamp_ignores_blocking_timestamp_components()
    {
        _builder.AddBlockingTimestamp(lengthInBits: 1, epoch: default);

        Assert.Throws<InvalidOperationException>(() =>
            _builder.AddSequenceForTimestamp(lengthInBits: 1));

        var component = _builder
            .AddTimestamp(lengthInBits: 1, epoch: default)
            .AddSequenceForTimestamp(lengthInBits: 1)
            .Build().Components[^1];

        var seqComponent = Assert.IsType<SequenceSnowflakeComponent<long>>(component);

        Assert.Equal(1, seqComponent.ReferenceComponentIndex);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 3)]
    public void AddSequenceForTimestamp_creates_component_with_correct_properties(
        int tsComponentIndex, int seqComponentIndex)
    {
        var random = new Random();

        var lengthInBits = random.Next(1, 10);

        for (var i = 0; i < tsComponentIndex; i++)
            _builder.AddConstant(lengthInBits: 1, value: 0);

        _builder.AddTimestamp(1, epoch: DateTimeOffset.UnixEpoch);

        for (var i = tsComponentIndex + 1; i < seqComponentIndex; i++)
            _builder.AddConstant(lengthInBits: 1, value: 0);

        _builder.AddSequenceForTimestamp(lengthInBits);

        var component = _builder.Build().Components[^1];
        var seqComponent = Assert.IsType<SequenceSnowflakeComponent<long>>(component);

        Assert.Equal(lengthInBits, seqComponent.LengthInBits);
        Assert.Equal(tsComponentIndex, seqComponent.ReferenceComponentIndex);
    }

    [Fact]
    public void AddSequence_throws_when_index_is_out_of_range()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _builder.AddSequence(lengthInBits: 1, refComponentIndex: 0));

        _builder.AddConstant(lengthInBits: 1, value: 1);

        Assert.Throws<ArgumentOutOfRangeException>("refComponentIndex", () =>
            _builder.AddSequence(lengthInBits: 1, refComponentIndex: -1));

        Assert.Throws<ArgumentOutOfRangeException>("refComponentIndex", () =>
            _builder.AddSequence(lengthInBits: 1, refComponentIndex: 1));
    }

    [Fact]
    public void AddSequence_throws_when_reference_component_is_blocking_timestamp()
    {
        _builder.AddBlockingTimestamp(lengthInBits: 1, epoch: default);

        Assert.Throws<ArgumentOutOfRangeException>("refComponentIndex", () =>
            _builder.AddSequence(lengthInBits: 1, refComponentIndex: 0));
    }

    [Fact]
    public void AddSequence_creates_component_with_correct_properties()
    {
        var random = new Random();

        var lengthInBits = random.Next(1, 10);
        var refComponentIndex = random.Next(1, 10);

        for (var i = 0; i <= refComponentIndex; i++)
            _builder.AddConstant(lengthInBits: 1, value: 0);

        var component = _builder
            .AddSequence(lengthInBits, refComponentIndex)
            .Build().Components[^1];

        var seqComponent = Assert.IsType<SequenceSnowflakeComponent<long>>(component);

        Assert.Equal(lengthInBits, seqComponent.LengthInBits);
        Assert.Equal(refComponentIndex, seqComponent.ReferenceComponentIndex);
    }

    [Fact]
    public void Build_throws_when_no_components_added()
    {
        Assert.Throws<InvalidOperationException>(() => _builder.Build());
    }

    [Fact]
    public void Build_adds_components_in_correct_order()
    {
        var gen1 = _builder
            .AddConstant(lengthInBits: 1, value: 8)
            .AddSequence(lengthInBits: 1, refComponentIndex: 0)
            .Build();

        Assert.Equal(2, gen1.Components.Length);
        Assert.Equal(typeof(ConstantSnowflakeComponent<long>), gen1.Components[0].GetType());
        Assert.Equal(typeof(SequenceSnowflakeComponent<long>), gen1.Components[1].GetType());

        var gen2 = new SnowflakeGeneratorBuilder<long>()
            .AddTimestamp(lengthInBits: 1, epoch: DateTimeOffset.UnixEpoch)
            .AddConstant(lengthInBits: 1, value: 8)
            .Build();

        Assert.Equal(2, gen2.Components.Length);
        Assert.Equal(typeof(TimestampSnowflakeComponent<long>), gen2.Components[0].GetType());
        Assert.Equal(typeof(ConstantSnowflakeComponent<long>), gen2.Components[1].GetType());
    }
}
