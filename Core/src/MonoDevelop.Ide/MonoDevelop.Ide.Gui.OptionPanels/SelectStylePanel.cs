// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

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
		readonly Gtk.IconSize[] sizes = new Gtk.IconSize [] { Gtk.IconSize.Menu, Gtk.IconSize.SmallToolbar, Gtk.IconSize.LargeToolbar };
		
		public SelectStylePanelWidget ()
		{
			Build ();
			
			string name = fontButton.Style.FontDescription.ToString ();
			
			extensionButton.Active = PropertyService.Get ("MonoDevelop.Core.Gui.ProjectBrowser.ShowExtensions", true);
			hiddenButton.Active = PropertyService.Get ("MonoDevelop.Core.Gui.FileScout.ShowHidden", false);
			fontCheckbox.Active = PropertyService.Get ("MonoDevelop.Core.Gui.Pads.UseCustomFont", false);
			fontButton.FontName = PropertyService.Get ("MonoDevelop.Core.Gui.Pads.CustomFont", name);
			fontButton.Sensitive = fontCheckbox.Active;
			
			fontCheckbox.Toggled += new EventHandler (FontCheckboxToggled);

			Gtk.IconSize curSize = PropertyService.Get <Gtk.IconSize> ("MonoDevelop.ToolbarSize", Gtk.IconSize.LargeToolbar);
			toolbarCombobox.Active = Array.IndexOf (sizes, curSize);
		}
		
		void FontCheckboxToggled (object sender, EventArgs e)
		{
			fontButton.Sensitive = fontCheckbox.Active;
		}
		
		public void Store()
		{
			PropertyService.Set ("MonoDevelop.Core.Gui.ProjectBrowser.ShowExtensions", extensionButton.Active);
			PropertyService.Set ("MonoDevelop.Core.Gui.FileScout.ShowHidden", hiddenButton.Active);
			PropertyService.Set ("MonoDevelop.Core.Gui.Pads.UseCustomFont", fontCheckbox.Active);
			PropertyService.Set ("MonoDevelop.Core.Gui.Pads.CustomFont", fontButton.FontName);
			
			PropertyService.Set("MonoDevelop.ToolbarSize", sizes [toolbarCombobox.Active]);
		}
	}
}
