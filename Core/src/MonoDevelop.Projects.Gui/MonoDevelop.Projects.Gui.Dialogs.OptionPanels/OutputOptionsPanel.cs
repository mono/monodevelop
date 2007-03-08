// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;

using MonoDevelop.Projects;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Properties;

using Gtk;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	public class OutputOptionsPanel : AbstractOptionPanel
	{
		class OutputOptionsPanelWidget : GladeWidgetExtract 
		{
			//
			// Gtk Controls	
			//
			[Glade.Widget] Entry assemblyNameEntry;
			[Glade.Widget] Gnome.FileEntry outputPathButton;
			[Glade.Widget] Entry parametersEntry;
			[Glade.Widget] CheckButton pauseConsoleOutputCheckButton;			
			[Glade.Widget] CheckButton externalConsoleCheckButton;			
			
			DotNetProjectConfiguration configuration;
			Project project;

			public  OutputOptionsPanelWidget(IProperties CustomizationObject) : base ("Base.glade", "OutputOptionsPanel")
 			{			
				configuration = (DotNetProjectConfiguration)((IProperties)CustomizationObject).GetProperty("Config");
				project = (Project)((IProperties)CustomizationObject).GetProperty("Project");
				externalConsoleCheckButton.Toggled += new EventHandler (ExternalConsoleToggle);
				
				assemblyNameEntry.Text = configuration.OutputAssembly;
				parametersEntry.Text = configuration.CommandLineParameters;
				
				outputPathButton.DefaultPath = project.BaseDirectory;
				outputPathButton.Filename = configuration.OutputDirectory;
				
 				externalConsoleCheckButton.Active = configuration.ExternalConsole;
 				pauseConsoleOutputCheckButton.Active = configuration.PauseConsoleOutput;
			}

			public bool Store ()
			{	
				if (configuration == null) {
					return true;
				}
				
				if (!Runtime.FileService.IsValidFileName(assemblyNameEntry.Text)) {
					Services.MessageService.ShowError (null, GettextCatalog.GetString ("Invalid assembly name specified"), (Gtk.Window) Toplevel, true);
					return false;
				}

				if (!Runtime.FileService.IsValidFileName (outputPathButton.Filename)) {
					Services.MessageService.ShowError (null, GettextCatalog.GetString ("Invalid output directory specified"), (Gtk.Window) Toplevel, true);
					return false;
				}
				
				configuration.OutputAssembly = assemblyNameEntry.Text;
				configuration.OutputDirectory = outputPathButton.Filename;
				configuration.CommandLineParameters = parametersEntry.Text;
 				configuration.ExternalConsole = externalConsoleCheckButton.Active;
				configuration.PauseConsoleOutput = pauseConsoleOutputCheckButton.Active;
				return true;
			}
			
			void ExternalConsoleToggle (object sender, EventArgs e)
			{
				if (externalConsoleCheckButton.Active) {
	 				pauseConsoleOutputCheckButton.Sensitive = true;
					pauseConsoleOutputCheckButton.Active = true;
				} else {
	 				pauseConsoleOutputCheckButton.Sensitive = false;
					pauseConsoleOutputCheckButton.Active = false;
				}
			}
		}

		OutputOptionsPanelWidget  widget;
		public override void LoadPanelContents()
		{
			Add (widget = new  OutputOptionsPanelWidget ((IProperties) CustomizationObject));
		}
		
		public override bool StorePanelContents()
		{
			bool result = true;
			result = widget.Store ();
 			return result;
		}
	}
}
