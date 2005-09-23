// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Drawing;

using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.ExternalTool;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Services;

using Gtk;

namespace MonoDevelop.Gui.Dialogs.OptionPanels
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
			[Glade.Widget] Gnome.FileEntry beforeButton;
			[Glade.Widget] Gnome.FileEntry executeButton;
			[Glade.Widget] Gnome.FileEntry afterButton;
			[Glade.Widget] CheckButton pauseConsoleOutputCheckButton;			
			[Glade.Widget] CheckButton externalConsoleCheckButton;			
			
			DotNetProjectConfiguration configuration;

			public  OutputOptionsPanelWidget(IProperties CustomizationObject) : base ("Base.glade", "OutputOptionsPanel")
 			{			
				configuration = (DotNetProjectConfiguration)((IProperties)CustomizationObject).GetProperty("Config");
				externalConsoleCheckButton.Toggled += new EventHandler (ExternalConsoleToggle);
				
				assemblyNameEntry.Text = configuration.OutputAssembly;
				outputPathButton.Filename = configuration.OutputDirectory;
				parametersEntry.Text      = configuration.CommandLineParameters;
				executeButton.Filename = configuration.ExecuteScript;
 				beforeButton.Filename = configuration.ExecuteBeforeBuild;
 				afterButton.Filename = configuration.ExecuteAfterBuild;
				
 				externalConsoleCheckButton.Active = configuration.ExternalConsole;
 				pauseConsoleOutputCheckButton.Active = configuration.PauseConsoleOutput;
			}

			public bool Store ()
			{	
				if (configuration == null) {
					return true;
				}
				
				if (!Runtime.FileUtilityService.IsValidFileName(assemblyNameEntry.Text)) {
					Runtime.MessageService.ShowError (GettextCatalog.GetString ("Invalid assembly name specified"));
					return false;
				}

				if (!Runtime.FileUtilityService.IsValidFileName (outputPathButton.Filename)) {
					Runtime.MessageService.ShowError (GettextCatalog.GetString ("Invalid output directory specified"));
					return false;
				}
				
				configuration.OutputAssembly = assemblyNameEntry.Text;
				configuration.OutputDirectory = outputPathButton.Filename;
				configuration.CommandLineParameters = parametersEntry.Text;
				configuration.ExecuteBeforeBuild = beforeButton.Filename;
				configuration.ExecuteAfterBuild = afterButton.Filename;
				configuration.ExecuteScript = executeButton.Filename;
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
