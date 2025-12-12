using WorkflowCore.Models;

namespace WorkflowCore.Services.Persistence;

public interface IScheduledCommandRepository
{
    bool SupportsScheduledCommands { get; }

    Task ScheduleCommandAsync(ScheduledCommand command);

    Task ProcessCommandsAsync(DateTimeOffset asOf, Func<ScheduledCommand, Task> action, CancellationToken cancellationToken = default);
}
