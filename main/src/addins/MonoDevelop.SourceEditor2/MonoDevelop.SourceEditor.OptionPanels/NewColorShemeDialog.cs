// 
// NewColorShemeDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	partial class NewColorShemeDialog : Gtk.Dialog
	{
		Gtk.ListStore store = new Gtk.ListStore (typeof(string));
			
		public NewColorShemeDialog ()
		{
			this.Build ();
			
			foreach (string styleName in SyntaxHighlightingService.Styles) {
				store.AppendValues (styleName);
			}
			comboboxBaseStyle.Model = store;
			comboboxBaseStyle.Active = 0;
			
			this.entryName.Changed += HandleEntryNameChanged;
			this.entryDescription.Changed += HandleEntryNameChanged;
			this.entryDescription.Visible = label3.Visible = false;
			this.buttonOk.Clicked += HandleButtonOkClicked;
			this.buttonOk.Sensitive = false;
			
		}
		
		void HandleEntryNameChanged (object sender, EventArgs e)
		{
			this.buttonOk.Sensitive = !string.IsNullOrEmpty (entryName.Text);
		}

		void HandleButtonOkClicked (object sender, EventArgs e)
		{
			Gtk.TreeIter iter;
			if (!store.IterNthChild (out iter, comboboxBaseStyle.Active))
				return;
			string name = (string)store.GetValue (iter, 0);

			var style = SyntaxHighlightingService.GetEditorTheme (name);

			style = style.CloneWithName (entryName.Text);

			string path = MonoDevelop.Ide.Editor.TextEditorDisplayBinding.SyntaxModePath;
			string baseName = style.Name.Replace (" ", "_");

			while (File.Exists (System.IO.Path.Combine (path, baseName + ".tmTheme"))) {
				baseName = baseName + "_";
			}
			string fileName = System.IO.Path.Combine (path, baseName + ".tmTheme");
			try {
				using (var writer = new StreamWriter (fileName))
					TextMateFormat.Save (writer, style); 
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}
		}
	}
}

