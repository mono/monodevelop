//
// ProjectTemplateTest.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2018 Microsoft Inc. (http://www.microsoft.com)
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
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using UnitTests;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Projects;
using NUnit.Framework;
using MonoDevelop.Ide.Templates;
using System;

namespace IdeUnitTests
{
	public class ProjectTemplateTest : IDisposable
	{
		static TemplatingService templatingService;
		string templateId;

		public Solution Solution { get; private set; }

		public FilePath SolutionDirectory { get; private set; }

		public string ProjectName { get; private set; }

		public NewProjectConfiguration Config { get; private set; }

		public ProjectTemplateTest (string basename, string templateId)
		{
			if (templatingService == null) {
				templatingService = new TemplatingService ();
			}

			if (!IdeApp.IsInitialized) {
				IdeApp.Initialize (Util.GetMonitor ());
			}

			this.templateId = templateId;

			SolutionDirectory = Util.CreateTmpDir (basename);
			CreateNuGetConfigFile (SolutionDirectory);
			ProjectName = GetProjectName (templateId);

			Config = new NewProjectConfiguration {
				CreateSolution = true,
				CreateProjectDirectoryInsideSolutionDirectory = true,
				CreateGitIgnoreFile = false,
				UseGit = false,
				Location = SolutionDirectory,
				ProjectName = ProjectName,
				SolutionName = ProjectName
			};

			Directory.CreateDirectory (Config.ProjectLocation);
		}

		public async Task<SolutionTemplate> CreateAndBuild ()
		{
			var template = FindTemplate ();
			var result = await templatingService.ProcessTemplate (template, Config, null);

			Solution = result.WorkspaceItems.FirstOrDefault () as Solution;
			await Solution.SaveAsync (Util.GetMonitor ());

			// RestoreDisableParallel prevents parallel restores which sometimes cause
			// the restore to fail on Mono.
			RunMSBuild ($"/t:Restore /p:RestoreDisableParallel=true \"{Solution.FileName}\"");
			RunMSBuild ($"/t:Build \"{Solution.FileName}\"");

			return template;
		}

		void RunMSBuild (string arguments)
		{
			var process = new Process ();
			process.StartInfo = new ProcessStartInfo ("msbuild", arguments) {
				RedirectStandardOutput = true,
				UseShellExecute = false
			};
			process.Start ();
			var standardError = $"Error: {process.StandardOutput.ReadToEnd ()}";

			Assert.IsTrue (process.WaitForExit (240000), "Timed out waiting for MSBuild.");
			Assert.AreEqual (0, process.ExitCode, $"msbuild {arguments} failed. Exit code: {process.ExitCode}. {standardError}");
		}

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

		static string GetProjectName (string templateId)
		{
			return templateId.Replace ("Microsoft.Test.", "")
				.Replace ("Microsoft.Common.", "")
				.Replace ("Microsoft.", "")
				.Replace (".", "");
		}

		public static string GetLanguage (string templateId)
		{
			if (templateId.Contains ("FSharp")) {
				return "F#";
			}
			if (templateId.Contains ("VisualBasic")) {
				return "VBNet";
			}

			return "C#";
		}

		SolutionTemplate FindTemplate ()
		{
			var categories = templatingService
				.GetProjectTemplateCategories (t => t.Id == templateId)
				.ToList ();

			var templates = categories.First ()
				.Categories.First ()
				.Categories.First ()
				.Templates.ToList ();

			var template = templates.Single ();

			string language = GetLanguage (templateId);

			return template.GetTemplate (language, Config.Parameters);
		}

		public void Dispose ()
		{
			Solution?.Dispose ();
			Solution = null;
		}
	}
}
