using System;
using System.Linq;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.WebReferences.Dialogs;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using System.Collections.Generic;
using MonoDevelop.Core.Assemblies;
using System.Threading.Tasks;

namespace MonoDevelop.WebReferences.Commands
{
	/// <summary>Defines the properties and methods for the WebReferenceCommandHandler class.</summary>
	public class WebReferenceCommandHandler : NodeCommandHandler
	{
		bool updatingReferences;

		/// <summary>Execute the command for adding a new web reference to a project.</summary>
		[CommandHandler (WebReferenceCommands.Add)]
		public async void NewWebReference()
		{
			// Get the project and project folder
			var project = CurrentNode.GetParentDataItem (typeof(DotNetProject), true) as DotNetProject;
			
			// Check and switch the runtime environment for the current project
			if (project.TargetFramework.Id == TargetFrameworkMoniker.NET_1_1)
			{
				string msg1 = GettextCatalog.GetString ("The current runtime environment for your project is set to version 1.0.");
				string msg2 = GettextCatalog.GetString ("Web Service is not supported in this version.");
				string msg3 = GettextCatalog.GetString ("Do you want switch the runtime environment for this project version 2.0?");
				string question = $"{msg1} {msg2} {msg3}";
				
				var switchButton = new AlertButton (GettextCatalog.GetString ("_Switch to .NET 2.0")); 
				if (MessageService.AskQuestion(question, AlertButton.Cancel, switchButton) == switchButton)
					project.TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (TargetFrameworkMoniker.NET_2_0);					
				else
					return;
			}
			
			var dialog = new WebReferenceDialog (project);
			dialog.NamespacePrefix = project.DefaultNamespace;
			
			try {
				if (MessageService.RunCustomDialog (dialog) != (int)Gtk.ResponseType.Ok)
					return;

				await dialog.SelectedService.GenerateFiles (project, dialog.Namespace, dialog.ReferenceName);
				await IdeApp.ProjectOperations.SaveAsync(project);
			} catch (Exception exception) {
				MessageService.ShowError (GettextCatalog.GetString ("The web reference could not be added"), exception);
			} finally {
				dialog.Destroy ();
				dialog.Dispose ();
			}
		}

		[CommandUpdateHandler (WebReferenceCommands.Update)]
		[CommandUpdateHandler (WebReferenceCommands.UpdateAll)]
		void CanUpdateWebReferences (CommandInfo ci)
		{
			ci.Enabled = !updatingReferences;
		}
		
		/// <summary>Execute the command for updating a web reference in a project.</summary>
		[CommandHandler (WebReferenceCommands.Update)]
		public async void Update()
		{
			await UpdateReferences (new [] { (WebReferenceItem) CurrentNode.DataItem });
		}

		/// <summary>Execute the command for updating all web reference in a project.</summary>
		[CommandHandler (WebReferenceCommands.UpdateAll)]
		public async void UpdateAll()
		{
			var folder = (WebReferenceFolder)CurrentNode.DataItem;
			DotNetProject project = folder.Project;
			if (folder.IsWCF)
				await UpdateReferences (WebReferencesService.GetWebReferenceItemsWCF (project).ToArray ());
			else
				await UpdateReferences (WebReferencesService.GetWebReferenceItemsWS (project).ToArray ());
		}
		
		async Task UpdateReferences (IList<WebReferenceItem> items)
		{
			if (updatingReferences)
				return;
			try {
				updatingReferences = true;
				using (var monitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (GettextCatalog.GetPluralString ("Updating web reference", "Updating web references", items.Count), MonoDevelop.Ide.Gui.Stock.PadDownload, true)) {
					monitor.BeginTask (GettextCatalog.GetPluralString ("Updating web reference", "Updating web references", items.Count), items.Count);

					for (int i = 0; i < items.Count; i++) {
						try {
							await items [i].Update ();
						} catch (Exception ex) {
							MessageService.ShowError (GettextCatalog.GetString ("Failed to update Web Reference '{0}'", items [i].Name), ex);
							return;
						}
						monitor.Step ();
					}

					// Make sure that we save all relevant projects, there should only be 1 though
					foreach (var project in items.Select (i => i.Project).Distinct ())
						await IdeApp.ProjectOperations.SaveAsync (project);
				}
				IdeApp.Workbench.StatusBar.ShowMessage (GettextCatalog.GetPluralString ("Updated Web Reference {0}", "Updated Web References", items.Count, items [0].Name));
			} finally {
				updatingReferences = false;
			}
		}
	
		/// <summary>Execute the command for removing a web reference from a project.</summary>
		[CommandHandler (WebReferenceCommands.Delete)]
		public void Delete()
		{
			var item = (WebReferenceItem) CurrentNode.DataItem;
			if (!MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to delete the web service reference '{0}'?", item.Name), AlertButton.Delete))
				return;
			item.Delete();
			IdeApp.ProjectOperations.SaveAsync (item.Project);
			IdeApp.Workbench.StatusBar.ShowMessage(GettextCatalog.GetString ("Deleted Web Reference {0}", item.Name));
		}
		
		/// <summary>Execute the command for removing all web references from a project.</summary>
		[CommandHandler (WebReferenceCommands.DeleteAll)]
		public void DeleteAll()
		{
			var folder = (WebReferenceFolder)CurrentNode.DataItem;
			DotNetProject project = folder.Project;
			IEnumerable<WebReferenceItem> items;

			if (folder.IsWCF)
				items = WebReferencesService.GetWebReferenceItemsWCF (project);
			else
				items = WebReferencesService.GetWebReferenceItemsWS (project);

			foreach (var item in items.ToList ())
				item.Delete();

			IdeApp.ProjectOperations.SaveAsync(project);
			IdeApp.Workbench.StatusBar.ShowMessage(GettextCatalog.GetString ("Deleted all Web References"));
		}

		[CommandUpdateHandler (WebReferenceCommands.Configure)]
		void CanConfigureWebReferences (CommandInfo ci)
		{
			var item = CurrentNode.DataItem as WebReferenceItem;
			ci.Enabled = item != null && WCFConfigWidget.IsSupported (item);
		}

		/// <summary>Execute the command for configuring a web reference in a project.</summary>
		[CommandHandler (WebReferenceCommands.Configure)]
		public async void Configure ()
		{
			var item = (WebReferenceItem) CurrentNode.DataItem;

			if (!WCFConfigWidget.IsSupported (item))
				return;

			WCF.ReferenceGroup refgroup;
			WCF.ClientOptions options;

			try {
				refgroup = WCF.ReferenceGroup.Read (item.MapFile.FilePath);
				if (refgroup == null || refgroup.ClientOptions == null)
					return;
				options = refgroup.ClientOptions;
			} catch {
				return;
			}

			var dialog = new WebReferenceDialog (item, options);

			try {
				if (MessageService.RunCustomDialog (dialog) != (int)Gtk.ResponseType.Ok)
					return;
				if (!dialog.Modified)
					return;
				
				refgroup.Save (item.MapFile.FilePath);
				await UpdateReferences (new [] { item });
			} catch (Exception exception) {
				LoggingService.LogInternalError (exception);
			} finally {
				dialog.Destroy ();
				dialog.Dispose ();
			}
		}
	}	
}
