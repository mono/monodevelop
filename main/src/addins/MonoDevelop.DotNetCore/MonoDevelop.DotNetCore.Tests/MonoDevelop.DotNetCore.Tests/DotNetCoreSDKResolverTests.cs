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
				DotNetCoreVersion.Parse ("1.0.5"),
				DotNetCoreVersion.Parse ("1.0.7"),
				DotNetCoreVersion.Parse ("1.0.8"),
				DotNetCoreVersion.Parse ("1.0.9"),
				DotNetCoreVersion.Parse ("1.0.10"),
				DotNetCoreVersion.Parse ("1.0.11"),
				DotNetCoreVersion.Parse ("1.0.12"),
				DotNetCoreVersion.Parse ("1.0.13"),
				DotNetCoreVersion.Parse ("1.1.1"),
				DotNetCoreVersion.Parse ("1.1.2"),
				DotNetCoreVersion.Parse ("1.1.4"),
				DotNetCoreVersion.Parse ("1.1.5"),
				DotNetCoreVersion.Parse ("1.1.6"),
				DotNetCoreVersion.Parse ("1.1.7"),
				DotNetCoreVersion.Parse ("1.1.8"),
				DotNetCoreVersion.Parse ("1.1.9"),
				DotNetCoreVersion.Parse ("1.1.10"),
				//.Net Core v 2.0
				DotNetCoreVersion.Parse ("2.0.0-preview1-005977"),
				DotNetCoreVersion.Parse ("2.0.0-preview2-006497"),
				DotNetCoreVersion.Parse ("2.0.0"),
				DotNetCoreVersion.Parse ("2.0.3"),
				DotNetCoreVersion.Parse ("2.1.2"),
				DotNetCoreVersion.Parse ("2.1.4"),
				DotNetCoreVersion.Parse ("2.1.100"),
				DotNetCoreVersion.Parse ("2.1.101"),
				DotNetCoreVersion.Parse ("2.1.102"),
				DotNetCoreVersion.Parse ("2.1.103"),
				DotNetCoreVersion.Parse ("2.1.104"),
				DotNetCoreVersion.Parse ("2.1.105"),
				DotNetCoreVersion.Parse ("2.1.200"),
				DotNetCoreVersion.Parse ("2.1.201"),
				DotNetCoreVersion.Parse ("2.1.202"),
				//.Net Core v 2.1
				DotNetCoreVersion.Parse ("2.1.300-preview1"),
				DotNetCoreVersion.Parse ("2.1.300-preview2"),
				DotNetCoreVersion.Parse ("2.1.300-rc1"),
				DotNetCoreVersion.Parse ("2.1.300"),
				DotNetCoreVersion.Parse ("2.1.301"),
				DotNetCoreVersion.Parse ("2.1.302"),
				DotNetCoreVersion.Parse ("2.1.400"),
				DotNetCoreVersion.Parse ("2.1.401"),
				DotNetCoreVersion.Parse ("2.1.403"),
				DotNetCoreVersion.Parse ("2.1.500"),
				DotNetCoreVersion.Parse ("2.1.502"),
				//.Net Core v 2.2
				DotNetCoreVersion.Parse ("2.2.100-preview1"),
				DotNetCoreVersion.Parse ("2.2.100-preview2"),
				DotNetCoreVersion.Parse ("2.2.100-preview3"),
				DotNetCoreVersion.Parse ("2.2.100"),
				DotNetCoreVersion.Parse ("2.2.101"),
				//.Net Core 3.0
				DotNetCoreVersion.Parse ("3.0.100-preview-009812"),
				//. Fake versions
				DotNetCoreVersion.Parse ("2.2.0"),
				DotNetCoreVersion.Parse ("2.2.2"),
				DotNetCoreVersion.Parse ("2.1.399"),

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
			var expectedVersion = DotNetCoreVersion.Parse ("2.1.500");
			var expectedResult = Path.Combine (resolver.SdkRootPath, expectedVersion.OriginalString, "Sdks");

			using (var solution = await GetSolution ()) {
				var workingDirectory = Path.GetDirectoryName (solution.FileName);
				var globalJsonPath = CreateGlobalJson (workingDirectory, expectedVersion.OriginalString);

				resolver.GlobalJsonPath = globalJsonPath;
				resolver.ResolveSDK (workingDirectory);

				Assert.That (resolver.MSBuildSDKsPath, Is.EqualTo (expectedResult));
			}
		}

		[Test (Description = "This test is essentially the same than WhenGlobalJsonAndVersionMatches_ThenItReturnsThatVersion " + 
			" but the global.json is generated into parent's workingDirectory forcing ResolveSDK to look it up")]
		public async Task WhenGlobalJsonIsInParentWorkDirAndVersionMatches_ThenItReturnsThatVersion ()
		{
			var resolver = CreateResolver (DotNetCoreRuntime.FileName, mockSdkVersions: true);
			var expectedVersion = DotNetCoreVersion.Parse ("2.1.500");
			var expectedResult = Path.Combine (resolver.SdkRootPath, expectedVersion.OriginalString, "Sdks");

			using (var solution = await GetSolution ()) {
				var workingDirectory = Path.GetDirectoryName (solution.FileName);
				var workingParentDirectory = new DirectoryInfo (workingDirectory).Parent;
				var globalJsonPath = CreateGlobalJson (workingParentDirectory.FullName, expectedVersion.OriginalString);
					
				//in this test we force ResolveSDK to look up global.json in parent's directories
				resolver.ResolveSDK (workingDirectory, forceLookUpGlobalJson: true);

				Assert.That (globalJsonPath, Is.EqualTo (resolver.GlobalJsonPath));
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

				resolver.GlobalJsonPath = globalJsonPath;
				resolver.ResolveSDK (workingDirectory);

				Assert.That (resolver.MSBuildSDKsPath, Is.EqualTo (expectedResult));
			}
		}

		[Test]
		public async Task WhenGlobalJsonAndVersionNotMatches_AndVersionIs21OrBefore_ThenItReturnsLatestPatchVersion ()
		{
			var resolver = CreateResolver (DotNetCoreRuntime.FileName, mockSdkVersions: true);
			var versionThatDoesNotExists = DotNetCoreVersion.Parse ("2.1.310");
			var versionThatShouldReturn = DotNetCoreVersion.Parse ("2.1.399");
			var expectedResult = Path.Combine (resolver.SdkRootPath, versionThatShouldReturn.OriginalString, "Sdks");

			using (var solution = await GetSolution ()) {
				var workingDirectory = Path.GetDirectoryName (solution.FileName);
				var globalJsonPath = CreateGlobalJson (workingDirectory, versionThatDoesNotExists.OriginalString);

				resolver.GlobalJsonPath = globalJsonPath;
				resolver.ResolveSDK (workingDirectory);

				Assert.That (resolver.MSBuildSDKsPath, Is.EqualTo (expectedResult));
			}
		}

		[Test]
		public async Task WhenGlobalJsonAndVersionNotMatches_AndVersionIs21OrBefore_ThenItReturnsIsNotSupported ()
		{
			var resolver = CreateResolver (DotNetCoreRuntime.FileName, mockSdkVersions: true);
			var versionThatDoesNotExists = DotNetCoreVersion.Parse ("2.1.503");
		
			using (var solution = await GetSolution ()) {
				var workingDirectory = Path.GetDirectoryName (solution.FileName);
				var globalJsonPath = CreateGlobalJson (workingDirectory, versionThatDoesNotExists.OriginalString);

				resolver.GlobalJsonPath = globalJsonPath;
				resolver.ResolveSDK (workingDirectory);

				Assert.True (resolver.IsUnsupportedSdkVersion);
			}
		}

		[TestCase ("2.1.303", "2.1.399", true, 
			Description = "WHEN we set the global.json to SDK=2.1.303 and it does not exist " 
						+ " since version is 2.1.100 or higher THEN Resolver should return that IsSupported AND the latest patch that in this case is 2.1.399")]
		[TestCase ("2.1.402", "2.1.403", true,
			Description ="WHEN we set the global.json to SDK=2.1.402 and it does not exist " 
						+ " since version is 2.1.100 or higher THEN Resolver should return that IsSupported AND the latest patch that in this case is 2.1.403")]
		[TestCase ("2.1.503", "", false, 
			Description = "WHEN we set the global.json to SDK=2.1.503 and it does not exist " 
						+ "THEN since there is no higher patch installed it should return that NOT IsSupported AND empty expected version")]
 		[TestCase ("2.0.4", "2.0.3", true,
			Description = "WHEN we set the global.json to SDK=2.0.4 and it does not exist "
						+ " since version before 2.1.100 THEN Resolver should return that IsSupported AND the latest patch that in this case is 2.0.3")]
		public async Task WhenGlobalJsonAndVersionNotMatches (string requestedVersion, string expectedVersion, bool isSupported)
		{
			var resolver = CreateResolver (DotNetCoreRuntime.FileName, mockSdkVersions: true);
			var versionThatDoesNotExists = DotNetCoreVersion.Parse (requestedVersion);
			DotNetCoreVersion versionThatShouldReturn;
			string expectedResult = string.Empty;

			if (!string.IsNullOrEmpty (expectedVersion)) {
				versionThatShouldReturn = DotNetCoreVersion.Parse (expectedVersion);
				expectedResult = Path.Combine (resolver.SdkRootPath, versionThatShouldReturn.OriginalString, "Sdks");
			}

			using (var solution = await GetSolution ()) {
				var workingDirectory = Path.GetDirectoryName (solution.FileName);
				var globalJsonPath = CreateGlobalJson (workingDirectory, versionThatDoesNotExists.OriginalString);

				resolver.GlobalJsonPath = globalJsonPath;
				resolver.ResolveSDK (workingDirectory);

				Assert.That (resolver.IsUnsupportedSdkVersion, Is.EqualTo (!isSupported));
				if (isSupported)
					Assert.That (resolver.MSBuildSDKsPath, Is.EqualTo (expectedResult));
			}
		}

		[TearDown]
		public void TearDown ()
		{
			Util.ClearTmpDir ();
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
