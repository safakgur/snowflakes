﻿using System.Security.Cryptography;
using Snowflakes.Components;

namespace Snowflakes;

/// <summary>Builds <see cref="SnowflakeGenerator" /> instances.</summary>
public sealed class SnowflakeGeneratorBuilder
{
    private readonly HashSet<SnowflakeComponent> _componentSet = new(3);
    private readonly List<SnowflakeComponent> _componentList = new(3);

    private int _totalLengthInBits;

    /// <summary>Adds a timestamp component to the snowflakes that will be generated.</summary>
    /// <returns>A reference to the current builder instance.</returns>
    /// <exception cref="ArgumentException">
    ///     Adding the component would make the total component length exceed 63 bits.
    /// </exception>
    /// <inheritdoc cref="TimestampSnowflakeComponent.TimestampSnowflakeComponent" />
    public SnowflakeGeneratorBuilder AddTimestamp(
        int lengthInBits,
        DateTimeOffset epoch,
        double ticksPerUnit = TimeSpan.TicksPerMillisecond,
        TimeProvider? timeProvider = null)
    {
        var component = new TimestampSnowflakeComponent(lengthInBits, epoch, ticksPerUnit, timeProvider);

        return Add(component);
    }

    /// <summary>Adds a constant component to the snowflakes that will be generated.</summary>
    /// <returns>A reference to the current builder instance.</returns>
    /// <exception cref="ArgumentException">
    ///     Adding the component would make the total component length exceed 63 bits.
    /// </exception>
    /// <inheritdoc cref="ConstantSnowflakeComponent(int, long)" />
    public SnowflakeGeneratorBuilder AddConstant(int lengthInBits, long value)
    {
        var component = new ConstantSnowflakeComponent(lengthInBits, value);

        return Add(component);
    }

    /// <summary>Adds a constant component to the snowflakes that will be generated.</summary>
    /// <returns>A reference to the current builder instance.</returns>
    /// <exception cref="ArgumentException">
    ///     <inheritdoc cref="ConstantSnowflakeComponent(int, string, HashAlgorithm)" path="/exception[@cref='ArgumentException']"/>
    ///     -or-
    ///     Adding the component would make the total component length exceed 63 bits.
    /// </exception>
    /// <inheritdoc cref="ConstantSnowflakeComponent(int, string, HashAlgorithm)" />
    public SnowflakeGeneratorBuilder AddConstant(int lengthInBits, string valueToHash, HashAlgorithm hashAlg)
    {
        var component = new ConstantSnowflakeComponent(lengthInBits, valueToHash, hashAlg);

        return Add(component);
    }

    /// <summary>
    ///     Adds a sequence component to the snowflakes that will be generated.
    ///     The sequence will be bound to the first timestamp component found.
    /// </summary>
    /// <returns>A reference to the current builder instance.</returns>
    /// <exception cref="InvalidOperationException">
    ///     No timestamp component found. Call <see cref="AddTimestamp" /> before
    ///     <see cref="AddSequenceForTimestamp" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Adding the component would make the total component length exceed 63 bits.
    /// </exception>
    /// <inheritdoc cref="SequenceSnowflakeComponent(int, int)" />
    public SnowflakeGeneratorBuilder AddSequenceForTimestamp(int lengthInBits)
    {
        var refComponentIndex = -1;
        for (var i = 0; i < _componentList.Count; i++)
            if (_componentList[i] is TimestampSnowflakeComponent)
            {
                refComponentIndex = i;
                break;
            }

        if (refComponentIndex == -1)
            throw new InvalidOperationException("No timestamp component found.");

        var component = new SequenceSnowflakeComponent(lengthInBits, refComponentIndex);

        return Add(component);
    }

    /// <summary>
    ///     Adds a sequence component to the snowflakes that will be generated.
    ///     The sequence will be bound to the component at the specified index.
    /// </summary>
    /// <returns>A reference to the current builder instance.</returns>
    /// <exception cref="ArgumentException">
    ///     Adding the component would make the total component length exceed 63 bits.
    /// </exception>
    /// <inheritdoc cref="SequenceSnowflakeComponent(int, int)" />
    public SnowflakeGeneratorBuilder AddSequence(int lengthInBits, int refComponentIndex)
    {
        var component = new SequenceSnowflakeComponent(lengthInBits, refComponentIndex);

        return Add(component);
    }

    /// <summary>Adds the specified component to the snowflakes that will be generated.</summary>
    /// <param name="component">The component to add.</param>
    /// <returns>A reference to the current builder instance.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="component" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="component" /> is already added, or adding it would make the total
    ///     component length exceed 63 bits.
    /// </exception>
    public SnowflakeGeneratorBuilder Add(SnowflakeComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        if (_totalLengthInBits + component.LengthInBits > SnowflakeComponent.MaxLengthInBits)
            throw new ArgumentException(
                $"Adding the component would make the total snowflake length exceed {SnowflakeComponent.MaxLengthInBits} bits.",
                nameof(component));

        if (!_componentSet.Add(component))
            throw new ArgumentException("Component already added.", nameof(component));

        _componentList.Add(component);

        _totalLengthInBits += component.LengthInBits;

        return this;
    }

    /// <summary>Creates a snowflake generator instance with the added components.</summary>
    /// <returns>A new snowflake generator with the added components.</returns>
    /// <exception cref="InvalidOperationException">No components added.</exception>
    public SnowflakeGenerator Build()
    {
        if (_componentList.Count == 0)
            throw new InvalidOperationException(
                "At least one component is needed to build a snowflake generator.");

        return new(_componentList);
    }
}
