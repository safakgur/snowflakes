using System.Security.Cryptography;
using System.Text;

namespace Snowflakes.Components;

/// <summary>Provides a fixed value to be placed in a snowflake.</summary>
public sealed class ConstantSnowflakeComponent : SnowflakeComponent
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConstantSnowflakeComponent" /> class
    ///     that provides the specified value..
    /// </summary>
    /// <param name="lengthInBits">
    ///     <inheritdoc cref="SnowflakeComponent.SnowflakeComponent" path="/param[@name='lengthInBits']" />
    /// </param>
    /// <param name="value">The value that will be provided by this component.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <inheritdoc cref="SnowflakeComponent.SnowflakeComponent" path="/exception[@cref='ArgumentOutOfRangeException']"/>
    ///     -or-
    ///     <paramref name="value"/> is negative.
    /// </exception>
    public ConstantSnowflakeComponent(int lengthInBits, long value)
        : base(lengthInBits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);

        Value = value;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConstantSnowflakeComponent" /> class
    ///     that provides a (up to 63-bit) hash of the specified string.
    /// </summary>
    /// <param name="lengthInBits">
    ///     <inheritdoc cref="SnowflakeComponent(int)" path="/param[@name='lengthInBits']" />
    /// </param>
    /// <param name="valueToHash">The string value to be hashed to produce the fixed value.</param>
    /// <param name="hashAlg">The hash algorithm used to hash the string value.</param>
    /// <remarks>
    ///     <para>
    ///         Prefer the other constructor when possible and fallback to this one only if you
    ///         cannot get an integer value, as hashing might produce conflicts. Choose a good
    ///         algorithm and specify a higher length to minimize the risk of hash collisions.
    ///     </para>
    ///     <para>
    ///         For example, if you use Azure App Services, there is no easy and reliable way of
    ///         getting an integer index of the current "instance" (also referred to as "machine"
    ///         or "shard" by other providers) of your application, but a string that uniquely
    ///         identifies the instance can be retrieved from the "WEBSITE_INSTANCE_ID" environment
    ///         variable. Hashing that string can be a relatively safe way to get a smaller,
    ///         fixed-length identifier.
    ///     </para>
    /// </remarks>
    public ConstantSnowflakeComponent(int lengthInBits, string valueToHash, HashAlgorithm hashAlg)
        : base(lengthInBits)
    {
        ArgumentException.ThrowIfNullOrEmpty(valueToHash);
        ArgumentNullException.ThrowIfNull(hashAlg);

        var sourceValueInUtf8 = Encoding.UTF8.GetBytes(valueToHash);

        var hash = new byte[hashAlg.HashSize / 8].AsSpan();
        hashAlg.TryComputeHash(sourceValueInUtf8, hash, out _);
        hash = hash[..8];

        if (!BitConverter.IsLittleEndian)
        {
            // So that the same value produces the same hash on different machines.
            hash.Reverse();
        }

        Value = BitConverter.ToInt64(hash);
        Value = Math.Abs(Value);
    }

    /// <summary>Gets the value that will be provided by this component.</summary>
    /// <remarks>
    ///     Note that <see cref="SnowflakeComponent.GetValue" /> masks this value to get only the
    ///     lowest <see cref="SnowflakeComponent.LengthInBits" /> number of bits. Any higher bits,
    ///     i.e., bits to the left, are discarded.
    /// </remarks>
    public long Value { get; }

    /// <inheritdoc />
    protected override long CalculateValue(SnowflakeGenerationContext ctx) => Value;
}
