//
// DotNetMigrate.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
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
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Projects;
using MonoDevelop.PackageManagement.Commands;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore
{
	// Put better message on .xproj projects to explain user what can be done
	class ProjXNodeBuilderExtension : NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof (UnknownProject).IsAssignableFrom (dataType);
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			if (!DotNetMigrateCommandHandler.IsMigratableProject (dataObject as UnknownProject))
				return;

			nodeInfo.StatusMessage = GettextCatalog.GetString ("Project format is not supported by {0}.\nUse 'Migrate to New Format' command on solution or single project to migrate to format supported by {0}.", BrandingService.ApplicationName);
		}
	}

	class DotNetMigrateCommandHandler : CommandHandler
	{
		public static bool IsMigratableProject (UnknownProject project)
		{
			if (project == null)
				return false;
			if (!project.FileName.HasExtension (".xproj"))
				return false;
			return File.Exists (Path.Combine (Path.GetDirectoryName (project.FileName), "project.json"));
		}

		async Task<bool> Migrate (UnknownProject project, Solution solution, ProgressMonitor monitor)
		{
			FilePath projectFile;
			string migrationFile;
			if (project == null) {
				projectFile = null;
				migrationFile = solution.FileName;
			} else {
				projectFile = project.FileName;
				migrationFile = Path.Combine (Path.GetDirectoryName (projectFile), "project.json");
			}
			if (DotNetCoreRuntime.IsMissing) {
				monitor.ReportError (GettextCatalog.GetString (".NET Core is not installed"));
				return false;
			}

			var process = Runtime.ProcessService.StartProcess (
				DotNetCoreRuntime.FileName,
				$"migrate \"{migrationFile}\"",
				Path.GetDirectoryName (migrationFile),
				monitor.Log,
				monitor.Log,
				null);

			await process.Task;

			if (process.ExitCode > 0) {
				monitor.ReportError (GettextCatalog.GetString ("An unspecified error occurred while running '{0}'", $"dotnet migrate({process.ExitCode})"));
				return false;
			}

			if (project != null) {
				string newProjectFile;
				if (File.Exists (Path.ChangeExtension (projectFile, ".csproj")))
					newProjectFile = Path.ChangeExtension (projectFile, ".csproj");
				else if (File.Exists (Path.ChangeExtension (projectFile, ".fsproj")))
					newProjectFile = Path.ChangeExtension (projectFile, ".fsproj");
				else if (File.Exists (Path.ChangeExtension (projectFile, ".vbproj")))
					newProjectFile = Path.ChangeExtension (projectFile, ".vbproj");
				else {
					monitor.ReportError (GettextCatalog.GetString ("Migrated project file not found."));
					return false;
				}
				var newProject = await project.ParentFolder.AddItem (monitor, newProjectFile);
				project.ParentFolder.Items.Remove (project);
				await newProject.ParentSolution.SaveAsync (monitor);

				RestorePackagesInProjectHandler.Run ((DotNetProject)newProject);
			} else {
				solution.NeedsReload = true;
				FileService.NotifyFileChanged (solution.FileName);
			}
			return true;
		}

		protected override async void Run (object dataItem)
		{
			var project = IdeApp.ProjectOperations.CurrentSelectedProject as UnknownProject;
			var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			UnknownProject [] selectedProjects = null;
			bool wholeSolution = false;
			if (project == null) {
				// ContextMenu on Solution
				var dlg = new ProjectSelectorDialog ();
				try {
					dlg.Title = GettextCatalog.GetString ("Select projects to migrate");
					dlg.RootItem = solution;
					dlg.AllowEmptySelection = false;
					dlg.SelectableFilter = (arg) => IsMigratableProject (arg as UnknownProject);
					dlg.SelectedItem = IdeApp.ProjectOperations.CurrentSelectedObject;
					dlg.SelectableItemTypes = new Type [] { typeof (UnknownProject) };
					dlg.ActiveItems = solution.GetAllProjects ().OfType<UnknownProject> ().Where (p => p.FileName.HasExtension (".xproj"));
					dlg.ShowCheckboxes = true;
					if (MessageService.RunCustomDialog (dlg, IdeApp.Workbench.RootWindow) == (int)Gtk.ResponseType.Ok) {
						if (dlg.ActiveItems.SequenceEqual (solution.GetAllProjects ().OfType<UnknownProject> ().Where (p => p.FileName.HasExtension (".xproj"))))
							wholeSolution = true;
						else
							selectedProjects = dlg.ActiveItems.OfType<UnknownProject> ().ToArray ();
					} else {
						return;
					}
				} finally {
					dlg.Destroy ();
					dlg.Dispose ();
				}
			} else {
				selectedProjects = new UnknownProject [] { project };
			}
			using (var monitor = CreateOutputProgressMonitor ()) {
				try {
					monitor.BeginTask (GettextCatalog.GetString ("Migrating…"), 1);
					if (wholeSolution) {
						if (!await Migrate (null, solution, monitor))
							return;
					} else {
						foreach (var proj in selectedProjects)
							if (!await Migrate (proj, solution, monitor))
								return;
					}
					monitor.ReportSuccess (GettextCatalog.GetString ("Successfully migrated"));
				} catch (Exception e) {
					monitor.ReportError (GettextCatalog.GetString ("Failed to migrate") + Environment.NewLine + e);
				}
			}
		}

		ProgressMonitor CreateOutputProgressMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				"DotNetCoreMigration",
				GettextCatalog.GetString (".NET Core Migration"),
				Stock.PadExecute,
				true,
				true);
		}

		protected override void Update (CommandInfo info)
		{
			if (IsMigratableProject (IdeApp.ProjectOperations.CurrentSelectedProject as UnknownProject))
				info.Visible = true;
			else if (IdeApp.ProjectOperations.CurrentSelectedSolution?.GetAllProjects ().OfType<UnknownProject> ().Any (p => IsMigratableProject (p)) ?? false)
				info.Visible = true;
			else
				info.Visible = false;
		}
	}
}
