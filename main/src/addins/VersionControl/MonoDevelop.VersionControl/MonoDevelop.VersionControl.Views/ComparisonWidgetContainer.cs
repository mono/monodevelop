// 
// ComparisonWidgetContainer.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using Mono.TextEditor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Diff;
using MonoDevelop.Ide.Gui.Dialogs;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl.Views
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ComparisonWidgetContainer : Gtk.Bin
	{
		Mono.TextEditor.TextEditor diffEditor;
		ComparisonWidget widget;
		public ComparisonWidget ComparisonWidget {
			get {
				return this.widget;
			}
		}
		public ComparisonWidgetContainer (MonoDevelop.Ide.Gui.Document doc, VersionControlItem item)
		{
			this.Build ();
			diffEditor = new Mono.TextEditor.TextEditor ();
			diffEditor.Document.MimeType = "text/x-diff";
			diffEditor.Options.FontName = doc.TextEditorData.Options.FontName;
			diffEditor.Options.ColorScheme = doc.TextEditorData.Options.ColorScheme;
			diffEditor.Options.ShowFoldMargin = false;
			diffEditor.Options.ShowIconMargin = false;
			diffEditor.Options.ShowTabs = true;
			diffEditor.Options.ShowSpaces = true;
			diffEditor.Options.ShowInvalidLines = doc.TextEditorData.Options.ShowInvalidLines;
			diffEditor.Document.ReadOnly = true;
			scrolledwindow1.Child = diffEditor;
			diffEditor.ShowAll ();
			
			widget = new ComparisonWidget ();
			widget.LeftEditor.Document.MimeType = widget.RightEditor.Document.MimeType = doc.TextEditorData.Document.MimeType;
			widget.LeftEditor.Options.FontName = widget.RightEditor.Options.FontName = doc.TextEditorData.Options.FontName;
			widget.LeftEditor.Options.ColorScheme = widget.RightEditor.Options.ColorScheme = doc.TextEditorData.Options.ColorScheme;
			widget.LeftEditor.Options.ShowFoldMargin = widget.RightEditor.Options.ShowFoldMargin = false;
			widget.LeftEditor.Options.ShowIconMargin = widget.RightEditor.Options.ShowIconMargin = false;
			
			widget.LeftEditor.Document = doc.TextEditorData.Document;
			widget.RightEditor.Document.Text = System.IO.File.ReadAllText (item.Repository.GetPathToBaseText (item.Path));
			widget.ShowAll ();
			
			widget.LeftEditor.Document.TextReplaced += HandleWidgetLeftEditorDocumentTextReplaced;
			
			HandleWidgetLeftEditorDocumentTextReplaced (null, null);
			notebook3.Add (widget);
			notebook3.Page = 1;
			widget.textButton.Clicked += delegate {
				var writer = new System.IO.StringWriter ();
				UnifiedDiff.WriteUnifiedDiff (widget.Diff, writer, 
				                              System.IO.Path.GetFileName(item.Path) + "    (repository)", 
				                              System.IO.Path.GetFileName(item.Path) + "    (working copy)",
				                              3);
				diffEditor.Document.Text = writer.ToString ();
				notebook3.Page = 0;
			};
			buttonGraph.Clicked += delegate {
				notebook3.Page = 1;
			};
			
			buttonSave.Clicked += delegate {
				var dlg = new OpenFileDialog (GettextCatalog.GetString ("Save as..."), FileChooserAction.Save) {
					TransientFor = IdeApp.Workbench.RootWindow
				};
				
				if (!dlg.Run ())
					return;
				File.WriteAllText (dlg.SelectedFile, diffEditor.Document.Text);
			};
		}
		
		void HandleWidgetLeftEditorDocumentTextReplaced (object sender, Mono.TextEditor.ReplaceEventArgs e)
		{
			var leftLines = from l in widget.LeftEditor.Document.Lines select widget.LeftEditor.Document.GetTextAt (l.Offset, l.EditableLength);
			var rightLines = from l in widget.RightEditor.Document.Lines select widget.RightEditor.Document.GetTextAt (l.Offset, l.EditableLength);
			
			widget.Diff = new Diff (rightLines.ToArray (), leftLines.ToArray (), true, true);
			widget.QueueDraw ();
		}
	}
	
}

