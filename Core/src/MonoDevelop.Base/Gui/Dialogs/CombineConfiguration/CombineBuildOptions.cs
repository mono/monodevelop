// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using MonoDevelop.Core.AddIns.Codons;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Internal.Project;

using Gtk;
using MonoDevelop.Gui.Widgets;

namespace MonoDevelop.Gui.Dialogs.OptionPanels
{
	public class CombineBuildOptions : AbstractOptionPanel
	{
		CombineBuildOptionsWidget widget;
		
		class CombineBuildOptionsWidget : GladeWidgetExtract 
		{
			// Gtk Controls
			[Glade.Widget] Gnome.FileEntry outputDirButton;
			
			Combine combine;

			public  CombineBuildOptionsWidget(IProperties CustomizationObject) : 
				base ("Base.glade", "CombineBuildOptions")
			{
				this.combine = (Combine)((IProperties)CustomizationObject).GetProperty("Combine");
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
			Add (widget = new  CombineBuildOptionsWidget ((IProperties) CustomizationObject));
		}

		public override bool StorePanelContents()
		{
			bool success = widget.Store ();
 			return success;
		}					
	}
}
