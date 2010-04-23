using System;
using System.IO;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.WebReferences.Dialogs;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using System.Collections.Generic;

namespace MonoDevelop.WebReferences.Commands
{
	/// <summary>Defines the properties and methods for the WebReferenceCommandHandler class.</summary>
	public class WebReferenceCommandHandler : NodeCommandHandler
	{
		/// <summary>Execute the command for adding a new web reference to a project.</summary>
		[CommandHandler (MonoDevelop.WebReferences.WebReferenceCommands.Add)]
		public void NewWebReference()
		{
			// Get the project and project folder
			DotNetProject project = CurrentNode.GetParentDataItem (typeof(DotNetProject), true) as DotNetProject;
			
			// Check and switch the runtime environment for the current project
			if (project.TargetFramework.Id == "1.1")
			{
				string question = "The current runtime environment for your project is set to version 1.0.";
				question += "Web Service is not supported in this version.";
				question += "Do you want switch the runtime environment for this project version 2.0 ?";
				
				AlertButton switchButton = new AlertButton ("_Switch to .NET2"); 
				if (MessageService.AskQuestion(question, AlertButton.Cancel, switchButton) == switchButton)
					project.TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework ("2.0");					
				else
					return;
			}
			
			WebReferenceDialog dialog = new WebReferenceDialog (project);
			dialog.NamespacePrefix = project.Name;
			
			int response = dialog.Run();
			dialog.Destroy();
			if (response == (int)Gtk.ResponseType.Ok)
			{
				try
				{
					dialog.SelectedService.GenerateFiles (project, dialog.Namespace, dialog.ReferenceName);
					IdeApp.ProjectOperations.Save(project);
				}
				catch(Exception exception)
				{
					MessageService.ShowException (exception);
				}
			}
		}
		
		/// <summary>Execute the command for updating a web reference in a project.</summary>
		[CommandHandler (MonoDevelop.WebReferences.WebReferenceCommands.Update)]
		public void Update()
		{
			using (StatusBarContext sbc = IdeApp.Workbench.StatusBar.CreateContext ()) {
				sbc.BeginProgress (GettextCatalog.GetString ("Updating web reference"));
				sbc.AutoPulse = true;
				WebReferenceItem item = (WebReferenceItem) CurrentNode.DataItem;
				DispatchService.BackgroundDispatchAndWait (item.Update);
				IdeApp.ProjectOperations.Save (item.Project);
				IdeApp.Workbench.StatusBar.ShowMessage("Updated Web Reference " + item.Name);
			}
		}
		
		/// <summary>Execute the command for updating all web reference in a project.</summary>
		[CommandHandler (MonoDevelop.WebReferences.WebReferenceCommands.UpdateAll)]
		public void UpdateAll()
		{
			using (StatusBarContext sbc = IdeApp.Workbench.StatusBar.CreateContext ()) {
				sbc.BeginProgress (GettextCatalog.GetString ("Updating web references"));
				sbc.AutoPulse = true;
				DotNetProject project = ((WebReferenceFolder) CurrentNode.DataItem).Project;
				List<WebReferenceItem> items = new List<WebReferenceItem> (WebReferencesService.GetWebReferenceItems (project));
				DispatchService.BackgroundDispatchAndWait (delegate {
					foreach (var item in items)
						item.Update();
				});
				IdeApp.ProjectOperations.Save (project);
				IdeApp.Workbench.StatusBar.ShowMessage("Updated all Web References");
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
	}	
}
