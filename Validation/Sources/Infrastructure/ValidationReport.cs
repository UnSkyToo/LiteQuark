using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
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

        internal void Fail(string message)
        {
            passed = false;
            status = "failed";
            failure = string.IsNullOrEmpty(failure) ? message : failure + "\n" + message;
        }
    }

    [Serializable]
    internal sealed class ValidationReport
    {
        internal const string SceneCollisionId = "UNITY-SAME-NAME-SCENE-BUNDLES";
        internal const string NativeCollisionCase = "loading/same-name-native";
        internal const string SceneCollisionCase = "loading/same-name-limit";

        public string unityVersion;
        public string finishedUtc;
        public string fatalError;
        public List<string> unexpectedErrors = new();
        public List<ValidationCase> cases = new();
        public ValidationSummary summary;

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

        internal void Finish(string suite)
        {
            EditorApplication.Exit(Complete(suite) ? 0 : 2);
        }

        internal bool Complete(string suite)
        {
            var known = cases.Where(item => item.status == "known_limitation").ToArray();
            var diagnostics = cases.Where(item => item.diagnostic).ToArray();
            var failures = cases.Where(item => !item.passed && item.status != "known_limitation").ToArray();
            summary = new ValidationSummary
            {
                Suite = suite,
                Cases = cases.Count,
                RegressionCases = cases.Count - known.Length - diagnostics.Length,
                Passed = cases.Count(item => item.passed && !item.diagnostic),
                Failed = failures.Length,
                KnownLimitations = known.Length,
                Diagnostics = diagnostics.Length,
                DiagnosticsPassed = diagnostics.Count(item => item.passed),
                Assertions = cases.Sum(item => item.checks),
                Failures = failures.Select(item => new ValidationSummary.FailedCase { Name = item.id, Failure = item.failure }).ToArray(),
                KnownLimitationDetails = known.Select(item => new ValidationSummary.Limitation
                {
                    Id = item.limitationId, Name = item.id, Reason = item.failure, Evidence = item.evidence
                }).ToArray(),
                DiagnosticDetails = diagnostics.Select(item => new ValidationSummary.Diagnostic
                {
                    Name = item.id, Confirmed = item.passed, Evidence = item.evidence, Failure = item.failure
                }).ToArray(),
            };
            summary.Error = CheckResult(suite, known, diagnostics);
            summary.Success = summary.Error == null;
            Save(suite);
            return summary.Success;
        }

        private string CheckResult(string suite, ValidationCase[] known, ValidationCase[] diagnostics)
        {
            string[] expected;
            try
            {
                expected = ValidationEnvironment.ExpectedScenarios(suite);
            }
            catch (Exception ex)
            {
                return "Cannot read the expected scenario IDs: " + ex.Message;
            }

            var actual = cases.Select(item => item.id).ToArray();
            if (expected.Length == 0 || expected.Distinct(StringComparer.Ordinal).Count() != expected.Length ||
                !expected.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(actual.OrderBy(id => id, StringComparer.Ordinal)))
                return $"Missing, duplicate or unexpected scenario ID. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].";

            if (suite == "loading")
            {
                if (known.Length != 1 || diagnostics.Length != 1 || diagnostics[0].id != NativeCollisionCase)
                    return "Missing or unexpected native diagnostic classification.";
                var item = known[0];
                if (item.passed || item.diagnostic || item.id != SceneCollisionCase || item.limitationId != SceneCollisionId ||
                    string.IsNullOrWhiteSpace(item.failure) || item.evidence.Count == 0 || item.errors.Count == 0 ||
                    !diagnostics[0].passed || diagnostics[0].status != "diagnostic_passed")
                    return "Unconfirmed or unexpected known limitation; counted as a validation failure.";
            }
            else if (known.Length != 0 || diagnostics.Length != 0)
            {
                return "Unexpected limitation or diagnostic outside loading.";
            }

            var harnessCleanup = cases.Any(item => item.evidence.Any(text => text.StartsWith("Harness-only cleanup", StringComparison.Ordinal)));
            if (summary.Passed + summary.KnownLimitations + summary.DiagnosticsPassed != expected.Length || summary.Failed != 0 ||
                !string.IsNullOrEmpty(fatalError) || unexpectedErrors.Any(error => !string.IsNullOrEmpty(error)) || harnessCleanup)
                return $"Validation failed. Expected {expected.Length} cases; fatal error: {fatalError}; harness cleanup: {harnessCleanup}.";
            return null;
        }
    }

    [Serializable]
    internal sealed class ValidationSummary
    {
        public string Suite;
        public int Passed;
        public int Failed;
        public int Cases;
        public int RegressionCases;
        public int KnownLimitations;
        public int Diagnostics;
        public int DiagnosticsPassed;
        public int Assertions;
        public bool Success;
        public string Error;
        public FailedCase[] Failures;
        public Limitation[] KnownLimitationDetails;
        public Diagnostic[] DiagnosticDetails;

        [Serializable]
        internal sealed class FailedCase
        {
            public string Name;
            public string Failure;
        }

        [Serializable]
        internal sealed class Limitation
        {
            public string Id;
            public string Name;
            public string Reason;
            public List<string> Evidence;
        }

        [Serializable]
        internal sealed class Diagnostic
        {
            public string Name;
            public bool Confirmed;
            public List<string> Evidence;
            public string Failure;
        }
    }

    internal abstract class EditorValidation
    {
        protected static ValidationReport Results { get; private set; } = new();
        protected static ValidationCase Active;

        internal static bool RunSuite(string suite, Action runCases)
        {
            Results = new ValidationReport();
            try
            {
                runCases();
            }
            catch (Exception ex)
            {
                Results.fatalError = ex.ToString();
            }
            finally
            {
                Active = null;
            }
            return Results.Complete(suite);
        }

        protected static void Run(string id, Action scenario, params string[] expectedErrors)
        {
            using var execution = new ValidationCaseExecution(Results, id);
            Active = execution.Result;
            execution.Run(scenario);
            execution.RejectUnexpectedErrors(expectedErrors);
        }

        protected static void Check(bool condition, string message) => Active.Check(condition, message);
    }
}
