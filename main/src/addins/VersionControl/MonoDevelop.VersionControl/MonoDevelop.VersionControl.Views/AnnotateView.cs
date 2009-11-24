//
// AnnotateView.cs
//
// Author:
//   Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com>
//
// Copyright (C) 2009 Levi Bard
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
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.SourceEditor;

using Mono.TextEditor;

namespace MonoDevelop.VersionControl.Views
{
	/// <summary>
	/// A view for displaying annotated source code.
	/// </summary>
	internal class AnnotateView
	{
		/// <summary>
		/// Retrieve and display annotations for a given VersionControlItem
		/// </summary>
		/// <param name="repo">
		/// A <see cref="Repository"/> to which file belongs
		/// </param>
		/// <param name="file">
		/// A <see cref="FilePath"/>: The file to annotate
		/// </param>
		/// <param name="test">
		/// A <see cref="System.Boolean"/>: Whether this is a test run
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>: Whether annotations are supported
		/// </returns>
		public static bool Show (Repository repo, FilePath file, bool test)
		{
			if (test){ 
				if (null != repo && repo.CanGetAnnotations (file)) {
					foreach (Ide.Gui.Document guidoc in IdeApp.Workbench.Documents) {
						if (guidoc.FileName.Equals (file)) {
							SourceEditorView seview  = guidoc.ActiveView as SourceEditorView;
							if (null != seview && 
							    seview.TextEditor.HasMargin (typeof (AnnotationMargin)) && 
							    seview.TextEditor.GetMargin (typeof (AnnotationMargin)).IsVisible) { 
								return false;
							}
						}
					}
					return true;
				}
				return false;
			}
			
			MonoDevelop.Ide.Gui.Document doc = MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (file, true);
			SourceEditorView view = doc.ActiveView as SourceEditorView;
			if (view != null) {
				if (view.TextEditor.HasMargin (typeof (AnnotationMargin))) { 
					view.TextEditor.GetMargin (typeof (AnnotationMargin)).IsVisible = true;
				} else {
					view.TextEditor.InsertMargin (0, new AnnotationMargin (repo, view.TextEditor, doc));
				}
				view.TextEditor.RedrawFromLine (0);
			}
			
			return true;
		}
		
		/// <summary>
		/// Hide annotations for a given VersionControlItem
		/// </summary>
		/// <param name="repo">
		/// A <see cref="Repository"/> to which file belongs
		/// </param>
		/// <param name="file">
		/// A <see cref="FilePath"/>: The file to annotate
		/// </param>
		/// <param name="test">
		/// A <see cref="System.Boolean"/>: Whether this is a test run
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>: Whether annotations are supported
		/// </returns>
		public static bool Hide (Repository repo, FilePath file, bool test)
		{
			if (test){ return (null != repo && repo.CanGetAnnotations (file) && !Show (repo, file, test)); }
			
			MonoDevelop.Ide.Gui.Document doc = MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (file, true);
			SourceEditorView view = doc.ActiveView as SourceEditorView;
			if (view != null && view.TextEditor.HasMargin (typeof (AnnotationMargin))) { 
				view.TextEditor.GetMargin (typeof (AnnotationMargin)).IsVisible = false;
				view.TextEditor.RedrawFromLine (0);
			}
			
			return true;
		}
	}
	
	/// <summary>
	/// Margin for displaying annotations
	/// </summary>
	internal class AnnotationMargin: Margin
	{
		Repository repo;
		List<string> annotations;
		Revision[] history;
		Pango.Layout layout;
		Gdk.GC lineNumberBgGC, lineNumberGC, lineNumberHighlightGC, locallyModifiedGC;
		Mono.TextEditor.TextEditor editor;
		AnnotationTooltipProvider tooltipProvider;
		
		private static readonly string locallyModified = "*****";
		
		public override int Width {
			get { return width; }
		}
		int width;
		
		/// <summary>
		/// Creates a new AnnotationMargin
		/// </summary>
		/// <param name="repo">
		/// A <see cref="Repository"/>: The repo to use for annotation
		/// </param>
		/// <param name="editor">
		/// A <see cref="Mono.TextEditor.TextEditor"/>: The editor to which the margin belongs
		/// </param>
		/// <param name="doc">
		/// A <see cref="Ide.Gui.Document"/>: The document to be annotated
		/// </param>
		public AnnotationMargin (Repository repo, Mono.TextEditor.TextEditor editor, Ide.Gui.Document doc)
		{
			this.repo = repo;
			this.width = 0;
			this.editor = editor;
			annotations = new List<string> ();
			UpdateAnnotations (null, null);
			
			tooltipProvider = new AnnotationTooltipProvider (this);
			
			editor.Document.TextReplacing += EditorDocumentTextReplacing;
			editor.Document.LineChanged += EditorDocumentLineChanged;
			editor.Caret.PositionChanged += EditorCarethandlePositionChanged;
			editor.TooltipProviders.Add (tooltipProvider);

			doc.Saved += UpdateAnnotations;
			
			layout = new Pango.Layout (editor.PangoContext);
			layout.FontDescription = editor.Options.Font;
			
			lineNumberBgGC = new Gdk.GC (editor.GdkWindow);
			lineNumberBgGC.RgbFgColor = editor.ColorStyle.LineNumber.BackgroundColor;
			
			lineNumberGC = new Gdk.GC (editor.GdkWindow);
			lineNumberGC.RgbFgColor = editor.ColorStyle.LineNumber.Color;
			
			lineNumberHighlightGC = new Gdk.GC (editor.GdkWindow);
			lineNumberHighlightGC.RgbFgColor = editor.ColorStyle.LineNumberFgHighlighted;
			
			locallyModifiedGC = new Gdk.GC (editor.GdkWindow);
			locallyModifiedGC.RgbFgColor = editor.ColorStyle.LineDirtyBg;
		}
		
		/// <summary>
		/// Reloads annotations for the current document
		/// </summary>
		private void UpdateAnnotations (object sender, EventArgs e)
		{
			ThreadPool.QueueUserWorkItem (delegate {
				annotations = new List<string> (repo.GetAnnotations (editor.Document.FileName));
				if (null == history) {
					try {
						history = repo.GetHistory (editor.Document.FileName, null);
					} catch (Exception ex) {
						LoggingService.LogError ("Error retrieving history", ex);
					}
				}
				
				bool redrawAll = (0 == width);
				DispatchService.GuiDispatch (delegate {
					UpdateWidth ();
					if (redrawAll) {
						editor.RedrawFromLine (0);
					} else {
						editor.RedrawMarginLines (this, 0, -1);
					}
				});
			});
		}

		/// <summary>
		/// Marks a line as locally modified
		/// </summary>
		private void EditorDocumentLineChanged (object sender, LineEventArgs e)
		{
			int  startLine = editor.Document.OffsetToLineNumber (e.Line.Offset),
			     endLine = editor.Document.OffsetToLineNumber (e.Line.EndOffset);
			
			if (startLine == endLine) {
				SetAnnotation (startLine, locallyModified);
			}
			for (int i=startLine; i<endLine; ++i) {
				SetAnnotation (i, locallyModified);
			}
		}

		/// <summary>
		/// Marks necessary lines modified when text is replaced
		/// </summary>
		private void EditorDocumentTextReplacing (object sender, ReplaceEventArgs e)
		{
			int  startLine = editor.Document.OffsetToLineNumber (e.Offset),
			     endLine = editor.Document.OffsetToLineNumber (e.Offset+e.Count),
			     lineCount = 0;
			string[] tokens = null;
			
			if (startLine < endLine) {
				// change crosses line boundary
				
				lineCount = endLine - startLine;
				
				if (string.IsNullOrEmpty (e.Value)) {
					// delete
					annotations.RemoveRange (startLine, lineCount);
				}  else {
					// replace
					annotations.RemoveRange (startLine, lineCount);
					for (int i=0; i<lineCount; ++i) {
						annotations.Insert (startLine, locallyModified);
					}
				}
				return;
			} else if (0 == e.Count) {
					// insert
					tokens = e.Value.Split (new string[]{Environment.NewLine}, StringSplitOptions.None);
					lineCount = tokens.Length - 1;
					
					for (int i=0; i<lineCount; ++i) {
						annotations.Insert (startLine+1, locallyModified);
					}
			} else if (startLine > endLine) {
				// revert
				UpdateAnnotations (null, null);
				return;
			}
			
			SetAnnotation (startLine, locallyModified);
		}

		/// <summary>
		/// Force line repaint on line change
		/// </summary>
		private void EditorCarethandlePositionChanged (object sender, DocumentLocationEventArgs e) 
		{
			if (e.Location.Line == editor.Caret.Line)
				return;
			editor.RedrawMarginLine (this, e.Location.Line);
			editor.RedrawMarginLine (this, editor.Caret.Line);
		}
	
		/// <summary>
		/// Calculate the maximum width required to render annotations
		/// </summary>
		private void UpdateWidth ()
		{
			int tmpwidth = 0,
			    height = 0;
			
			foreach (string note in annotations) {
				if (!string.IsNullOrEmpty (note)) { 
					layout.SetText (note + "_");
					layout.GetPixelSize (out tmpwidth, out height);
					width = Math.Max (width, tmpwidth);
				}
			}
		}
		
		public override void Dispose ()
		{
			editor.Document.TextReplacing -= EditorDocumentTextReplacing;
			editor.Document.LineChanged -= EditorDocumentLineChanged;
			editor.TooltipProviders.Remove (tooltipProvider);
			layout.Dispose ();
			lineNumberBgGC.Dispose ();
			lineNumberGC.Dispose ();
			lineNumberHighlightGC.Dispose ();
			locallyModifiedGC.Dispose ();
			base.Dispose ();
		}
		
		/// <summary>
		/// Render an annotation on each line
		/// </summary>
		protected override void Draw (Gdk.Drawable drawable, Gdk.Rectangle area, int line, int x, int y, int lineHeight)
		{
			string ann = (line < annotations.Count)? annotations[line]: string.Empty;
			Gdk.Rectangle drawArea = new Gdk.Rectangle (x, y, Width, lineHeight);
			drawable.DrawRectangle (locallyModified.Equals (ann, StringComparison.Ordinal)? locallyModifiedGC: lineNumberBgGC, true, drawArea);
			
			if (!locallyModified.Equals (ann, StringComparison.Ordinal) &&
			    (line < annotations.Count)) {
				layout.SetText (annotations[line]);
				drawable.DrawLayout ((editor.Caret.Line == line)? lineNumberHighlightGC: lineNumberGC, x + 1, y, layout);
			}
		}
		
		/// <summary>
		/// Sets a given annotation line, 
		/// accounting for end-of-buffer overrun.
		/// </summary>
		/// <param name="index">
		/// A <see cref="System.Int32"/>: The index of the item to be set.
		/// </param>
		/// <param name="text">
		/// A <see cref="System.String"/>: The annotation to be used.
		/// </param>
		void SetAnnotation (int index, string text)
		{
			int difference = index - annotations.Count;
			
			if (0 > difference) {
				annotations[index] = text;
			} else {
				for (int i=0; i<difference; ++i) {
					annotations.Add (locallyModified);
				}
				annotations.Add (text);
			}
		}
		
		/// <summary>
		/// Gets the commit message matching a given annotation index.
		/// </summary>
		internal string GetCommitMessage (int index)
		{
			string annotation = (index < annotations.Count)? annotations[index]: null;
			
			if (null != history && !string.IsNullOrEmpty (annotation))
			{
				string[] tokens = annotation.Split (new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
				if (1 < tokens.Length) {
					foreach (Revision rev in history) {
						if (rev.ToString ().Equals (tokens[0], StringComparison.Ordinal)) {
							return rev.Message;
						}
					}
				}
			}
			
			return null;
		}
	}
	
	/// <summary>
	/// Tooltip provider for annotation margins
	/// </summary>
	class AnnotationTooltipProvider: ITooltipProvider
	{
		AnnotationMargin margin;
		
		public AnnotationTooltipProvider (AnnotationMargin margin)
		{
			this.margin = margin;
		}
		
		#region ITooltipProvider implementation
		
		public object GetItem (Mono.TextEditor.TextEditor editor, int offset)
		{
			DocumentLocation location = editor.Document.OffsetToLocation (offset);
			return (0 >= location.Column)? margin.GetCommitMessage (location.Line): null;
		}
		
		
		public Window CreateTooltipWindow (Mono.TextEditor.TextEditor editor, Gdk.ModifierType modifierState, object item)
		{
			return new LanguageItemWindow (null, modifierState, null, null, item as string, null);
		}
		
		
		public void GetRequiredPosition (Mono.TextEditor.TextEditor editor, Window tipWindow, out int requiredWidth, out double xalign)
		{
			LanguageItemWindow win = (LanguageItemWindow) tipWindow;
			requiredWidth = win.SetMaxWidth (win.Screen.Width);
			xalign = 0.5;
		}
		
		
		public bool IsInteractive (Mono.TextEditor.TextEditor editor, Window tipWindow)
		{
			return false;
		}
		
		#endregion
	}
}
