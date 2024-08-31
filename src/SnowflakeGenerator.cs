using System.Diagnostics;
using Snowflakes.Components;

namespace Snowflakes;

/// <summary>Generates 64-bit Snowflake IDs, also known as snowflakes.</summary>
public sealed class SnowflakeGenerator
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly object _syncRoot = new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly (SnowflakeComponent Component, int BitsToShiftLeft)[] _componentsInExecutionOrder;

    private readonly SnowflakeGenerationContext _context;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnowflakeGenerator" /> class with the
    ///     specified components.
    /// </summary>
    /// <param name="components">
    ///     <para>
    ///         The components to produce the parts that will make up the generated snowflakes.
    ///     </para>
    ///     <para>
    ///         The order of the components determine their place in the snowflake, with the bits
    ///         produced by the first component taking the highest (leftmost) location and the bits
    ///         produced by the last component taking the lowest (rightmost) location.
    ///     </para>
    ///     <para>
    ///         Note that an instance of a snowflake component must only be used by a single
    ///         generator, as it may store state that should not be shared.
    ///     </para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="components" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="components" /> is empty, contains a null item, contains duplicate items,
    ///     or the sum of its items' lengths exceeds 63 bits.
    /// </exception>
    public SnowflakeGenerator(IEnumerable<SnowflakeComponent> components)
    {
        ArgumentNullException.ThrowIfNull(components);

        var inInsertionOrder = components.ToArray();

        if (inInsertionOrder.Length == 0)
            throw new ArgumentException(
                "At least one component is required.", nameof(components));

        _componentsInExecutionOrder = new (SnowflakeComponent Component, int)[inInsertionOrder.Length];

        var set = new HashSet<SnowflakeComponent>();
        var totalLengthInBits = 0;
        var needsSort = false;
        for (var i = inInsertionOrder.Length - 1; i >= 0; i--)
        {
            var component = inInsertionOrder[i]
                ?? throw new ArgumentException(
                    "Component collection cannot contain null items.", nameof(components));

            if (!set.Add(component))
                throw new ArgumentException(
                    "Component collection cannot contain duplicate items.", nameof(components));

            _componentsInExecutionOrder[i] = (component, totalLengthInBits);

            totalLengthInBits += component.LengthInBits;

            if (component.ExecutionOrder != 0)
                needsSort = true;
        }

        if (totalLengthInBits > SnowflakeComponent.MaxLengthInBits)
            throw new ArgumentException(
                $"Total number of bits produced by the components cannot exceed {SnowflakeComponent.MaxLengthInBits}.",
                nameof(components));

        if (needsSort)
            Array.Sort(_componentsInExecutionOrder,
                static (a, b) => a.Component.ExecutionOrder.CompareTo(b.Component.ExecutionOrder));

        _context = new(inInsertionOrder);
    }

    /// <summary>
    ///     Gets the components to produce the parts that make up the generated snowflakes.
    /// </summary>
    public ReadOnlySpan<SnowflakeComponent> Components => _context.Components;

    /// <summary>Generates a snowflake with the configured components.</summary>
    /// <returns>A non-negative 64-bit integer, sortable based on the configured components.</returns>
    public long NewSnowflake()
    {
        var result = 0L;

        lock (_syncRoot)
            foreach (var (component, bitsToShiftLeft) in _componentsInExecutionOrder)
                result |= component.GetValue(_context) << bitsToShiftLeft;

        return result;
    }
}
