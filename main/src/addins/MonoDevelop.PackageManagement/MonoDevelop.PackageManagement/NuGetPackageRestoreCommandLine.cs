// 
// NuGetPackageRestoreCommandLine.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using MonoDevelop.Core.Assemblies;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class NuGetPackageRestoreCommandLine
	{
		MonoRuntimeInfo monoRuntime;
		bool isMonoRuntime;

		public NuGetPackageRestoreCommandLine (IPackageManagementSolution solution)
			: this (
				solution,
				MonoRuntimeInfo.FromCurrentRuntime (),
				EnvironmentUtility.IsMonoRuntime)
		{
		}

		public NuGetPackageRestoreCommandLine (
			IPackageManagementSolution solution,
			MonoRuntimeInfo monoRuntime,
			bool isMonoRuntime)
		{
			this.monoRuntime = monoRuntime;
			this.isMonoRuntime = isMonoRuntime;

			GenerateCommandLine(solution);
			GenerateWorkingDirectory(solution);
		}
		
		public string Command { get; set; }
		public string Arguments { get; private set; }
		public string WorkingDirectory { get; private set; }
		
		void GenerateCommandLine(IPackageManagementSolution solution)
		{
			if (isMonoRuntime) {
				GenerateMonoCommandLine(solution);
			} else {
				GenerateWindowsCommandLine(solution);
			}
		}
		
		void GenerateMonoCommandLine(IPackageManagementSolution solution)
		{
			Arguments = String.Format(
				"--runtime=v4.0 \"{0}\" restore \"{1}\"",
				NuGetExePath.GetPath(),
				solution.FileName);

			Command = Path.Combine (monoRuntime.Prefix, "bin", "mono");
		}
		
		void GenerateWindowsCommandLine(IPackageManagementSolution solution)
		{
			Arguments = String.Format("restore \"{0}\"", solution.FileName);
			Command = NuGetExePath.GetPath();
		}
		
		void GenerateWorkingDirectory(IPackageManagementSolution solution)
		{
			WorkingDirectory = Path.GetDirectoryName(solution.FileName);
		}
		
		public override string ToString()
		{
			return String.Format("{0} {1}", GetQuotedCommand(), Arguments);
		}
		
		string GetQuotedCommand()
		{
			if (Command.Contains(" ")) {
				return String.Format("\"{0}\"", Command);
			}
			return Command;
		}
	}
}
