//
// MultiTargetProjectTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
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

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MonoDevelop.Core;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class MultiTargetProjectTests : TestBase
	{
		/// <summary>
		/// Need to keep the original short name formats for the target frameworks. These are used when running MSBuild
		/// targets so the conditions are correctly evaluated.
		/// </summary>
		[Test]
		public async Task TargetFrameworkMonikers_DifferentShortNameFormats ()
		{
			FilePath projectFile = Util.GetSampleProject ("multi-target", "short-name-formats.csproj");
			CreateNuGetConfigFile (projectFile.ParentDirectory);
			NuGetRestore (projectFile);

			using (var project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile)) {

				var netcore = project.TargetFrameworkMonikers [0];
				var netstandard = project.TargetFrameworkMonikers [1];
				var netframework = project.TargetFrameworkMonikers [2];
				var netstandardMixedCase = project.TargetFrameworkMonikers [3];

				Assert.AreEqual (".NETCoreApp,Version=v1.1", netcore.ToString ());
				Assert.AreEqual (".NETStandard,Version=v1.0", netstandard.ToString ());
				Assert.AreEqual (".NETFramework,Version=v4.7.2", netframework.ToString ());
				Assert.AreEqual (".NETStandard,Version=v1.2", netstandardMixedCase.ToString ());
				Assert.AreEqual ("netcoreapp1.1", netcore.ShortName);
				Assert.AreEqual ("netstandard10", netstandard.ShortName);
				Assert.AreEqual ("net472", netframework.ShortName);
				Assert.AreEqual ("NETStandard12", netstandardMixedCase.ShortName);
			}
		}

		static void NuGetRestore (FilePath file)
		{
			var process = Process.Start ("msbuild", $"/t:Restore /p:RestoreDisableParallel=true \"{file}\"");
			Assert.IsTrue (process.WaitForExit (120000), "Timeout restoring NuGet packages.");
			Assert.AreEqual (0, process.ExitCode);
		}

		/// <summary>
		/// Clear all other package sources and just use the main NuGet package source when
		/// restoring the packages for the project tests.
		/// </summary>
		static void CreateNuGetConfigFile (FilePath directory)
		{
			var fileName = directory.Combine ("NuGet.Config");

			string xml =
				"<configuration>\r\n" +
				"  <packageSources>\r\n" +
				"    <clear />\r\n" +
				"    <add key=\"NuGet v3 Official\" value=\"https://api.nuget.org/v3/index.json\" />\r\n" +
				"  </packageSources>\r\n" +
				"</configuration>";

			File.WriteAllText (fileName, xml);
		}
	}
}
