// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Interactive
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using CommandLine;
    using Snoop.Data;
    using Snoop.Infrastructure;
    using Snoop.InjectorLauncher;

    /// <summary>
    /// Class responsible for launching a new injector process.
    /// </summary>
    public static class InjectorLauncherManager
    {
        public static void Launch(
            int processId,
            MethodInfo methodInfo)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            Process process = Process.GetProcessById(processId);
            if (process is null)
            {
                throw new ArgumentException($"Could not find process with id '{processId}'", nameof(processId));
            }

            IntPtr windowHwnd = NativeMethods.GetRootWindowsOfProcess(processId).FirstOrDefault();
            Launch(process, windowHwnd, methodInfo, null);
        }

        public static void Launch(
            Process process,
            IntPtr targetHwnd,
            MethodInfo methodInfo,
            TransientSettingsData transientSettingsData = null)
        {
            if (process is null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            Architecture architecture = process.GetArchitecture();
            bool isProcessElevated = process.IsElevated();

            Launch(process.Id,
                   isProcessElevated,
                   architecture,
                   targetHwnd,
                   methodInfo.DeclaringType.Assembly.Location,
                   methodInfo.DeclaringType.FullName,
                   methodInfo.Name,
                   transientSettingsData?.WriteToFile());
        }

        public static void Launch(
            int processId,
            bool isProcessElevated,
            Architecture architecture,
            IntPtr targetHwnd,
            MethodInfo methodInfo,
            TransientSettingsData transientSettingsData = null)
        {
            Launch(processId, isProcessElevated, architecture, targetHwnd, methodInfo.DeclaringType.Assembly.GetName().Name, methodInfo.DeclaringType.FullName, methodInfo.Name, transientSettingsData.WriteToFile());
        }

        public static void Launch(
            int processId,
            bool isProcessElevated,
            Architecture architecture,
            IntPtr targetHwnd,
            string assembly,
            string className,
            string methodName,
            string transientSettingsFile = null,
            bool debug = false)
        {
            if (architecture == Architecture.Unknown)
            {
                throw new ArgumentException("Architecture must be specified", nameof(architecture));
            }

            if (transientSettingsFile != null && File.Exists(transientSettingsFile) == false)
            {
                throw new FileNotFoundException("The generated temporary settings file could not be found.", transientSettingsFile);
            }

            try
            {
                var location = Assembly.GetExecutingAssembly().Location;
                var directory = Path.GetDirectoryName(location) ?? string.Empty;
                //directory = Path.GetFullPath(Path.Combine(directory, "../../build"));
                var injectorLauncherExe = Path.Combine(directory, $"Snoop.InjectorLauncher.{architecture.GetSuffix()}.exe");

                if (File.Exists(injectorLauncherExe) == false)
                {
                    var message = @$"Could not find the injector launcher ""{injectorLauncherExe}"".
Snoop requires this component, which is part of the Snoop project, to do it's job.
- If you compiled snoop yourself, you should compile all required components.
- If you downloaded snoop you should not omit any files contained in the archive you downloaded and make sure that no anti virus deleted the file.";
                    throw new FileNotFoundException(message, injectorLauncherExe);
                }

                var injectorLauncherCommandLineOptions = new InjectorLauncherCommandLineOptions
                {
                    TargetPID = processId,
                    TargetHwnd = targetHwnd.ToInt32(),
                    Assembly = assembly,
                    ClassName = className,
                    MethodName = methodName,
                    SettingsFile = transientSettingsFile,
                    Debug = debug
                };

                var commandLine = Parser.Default.FormatCommandLine(injectorLauncherCommandLineOptions);
                var startInfo = new ProcessStartInfo(injectorLauncherExe, commandLine)
                {
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Verb = isProcessElevated ? "runas" : null
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();
            }
            finally
            {
                if (transientSettingsFile != null)
                {
                    File.Delete(transientSettingsFile);
                }
            }
        }
    }
}