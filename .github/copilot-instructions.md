# Snowflakes AI Coding Instructions

## Project Overview

Snowflakes are integer identifiers that can be generated in a distributed system without a central
authority. This project is a .NET class library that allows users to define their own snowflake
formats and generate snowflakes.

## File Layout

- `src/`: Library source code.
- `test/`: Test source code.
- `.editorconfig`: Formatting preferences.
- `VERSION`: Version of the NuGet package to publish.

## Development

Ensure the solution can build.

- `dotnet build` at root - Full solution build.
- `dotnet build src` or `dotnet build test` can be used to build individual projects.

Ensure every change is covered by tests, and all tests pass
 
- `dotnet test` at root - Run all tests.

Ensure the code follows style rules defined in the .editorconfig file.

- `dotnet format` - Formats the code based on rules defined in .editorconfig.
- Prefer `dotnet format --include <space-delimited relative paths>` to format only the changed files.
  - Example: `dotnet format --include 'src/SnowflakeEncoder.cs' 'src/SnowflakeGenerator.cs'`

Ensure the `VERSION` file is updated if necessary.

- Change in version causes a new package to be deployed to NuGet. No version change = No deployment.
- Bump the version only if there is a change in `src/`, as that's the project we ship.
  - Do not bump the version for insignificant, documentation-only changes, like typo fixes.
    They can be batched with the next meaningful update.
- Follow Semantic Versioning:
  - Major version bump for breaking changes.
  - Minor version bump for backward-compatible new features.
  - Patch version bump for backward-compatible bug fixes.

For more context see `README.md`, which acts as the user guide and has code examples.
