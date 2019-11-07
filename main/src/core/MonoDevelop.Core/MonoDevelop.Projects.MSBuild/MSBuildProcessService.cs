//
// MSBuildProcessService.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Projects.MSBuild
{
	public static class MSBuildProcessService
	{
		/// <summary>
		/// Allows the MSBuild process start to be intercepted and monitored.
		/// </summary>
		public static Func<string, string, string, TextWriter, TextWriter, EventHandler, ProcessWrapper>
			StartProcessHandler { get; set; } = Runtime.ProcessService.StartProcess;

		public static ProcessWrapper StartMSBuild (string arguments, string workingDirectory, TextWriter outWriter, TextWriter errorWriter, EventHandler exited)
		{
			FilePath msbuildBinPath = GetMSBuildBinPath ();
			string command = null;

			if (Platform.IsWindows) {
				command = msbuildBinPath;
			} else {
				command = GetMonoPath ();
				var argumentsBuilder = new ProcessArgumentBuilder ();
				argumentsBuilder.AddQuoted (msbuildBinPath);
				argumentsBuilder.Add (arguments);
				arguments = argumentsBuilder.ToString ();
			}

			return StartProcessHandler (
				command,
				arguments,
				workingDirectory,
				outWriter,
				errorWriter,
				exited);
		}

		public static FilePath GetMSBuildBinDirectory ()
		{
			return MSBuildProjectService.GetMSBuildBinPath (Runtime.SystemAssemblyService.CurrentRuntime);
		}

		static FilePath GetMSBuildBinDirectory (TargetRuntime runtime)
		{
			return MSBuildProjectService.GetMSBuildBinPath (runtime);
		}

		static FilePath GetMSBuildBinPath ()
		{
			return GetMSBuildBinPath (Runtime.SystemAssemblyService.CurrentRuntime);
		}

		internal static FilePath GetMSBuildBinPath (TargetRuntime runtime)
		{
			FilePath binDirectory = GetMSBuildBinDirectory (runtime);
			FilePath binPath = binDirectory.Combine ("MSBuild.dll");
			if (File.Exists (binPath)) {
				return binPath;
			}

			return binDirectory.Combine ("MSBuild.exe");
		}

		static string GetMonoPath ()
		{
			var monoRuntime = Runtime.SystemAssemblyService.DefaultRuntime as MonoTargetRuntime;
			return Path.Combine (monoRuntime.MonoRuntimeInfo.Prefix, "bin", "mono64");
		}
	}
}
