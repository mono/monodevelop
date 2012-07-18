// 
// RefactoringPreviewDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using Gtk;
using Gdk;

using MonoDevelop.Core;
using Mono.TextEditor;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;


namespace MonoDevelop.Refactoring
{
	public partial class RefactoringPreviewDialog : Gtk.Dialog
	{
		TreeStore store = new TreeStore (typeof(Gdk.Pixbuf), typeof(string), typeof(object), typeof (bool));

		const int pixbufColumn = 0;
		const int textColumn = 1;
		const int objColumn = 2;
		const int statusVisibleColumn = 3;

		List<Change> changes;

		public RefactoringPreviewDialog (List<Change> changes)
		{
			this.Build ();
			this.changes = changes;
			treeviewPreview.Model = store;

			TreeViewColumn column = new TreeViewColumn ();

			// pixbuf column
			var pixbufCellRenderer = new CellRendererPixbuf ();
			column.PackStart (pixbufCellRenderer, false);
			column.SetAttributes (pixbufCellRenderer, "pixbuf", pixbufColumn);
			column.AddAttribute (pixbufCellRenderer, "visible", statusVisibleColumn);
			
			// text column
			CellRendererText cellRendererText = new CellRendererText ();
			column.PackStart (cellRendererText, false);
			column.SetAttributes (cellRendererText, "text", textColumn);
			column.AddAttribute (cellRendererText, "visible", statusVisibleColumn);
			
			// location column
			CellRendererText cellRendererText2 = new CellRendererText ();
			column.PackStart (cellRendererText2, false);
			column.SetCellDataFunc (cellRendererText2, new TreeCellDataFunc (SetLocationTextData));
			
			CellRendererDiff cellRendererDiff = new CellRendererDiff ();
			column.PackStart (cellRendererDiff, true);
			column.SetCellDataFunc (cellRendererDiff, new TreeCellDataFunc (SetDiffCellData));

			treeviewPreview.AppendColumn (column);
			treeviewPreview.HeadersVisible = false;
			
			buttonCancel.Clicked += delegate {
				Destroy ();
			};
			
			buttonOk.Clicked += delegate {
				IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (this.Title, null);
				RefactoringService.AcceptChanges (monitor, changes);
				
				Destroy ();
			};
			
			FillChanges ();
		}
		
		void SetLocationTextData (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererText cellRendererText = (CellRendererText)cell;
			Change change = store.GetValue (iter, objColumn) as Change;
			cellRendererText.Visible = (bool)store.GetValue (iter, statusVisibleColumn);
			TextReplaceChange replaceChange = change as TextReplaceChange;
			if (replaceChange == null) {
				cellRendererText.Text = "";
				return;
			}
			
			Mono.TextEditor.TextDocument doc = new Mono.TextEditor.TextDocument ();
			doc.Text = Mono.TextEditor.Utils.TextFileUtility.ReadAllText (replaceChange.FileName);
			DocumentLocation loc = doc.OffsetToLocation (replaceChange.Offset);
			
			string text = string.Format (GettextCatalog.GetString ("(Line:{0}, Column:{1})"), loc.Line, loc.Column);
			if (treeviewPreview.Selection.IterIsSelected (iter)) {
				cellRendererText.Text = text;
			} else {
				cellRendererText.Markup = "<span foreground=\"" + MonoDevelop.Components.PangoCairoHelper.GetColorString (Style.Text (StateType.Insensitive)) + "\">" + text + "</span>";
			}
		}
		
		void SetDiffCellData (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			try {
				CellRendererDiff cellRendererDiff = (CellRendererDiff)cell;
				Change change = store.GetValue (iter, objColumn) as Change;
				cellRendererDiff.Visible = !(bool)store.GetValue (iter, statusVisibleColumn);
				if (change == null || !cellRendererDiff.Visible) {
					cellRendererDiff.InitCell (treeviewPreview, false, "", "");
					return;
				}
				TextReplaceChange replaceChange = change as TextReplaceChange;
				if (replaceChange == null) 
					return;
			
				var openDocument = IdeApp.Workbench.GetDocument (replaceChange.FileName);
				Mono.TextEditor.TextDocument originalDocument = new Mono.TextEditor.TextDocument ();
				originalDocument.FileName = replaceChange.FileName;
				if (openDocument == null) {
					originalDocument.Text = Mono.TextEditor.Utils.TextFileUtility.ReadAllText (replaceChange.FileName);
				} else {
					originalDocument.Text = openDocument.Editor.Document.Text;
				}
				
				Mono.TextEditor.TextDocument changedDocument = new Mono.TextEditor.TextDocument ();
				changedDocument.FileName = replaceChange.FileName;
				changedDocument.Text = originalDocument.Text;
				
				changedDocument.Replace (replaceChange.Offset, replaceChange.RemovedChars, replaceChange.InsertedText);
				
				string diffString = Mono.TextEditor.Utils.Diff.GetDiffString (originalDocument, changedDocument);
				
				cellRendererDiff.InitCell (treeviewPreview, true, diffString, replaceChange.FileName);
			} catch (Exception e) {
				Console.WriteLine (e);
			}
		}

		Dictionary<string, TreeIter> fileDictionary = new Dictionary<string, TreeIter> ();
		TreeIter GetFile (Change change)
		{
			TextReplaceChange replaceChange = change as TextReplaceChange;
			if (replaceChange == null) 
				return TreeIter.Zero;
			
			TreeIter result;
			if (!fileDictionary.TryGetValue (replaceChange.FileName, out result))
				fileDictionary[replaceChange.FileName] = result = store.AppendValues (DesktopService.GetPixbufForFile (replaceChange.FileName, IconSize.Menu), System.IO.Path.GetFileName (replaceChange.FileName), null, true);
			return result;
		}

		void FillChanges ()
		{
			foreach (Change change in changes) {
				TreeIter iter = GetFile (change);
				if (iter.Equals (TreeIter.Zero)) {
					iter = store.AppendValues (ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.ReplaceIcon, IconSize.Menu), change.Description, change, true);
				} else {
					iter = store.AppendValues (iter, ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.ReplaceIcon, IconSize.Menu), change.Description, change, true);
				}
				TextReplaceChange replaceChange = change as TextReplaceChange;
				if (replaceChange != null && replaceChange.Offset >= 0)
					store.AppendValues (iter, null, null, change, false);
			}
			if (changes.Count < 4) {
				treeviewPreview.ExpandAll ();
			} else {
				foreach (TreeIter iter in fileDictionary.Values) {
					treeviewPreview.ExpandRow (store.GetPath (iter), false);
				}
			}
		}

		class CellRendererDiff : Gtk.CellRendererText
		{
			Pango.Layout layout;
			Pango.FontDescription font;
			bool diffMode;
			int width, height, lineHeight;
			string[] lines;

			public CellRendererDiff ()
			{
				font = Pango.FontDescription.FromString (DesktopService.DefaultMonospaceFont);
			}

			void DisposeLayout ()
			{
				if (layout != null) {
					layout.Dispose ();
					layout = null;
				}
			}

			bool isDisposed = false;
			protected override void OnDestroyed ()
			{
				isDisposed = true;
				DisposeLayout ();
				if (font != null) {
					font.Dispose ();
					font = null;
				}
				base.OnDestroyed ();
			}

			public void Reset ()
			{
			}

			public void InitCell (Widget container, bool diffMode, string text, string path)
			{
				if (isDisposed)
					return;
				this.diffMode = diffMode;

				if (diffMode) {
					if (text.Length > 0) {
						lines = text.Split ('\n');
						int maxlen = -1;
						int maxlin = -1;
						for (int n = 0; n < lines.Length; n++) {
							if (lines[n].Length > maxlen) {
								maxlen = lines[n].Length;
								maxlin = n;
							}
						}
						DisposeLayout ();
						layout = CreateLayout (container, lines[maxlin]);
						layout.GetPixelSize (out width, out lineHeight);
						height = lineHeight * lines.Length;
					} else
						width = height = 0;
				} else {
					DisposeLayout ();
					layout = CreateLayout (container, text);
					layout.GetPixelSize (out width, out height);
				}
			}

			Pango.Layout CreateLayout (Widget container, string text)
			{
				Pango.Layout layout = new Pango.Layout (container.PangoContext);
				layout.SingleParagraphMode = false;
				if (diffMode) {
					layout.FontDescription = font;
					layout.SetText (text);
				} else
					layout.SetMarkup (text);
				return layout;
			}

			protected override void Render (Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
			{
				if (isDisposed)
					return;
				try {
					if (diffMode) {
						int w, maxy;
						window.GetSize (out w, out maxy);

						int recty = cell_area.Y;
						int recth = cell_area.Height - 1;
						if (recty < 0) {
							recth += recty + 1;
							recty = -1;
						}
						if (recth > maxy + 2)
							recth = maxy + 2;

						window.DrawRectangle (widget.Style.BaseGC (Gtk.StateType.Normal), true, cell_area.X, recty, cell_area.Width - 1, recth);

						Gdk.GC normalGC = widget.Style.TextGC (StateType.Normal);
						Gdk.GC removedGC = new Gdk.GC (window);
						removedGC.Copy (normalGC);
						removedGC.RgbFgColor = new Color (255, 0, 0);
						Gdk.GC addedGC = new Gdk.GC (window);
						addedGC.Copy (normalGC);
						addedGC.RgbFgColor = new Color (0, 0, 255);
						Gdk.GC infoGC = new Gdk.GC (window);
						infoGC.Copy (normalGC);
						infoGC.RgbFgColor = new Color (0xa5, 0x2a, 0x2a);

						int y = cell_area.Y + 2;

						for (int n = 0; n < lines.Length; n++,y += lineHeight) {
							if (y + lineHeight < 0)
								continue;
							if (y > maxy)
								break;
							string line = lines[n];
							if (line.Length == 0)
								continue;

							Gdk.GC gc;
							switch (line[0]) {
							case '-':
								gc = removedGC;
								break;
							case '+':
								gc = addedGC;
								break;
							case '@':
								gc = infoGC;
								break;
							default:
								gc = normalGC;
								break;
							}

							layout.SetText (line);
							window.DrawLayout (gc, cell_area.X + 2, y, layout);
						}
						window.DrawRectangle (widget.Style.DarkGC (Gtk.StateType.Prelight), false, cell_area.X, recty, cell_area.Width - 1, recth);
						removedGC.Dispose ();
						addedGC.Dispose ();
						infoGC.Dispose ();
					} else {
						int y = cell_area.Y + (cell_area.Height - height) / 2;
						window.DrawLayout (widget.Style.TextGC (GetState (flags)), cell_area.X, y, layout);
					}
				} catch (Exception e) {
					Console.WriteLine (e);
				}
			}

			public override void GetSize (Widget widget, ref Rectangle cell_area, out int x_offset, out int y_offset, out int c_width, out int c_height)
			{
				x_offset = y_offset = 0;
				c_width = width;
				c_height = height;

				if (diffMode) {
					// Add some spacing for the margin
					c_width += 4;
					c_height += 4;
				}
			}

			StateType GetState (CellRendererState flags)
			{
				if ((flags & CellRendererState.Selected) != 0)
					return StateType.Selected; else
					return StateType.Normal;
			}
		}
	}
}
