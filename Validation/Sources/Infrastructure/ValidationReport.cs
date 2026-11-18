using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LiteQuark.Editor
{
    [Serializable]
    internal sealed class ValidationCase
    {
        public string id;
        public bool passed;
        public int checks;
        public float seconds;
        public string status;
        public bool diagnostic;
        public string limitationId;
        public string failure;
        public List<string> evidence = new();
        public List<string> errors = new();

        public void Check(bool condition, string message)
        {
            checks++;
            if (!condition) throw new InvalidOperationException(message);
            evidence.Add(message);
        }
    }

    [Serializable]
    internal sealed class ValidationReport
    {
        public string unityVersion;
        public string finishedUtc;
        public string fatalError;
        public List<string> unexpectedErrors = new();
        public List<ValidationCase> cases = new();

        public void Save(string suite)
        {
            unityVersion = Application.unityVersion;
            finishedUtc = DateTime.UtcNow.ToString("O");
            foreach (var item in cases)
            {
                if (string.IsNullOrEmpty(item.status))
                    item.status = item.passed ? "passed" : "failed";
            }
            File.WriteAllText(Path.Combine(ValidationEnvironment.Reports(suite), "results.json"), JsonUtility.ToJson(this, true));
        }
    }

    internal abstract class EditorValidation
    {
        protected static readonly ValidationReport Results = new();
        protected static ValidationCase Active;

        protected static void Run(string id, Action scenario, params string[] expectedErrors)
        {
            Active = new ValidationCase { id = id };
            Results.cases.Add(Active);
            var start = Time.realtimeSinceStartup;
            void Capture(string message, string stack, LogType type)
            {
                if (type is LogType.Error or LogType.Exception or LogType.Assert) Active.errors.Add(message);
            }
            Application.logMessageReceived += Capture;
            try { scenario(); Active.passed = true; }
            catch (Exception ex) { Active.failure = ex.ToString(); }
            finally
            {
                Application.logMessageReceived -= Capture;
                var unexpected = Active.errors.Where(message => !expectedErrors.Any(message.Contains)).ToArray();
                if (unexpected.Length > 0)
                {
                    Active.passed = false;
                    Active.failure += "\nUnexpected errors: " + string.Join("\n", unexpected);
                }
                Active.seconds = Time.realtimeSinceStartup - start;
            }
        }

        protected static void Check(bool condition, string message) => Active.Check(condition, message);
    }
}
