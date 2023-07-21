using Spectre.Console;
namespace Extractor;

public static class ProgressTaskExtensions
{
    /// <summary>Increments the value of the progress task to 100% and stops the task.</summary>
    public static ProgressTask Complete(this ProgressTask task)
    {
        task.Increment(task.MaxValue - task.Value);
        task.IsIndeterminate = false;
        task.StopTask();
        return task;
    }
    
    public static ProgressTask IncrementMaxValue(this ProgressTask task, int increment = 1)
    {
        task.MaxValue(task.MaxValue + increment);
        return task;
    }
}
