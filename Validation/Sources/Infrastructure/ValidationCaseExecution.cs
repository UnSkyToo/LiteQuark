using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed class ValidationCaseExecution : IDisposable
    {
        private readonly float _started = Time.realtimeSinceStartup;
        private readonly Action<ValidationCase, string, string> _recordError;

        internal ValidationCase Result { get; }

        internal ValidationCaseExecution(ValidationReport report, string id, Action<ValidationCase, string, string> recordError = null)
        {
            Result = new ValidationCase { id = id };
            report.cases.Add(Result);
            _recordError = recordError;
            Application.logMessageReceived += Capture;
        }

        internal void Run(Action scenario)
        {
            try
            {
                scenario();
                Result.passed = string.IsNullOrEmpty(Result.failure);
            }
            catch (Exception ex)
            {
                Result.Fail(ex.ToString());
            }
        }

        internal async UniTask RunAsync(Func<UniTask> scenario, Func<UniTask> cleanup, Action<Exception> classifyFailure = null)
        {
            try
            {
                await scenario();
                Result.passed = string.IsNullOrEmpty(Result.failure);
            }
            catch (Exception ex)
            {
                Result.Fail(ex.ToString());
                classifyFailure?.Invoke(ex);
            }
            finally
            {
                try { await cleanup(); }
                catch (Exception ex) { Result.Fail("Cleanup: " + ex); }
            }
        }

        internal void RejectUnexpectedErrors(string[] expectedErrors) =>
            RejectUnexpectedErrors(message => expectedErrors.Any(message.Contains));

        internal void RejectUnexpectedErrors(Func<string, bool> isExpectedError = null)
        {
            var unexpected = Result.errors.Where(message => isExpectedError == null || !isExpectedError(message)).ToArray();
            if (unexpected.Length > 0)
                Result.Fail("Unexpected errors: " + string.Join("\n", unexpected));
        }

        internal static bool IsError(LogType type) => type is LogType.Error or LogType.Exception or LogType.Assert;

        private void Capture(string message, string stack, LogType type)
        {
            if (!IsError(type)) return;
            if (_recordError == null) Result.errors.Add(message);
            else _recordError(Result, message, stack);
        }

        public void Dispose()
        {
            Application.logMessageReceived -= Capture;
            Result.seconds = Time.realtimeSinceStartup - _started;
        }
    }
}
