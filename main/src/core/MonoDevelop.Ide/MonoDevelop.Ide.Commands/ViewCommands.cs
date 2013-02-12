// ViewCommands.cs
//
// Author:
//   Carlo Kok (ck@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//


using System;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.Commands
{
	public enum ViewCommands
	{
		ViewList,
		LayoutList,
		NewLayout,
		DeleteCurrentLayout,
		LayoutSelector,
		FullScreen,
		Open,
		TreeDisplayOptionList,
		ResetTreeDisplayOptions,
		RefreshTree,
		CollapseAllTreeNodes,
		OpenWithList,
		ShowNext,
		ShowPrevious,
		ZoomIn,
		ZoomOut,
		ZoomReset,
		FocusCurrentDocument,
		ShowWelcomePage
	}

	// MonoDevelop.Ide.Commands.ViewCommands.ViewList
	public class ViewListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			for (int i = 0; i < IdeApp.Workbench.Pads.Count; i++) {
				Pad pad = IdeApp.Workbench.Pads[i];

				CommandInfo ci = new CommandInfo(pad.Title);
				ci.Icon = pad.Icon;
				ci.UseMarkup = true;
				ci.Description = GettextCatalog.GetString ("Show {0}", pad.Title);

				ActionCommand cmd = IdeApp.CommandService.GetActionCommand ("Pad|" + pad.Id);
				if (cmd != null) ci.AccelKey = cmd.AccelKey; 

				CommandArrayInfo list = info;
				if (pad.Categories != null) {
					for (int j = 0; j < pad.Categories.Length; j++) {
						bool found = false;
						for (int k = list.Count - 1; k >= 0; k--) {
							if (list[k].Text == pad.Categories[j] && list[k] is CommandInfoSet) {
								list = ((CommandInfoSet)list[k]).CommandInfos;
								found = true;
								break;
							}
						}
						if (!found) {
							CommandInfoSet set = new CommandInfoSet();
							set.Text = pad.Categories[j];
							set.Description = GettextCatalog.GetString ("Show {0}", set.Text);
							list.Add (set);
							list = set.CommandInfos;
						}
					}
				}
				for (int j = list.Count - 1; j >= 0; j--) {
					if (!(list[j] is CommandInfoSet)) {
						list.Insert (j + 1, ci, pad);
						pad = null;
						break;
					}
				}
				if (pad != null) list.Insert (0, ci, pad); 
			}
		}

		protected override void Run (object dataItem)
		{
			if (dataItem == null) return; 
			Pad pad = (Pad)dataItem;
			pad.Visible = true;
			pad.BringToFront (true);
		}
	}

	// MonoDevelop.Ide.Commands.ViewCommands.LayoutList
	public class LayoutListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			foreach (var name in IdeApp.Workbench.Layouts) {
				CommandInfo item = new CommandInfo(GettextCatalog.GetString (name));
				item.Checked = IdeApp.Workbench.CurrentLayout == name;
				item.Description = GettextCatalog.GetString ("Switch to layout '{0}'", name);
				info.Add (item, name);
			}
		}

		protected override void Run (object dataItem)
		{
			IdeApp.Workbench.CurrentLayout = (string)dataItem; 
		}
	}

	// MonoDevelop.Ide.Commands.ViewCommands.NewLayout
	public class NewLayoutHandler : CommandHandler
	{
		protected override void Run ()
		{
			string newLayoutName = null;
			var dlg = new NewLayoutDialog();
			try {
				if (MessageService.RunCustomDialog (dlg) == (int)Gtk.ResponseType.Ok) 
					newLayoutName = dlg.LayoutName; 
			} finally {
				dlg.Destroy ();
			}
			if (newLayoutName != null) {
				IdeApp.Workbench.CurrentLayout = newLayoutName;
			}
		}
	}
	// MonoDevelop.Ide.Commands.ViewCommands.DeleteCurrentLayout
	public class DeleteCurrentLayoutHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = !String.Equals ("Solution", IdeApp.Workbench.CurrentLayout, StringComparison.OrdinalIgnoreCase);
		}
		protected override void Run ()
		{
			if (MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to delete the active layout?"), AlertButton.Delete)) {
				string clayout = IdeApp.Workbench.CurrentLayout;
				IdeApp.Workbench.CurrentLayout = "Solution";
				IdeApp.Workbench.DeleteLayout (clayout);
			}
		}
	}
	// MonoDevelop.Ide.Commands.ViewCommands.FullScreen
	public class FullScreenHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (Platform.IsMac) {
				info.Text = IdeApp.Workbench.FullScreen
					? GettextCatalog.GetString ("Exit Full Screen")
					: GettextCatalog.GetString ("Enter Full Screen");
			} else {
				info.Checked = IdeApp.Workbench.FullScreen;
			}
		}
		
		protected override void Run ()
		{
			IdeApp.Workbench.FullScreen = !IdeApp.Workbench.FullScreen;
		}
	}

	// MonoDevelop.Ide.Commands.ViewCommands.ShowNext
	public class ShowNextHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ShowNext ();
		}

		protected override void Update (CommandInfo info)
		{
			ILocationList list = IdeApp.Workbench.ActiveLocationList;
			if (list == null)
				info.Enabled = false;
			else
				info.Text = GettextCatalog.GetString ("Show Next ({0})", list.ItemName);
		}
	}

	// MonoDevelop.Ide.Commands.ViewCommands.ShowPrevious
	public class ShowPreviousHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ShowPrevious ();
		}

		protected override void Update (CommandInfo info)
		{
			ILocationList list = IdeApp.Workbench.ActiveLocationList;
			if (list == null)
				info.Enabled = false;
			else
				info.Text = GettextCatalog.GetString ("Show Previous ({0})", list.ItemName);
		}
	}

	// MonoDevelop.Ide.Commands.ViewCommands.ZoomIn
	public class ZoomIn : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null)
				info.Enabled = false;
			else {
				IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent<IZoomable> ();
				info.Enabled = zoom != null && zoom.EnableZoomIn;
			}
		}

		protected override void Run ()
		{
			IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent<IZoomable> ();
			zoom.ZoomIn ();
		}
	}
	// MonoDevelop.Ide.Commands.ViewCommands.ZoomOut
	public class ZoomOut : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null)
				info.Enabled = false;
			else {
				IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent<IZoomable> ();
				info.Enabled = zoom != null && zoom.EnableZoomOut;
			}
		}

		protected override void Run ()
		{
			IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent<IZoomable> ();
			zoom.ZoomOut ();
		}
	}
	// MonoDevelop.Ide.Commands.ViewCommands.ZoomReset
	public class ZoomReset : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null)
				info.Enabled = false;
			else {
				IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent<IZoomable> ();
				info.Enabled = zoom != null && zoom.EnableZoomReset;
			}
		}

		protected override void Run ()
		{
			IZoomable zoom = IdeApp.Workbench.ActiveDocument.GetContent<IZoomable> ();
			zoom.ZoomReset ();
		}
	}
	
	public class FocusCurrentDocumentHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.Editor != null;
		}

		protected override void Run ()
		{
			IdeApp.Workbench.ActiveDocument.Editor.SetCaretTo (IdeApp.Workbench.ActiveDocument.Editor.Caret.Line, IdeApp.Workbench.ActiveDocument.Editor.Caret.Column);
		}

	}
}
