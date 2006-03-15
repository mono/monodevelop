//
// GuiBuilderView.cs
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
	public class GuiBuilderView : AbstractViewContent, IEditableTextBuffer, IPositionable, IBookmarkBuffer, IDebuggableEditor, ICodeStyleOperations,
		IDocumentInformation
	{
		IViewContent content;
		Gtk.Notebook notebook;
		GuiBuilderEditSession editSession;
		Gtk.EventBox designerPage;
		VBox box;
		ToggleToolButton codeButton;
		ToggleToolButton designerButton;
		VBox designerBox;
		Button bindButton;
		
		Gtk.Widget currentDesigner;
		WidgetTreeCombo widgetsCombo;
		
		public GuiBuilderView (IViewContent content, GuiBuilderWindow window)
		{
			editSession = window.CreateEditSession (new OpenDocumentFileProvider ());
			this.content = content;
			
			content.ContentChanged += new EventHandler (OnTextContentChanged);
			content.DirtyChanged += new EventHandler (OnTextDirtyChanged);
			editSession.ModifiedChanged += new EventHandler (OnWindowChanged);
			editSession.SteticProject.Selected += new Stetic.Project.SelectedHandler (OnSelectionChanged);
			
			notebook = new Gtk.Notebook ();
			designerPage = new Gtk.EventBox ();
			designerPage.Show ();
			notebook.AppendPage (content.Control, new Gtk.Label ());
			notebook.AppendPage (designerPage, new Gtk.Label ());
			notebook.TabPos = Gtk.PositionType.Bottom;
			notebook.ShowTabs = false;
			notebook.Show ();
			box = new VBox ();
			
			// Bottom toolbar
			
			Toolbar hbox = new Toolbar ();
			hbox.IconSize = IconSize.SmallToolbar;
			hbox.ToolbarStyle = ToolbarStyle.BothHoriz;
			hbox.ShowArrow = false;
			codeButton = new ToggleToolButton ();
			codeButton.Label = GettextCatalog.GetString ("Source Code");
			codeButton.IsImportant = true;
			codeButton.Active = true;
			codeButton.Clicked += new EventHandler (OnToggledCode);
			designerButton = new ToggleToolButton ();
			designerButton.Label = GettextCatalog.GetString ("Designer");
			designerButton.IsImportant = true;
			designerButton.Clicked += new EventHandler (OnToggledDesigner);
			
			hbox.Insert (codeButton, -1);
			hbox.Insert (designerButton, -1);
			hbox.ShowAll ();
			
			box.PackStart (notebook, true, true, 0);
			box.PackStart (hbox, false, false, 0);
			
			box.Show ();
			
			// Widget toolbar
			HBox widgetBar = new HBox ();
			bindButton = new Button ("");
			bindButton.Relief = Gtk.ReliefStyle.None;
			bindButton.Label = GettextCatalog.GetString ("Bind to Field");
			bindButton.Clicked += new EventHandler (OnBindWidget);
			
			// Widget combobox
			widgetsCombo = new WidgetTreeCombo ();
			widgetsCombo.RootWidget = editSession.RootWidget;
			
			widgetBar.PackStart (widgetsCombo, false, false, 0);
			widgetBar.PackStart (bindButton, false, false, 3);
			widgetBar.BorderWidth = 3;
			
			// Designer page
			designerBox = new VBox ();
			designerPage.Add (designerBox);
			designerBox.PackStart (widgetBar, false, false, 0);
			designerBox.ShowAll ();
			
			designerBox.PackStart (editSession.WrapperWidget, true, true, 0);
			
			currentDesigner = editSession.WrapperWidget;
			
			editSession.RootWidgetChanged += new EventHandler (OnRootWidgetChanged);
		}
		
		public override MonoDevelop.Projects.Project Project {
			get { return base.Project; }
			set { 
				base.Project = value; 
				content.Project = value; 
			}
		}
		
		void OnRootWidgetChanged (object o, EventArgs a)
		{
			designerBox.Remove (currentDesigner);
			currentDesigner = editSession.WrapperWidget;
			designerBox.PackStart (currentDesigner, true, true, 0);
			editSession.WrapperWidget.ShowAll ();
			widgetsCombo.RootWidget = editSession.RootWidget;
		}
		
		void OnBindWidget (object o, EventArgs a)
		{
			GuiBuilderService.AddCurrentWidgetToClass ();
		}
		
		protected override void OnWorkbenchWindowChanged (EventArgs e)
		{
			base.OnWorkbenchWindowChanged (e);
			content.WorkbenchWindow = WorkbenchWindow;
		}
		
		public GuiBuilderEditSession EditSession {
			get { return editSession; }
		}
		
		void OnSelectionChanged (Gtk.Widget selection, Stetic.ProjectNode node)
		{
			Stetic.Wrapper.Widget sel = Stetic.Wrapper.Widget.Lookup (editSession.SteticProject.Selection);
			bindButton.Sensitive = (sel != null && sel != editSession.RootWidget);
		}
		
		void OnToggledCode (object s, EventArgs args)
		{
			if (codeButton.Active) {
				notebook.CurrentPage = 0;
				designerButton.Active = false;
			}
			else if (!designerButton.Active)
				codeButton.Active = true;
		}
		
		void OnToggledDesigner (object s, EventArgs args)
		{
			if (designerButton.Active) {
				notebook.CurrentPage = 1;
				codeButton.Active = false;
			}
			else if (!codeButton.Active)
				designerButton.Active = true;
		}
		
		public override void Dispose ()
		{
			designerPage.Remove (editSession.WrapperWidget);
			editSession.Dispose ();
			content.ContentChanged -= new EventHandler (OnTextContentChanged);
			content.DirtyChanged -= new EventHandler (OnTextDirtyChanged);
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
			editSession.UpdateBindings (fileName);
			editSession.Save ();
		}
		
		public override bool IsDirty {
			get {
				return content.IsDirty || editSession.Modified;
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
		
		public void AddCurrentWidgetToClass ()
		{
			editSession.AddCurrentWidgetToClass ();
		}
		
		public void JumpToSignalHandler (Stetic.Wrapper.Signal signal)
		{
			IClass cls = editSession.GetClass ();
			foreach (IMethod met in cls.Methods) {
				if (met.Name == signal.Handler) {
					if (!codeButton.Active)
						codeButton.Active = true;
					JumpTo (met.Region.BeginLine, met.Region.BeginColumn);
					break;
				}
			}
		}
		
		public void ShowDesignerView ()
		{
			designerButton.Active = true;
		}
		
		void OnWindowChanged (object s, EventArgs args)
		{
//			Console.WriteLine (Environment.StackTrace);
			OnContentChanged (args);
			OnDirtyChanged (args);
		}
		
		void OnTextContentChanged (object s, EventArgs args)
		{
			OnContentChanged (args);
		}
		
		void OnTextDirtyChanged (object s, EventArgs args)
		{
			OnDirtyChanged (args);
		}
		
		/* Commands *********************************/
		
		[CommandHandler (EditCommands.Delete)]
		protected void OnDelete ()
		{
			if (editSession.SteticProject.Selection != null) {
				Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (editSession.SteticProject.Selection);
				wrapper.Delete ();
			}
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			if (editSession.SteticProject.Selection != null)
				Stetic.Clipboard.Copy (editSession.SteticProject.Selection);
		}
		
		[CommandHandler (EditCommands.Cut)]
		protected void OnCut ()
		{
			if (editSession.SteticProject.Selection != null)
				Stetic.Clipboard.Cut (editSession.SteticProject.Selection);
		}
		
		[CommandHandler (EditCommands.Paste)]
		protected void OnPaste ()
		{
			if (editSession.SteticProject.Selection != null)
				Stetic.Clipboard.Cut (editSession.SteticProject.Selection as Stetic.Placeholder);
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
			if (!codeButton.Active)
				codeButton.Active = true;
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

