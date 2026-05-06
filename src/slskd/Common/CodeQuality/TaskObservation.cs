// <copyright file="TaskObservation.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Common.CodeQuality;

using System;
using System.Threading.Tasks;

/// <summary>
/// Helpers for fire-and-forget task observation.
/// </summary>
public static class TaskObservation
{
    /// <summary>
    /// Attach a fault handler to a task that is intentionally detached.
    /// </summary>
    /// <param name="task">The task to observe.</param>
    /// <param name="onFault">Callback invoked when the task faults.</param>
    /// <returns>The same task.</returns>
    public static Task Observe(Task task, Action<Exception> onFault)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        if (onFault == null)
        {
            throw new ArgumentNullException(nameof(onFault));
        }

        _ = task.ContinueWith(
            static (faultedTask, state) =>
            {
                var callback = (Action<Exception>?)state;
                if (callback == null)
                {
                    return;
                }

                var exception = faultedTask.Exception?.GetBaseException();
                if (exception != null)
                {
                    callback(exception);
                }
            },
            onFault,
            default,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return task;
    }

    /// <summary>
    /// Attach a fault handler to a generic task that is intentionally detached.
    /// </summary>
    /// <typeparam name="T">Task result type.</typeparam>
    /// <param name="task">The task to observe.</param>
    /// <param name="onFault">Callback invoked when the task faults.</param>
    /// <returns>The same task.</returns>
    public static Task<T> Observe<T>(Task<T> task, Action<Exception> onFault)
    {
        _ = Observe((Task)task, onFault);
        return task;
    }
}
