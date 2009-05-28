//
// IDEStyleOptionsPanel.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Components.Commands;
using Mono.Addins;
using Gtk;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	internal class IDEStyleOptionsPanel : OptionsPanel
	{
		IDEStyleOptionsPanelWidget widget;

		public override Widget CreatePanelWidget ()
		{
			return widget = new IDEStyleOptionsPanelWidget ();
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
	
	public partial class IDEStyleOptionsPanelWidget : Gtk.Bin
	{
		readonly Gtk.IconSize[] sizes = new Gtk.IconSize [] { Gtk.IconSize.Menu, Gtk.IconSize.SmallToolbar, Gtk.IconSize.LargeToolbar };
				
		public IDEStyleOptionsPanelWidget ()
		{
			this.Build();
			Load ();
		}
		
		void Load ()
		{
			string name = fontButton.Style.FontDescription.ToString ();
			
			documentSwitcherButton.Active = PropertyService.Get ("MonoDevelop.Core.Gui.EnableDocumentSwitchDialog", true);
			hiddenButton.Active = PropertyService.Get ("MonoDevelop.Core.Gui.FileScout.ShowHidden", false);
			fontCheckbox.Active = PropertyService.Get ("MonoDevelop.Core.Gui.Pads.UseCustomFont", false);
			fontButton.FontName = PropertyService.Get ("MonoDevelop.Core.Gui.Pads.CustomFont", name);
			fontButton.Sensitive = fontCheckbox.Active;
			
			fontCheckbox.Toggled += new EventHandler (FontCheckboxToggled);

			Gtk.IconSize curSize = IdeApp.Preferences.ToolbarSize;
			toolbarCombobox.Active = Array.IndexOf (sizes, curSize);
		}
		void FontCheckboxToggled (object sender, EventArgs e)
		{
			fontButton.Sensitive = fontCheckbox.Active;
		}
		
		public void Store()
		{
			PropertyService.Set ("MonoDevelop.Core.Gui.FileScout.ShowHidden", hiddenButton.Active);
			PropertyService.Set ("MonoDevelop.Core.Gui.Pads.UseCustomFont", fontCheckbox.Active);
			PropertyService.Set ("MonoDevelop.Core.Gui.Pads.CustomFont", fontButton.FontName);
			
			IdeApp.Preferences.ToolbarSize = sizes [toolbarCombobox.Active];
			PropertyService.Set ("MonoDevelop.Core.Gui.EnableDocumentSwitchDialog", documentSwitcherButton.Active);
		}
		
	}
}
