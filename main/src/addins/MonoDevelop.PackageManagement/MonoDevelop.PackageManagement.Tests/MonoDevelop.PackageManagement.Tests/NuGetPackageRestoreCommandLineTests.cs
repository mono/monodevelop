//
// NuGetPackageRestoreCommandLineTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.PackageManagement;
using NUnit.Framework;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.PackageManagement.Tests.Helpers;
using System.IO;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class NuGetPackageRestoreCommandLineTests
	{
		NuGetPackageRestoreCommandLine commandLine;

		void CreateCommandLineWithSolution (string fileName)
		{
			CreateCommandLineWithSolution (fileName, null, false);
		}

		void CreateCommandLineWithSolution (string fileName, MonoRuntimeInfo monoRuntimeInfo)
		{
			CreateCommandLineWithSolution (fileName, monoRuntimeInfo, true);
		}

		void CreateCommandLineWithSolution (string fileName, MonoRuntimeInfo monoRuntimeInfo, bool isMonoRuntime)
		{
			var solution = new FakePackageManagementSolution ();
			solution.FileName = fileName;
			commandLine = new NuGetPackageRestoreCommandLine (
				solution,
				monoRuntimeInfo,
				isMonoRuntime);
		}

		[Test]
		public void Arguments_RestoreSolution_SolutionFullFileNameUsed ()
		{
			CreateCommandLineWithSolution (@"d:\projects\MySolution\MySolution.sln");

			string arguments = commandLine.Arguments;

			string expectedArguments = "restore -NonInteractive \"d:\\projects\\MySolution\\MySolution.sln\"";
			Assert.AreEqual (expectedArguments, arguments);
		}

		[Test]
		public void CommandLine_RestoreSolutionOnMono_MonoUsedFromCurrentPrefix ()
		{
			var monoRuntime = new MonoRuntimeInfo (@"c:\Users\Prefix");
			CreateCommandLineWithSolution (@"d:\projects\MySolution\MySolution.sln", monoRuntime);

			string arguments = commandLine.Arguments;

			string expectedCommandLine = Path.Combine (@"c:\Users\Prefix", "bin", "mono");
			Assert.IsTrue (arguments.StartsWith ("--runtime=v4.0 "), arguments);
			Assert.IsTrue (arguments.EndsWith ("restore -NonInteractive \"d:\\projects\\MySolution\\MySolution.sln\""), arguments);
			Assert.AreEqual (expectedCommandLine, commandLine.Command);
		}
	}
}