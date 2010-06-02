// 
// InsertionCursorEditMode.cs
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
using System.Collections.Generic;

namespace Mono.TextEditor
{
	public enum NewLineInsertion
	{
		None,
		Eol,
		BlankLine
	}
	
	public class InsertionPoint 
	{
		public DocumentLocation Location {
			get;
			set;
		}
		
		public NewLineInsertion LineBefore { get; set; }
		public NewLineInsertion LineAfter { get; set; }
		
		public InsertionPoint (DocumentLocation location, NewLineInsertion lineBefore, NewLineInsertion lineAfter)
		{
			this.Location = location;
			this.LineBefore = lineBefore;
			this.LineAfter = lineAfter;
		}
		
		public override string ToString ()
		{
			return string.Format ("[InsertionPoint: Location={0}, LineBefore={1}, LineAfter={2}]", Location, LineBefore, LineAfter);
		}
		
		public void InsertNewLine (TextEditor editor, NewLineInsertion insertion, ref int offset)
		{
			string str = null;
			switch (insertion) {
			case NewLineInsertion.Eol:
				str = editor.GetTextEditorData ().EolMarker;
				break;
			case NewLineInsertion.BlankLine:
				str = editor.GetTextEditorData ().EolMarker + editor.GetTextEditorData ().EolMarker;
				break;
			default:
				return;
			}
			
			editor.Insert (offset, str);
			offset += str.Length;
		}
		
		public void Insert (TextEditor editor, string text)
		{
			int offset = editor.Document.LocationToOffset (Location);
			editor.Document.BeginAtomicUndo ();
			InsertNewLine (editor, LineBefore, ref offset);
			LineSegment line = editor.Document.GetLineByOffset (offset);
			string indent = editor.Document.GetLineIndent (line) ?? "";
			editor.Replace (line.Offset, indent.Length, text);
			offset = line.Offset + text.Length;
			InsertNewLine (editor, LineAfter, ref offset);
			if (!string.IsNullOrEmpty (indent))
				editor.Insert (offset, indent);
			editor.Document.EndAtomicUndo ();
		}
	}
	
	public class InsertionCursorEditMode : SimpleEditMode
	{
		TextEditor editor;
		List<InsertionPoint> insertionPoints;
		CursorDrawer drawer;
		
		public int CurIndex {
			get;
			set;
		}
		
		public DocumentLocation CurrentInsertionPoint {
			get {
				return insertionPoints[CurIndex].Location;
			}
		}
		
		public List<InsertionPoint> InsertionPoints {
			get { return this.insertionPoints; }
		}
		
		public InsertionCursorEditMode (TextEditor editor, List<InsertionPoint> insertionPoints)
		{
			this.editor = editor;
			this.insertionPoints = insertionPoints;
			drawer = new CursorDrawer (this);
		}
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			switch (key) {
			case Gdk.Key.Up:
				if (CurIndex > 0)
					CurIndex--;
				DocumentLocation loc = insertionPoints[CurIndex].Location;
				editor.CenterTo (loc.Line - 1, 0);
				editor.QueueDraw ();
				break;
			case Gdk.Key.Down:
				if (CurIndex < insertionPoints.Count - 1)
					CurIndex++;
				loc = insertionPoints[CurIndex].Location;
				editor.CenterTo (loc.Line + 1, 0);
				editor.QueueDraw ();
				break;
				
			case Gdk.Key.KP_Enter:
			case Gdk.Key.Return:
				OnExited (new InsertionCursorEventArgs (true, insertionPoints[CurIndex]));
				break;
				
			case Gdk.Key.Escape:
				OnExited (new InsertionCursorEventArgs (false, null));
				break;
			}
		}
		
		EditMode oldMode;
		public void StartMode ()
		{
			oldMode = editor.CurrentMode;
			
			
			editor.Caret.IsVisible = false;
			editor.TextViewMargin.AddDrawer (drawer);
			editor.CurrentMode = this;
			
			editor.ScrollTo (insertionPoints[CurIndex].Location);
			editor.QueueDraw ();
		}
		
		protected virtual void OnExited (InsertionCursorEventArgs e)
		{
			editor.Caret.IsVisible = true;
			editor.TextViewMargin.RemoveDrawer (drawer);
			editor.CurrentMode = oldMode;
			
			EventHandler<InsertionCursorEventArgs> handler = this.Exited;
			if (handler != null)
				handler (this, e);
			
			editor.Document.CommitUpdateAll ();
		}
		
		public event EventHandler<InsertionCursorEventArgs> Exited;

		class CursorDrawer : MarginDrawer
		{
			InsertionCursorEditMode mode;
			
			public CursorDrawer (InsertionCursorEditMode mode)
			{
				this.mode = mode;
			}
			
			void DrawArrow (Cairo.Context g, int x, int y)
			{
				TextEditor editor = mode.editor;
				double phi = 1.618;
				double arrowLength = editor.LineHeight * phi;
				double arrowHeight = editor.LineHeight / phi;
				
				g.MoveTo (x - arrowLength, y - arrowHeight);
				g.LineTo (x, y);
				g.LineTo (x - arrowLength, y + arrowHeight);
				
				g.LineTo (x - arrowLength / phi, y);
				g.ClosePath ();
				g.Color = new Cairo.Color (1.0, 0, 0);
				g.StrokePreserve ();
				
				g.Color = new Cairo.Color (1.0, 0, 0, 0.1);
				g.Fill ();
			}
			
			public override void Draw (Gdk.Drawable drawable, Gdk.Rectangle area)
			{
				TextEditor editor = mode.editor;
				int y = editor.LineToVisualY (mode.CurrentInsertionPoint.Line) - (int)editor.VAdjustment.Value; 
				using (var g = Gdk.CairoHelper.Create (drawable)) {
					g.LineWidth = System.Math.Min (1, editor.Options.Zoom);
					LineSegment lineAbove = editor.Document.GetLine (mode.CurrentInsertionPoint.Line - 1);
					LineSegment lineBelow = editor.Document.GetLine (mode.CurrentInsertionPoint.Line);
					
					int aboveStart = 0, aboveEnd = editor.TextViewMargin.XOffset;
					int belowStart = 0, belowEnd = editor.TextViewMargin.XOffset;
					int l = 0;
					if (lineAbove != null) {
						var wrapper = editor.TextViewMargin.GetLayout (lineAbove);
						wrapper.Layout.IndexToLineX (lineAbove.GetIndentation (editor.Document).Length, true, out l, out aboveStart);
						aboveStart = (int)(aboveStart / Pango.Scale.PangoScale);
						aboveEnd = (int)(wrapper.PangoWidth / Pango.Scale.PangoScale);
						
						if (wrapper.IsUncached)
							wrapper.Dispose ();
					}
					if (lineBelow != null) {
						var wrapper = editor.TextViewMargin.GetLayout (lineBelow);
						int index = lineAbove.GetIndentation (editor.Document).Length;
						if (index == 0) {
							belowStart = 0;
						} else if (index >= lineBelow.EditableLength) {
							belowStart = wrapper.PangoWidth;
						} else {
							wrapper.Layout.IndexToLineX (index, true, out l, out belowStart);
						}
						
						belowStart = (int)(belowStart / Pango.Scale.PangoScale);
						belowEnd = (int)(wrapper.PangoWidth / Pango.Scale.PangoScale);
						if (wrapper.IsUncached)
							wrapper.Dispose ();
					}
					
					int d = editor.LineHeight / 3;
					int x1 = editor.TextViewMargin.XOffset - (int)editor.HAdjustment.Value;
					int x2 = x1;
					if (aboveStart < belowEnd) {
						x1 += aboveStart;
						x2 += belowEnd;
					} else if (aboveStart > belowEnd) {
						d *= -1;
						x1 += belowEnd;
						x2 += aboveStart;
					} else {
						x1 += System.Math.Min (aboveStart, belowStart);
						x2 += System.Math.Max (aboveEnd, belowEnd);
						if (x1 == x2)
							x2 += 50;
					}
					
					g.MoveTo (x1, y + d);
					g.LineTo (x1, y);
					g.LineTo (x2, y);
					g.LineTo (x2, y - d);
					
					g.Color = new Cairo.Color (1.0, 0, 0);
					g.Stroke ();
					
					DrawArrow (g, x1 - 4, y);
				}
			}
		}
	}
	
	[Serializable]
	public sealed class InsertionCursorEventArgs : EventArgs
	{
		public bool Success {
			get;
			private set;
		}
		
		public InsertionPoint InsertionPoint {
			get;
			private set;
		}
		
		public InsertionCursorEventArgs (bool success, InsertionPoint insertionPoint)
		{
			this.Success = success;
			this.InsertionPoint = insertionPoint;
		}
	}
	
}

