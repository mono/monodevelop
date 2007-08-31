// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;
using System.ComponentModel;

using MonoDevelop.Core;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Projects;

using Gtk;
using MonoDevelop.Components;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	public class CombineBuildOptions : AbstractOptionPanel
	{
		CombineBuildOptionsWidget widget;
		
		class CombineBuildOptionsWidget : GladeWidgetExtract 
		{
			// Gtk Controls
			[Glade.Widget] Gnome.FileEntry outputDirButton;
			
			Combine combine;

			public  CombineBuildOptionsWidget(Properties CustomizationObject) : 
				base ("Base.glade", "CombineBuildOptions")
			{
				this.combine = ((Properties)CustomizationObject).Get<Combine> ("Combine");
				outputDirButton.Filename = combine.OutputDirectory + System.IO.Path.DirectorySeparatorChar;
			}

			public bool Store()
			{
				combine.OutputDirectory = outputDirButton.Filename;
				return true;
			}
		}

		public override void LoadPanelContents()
		{
			Add (widget = new  CombineBuildOptionsWidget ((Properties) CustomizationObject));
		}

		public override bool StorePanelContents()
		{
			bool success = widget.Store ();
 			return success;
		}					
	}
}
