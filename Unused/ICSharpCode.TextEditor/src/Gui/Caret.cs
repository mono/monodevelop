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
using System.Drawing.Text;
using System.Diagnostics;
using System.Text;

using MonoDevelop.TextEditor.Document;

namespace MonoDevelop.TextEditor
{
	/// <summary>
	/// In this enumeration are all caret modes listed.
	/// </summary>
	public enum CaretMode {
		/// <summary>
		/// If the caret is in insert mode typed characters will be
		/// inserted at the caret position
		/// </summary>
		InsertMode,
		
		/// <summary>
		/// If the caret is in overwirte mode typed characters will 
		/// overwrite the character at the caret position
		/// </summary>
		OverwriteMode
	}
	
	
	public class Caret : System.IDisposable
	{
		int       line          = 0;
		int       column        = 0;
		int       desiredColumn = 0;
		Gdk.Point     physicalPosition;
		CaretMode caretMode;
		uint currentTimeout = 0;
		
		static bool     caretCreated = false;
		bool     hidden       = false;
		bool     blinkShows   = false;
		TextArea textArea;
		
		/// <value>
		/// The 'prefered' column in which the caret moves, when it is moved
		/// up/down.
		/// </value>
		public int DesiredColumn {
			get {
				return desiredColumn;
			}
			set {
				desiredColumn = value;
			}
		}
		
		/// <value>
		/// The current caret mode.
		/// </value>
		public CaretMode CaretMode {
			get {
				return caretMode;
			}
			set {
				caretMode = value;
				OnCaretModeChanged(EventArgs.Empty);
			}
		}
		
		public int Line {
			get {
				return line;
			}
			set {
				int oldLine = line;
				line = value;
				ValidateCaretPos();
				UpdateCaretPosition(oldLine, column, line, column);
				OnPositionChanged(EventArgs.Empty);
				//Console.WriteLine("set_Line: Caret position ({0}, {1})", line, column);
			}
		}
		
		public int Column {
			get {
				return column;
			}
			set {
				int oldColumn = column;
				column = value;
				ValidateCaretPos();
				UpdateCaretPosition(line, oldColumn, line, column);
				OnPositionChanged(EventArgs.Empty);
				//Console.WriteLine("set_Column: Caret position ({0}, {1})", line, column);
			}
		}
		
		public Point Position {
			get {
				return new Point(column, line);
			}
			set {
				int oldLine = line;
				int oldColumn = column;
				line = value.Y;
				column = value.X;
				ValidateCaretPos();
				UpdateCaretPosition(oldLine, oldColumn, line, column);
				OnPositionChanged(EventArgs.Empty);
				//Console.WriteLine("set_Position: Caret position ({0}, {1})", line, column);
			}
		}
		
		public Gdk.Point PhysicalPosition {
			get {
				return physicalPosition;
			}
			set {
				physicalPosition = value;
			}
		}
		
		public void Paint(Gdk.Drawable g, Gdk.GC gc) {
			if (hidden || !blinkShows) {
				return;
			}
			int x = physicalPosition.x;
			int y = physicalPosition.y;
			int xp = physicalPosition.x + (int)textArea.TextView.GetWidth(' ') - 1;
			int yp = physicalPosition.y + textArea.TextView.FontHeight - 1;
			
			if (caretMode == CaretMode.OverwriteMode) {
				g.DrawLine(gc, x, y, x, yp);
				g.DrawLine(gc, xp, y, xp, yp);
				g.DrawLine(gc, x, y, xp, y);
				g.DrawLine(gc, x, yp, xp, yp);
			} else {
				g.DrawLine(gc, x, y, x, yp);
				g.DrawLine(gc, x + 1, y, x + 1, yp);
			}
		}

		public int Offset {
			get {
				return textArea.Document.PositionToOffset(Position);
			}
		}
		
		public Caret(TextArea textArea)
		{
			this.textArea = textArea;
			textArea.FocusInEvent += new GtkSharp.FocusInEventHandler(GotFocus);
			textArea.FocusOutEvent += new GtkSharp.FocusOutEventHandler(LostFocus);
			//textArea.GotFocus  += new EventHandler(GotFocus);
			//textArea.LostFocus += new EventHandler(LostFocus);
		}
		
		/// <remarks>
		/// If the caret position is outside the document text bounds
		/// it is set to the correct position by calling ValidateCaretPos.
		/// </remarks>
		public void ValidateCaretPos()
		{
			line = Math.Max(0, Math.Min(textArea.Document.TotalNumberOfLines - 1, line));
			column = Math.Max(0, column);
			
			if (!textArea.TextEditorProperties.AllowCaretBeyondEOL) {
				LineSegment lineSegment = textArea.Document.GetLineSegment(line);
				column = Math.Min(column, lineSegment.Length);
			}
		}
		
#if GTK
		void GotFocus(object sender, GtkSharp.FocusInEventArgs e)
#else
		void GotFocus(object sender, EventArgs e)
#endif
		{
			StartBlinking();
			hidden = false;
			if (!textArea.MotherTextEditorControl.IsUpdating) {
				//CreateCaret();
			}
		}

#if GTK
		void LostFocus(object sender, GtkSharp.FocusOutEventArgs e)
#else
		void LostFocus(object sender, EventArgs e)
#endif
		{
			EndBlinking();
			hidden       = true;
			//DisposeCaret();
		}
		
		bool DoBlink() {
			textArea.Invalidate(new System.Drawing.Rectangle(physicalPosition.x, physicalPosition.y, (int)textArea.TextView.GetWidth('w'), (int)textArea.TextView.FontHeight));
			blinkShows = !blinkShows;
			return true;
		}
		
		void StartBlinking() {
			if (currentTimeout != 0) {
				return;
			}
			currentTimeout = Gtk.Timeout.Add(500, new Gtk.Function(DoBlink));
		}
		
		void EndBlinking() {
			if (currentTimeout == 0) {
				return;
			}
			Gtk.Timeout.Remove(currentTimeout);
			currentTimeout = 0;
		}
		
		public void UpdateCaretPosition() {
			// Nothin (remove)
		}
		
		public void UpdateCaretPosition(int oldLine, int oldColumn, int newLine, int newColumn)
		{
			if (hidden) {
				return;
			}
			try {
				//ValidateCaretPos();
				textArea.UpdateLineToEnd(oldLine, 0);
				if (newLine != oldLine) {
					textArea.UpdateLineToEnd(newLine, 0);
				}
				
			} catch (Exception e) {
				Console.WriteLine("Got exception while update caret position : " + e);
			}
		}
		
		protected virtual void OnPositionChanged(EventArgs e)
		{
			EndBlinking();
			blinkShows = true;
			if (PositionChanged != null) {
				PositionChanged(this, e);
			}
			textArea.ScrollToCaret();
			StartBlinking();
		}
		
		protected virtual void OnCaretModeChanged(EventArgs e)
		{
			if (CaretModeChanged != null) {
				CaretModeChanged(this, e);
			}
			caretCreated = false;
		}
		
		public void Dispose()
		{
		}
		
		/// <remarks>
		/// Is called each time the caret is moved.
		/// </remarks>
		public event EventHandler PositionChanged;
		
		/// <remarks>
		/// Is called each time the CaretMode has changed.
		/// </remarks>
		public event EventHandler CaretModeChanged;
	}
}
