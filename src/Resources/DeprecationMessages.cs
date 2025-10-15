namespace Snowflakes.Resources;

internal static class DeprecationMessages
{
    internal const string HashedConstantComponent =
        "Using hash values as snowflake components is not recommended as a hash cannot guarantee uniqueness. " +
        "The support for this will be removed in the next major version of the Snowflakes library.";

    internal const string BuiltInBase36UpperEncoding =
        $"This encoder is getting renamed to {nameof(SnowflakeEncoder.Base36UpperOrdinal)}, " +
        $"please use that instead. " + BuiltInEncoding;

    internal const string BuiltInBase36LowerEncoding =
        $"This encoder is getting renamed to {nameof(SnowflakeEncoder.Base36LowerOrdinal)}, " +
        $"please use that instead. " + BuiltInEncoding;

    internal const string BuiltInBase62Encoding =
        $"This encoder is getting renamed to {nameof(SnowflakeEncoder.Base62Ordinal)}, " +
        $"please use that instead. " + BuiltInEncoding;

    internal const string BuiltInBase64Encoding =
        $"This encoder is getting renamed to {nameof(SnowflakeEncoder.Base64Ordinal)}, " +
        $"please use that instead. " + BuiltInEncoding;

    private const string BuiltInEncoding =
        "This is to avoid confusion between snowflake encodings and binary-to-text encodings of arbitrary byte " +
        "spans with similar names. This property will be removed in the next major version of the Snowflakes library.";
}
