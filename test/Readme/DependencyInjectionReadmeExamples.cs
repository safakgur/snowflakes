using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snowflakes.Tests.Testing;

namespace Snowflakes.Tests.Readme;

public sealed class DependencyInjectionReadmeExamples : BaseReadmeExamples
{
    [Fact]
    public void Dependency_injection_registration()
    {
        var services = new ServiceCollection()
            .AddSingleton(TestTimeProvider.Frozen)
            .Configure<ProgramOptions>(opts => opts.InstanceId = 1);

        // CONTENT-START

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

        // CONTENT-END

        services
            .BuildServiceProvider()
            .GetRequiredService<SnowflakeGenerator<long>>();
    }

    // Dependency_injection_usage
    // CONTENT-START

    public class FooService(SnowflakeGenerator<long> snowflakeGen)
    {
        public Foo CreateFoo() => new()
        {
            Id = snowflakeGen.NewSnowflake()
            // ...
        };
    }

    // CONTENT-END

    public class ProgramOptions { public int InstanceId { get; set; } }

    public record Foo { public required long Id { get; init; } }
}
