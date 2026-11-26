using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal static class EditModeValidationRunner
    {
        public static void Begin()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-validationSuites");
            if (index < 0 || index + 1 >= args.Length)
                throw new ArgumentException("Missing -validationSuites for Edit Mode validation.");

            var suites = args[index + 1].Split(',');
            if (suites.Any(suite => suite != "paths" && suite != "manifest") || suites.Distinct().Count() != suites.Length)
                throw new ArgumentException("Edit Mode validation requires distinct paths or manifest suites.");

            var success = true;
            foreach (var suite in suites)
            {
                Debug.Log("Validation Edit Mode suite: " + suite);
                Action runCases = suite == "paths" ? PathValidationRunner.RunCases : ManifestValidationRunner.RunCases;
                success &= EditorValidation.RunSuite(suite, runCases);
            }
            EditorApplication.Exit(success ? 0 : 2);
        }
    }
}
