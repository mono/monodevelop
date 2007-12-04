//  SelectStylePanel.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

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
