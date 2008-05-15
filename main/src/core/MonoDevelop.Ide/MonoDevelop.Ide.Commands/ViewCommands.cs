//  ViewCommands.cs
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
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Diagnostics;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Components.Commands;

using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.Commands
{
	public enum ViewCommands
	{
		ViewList,
		LayoutList,
		NewLayout,
		DeleteCurrentLayout,
		FullScreen,
		Open,
		OpenWithList,
		TreeDisplayOptionList,
		ResetTreeDisplayOptions,
		RefreshTree,
		CollapseAllTreeNodes,
		LayoutSelector,
		ShowNext,
		ShowPrevious,
		
		ZoomIn,
		ZoomOut,
		ZoomReset
	}
	
	internal class ZoomIn: CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				info.Enabled = false;
				return;
			}
			IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent <IZoomable> ();
			info.Enabled = zoom != null && zoom.EnableZoomIn;
		}
		protected override void Run (object doc)
		{
			IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent <IZoomable> ();
			Debug.Assert (zoom != null);
			zoom.ZoomIn ();
		}
	}
	
	internal class ZoomOut: CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				info.Enabled = false;
				return;
			}
			IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent <IZoomable> ();
			info.Enabled = zoom != null && zoom.EnableZoomOut;
		}
		protected override void Run (object doc)
		{
			IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent <IZoomable> ();
			Debug.Assert (zoom != null);
			zoom.ZoomOut ();
		}
	}
	
	internal class ZoomReset: CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				info.Enabled = false;
				return;
			}
			IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent <IZoomable> ();
			info.Enabled = zoom != null && zoom.EnableZoomReset;
		}
		protected override void Run (object doc)
		{
			IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent <IZoomable> ();
			Debug.Assert (zoom != null);
			zoom.ZoomReset ();
		}
	}
	
	internal class FullScreenHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.FullScreen = !IdeApp.Workbench.FullScreen;
		}
	}
	
	internal class NewLayoutHandler: CommandHandler
	{
		protected override void Run ()
		{
			NewLayoutDialog dlg = new NewLayoutDialog ();
			try {
				Gtk.ResponseType response = (Gtk.ResponseType) dlg.Run ();
				if (response == Gtk.ResponseType.Ok)
					IdeApp.Workbench.CurrentLayout = dlg.LayoutName;
			} finally {
				dlg.Destroy ();
			}
		}
	}
	
	internal class DeleteCurrentLayoutHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to delete the active layout?"), AlertButton.Delete)) {
				string clayout = IdeApp.Workbench.CurrentLayout;
				IdeApp.Workbench.CurrentLayout = "Default";
				IdeApp.Workbench.DeleteLayout (clayout);
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workbench.CurrentLayout != "Default";
		}
	}
	
	internal class ViewListHandler: CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			foreach (Pad pad in IdeApp.Workbench.Pads) {
				CommandArrayInfo ciset = FindArray (info, pad.Categories);
				CommandInfo cmd = new CommandInfo (pad.Title);
				cmd.Icon = pad.Icon;
				cmd.UseMarkup = true;
				cmd.Description = GettextCatalog.GetString ("Show {0}", pad.Title);
				
				// Insert before the submenus
				int n = ciset.Count-1;
				while (n >= 0 && ciset [n] is CommandInfoSet)
					n--;
				ciset.Insert (n+1, cmd, pad);
			}
		}
		
		public CommandArrayInfo FindArray (CommandArrayInfo iset, string[] categories)
		{
			for (int n=0; n<categories.Length; n++) {
				CommandArrayInfo foundSet = null;
				for (int i=0; i<iset.Count; i++) {
					CommandInfoSet s = iset [i] as CommandInfoSet;
					if (s != null && s.Text == categories [n]) {
						foundSet = s.CommandInfos;
						break;
					}
				}
				if (foundSet == null) {
					CommandInfoSet s = new CommandInfoSet ();
					s.Text = s.Description = categories [n];
					iset.Add (s);
					foundSet = s.CommandInfos;
				}
				iset = foundSet;
			}
			return iset;
		}
		
		protected override void Run (object ob)
		{
			if (ob == null)
				return;
			Pad pad = (Pad) ob;
			pad.Visible = true;
			pad.BringToFront ();
		}
	}
	
	internal class LayoutListHandler: CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			string[] layouts = IdeApp.Workbench.Layouts;
			Array.Sort (layouts);
			foreach (string layout in layouts) {
				CommandInfo cmd = new CommandInfo (GettextCatalog.GetString (layout));
				cmd.Checked = (layout == IdeApp.Workbench.CurrentLayout);
				cmd.Description = GettextCatalog.GetString ("Switch to layout '{0}'", cmd.Text);
				info.Add (cmd, layout);
			}
		}
		
		protected override void Run (object layout)
		{
			IdeApp.Workbench.CurrentLayout = (string) layout;
		}
	}
	
	internal class ShowNextHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ShowNext ();
		}
		
		protected override void Update (CommandInfo info)
		{
			Pad pad = IdeApp.Workbench.GetLocationListPad ();
			if (pad != null)
				info.Text = GettextCatalog.GetString ("Show Next ({0})", pad.Title);
			else
				info.Enabled = false;
		}
	}
	
	internal class ShowPreviousHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ShowPrevious ();
		}
		
		protected override void Update (CommandInfo info)
		{
			Pad pad = IdeApp.Workbench.GetLocationListPad ();
			if (pad != null)
				info.Text = GettextCatalog.GetString ("Show Previous ({0})", pad.Title);
			else
				info.Enabled = false;
		}
	}
}
