//
// MSBuildProjectServiceTests.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects.MSBuild;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class MSBuildProjectServiceTests
	{
		[TestCase ("a(b)", "a%28b%29")]
		[TestCase ("a%bcd", "a%25bcd")]
		[TestCase ("%a%b%c%d%", "%25a%25b%25c%25d%25")]
		[TestCase ("%%%", "%25%25%25")]
		[TestCase ("abc", "abc")]
		public void EscapeString (string input, string expected)
		{
			Assert.AreEqual (expected, MSBuildProjectService.EscapeString (input));
		}

		[TestCase ("a%28b%29", "a(b)")]
		[TestCase ("a%25bcd", "a%bcd")]
		[TestCase ("%25a%25b%25c%25d%25", "%a%b%c%d%")]
		[TestCase ("%25%25%25", "%%%")]
		[TestCase ("abc", "abc")]
		public void UnescapeString (string input, string expected)
		{
			Assert.AreEqual (expected, MSBuildProjectService.UnscapeString (input));
		}

		[Test]
		public void SearchImportPath_MultiplePathValues ()
		{
			if (!Platform.IsMac)
				Assert.Ignore ("Only Mac supported.");

			FilePath msbuildConfigDir = Util.GetSampleProjectPath ("msbuild-search-paths");
			var msbuildConfigFile = msbuildConfigDir.Combine ("MSBuild.dll.config");
			var runtime = new TestMSBuildBinPathTargetRuntime ();
			runtime.MSBuildBinPath = msbuildConfigDir;
			var imports = MSBuildProjectService.GetProjectImportSearchPaths (runtime, includeImplicitImports: true).ToList ();

			var firstImport = imports [0];
			var secondImport = imports [1];

			Assert.AreEqual ("$(MSBuildExtensionsPath)", firstImport.MSBuildProperty);
			Assert.AreEqual ("$(MSBuildExtensionsPathFallbackPathsOverride)", firstImport.Path);
			Assert.AreEqual ("$(MSBuildExtensionsPath)", secondImport.MSBuildProperty);
			Assert.AreEqual ("/Library/Frameworks/Mono.framework/External/xbuild/", secondImport.Path);
		}
	}

	class TestMSBuildBinPathTargetRuntime : TargetRuntime
	{
		public override string RuntimeId => throw new NotImplementedException ();
		public override string Version => throw new NotImplementedException ();
		public override bool IsRunning => throw new NotImplementedException ();

		[Obsolete]
		public override string GetAssemblyDebugInfoFile (string assemblyPath)
		{
			throw new NotImplementedException ();
		}

		public override IExecutionHandler GetExecutionHandler ()
		{
			throw new NotImplementedException ();
		}

		public string MSBuildBinPath { get; set; }

		public override string GetMSBuildBinPath (string toolsVersion)
		{
			return MSBuildBinPath;
		}

		public override string GetMSBuildExtensionsPath ()
		{
			throw new NotImplementedException ();
		}

		public override string GetMSBuildToolsPath (string toolsVersion)
		{
			throw new NotImplementedException ();
		}

		protected override void OnInitialize ()
		{
		}

		protected internal override IEnumerable<string> GetGacDirectories ()
		{
			throw new NotImplementedException ();
		}
	}
}
