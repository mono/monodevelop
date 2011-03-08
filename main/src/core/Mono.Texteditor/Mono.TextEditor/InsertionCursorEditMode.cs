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
using Mono.TextEditor.PopupWindow;

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
		
		public void InsertNewLine (TextEditorData editor, NewLineInsertion insertion, ref int offset)
		{
			string str = null;
			switch (insertion) {
			case NewLineInsertion.Eol:
				str = editor.EolMarker;
				break;
			case NewLineInsertion.BlankLine:
				str = editor.EolMarker + editor.EolMarker;
				break;
			default:
				return;
			}
			
			offset += editor.Insert (offset, str);
		}
		
		public void Insert (TextEditorData editor, string text)
		{
			int offset = editor.Document.LocationToOffset (Location);
			editor.Document.BeginAtomicUndo ();
			text = editor.FormatString (Location, text);
			
			LineSegment line = editor.Document.GetLineByOffset (offset);
			
			int insertionOffset = line.Offset;
			offset = insertionOffset;
			InsertNewLine (editor, LineBefore, ref offset);
			
			offset += editor.Insert (offset, text);
			InsertNewLine (editor, LineAfter, ref offset);
			editor.Document.EndAtomicUndo ();
		}
	}
	
	public class HelpWindowEditMode : SimpleEditMode
	{
		protected new TextEditor editor;
		
		public new TextEditor Editor {
			get {
				return this.editor;
			}
			set {
				this.editor = value;
			}
		}
		
		public ModeHelpWindow HelpWindow {
			get;
			set;
		}
		
		protected void ShowHelpWindow ()
		{
			if (HelpWindow == null) 
				return;
			MoveHelpWindow (null, null);
			editor.SizeAllocated += MoveHelpWindow;
			HelpWindow.Show ();
		}
		
		public void DestroyHelpWindow ()
		{
			if (HelpWindow == null) 
				return;
			editor.SizeAllocated -= MoveHelpWindow;
			editor.Destroyed -= HandleEditorDestroy;
			HelpWindow.Destroy ();
			HelpWindow = null;
		}
		
		void HandleEditorDestroy (object sender, EventArgs e)
		{
			DestroyHelpWindow ();
		}

		
		public void PositionHelpWindow ()
		{
			if (editor == null || HelpWindow == null)
				return;
			int ox, oy;
			editor.GdkWindow.GetOrigin (out ox, out oy);
			editor.Destroyed += HandleEditorDestroy;
			Gdk.Rectangle geometry = editor.Screen.GetMonitorGeometry (editor.Screen.GetMonitorAtPoint (ox, oy));
			var req = HelpWindow.SizeRequest ();
			int x = System.Math.Min (ox + editor.Allocation.Width - req.Width / 2, geometry.X + geometry.Width - req.Width);
			int y = System.Math.Min (oy + editor.Allocation.Height - req.Height / 2, geometry.Y + geometry.Height - req.Height);
			HelpWindow.Move (x, y);
		}
		
		public void PositionHelpWindow (int x, int y)
		{
			if (editor == null || HelpWindow == null)
				return;
			int ox, oy;
			editor.GdkWindow.GetOrigin (out ox, out oy);
			editor.Destroyed += HandleEditorDestroy;
			Gdk.Rectangle geometry = editor.Screen.GetMonitorGeometry (editor.Screen.GetMonitorAtPoint (ox, oy));
			var req = HelpWindow.SizeRequest ();
			x = System.Math.Min (x, geometry.X + geometry.Width - req.Width);
			HelpWindow.Move (ox + x, oy + y - req.Height / 2);
		}
		
		void MoveHelpWindow (object o, Gtk.SizeAllocatedArgs args)
		{
			PositionHelpWindow ();
		}
	}
	
	public class InsertionCursorEditMode : HelpWindowEditMode
	{
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
				editor.CenterTo (loc.Line - 1, DocumentLocation.MinColumn);
				editor.QueueDraw ();
				SetHelpWindowPosition ();
				break;
			case Gdk.Key.Down:
				if (CurIndex < insertionPoints.Count - 1)
					CurIndex++;
				loc = insertionPoints[CurIndex].Location;
				editor.CenterTo (loc.Line + 1, DocumentLocation.MinColumn);
				editor.QueueDraw ();
				SetHelpWindowPosition ();
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
			if (insertionPoints.Count == 0)
				return;
			oldMode = editor.CurrentMode;
			
			
			editor.Caret.IsVisible = false;
			editor.TextViewMargin.AddDrawer (drawer);
			editor.CurrentMode = this;
			
			editor.ScrollTo (insertionPoints[CurIndex].Location);
			editor.QueueDraw ();
			
			ShowHelpWindow ();
			SetHelpWindowPosition ();
		}
		
		void SetHelpWindowPosition ()
		{
			int y = (int)(editor.LineToY (insertionPoints[CurIndex].Location.Line) - editor.VAdjustment.Value);
			double x1, x2, d;
			drawer.CalculateLineStarts (out x1, out x2, out d);
			
			PositionHelpWindow ((int)x2 + 8, y);
		}
		
		protected virtual void OnExited (InsertionCursorEventArgs e)
		{
			DestroyHelpWindow ();
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
			
			void DrawArrow (Cairo.Context g, double x, double y)
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

			
			public void CalculateLineStarts (out double x1, out double x2, out double delta)
			{
				TextEditor editor = mode.editor;
				
				LineSegment lineAbove = editor.Document.GetLine (mode.CurrentInsertionPoint.Line - 1);
				LineSegment lineBelow = editor.Document.GetLine (mode.CurrentInsertionPoint.Line);
				
				double aboveStart = 0, aboveEnd = editor.TextViewMargin.XOffset;
				double belowStart = 0, belowEnd = editor.TextViewMargin.XOffset;
				int l = 0, tmp;
				if (lineAbove != null) {
					var wrapper = editor.TextViewMargin.GetLayout (lineAbove);
					wrapper.Layout.IndexToLineX (lineAbove.GetIndentation (editor.Document).Length, true, out l, out tmp);
					aboveStart = tmp / Pango.Scale.PangoScale;
					aboveEnd = wrapper.PangoWidth / Pango.Scale.PangoScale;
					
					if (wrapper.IsUncached)
						wrapper.Dispose ();
				}
				if (lineBelow != null) {
					var wrapper = editor.TextViewMargin.GetLayout (lineBelow);
					int index = lineAbove != null ? lineAbove.GetIndentation (editor.Document).Length : 0;
					if (index == 0) {
						tmp = 0;
					} else if (index >= lineBelow.EditableLength) {
						tmp = wrapper.PangoWidth;
					} else {
						wrapper.Layout.IndexToLineX (index, true, out l, out tmp);
					}
					
					belowStart = tmp / Pango.Scale.PangoScale;
					belowEnd = wrapper.PangoWidth / Pango.Scale.PangoScale;
					if (wrapper.IsUncached)
						wrapper.Dispose ();
				}
					
				delta = editor.LineHeight / 3;
				x1 = editor.TextViewMargin.XOffset - editor.HAdjustment.Value;
				x2 = x1;
				if (aboveStart < belowEnd) {
					x1 += aboveStart;
					x2 += belowEnd;
				} else if (aboveStart > belowEnd) {
					delta *= -1;
					x1 += belowEnd;
					x2 += aboveStart;
				} else {
					x1 += System.Math.Min (aboveStart, belowStart);
					x2 += System.Math.Max (aboveEnd, belowEnd);
					if (x1 == x2)
						x2 += 50;
				}
			}
			
			public override void Draw (Cairo.Context cr, Cairo.Rectangle erea)
			{
				TextEditor editor = mode.editor;
				
				double y = editor.LineToY (mode.CurrentInsertionPoint.Line) - editor.VAdjustment.Value; 
				double x1, x2, d;
				CalculateLineStarts (out x1, out x2, out d);
					
				cr.MoveTo (x1, y + d);
				cr.LineTo (x1, y);
				cr.LineTo (x2, y);
				cr.LineTo (x2, y - d);
				
				cr.Color = new Cairo.Color (1.0, 0, 0);
				cr.Stroke ();
				
				DrawArrow (cr, x1 - 4, y);
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

