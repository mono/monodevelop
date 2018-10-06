﻿//
// DotNetCoreProjectTemplateTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.DotNetCore.Tests
{
	/// <summary>
	/// Creates and builds .NET Core projects.
	/// </summary>
	[TestFixture]
	class DotNetCoreProjectTemplateTests : DotNetCoreTestBase
	{
		TemplatingService templatingService;
		Solution solution;

		[TestFixtureSetUp]
		public void SetUp ()
		{
			string runTests = Environment.GetEnvironmentVariable ("DOTNETCORE_IGNORE_PROJECT_TEMPLATE_TESTS");
			if (!string.IsNullOrEmpty (runTests)) {
				Assert.Ignore ("Ignoring DotNetCoreProjectTemplateTests");
			}

			templatingService = new TemplatingService ();

			if (!IdeApp.IsInitialized) {
				IdeApp.Initialize (Util.GetMonitor ());
			}
		}

		[TearDown]
		public override void TearDown ()
		{
			solution?.Dispose ();
			solution = null;

			base.TearDown ();
		}

		[Test]
		[TestCase ("Microsoft.Common.Library.CSharp", "UseNetStandard1x=true;Framework=netstandard1.0")]
		[TestCase ("Microsoft.Common.Library.CSharp", "UseNetStandard1x=true;Framework=netstandard1.5")]
		[TestCase ("Microsoft.Common.Library.FSharp", "UseNetStandard1x=true;Framework=netstandard1.6")]
		public async Task NetStandard1x (string templateId, string parameters)
		{
			var config = CreateNewProjectConfig ("NetStandard1x", templateId, parameters);
			SolutionTemplate template = FindTemplate (templateId, config);

			await CreateAndBuild (template, config);
		}

		[Test]
		[TestCase ("Microsoft.Common.Console.CSharp", "UseNetCore1x=true;Framework=netcoreapp1.0")]
		[TestCase ("Microsoft.Common.Console.CSharp", "UseNetCore1x=true;Framework=netcoreapp1.1")]

		[TestCase ("Microsoft.Common.Library.CSharp-netcoreapp", "UseNetCore1x=true;Framework=netcoreapp1.0")]
		[TestCase ("Microsoft.Common.Library.CSharp-netcoreapp", "UseNetCore1x=true;Framework=netcoreapp1.1")]

		[TestCase ("Microsoft.Test.xUnit.CSharp", "UseNetCore1x=true;Framework=netcoreapp1.0")]
		[TestCase ("Microsoft.Test.xUnit.CSharp", "UseNetCore1x=true;Framework=netcoreapp1.1")]

		[TestCase ("Microsoft.Test.MSTest.CSharp", "UseNetCore1x=true;Framework=netcoreapp1.0")]
		[TestCase ("Microsoft.Test.MSTest.CSharp", "UseNetCore1x=true;Framework=netcoreapp1.1")]

		[TestCase ("Microsoft.Common.Console.FSharp", "UseNetCore1x=true;Framework=netcoreapp1.0")]
		[TestCase ("Microsoft.Common.Console.FSharp", "UseNetCore1x=true;Framework=netcoreapp1.1")]

		[TestCase ("Microsoft.Test.xUnit.FSharp", "UseNetCore1x=true;Framework=netcoreapp1.0")]
		[TestCase ("Microsoft.Test.xUnit.FSharp", "UseNetCore1x=true;Framework=netcoreapp1.1")]

		[TestCase ("Microsoft.Test.MSTest.FSharp", "UseNetCore1x=true;Framework=netcoreapp1.0")]
		[TestCase ("Microsoft.Test.MSTest.FSharp", "UseNetCore1x=true;Framework=netcoreapp1.1")]
		public async Task NetCore1x (string templateId, string parameters)
		{
			if (!DotNetCoreRuntime.IsInstalled) {
				Assert.Ignore (".NET Core runtime is not installed - required by project template.");
			}

			var config = CreateNewProjectConfig ("NetCore1x", templateId, parameters);
			SolutionTemplate template = FindTemplate (templateId, config);

			await CreateAndBuild (template, config);
		}

		[Test]
		[TestCase ("Microsoft.Common.Library.CSharp", "UseNetStandard20=true")]
		[TestCase ("Microsoft.Common.Library.FSharp", "UseNetStandard20=true")]
		[TestCase ("Microsoft.Common.Library.VisualBasic", "UseNetStandard20=true")]
		public async Task NetStandard20 (string templateId, string parameters)
		{
			if (!IsDotNetCoreSdk2xInstalled ()) {
				Assert.Ignore (".NET Core 2 SDK is not installed - required by project template.");
			}

			var config = CreateNewProjectConfig ("NetStandard2x", templateId, parameters);
			SolutionTemplate template = FindTemplate (templateId, config);

			await CreateAndBuild (template, config);
		}

		[TestCase ("Microsoft.Common.Console.CSharp","UseNetCore20=true")]
		[TestCase ("Microsoft.Common.Library.CSharp-netcoreapp", "UseNetCore20=true;Framework=netcoreapp2.0")]
		[TestCase ("Microsoft.Test.xUnit.CSharp", "UseNetCore20=true")]
		[TestCase ("Microsoft.Test.MSTest.CSharp", "UseNetCore20=true")]

		[TestCase ("Microsoft.Common.Console.FSharp", "UseNetCore20=true")]
		[TestCase ("Microsoft.Common.Library.FSharp-netcoreapp", "UseNetCore20=true;Framework=netcoreapp2.0")]
		[TestCase ("Microsoft.Test.xUnit.FSharp", "UseNetCore20=true")]
		[TestCase ("Microsoft.Test.MSTest.FSharp", "UseNetCore20=true")]
		public async Task NetCore20 (string templateId, string parameters)
		{
			if (!IsDotNetCoreSdk20Installed ()) {
				Assert.Ignore (".NET Core 2.0 SDK is not installed - required by project template.");
			}

			var config = CreateNewProjectConfig ("NetCore2x", templateId, parameters);
			SolutionTemplate template = FindTemplate (templateId, config);

			await CreateAndBuild (template, config);
		}

		[TestCase ("Microsoft.Common.Console.CSharp","UseNetCore21=true")]
		[TestCase ("Microsoft.Common.Library.CSharp-netcoreapp", "UseNetCore21=true;Framework=netcoreapp2.1")]
		[TestCase ("Microsoft.Test.xUnit.CSharp", "UseNetCore21=true")]
		[TestCase ("Microsoft.Test.MSTest.CSharp", "UseNetCore21=true")]

		[TestCase ("Microsoft.Common.Console.FSharp", "UseNetCore21=true")]
		[TestCase ("Microsoft.Common.Library.FSharp-netcoreapp", "UseNetCore21=true;Framework=netcoreapp2.1")]
		[TestCase ("Microsoft.Test.xUnit.FSharp", "UseNetCore21=true")]
		[TestCase ("Microsoft.Test.MSTest.FSharp", "UseNetCore21=true")]

		[TestCase ("Microsoft.Common.Console.VisualBasic", "UseNetCore21=true")]
		[TestCase ("Microsoft.Common.Library.VisualBasic-netcoreapp", "UseNetCore21=true;Framework=netcoreapp2.1")]
		[TestCase ("Microsoft.Test.xUnit.VisualBasic", "UseNetCore21=true")]
		[TestCase ("Microsoft.Test.MSTest.VisualBasic", "UseNetCore21=true")]
		public async Task NetCore21 (string templateId, string parameters)
		{
			if (!IsDotNetCoreSdk21Installed ()) {
				Assert.Ignore (".NET Core 2.1 SDK is not installed - required by project template.");
			}

			var config = CreateNewProjectConfig ("NetCore2x", templateId, parameters);
			SolutionTemplate template = FindTemplate (templateId, config);

			await CreateAndBuild (template, config);
		}

		[TestCase ("Microsoft.Common.Console.CSharp", "UseNetCore22=true")]
		[TestCase ("Microsoft.Common.Library.CSharp-netcoreapp", "UseNetCore22=true;Framework=netcoreapp2.2")]
		[TestCase ("Microsoft.Test.xUnit.CSharp", "UseNetCore22=true")]
		[TestCase ("Microsoft.Test.MSTest.CSharp", "UseNetCore22=true")]

		[TestCase ("Microsoft.Common.Console.FSharp", "UseNetCore22=true")]
		[TestCase ("Microsoft.Common.Library.FSharp-netcoreapp", "UseNetCore22=true;Framework=netcoreapp2.2")]
		[TestCase ("Microsoft.Test.xUnit.FSharp", "UseNetCore22=true")]
		[TestCase ("Microsoft.Test.MSTest.FSharp", "UseNetCore22=true")]

		[TestCase ("Microsoft.Common.Console.VisualBasic", "UseNetCore22=true")]
		[TestCase ("Microsoft.Common.Library.VisualBasic-netcoreapp", "UseNetCore22=true;Framework=netcoreapp2.2")]
		[TestCase ("Microsoft.Test.xUnit.VisualBasic", "UseNetCore22=true")]
		[TestCase ("Microsoft.Test.MSTest.VisualBasic", "UseNetCore22=true")]
		public async Task NetCore22 (string templateId, string parameters)
		{
			if (!IsDotNetCoreSdk22Installed ()) {
				Assert.Ignore (".NET Core 2.2 SDK is not installed - required by project template.");
			}

			var config = CreateNewProjectConfig ("NetCore2x", templateId, parameters);
			SolutionTemplate template = FindTemplate (templateId, config);

			await CreateAndBuild (template, config);
		}

		[TestCase ("Microsoft.Web.Empty.CSharp", "UseNetCore1x=true;Framework=netcoreapp1.0")]
		[TestCase ("Microsoft.Web.Empty.CSharp", "UseNetCore1x=true;Framework=netcoreapp1.1")]

		[TestCase ("Microsoft.Web.Empty.FSharp", "UseNetCore1x=true;Framework=netcoreapp1.0")]
		[TestCase ("Microsoft.Web.Empty.FSharp", "UseNetCore1x=true;Framework=netcoreapp1.1")]

		[TestCase ("Microsoft.Web.Mvc.CSharp", "UseNetCore1x=true;Framework=netcoreapp1.0")]
		[TestCase ("Microsoft.Web.Mvc.CSharp", "UseNetCore1x=true;Framework=netcoreapp1.1")]

		[TestCase ("Microsoft.Web.Mvc.FSharp", "UseNetCore1x=true;Framework=netcoreapp1.0")]
		[TestCase ("Microsoft.Web.Mvc.FSharp", "UseNetCore1x=true;Framework=netcoreapp1.1")]

		[TestCase ("Microsoft.Web.WebApi.CSharp", "UseNetCore1x=true;Framework=netcoreapp1.0")]
		[TestCase ("Microsoft.Web.WebApi.CSharp", "UseNetCore1x=true;Framework=netcoreapp1.1")]
		public async Task AspNetCore1x (string templateId, string parameters)
		{
			if (!DotNetCoreRuntime.IsInstalled) {
				Assert.Ignore (".NET Core runtime is not installed - required by project template.");
			}

			var config = CreateNewProjectConfig ("AspNetCore1x", templateId, parameters);
			SolutionTemplate template = FindTemplate (templateId, config);

			await CreateAndBuild (template, config);
		}

		[TestCase ("Microsoft.Web.Empty.CSharp", "UseNetCore20=true")]
		[TestCase ("Microsoft.Web.Empty.FSharp", "UseNetCore20=true")]
		[TestCase ("Microsoft.Web.Mvc.CSharp", "UseNetCore20=true")]
		[TestCase ("Microsoft.Web.Mvc.FSharp", "UseNetCore20=true")]
		[TestCase ("Microsoft.Web.RazorPages.CSharp", "UseNetCore20=true")]
		[TestCase ("Microsoft.Web.WebApi.CSharp", "UseNetCore20=true")]
		[TestCase ("Microsoft.Web.WebApi.FSharp", "UseNetCore20=true")]
		public async Task AspNetCore20 (string templateId, string parameters)
		{
			if (!IsDotNetCoreSdk20Installed ()) {
				Assert.Ignore (".NET Core 2.0 SDK is not installed - required by project template.");
			}

			var config = CreateNewProjectConfig ("NetCore2x", templateId, parameters);
			SolutionTemplate template = FindTemplate (templateId, config);

			await CreateAndBuild (template, config);
		}

		[TestCase ("Microsoft.Web.Empty.CSharp", "UseNetCore21=true")]
		[TestCase ("Microsoft.Web.Empty.FSharp", "UseNetCore21=true")]
		[TestCase ("Microsoft.Web.Mvc.CSharp", "UseNetCore21=true")]
		[TestCase ("Microsoft.Web.Mvc.FSharp", "UseNetCore21=true")]
		[TestCase ("Microsoft.Web.RazorPages.CSharp", "UseNetCore21=true")]
		[TestCase ("Microsoft.Web.WebApi.CSharp", "UseNetCore21=true")]
		[TestCase ("Microsoft.Web.WebApi.FSharp", "UseNetCore21=true")]
		public async Task AspNetCore21 (string templateId, string parameters)
		{
			if (!IsDotNetCoreSdk21Installed ()) {
				Assert.Ignore (".NET Core 2.1 SDK is not installed - required by project template.");
			}

			var config = CreateNewProjectConfig ("NetCore2x", templateId, parameters);
			SolutionTemplate template = FindTemplate (templateId, config);

			await CreateAndBuild (template, config);
		}

		[TestCase ("Microsoft.Web.Empty.CSharp", "UseNetCore22=true")]
		[TestCase ("Microsoft.Web.Empty.FSharp", "UseNetCore22=true")]
		[TestCase ("Microsoft.Web.Mvc.CSharp", "UseNetCore22=true")]
		[TestCase ("Microsoft.Web.Mvc.FSharp", "UseNetCore22=true")]
		[TestCase ("Microsoft.Web.RazorPages.CSharp", "UseNetCore22=true")]
		[TestCase ("Microsoft.Web.WebApi.CSharp", "UseNetCore22=true")]
		[TestCase ("Microsoft.Web.WebApi.FSharp", "UseNetCore22=true")]
		public async Task AspNetCore22 (string templateId, string parameters)
		{
			if (!IsDotNetCoreSdk22Installed ()) {
				Assert.Ignore (".NET Core 2.2 SDK is not installed - required by project template.");
			}

			var config = CreateNewProjectConfig ("NetCore2x", templateId, parameters);
			SolutionTemplate template = FindTemplate (templateId, config);

			await CreateAndBuild (template, config);
		}

		static bool IsDotNetCoreSdk2xInstalled ()
		{
			return DotNetCoreSdk.Versions.Any (version => version.Major == 2);
		}

		static bool IsDotNetCoreSdk20Installed ()
		{
			return DotNetCoreSdk.Versions.Any (version => version.Major == 2 && version.Minor == 0) ||
				DotNetCoreSdk.Versions.Any (version => version.Major == 2 && version.Minor == 1 && version.Patch < 300);
		}

		static bool IsDotNetCoreSdk21Installed ()
		{
			return DotNetCoreSdk.Versions.Any (version => version.Major == 2 && version.Minor == 1 && version.Patch >= 300);
		}

		static bool IsDotNetCoreSdk22Installed ()
		{
			return DotNetCoreSdk.Versions.Any (version => version.Major == 2 && version.Minor == 2);
		}

		NewProjectConfiguration CreateNewProjectConfig (string baseName, string templateId, string parameters)
		{
			FilePath solutionDirectory = Util.CreateTmpDir (baseName);

			CreateNuGetConfigFile (solutionDirectory);

			string projectName = GetProjectName (templateId);

			var config = new NewProjectConfiguration {
				CreateSolution = true,
				CreateProjectDirectoryInsideSolutionDirectory = true,
				CreateGitIgnoreFile = false,
				UseGit = false,
				Location = solutionDirectory,
				ProjectName = projectName,
				SolutionName = projectName
			};

			foreach (var templateParameter in TemplateParameter.CreateParameters (parameters)) {
				config.Parameters [templateParameter.Name] = templateParameter.Value;
			}

			Directory.CreateDirectory (config.ProjectLocation);

			return config;
		}

		static string GetProjectName (string templateId)
		{
			return templateId.Replace ("Microsoft.Test.", "")
				.Replace ("Microsoft.Common.", "")
				.Replace ("Microsoft.", "")
				.Replace (".", "");
		}

		SolutionTemplate FindTemplate (string templateId, NewProjectConfiguration config)
		{
			var categories = templatingService
				.GetProjectTemplateCategories (t => MatchTemplate (t, templateId))
				.ToList ();

			var templates = categories.First ()
				.Categories.First ()
				.Categories.First ()
				.Templates.ToList ();

			var template = templates.Single ();

			string language = GetLanguage (templateId);

			return template.GetTemplate (language, config.Parameters);
		}

		static bool MatchTemplate (SolutionTemplate template, string templateId)
		{
			return template.Id == templateId;
		}

		static string GetLanguage (string templateId)
		{
			if (templateId.Contains ("FSharp")) {
				return "F#";
			}
			if (templateId.Contains ("VisualBasic")) {
				return "VB";
			}

			return "C#";
		}

		async Task CreateAndBuild (
			SolutionTemplate template,
			NewProjectConfiguration config)
		{
			var result = await templatingService.ProcessTemplate (template, config, null);

			solution = result.WorkspaceItems.FirstOrDefault () as Solution;
			await solution.SaveAsync (Util.GetMonitor ());

			// RestoreDisableParallel prevents parallel restores which sometimes cause
			// the restore to fail on Mono.
			RunMSBuild ($"/t:Restore /p:RestoreDisableParallel=true \"{solution.FileName}\"");
			RunMSBuild ($"/t:Build \"{solution.FileName}\"");

			CheckProjectTypeGuids (solution, GetProjectTypeGuid (template));
		}

		void RunMSBuild (string arguments)
		{
			var process = Process.Start ("msbuild", arguments);
			Assert.IsTrue (process.WaitForExit (240000), "Timed out waiting for MSBuild.");
			Assert.AreEqual (0, process.ExitCode, $"msbuild {arguments} failed");
		}

		static void CheckProjectTypeGuids (Solution solution, string expectedProjectTypeGuid)
		{
			foreach (Project project in solution.GetAllProjects ()) {
				Assert.AreEqual (expectedProjectTypeGuid, project.TypeGuid);
			}
		}

		static string GetProjectTypeGuid (SolutionTemplate template)
		{
			string language = GetLanguage (template.Id);
			if (language == "F#")
				return "{F2A71F9B-5D33-465A-A702-920D77279786}";

			if (language == "VB")
				return "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";

			// C#
			return "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
		}
	}
}
