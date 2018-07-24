//
// ProjectReevaluationTests.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
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
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class ProjectReevaluationTests: TestBase
	{
		[Test ()]
		public async Task ReevaluateLoadSave ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (Project)sol.Items [0];

			int itemAdded = 0;
			int itemRemoved = 0;
			int configAdded = 0;
			int configRemoved = 0;
			bool configsChanged = false;
			bool runConfigsChanged = false;
			bool capabilitiesChanged = false;
			p.ProjectItemAdded += (sender, e) => itemAdded += e.Count;
			p.ProjectItemRemoved += (sender, e) => itemRemoved += e.Count;
			p.ConfigurationAdded += (sender, e) => configAdded++;
			p.ConfigurationRemoved += (sender, e) => configRemoved++;
			p.ConfigurationsChanged += (sender, e) => configsChanged = true;
			p.RunConfigurationsChanged += (sender, e) => runConfigsChanged = true;
			p.ProjectCapabilitiesChanged += (sender, e) => capabilitiesChanged = true;

			await p.ReevaluateProject (Util.GetMonitor ());

			Assert.AreEqual (0, itemAdded);
			Assert.AreEqual (0, itemRemoved);
			Assert.AreEqual (0, configAdded);
			Assert.AreEqual (0, configRemoved);
			Assert.IsFalse (configsChanged);
			Assert.IsFalse (runConfigsChanged);
			Assert.IsFalse (capabilitiesChanged);

			string projectXml = File.ReadAllText (p.FileName);

			await sol.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (projectXml, File.ReadAllText (p.FileName));

			sol.Dispose ();
		}

		[Test ()]
		public async Task ReevaluateAddRemoveItem ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (Project)sol.Items [0];

			int itemAdded = 0;
			int itemRemoved = 0;
			int configAdded = 0;
			int configRemoved = 0;
			bool configsChanged = false;
			bool runConfigsChanged = false;
			bool capabilitiesChanged = false;
			p.ProjectItemAdded += (sender, e) => itemAdded += e.Count;
			p.ProjectItemRemoved += (sender, e) => itemRemoved += e.Count;
			p.ConfigurationAdded += (sender, e) => configAdded++;
			p.ConfigurationRemoved += (sender, e) => configRemoved++;
			p.ConfigurationsChanged += (sender, e) => configsChanged = true;
			p.RunConfigurationsChanged += (sender, e) => runConfigsChanged = true;
			p.ProjectCapabilitiesChanged += (sender, e) => capabilitiesChanged = true;

			var it = p.MSBuildProject.GetAllItems ().First (i => i.Include == "Program.cs");
			p.MSBuildProject.AddNewItem ("Compile", "foo.cs", it);
			p.MSBuildProject.AddNewItem ("Compile", "bar.cs");
			p.MSBuildProject.RemoveItem (it);

			await p.ReevaluateProject (Util.GetMonitor ());

			Assert.AreEqual (2, itemAdded);
			Assert.AreEqual (1, itemRemoved);
			Assert.AreEqual (0, configAdded);
			Assert.AreEqual (0, configRemoved);
			Assert.IsFalse (configsChanged);
			Assert.IsFalse (runConfigsChanged);
			Assert.IsFalse (capabilitiesChanged);

			Assert.AreEqual (new [] { @"Properties\AssemblyInfo.cs", "foo.cs", "bar.cs" }, p.Files.Select (f => f.Include).ToArray ());
			Assert.AreEqual (new [] { "Compile", "Compile", "Compile" }, p.Files.Select (f => f.BuildAction).ToArray ());

			await sol.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (File.ReadAllText (p.FileName), File.ReadAllText (p.FileName.ChangeName ("ConsoleProject-refresh-saved")));

			sol.Dispose ();
		}

		/// <summary>
		/// Ensure that changes to the project which are not yet saved are handled if a re-evaluation occurs
		/// </summary>
		[Test]
		public async Task Reevaluate_AfterReferenceAddedToProject_BeforeProjectSaved ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (DotNetProject)sol.Items [0];
			Assert.AreEqual (3, p.References.Count);

			var systemXmlLinqRef = ProjectReference.CreateAssemblyReference ("System.Xml.Linq");
			p.References.Add (systemXmlLinqRef);

			var systemNetRef = ProjectReference.CreateAssemblyReference ("System.Net");
			p.References.Add (systemNetRef);

			int itemAdded = 0;
			int itemRemoved = 0;
			int configAdded = 0;
			int configRemoved = 0;
			bool configsChanged = false;
			bool runConfigsChanged = false;
			bool capabilitiesChanged = false;
			p.ProjectItemAdded += (sender, e) => itemAdded += e.Count;
			p.ProjectItemRemoved += (sender, e) => itemRemoved += e.Count;
			p.ConfigurationAdded += (sender, e) => configAdded++;
			p.ConfigurationRemoved += (sender, e) => configRemoved++;
			p.ConfigurationsChanged += (sender, e) => configsChanged = true;
			p.RunConfigurationsChanged += (sender, e) => runConfigsChanged = true;
			p.ProjectCapabilitiesChanged += (sender, e) => capabilitiesChanged = true;

			await p.ReevaluateProject (Util.GetMonitor ());

			Assert.AreEqual (0, itemAdded);
			Assert.AreEqual (0, itemRemoved);
			Assert.AreEqual (0, configAdded);
			Assert.AreEqual (0, configRemoved);
			Assert.IsFalse (configsChanged);
			Assert.IsFalse (runConfigsChanged);
			Assert.IsFalse (capabilitiesChanged);

			Assert.AreEqual (systemXmlLinqRef, p.References.Single (r => r.Include == "System.Xml.Linq"));
			Assert.AreEqual (systemNetRef, p.References.Single (r => r.Include == "System.Net"));
			Assert.AreEqual (5, p.References.Count);

			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (File.ReadAllText (p.FileName), Util.ToSystemEndings (File.ReadAllText (p.FileName + ".reference-added")));

			sol.Dispose ();
		}

		/// <summary>
		/// Ensure that changes to the project which are not yet saved are handled if a re-evaluation occurs.
		/// Similar to the test above but the references are added during the re-evaluation itself. This is to
		/// simulate the references added on the UI thread whilst the ReevaluateProject calls sourceProject.EvaluateAsync.
		/// </summary>
		[Test]
		public async Task Reevaluate_AfterReferenceAddedToProject_BeforeProjectSaved2 ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (DotNetProject)sol.Items [0];
			Assert.AreEqual (3, p.References.Count);

			var fn = new CustomItemNode<AddReferenceOnReevaluateProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				int itemAdded = 0;
				int itemRemoved = 0;
				int configAdded = 0;
				int configRemoved = 0;
				bool configsChanged = false;
				bool runConfigsChanged = false;
				bool capabilitiesChanged = false;
				p.ProjectItemAdded += (sender, e) => itemAdded += e.Count;
				p.ProjectItemRemoved += (sender, e) => itemRemoved += e.Count;
				p.ConfigurationAdded += (sender, e) => configAdded++;
				p.ConfigurationRemoved += (sender, e) => configRemoved++;
				p.ConfigurationsChanged += (sender, e) => configsChanged = true;
				p.RunConfigurationsChanged += (sender, e) => runConfigsChanged = true;
				p.ProjectCapabilitiesChanged += (sender, e) => capabilitiesChanged = true;

				await p.ReevaluateProject (Util.GetMonitor ());

				Assert.AreEqual (1, p.References.Count (r => r.Include == "System.Xml.Linq"));
				Assert.AreEqual (1, p.References.Count (r => r.Include == "System.Net"));
				Assert.AreEqual (5, p.References.Count);

				await p.SaveAsync (Util.GetMonitor ());

				Assert.AreEqual (File.ReadAllText (p.FileName), Util.ToSystemEndings (File.ReadAllText (p.FileName + ".reference-added")));

				Assert.AreEqual (2, itemAdded);
				Assert.AreEqual (0, itemRemoved);
				Assert.AreEqual (0, configAdded);
				Assert.AreEqual (0, configRemoved);
				Assert.IsFalse (configsChanged);
				Assert.IsFalse (runConfigsChanged);
				Assert.IsFalse (capabilitiesChanged);

				sol.Dispose ();

			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}


		/// <summary>
		/// Ensure that changes to the project which are not yet saved are handled if a re-evaluation occurs
		/// </summary>
		[Test]
		public async Task Reevaluate_AfterReferenceRemovedFromProject_BeforeProjectSaved ()
		{
			FilePath oldProjectFile = Util.GetSampleProject ("console-project", "ConsoleProject", "ConsoleProject.csproj.reference-added");
			FilePath projectFile = oldProjectFile.ParentDirectory.Combine ("ConsoleProject-reference-added.csproj");
			File.Move (oldProjectFile, projectFile);

			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile);

			var systemXmlLinqRef = p.References.Single (r => r.Include == "System.Xml.Linq");
			var systemNetRef = p.References.Single (r => r.Include == "System.Net");
			Assert.AreEqual (5, p.References.Count);

			p.References.Remove (systemXmlLinqRef);
			p.References.Remove (systemNetRef);

			int itemAdded = 0;
			int itemRemoved = 0;
			int configAdded = 0;
			int configRemoved = 0;
			bool configsChanged = false;
			bool runConfigsChanged = false;
			bool capabilitiesChanged = false;
			p.ProjectItemAdded += (sender, e) => itemAdded += e.Count;
			p.ProjectItemRemoved += (sender, e) => itemRemoved += e.Count;
			p.ConfigurationAdded += (sender, e) => configAdded++;
			p.ConfigurationRemoved += (sender, e) => configRemoved++;
			p.ConfigurationsChanged += (sender, e) => configsChanged = true;
			p.RunConfigurationsChanged += (sender, e) => runConfigsChanged = true;
			p.ProjectCapabilitiesChanged += (sender, e) => capabilitiesChanged = true;

			await p.ReevaluateProject (Util.GetMonitor ());

			Assert.AreEqual (0, itemAdded);
			Assert.AreEqual (0, itemRemoved);
			Assert.AreEqual (0, configAdded);
			Assert.AreEqual (0, configRemoved);
			Assert.IsFalse (configsChanged);
			Assert.IsFalse (runConfigsChanged);
			Assert.IsFalse (capabilitiesChanged);

			Assert.IsFalse (p.References.Any (r => r.Include == "System.Xml.Linq"));
			Assert.IsFalse (p.References.Any (r => r.Include == "System.Net"));
			Assert.AreEqual (3, p.References.Count);

			await p.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (Util.ToSystemEndings (File.ReadAllText (p.FileName)), File.ReadAllText (p.FileName.ParentDirectory.Combine ("ConsoleProject.csproj")));

			p.Dispose ();
		}

		/// <summary>
		/// Ensure that changes to the project which are not yet saved are handled if a re-evaluation occurs
		/// Similar to the test above but the references are removed during the re-evaluation itself. This is to
		/// simulate the references added on the UI thread whilst the ReevaluateProject calls sourceProject.EvaluateAsync.
		/// </summary>
		[Test]
		public async Task Reevaluate_AfterReferenceRemovedFromProject_BeforeProjectSaved2 ()
		{
			FilePath oldProjectFile = Util.GetSampleProject ("console-project", "ConsoleProject", "ConsoleProject.csproj.reference-added");
			FilePath projectFile = oldProjectFile.ParentDirectory.Combine ("ConsoleProject-reference-added.csproj");
			File.Move (oldProjectFile, projectFile);

			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile);

			var systemXmlLinqRef = p.References.Single (r => r.Include == "System.Xml.Linq");
			var systemNetRef = p.References.Single (r => r.Include == "System.Net");
			Assert.AreEqual (5, p.References.Count);

			var fn = new CustomItemNode<RemoveReferenceOnReevaluateProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			try {
				int itemAdded = 0;
				int itemRemoved = 0;
				int configAdded = 0;
				int configRemoved = 0;
				bool configsChanged = false;
				bool runConfigsChanged = false;
				bool capabilitiesChanged = false;
				p.ProjectItemAdded += (sender, e) => itemAdded += e.Count;
				p.ProjectItemRemoved += (sender, e) => itemRemoved += e.Count;
				p.ConfigurationAdded += (sender, e) => configAdded++;
				p.ConfigurationRemoved += (sender, e) => configRemoved++;
				p.ConfigurationsChanged += (sender, e) => configsChanged = true;
				p.RunConfigurationsChanged += (sender, e) => runConfigsChanged = true;
				p.ProjectCapabilitiesChanged += (sender, e) => capabilitiesChanged = true;

				await p.ReevaluateProject (Util.GetMonitor ());

				Assert.IsFalse (p.References.Any (r => r.Include == "System.Xml.Linq"));
				Assert.IsFalse (p.References.Any (r => r.Include == "System.Net"));
				Assert.AreEqual (3, p.References.Count);

				await p.SaveAsync (Util.GetMonitor ());

				Assert.AreEqual (Util.ToSystemEndings (File.ReadAllText (p.FileName)), File.ReadAllText (p.FileName.ParentDirectory.Combine ("ConsoleProject.csproj")));

				Assert.AreEqual (0, itemAdded);
				Assert.AreEqual (2, itemRemoved);
				Assert.AreEqual (0, configAdded);
				Assert.AreEqual (0, configRemoved);
				Assert.IsFalse (configsChanged);
				Assert.IsFalse (runConfigsChanged);
				Assert.IsFalse (capabilitiesChanged);

				p.Dispose ();

			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
			}
		}


		[Test ()]
		public async Task ReevaluateModifyItem ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (Project)sol.Items [0];

			int itemAdded = 0;
			int itemRemoved = 0;
			int configAdded = 0;
			int configRemoved = 0;
			bool configsChanged = false;
			bool runConfigsChanged = false;
			bool capabilitiesChanged = false;
			p.ProjectItemAdded += (sender, e) => itemAdded += e.Count;
			p.ProjectItemRemoved += (sender, e) => itemRemoved += e.Count;
			p.ConfigurationAdded += (sender, e) => configAdded++;
			p.ConfigurationRemoved += (sender, e) => configRemoved++;
			p.ConfigurationsChanged += (sender, e) => configsChanged = true;
			p.RunConfigurationsChanged += (sender, e) => runConfigsChanged = true;
			p.ProjectCapabilitiesChanged += (sender, e) => capabilitiesChanged = true;

			var file = p.Files.FirstOrDefault (f => f.FilePath.FileName == "Program.cs");
			Assert.IsTrue (file.Visible);

			var it = p.MSBuildProject.GetAllItems ().First (i => i.Include == "Program.cs");
			it.Metadata.SetValue ("Visible", "False");
			it.Metadata.SetValue ("Foo", "Bar");

			await p.ReevaluateProject (Util.GetMonitor ());

			Assert.AreEqual (1, itemAdded);
			Assert.AreEqual (1, itemRemoved);
			Assert.AreEqual (0, configAdded);
			Assert.AreEqual (0, configRemoved);
			Assert.IsFalse (configsChanged);
			Assert.IsFalse (runConfigsChanged);
			Assert.IsFalse (capabilitiesChanged);

			Assert.AreEqual (new [] { @"Properties\AssemblyInfo.cs", "Program.cs" }, p.Files.Select (f => f.Include).ToArray ());

			file = p.Files.FirstOrDefault (f => f.FilePath.FileName == "Program.cs");
			Assert.IsFalse (file.Visible);

			await sol.SaveAsync (Util.GetMonitor ());

			Assert.AreEqual (File.ReadAllText (p.FileName), File.ReadAllText (p.FileName.ChangeName ("ConsoleProject-refresh-item-changed-saved")));

			sol.Dispose ();
		}

		[Test]
		public async Task ReevaluateExistingProjectReferencesAfterLoad ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (DotNetProject)sol.Items [0];

			int itemAdded = 0;
			int itemRemoved = 0;
			p.ProjectItemAdded += (sender, e) => itemAdded += e.Count;
			p.ProjectItemRemoved += (sender, e) => itemRemoved += e.Count;

			await p.ReevaluateProject (Util.GetMonitor ());

			var library1Reference = p.References.First (r => r.Include == @"..\library1\library1.csproj");
			var library2Reference = p.References.First (r => r.Include == @"..\library2\library2.csproj");

			var library1Item = p.Items.OfType<ProjectReference> ().First (r => r.Include == @"..\library1\library1.csproj");
			var library2Item = p.Items.OfType<ProjectReference> ().First (r => r.Include == @"..\library2\library2.csproj");

			Assert.AreEqual (0, itemAdded);
			Assert.AreEqual (0, itemRemoved);
			Assert.AreEqual (p, library1Item.OwnerProject);
			Assert.AreEqual (p, library2Item.OwnerProject);
			Assert.AreSame (library1Reference, library1Item);
			Assert.AreSame (library2Reference, library2Item);

			sol.Dispose ();
		}

		[Test]
		public async Task ReevaluateNewProjectReferencesAfterSave ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (DotNetProject)sol.Items [0];

			// Remove all existing project references and save.
			var existingProjectReferences = p.References.Where (r => r.ReferenceType == ReferenceType.Project).ToArray ();
			p.References.RemoveRange (existingProjectReferences);
			await p.SaveAsync (Util.GetMonitor ());

			// Reload solution.
			sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			p = (DotNetProject)sol.Items [0];

			Assert.IsFalse (p.References.Any (r => r.ReferenceType == ReferenceType.Project));

			// Add reference to library1.
			var library1Project = (DotNetProject)sol.Items.First (item => item.Name == "library1");
			var projectReference = ProjectReference.CreateProjectReference (library1Project);
			p.References.Add (projectReference);
			await p.SaveAsync (Util.GetMonitor ());

			int itemAdded = 0;
			int itemRemoved = 0;
			p.ProjectItemAdded += (sender, e) => itemAdded += e.Count;
			p.ProjectItemRemoved += (sender, e) => itemRemoved += e.Count;

			await p.ReevaluateProject (Util.GetMonitor ());

			var library1Reference = p.References.First (r => r.Include == @"..\library1\library1.csproj");

			var library1Item = p.Items.OfType<ProjectReference> ().First (r => r.Include == @"..\library1\library1.csproj");

			Assert.AreEqual (0, itemAdded);
			Assert.AreEqual (0, itemRemoved);
			Assert.AreEqual (p, library1Item.OwnerProject);
			Assert.AreSame (library1Reference, library1Item);
			Assert.AreEqual (library1Reference, library1Item);

			sol.Dispose ();
		}

		[TestCase ("ConsoleProject-duplicate-reference.csproj", 2)]
		[TestCase ("ConsoleProject-triplicate-reference.csproj", 3)]
		public async Task ReevaluateProjectWithDuplicateReference (string projectName, int expectedReferences)
		{
			string projectFile = Util.GetSampleProject ("console-project", "ConsoleProject", projectName);

			var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile);

			Assert.AreEqual (expectedReferences, p.References.Count (reference => reference.Include == "System.Xml"));

			int itemAdded = 0;
			int itemRemoved = 0;
			p.ProjectItemAdded += (sender, e) => itemAdded += e.Count;
			p.ProjectItemRemoved += (sender, e) => itemRemoved += e.Count;

			// Make sure this does not fail.
			await p.ReevaluateProject (Util.GetMonitor ());

			Assert.AreEqual (expectedReferences, p.References.Count (reference => reference.Include == "System.Xml"));
			Assert.AreEqual (0, itemAdded);
			Assert.AreEqual (0, itemRemoved);

			p.Dispose ();
		}

		/// <summary>
		/// Ensures that the solution's StartupConfiguration still refers to the project's
		/// default configuration after the project is re-evaluated. The StartupConfiguration
		/// was not being refreshed after the project was re-evaluated so the solution's
		/// StartupConfiguration was referring to a different project run configuration than
		/// was stored with the project. This resulted in a new .NET Core project not using
		/// arguments defined for the project's run configuration when running the solution.
		/// </summary>
		[Test]
		public async Task ReevaluateProjectAfterChangingProjectRunConfiguration ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {

				var p = (DotNetProject)sol.Items [0];

				// By default the project's default run configuration will be used as the startup configuration
				// for the solution when the solution loads.
				var startupConfiguration = sol.StartupConfiguration as SingleItemSolutionRunConfiguration;
				Assert.AreEqual (p.RunConfigurations [0], startupConfiguration.RunConfiguration);

				// Re-evaluate the project.
				await p.ReevaluateProject (Util.GetMonitor ());

				// Change the project run configuration.
				var projectRunConfiguration = p.RunConfigurations [0] as AssemblyRunConfiguration;
				projectRunConfiguration.StartArguments = "Test";
				await p.SaveAsync (Util.GetMonitor ());

				// After re-evaluation the solution's startup configuration should be pointing to the
				// refreshed project run configuration.
				startupConfiguration = sol.StartupConfiguration as SingleItemSolutionRunConfiguration;
				var projectRunConfigurationAfterReevaluation = p.RunConfigurations [0] as AssemblyRunConfiguration;
				var startupConfigurationAfterReevaluation = sol.StartupConfiguration as SingleItemSolutionRunConfiguration;
				var startupProjectRunConfiguration = startupConfigurationAfterReevaluation.RunConfiguration as AssemblyRunConfiguration;

				Assert.AreEqual ("Test", startupProjectRunConfiguration.StartArguments);
				Assert.AreEqual (projectRunConfigurationAfterReevaluation, startupConfigurationAfterReevaluation.RunConfiguration);
			}
		}

		class AddReferenceOnReevaluateProjectExtension : DotNetProjectExtension
		{
			protected internal override Task OnReevaluateProject (ProgressMonitor monitor)
			{
				var builder = ImmutableList.CreateBuilder<ProjectItem> ();

				var systemXmlLinq = ProjectReference.CreateAssemblyReference ("System.Xml.Linq");
				var systemNet = ProjectReference.CreateAssemblyReference ("System.Net");

				builder.Add (systemXmlLinq);
				builder.Add (systemNet);

				Project.References.Add (systemXmlLinq);
				Project.References.Add (systemNet);

				Project.itemsAddedDuringReevaluation = builder;

				return base.OnReevaluateProject (monitor);
			}
		}

		class RemoveReferenceOnReevaluateProjectExtension : DotNetProjectExtension
		{
			protected internal override Task OnReevaluateProject (ProgressMonitor monitor)
			{
				var builder = ImmutableList.CreateBuilder<ProjectItem> ();

				var systemXmlLinq = Project.References.Single (r => r.Include == "System.Xml.Linq");
				var systemNet = Project.References.Single (r => r.Include == "System.Net");

				builder.Add (systemXmlLinq);
				builder.Add (systemNet);

				Project.References.Remove (systemXmlLinq);
				Project.References.Remove (systemNet);

				Project.itemsRemovedDuringReevaluation = builder;

				return base.OnReevaluateProject (monitor);
			}
		}
	}
}
