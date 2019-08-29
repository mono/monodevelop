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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using NUnit.Framework;
using IdeUnitTests;
using MonoDevelop.Ide.Projects.FileNesting;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.DotNetCore.Tests
{
	/// <summary>
	/// Creates and builds .NET Core projects.
	/// </summary>
	[TestFixture]
	class DotNetCoreProjectTemplateTests : DotNetCoreTestBase
	{
		class DotNetCoreProjectTemplateTest : ProjectTemplateTest
		{
			public DotNetCoreProjectTemplateTest (string basename, string templateId) : base (basename, templateId)
			{
			}

			protected override string GetExtraNuGetSources ()
			{
				return "    <add key=\"dotnet-core\" value=\"https://dotnetfeed.blob.core.windows.net/dotnet-core/index.json\" />\r\n" +
					"    <add key=\"dotnet-windowsdesktop\" value=\"https://dotnetfeed.blob.core.windows.net/dotnet-windowsdesktop/index.json\" />\r\n" +
					"    <add key=\"aspnet-aspnetcore\" value=\"https://dotnetfeed.blob.core.windows.net/aspnet-aspnetcore/index.json\" />\r\n" +
					"    <add key=\"aspnet-aspnetcore-tooling\" value=\"https://dotnetfeed.blob.core.windows.net/aspnet-aspnetcore-tooling/index.json\" />\r\n" +
					"    <add key=\"aspnet-entityframeworkcore\" value=\"https://dotnetfeed.blob.core.windows.net/aspnet-entityframeworkcore/index.json\" />\r\n" +
					"    <add key=\"aspnet-extensions\" value=\"https://dotnetfeed.blob.core.windows.net/aspnet-extensions/index.json\" />\r\n" +
					"    <add key=\"gRPCrepository\" value=\"https://grpc.jfrog.io/grpc/api/nuget/v3/grpc-nuget-dev\" />\r\n";
			}
		}

		[TestFixtureSetUp]
		public void SetUp ()
		{
			string runTests = Environment.GetEnvironmentVariable ("DOTNETCORE_IGNORE_PROJECT_TEMPLATE_TESTS");
			if (!string.IsNullOrEmpty (runTests)) {
				Assert.Ignore ("Ignoring DotNetCoreProjectTemplateTests");
			}

			// Set environment variable to enable VB.NET support
			// Disabled for now due to .NET Core 3.0 preview 8 bug - https://github.com/dotnet/corefx/issues/40012
			Environment.SetEnvironmentVariable ("MD_FEATURES_ENABLED", "VBNetDotnetCoreTemplates");

			// Set $PATH to point to the .NET Core SDK we provision, as in VSTS bots are
			// setup with lots of old and incompatible SDK versions under $HOME/.dotnet
			Environment.SetEnvironmentVariable ("PATH", $"/usr/local/share/dotnet:{Environment.GetEnvironmentVariable ("PATH")}");
		}

		[Test]
		[TestCase ("Microsoft.Common.Library.CSharp", "UseNetStandard1x=true;Framework=netstandard1.0")]
		[TestCase ("Microsoft.Common.Library.CSharp", "UseNetStandard1x=true;Framework=netstandard1.5")]
		[TestCase ("Microsoft.Common.Library.FSharp", "UseNetStandard1x=true;Framework=netstandard1.6")]
		public async Task NetStandard1x (string templateId, string parameters)
		{
			await CreateFromTemplateAndBuild ("NetStandard1x", templateId, parameters);
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

			await CreateFromTemplateAndBuild ("NetCore1x", templateId, parameters);
		}

		[Test]
		[TestCase ("Microsoft.Common.Library.CSharp", "UseNetStandard20=true")]
		[TestCase ("Microsoft.Common.Library.FSharp", "UseNetStandard20=true")]
		public async Task NetStandard20 (string templateId, string parameters)
		{
			if (!IsDotNetCoreSdk2xInstalled ()) {
				Assert.Ignore (".NET Core 2 SDK is not installed - required by project template.");
			}

			await CreateFromTemplateAndBuild ("NetStandard2x", templateId, parameters);
		}

		[Ignore (".NET Standard 2.1 still not supported")]
		[Test]
		[TestCase ("Microsoft.Common.Library.CSharp", "UseNetStandard21=true")]
		[TestCase ("Microsoft.Common.Library.FSharp", "UseNetStandard21=true")]
		public async Task NetStandard21 (string templateId, string parameters)
		{
			if (!IsDotNetCoreSdk30Installed ()) {
				Assert.Ignore (".NET Core 3 SDK is not installed - required by project template.");
			}

			await CreateFromTemplateAndBuild ("NetStandard21", templateId, parameters);
		}

		[Test]
		public async Task NetStandard20_VBNet ()
		{
			if (IsDotNetCoreSdk21Installed () || IsDotNetCoreSdk22Installed ()) {
				await CreateFromTemplateAndBuild ("NetStandard2x", "Microsoft.Common.Library.VisualBasic", "UseNetStandard20=true");
			} else {
				Assert.Ignore (".NET Core >= 2.1 SDK is not installed - required by project template.");
			}
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

			await CreateFromTemplateAndBuild ("NetCore2x", templateId, parameters);
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

			await CreateFromTemplateAndBuild ("NetCore2x", templateId, parameters);
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

		// NUnit3 templates come with .NET Core 2.2, but they only support .NET Core 2.1 framework
		[TestCase ("NUnit3.DotNetNew.Template.CSharp", "UseNetCore21=true")]
		[TestCase ("NUnit3.DotNetNew.Template.FSharp", "UseNetCore21=true")]
		[TestCase ("NUnit3.DotNetNew.Template.VisualBasic", "UseNetCore21=true")]
		public async Task NetCore22 (string templateId, string parameters)
		{
			if (!IsDotNetCoreSdk22Installed ()) {
				Assert.Ignore (".NET Core 2.2 SDK is not installed - required by project template.");
			}

			await CreateFromTemplateAndBuild ("NetCore2x", templateId, parameters);
		}

		[TestCase ("Microsoft.Common.Console.CSharp", "UseNetCore30=true")]
		[TestCase ("Microsoft.Common.Library.CSharp-netcoreapp", "UseNetCore30=true;Framework=netcoreapp3.0")]
		[TestCase ("Microsoft.Test.xUnit.CSharp", "UseNetCore30=true")]
		[TestCase ("Microsoft.Test.MSTest.CSharp", "UseNetCore30=true")]

		[TestCase ("Microsoft.Common.Console.FSharp", "UseNetCore30=true")]
		[TestCase ("Microsoft.Common.Library.FSharp-netcoreapp", "UseNetCore30=true;Framework=netcoreapp3.0")]
		[TestCase ("Microsoft.Test.xUnit.FSharp", "UseNetCore30=true")]
		[TestCase ("Microsoft.Test.MSTest.FSharp", "UseNetCore30=true")]

		[TestCase ("Microsoft.Common.Console.VisualBasic", "UseNetCore30=true")]
		[TestCase ("Microsoft.Common.Library.VisualBasic-netcoreapp", "UseNetCore30=true;Framework=netcoreapp3.0")]
		[TestCase ("Microsoft.Test.xUnit.VisualBasic", "UseNetCore30=true")]
		[TestCase ("Microsoft.Test.MSTest.VisualBasic", "UseNetCore30=true")]

		// NUnit3 templates come with .NET Core 2.2, but they only support .NET Core 2.1 framework
		[TestCase ("NUnit3.DotNetNew.Template.CSharp", "UseNetCore30=true")]
		[TestCase ("NUnit3.DotNetNew.Template.FSharp", "UseNetCore30=true")]
		[TestCase ("NUnit3.DotNetNew.Template.VisualBasic", "UseNetCore30=true")]
		public async Task NetCore30 (string templateId, string parameters)
		{
			if (!IsDotNetCoreSdk30Installed ()) {
				Assert.Ignore (".NET Core 3.0 SDK is not installed - required by project template.");
			}

			await CreateFromTemplateAndBuild ("NetCore30", templateId, parameters);
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

			await CreateFromTemplateAndBuild ("AspNetCore1x", templateId, parameters);
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

			await CreateFromTemplateAndBuild ("NetCore2x", templateId, parameters);
		}

		[TestCase ("Microsoft.Web.Empty.CSharp", "UseNetCore21=true", true)]
		[TestCase ("Microsoft.Web.Empty.FSharp", "UseNetCore21=true", true)]
		[TestCase ("Microsoft.Web.Mvc.CSharp", "UseNetCore21=true", true)]
		[TestCase ("Microsoft.Web.Mvc.FSharp", "UseNetCore21=true", true)]
		[TestCase ("Microsoft.Web.RazorPages.CSharp", "UseNetCore21=true", true)]
		[TestCase ("Microsoft.Web.WebApi.CSharp", "UseNetCore21=true", true)]
		[TestCase ("Microsoft.Web.WebApi.FSharp", "UseNetCore21=true", true)]
		[TestCase ("Microsoft.Web.Razor.Library.CSharp", "UseNetCore21=true", false)]
		[TestCase ("Microsoft.Web.Spa.Angular.CSharp", "UseNetCore21=true", true)]
		[TestCase ("Microsoft.Web.Spa.React.CSharp", "UseNetCore21=true", true)]
		[TestCase ("Microsoft.Web.Spa.ReactRedux.CSharp", "UseNetCore21=true", true)]
		public async Task AspNetCore21 (string templateId, string parameters, bool checkExecutionTargets)
		{
			if (!IsDotNetCoreSdk21Installed ()) {
				Assert.Ignore (".NET Core 2.1 SDK is not installed - required by project template.");
			}

			if (templateId.Contains("Microsoft.Web.Spa") &&
				!TemplateDependencyChecker.Check (TemplateDependency.Node)) {
				Assert.Ignore ("Node is not installed - required by project template");
			}

			await CreateFromTemplateAndBuild ("NetCore2x", templateId, parameters, CheckAspNetCoreNestingRules, checkExecutionTargets);
		}

		[TestCase ("Microsoft.Web.Empty.CSharp", "UseNetCore22=true", true)]
		[TestCase ("Microsoft.Web.Empty.FSharp", "UseNetCore22=true", true)]
		[TestCase ("Microsoft.Web.Mvc.CSharp", "UseNetCore22=true", true)]
		[TestCase ("Microsoft.Web.Mvc.FSharp", "UseNetCore22=true", true)]
		[TestCase ("Microsoft.Web.RazorPages.CSharp", "UseNetCore22=true", true)]
		[TestCase ("Microsoft.Web.WebApi.CSharp", "UseNetCore22=true", true)]
		[TestCase ("Microsoft.Web.WebApi.FSharp", "UseNetCore22=true", true)]
		[TestCase ("Microsoft.Web.Razor.Library.CSharp", "UseNetCore22=true", false)]
		[TestCase ("Microsoft.Web.Spa.Angular.CSharp", "UseNetCore22=true", true)]
		[TestCase ("Microsoft.Web.Spa.React.CSharp", "UseNetCore22=true", true)]
		[TestCase ("Microsoft.Web.Spa.ReactRedux.CSharp", "UseNetCore22=true", true)]
		public async Task AspNetCore22 (string templateId, string parameters, bool checkExecutionTargets)
		{
			if (!IsDotNetCoreSdk22Installed ()) {
				Assert.Ignore (".NET Core 2.2 SDK is not installed - required by project template.");
			}

			if (templateId.Contains ("Microsoft.Web.Spa") &&
				!TemplateDependencyChecker.Check (TemplateDependency.Node)) {
				Assert.Ignore ("Node is not installed - required by project template");
			}

			await CreateFromTemplateAndBuild ("NetCore2x", templateId, parameters, CheckAspNetCoreNestingRules, checkExecutionTargets);
		}

		[TestCase ("Microsoft.Web.Empty.CSharp", "UseNetCore30=true", true)]
		[TestCase ("Microsoft.Web.Empty.FSharp", "UseNetCore30=true", true)]
		[TestCase ("Microsoft.Web.Mvc.CSharp", "UseNetCore30=true", true)]
		[TestCase ("Microsoft.Web.Mvc.FSharp", "UseNetCore30=true", true)]
		[TestCase ("Microsoft.Web.RazorPages.CSharp", "UseNetCore30=true", true)]
		[TestCase ("Microsoft.Web.WebApi.CSharp", "UseNetCore30=true", true)]
		[TestCase ("Microsoft.Web.WebApi.FSharp", "UseNetCore30=true", true)]
		[TestCase ("Microsoft.Web.Razor.Library.CSharp", "UseNetCore30=true", false)]
		[TestCase ("Microsoft.Web.Spa.Angular.CSharp", "UseNetCore30=true", true)]
		[TestCase ("Microsoft.Web.Spa.React.CSharp", "UseNetCore30=true", true)]
		[TestCase ("Microsoft.Web.Spa.ReactRedux.CSharp", "UseNetCore30=true", true)]
		[TestCase ("Microsoft.Worker.Empty.CSharp", "UseNetCore30=true", false)]
		public async Task AspNetCore30 (string templateId, string parameters, bool checkExecutionTargets)
		{
			if (!IsDotNetCoreSdk30Installed ()) {
				Assert.Ignore (".NET Core 3.0 SDK is not installed - required by project template.");
			}

			if (templateId.Contains ("Microsoft.Web.Spa") &&
				!TemplateDependencyChecker.Check (TemplateDependency.Node)) {
				Assert.Ignore ("Node is not installed - required by project template");
			}

			await CreateFromTemplateAndBuild ("NetCore30", templateId, parameters, CheckAspNetCoreNestingRules, checkExecutionTargets);
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

		static bool IsDotNetCoreSdk30Installed ()
		{
			return DotNetCoreSdk.Versions.Any (version => version.Major == 3 && version.Minor == 0);
		}

		static async Task CreateFromTemplateAndBuild (string basename, string templateId, string parameters, Action<Solution> preBuildChecks = null, bool checkExecutionTargets = false)
		{
			using (var ptt = new DotNetCoreProjectTemplateTest (basename, templateId)) {

				foreach (var templateParameter in TemplateParameter.CreateParameters (parameters)) {
					ptt.Config.Parameters [templateParameter.Name] = templateParameter.Value;
				}

				var template = await ptt.CreateAndBuild (preBuildChecks);

				CheckProjectTypeGuids (ptt.Solution, GetProjectTypeGuid (template));
				// Blacklist library projects, which don't get any execution target
				if (checkExecutionTargets) {
					foreach (var p in ptt.Solution.GetAllProjects ().OfType<DotNetProject> ()) {
						foreach (var config in p.Configurations) {
							var targets = p.GetExecutionTargets (config.Selector)?.ToList () ?? new List<ExecutionTarget> ();
							if (System.IO.Directory.Exists ("/Applications/Safari.app"))
								Assert.True (targets.Any (x => x.Name.Contains ("Safari")), $"Configuration {config.Name} didn't contain Safari");
							if (System.IO.Directory.Exists ("/Applications/Google Chrome.app"))
								Assert.True (targets.Any (x => x.Name.Contains ("Google Chrome")), $"Configuration {config.Name} didn't contain Chrome");
						}
					}
				}
			}
		}

		static void CheckAspNetCoreNestingRules (Solution sol)
		{
			foreach (var p in sol.GetAllProjects ()) {
				foreach (var si in p.Files) {
					if (si.DependentChildren != null && si.DependentChildren.Count > 0) {
						foreach (var c in si.DependentChildren) {
							Assert.True (FileNestingService.GetParentFile (c) == si);
						}
					}
				}
			}
		}

		static void CheckProjectTypeGuids (Solution solution, string expectedProjectTypeGuid)
		{
			foreach (Project project in solution.GetAllProjects ()) {
				Assert.AreEqual (expectedProjectTypeGuid, project.TypeGuid, $"For project: {project.Name} Expected Type is {expectedProjectTypeGuid} and is returning {project.TypeGuid}");
			}
		}

		static string GetProjectTypeGuid (SolutionTemplate template)
		{
			string language = ProjectTemplateTest.GetLanguage (template.Id);
			if (language == "F#")
				return "{F2A71F9B-5D33-465A-A702-920D77279786}";

			if (language == "VBNet")
				return "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";

			// C#
			return "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
		}
	}
}
