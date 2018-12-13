//
// DotNetCoreSDKResolverTests.cs
//
// Author:
//       josemiguel <jostor@microsoft.com>
//
// Copyright (c) 2018 
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.DotNetCore.Tests
{
	[TestFixture]
	public class DotNetCoreSDKResolverTests
	{
		readonly DotNetCoreVersion [] SdkVersions = {
				DotNetCoreVersion.Parse ("1.1.1"),
				DotNetCoreVersion.Parse ("1.0.5"),
				DotNetCoreVersion.Parse ("1.1.2"),
				DotNetCoreVersion.Parse ("2.0.0-preview1"),
				DotNetCoreVersion.Parse ("2.0.0-preview2"),
				DotNetCoreVersion.Parse ("2.0.0"),
				DotNetCoreVersion.Parse ("1.0.7"),
				DotNetCoreVersion.Parse ("1.1.4"),
				DotNetCoreVersion.Parse ("1.0.8"),
				DotNetCoreVersion.Parse ("1.1.5"),
				DotNetCoreVersion.Parse ("2.0.3"),
				DotNetCoreVersion.Parse ("2.0.4"),
				DotNetCoreVersion.Parse ("1.0.9"),
				DotNetCoreVersion.Parse ("1.1.6"),
				DotNetCoreVersion.Parse ("2.0.5"),
				DotNetCoreVersion.Parse ("2.1-preview1"),
				DotNetCoreVersion.Parse ("1.0.10"),
				DotNetCoreVersion.Parse ("2.0.6"),
				DotNetCoreVersion.Parse ("2.1-preview2"),
				DotNetCoreVersion.Parse ("1.0.11"),
				DotNetCoreVersion.Parse ("1.1.7"),
				DotNetCoreVersion.Parse ("1.1.8"),
				DotNetCoreVersion.Parse ("2.0.7"),
				DotNetCoreVersion.Parse ("2.1-rc1"),
				DotNetCoreVersion.Parse ("2.0.7-2"),
				DotNetCoreVersion.Parse ("2.1.0"),
				DotNetCoreVersion.Parse ("2.1.1"),
				DotNetCoreVersion.Parse ("1.0.12"),
				DotNetCoreVersion.Parse ("1.1.9"),
				DotNetCoreVersion.Parse ("2.0.9"),
				DotNetCoreVersion.Parse ("2.1.2"),
				DotNetCoreVersion.Parse ("2.1.3"),
				DotNetCoreVersion.Parse ("2.2.0-preview1"),
				DotNetCoreVersion.Parse ("2.1.4"),
				DotNetCoreVersion.Parse ("2.2.0-preview2"),
				DotNetCoreVersion.Parse ("2.1.5"),
				DotNetCoreVersion.Parse ("1.0.13"),
				DotNetCoreVersion.Parse ("1.1.10"),
				DotNetCoreVersion.Parse ("2.2.0-preview3"),
				DotNetCoreVersion.Parse ("2.1.6"),
				DotNetCoreVersion.Parse ("3.0.100-preview-009790"),

				DotNetCoreVersion.Parse ("2.2.0"),
				DotNetCoreVersion.Parse ("2.2.2"),

			};
	
		DotNetCoreSdkPaths CreateResolver (string dotnetCorePath, bool mockSdkVersions = true)
		{
			if (string.IsNullOrEmpty (DotNetCoreRuntime.FileName))
				Assert.Inconclusive ($"'DotNetCoreRuntime.FileName' is empty. Unable to run the test. DotNetCore installed? {DotNetCoreRuntime.IsInstalled}");

			var resolver = new DotNetCoreSdkPaths (dotnetCorePath);
			if (mockSdkVersions)
				resolver.SdkVersions = this.SdkVersions;

			return resolver;
		}

		async Task<Solution> GetSolution ()
		{
			var solutionFileName = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-sdk-console.sln");
			return (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
		}

		[Test]
		public void WhenDotNetCorePathIsNull_ThenItReturnsLatestSdk ()
		{
			var resolver = CreateResolver (string.Empty, mockSdkVersions: true);
			var expectedVersion = resolver.GetLatestSdk ();
			var expectedResult = Path.Combine (resolver.SdkRootPath, expectedVersion.OriginalString, "Sdks");

			resolver.ResolveSDK ();

			Assert.That (resolver.MSBuildSDKsPath, Is.EqualTo (expectedResult));
		}

		[Test]
		public void WhenDotNetCorePathDoesNotExist_ThenItReturnsNull ()
		{
			var resolver = CreateResolver ("/usr/fake_folder/dotnet", mockSdkVersions: false);
			resolver.ResolveSDK ();

			Assert.That (resolver.MSBuildSDKsPath, Is.Null);
		}

		[Test]
		public void WhenNoGlobalJson_ThenItReturnsLatestSdk ()
		{
			var resolver = CreateResolver (DotNetCoreRuntime.FileName, mockSdkVersions: true);
			var expectedVersion = resolver.GetLatestSdk ();
			var expectedResult = Path.Combine (resolver.SdkRootPath, expectedVersion.OriginalString, "Sdks");

			resolver.ResolveSDK ();

			Assert.That (resolver.MSBuildSDKsPath, Is.EqualTo (expectedResult));
		}

		[Test]
		public async Task WhenGlobalJsonAndVersionMatches_ThenItReturnsThatVersion ()
		{
			var resolver = CreateResolver (DotNetCoreRuntime.FileName, mockSdkVersions: true);
			var expectedVersion = DotNetCoreVersion.Parse ("2.1.0");
			var expectedResult = Path.Combine (resolver.SdkRootPath, expectedVersion.OriginalString, "Sdks");

			using (var solution = await GetSolution ()) {
				var workingDirectory = Path.GetDirectoryName (solution.FileName);
				var globalJsonPath = CreateGlobalJson (workingDirectory, expectedVersion.OriginalString);

				resolver.ResolveSDK (workingDirectory);

				Assert.That (resolver.MSBuildSDKsPath, Is.EqualTo (expectedResult));
			}
		}

		[Test]
		public async Task WhenGlobalJsonAndVersionNotMatches_AndVersionLessThan21_ThenItReturnsLatestPatchVersion ()
		{
			var resolver = CreateResolver (DotNetCoreRuntime.FileName, mockSdkVersions: true);
			var versionThatDoesNotExists = DotNetCoreVersion.Parse ("1.0.6");
			var versionThatShouldReturn = DotNetCoreVersion.Parse ("1.0.13");
			var expectedResult = Path.Combine (resolver.SdkRootPath, versionThatShouldReturn.OriginalString, "Sdks");

			using (var solution = await GetSolution ()) {
				var workingDirectory = Path.GetDirectoryName (solution.FileName);
				var globalJsonPath = CreateGlobalJson (workingDirectory, versionThatDoesNotExists.OriginalString);

				resolver.ResolveSDK (workingDirectory);

				Assert.That (resolver.MSBuildSDKsPath, Is.EqualTo (expectedResult));
			}
		}

		[Test]
		public async Task WhenGlobalJsonAndVersionNotMatches_AndVersionIs21OrBefore_ThenItReturnsLatestPatchVersion ()
		{
			var resolver = CreateResolver (DotNetCoreRuntime.FileName, mockSdkVersions: true);
			var versionThatDoesNotExists = DotNetCoreVersion.Parse ("2.2.1");
			var versionThatShouldReturn = DotNetCoreVersion.Parse ("2.2.2");
			var expectedResult = Path.Combine (resolver.SdkRootPath, versionThatShouldReturn.OriginalString, "Sdks");

			using (var solution = await GetSolution ()) {
				var workingDirectory = Path.GetDirectoryName (solution.FileName);
				var globalJsonPath = CreateGlobalJson (workingDirectory, versionThatDoesNotExists.OriginalString);

				resolver.ResolveSDK (workingDirectory);

				Assert.That (resolver.MSBuildSDKsPath, Is.EqualTo (expectedResult));
			}
		}

		[Test]
		public async Task WhenGlobalJsonAndVersionNotMatches_AndVersionIs21OrBefore_ThenItReturnsIsNotSupported ()
		{
			var resolver = CreateResolver (DotNetCoreRuntime.FileName, mockSdkVersions: true);
			var versionThatDoesNotExists = DotNetCoreVersion.Parse ("2.2.3");
			var versionThatShouldReturn = resolver.GetLatestSdk ();
			var expectedResult = Path.Combine (resolver.SdkRootPath, versionThatShouldReturn.OriginalString, "Sdks");

			using (var solution = await GetSolution ()) {
				var workingDirectory = Path.GetDirectoryName (solution.FileName);
				var globalJsonPath = CreateGlobalJson (workingDirectory, versionThatDoesNotExists.OriginalString);

				resolver.ResolveSDK (workingDirectory);

				Assert.True (resolver.IsUnsupportedSdkVersion);
			}
		}

		string CreateGlobalJson (string workingDirectory, string version)
		{
			try {
				var GlobalJsonContent = $"\n\t{{\n\t\t\"sdk\": {{\n\t\t\t\"version\": \"{version}\"\n\t\t}}\n\t}}";
				var GlobalJsonLocation = Path.Combine (workingDirectory, "global.json");

				File.WriteAllText (GlobalJsonLocation, GlobalJsonContent);

				return GlobalJsonLocation;
			} catch (Exception) {
				return string.Empty;
			}
		}
	}
}
