//
// FlavorMigration.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Linq;
using NUnit.Framework;
using UnitTests;
using System.Threading.Tasks;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core;
using MonoDevelop.Projects.MSBuild;
using MonoDevelop.Core.ProgressMonitoring;
using System.Collections.Generic;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class FlavorMigration: TestBase
	{
		SolutionItemExtensionNode migrator, newFlavor;
		SimpleMigrator migrationHandler;

		async Task<SolutionItem> LoadProject (CustomProjectLoadProgressMonitor m)
		{
			string solFile = Util.GetSampleProject ("flavor-migration", "ConsoleProject.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (m, solFile);
			return sol.GetAllItems<SolutionItem>().FirstOrDefault ();
		}

		[TestFixtureSetUp]
		public void MigrationSetup ()
		{
			migrationHandler = new SimpleMigrator ();

			migrator = new CustomItemNode<ObsoleteFlavor> () {
				Guid = "{D049D6DC-2C66-40ED-8249-2DB930ACA0B4}",
				IsMigrationRequired = false,
				MigrationHandler = migrationHandler
			};
			newFlavor = new CustomItemNode<NewFlavor> () {
				Guid = "{466CB615-7798-440F-9326-B783655656F0}"
			};
			WorkspaceObject.RegisterCustomExtension (migrator);
			WorkspaceObject.RegisterCustomExtension (newFlavor);
		}

		[TestFixtureTearDown]
		public void MigrationTearDown ()
		{
			WorkspaceObject.UnregisterCustomExtension (migrator);
			WorkspaceObject.UnregisterCustomExtension (newFlavor);
		}

		[Test]
		public async Task OptionalMigration_NoPrompt_MonitorIgnore ()
		{
			// It should not migrate
			migrationHandler.CanPromptForMigrationResult = false;
			var m = new CustomProjectLoadProgressMonitor { ShouldMigrateValue = MigrationType.Ignore };
			var p = await LoadProject (m);
			Assert.IsInstanceOf<Project> (p);
			Assert.IsFalse (((Project)p).HasFlavor<NewFlavor> ());
			Assert.IsTrue (((Project)p).HasFlavor<ObsoleteFlavor> ());
			p.Dispose ();
		}

		[Test]
		public async Task OptionalMigration_NoPrompt_MonitorMigrate ()
		{
			// It should migrate
			migrationHandler.CanPromptForMigrationResult = false;
			var m = new CustomProjectLoadProgressMonitor { ShouldMigrateValue = MigrationType.Migrate };
			var p = await LoadProject (m);
			Assert.IsInstanceOf<Project> (p);
			Assert.IsTrue (((Project)p).HasFlavor<NewFlavor> ());
			Assert.IsFalse (((Project)p).HasFlavor<ObsoleteFlavor> ());
			p.Dispose ();
		}

		[Test]
		public async Task OptionalMigration_PromptIgnore_MonitorMigrate ()
		{
			// It should not migrate
			migrationHandler.CanPromptForMigrationResult = true;
			migrationHandler.PromptForMigrationResult = MigrationType.Ignore;
			var m = new CustomProjectLoadProgressMonitor { ShouldMigrateValue = MigrationType.Migrate };
			var p = await LoadProject (m);
			Assert.IsInstanceOf<Project> (p);
			Assert.IsFalse (((Project)p).HasFlavor<NewFlavor> ());
			Assert.IsTrue (((Project)p).HasFlavor<ObsoleteFlavor> ());
			p.Dispose ();
		}

		[Test]
		public async Task OptionalMigration_PromptMigrate_MonitorMigrate ()
		{
			// It should migrate
			migrationHandler.CanPromptForMigrationResult = true;
			migrationHandler.PromptForMigrationResult = MigrationType.Migrate;
			var m = new CustomProjectLoadProgressMonitor { ShouldMigrateValue = MigrationType.Migrate };
			var p = await LoadProject (m);
			Assert.IsInstanceOf<Project> (p);
			Assert.IsTrue (((Project)p).HasFlavor<NewFlavor> ());
			Assert.IsFalse (((Project)p).HasFlavor<ObsoleteFlavor> ());
			p.Dispose ();
		}

		[Test]
		public async Task OptionalMigration_PromptMigrate_MonitorIgnore ()
		{
			// It should not migrate
			migrationHandler.CanPromptForMigrationResult = true;
			migrationHandler.PromptForMigrationResult = MigrationType.Migrate;
			var m = new CustomProjectLoadProgressMonitor { ShouldMigrateValue = MigrationType.Ignore };
			var p = await LoadProject (m);
			Assert.IsInstanceOf<Project> (p);
			Assert.IsTrue (((Project)p).HasFlavor<NewFlavor> ());
			Assert.IsFalse (((Project)p).HasFlavor<ObsoleteFlavor> ());
			p.Dispose ();
		}

		[Test]
		public async Task MandatoryMigration_NoPrompt_MonitorIgnore ()
		{
			// Project load should fail
			migrationHandler.CanPromptForMigrationResult = false;
			migrator.IsMigrationRequired = true;
			try {
				var m = new CustomProjectLoadProgressMonitor { ShouldMigrateValue = MigrationType.Ignore };
				var p = await LoadProject (m);
				Assert.IsInstanceOf<UnknownSolutionItem> (p);
				p.Dispose ();
			} finally {
				migrator.IsMigrationRequired = false;
			}
		}

		[Test]
		public async Task MandatoryMigration_NoPrompt_MonitorMigrate ()
		{
			// It should migrate
			migrationHandler.CanPromptForMigrationResult = false;
			migrator.IsMigrationRequired = true;
			try {
				var m = new CustomProjectLoadProgressMonitor { ShouldMigrateValue = MigrationType.Migrate };
				var p = await LoadProject (m);
				Assert.IsInstanceOf<Project> (p);
				Assert.IsTrue (((Project)p).HasFlavor<NewFlavor> ());
				Assert.IsFalse (((Project)p).HasFlavor<ObsoleteFlavor> ());
				p.Dispose ();
			} finally {
				migrator.IsMigrationRequired = false;
			}
		}

		[Test]
		public async Task MandatoryMigration_PromptMigrate_MonitorIgnore ()
		{
			// It should migrate
			migrationHandler.CanPromptForMigrationResult = true;
			migrationHandler.PromptForMigrationResult = MigrationType.Migrate;
			migrator.IsMigrationRequired = true;
			try {
				var m = new CustomProjectLoadProgressMonitor { ShouldMigrateValue = MigrationType.Ignore };
				var p = await LoadProject (m);
				Assert.IsTrue (((Project)p).HasFlavor<NewFlavor> ());
				Assert.IsFalse (((Project)p).HasFlavor<ObsoleteFlavor> ());
				p.Dispose ();
			} finally {
				migrator.IsMigrationRequired = false;
			}
		}
		[Test]
		public async Task MandatoryMigration_PromptIgnore_MonitorMigrate ()
		{
			// Project load should fail
			migrationHandler.CanPromptForMigrationResult = true;
			migrationHandler.PromptForMigrationResult = MigrationType.Ignore;
			migrator.IsMigrationRequired = true;
			try {
				var m = new CustomProjectLoadProgressMonitor { ShouldMigrateValue = MigrationType.Migrate };
				var p = await LoadProject (m);
				Assert.IsInstanceOf<UnknownSolutionItem> (p);
				p.Dispose ();
			} finally {
				migrator.IsMigrationRequired = false;
			}
		}

		[Test]
		public async Task MigrationFails ()
		{
			// Project load should fail
			migrationHandler.CanPromptForMigrationResult = false;
			try {
				migrationHandler.MigrationResult = false;
				var m = new CustomProjectLoadProgressMonitor { ShouldMigrateValue = MigrationType.Migrate };
				var p = await LoadProject (m);
				Assert.IsInstanceOf<UnknownSolutionItem> (p);
				p.Dispose ();
			} finally {
				migrationHandler.MigrationResult = true;
			}
		}
	}

	class CustomItemNode<T>: SolutionItemExtensionNode where T:new()
	{
		public override object CreateInstance ()
		{
			return new T ();
		}
	}

	class SimpleMigrator: ProjectMigrationHandler
	{
		public MigrationType PromptForMigrationResult { get; set; }
		public string[] FilesToBackupResult { get; set; }
		public bool CanPromptForMigrationResult { get; set; }
		public bool MigrationResult { get; set; }

		public SimpleMigrator ()
		{
			MigrationResult = true;
		}

		public override Task<bool> Migrate (ProjectLoadProgressMonitor monitor, MSBuildProject project, string fileName, string language)
		{
			project.RemoveProjectTypeGuid ("{D049D6DC-2C66-40ED-8249-2DB930ACA0B4}");
			project.AddProjectTypeGuid ("{466CB615-7798-440F-9326-B783655656F0}");
			return Task.FromResult (MigrationResult);
		}

		public override bool CanPromptForMigration {
			get {
				return CanPromptForMigrationResult;
			}
		}

		public override IEnumerable<string> FilesToBackup (string filename)
		{
			return FilesToBackupResult ?? new string[0];
		}

		public override Task<MigrationType> PromptForMigration (ProjectLoadProgressMonitor monitor, MSBuildProject project, string fileName, string language)
		{
			return Task.FromResult (PromptForMigrationResult);
		}
	}

	class ObsoleteFlavor: ProjectExtension
	{
	}

	class NewFlavor: ProjectExtension
	{
	}

	class CustomProjectLoadProgressMonitor : ProjectLoadProgressMonitor
	{
		public MigrationType ShouldMigrateValue { get; set; }

		public CustomProjectLoadProgressMonitor ()
		{
			AddFollowerMonitor (new ConsoleProgressMonitor ());
		}

		public override MigrationType ShouldMigrateProject ()
		{
			return ShouldMigrateValue;
		}
	}
}

