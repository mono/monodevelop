using System;
using System.IO;
using System.Linq;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Execution;
using MonoDevelop.Debugger;
using NUnit.Framework;

using MDTextFile = MonoDevelop.Projects.Text.TextFile;
using System.Diagnostics;

namespace Mono.Debugging.Tests
{
	public abstract partial class DebugTests
	{
		DebuggerEngine engine;
		TargetRuntime runtime;

		static bool testProjectReady;

		partial void SetUpPartial()
		{
			foreach (var e in DebuggingService.GetDebuggerEngines ()) {
				if (e.Id == EngineId) {
					engine = e;
					break;
				}
			}
			if (engine == null)
				Assert.Ignore ("Engine not found: {0}", EngineId);

			if (!testProjectReady) {
				testProjectReady = true;
				var packagesConfig = Path.Combine (TargetProjectSourceDir, "packages.config");
				var packagesDir = Path.Combine (TargetProjectSourceDir, "packages");
				Process.Start ("nuget", $"restore \"{packagesConfig}\" -PackagesDirectory \"{packagesDir}\"").WaitForExit ();
				Process.Start ("msbuild", "\"" + TargetProjectSourceDir + "\"").WaitForExit ();
			}
		}

		partial void TearDownPartial ()
		{
		}

		protected string TargetExeDirectory
		{
			get {
				FilePath path = TargetProjectSourceDir;
				return path.Combine ("bin", "Debug");
			}
		}

		protected string TargetProjectSourceDir
		{
			get {
				FilePath path = Path.GetDirectoryName (GetType ().Assembly.Location);
				return path.Combine ("DebuggerTestProjects", TestAppProjectDirName);
			}
		}

		protected DebuggerSession CreateSession (string test, string engineId)
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
							Version version;
							if (Version.TryParse (hopefullyVersion, out version)) {
								return version;
							} else {
								return new Version (0, 0, 0, 0);
							}
						}).FirstOrDefault ();
					break;
				default:
					runtime = Runtime.SystemAssemblyService.DefaultRuntime;
					break;
			}

			if (runtime == null) {
				Assert.Ignore ("Runtime not found for: {0}", engineId);
			}

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
					var psi = new System.Diagnostics.ProcessStartInfo (Path.Combine (monoRuntime.Prefix, "bin", "pdb2mdb.bat"), exe);
					psi.UseShellExecute = false;
					psi.CreateNoWindow = true;
					System.Diagnostics.Process.Start (psi).WaitForExit ();
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
			return new TextFile(MDTextFile.ReadFile (sourcePath));
		}
	}
}