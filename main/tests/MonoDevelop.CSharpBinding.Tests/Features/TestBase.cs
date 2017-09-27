//
// TestBase.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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

using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp;
using System.Text;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis;
using System.IO;
using System;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Core.Assemblies;

namespace ICSharpCode.NRefactory6
{
	class TestBase
	{
		static bool firstRun = true;

		[TestFixtureSetUp]
		public void Simulate ()
		{
			if (firstRun) {
				string rootDir = Path.Combine (Util.TestsRootDir, "config");
				try {
					firstRun = false;
					InternalSetup (rootDir);
				} catch (Exception) {
					// if we encounter an error, try to re create the configuration directory
					// (This takes much time, therfore it's only done when initialization fails)
					try {
						if (Directory.Exists (rootDir))
							Directory.Delete (rootDir, true);
						InternalSetup (rootDir);
					} catch (Exception e) {
						Console.WriteLine (e);
					}
				}
			}
		}

		protected virtual void InternalSetup (string rootDir)
		{
			Xwt.Application.Initialize (Xwt.ToolkitType.Gtk);
			Util.ClearTmpDir ();
			Environment.SetEnvironmentVariable ("MONO_ADDINS_REGISTRY", rootDir);
			Environment.SetEnvironmentVariable ("XDG_CONFIG_HOME", rootDir);
			Runtime.Initialize (true);
			DesktopService.Initialize ();

			global::MonoDevelop.Projects.Services.ProjectService.DefaultTargetFramework
				= Runtime.SystemAssemblyService.GetTargetFramework (TargetFrameworkMoniker.NET_4_0);
		}

	}
	
}
