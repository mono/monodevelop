using System;
using System.Collections;
using System.IO;
using System.Web.Services.Discovery;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.SolutionViewPad;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Projects.Item;
using MonoDevelop.WebReferences.Dialogs;

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
			SolutionProject project = CurrentNode.GetParentDataItem (typeof(SolutionProject), true) as SolutionProject;
			MSBuildProject dotProject = (MSBuildProject) project.Project;
			
			// Check and switch the runtime environment for the current project
// TODO: Project Conversion
//			if (dotProject.ClrVersion == ClrVersion.Net_1_1)
//			{
//				string question = "The current runtime environment for your project is set to version 1.0.";
//				question += "Web Service is not supported in this version.";
//				question += "Do you want switch the runtime environment for this project version 2.0 ?";
//				
//				if (IdeApp.Services.MessageService.AskQuestion(question))
//					dotProject.ClrVersion = ClrVersion.Net_2_0;					
//				else
//					return;
//			}
			
			WebReferenceDialog dialog = new WebReferenceDialog(Library.GetWebReferencePath(project.Project));
			dialog.NamespacePrefix = project.Name;
			
			int response = dialog.Run();
			dialog.Destroy();
			if (response == (int)Gtk.ResponseType.Ok)
			{
				try
				{
					CodeGenerator gen = new CodeGenerator(project.Project, dialog.SelectedService);
					
					// Create the base directory if it does not exists
					string basePath = dialog.ReferencePath;
					if (!Directory.Exists(basePath))
						Directory.CreateDirectory(basePath);
					
					// Generate the wsdl, disco and map files
					string mapSpec = gen.CreateMapFile(basePath, "Reference.map");
					ProjectFile mapFile = new ProjectFile(mapSpec, FileType.Content);
					project.Project.Add(mapFile);
			
					// Generate the proxy class
					string proxySpec = gen.CreateProxyFile(basePath, dialog.Namespace + "." + dialog.ReferenceName, "Reference");
					ProjectFile proxyFile = new ProjectFile(proxySpec, FileType.Compile);
					project.Project.Add(proxyFile);
					
					// Add a reference System.Web.Services to the project if it does not exists
//					string refName = "System.Web.Services, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
//					ProjectReference gacRef = new ProjectReference(ReferenceType.Gac, refName);
//					if (!project.ProjectReferences.Contains(gacRef))
					bool containsReference = false;
					foreach (ProjectItem item in project.Project.Items) {
						if (item is ReferenceProjectItem) {
							containsReference = (item as ReferenceProjectItem).Include == "System.Web.Services";
							if (containsReference)
								break;
						}
					}
					if (!containsReference)
						project.Project.Add (new ReferenceProjectItem("System.Web.Services"));

					ProjectService.SaveProject (project.Project);
				}
				catch(Exception exception)
				{
					IdeApp.Services.MessageService.ShowError(exception);
				}
			}
		}
		
		/// <summary>Execute the command for updating a web reference in a project.</summary>
		[CommandHandler (MonoDevelop.WebReferences.WebReferenceCommands.Update)]
		public void Update()
		{
			WebReferenceItem item = (WebReferenceItem) CurrentNode.DataItem;
			item.Update();
			IdeApp.Services.StatusBar.SetMessage("Updated Web Reference " + item.Name);
		}
		
		/// <summary>Execute the command for updating all web reference in a project.</summary>
		[CommandHandler (MonoDevelop.WebReferences.WebReferenceCommands.UpdateAll)]
		public void UpdateAll()
		{
			IProject project = ((WebReferenceFolder) CurrentNode.DataItem).Project;
			WebReferenceItemCollection items = new WebReferenceItemCollection (project);
			for (int index = 0; index < items.AllKeys.Length; index ++)
			{
				items[items.AllKeys[index]].Update();
				IdeApp.Services.StatusBar.SetMessage("Updated Web Reference " + items.AllKeys[index]);
			}
			IdeApp.Services.StatusBar.SetMessage("Updated all Web References");
		}
		
		/// <summary>Execute the command for removing a web reference from a project.</summary>
		[CommandHandler (MonoDevelop.WebReferences.WebReferenceCommands.Delete)]
		public void Delete()
		{
			WebReferenceItem item = (WebReferenceItem) CurrentNode.DataItem;
			IProject project = item.ProxyFile.Project;
			item.Delete();
			ProjectService.SaveProject(project);
			IdeApp.Services.StatusBar.SetMessage("Deleted Web Reference " + item.Name);
		}
		
		/// <summary>Execute the command for removing all web references from a project.</summary>
		[CommandHandler (MonoDevelop.WebReferences.WebReferenceCommands.DeleteAll)]
		public void DeleteAll()
		{
			IProject project = ((WebReferenceFolder) CurrentNode.DataItem).Project;
			WebReferenceItemCollection items = new WebReferenceItemCollection (project);
			for (int index = 0; index < items.AllKeys.Length; index ++)
			{
				items[items.AllKeys[index]].Delete();
				IdeApp.Services.StatusBar.SetMessage("Deleted Web Reference " + items.AllKeys[index]);
			}
			ProjectService.SaveProject(project);
			IdeApp.Services.StatusBar.SetMessage("Deleted all Web References");
		}
	}	
}
