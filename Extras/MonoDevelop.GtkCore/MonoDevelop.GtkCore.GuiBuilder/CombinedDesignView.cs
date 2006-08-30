//
// CombinedDesignView.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Parser;

using Gtk;
using Gdk;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class CombinedDesignView : AbstractViewContent, IEditableTextBuffer, IPositionable, IBookmarkBuffer, IDebuggableEditor, ICodeStyleOperations,
		IDocumentInformation, IEncodedTextContent
	{
		IViewContent content;
		Gtk.Notebook notebook;
		VBox box;
		Toolbar toolbar;
		
		bool updating;
		
		public CombinedDesignView (IViewContent content)
		{
			this.content = content;
			
			content.ContentChanged += new EventHandler (OnTextContentChanged);
			content.DirtyChanged += new EventHandler (OnTextDirtyChanged);
			
			notebook = new Gtk.Notebook ();
			
			// Main notebook
			
			notebook.TabPos = Gtk.PositionType.Bottom;
			notebook.ShowTabs = false;
			notebook.Show ();
			box = new VBox ();
			
			// Bottom toolbar
			
			toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.SmallToolbar;
			toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
			toolbar.ShowArrow = false;
			
			AddButton (GettextCatalog.GetString ("Source Code"), content.Control).Active = true;
			
			toolbar.ShowAll ();
			
			box.PackStart (notebook, true, true, 0);
			box.PackStart (toolbar, false, false, 0);
			
			box.Show ();
			
			IdeApp.Workbench.ActiveDocumentChanged += new EventHandler (OnActiveDocumentChanged);
		}
		
		protected ToggleToolButton AddButton (string label, Gtk.Widget page)
		{
			updating = true;
			ToggleToolButton button = new ToggleToolButton ();
			button.Label = label;
			button.IsImportant = true;
			button.Clicked += new EventHandler (OnButtonToggled);
			button.ShowAll ();
			toolbar.Insert (button, -1);
			notebook.AppendPage (page, new Gtk.Label ());
			updating = false;
			return button;
		}
		
		public override MonoDevelop.Projects.Project Project {
			get { return base.Project; }
			set { 
				base.Project = value; 
				content.Project = value; 
			}
		}
		
		protected override void OnWorkbenchWindowChanged (EventArgs e)
		{
			base.OnWorkbenchWindowChanged (e);
			content.WorkbenchWindow = WorkbenchWindow;
		}
		
		void OnButtonToggled (object s, EventArgs args)
		{
			int i = Array.IndexOf (toolbar.Children, s);
			if (i != -1)
				ShowPage (i);
		}
		
		public void ShowPage (int npage)
		{
			if (notebook.CurrentPage == npage)
				return;
				
			if (updating) return;
			updating = true;
			
			notebook.CurrentPage = npage;
			Gtk.Widget[] buttons = toolbar.Children;
			for (int n=0; n<buttons.Length; n++) {
				ToggleToolButton b = (ToggleToolButton) buttons [n];
				b.Active = (n == npage);
			}

			updating = false;
		}
		
		public override void Dispose ()
		{
			content.ContentChanged -= new EventHandler (OnTextContentChanged);
			content.DirtyChanged -= new EventHandler (OnTextDirtyChanged);
			IdeApp.Workbench.ActiveDocumentChanged -= new EventHandler (OnActiveDocumentChanged);
			Gtk.Widget w = content.Control;
			content.Dispose ();
			w.Destroy ();
			content = null;
			box.Destroy ();
			box = null;
			base.Dispose ();
		}
		
		public override void Load (string fileName)
		{
			ContentName = fileName;
			content.Load (fileName);
		}
		
		public override Gtk.Widget Control {
			get { return box; }
		}
		
		public override void Save (string fileName)
		{
			content.Save (fileName);
		}
		
		public override bool IsDirty {
			get {
				return content.IsDirty;
			}
			set {
				content.IsDirty = value;
			}
		}
		
		public override bool IsReadOnly
		{
			get {
				return content.IsReadOnly;
			}
		}
		
		public virtual void AddCurrentWidgetToClass ()
		{
		}
		
		public virtual void JumpToSignalHandler (Stetic.Signal signal)
		{
		}
		
		void OnTextContentChanged (object s, EventArgs args)
		{
			OnContentChanged (args);
		}
		
		void OnTextDirtyChanged (object s, EventArgs args)
		{
			OnDirtyChanged (args);
		}
		
		void OnActiveDocumentChanged (object s, EventArgs args)
		{
			if (IdeApp.Workbench.ActiveDocument.Content == this)
				OnDocumentActivated ();
		}
		
		protected virtual void OnDocumentActivated ()
		{
		}
		
		/* IEditableTextBuffer **********************/
		
		public IClipboardHandler ClipboardHandler {
			get { return ((IEditableTextBuffer)content).ClipboardHandler; }
		}
		
		public void Undo()
		{
			((IEditableTextBuffer)content).Undo ();
		}
		
		public void Redo()
		{
			((IEditableTextBuffer)content).Redo ();
		}
		
		public string SelectedText {
			get { return ((IEditableTextBuffer)content).SelectedText; } 
			set { ((IEditableTextBuffer)content).SelectedText = value; }
		}
		
		public event EventHandler TextChanged {
			add { ((IEditableTextBuffer)content).TextChanged += value; }
			remove { ((IEditableTextBuffer)content).TextChanged -= value; }
		}
		
		public void InsertText (int position, string text)
		{
			((IEditableTextBuffer)content).InsertText (position, text);
		}
		
		public void DeleteText (int position, int length)
		{
			((IEditableTextBuffer)content).DeleteText (position, length);
		}
		
		/* IEncodedTextContent **************/
		
		public string SourceEncoding {
			get { return ((IEncodedTextContent)content).SourceEncoding; }
		}
		
		public void Save (string fileName, string encoding)
		{
			((IEncodedTextContent)content).Save (fileName, encoding);
		}
		
		public void Load (string fileName, string encoding)
		{
			((IEncodedTextContent)content).Load (fileName, encoding);
		}
		
		/* ITextBuffer **********************/
		
		public string Name {
			get { return ((ITextFile)content).Name; } 
		}
		
		public int Length {
			get { return ((ITextFile)content).Length; } 
		}
		
		public string Text {
			get { return ((IEditableTextFile)content).Text; }
			set { ((IEditableTextFile)content).Text = value; }
		}
		
		public int CursorPosition {
			get { return ((ITextBuffer)content).CursorPosition; } 
			set { ((ITextBuffer)content).CursorPosition = value; }
		}

		public int SelectionStartPosition {
			get { return ((ITextBuffer)content).SelectionStartPosition; } 
		}
		public int SelectionEndPosition {
			get { return ((ITextBuffer)content).SelectionEndPosition; } 
		}
		
		public string GetText (int startPosition, int endPosition)
		{
			return ((ITextBuffer)content).GetText (startPosition, endPosition);
		}
		
		public void Select (int startPosition, int endPosition)
		{
			((ITextBuffer)content).Select (startPosition, endPosition);
		}
		
		public void ShowPosition (int position)
		{
			((ITextBuffer)content).ShowPosition (position);
		}
		
		public int GetPositionFromLineColumn (int line, int column)
		{
			return ((ITextBuffer)content).GetPositionFromLineColumn (line, column);
		}
		
		public void GetLineColumnFromPosition (int position, out int line, out int column)
		{
			((ITextBuffer)content).GetLineColumnFromPosition (position, out line, out column);
		}
		
		/* IPositionable **********************/
		
		public void JumpTo(int line, int column)
		{
			ShowPage (0);
			((IPositionable)content).JumpTo (line, column);
		}
		
		/* IBookmarkBuffer **********************/
		
		public void SetBookmarked (int position, bool mark)
		{
			((IBookmarkBuffer)content).SetBookmarked (position, mark);
		}
		
		public bool IsBookmarked (int position)
		{
			return ((IBookmarkBuffer)content).IsBookmarked (position);
		}
		
		public void PrevBookmark ()
		{
			((IBookmarkBuffer)content).PrevBookmark ();
		}
		
		public void NextBookmark ()
		{
			((IBookmarkBuffer)content).NextBookmark ();
		}
		
		public void ClearBookmarks ()
		{
			((IBookmarkBuffer)content).ClearBookmarks ();
		}
		
		/* IDebuggableEditor **********************/
		
		public void ExecutingAt (int lineNumber)
		{
			((IDebuggableEditor)content).ExecutingAt (lineNumber);
		}
		
		public void ClearExecutingAt (int lineNumber)
		{
			((IDebuggableEditor)content).ExecutingAt (lineNumber);
		}
		
		/* ICodeStyleOperations **********************/
		
		public void CommentCode ()
		{
			((ICodeStyleOperations)content).CommentCode ();
		}
		
		public void UncommentCode ()
		{
			((ICodeStyleOperations)content).UncommentCode ();
		}
		
		public void IndentSelection ()
		{
			((ICodeStyleOperations)content).IndentSelection ();
		}
		
		public void UnIndentSelection ()
		{
			((ICodeStyleOperations)content).UnIndentSelection ();
		}
				
		/* IDocumentInformation **********************/
		
		public string FileName {
			get { return ((IDocumentInformation)content).FileName; }
		}
		
		public ITextIterator GetTextIterator ()
		{
			return ((IDocumentInformation)content).GetTextIterator ();
		}
		
		public string GetLineTextAtOffset (int offset)
		{
			return ((IDocumentInformation)content).GetLineTextAtOffset (offset);
		}
	}
}

