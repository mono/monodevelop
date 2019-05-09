using System;
using System.IO;
using System.Linq;
using System.Diagnostics;

using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Execution;
using MonoDevelop.Debugger;

using NUnit.Framework;

using MDTextFile = MonoDevelop.Projects.Text.TextFile;

namespace Mono.Debugging.Tests
{
	public abstract partial class DebugTests
	{
		static bool testProjectReady;

		DebuggerEngine engine;
		TargetRuntime runtime;

		protected virtual string TestAppProjectDirName {
			get { return "MonoDevelop.Debugger.Tests.TestApp"; }
		}

		protected virtual string TestAppExeName {
			get { return TestAppProjectDirName + ".exe"; }
		}

		partial void SetUpPartial ()
		{
			engine = DebuggingService.GetDebuggerEngines ().FirstOrDefault (e => e.Id == EngineId);

			if (engine == null)
				Assert.Ignore ("Engine not found: {0}", EngineId);

			if (!testProjectReady) {
				testProjectReady = true;
				var packagesConfig = Path.Combine (TargetProjectSourceDir, "packages.config");
				var packagesDir = Path.Combine (TargetProjectSourceDir, "packages");

				if (File.Exists (packagesConfig)) {
					Process.Start ("nuget", $"restore \"{packagesConfig}\" -PackagesDirectory \"{packagesDir}\"").WaitForExit ();
				} else {
					var projFile = Path.Combine (TargetProjectSourceDir, TestAppProjectDirName + ".csproj");

					Process.Start ("nuget", $"restore \"{projFile}\" -PackagesDirectory \"{packagesDir}\"").WaitForExit ();
				}

				Process.Start ("msbuild", "\"" + TargetProjectSourceDir + "\"").WaitForExit ();
			}
		}

		partial void TearDownPartial ()
		{
		}

		protected string TargetExeDirectory {
			get {
				return Path.Combine (TargetProjectSourceDir, "bin", "Debug");
			}
		}

		protected string TargetProjectSourceDir {
			get {
				var path = Path.GetDirectoryName (GetType ().Assembly.Location);

				return Path.Combine (path, "DebuggerTestProjects", TestAppProjectDirName);
			}
		}

		protected virtual DebuggerSession CreateSession (string test, string engineId)
		{
			switch (engineId) {
				case "MonoDevelop.Debugger.Win32":
					runtime = Runtime.SystemAssemblyService.GetTargetRuntime ("MS.NET");
					break;
				case "Mono.Debugger.Soft":
					runtime = Runtime.SystemAssemblyService.GetTargetRuntimes ()
						.OfType<MonoTargetRuntime> ()
						.OrderByDescending ((o) => {
							//Attempt to find latest version of Mono registred in IDE and use that for unit tests
							if (string.IsNullOrWhiteSpace (o.Version) || o.Version == "Unknown")
								return new Version (0, 0, 0, 0);

							int indexOfBeforeDetails = o.Version.IndexOf (" (", StringComparison.Ordinal);

							string hopefullyVersion;
							if (indexOfBeforeDetails != -1)
								hopefullyVersion = o.Version.Remove (indexOfBeforeDetails);
							else
								hopefullyVersion = o.Version;

							if (Version.TryParse (hopefullyVersion, out var version))
								return version;

							return new Version (0, 0, 0, 0);
						}).FirstOrDefault ();
					break;
				default:
					runtime = Runtime.SystemAssemblyService.DefaultRuntime;
					break;
			}

			if (runtime == null)
				Assert.Ignore ("Runtime not found for: {0}", engineId);

			Console.WriteLine ("Target Runtime: " + runtime.DisplayRuntimeName + " " + runtime.Version + " " + (IntPtr.Size == 8 ? "64bit" : "32bit"));

			// main/build/tests
			var exe = TargetExePath;

			var cmd = new DotNetExecutionCommand ();
			cmd.TargetRuntime = runtime;
			cmd.Command = exe;
			cmd.Arguments = test;

			if (Platform.IsWindows) {
				var monoRuntime = runtime as MonoTargetRuntime;
				if (monoRuntime != null) {
					var psi = new ProcessStartInfo (Path.Combine (monoRuntime.Prefix, "bin", "pdb2mdb.bat"), exe);
					psi.UseShellExecute = false;
					psi.CreateNoWindow = true;
					Process.Start (psi).WaitForExit ();
				}
			}

			return engine.CreateSession ();
		}

		protected DebuggerStartInfo CreateStartInfo (string test, string engineId)
		{
			var cmd = new DotNetExecutionCommand {
				TargetRuntime = runtime,
				Command = TargetExePath,
				Arguments = test
			};
			var dsi = engine.CreateDebuggerStartInfo (cmd);
			return dsi;
		}

		/// <summary>
		/// Reads file from given path
		/// </summary>
		/// <param name="sourcePath"></param>
		/// <returns></returns>
		public static ITextFile ReadFile (string sourcePath)
		{
			return new TextFile (MDTextFile.ReadFile (sourcePath));
		}
	}
}
