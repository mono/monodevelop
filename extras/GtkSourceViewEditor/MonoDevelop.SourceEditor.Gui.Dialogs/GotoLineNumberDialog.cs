//  GotoLineNumberDialog.cs
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
using System.IO;
using System.Resources;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

using Gtk;
using Glade;

namespace MonoDevelop.SourceEditor.Gui.Dialogs
{
	public class GotoLineNumberDialog : IDisposable
	{
		public static bool IsVisible = false;
	
		[Widget] Dialog GotoLineDialog;
		[Widget] Entry line_number_entry;
		
		public GotoLineNumberDialog ()
		{
			new Glade.XML (null, "texteditoraddin.glade", "GotoLineDialog", null).Autoconnect (this);
			GotoLineDialog.Close += new EventHandler(on_btn_close_clicked);
		}
		
		public void Run ()
		{
			GotoLineDialog.TransientFor = IdeApp.Workbench.RootWindow;
			GotoLineDialog.ShowAll ();
			IsVisible = true;
			GotoLineDialog.Run ();
		}
		
		public void Hide ()
		{
			GotoLineDialog.Hide ();
			IsVisible = false;
		}
		
		void on_btn_close_clicked (object sender, EventArgs e)
		{
			GotoLineDialog.Hide ();
		}
		
		protected void on_btn_go_to_line_clicked (object sender, EventArgs e)
		{
			try {
				IEditableTextBuffer view = IdeApp.Workbench.ActiveDocument.GetContent<IEditableTextBuffer> ();
				if (view != null) {			
					int l = Math.Max (1, Int32.Parse(line_number_entry.Text));
					view.SetCaretTo (l, 1);
				}
			} catch (Exception) {
				
			} finally {
				GotoLineDialog.Hide ();
			}
		}
		
		public void Dispose ()
		{
			if (GotoLineDialog != null) {
				GotoLineDialog.Dispose ();
				GotoLineDialog = null;
				line_number_entry = null;
				IsVisible = false;
			}
		}
	}
}
