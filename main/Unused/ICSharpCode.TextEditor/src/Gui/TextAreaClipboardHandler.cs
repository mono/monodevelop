//  TextAreaClipboardHandler.cs
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
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.InteropServices;
using System.Xml;
using System.Text;

using MonoDevelop.TextEditor.Document;
using MonoDevelop.TextEditor.Undo;
using MonoDevelop.TextEditor.Util;

namespace MonoDevelop.TextEditor
{
	public class TextAreaClipboardHandler
	{
		TextArea textArea;
		Gtk.Clipboard clipboard;
		
		public bool EnableCut {
			get {
				return textArea.SelectionManager.HasSomethingSelected;
			}
		}
		
		public bool EnableCopy {
			get {
				return textArea.SelectionManager.HasSomethingSelected;
			}
		}
	
		
		public bool EnablePaste {
			get {
				return clipboard.WaitIsTextAvailable ();
			}
		}
		
		public bool EnableDelete {
			get {
				return textArea.SelectionManager.HasSomethingSelected;
			}
		}
		
		public bool EnableSelectAll {
			get {
				return true;
			}
		}
		
		public TextAreaClipboardHandler(TextArea textArea)
		{
			this.textArea = textArea;
			clipboard = Gtk.Clipboard.Get (Gdk.Atom.Intern("CLIPBOARD", false));
			textArea.SelectionManager.SelectionChanged += new EventHandler(DocumentSelectionChanged);
		}
		
		void DocumentSelectionChanged(object sender, EventArgs e)
		{
//			((DefaultWorkbench)WorkbenchSingleton.Workbench).UpdateToolbars();
		}
		
		bool CopyTextToClipboard()
		{
			string str = textArea.SelectionManager.SelectedText;
			clipboard.SetText(str);
			
			return true;
		}
		
		public void Cut(object sender, EventArgs e)
		{
			if (CopyTextToClipboard()) {
				// remove text
				textArea.BeginUpdate();
				textArea.Caret.Position = textArea.SelectionManager.SelectionCollection[0].StartPosition;
				textArea.SelectionManager.RemoveSelectedText();
				textArea.EndUpdate();
			}
		}
		
		public void Copy(object sender, EventArgs e)
		{
			CopyTextToClipboard();
		}
		
		public void Paste(object sender, EventArgs e)
		{
			try {
				if (clipboard.WaitIsTextAvailable()) {
					textArea.InsertString(clipboard.WaitForText());
				}
			} catch (Exception ex) {
				Console.WriteLine("Error pasting text: " + ex.Message);
			}
		}
		
		public void Delete(object sender, EventArgs e)
		{
			new MonoDevelop.TextEditor.Actions.Delete().Execute(textArea);
		}
		
		public void SelectAll(object sender, EventArgs e)
		{
			new MonoDevelop.TextEditor.Actions.SelectWholeDocument().Execute(textArea);
		}
	}
}
