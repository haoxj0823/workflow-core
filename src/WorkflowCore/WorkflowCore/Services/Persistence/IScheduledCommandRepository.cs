using WorkflowCore.Models;

namespace WorkflowCore.Services.Persistence;

public interface IScheduledCommandRepository
{
    bool SupportsScheduledCommands { get; }

    Task ScheduleCommand(ScheduledCommand command);

    Task ProcessCommands(DateTimeOffset asOf, Func<ScheduledCommand, Task> action, CancellationToken cancellationToken = default);
}
