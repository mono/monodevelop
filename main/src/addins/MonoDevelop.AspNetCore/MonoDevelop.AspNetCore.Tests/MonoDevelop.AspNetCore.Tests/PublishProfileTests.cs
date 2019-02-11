//
// PublishProfileTests.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2018 Microsoft Inc. (http://microsoft.com)
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
using NUnit.Framework;
using MonoDevelop.AspNetCore.Commands;
using System.IO;
using MonoDevelop.AspNetCore.Dialogs;
using MonoDevelop.Projects;
using UnitTests;
using System.Threading.Tasks;
using System.Linq;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Ide;
using MonoDevelop.Core;

namespace MonoDevelop.AspNetCore.Tests
{
	[TestFixture]
	class PublishProfileTests
	{
		[SetUp]
		public void SetUp ()
		{
			DesktopService.Initialize ();
			if (!IdeApp.IsInitialized)
				IdeApp.Initialize (new ProgressMonitor ());
		}

		[TestCase ("Debug", "Any CPU", "netcoreapp2.0", "bin/Debug/netcoreapp2.0/publish")]
		[TestCase ("Release", "x86", "netcoreapp2.1", "bin/Debug/netcoreapp2.1/publish")]
		public void PublishProfilesCanBeRead (string configuration, string platform, string targetFramework, string publishDir)
		{
			var profile = new ProjectPublishProfile {
				LastUsedBuildConfiguration = configuration,
				LastUsedPlatform = platform,
				TargetFramework = targetFramework,
				PublishUrl = publishDir
			};

			string tmpFile = Path.GetTempFileName ();
			File.WriteAllText (tmpFile, ProjectPublishProfile.WriteModel (profile));

			var readProfile = ProjectPublishProfile.ReadModel (tmpFile);

			Assert.That (profile.LastUsedBuildConfiguration, Is.EqualTo (readProfile.LastUsedBuildConfiguration));
			Assert.That (profile.LastUsedPlatform, Is.EqualTo (readProfile.LastUsedPlatform));
			Assert.That (profile.TargetFramework, Is.EqualTo (readProfile.TargetFramework));
			Assert.That (profile.PublishUrl, Is.EqualTo (readProfile.PublishUrl));
		}

		[Test]
		public async Task GetDefaultFolderWhenPublish ()
		{
			var solutionFileName = Util.GetSampleProject ("restore-netcore-offline", "dotnetcoreconsole.sln");
			var solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllProjects ().OfType<DotNetProject>().Single ();

			var resolver = new DefaultFolderResolver (project);

			//if there is a Release configuration id, the expected relative path should follow: "bin/Release/<netcore-version>/publish"
			var expected = $"bin/{DefaultFolderResolver.DefaultConfiguration}/{GetShortFrameworkIdentifier (project.TargetFramework.Id)}/publish";
			Assert.That (resolver.GetDefaultFolder (UriKind.Relative) == expected);

			//if there is NOT a Release configuration id, the expected relative path should follow: "bin/<CurrentActiveConfig>/<netcore-version>/publish"
			project.Configurations.Remove ("Release");
			IdeApp.Workspace.ActiveConfigurationId = "Debug";
			expected = $"bin/{project.GetActiveConfiguration ()}/{GetShortFrameworkIdentifier (project.TargetFramework.Id)}/publish";
			Assert.That (resolver.GetDefaultFolder (UriKind.Relative) == expected);

			solution.Dispose ();
		}

		[TearDown]
		public async Task TearDown () => await IdeApp.Workspace.Close ();

		string GetShortFrameworkIdentifier (TargetFrameworkMoniker framework)
		{
			string shortFrameworkIdentifier = framework.Identifier;

			if (shortFrameworkIdentifier [0] == '.')
				shortFrameworkIdentifier = shortFrameworkIdentifier.Substring (1);

			return shortFrameworkIdentifier.ToLower () + framework.Version;
		}
	}
}
