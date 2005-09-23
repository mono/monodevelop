// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

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
