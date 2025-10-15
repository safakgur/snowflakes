namespace Snowflakes.Resources;

internal static class DeprecationMessages
{
    internal const string HashedConstantComponent = """
        Using hash values as snowflake components is not recommended as a hash cannot guarantee uniqueness.
        The support for this will be removed in the next major version of the Snowflakes library.
        """;
}
