//
// BuildEventsOptionsTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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

using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Ide.Projects.OptionPanels;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.Projects
{
	[TestFixture]
	public class BuildEventsOptionsTests : IdeTestBase
	{
		DotNetProject project;
		BuildEventsOptionsPanel buildOptionsPanel;
		Control control;
		BuildEventsWidget buildOptionsWidget;

		public override void TearDown ()
		{
			base.TearDown ();

			project?.Dispose ();
			buildOptionsPanel?.Dispose ();
			control?.Dispose ();
			buildOptionsWidget?.Dispose ();
		}

		async Task<DotNetProject> LoadConsoleProject (string baseDirectoryName = "console-project")
		{
			var projectFile = Util.GetSampleProject (baseDirectoryName, "ConsoleProject", "ConsoleProject.csproj");
			return (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile);
		}

		async Task<DotNetProject> LoadNetStandardSdkProject (string baseDirectoryName = "netstandard-sdk")
		{
			var projectFile = Util.GetSampleProject (baseDirectoryName, "NetStandard", "NetStandard.csproj");
			return (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile);
		}

		void OpenBuildOptionsPanel ()
		{
			buildOptionsPanel = new BuildEventsOptionsPanel ();
			buildOptionsPanel.Initialize (null, project);
			control = buildOptionsPanel.CreatePanelWidget ();
			var xwtControl = (XwtControl)control;
			buildOptionsWidget = (BuildEventsWidget)xwtControl.Widget;
		}

		[Test]
		public async Task AddPreBuildCommand_PreBuildPropertiesAddedAtEndOfProject ()
		{
			project = await LoadConsoleProject ();
			OpenBuildOptionsPanel ();

			buildOptionsWidget.PreBuildEventText = "prebuild";

			buildOptionsPanel.ApplyChanges ();

			var item = project.MSBuildProject.ChildNodes.Last ();
			var propertyGroup = item as MSBuildPropertyGroup;
			Assert.IsNotNull (propertyGroup);
			Assert.AreEqual ("prebuild", propertyGroup.GetValue ("PreBuildEvent"));
		}

		[Test]
		public async Task AddPostBuildCommand_PostBuildPropertiesAddedAtEndOfProject ()
		{
			project = await LoadConsoleProject ();
			OpenBuildOptionsPanel ();

			buildOptionsWidget.PostBuildEventText = "postbuild";
			string defaultRunPostBuildEvent = buildOptionsWidget.SelectedRunPostBuildEvent;
			buildOptionsWidget.SelectedRunPostBuildEvent = "Always";

			buildOptionsPanel.ApplyChanges ();

			var mainPropertyGroup = project.MSBuildProject.GetGlobalPropertyGroup ();
			var item = project.MSBuildProject.ChildNodes.Last ();
			var propertyGroup = item as MSBuildPropertyGroup;
			Assert.IsNotNull (propertyGroup);
			Assert.AreEqual ("postbuild", propertyGroup.GetValue ("PostBuildEvent"));
			Assert.AreEqual ("Always", mainPropertyGroup.GetValue ("RunPostBuildEvent"));
			Assert.AreEqual ("OnBuildSuccess", defaultRunPostBuildEvent);
		}

		[Test]
		public async Task NothingChanged_NoBuildEventsAdded ()
		{
			project = await LoadConsoleProject ();
			OpenBuildOptionsPanel ();

			int originalPropertyGroupCount = project.MSBuildProject.PropertyGroups.Count ();
			buildOptionsPanel.ApplyChanges ();

			var mainPropertyGroup = project.MSBuildProject.GetGlobalPropertyGroup ();
			Assert.IsFalse (mainPropertyGroup.HasProperty ("RunPostBuildEvent"));
			Assert.AreEqual (originalPropertyGroupCount, project.MSBuildProject.PropertyGroups.Count ());
		}

		[Test]
		public async Task RunPostBuildEventChanged_ProjectUpdated ()
		{
			project = await LoadConsoleProject ();
			OpenBuildOptionsPanel ();

			buildOptionsWidget.SelectedRunPostBuildEvent = "OnOutputUpdated";
			buildOptionsPanel.ApplyChanges ();

			var mainPropertyGroup = project.MSBuildProject.GetGlobalPropertyGroup ();
			Assert.AreEqual ("OnOutputUpdated", mainPropertyGroup.GetValue ("RunPostBuildEvent"));
		}

		[Test]
		public async Task OpenProjectWithExistingPreAndPostBuildEvents_InformationShownInOptionsPanel ()
		{
			project = await LoadConsoleProject ("build-events");
			OpenBuildOptionsPanel ();

			Assert.AreEqual ("Always", buildOptionsWidget.SelectedRunPostBuildEvent);
			Assert.AreEqual ("prebuild", buildOptionsWidget.PreBuildEventText);
			Assert.AreEqual ("postbuild", buildOptionsWidget.PostBuildEventText);
		}

		[Test]
		public async Task OpenProjectWithExistingPreAndPostBuildEvents_ChangeEvents ()
		{
			project = await LoadConsoleProject ("build-events");
			OpenBuildOptionsPanel ();

			buildOptionsWidget.SelectedRunPostBuildEvent = "OnOutputUpdated";
			buildOptionsWidget.PreBuildEventText = "prebuild-updated";
			buildOptionsWidget.PostBuildEventText = "postbuild-updated";

			buildOptionsPanel.ApplyChanges ();

			var mainPropertyGroup = project.MSBuildProject.GetGlobalPropertyGroup ();
			var postBuildPropertyGroup = project.MSBuildProject.ChildNodes.Last () as MSBuildPropertyGroup;
			var preBuildPropertyGroup = project.MSBuildProject.ChildNodes [project.MSBuildProject.ChildNodes.Count - 2] as MSBuildPropertyGroup;
			Assert.IsNotNull (postBuildPropertyGroup);
			Assert.IsNotNull (preBuildPropertyGroup);
			Assert.AreEqual ("postbuild-updated", postBuildPropertyGroup.GetValue ("PostBuildEvent"));
			Assert.AreEqual ("prebuild-updated", preBuildPropertyGroup.GetValue ("PreBuildEvent"));
			Assert.AreEqual ("OnOutputUpdated", mainPropertyGroup.GetValue ("RunPostBuildEvent"));
		}

		[Test]
		public async Task OpenSdkStyleProjectWithExistingPreAndPostBuildEvents_InformationShownInOptionsPanel ()
		{
			project = await LoadNetStandardSdkProject ("build-events-sdk");
			OpenBuildOptionsPanel ();

			Assert.AreEqual ("Always", buildOptionsWidget.SelectedRunPostBuildEvent);
			Assert.AreEqual ("prebuild", buildOptionsWidget.PreBuildEventText);
			Assert.AreEqual ("postbuild", buildOptionsWidget.PostBuildEventText);
		}

		[Test]
		public async Task AddPreBuildCommand_SdkStyleProject_PreBuildTargetAddedAtEndOfProject ()
		{
			project = await LoadNetStandardSdkProject ();
			OpenBuildOptionsPanel ();

			buildOptionsWidget.PreBuildEventText = "prebuild";

			buildOptionsPanel.ApplyChanges ();

			var item = project.MSBuildProject.ChildNodes.Last ();
			var target = item as MSBuildTarget;
			Assert.IsNotNull (target);
			Assert.AreEqual ("PreBuild", target.Name);
			Assert.AreEqual ("PreBuildEvent", target.BeforeTargets);

			var task = target.Tasks.Single () as MSBuildExecTask;
			Assert.AreEqual ("prebuild", task.Command);
		}

		[Test]
		public async Task AddPostBuildCommand_SdkStyleProject_PostBuildTargetAddedAtEndOfProject ()
		{
			project = await LoadNetStandardSdkProject ();
			OpenBuildOptionsPanel ();

			buildOptionsWidget.PostBuildEventText = "postbuild";
			buildOptionsWidget.SelectedRunPostBuildEvent = "Always";

			buildOptionsPanel.ApplyChanges ();

			var mainPropertyGroup = project.MSBuildProject.GetGlobalPropertyGroup ();
			var item = project.MSBuildProject.ChildNodes.Last ();
			var target = item as MSBuildTarget;
			Assert.IsNotNull (target);
			Assert.AreEqual ("PostBuild", target.Name);
			Assert.AreEqual ("PostBuildEvent", target.AfterTargets);

			var task = target.Tasks.Single () as MSBuildExecTask;
			Assert.AreEqual ("postbuild", task.Command);
			Assert.AreEqual ("Always", mainPropertyGroup.GetValue ("RunPostBuildEvent"));
		}

		[Test]
		public async Task OpenSdkProjectWithExistingPreAndPostBuildEvents_ChangeEvents ()
		{
			project = await LoadNetStandardSdkProject ("build-events-sdk");
			OpenBuildOptionsPanel ();

			buildOptionsWidget.SelectedRunPostBuildEvent = "OnOutputUpdated";
			buildOptionsWidget.PreBuildEventText = "prebuild-updated";
			buildOptionsWidget.PostBuildEventText = "postbuild-updated";

			buildOptionsPanel.ApplyChanges ();

			var mainPropertyGroup = project.MSBuildProject.GetGlobalPropertyGroup ();
			var postBuildTarget = project.MSBuildProject.ChildNodes.Last () as MSBuildTarget;
			var preBuildTarget = project.MSBuildProject.ChildNodes [project.MSBuildProject.ChildNodes.Count - 2] as MSBuildTarget;
			Assert.IsNotNull (postBuildTarget);
			Assert.IsNotNull (preBuildTarget);

			var preBuildTask = preBuildTarget.Tasks.Single () as MSBuildExecTask;
			Assert.AreEqual ("prebuild-updated", preBuildTask.Command);

			var postBuildTask = postBuildTarget.Tasks.Single () as MSBuildExecTask;
			Assert.AreEqual ("postbuild-updated", postBuildTask.Command);
			Assert.AreEqual ("OnOutputUpdated", mainPropertyGroup.GetValue ("RunPostBuildEvent"));
		}

		[Test]
		public async Task OpenSdkProjectWithExistingPreAndPostBuildTarget_NoExecTasks_ChangeEvents ()
		{
			project = await LoadNetStandardSdkProject ("build-events-sdk");
			var postBuildTarget = project.MSBuildProject.ChildNodes.Last () as MSBuildTarget;
			var preBuildTarget = project.MSBuildProject.ChildNodes [project.MSBuildProject.ChildNodes.Count - 2] as MSBuildTarget;
			var preBuildTask = preBuildTarget.Tasks.Single () as MSBuildExecTask;
			var postBuildTask = postBuildTarget.Tasks.Single () as MSBuildExecTask;
			postBuildTarget.RemoveTask (postBuildTask);
			preBuildTarget.RemoveTask (preBuildTask);
			await project.SaveAsync (Util.GetMonitor ());
			Assert.AreEqual (0, postBuildTarget.Tasks.Count ());
			Assert.AreEqual (0, preBuildTarget.Tasks.Count ());

			OpenBuildOptionsPanel ();

			buildOptionsWidget.SelectedRunPostBuildEvent = "OnOutputUpdated";
			buildOptionsWidget.PreBuildEventText = "prebuild-updated";
			buildOptionsWidget.PostBuildEventText = "postbuild-updated";

			buildOptionsPanel.ApplyChanges ();

			var mainPropertyGroup = project.MSBuildProject.GetGlobalPropertyGroup ();
			postBuildTarget = project.MSBuildProject.ChildNodes.Last () as MSBuildTarget;
			preBuildTarget = project.MSBuildProject.ChildNodes [project.MSBuildProject.ChildNodes.Count - 2] as MSBuildTarget;
			Assert.IsNotNull (postBuildTarget);
			Assert.IsNotNull (preBuildTarget);

			preBuildTask = preBuildTarget.Tasks.Single () as MSBuildExecTask;
			Assert.AreEqual ("prebuild-updated", preBuildTask.Command);

			postBuildTask = postBuildTarget.Tasks.Single () as MSBuildExecTask;
			Assert.AreEqual ("postbuild-updated", postBuildTask.Command);
			Assert.AreEqual ("OnOutputUpdated", mainPropertyGroup.GetValue ("RunPostBuildEvent"));
		}
	}
}
