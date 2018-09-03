using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.Core.Extensions;

namespace Milou.Deployer.Core.Processes
{
    public static class ProcessRunner
    {
        public static async Task<ExitCode> ExecuteAsync(
            string executePath,
            IEnumerable<string> arguments = null,
            Action<string, string> standardOutLog = null,
            Action<string, string> standardErrorAction = null,
            Action<string, string> toolAction = null,
            Action<string, string> verboseAction = null,
            IEnumerable<KeyValuePair<string, string>> environmentVariables = null,
            Action<string, string> debugAction = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(executePath))
            {
                throw new ArgumentNullException(nameof(executePath));
            }

            if (!File.Exists(executePath))
            {
                throw new ArgumentException(
                    $"The executable file '{executePath}' does not exist",
                    nameof(executePath));
            }

            IEnumerable<string> usedArguments = arguments ?? Enumerable.Empty<string>();

            string formattedArguments = string.Join(" ", usedArguments.Select(arg => $"\"{arg}\""));

            Task<ExitCode> task = RunProcessAsync(executePath,
                formattedArguments,
                standardErrorAction,
                standardOutLog,
                toolAction,
                verboseAction,
                environmentVariables,
                debugAction,
                cancellationToken);

            ExitCode exitCode = await task;

            return exitCode;
        }

        private static async Task<ExitCode> RunProcessAsync(
            string executePath,
            string formattedArguments,
            Action<string, string> standardErrorAction,
            Action<string, string> standardOutputLog,
            Action<string, string> toolAction,
            Action<string, string> verboseAction = null,
            IEnumerable<KeyValuePair<string, string>> environmentVariables = null,
            Action<string, string> debugAction = null,
            CancellationToken cancellationToken = default)
        {
            Action<string, string> usedToolAction = toolAction ?? ((_, __) => { });
            Action<string, string> standardAction = standardOutputLog ?? ((_, __) => { });
            Action<string, string> errorAction = standardErrorAction ?? ((_, __) => { });
            Action<string, string> verbose = verboseAction ?? ((_, __) => { });
            Action<string, string> debug = debugAction ?? ((_, __) => { });

            var taskCompletionSource = new TaskCompletionSource<ExitCode>();

            string processWithArgs = $"\"{executePath}\" {formattedArguments}".Trim();

            usedToolAction($"[{typeof(ProcessRunner).Name}] Executing: {processWithArgs}", null);

            bool useShellExecute = standardErrorAction == null && standardOutputLog == null;

            bool redirectStandardError = standardErrorAction != null;

            bool redirectStandardOutput = standardOutputLog != null;

            var processStartInfo = new ProcessStartInfo(executePath)
            {
                Arguments = formattedArguments,
                RedirectStandardError = redirectStandardError,
                RedirectStandardOutput = redirectStandardOutput,
                UseShellExecute = useShellExecute,
                CreateNoWindow = true
            };

            if (environmentVariables != null)
            {
                foreach (KeyValuePair<string, string> environmentVariable in environmentVariables)
                {
                    processStartInfo.EnvironmentVariables.Add(environmentVariable.Key, environmentVariable.Value);
                }
            }

            var exitCode = new ExitCode(99);

            bool disposed = false;

            var process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };

            process.Disposed += (_, __) =>
            {
                if (!taskCompletionSource.Task.CanBeAwaited())
                {
                    verbose("Task was not completed, but process was disposed", null);
                    taskCompletionSource.TrySetResult(ExitCode.Failure);
                }

                verbose($"Disposed process '{processWithArgs}'", null);
                disposed = true;
                process = null;
            };

            if (redirectStandardError)
            {
                process.ErrorDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        errorAction(args.Data, null);
                    }
                };
            }

            if (redirectStandardOutput)
            {
                process.OutputDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        standardAction(args.Data, null);
                    }
                };
            }

            process.Exited += (sender, args) =>
            {
                if (!(sender is Process proc))
                {
                    if (!taskCompletionSource.Task.CanBeAwaited())
                    {
                        errorAction("Task is not in a valid state, sender is not process", null);
                        taskCompletionSource.SetResult(ExitCode.Failure);
                    }

                    return;
                }

                if (disposed)
                {
                    debugAction?.Invoke("Process disposed", null);
                    process = null;
                    return;
                }

                proc.EnableRaisingEvents = false;

                if (taskCompletionSource.Task.CanBeAwaited())
                {
                    return;
                }

                proc.Refresh();
                int procExitCode;
                try
                {
                    if (taskCompletionSource.Task.CanBeAwaited())
                    {
                        return;
                    }

                    procExitCode = proc.ExitCode;
                }
                catch (Exception ex)
                {
                    errorAction($"Failed to get exit code from process {ex}", null);
                    disposed = true;
                    proc.Dispose();
                    process = null;
                    taskCompletionSource.SetException(ex);
                    return;
                }

                var result = new ExitCode(procExitCode);
                toolAction?.Invoke($"Process '{processWithArgs}' exited with code {result}", null);

                if (!taskCompletionSource.Task.CanBeAwaited())
                {
                    taskCompletionSource.SetResult(result);
                }

                proc.Dispose();
                disposed = true;
                process = null;
            };

            int processId = -1;

            try
            {
                bool started = process.Start();

                if (!started)
                {
                    errorAction($"Process '{processWithArgs}' could not be started", null);
                    process.Dispose();
                    disposed=true;
                    process = null;

                    taskCompletionSource.SetResult(ExitCode.Failure);

                    return await taskCompletionSource.Task;
                }

                if (redirectStandardError)
                {
                    process.BeginErrorReadLine();
                }

                if (redirectStandardOutput)
                {
                    process.BeginOutputReadLine();
                }

                int bits = process.IsWin64() ? 64 : 32;

                try
                {
                    processId = process.Id;
                }
                catch (InvalidOperationException ex)
                {
                    debug($"Could not get process id for process '{processWithArgs}'. {ex}", null);
                }

                string temp = process.HasExited ? "was" : "is";

                verbose(
                    $"The process '{processWithArgs}' {temp} running in {bits}-bit mode",
                    null);
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }

                errorAction($"An error occured while running process {processWithArgs}: {ex}", null);
                taskCompletionSource.SetException(ex);
            }

            bool done = false;

            if (taskCompletionSource.Task.CanBeAwaited())
            {
                if (!disposed)
                {
                    process?.Dispose();
                    disposed = true;
                    process = null;
                }

                return await taskCompletionSource.Task;
            }

            try
            {
                while (IsAlive(process, taskCompletionSource.Task, cancellationToken, done, processWithArgs, verbose))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    Task delay = Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);

                    await delay;

                    if (taskCompletionSource.Task.IsCompleted)
                    {
                        done = true;
                        exitCode = await taskCompletionSource.Task;
                    }
                    else if (taskCompletionSource.Task.IsCanceled)
                    {
                        exitCode = await taskCompletionSource.Task;
                    }
                    else if (taskCompletionSource.Task.IsFaulted)
                    {
                        exitCode = await taskCompletionSource.Task;
                    }
                    else
                    {
                        exitCode = ExitCode.Failure;
                    }
                }
            }
            finally
            {
                if (!exitCode.IsSuccess)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        process?.Refresh();

                        if (process?.HasExited == false)
                        {
                            try
                            {
                                toolAction($"Cancellation is requested, trying to kill process {processWithArgs}",
                                    null);

                                if (processId > 0)
                                {
                                    string args = $"/PID {processId}";
                                    string killProcessPath =
                                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                                            "taskkill.exe");
                                    toolAction($"Running {killProcessPath} {args}", null);

                                    using (Process.Start(killProcessPath, args))
                                    {

                                    }

                                    errorAction(
                                        $"Killed process {processWithArgs} because cancellation was requested",
                                        null);
                                }
                                else
                                {
                                    debugAction(
                                        $"Could not kill process '{processWithArgs}', missing process id",
                                        null);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (ex.IsFatal())
                                {
                                    throw;
                                }

                                errorAction(
                                    $"ProcessRunner could not kill process {processWithArgs} when cancellation was requested",
                                    null);
                                errorAction(
                                    $"Could not kill process {processWithArgs} when cancellation was requested",
                                    null);
                                errorAction(ex.ToString(), null);
                            }
                        }
                    }
                }

                using (process)
                {
                    verbose(
                        $"Task status: {taskCompletionSource.Task.Status}, {taskCompletionSource.Task.IsCompleted}",
                        null);
                    verbose($"Disposing process {processWithArgs}", null);
                }
            }

            verbose($"Process runner exit code {exitCode} for process {processWithArgs}", null);

            try
            {
                if (processId > 0)
                {
                    bool stillAlive = false;

                    using (Process stillRunningProcess = Process.GetProcesses().SingleOrDefault(p => p.Id == processId))
                    {
                        if (stillRunningProcess != null)
                        {
                            if (!stillRunningProcess.HasExited)
                            {
                                stillAlive = true;

                            }
                        }
                    }

                    if (stillAlive)
                    {
                        errorAction(
                            $"The process with ID {processId.ToString(CultureInfo.InvariantCulture)} '{processWithArgs}' is still running",
                            null);

                        taskCompletionSource.SetResult(ExitCode.Failure);

                        return await taskCompletionSource.Task;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }

                debugAction($"Could not check processes. {ex}", null);
            }

            if (!disposed)
            {
                process?.Dispose();
                disposed = true;
                process = null;
            }


            return await taskCompletionSource.Task;
        }

        private static bool IsAlive(
            Process process,
            Task<ExitCode> task,
            CancellationToken cancellationToken,
            bool done,
            string processWithArgs,
            Action<string, string> verbose)
        {
            if (process == null)
            {
                verbose($"Process {processWithArgs} does no longer exist", null);
                return false;
            }

            if (task.IsCompleted)
            {
                return false;
            }

            process.Refresh();

            try
            {
                if (process.HasExited)
                {
                    return false;
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                //ignore
            }

            if (task.CanBeAwaited())
            {
                TaskStatus status = task.Status;
                verbose($"Task status for process {processWithArgs} is {status}", null);
                return false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                verbose($"Cancellation is requested for process {processWithArgs}", null);
                return false;
            }

            if (done)
            {
                verbose($"Process {processWithArgs} is flagged as done", null);
                return false;
            }

            bool canBeAlive = !task.CanBeAwaited();

            return canBeAlive;
        }
    }

    internal static class TaskExtensions
    {
        public static bool CanBeAwaited(this Task task)
        {
            if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
            {
                return true;
            }

            return false;
        }

        public static bool CanBeAwaited<T>(this Task<T> task)
        {
            if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
            {
                return true;
            }

            return false;
        }
    }
}
