// 
// QuickTaskFullMode.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using Gtk;
using Mono.TextEditor;
using System.Collections.Generic;
using Gdk;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Components.Commands;
using ICSharpCode.NRefactory;

namespace MonoDevelop.SourceEditor.QuickTasks
{
	
	public class QuickTaskMiniMapMode : HBox
	{
		QuickTaskOverviewMode rightMap;
	/*
		Pixmap backgroundPixbuf, backgroundBuffer;
		uint redrawTimeout;
		TextDocument doc;
		protected override double IndicatorHeight  {
			get {
				return 16;
			}
		}
		*/
		public QuickTaskMiniMapMode (QuickTaskStrip parent)
		{
			rightMap = new QuickTaskOverviewMode (parent);
			PackStart (rightMap, true, true, 0);
			
/*			doc = parent.TextEditor.Document;
			doc.TextReplaced += TextReplaced;
			doc.Folded += HandleFolded;*/
		}
			/*
		void HandleFolded (object sender, FoldSegmentEventArgs e)
		{
			RequestRedraw ();
		}
		
		void TextReplaced (object sender, DocumentChangeEventArgs args)
		{
			RequestRedraw ();
		}

		public void RemoveRedrawTimer ()
		{
			if (redrawTimeout != 0) {
				GLib.Source.Remove (redrawTimeout);
				redrawTimeout = 0;
			}
		}

		void RequestRedraw ()
		{
			RemoveRedrawTimer ();
			redrawTimeout = GLib.Timeout.Add (450, delegate {
				if (curUpdate != null) {
					curUpdate.RemoveHandler ();
					curUpdate = null;
				}
				if (backgroundPixbuf != null)
					curUpdate = new BgBufferUpdate (this);
				redrawTimeout = 0;
				return false;
			});
		}
		
		protected override void DrawBar (Cairo.Context cr)
		{
			if (VAdjustment == null || VAdjustment.Upper <= VAdjustment.PageSize) 
				return;
			var h = Allocation.Height - IndicatorHeight;
			cr.Rectangle (1.5,
				              h * VAdjustment.Value / VAdjustment.Upper + cr.LineWidth + 0.5,
				              Allocation.Width - 2,
				              h * (VAdjustment.PageSize / VAdjustment.Upper));
			Cairo.Color color = (TextEditor.ColorStyle != null) ? TextEditor.ColorStyle.Default.CairoColor : new Cairo.Color (0, 0, 0);
			color.A = 0.5;
			cr.Color = color;
			cr.StrokePreserve ();
			
			color.A = 0.05;
			cr.Color = color;
			cr.Fill ();
		}
	
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Width = 164;
		}
		
		void DestroyBgBuffer ()
		{
			if (curUpdate != null)
				curUpdate.RemoveHandler ();
			if (backgroundPixbuf != null) {
				backgroundPixbuf.Dispose ();
				backgroundBuffer.Dispose ();
				backgroundPixbuf = backgroundBuffer = null;
				curWidth = curHeight = -1;
			}
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			doc.Folded -= HandleFolded;
			doc.TextReplaced -= TextReplaced;
			RemoveRedrawTimer ();
			DestroyBgBuffer ();
		}
		
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (allocation.Width > 1 && (allocation.Width != curWidth || allocation.Height != curHeight))
				CreateBgBuffer ();
		}
		
		protected override void OnMapped ()
		{
			if (backgroundPixbuf == null && Allocation.Width > 1)
				CreateBgBuffer ();
			base.OnMapped ();
		}
		
		protected override void OnUnmapped ()
		{
			DestroyBgBuffer ();
			base.OnUnmapped ();
		}
		
		BgBufferUpdate curUpdate = null;
		void SwapBuffer ()
		{
			var tmp = backgroundPixbuf;
			backgroundPixbuf = backgroundBuffer;
			backgroundBuffer = tmp;
		}
		
		int curWidth = -1, curHeight = -1;
		void CreateBgBuffer ()
		{
			DestroyBgBuffer ();
			curWidth = Allocation.Width;
			curHeight = Allocation.Height;
			backgroundPixbuf = new Pixmap (GdkWindow, curWidth, curHeight);
			backgroundBuffer = new Pixmap (GdkWindow, curWidth, curHeight);
			
			if (TextEditor.ColorStyle != null) {
				using (var cr = Gdk.CairoHelper.Create (backgroundPixbuf)) {
					cr.Rectangle (0, 0, curWidth, curHeight);
					cr.Color = TextEditor.ColorStyle.Default.CairoBackgroundColor;
					cr.Fill ();
				}
			}
			curUpdate = new BgBufferUpdate (this);
		}
		
		class BgBufferUpdate {
			int maxLine;
			double sx;
			double sy;
			uint handler;
			
			Cairo.Context cr;
			
			QuickTaskFullMode mode;
			
			int curLine = 1;
			
			public BgBufferUpdate (QuickTaskFullMode mode)
			{
				this.mode = mode;
				
				cr = Gdk.CairoHelper.Create (mode.backgroundBuffer);
				
				cr.LineWidth = 1;
				cr.Rectangle (0, 0, mode.Allocation.Width, mode.Allocation.Height);
				if (mode.TextEditor.ColorStyle != null)
					cr.Color = mode.TextEditor.ColorStyle.Default.CairoBackgroundColor;
				cr.Fill ();
				
				maxLine = mode.TextEditor.GetTextEditorData ().VisibleLineCount;
				sx = mode.Allocation.Width / (double)mode.TextEditor.Allocation.Width;
				sy = Math.Min (1, (mode.Allocation.Height - mode.IndicatorHeight) / (double)mode.TextEditor.GetTextEditorData ().TotalHeight);
				cr.Scale (sx, sy);
				
				handler = GLib.Idle.Add (BgBufferUpdater);
			}
			
			public void RemoveHandler ()
			{
				if (cr == null)
					return;
				GLib.Source.Remove (handler);
				handler = 0;
				((IDisposable)cr).Dispose ();
				cr = null;
				mode.curUpdate = null;
			}
		
			bool BgBufferUpdater ()
			{
				if (mode.TextEditor.Document == null || handler == 0)
					return false;
				try {
					for (int i = 0; i < 25 && curLine < maxLine; i++) {
						var nr = mode.TextEditor.GetTextEditorData ().VisualToLogicalLine (curLine);
						var line = mode.TextEditor.GetLine (nr);
						if (line != null) {
							var layout = mode.TextEditor.TextViewMargin.GetLayout (line);
							cr.MoveTo (0, (curLine - 1) * mode.TextEditor.LineHeight);
							cr.ShowLayout (layout.Layout);
								
							if (layout.IsUncached)
								layout.Dispose ();
						}
						
						curLine++;
					}
					
					if (curLine >= maxLine) {
						mode.SwapBuffer ();
						((IDisposable)cr).Dispose ();
						cr = null;
						mode.curUpdate = null;
						mode.QueueDraw ();
						return false;
					}
				} catch (Exception e) {
					LoggingService.LogError ("Error in background buffer drawer.", e);
					return false;
				}
				return true;
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			if (TextEditor == null)
				return true;
			using (Cairo.Context cr = Gdk.CairoHelper.Create (e.Window)) {
				cr.LineWidth = 1;
				if (backgroundPixbuf != null) {
					e.Window.DrawDrawable (Style.BlackGC, backgroundPixbuf, 0, 0, 0, 0, Allocation.Width, Allocation.Height);
				} else {
					cr.Rectangle (0, 0, Allocation.Width, Allocation.Height);
					if (TextEditor.ColorStyle != null)
						cr.Color = TextEditor.ColorStyle.Default.CairoBackgroundColor;
					cr.Fill ();
				}
				
				cr.Color = (HslColor)Style.Dark (State);
				cr.MoveTo (0.5, 0.5);
				cr.LineTo (Allocation.Width, 0.5);
				cr.Stroke ();
				
				if (TextEditor.HighlightSearchPattern) {
					DrawSearchResults (cr);
					DrawSearchIndicator (cr);
				} else {
					var severity = DrawQuickTasks (cr);
					DrawIndicator (cr, severity);
				}
				
				DrawCaret (cr);
				DrawBar (cr);
				DrawLeftBorder (cr);
			}
			
			return true;
		}*/
	}
	
}
