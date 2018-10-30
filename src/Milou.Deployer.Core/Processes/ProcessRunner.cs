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
    public sealed class ProcessRunner : IDisposable
    {
        public static async Task<ExitCode> ExecuteProcessAsync(
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
            ExitCode exitCode;
            Stopwatch processStopWatch = Stopwatch.StartNew();

            string[] args = arguments?.ToArray() ?? Array.Empty<string>();

            using (var runner = new ProcessRunner())
            {
                exitCode = await runner.ExecuteAsync(executePath,
                    args,
                    standardOutLog,
                    standardErrorAction,
                    toolAction,
                    verboseAction,
                    environmentVariables,
                    debugAction,
                    cancellationToken).ConfigureAwait(false);

                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken)
                    .ConfigureAwait(false);
            }

            processStopWatch.Stop();
            string processWithArgs = $"\"{executePath}\" {string.Join(" ", args.Select(arg => $"\"{arg}\""))}";
            toolAction?.Invoke($"Running process {processWithArgs} took {processStopWatch.Elapsed.TotalMilliseconds:F1}", null);

            return exitCode;
        }

        private Action<string, string> _debugAction;
        private bool _disposed;
        private bool _disposing;

        private ExitCode? _exitCode;
        private Process _process;
        private Action<string, string> _standardErrorAction;

        private Action<string, string> _standardOutLog;
        private TaskCompletionSource<ExitCode> _taskCompletionSource;
        private Action<string, string> _toolAction;
        private Action<string, string> _verboseAction;

        private ProcessRunner()
        {
            _process = new Process();
            _taskCompletionSource = new TaskCompletionSource<ExitCode>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void Dispose()
        {
            if (!_disposed && !_disposing)
            {
                _disposing = true;

                if (_taskCompletionSource?.Task.CanBeAwaited() == false)
                {
                    _standardErrorAction?.Invoke("Task completion was not set on dispose, setting to failure", null);
                    _taskCompletionSource.TrySetResult(ExitCode.Failure);
                }

                _taskCompletionSource = null;

                if (_process != null)
                {
                    _process.Dispose();
                    _process.Disposed -= OnDisposed;
                    _process.Exited -= OnExited;
                }

                _disposed = true;
                _disposing = false;
            }

            _process = null;
        }

        private Task<ExitCode> ExecuteAsync(
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
            ThrowIfDisposed();
            ThrowIfDisposing();

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

           return task;
        }

        private bool IsAlive(
            string processWithArgs,
            CancellationToken cancellationToken)
        {
            if (CheckedDisposed())
            {
                _verboseAction?.Invoke($"Process {processWithArgs} does no longer exist", null);
                return false;
            }

            if (_taskCompletionSource.Task.IsCompleted)
            {
                return false;
            }

            _process?.Refresh();

            try
            {
                if (_process?.HasExited == true)
                {
                    return false;
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                //ignore
            }

            if (_taskCompletionSource.Task.CanBeAwaited())
            {
                TaskStatus status = _taskCompletionSource.Task.Status;
                _verboseAction?.Invoke($"Task status for process {processWithArgs} is {status}", null);
                return false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _verboseAction?.Invoke($"Cancellation is requested for process {processWithArgs}", null);
                return false;
            }

            if (_exitCode.HasValue)
            {
                _verboseAction?.Invoke($"Process {processWithArgs} is flagged as done", null);
                return false;
            }

            bool canBeAlive = !_taskCompletionSource.Task.CanBeAwaited();

            return canBeAlive;
        }

        private bool CheckedDisposed()
        {
            return _disposed || _disposing;
        }

        private async Task<ExitCode> RunProcessAsync(
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
            ThrowIfDisposed();
            ThrowIfDisposing();

            cancellationToken.Register(EnsureTaskIsCompleted);

            _toolAction = toolAction;
            _standardOutLog = standardOutputLog;
            _standardErrorAction = standardErrorAction;
            _verboseAction = verboseAction;
            _debugAction = debugAction;

            string processWithArgs = $"\"{executePath}\" {formattedArguments}".Trim();

            _toolAction?.Invoke($"[{typeof(ProcessRunner).Name}] Executing: {processWithArgs}", null);

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

            _process.StartInfo = processStartInfo;
            _process.Exited += OnExited;
            _process.Disposed += OnDisposed;

            if (redirectStandardError)
            {
                _process.ErrorDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        _standardErrorAction?.Invoke(args.Data, null);
                    }
                };
            }

            if (redirectStandardOutput && _standardOutLog != null)
            {
                _process.OutputDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        _standardOutLog(args.Data, null);
                    }
                };
            }

            _process.EnableRaisingEvents = true;

            int processId = -1;

            try
            {
                bool started = _process.Start();

                if (!started)
                {
                    _standardErrorAction?.Invoke($"Process '{processWithArgs}' could not be started", null);

                    SetFailureResult();

                    return await _taskCompletionSource.Task.ConfigureAwait(false);
                }

                if (redirectStandardError)
                {
                    _process.BeginErrorReadLine();
                }

                if (redirectStandardOutput)
                {
                    _process.BeginOutputReadLine();
                }

                int bits = _process.IsWin64() ? 64 : 32;

                try
                {
                    processId = _process.Id;
                }
                catch (InvalidOperationException ex)
                {
                    _debugAction?.Invoke($"Could not get process id for process '{processWithArgs}'. {ex}", null);
                }

                string temp = _process.HasExited ? "was" : "is";

                _verboseAction?.Invoke(
                    $"The process '{processWithArgs}' {temp} running in {bits}-bit mode",
                    null);
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }

                _standardErrorAction?.Invoke($"An error occured while running process {processWithArgs}: {ex}", null);
                SetResultException(ex);
            }

            if (_taskCompletionSource.Task.CanBeAwaited())
            {
                return await _taskCompletionSource.Task.ConfigureAwait(false);
            }

            try
            {
                while (IsAlive(processWithArgs, cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    Task delay = Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);

                    await delay.ConfigureAwait(false);

                    if (_taskCompletionSource.Task.IsCompleted)
                    {
                        _exitCode = await _taskCompletionSource.Task.ConfigureAwait(false);
                    }
                    else if (_taskCompletionSource.Task.IsCanceled)
                    {
                        _exitCode = await _taskCompletionSource.Task.ConfigureAwait(false);
                    }
                    else if (_taskCompletionSource.Task.IsFaulted)
                    {
                        _exitCode = await _taskCompletionSource.Task.ConfigureAwait(false);
                    }
                    else
                    {
                        _exitCode = ExitCode.Failure;
                    }
                }
            }
            finally
            {
                if (_exitCode?.IsSuccess is null || !_exitCode.Value.IsSuccess)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _process?.Refresh();

                        if (_process?.HasExited == false)
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

                                    _standardErrorAction?.Invoke(
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
                            catch (Exception ex) when (!ex.IsFatal())
                            {
                                _standardErrorAction?.Invoke(
                                    $"ProcessRunner could not kill process {processWithArgs} when cancellation was requested",
                                    null);
                                _standardErrorAction?.Invoke(
                                    $"Could not kill process {processWithArgs} when cancellation was requested",
                                    null);
                                _standardErrorAction?.Invoke(ex.ToString(), null);
                            }
                        }
                    }
                }

                if (_process != null)
                {
                    using (_process)
                    {
                        _verboseAction?.Invoke(
                            $"Task status: {_taskCompletionSource.Task.Status}, {_taskCompletionSource.Task.IsCompleted}",
                            null);
                        _verboseAction?.Invoke($"Disposing process {processWithArgs}", null);
                    }
                }
            }

            _verboseAction?.Invoke($"Process runner exit code {_exitCode} for process {processWithArgs}", null);

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
                        _verboseAction?.Invoke(
                            $"The process with ID {processId.ToString(CultureInfo.InvariantCulture)} '{processWithArgs}' is still running",
                            null);
                        SetFailureResult();

                        return await _taskCompletionSource.Task.ConfigureAwait(false);
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

            return await _taskCompletionSource.Task.ConfigureAwait(false);
        }

        private void EnsureTaskIsCompleted()
        {
            if (!CheckedDisposed() && _taskCompletionSource?.Task.CanBeAwaited() == false)
            {
                _taskCompletionSource.TrySetCanceled();
            }
        }

        private void OnExited(object sender, EventArgs e)
        {
            if (!(sender is Process proc))
            {
                if (!_taskCompletionSource.Task.CanBeAwaited())
                {
                    _standardErrorAction?.Invoke("Task is not in a valid state, sender is not process", null);
                    SetFailureResult();
                }

                return;
            }

            if (!IsAlive("", CancellationToken.None))
            {
                return;
            }

            proc.EnableRaisingEvents = false;

            if (_taskCompletionSource.Task.CanBeAwaited())
            {
                return;
            }

            proc.Refresh();
            int procExitCode;
            try
            {
                if (_taskCompletionSource.Task.CanBeAwaited())
                {
                    return;
                }

                procExitCode = proc.ExitCode;
            }
            catch (Exception ex)
            {
                _standardErrorAction?.Invoke($"Failed to get exit code from process {ex}", null);

                SetResultException(ex);

                return;
            }

            var result = new ExitCode(procExitCode);
            _toolAction?.Invoke($"Process '{_process.StartInfo.Arguments}' exited with code {result}", null);

            if (!_taskCompletionSource.Task.CanBeAwaited())
            {
                SetSuccessResult(result);
            }
        }

        private void SetSuccessResult(ExitCode result)
        {
            ThrowIfDisposed();
            ThrowIfDisposing();

            if (_taskCompletionSource.Task.CanBeAwaited())
            {
                if (_taskCompletionSource.Task.IsCompleted && _taskCompletionSource.Task.Result != result)
                {
                    _toolAction.Invoke(
                        $"Task result has already been set to {_taskCompletionSource.Task.Status}, cannot re-set to exit code to {result}",
                        null);
                }
            }

            _taskCompletionSource.TrySetResult(result);
        }

        private void SetResultException(Exception ex)
        {
            ThrowIfDisposed();
            ThrowIfDisposing();

            if (_taskCompletionSource.Task.CanBeAwaited())
            {
                throw new InvalidOperationException(
                    $"Task result has already been set to {_taskCompletionSource.Task.Status}, cannot re-set to with exception",
                    ex);
            }

            _taskCompletionSource.TrySetException(ex);
        }

        private void SetFailureResult()
        {
            ThrowIfDisposed();
            ThrowIfDisposing();

            if (_taskCompletionSource.Task.CanBeAwaited())
            {
                throw new InvalidOperationException(
                    $"Task result has already been set to {_taskCompletionSource.Task.Status}, cannot re-set to exit code to {ExitCode.Failure}");
            }

            _taskCompletionSource.TrySetResult(ExitCode.Failure);
        }

        private void OnDisposed(object sender, EventArgs _)
        {
            if (!_taskCompletionSource.Task.CanBeAwaited())
            {
                _verboseAction?.Invoke("Task was not completed, but process was disposed", null);
                SetFailureResult();
            }

            _verboseAction?.Invoke("Disposed process", null);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ProcessRunner));
            }
        }

        private void ThrowIfDisposing()
        {
            if (_disposed)
            {
                throw new InvalidOperationException("Disposing in progress");
            }
        }
    }
}
