//
// WrapperDesignView.cs: base class for wrapping an IViewContent. Heavily based on 
//         MonoDevelop.GtkCore.GuiBuilder.CombinedDesignView
//
// Author:
//   Michael Hutchinson
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Michael Hutchinson
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

namespace MonoDevelop.DesignerSupport
{
	
	public class WrapperDesignView : AbstractViewContent, IEditableTextBuffer, IPositionable, IBookmarkBuffer, IDebuggableEditor, ICodeStyleOperations,
		IDocumentInformation, IEncodedTextContent
	{
		IViewContent content;
		Gtk.VBox contentBox;
		Gtk.Widget topBar;
		
		public WrapperDesignView  (IViewContent content)
		{
			this.content = content;
			this.contentBox = new Gtk.VBox ();
			this.contentBox.PackEnd (content.Control, true, true, 0);
			this.contentBox.ShowAll ();
			
			content.ContentChanged += new EventHandler (OnTextContentChanged);
			content.DirtyChanged += new EventHandler (OnTextDirtyChanged);
			
			IdeApp.Workbench.ActiveDocumentChanged += new EventHandler (OnActiveDocumentChanged);
		}
		
		public Gtk.Widget TopBar {
			get {
				return topBar;
			}
			protected set {				
				if (topBar != null)
					contentBox.Remove (topBar);
				
				if (value != null)		
					contentBox.PackStart (value, false, false, 0);
				topBar = value;
			}
		}
		
		protected IViewContent Content {
			get { return content; }
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
		
		public override void Dispose ()
		{
			content.ContentChanged -= new EventHandler (OnTextContentChanged);
			content.DirtyChanged -= new EventHandler (OnTextDirtyChanged);
			IdeApp.Workbench.ActiveDocumentChanged -= new EventHandler (OnActiveDocumentChanged);
			content.Dispose ();
			base.Dispose ();
		}
		
		public override void Load (string fileName)
		{
			ContentName = fileName;
			content.Load (fileName);
		}
		
		public override Gtk.Widget Control {
			get { return contentBox; }
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
		
		public override string ContentName {
			get { return content.ContentName; }
			set { content.ContentName = value; }
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
			if (IdeApp.Workbench.ActiveDocument.GetContent<WrapperDesignView> () == this)
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
