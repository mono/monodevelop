//
// CustomProjectRuleSetTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2020 Microsoft Corporation
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

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.CSharpBinding.Tests
{
	[TestFixture]
	class CustomProjectRuleSetTests : IdeTestBase
	{
		FilePath globalRuleSetFileNameBackup;

		[TestFixtureSetUp]
		public void SetUp ()
		{
			FilePath backupFileName = GlobalRuleSetFileName + "-test-backup";
			File.Copy (GlobalRuleSetFileName, backupFileName, true);
			globalRuleSetFileNameBackup = backupFileName;
		}

		static string GlobalRuleSetFileName {
			get => IdeApp.TypeSystemService.RuleSetManager.GlobalRulesetFileName;
		}

		public override void TearDown ()
		{
			File.Copy (globalRuleSetFileNameBackup, IdeApp.TypeSystemService.RuleSetManager.GlobalRulesetFileName, true);
			File.Delete (globalRuleSetFileNameBackup);
			base.TearDown ();
		}

		[Test]
		public async Task CustomCodeAnalysisRuleSetFile ()
		{
			FilePath solutionFileName = Util.GetSampleProject ("ruleset", "ruleset.sln");
			File.Copy (solutionFileName.ParentDirectory.Combine ("global.ruleset"), GlobalRuleSetFileName, true);

			using (var solution = (MonoDevelop.Projects.Solution)await Ide.Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName)) {
				var project = solution.GetAllProjects ().OfType <DotNetProject> ().Single ();
				var config = (DotNetProjectConfiguration)project.GetConfiguration (ConfigurationSelector.Default);
				var compilationOptions = config.CompilationParameters.CreateCompilationOptions ();
				var diagnosticOptions = compilationOptions.SpecificDiagnosticOptions;

				// Project ruleset options.
				Assert.AreEqual (ReportDiagnostic.Error, diagnosticOptions ["SA1000"]);
				Assert.AreEqual (ReportDiagnostic.Warn, diagnosticOptions ["SA1001"]);
				Assert.AreEqual (ReportDiagnostic.Suppress, diagnosticOptions ["SA1002"]);

				// Global ruleset option which is not overridden by project ruleset.
				Assert.AreEqual (ReportDiagnostic.Error, diagnosticOptions ["SA1003"]);

				// NoWarn set in project file directly should override project ruleset.
				Assert.AreEqual (ReportDiagnostic.Suppress, diagnosticOptions ["SA1600"]);
			}
		}
	}
}
