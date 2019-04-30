//
// TemplateDependencyChecker.cs
//
// Author:
//       Ian Toal <iantoal@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Diagnostics;

namespace MonoDevelop.DotNetCore.Tests
{
	[Flags]
	public enum TemplateDependency
	{
		None,
		Npm = 1,
		Node = 2
	}

	public static class TemplateDependencyChecker
	{
		static bool isNpmInstalled;
		static bool isNodeInstalled;

		static TemplateDependencyChecker ()
		{
			CheckNpmIsInstalled ();
			CheckNodeIsInstalled ();
		}

		static void CheckNpmIsInstalled ()
		{
			isNpmInstalled = CheckProcessDependency ("npm", "-v");
		}

		static void CheckNodeIsInstalled ()
		{
			isNodeInstalled = CheckProcessDependency ("node", "-v");
		}

		public static bool Check (TemplateDependency dependencies)
		{
			if ((dependencies & TemplateDependency.Npm) == TemplateDependency.Npm &&
				!isNpmInstalled)
				return false;

			if ((dependencies & TemplateDependency.Node) == TemplateDependency.Node &&
				!isNodeInstalled)
				return false;

			return true;
		}

		static bool CheckProcessDependency (string fileName, string arguments)
		{
			bool processStarted = true;

			try {
				using (var process = new Process ()) {
					var startInfo = new ProcessStartInfo (fileName, arguments) {
						RedirectStandardOutput = true,
						UseShellExecute = false
					};

					process.StartInfo = startInfo;
					process.Start ();
					process.WaitForExit (2000);
					processStarted = process.ExitCode == 0;
				}
			}
			catch {
				processStarted = false;
			}

			return processStarted;
		}
	}
}
