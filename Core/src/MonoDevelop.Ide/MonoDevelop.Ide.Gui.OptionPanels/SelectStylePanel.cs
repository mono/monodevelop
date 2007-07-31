// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using Mono.Addins;

using Gtk;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	internal class SelectStylePanel : AbstractOptionPanel
	{
		SelectStylePanelWidget widget;

		public override void LoadPanelContents()
		{
			Add (widget = new SelectStylePanelWidget ());
		}
		
		public override bool StorePanelContents()
		{
			widget.Store ();
			return true;
		}
	}

	partial class SelectStylePanelWidget : Gtk.Bin 
	{
		public SelectStylePanelWidget ()
		{
			Build ();
			
			string name = fontButton.Style.FontDescription.ToString ();
			
			extensionButton.Active = Runtime.Properties.GetProperty ("MonoDevelop.Core.Gui.ProjectBrowser.ShowExtensions", true);
			hiddenButton.Active = Runtime.Properties.GetProperty ("MonoDevelop.Core.Gui.FileScout.ShowHidden", false);
			fontCheckbox.Active = Runtime.Properties.GetProperty ("MonoDevelop.Core.Gui.Pads.UseCustomFont", false);
			fontButton.FontName = Runtime.Properties.GetProperty ("MonoDevelop.Core.Gui.Pads.CustomFont", name);
			fontButton.Sensitive = fontCheckbox.Active;
			
			fontCheckbox.Toggled += new EventHandler (FontCheckboxToggled);
		}
		
		void FontCheckboxToggled (object sender, EventArgs e)
		{
			fontButton.Sensitive = fontCheckbox.Active;
		}
		
		public void Store()
		{
			Runtime.Properties.SetProperty ("MonoDevelop.Core.Gui.ProjectBrowser.ShowExtensions", extensionButton.Active);
			Runtime.Properties.SetProperty ("MonoDevelop.Core.Gui.FileScout.ShowHidden", hiddenButton.Active);
			Runtime.Properties.SetProperty ("MonoDevelop.Core.Gui.Pads.UseCustomFont", fontCheckbox.Active);
			Runtime.Properties.SetProperty ("MonoDevelop.Core.Gui.Pads.CustomFont", fontButton.FontName);
		}
	}
}
