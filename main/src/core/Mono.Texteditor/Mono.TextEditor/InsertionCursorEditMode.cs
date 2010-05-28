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
	public class InsertionCursorEditMode : SimpleEditMode
	{
		TextEditor editor;
		List<DocumentLocation> insertionPoints;
		CursorDrawer drawer;
		int curPoint = 0;
		public DocumentLocation CurrentInsertionPoint {
			get {
				return insertionPoints[curPoint];
			}
		}
			
		public InsertionCursorEditMode (TextEditor editor, List<DocumentLocation> insertionPoints)
		{
			this.editor = editor;
			this.insertionPoints = insertionPoints;
			drawer = new CursorDrawer (this);
		}
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			switch (key) {
			case Gdk.Key.Up:
				if (curPoint > 0)
					curPoint--;
				editor.QueueDraw ();
				break;
			case Gdk.Key.Down:
				if (curPoint < insertionPoints.Count - 1)
					curPoint++;
				editor.QueueDraw ();
				break;
				
			case Gdk.Key.KP_Enter:
			case Gdk.Key.Return:
				OnExited (new InsertionCursorEventArgs (true, editor.Caret.Location));
				break;
				
			case Gdk.Key.Escape:
				OnExited (new InsertionCursorEventArgs (false, DocumentLocation.Empty));
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
				int y = editor.LineToVisualY (mode.CurrentInsertionPoint.Line);
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
						wrapper.Layout.IndexToLineX (lineAbove.GetIndentation (editor.Document).Length, true, out l, out belowStart);
						belowStart = (int)(belowStart / Pango.Scale.PangoScale);
						belowEnd = (int)(wrapper.PangoWidth / Pango.Scale.PangoScale);
						if (wrapper.IsUncached)
							wrapper.Dispose ();
					}
					
					int d = editor.LineHeight / 3;
					int x1 = editor.TextViewMargin.XOffset;
					int x2 = editor.TextViewMargin.XOffset;
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
		
		public DocumentLocation InsertionPoint {
			get;
			private set;
		}
		
		public InsertionCursorEventArgs (bool success, DocumentLocation insertionPoint)
		{
			this.Success = success;
			this.InsertionPoint = insertionPoint;
		}
	}
	
}

