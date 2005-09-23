// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Threading;
using MonoDevelop.TextEditor.Document;

namespace MonoDevelop.TextEditor
{
	/// <summary>
	/// This class handles all mouse stuff for a textArea.
	/// </summary>
	public class TextAreaMouseHandler
	{
		
		TextArea  textArea;
		bool      doubleclick = false;
		Point     mousepos = new Point(0, 0);
		int       selbegin;
		int       selend;
		bool      clickedOnSelectedText = false;

		uint button;
		
		Point mousedownpos = new Point(-1, -1);
		bool gotmousedown = false;
		bool dodragdrop = false;
		
		public TextAreaMouseHandler(TextArea textArea)
		{
			this.textArea = textArea;
		}
		
		public void Attach()
		{
#if GTK
			textArea.ButtonReleaseEvent += new GtkSharp.ButtonReleaseEventHandler(OnButtonRelease);
			textArea.ButtonPressEvent += new GtkSharp.ButtonPressEventHandler(OnButtonPress);
			textArea.MotionNotifyEvent += new GtkSharp.MotionNotifyEventHandler(OnMotionNotify);
#else
			textArea.Click       += new EventHandler(TextAreaClick);
			textArea.MouseMove   += new MouseEventHandler(TextAreaMouseMove);
			
			textArea.MouseDown   += new MouseEventHandler(OnMouseDown);
			textArea.DoubleClick += new EventHandler(OnDoubleClick);
			textArea.MouseLeave  += new EventHandler(OnMouseLeave);
			textArea.MouseUp     += new MouseEventHandler(OnMouseUp);
			textArea.LostFocus   += new EventHandler(TextAreaLostFocus);
#endif
		}
		
		void ShowHiddenCursor()
		{
			if (TextArea.HiddenMouseCursor) {
#if GTK
				// FIXME: GTKize
#else
				Cursor.Show();
#endif
				TextArea.HiddenMouseCursor = false;
			}
		}
		
		void TextAreaLostFocus(object sender, EventArgs e)
		{
			ShowHiddenCursor();
		}
		void OnMouseLeave(object sender, EventArgs e)
		{
			ShowHiddenCursor();
			gotmousedown = false;
			mousedownpos = new Point(-1, -1);
		}

#if GTK
		private void OnButtonRelease (object obj, GtkSharp.ButtonReleaseEventArgs args) 
		{
			if (gotmousedown) { // It should
				TextAreaClick();
			}
			gotmousedown = false;
			mousedownpos = new Point(-1, -1);
		}
#else
		void OnMouseUp(object sender, MouseEventArgs e)
		{
			gotmousedown = false;
			mousedownpos = new Point(-1, -1);
		}
#endif
		
		void TextAreaClick()
		{
			if (dodragdrop) {
				return;
			}
			
			if (textArea.FoldMargin.DrawingPosition.Contains(mousepos.X, mousepos.Y)) {
				textArea.FoldMargin.OnClick(mousepos);
			}
			if (clickedOnSelectedText && textArea.TextView.DrawingPosition.Contains(mousepos.X, mousepos.Y)) {
				textArea.SelectionManager.ClearSelection();
				
				Point clickPosition = textArea.TextView.GetLogicalPosition(mousepos.X - textArea.TextView.DrawingPosition.X, mousepos.Y - textArea.TextView.DrawingPosition.Y);
				textArea.Caret.Position = clickPosition;
				textArea.SetDesiredColumn();
			}
		}

#if GTK	
		/*
		private void OnMotionNotifyEvent(object o, MotionNotifyEventArgs args) {
			DateTime start = DateTime.Now;
			//if (args.Event.time < last_draw) {
			//    return;
			//}
		}
		*/
#endif

#if GTK		
		void OnMotionNotify(object sender, GtkSharp.MotionNotifyEventArgs args)
#else
		void TextAreaMouseMove(object sender, MouseEventArgs e)
#endif
		{
#if GTK		
			if (gotmousedown == false) {
				return;
			}
#endif
			ShowHiddenCursor();
			if (dodragdrop) {
				dodragdrop = false;
				return;
			}
			
			doubleclick = false;
			mousepos    = new Point((int)args.Event.x, (int)args.Event.y);
			
			if (textArea.GutterMargin.DrawingPosition.Contains(mousepos.X, mousepos.Y) || textArea.FoldMargin.DrawingPosition.Contains(mousepos.X, mousepos.Y)) {
				if (button == 1) {
					Point realmousepos = textArea.TextView.GetLogicalPosition(0, mousepos.Y /*- textArea.TextView.DrawingPosition.Y*/);
					if (realmousepos.Y < textArea.Document.TotalNumberOfLines) {
						if (selectionStartPos.Y == realmousepos.Y) {
							textArea.SelectionManager.SetSelection(new DefaultSelection(textArea.Document, realmousepos, new Point(textArea.Document.GetLineSegment(realmousepos.Y).Length + 1, realmousepos.Y)));
						} else  if (selectionStartPos.Y < realmousepos.Y && textArea.SelectionManager.HasSomethingSelected) {
							textArea.SelectionManager.ExtendSelection(textArea.SelectionManager.SelectionCollection[0].EndPosition, realmousepos);
						} else {
							textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, realmousepos);
						}
						textArea.Caret.Position = realmousepos;
					}
				}
			} else if (textArea.TextView.DrawingPosition.Contains(mousepos.X, mousepos.Y)) {
				if (clickedOnSelectedText) {
#if GTK
					// FIXME: GTKize?
					if (Math.Abs(mousedownpos.X - mousepos.X) >= 5 ||
					    Math.Abs(mousedownpos.Y - mousepos.Y) >= 5) {
#else
					if (Math.Abs(mousedownpos.X - mousepos.X) >= SystemInformation.DragSize.Width / 2 ||
					    Math.Abs(mousedownpos.Y - mousepos.Y) >= SystemInformation.DragSize.Height / 2) {
#endif						
						clickedOnSelectedText = false;
						ISelection selection = textArea.SelectionManager.GetSelectionAt(textArea.Caret.Offset);
						if (selection != null) {
							string text = selection.SelectedText;
							if (text != null && text.Length > 0) {
#if GTK
								// FIXME: GTKize
#else
								DataObject dataObject = new DataObject ();
								dataObject.SetData(DataFormats.UnicodeText, true, text);
								dataObject.SetData(selection);
								dodragdrop = true;
								textArea.DoDragDrop(dataObject, DragDropEffects.All);
#endif
							}
						}
					}
					
					return;
				}

				if (button == 1) {
					if  (gotmousedown) {
						ExtendSelectionToMouse();
					}
				}
			}
		}

		void ExtendSelectionToMouse()
		{
			Point realmousepos = textArea.TextView.GetLogicalPosition(mousepos.X /*- textArea.TextView.DrawingPosition.X*/,
			                                                          mousepos.Y /*- textArea.TextView.DrawingPosition.Y*/);
			Point oldPos = textArea.Caret.Position;
			textArea.Caret.Position = realmousepos;
			textArea.SelectionManager.ExtendSelection(oldPos, textArea.Caret.Position);
			textArea.SetDesiredColumn();		
		}

		Point selectionStartPos  = new Point(-1, -1);

#if GTK
		private void OnButtonPress (object obj, GtkSharp.ButtonPressEventArgs args)
#else
		void OnMouseDown(object sender, MouseEventArgs e)
#endif
		{ 
			if (args.Event.type == Gdk.EventType.TwoButtonPress) {
				OnDoubleClick();
				return;
			}
			
			if (dodragdrop) {
				return;
			}
			
			if (doubleclick) {
				doubleclick = false;
				return;
			}
			
			gotmousedown = true;
			mousedownpos = new Point((int)args.Event.x, (int)args.Event.y);

			mousepos = mousedownpos;

			button = args.Event.button;
			
			if (textArea.GutterMargin.DrawingPosition.Contains(mousepos.X, mousepos.Y) || textArea.FoldMargin.DrawingPosition.Contains(mousepos.X, mousepos.Y)) {
				Point realmousepos = textArea.TextView.GetLogicalPosition(0, mousepos.Y - textArea.TextView.DrawingPosition.Y);
				if (realmousepos.Y < textArea.Document.TotalNumberOfLines) {
					selectionStartPos = realmousepos;
					textArea.SelectionManager.ClearSelection();
					textArea.SelectionManager.SetSelection(new DefaultSelection(textArea.Document, realmousepos, new Point(textArea.Document.GetLineSegment(realmousepos.Y).Length + 1, realmousepos.Y)));
					textArea.Caret.Position = realmousepos;
				}
			} else if (textArea.TextView.DrawingPosition.Contains(mousepos.X, mousepos.Y)) {
				if (button == 1) {
#if GTK
					//FIXME: GTKize
					if (true) {
#else
					if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) {
						ExtendSelectionToMouse();
					} else { 
#endif

#if GTK
						// FIZME: GTKize?
						Point realmousepos = textArea.TextView.GetLogicalPosition(mousepos.X, mousepos.Y);
#else
						Point realmousepos = textArea.TextView.GetLogicalPosition(mousepos.X - textArea.TextView.DrawingPosition.X, mousepos.Y - textArea.TextView.DrawingPosition.Y);
#endif
						clickedOnSelectedText = false;
						
						int offset = textArea.Document.PositionToOffset(realmousepos);
								
						
						if (textArea.SelectionManager.HasSomethingSelected && 
						    textArea.SelectionManager.IsSelected(offset)) {	
							clickedOnSelectedText = true;
						} else {
							selbegin = selend = offset;
							textArea.SelectionManager.ClearSelection();
							if (mousepos.Y > 0 && mousepos.Y < textArea.TextView.DrawingPosition.Height) {
								Point pos = new Point();
								pos.Y = Math.Min(textArea.Document.TotalNumberOfLines - 1,  realmousepos.Y);
								pos.X = realmousepos.X;
								textArea.Caret.Position = pos;//Math.Max(0, Math.Min(textArea.Document.TextLength, line.Offset + Math.Min(line.Length, pos.X)));
								textArea.SetDesiredColumn();
							}
						}
					}
				}
			}
#if GTK
			textArea.GrabFocus();
#else
			textArea.Focus();
#endif
		}
		
		int FindNext(IDocument document, int offset, char ch)
		{
			LineSegment line = document.GetLineSegmentForOffset(offset);
			int         endPos = line.Offset + line.Length;
			
			while (offset < endPos && document.GetCharAt(offset) != ch) {
				++offset;
			}
			return offset;
		}
		
		bool IsSelectableChar(char ch)
		{
			return Char.IsLetterOrDigit(ch) || ch=='_';
		}
		
		int FindWordStart(IDocument document, int offset)
		{
			LineSegment line = document.GetLineSegmentForOffset(offset);
			
			if (offset > 0 && Char.IsWhiteSpace(document.GetCharAt(offset - 1)) && Char.IsWhiteSpace(document.GetCharAt(offset))) {
				while (offset > line.Offset && Char.IsWhiteSpace(document.GetCharAt(offset - 1))) {
					--offset;
				}
			} else  if (IsSelectableChar(document.GetCharAt(offset)) || (offset > 0 && Char.IsWhiteSpace(document.GetCharAt(offset)) && IsSelectableChar(document.GetCharAt(offset - 1))))  {
				while (offset > line.Offset && IsSelectableChar(document.GetCharAt(offset - 1))) {
					--offset;
				}
			} else {
				if (offset > 0 && !Char.IsWhiteSpace(document.GetCharAt(offset - 1)) && !IsSelectableChar(document.GetCharAt(offset - 1)) ) {
					return Math.Max(0, offset - 1);
				}
			}
			return offset;
		}
		
		int FindWordEnd(IDocument document, int offset)
		{
			LineSegment line   = document.GetLineSegmentForOffset(offset);
			int         endPos = line.Offset + line.Length;
			
			if (IsSelectableChar(document.GetCharAt(offset)))  {
				while (offset < endPos && IsSelectableChar(document.GetCharAt(offset))) {
					++offset;
				}
			} else if (Char.IsWhiteSpace(document.GetCharAt(offset))) {
				if (offset > 0 && Char.IsWhiteSpace(document.GetCharAt(offset - 1))) {
					while (offset < endPos && Char.IsWhiteSpace(document.GetCharAt(offset))) {
						++offset;
					}
				}
			} else {
				return Math.Max(0, offset + 1);
			}
			
			return offset;
		}
		
		void OnDoubleClick()
		{
			if (dodragdrop) {
				return;
			}
			
			doubleclick = true;
			
			textArea.SelectionManager.ClearSelection();
			if (textArea.TextView.DrawingPosition.Contains(mousepos.X, mousepos.Y)) {
				if (textArea.Caret.Offset < textArea.Document.TextLength) {
					switch (textArea.Document.GetCharAt(textArea.Caret.Offset)) {
						case '"':
							if (textArea.Caret.Offset < textArea.Document.TextLength) {
								int next = FindNext(textArea.Document, textArea.Caret.Offset + 1, '"');
								textArea.SelectionManager.ExtendSelection(textArea.Caret.Position,
								                                          textArea.Document.OffsetToPosition(next > textArea.Caret.Offset ? next + 1 : next));
							}
							break;
						default:
							textArea.SelectionManager.ExtendSelection(textArea.Document.OffsetToPosition(FindWordStart(textArea.Document, textArea.Caret.Offset)),
							                                          textArea.Document.OffsetToPosition(FindWordEnd(textArea.Document, textArea.Caret.Offset)));
							break;
					
					}
					// HACK WARNING !!! 
					// must refresh here, because when a error tooltip is showed and the underlined
					// code is double clicked the textArea don't update corrctly, updateline doesn't
					// work ... but the refresh does.
					// Mike
#if GTK
					// FIXME: GTKize
#else
					textArea.Refresh(); 
#endif
				}
			}
		}
	}
}
