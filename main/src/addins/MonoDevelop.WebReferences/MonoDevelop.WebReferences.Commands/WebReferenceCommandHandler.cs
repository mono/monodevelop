using System;
using System.IO;
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
using System.ServiceModel;

namespace MonoDevelop.WebReferences.Commands
{
	/// <summary>Defines the properties and methods for the WebReferenceCommandHandler class.</summary>
	public class WebReferenceCommandHandler : NodeCommandHandler
	{
		StatusBarContext UpdateReferenceContext {
			get; set;
		}
		
		/// <summary>Execute the command for adding a new web reference to a project.</summary>
		[CommandHandler (MonoDevelop.WebReferences.WebReferenceCommands.Add)]
		public void NewWebReference()
		{
			// Get the project and project folder
			DotNetProject project = CurrentNode.GetParentDataItem (typeof(DotNetProject), true) as DotNetProject;
			
			// Check and switch the runtime environment for the current project
			if (project.TargetFramework.Id == TargetFrameworkMoniker.NET_1_1)
			{
				string question = "The current runtime environment for your project is set to version 1.0.";
				question += "Web Service is not supported in this version.";
				question += "Do you want switch the runtime environment for this project version 2.0 ?";
				
				AlertButton switchButton = new AlertButton ("_Switch to .NET2"); 
				if (MessageService.AskQuestion(question, AlertButton.Cancel, switchButton) == switchButton)
					project.TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (TargetFrameworkMoniker.NET_2_0);					
				else
					return;
			}
			
			WebReferenceDialog dialog = new WebReferenceDialog (project);
			dialog.NamespacePrefix = project.DefaultNamespace;
			
			try {
				if (MessageService.RunCustomDialog (dialog) != (int)Gtk.ResponseType.Ok)
					return;

				dialog.SelectedService.GenerateFiles (project, dialog.Namespace, dialog.ReferenceName);
				IdeApp.ProjectOperations.Save(project);
			} catch (Exception exception) {
				MessageService.ShowException (exception);
			} finally {
				dialog.Destroy ();
			}
		}

		[CommandUpdateHandler (MonoDevelop.WebReferences.WebReferenceCommands.Update)]
		[CommandUpdateHandler (MonoDevelop.WebReferences.WebReferenceCommands.UpdateAll)]
		void CanUpdateWebReferences (CommandInfo ci)
		{
			// This does not appear to work.
			ci.Enabled = UpdateReferenceContext == null;
		}
		
		/// <summary>Execute the command for updating a web reference in a project.</summary>
		[CommandHandler (MonoDevelop.WebReferences.WebReferenceCommands.Update)]
		public void Update()
		{
			UpdateReferences (new [] { (WebReferenceItem) CurrentNode.DataItem });
		}

		/// <summary>Execute the command for updating all web reference in a project.</summary>
		[CommandHandler (MonoDevelop.WebReferences.WebReferenceCommands.UpdateAll)]
		public void UpdateAll()
		{
			DotNetProject project = ((WebReferenceFolder) CurrentNode.DataItem).Project;
			UpdateReferences (WebReferencesService.GetWebReferenceItems (project).ToArray ());
		}
		
		void UpdateReferences (IList<WebReferenceItem> items)
		{
			try {
				UpdateReferenceContext = IdeApp.Workbench.StatusBar.CreateContext ();
				UpdateReferenceContext.BeginProgress (GettextCatalog.GetPluralString ("Updating web reference", "Updating web references", items.Count));
				
				DispatchService.ThreadDispatch (() => {
					for (int i = 0; i < items.Count; i ++) {
						DispatchService.GuiDispatch (() => UpdateReferenceContext.SetProgressFraction (Math.Max (0.1, (double)i / items.Count)));
						try {
							items [i].Update();
						} catch (Exception ex) {
							DispatchService.GuiSyncDispatch (() => {
								MessageService.ShowException (ex, GettextCatalog.GetString ("Failed to update Web Reference '{0}'", items [i].Name));
								DisposeUpdateContext ();
							});
							return;
						}
					}
					
					DispatchService.GuiDispatch (() => {
						// Make sure that we save all relevant projects, there should only be 1 though
						foreach (var project in items.Select (i =>i.Project).Distinct ())
							IdeApp.ProjectOperations.Save (project);
						
						IdeApp.Workbench.StatusBar.ShowMessage(GettextCatalog.GetPluralString ("Updated Web Reference {0}", "Updated Web References", items.Count, items[0].Name));
						DisposeUpdateContext ();
					});
				});
			} catch {
				DisposeUpdateContext ();
				throw;
			}
		}
	
		void DisposeUpdateContext ()
		{
			if (UpdateReferenceContext != null) {
				UpdateReferenceContext.Dispose ();
				UpdateReferenceContext = null;
			}
		}
		
		/// <summary>Execute the command for removing a web reference from a project.</summary>
		[CommandHandler (MonoDevelop.WebReferences.WebReferenceCommands.Delete)]
		public void Delete()
		{
			WebReferenceItem item = (WebReferenceItem) CurrentNode.DataItem;
			if (!MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to delete the web service reference '{0}'?", item.Name), AlertButton.Delete))
				return;
			item.Delete();
			IdeApp.ProjectOperations.Save (item.Project);
			IdeApp.Workbench.StatusBar.ShowMessage("Deleted Web Reference " + item.Name);
		}
		
		/// <summary>Execute the command for removing all web references from a project.</summary>
		[CommandHandler (MonoDevelop.WebReferences.WebReferenceCommands.DeleteAll)]
		public void DeleteAll()
		{
			DotNetProject project = ((WebReferenceFolder) CurrentNode.DataItem).Project;
			List<WebReferenceItem> items = new List<WebReferenceItem> (WebReferencesService.GetWebReferenceItems (project));
			foreach (var item in items)
				item.Delete();

			IdeApp.ProjectOperations.Save(project);
			IdeApp.Workbench.StatusBar.ShowMessage("Deleted all Web References");
		}

		[CommandUpdateHandler (MonoDevelop.WebReferences.WebReferenceCommands.Configure)]
		void CanConfigureWebReferences (CommandInfo ci)
		{
			var item = (WebReferenceItem) CurrentNode.DataItem;
			ci.Enabled = WCFConfigWidget.IsSupported (item);
		}

		/// <summary>Execute the command for configuring a web reference in a project.</summary>
		[CommandHandler (MonoDevelop.WebReferences.WebReferenceCommands.Configure)]
		public void Configure ()
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
				UpdateReferences (new [] { item });
			} catch (Exception exception) {
				MessageService.ShowException (exception);
			} finally {
				dialog.Destroy ();
			}
		}
	}	
}
