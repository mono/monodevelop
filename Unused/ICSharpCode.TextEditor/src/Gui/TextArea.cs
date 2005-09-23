// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using MonoDevelop.TextEditor.Actions;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.EditorBindings.Properties;

namespace MonoDevelop.TextEditor
{
	public delegate bool KeyEventHandler(char ch);
	
	/// <summary>
	/// This class paints the textarea.
	/// </summary>
	//[ToolboxItem(false)]
	public class TextArea : Gtk.DrawingArea 
	{
		public static bool HiddenMouseCursor = false;
		
		System.Drawing.Point virtualTop        = new System.Drawing.Point(0, 0);
		TextAreaControl         motherTextAreaControl;
		TextEditorControl       motherTextEditorControl;
		
		ArrayList                 bracketshemes  = new ArrayList();
		TextAreaClipboardHandler  textAreaClipboardHandler;
		bool autoClearSelection = false;
		
		ArrayList  leftMargins = new ArrayList();
		ArrayList  topMargins  = new ArrayList();
		
		TextView      textView;
		GutterMargin  gutterMargin;
		FoldMargin    foldMargin;
		IconBarMargin iconBarMargin;
		
		
		SelectionManager selectionManager;
		Caret            caret;
		
		public TextEditorControl MotherTextEditorControl {
			get {
				return motherTextEditorControl;
			}
		}
		
		public SelectionManager SelectionManager {
			get {
				return selectionManager;
			}
		}
        
		public Caret Caret {
			get {
				return caret;
			}
		}
		
		public TextView TextView {
			get {
				return textView;
			}
		}
		
		public GutterMargin GutterMargin {
			get {
				return gutterMargin;
			}
		}
		
		public FoldMargin FoldMargin {
			get {
				return foldMargin;
			}
		}
		
		public IconBarMargin IconBarMargin {
			get {
				return iconBarMargin;
			}
		}
		
		public Encoding Encoding {
			get {
				return motherTextEditorControl.Encoding;
			}
		}
        
		public System.Drawing.Point VirtualTop {
			get {
				return virtualTop;
			}
			set {
				if (virtualTop != value) {
					virtualTop = value;
					Invalidate();
				}
			}
		}
		
		public bool AutoClearSelection {
			get {
				return autoClearSelection;
			}
			set {
				autoClearSelection = value;
			}
		}
		
		//[Browsable(false)]
		public IDocument Document {
			get {
				return motherTextEditorControl.Document;
			}
		}

		public TextAreaClipboardHandler ClipboardHandler {
			get {
				return textAreaClipboardHandler;
			}
		}
		
		public ITextEditorProperties TextEditorProperties {
			get {
				return motherTextEditorControl.TextEditorProperties;
			}
		}
		
		public TextArea(TextEditorControl motherTextEditorControl, TextAreaControl motherTextAreaControl) : base ()
		{
			this.motherTextAreaControl      = motherTextAreaControl;
			this.motherTextEditorControl    = motherTextEditorControl;
			
			caret            = new Caret(this);
			selectionManager = new SelectionManager(Document);
			
			this.textAreaClipboardHandler = new TextAreaClipboardHandler(this);

#if GTK
			// FIXME: GTKize?
			this.DoubleBuffered = false;
#else
			ResizeRedraw = true;
			
			SetStyle(ControlStyles.DoubleBuffer, false);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.Opaque, false);
#endif
			
			textView = new TextView(this);
			
			gutterMargin = new GutterMargin(this);
			foldMargin   = new FoldMargin(this);
			iconBarMargin = new IconBarMargin(this);
			leftMargins.AddRange(new AbstractMargin[] { iconBarMargin, gutterMargin, foldMargin });
			OptionsChanged();
			
			new TextAreaMouseHandler(this).Attach();
			new TextAreaDragDropHandler().Attach(this);
			
			bracketshemes.Add(new BracketHighlightingSheme('{', '}'));
			bracketshemes.Add(new BracketHighlightingSheme('(', ')'));
			bracketshemes.Add(new BracketHighlightingSheme('[', ']'));

			CanFocus = true;
			
			caret.PositionChanged += new EventHandler(SearchMatchingBracket);
			Document.TextContentChanged += new EventHandler(TextContentChanged);

#if GTK
			KeyPressEvent += new GtkSharp.KeyPressEventHandler (OnKeyPress);

			AddEvents ((int) (Gdk.EventMask.ExposureMask |
					  Gdk.EventMask.LeaveNotifyMask |
					  Gdk.EventMask.ButtonPressMask |
					  Gdk.EventMask.ButtonReleaseMask |
					  Gdk.EventMask.PointerMotionMask |
					  Gdk.EventMask.KeyPressMask |
					  Gdk.EventMask.ScrollMask |
					  Gdk.EventMask.KeyReleaseMask));

			ExposeEvent += new GtkSharp.ExposeEventHandler (ExposeHandler);
#endif
		}

		public TextArea(): base()
		{
		}

		public void UpdateMatchingBracket()
		{
			SearchMatchingBracket(null, null);
		}
		
		void TextContentChanged(object sender, EventArgs e)
		{
#if GTK
			// FIXME: GTKize
#else
			Caret.Position = new System.Drawing.Point(0, 0);
#endif
			SelectionManager.SelectionCollection.Clear();
		}
		void SearchMatchingBracket(object sender, EventArgs e)
		{
			if (!TextEditorProperties.ShowMatchingBracket) {
				textView.Highlight = null;
				return;
			}
			bool changed = false;
			if (caret.Offset == 0) {
				if (textView.Highlight != null) {
					int line  = textView.Highlight.OpenBrace.Y;
					int line2 = textView.Highlight.CloseBrace.Y;
					textView.Highlight = null;
					UpdateLine(line);
					UpdateLine(line2);
				}
				return;
			}
			foreach (BracketHighlightingSheme bracketsheme in bracketshemes) {
//				if (bracketsheme.IsInside(textareapainter.Document, textareapainter.Document.Caret.Offset)) {
					Highlight highlight = bracketsheme.GetHighlight(Document, Caret.Offset - 1);
					if (textView.Highlight != null && textView.Highlight.OpenBrace.Y >=0 && textView.Highlight.OpenBrace.Y < Document.TotalNumberOfLines) {
						UpdateLine(textView.Highlight.OpenBrace.Y);
					}
					if (textView.Highlight != null && textView.Highlight.CloseBrace.Y >=0 && textView.Highlight.CloseBrace.Y < Document.TotalNumberOfLines) {
						UpdateLine(textView.Highlight.CloseBrace.Y);
					}
					textView.Highlight = highlight;
					if (highlight != null) {
						changed = true;
						break; 
					}
//				}
			}
			if (changed || textView.Highlight != null) {
				int line = textView.Highlight.OpenBrace.Y;
				int line2 = textView.Highlight.CloseBrace.Y;
				if (!changed) {
					textView.Highlight = null;
				}
				UpdateLine(line);
				UpdateLine(line2);
			}
		}
		
		public void SetDesiredColumn()
		{
			Caret.DesiredColumn = Caret.Column;
		}
		
		public void SetCaretToDesiredColumn(int caretLine)
		{
			Caret.Position = new System.Drawing.Point(Caret.DesiredColumn, caretLine);
		}
		
		public void OptionsChanged()
		{
#if GTK
			textView.OptionsChanged();
			Invalidate();
#else
			UpdateMatchingBracket();
			textView.OptionsChanged();
			caret.RecreateCaret();	
			Refresh();
#endif
		}
		
#if GTK
		void ExposeHandler (object obj, GtkSharp.ExposeEventArgs args)
		{
			Gdk.Rectangle grect = args.Event.area;
			System.Drawing.Rectangle drect = new System.Drawing.Rectangle(grect.x, grect.y, grect.width, grect.height);
			
			if (grect.width == 0 || grect.height == 0) {
				return;
			}
			/*
			if (updateMargin != null) {
				updateMargin.Paint(e.Graphics, updateMargin.DrawingPosition);
				return;
			}
			*/
			if (this.motherTextEditorControl.IsInUpdate) {
				return;
			}
			int currentXPos = 0;
			int currentYPos = 0;
			bool adjustScrollBars = false;
			
			System.Drawing.Rectangle clipRectangle = drect;
			
			GdkWindow.BeginPaintRect(grect);
			
			/*
			if (this.TextEditorProperties.UseAntiAliasedFont) {
				GdkWindow.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			} else {
				GdkWindow.TextRenderingHint = TextRenderingHint.SystemDefault;
			}*/
			
			foreach (AbstractMargin margin in leftMargins) {
				if (margin.IsVisible) {
					System.Drawing.Rectangle marginRectangle = 
						new System.Drawing.Rectangle(currentXPos , 
									     currentYPos, 
									     margin.Size.Width, 
									     GdkWindow.Size.Height - currentYPos);
					if (marginRectangle != margin.DrawingPosition) {	
						adjustScrollBars = true;
						margin.DrawingPosition = marginRectangle;
					}
					currentXPos += margin.DrawingPosition.Width;
					if (clipRectangle.IntersectsWith(marginRectangle)) {
						marginRectangle.Intersect(clipRectangle);
						margin.Paint(GdkWindow, marginRectangle);
					}
				}
			}
			
			System.Drawing.Rectangle textViewArea = new System.Drawing.Rectangle(currentXPos, currentYPos, GdkWindow.Size.Width - 2, GdkWindow.Size.Height - 2);
			if (textViewArea != textView.DrawingPosition) {
				adjustScrollBars = true;
				textView.DrawingPosition = textViewArea;
			}
			if (clipRectangle.IntersectsWith(textViewArea)) {
				textViewArea.Intersect(clipRectangle);
				textView.Paint(GdkWindow, textViewArea);
			}
			
			if (adjustScrollBars) {
				this.motherTextAreaControl.AdjustScrollBars(null, null);
			}
			Caret.UpdateCaretPosition();

			this.GdkWindow.EndPaint ();			
		}
#else /* !GTK */
		protected void OnMouseLeave(System.EventArgs e)
		{
			this.Cursor = Cursors.Default;
		}
		
		protected override void OnMouseMove(System.Windows.Forms.MouseEventArgs e)
		{
			base.OnMouseMove(e);
			foreach (AbstractMargin margin in leftMargins) {
				if (margin.DrawingPosition.Contains(e.X, e.Y)) {
					this.Cursor = margin.Cursor;
					return;
				}
			}
			if (textView.DrawingPosition.Contains(e.X, e.Y)) {
				this.Cursor = textView.Cursor;
				return;
			}
			this.Cursor = Cursors.Default;
		}
		
		AbstractMargin updateMargin = null;
		
		public void Refresh(AbstractMargin margin)
		{
			updateMargin = margin;
			Invalidate(updateMargin.DrawingPosition);
			Update();
			updateMargin = null;
		}
		
		protected override void OnPaintBackground(System.Windows.Forms.PaintEventArgs pevent)
		{
		}
		
		protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
		{
			if (updateMargin != null) {
				updateMargin.Paint(e.Graphics, updateMargin.DrawingPosition);
				return;
			}
			if (this.motherTextEditorControl.IsInUpdate) {
				return;
			}
			int currentXPos = 0;
			int currentYPos = 0;
			bool adjustScrollBars = false;
			Graphics  g             = e.Graphics;
			Rectangle clipRectangle = e.ClipRectangle;
			
			if (this.TextEditorProperties.UseAntiAliasedFont) {
				g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			} else {
				g.TextRenderingHint = TextRenderingHint.SystemDefault;
			}
			
			foreach (AbstractMargin margin in leftMargins) {
				if (margin.IsVisible) {
					Rectangle marginRectangle = new Rectangle(currentXPos , currentYPos, margin.Size.Width, Height - currentYPos);
					if (marginRectangle != margin.DrawingPosition) {
						adjustScrollBars = true;
						margin.DrawingPosition = marginRectangle;
					}
					currentXPos += margin.DrawingPosition.Width;
					if (clipRectangle.IntersectsWith(marginRectangle)) {
						marginRectangle.Intersect(clipRectangle);
						margin.Paint(g, marginRectangle);
					}
				}
			}
			
			Rectangle textViewArea = new Rectangle(currentXPos, currentYPos, Width - currentXPos, Height - currentYPos);
			if (textViewArea != textView.DrawingPosition) {
				adjustScrollBars = true;
				textView.DrawingPosition = textViewArea;
			}
			if (clipRectangle.IntersectsWith(textViewArea)) {
				textViewArea.Intersect(clipRectangle);
				textView.Paint(g, textViewArea);
			}
			
			if (adjustScrollBars) {
				this.motherTextAreaControl.AdjustScrollBars(null, null);
			}
			Caret.UpdateCaretPosition();
		}				
#endif /* GTK */
		
		/// <summary>
		/// This method is called on each Keypress
		/// </summary>
		/// <returns>
		/// True, if the key is handled by this method and should NOT be
		/// inserted in the textarea.
		/// </returns>
		protected virtual bool HandleKeyPress(char ch)
		{
			if (KeyEventHandler != null) {            
				return KeyEventHandler(ch);
			}
			return false;
		}
		
		public void SimulateKeyPress(Gdk.Key key)
		{
			//FIXME: This code below is prolly a real bad idea
			//char ch = key.ToString ().ToCharArray ()[0];
			char ch = (char)Gdk.Keyval.ToUnicode((uint)key);

			if (Document.ReadOnly) {
				return;
			}
			
			if (ch < ' ') {
				return;
			}
			
			if (!HiddenMouseCursor && TextEditorProperties.HideMouseCursor) {
				HiddenMouseCursor = true;
#if GTK
				// FIXME: GTKize
#else
				Cursor.Hide();
#endif
			}
			
			motherTextEditorControl.BeginUpdate();
			switch (ch) {
				default: // INSERT char
					if (!HandleKeyPress(ch)) {
						switch (Caret.CaretMode) {
							case CaretMode.InsertMode:
								InsertChar(ch);
								break;
							case CaretMode.OverwriteMode:
								ReplaceChar(ch);
								break;
							default:
								Debug.Assert(false, "Unknown caret mode " + Caret.CaretMode);
								break;
						}
					}
					break;
			}
			
			int currentLineNr = Caret.Line;
			int delta = Document.FormattingStrategy.FormatLine(Document, currentLineNr, Document.PositionToOffset(Caret.Position), ch);
			
			motherTextEditorControl.EndUpdate();
			if (delta != 0) {
#if GTK
				// FIXME: GTKize
#else
				this.motherTextEditorControl.UpdateLines(currentLineNr, currentLineNr);
#endif
			}
		}
		
		protected void OnKeyPress(object o, GtkSharp.KeyPressEventArgs e)
		{
#if GTK
			e.RetVal = true;
			/*if (KeyEventHandler != null) {            
				if (KeyEventHandler((char)e.Event.Key)) {
					return;
				}
			}*/

			if (ExecuteDialogKey(e.Event)) {
				return;
			}
#else
			base.OnKeyPress(e);
#endif
			SimulateKeyPress(e.Event.Key);
		}
		
		/// <summary>
		/// This method executes a dialog key
		/// </summary>
		public bool ExecuteDialogKey(Gdk.EventKey keyData)
		{
//			// try, if a dialog key processor was set to use this
//			if (ProcessDialogKeyProcessor != null && ProcessDialogKeyProcessor(keyData)) {
//				return true;
//			}
			
			// if not (or the process was 'silent', use the standard edit actions
            
			IEditAction action =  motherTextEditorControl.GetEditAction(keyData);
			AutoClearSelection = true;
			if (action != null) {
				motherTextEditorControl.BeginUpdate();
				try {
					lock (Document) {
						action.Execute(this);
						if (SelectionManager.HasSomethingSelected && AutoClearSelection /*&& caretchanged*/) {
							if (Document.TextEditorProperties.DocumentSelectionMode == DocumentSelectionMode.Normal) {
								SelectionManager.ClearSelection();
							}
						}
					}
				} catch (Exception e) {
					Console.WriteLine("Got Exception while executing action " + action + " : " + e.ToString());
				} finally {
					motherTextEditorControl.EndUpdate();
					Caret.UpdateCaretPosition();
				}
				return true;
			} 
			return false;
		}
		
#if GTK
		// FIXME: GTKize
#else
                protected override bool ProcessDialogKey(Keys keyData)
                {
                        return ExecuteDialogKey(keyData) || base.ProcessDialogKey(keyData);
                }
#endif
		
		public void ScrollToCaret()
		{
			motherTextAreaControl.ScrollToCaret();
		}
		
		public void ScrollTo(int line)
		{
			motherTextAreaControl.ScrollTo(line);
		}
		
		public void BeginUpdate()
		{
			motherTextEditorControl.BeginUpdate();
		}
		
		public void EndUpdate()
		{
			motherTextEditorControl.EndUpdate();
		}
		
		string GenerateWhitespaceString(int length)
		{
			return new String(' ', length);
		}
		/// <remarks>
		/// Inserts a single character at the caret position
		/// </remarks>
		public void InsertChar(char ch)
		{        
			bool updating = motherTextEditorControl.IsUpdating;
			if (!updating) {
				BeginUpdate();
			}
			
			// filter out forgein whitespace chars and replace them with standard space (ASCII 32)
			if (Char.IsWhiteSpace(ch) && ch != '\t' && ch != '\n') {
				ch = ' ';
			}
			bool removedText = false;
			if (Document.TextEditorProperties.DocumentSelectionMode == DocumentSelectionMode.Normal &&
			    SelectionManager.SelectionCollection.Count > 0) {
				Caret.Position = SelectionManager.SelectionCollection[0].StartPosition;
				SelectionManager.RemoveSelectedText();
				removedText = true;
			}
			LineSegment caretLine = Document.GetLineSegment(Caret.Line);
			int offset = Caret.Offset;
			if (caretLine.Length < Caret.Column && ch != '\n') {
				Document.Insert(offset, GenerateWhitespaceString(Caret.Column - caretLine.Length) + ch);
			} else {
				Document.Insert(offset, ch.ToString());
			}
			++Caret.Column;
			
			if (removedText) {
				Document.UndoStack.UndoLast(2);
			}
			
			if (!updating) {
				EndUpdate();
				UpdateLineToEnd(Caret.Line, Caret.Column);
			}
			
			// I prefer to set NOT the standard column, if you type something
//			++Caret.DesiredColumn;
		}
		
		/// <remarks>
		/// Inserts a whole string at the caret position
		/// </remarks>
		public void InsertString(string str)
		{
			bool updating = motherTextEditorControl.IsUpdating;
			if (!updating) {
				BeginUpdate();
			}
			try {
				bool removedText = false;
				if (Document.TextEditorProperties.DocumentSelectionMode == DocumentSelectionMode.Normal &&
				    SelectionManager.SelectionCollection.Count > 0) {
					Caret.Position = SelectionManager.SelectionCollection[0].StartPosition;
					SelectionManager.RemoveSelectedText();
					removedText = true;
				}
				
				int oldOffset = Document.PositionToOffset(Caret.Position);
				int oldLine   = Caret.Line;
				LineSegment caretLine = Document.GetLineSegment(Caret.Line);
				if (caretLine.Length < Caret.Column) {
					int whiteSpaceLength = Caret.Column - caretLine.Length;
					Document.Insert(oldOffset, GenerateWhitespaceString(whiteSpaceLength) + str);
					Caret.Position = Document.OffsetToPosition(oldOffset + str.Length + whiteSpaceLength);
				} else {
					Document.Insert(oldOffset, str);
					Caret.Position = Document.OffsetToPosition(oldOffset + str.Length);
				}
				if (removedText) {
					Document.UndoStack.UndoLast(2);
				}
				if (oldLine != Caret.Line) {
					UpdateToEnd(oldLine);
				} else {
					UpdateLineToEnd(Caret.Line, Caret.Column);
				}
			} finally {
				if (!updating) {
					EndUpdate();
				}
			}
		}
		
		/// <remarks>
		/// Replaces a char at the caret position
		/// </remarks>
		public void ReplaceChar(char ch)
		{
			bool updating = motherTextEditorControl.IsUpdating;
			if (!updating) {
				BeginUpdate();
			}
			if (Document.TextEditorProperties.DocumentSelectionMode == DocumentSelectionMode.Normal && SelectionManager.SelectionCollection.Count > 0) {
				Caret.Position = SelectionManager.SelectionCollection[0].StartPosition;
				SelectionManager.RemoveSelectedText();
			}
			
			int lineNr   = Caret.Line;
			LineSegment  line = Document.GetLineSegment(lineNr);
			int offset = Document.PositionToOffset(Caret.Position);
			if (offset < line.Offset + line.Length) {
				Document.Replace(offset, 1, ch.ToString());
			} else {
				Document.Insert(offset, ch.ToString());
			}
			if (!updating) {
				EndUpdate();
				UpdateLineToEnd(lineNr, Caret.Column);
			}
			++Caret.Column;
//			++Caret.DesiredColumn;
		}
		
#if GTK
		// FIXME: GTKize?
#else
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing) {
				Caret.Dispose();
			}
		}
#endif
		
		internal void UpdateLine(int line)
		{
			UpdateLines(0, line, line);
		}
		
		internal void UpdateLines(int lineBegin, int lineEnd)
		{
			UpdateLines(0, lineBegin, lineEnd);
		}
	
		internal void UpdateToEnd(int lineBegin) 
		{
			if (lineBegin > this.textView.FirstVisibleLine + textView.VisibleLineCount) {
				return;
			}
			
			lineBegin     = Math.Max(Document.GetLogicalLine(lineBegin), textView.FirstVisibleLine);
			int y         = Math.Max(    0, (int)(lineBegin  * textView.FontHeight));
			y = Math.Max(0, y - 1 - this.virtualTop.Y);
			System.Drawing.Rectangle r = new System.Drawing.Rectangle(0,
			                            y, 
			                            WidthRequest, 
			                            HeightRequest - y);
			Invalidate(r);
		}
		
		internal void UpdateLineToEnd(int lineNr, int xStart)
		{
			UpdateLines(xStart, lineNr, lineNr);
		}
		
		internal void UpdateLine(int line, int begin, int end)
		{
			UpdateLines(line, line);
		}
	
		internal void UpdateLines(int xPos, int lineBegin, int lineEnd)
		{
			if (lineEnd < this.textView.FirstVisibleLine || lineBegin > this.textView.FirstVisibleLine + textView.VisibleLineCount) {
				return;
			}
			
			InvalidateLines((int)(xPos * this.TextView.GetWidth(' ')), lineBegin, lineEnd);
		}
		
		void InvalidateLines(int xPos, int lineBegin, int lineEnd)
		{
			int firstLine = textView.FirstVisibleLine;
			
			lineBegin     = Math.Max(Document.GetLogicalLine(lineBegin), textView.FirstVisibleLine);
			lineEnd       = Math.Min(Document.GetLogicalLine(lineEnd),   textView.FirstVisibleLine + textView.VisibleLineCount);
			int y         = Math.Max(    0, (int)(lineBegin  * textView.FontHeight));
			int height    = Math.Min(textView.DrawingPosition.Height, (int)((1 + lineEnd - lineBegin) * (textView.FontHeight + 1)));
			
			System.Drawing.Rectangle r = new System.Drawing.Rectangle(0,
			                            y - 1 - this.virtualTop.Y, 
			                            WidthRequest, 
			                            height + 3);
			
			Invalidate(r);
		}

#if GTK
		public event KeyEventHandler KeyEventHandler;

		public void Invalidate() 
		{
			if (GdkWindow == null) {
				return;
			}
		
			Invalidate (new System.Drawing.Rectangle(0, 0, GdkWindow.Size.Width, GdkWindow.Size.Height));
		}
		
		internal void Invalidate (System.Drawing.Rectangle rect) 
		{
			// Uff, this are "empirical", non-optimal rules from what I saw
			// FIXME Improve this
			
			try {
				int x = Math.Max(rect.X, 0);
				int y = Math.Max(rect.Y, 0);
				int width = Math.Max(rect.Width, 0);
				if (rect.Width < 0) {
					width = GdkWindow.Size.Width;
				}
				int height = Math.Max(rect.Height, 0);
				if (rect.Height < 0) {
					height = GdkWindow.Size.Height;
				}

				//Console.WriteLine ("{0} {1} {2} {3}", x, y, width, height);
				GdkWindow.InvalidateRect(new Gdk.Rectangle(x, y, width, height), false);
			} catch {
				//This try/catch seems to fix a bug when creating files
			}
		}
		
		public void Refresh() {
			Invalidate(); // FIXME PEDRO
		}

#endif
	}	
}
