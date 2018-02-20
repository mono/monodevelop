// 
// MessageBubbleCache.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Mono.TextEditor;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Fonts;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Components;
using Cairo;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.SourceEditor
{
	class MessageBubbleCache : IDisposable
	{
		internal static Xwt.Drawing.Image errorPixbuf = Xwt.Drawing.Image.FromResource ("gutter-error-15.png");
		internal static Xwt.Drawing.Image warningPixbuf = Xwt.Drawing.Image.FromResource ("gutter-warning-15.png");
		
		internal Dictionary<string, LayoutDescriptor> textWidthDictionary = new Dictionary<string, LayoutDescriptor> ();
		
		internal MonoTextEditor editor;

		internal Pango.FontDescription fontDescription;
		internal Pango.FontDescription tooltipFontDescription;
		internal Pango.FontDescription errorCountFontDescription;

		public MessageBubbleTextMarker CurrentSelectedTextMarker;

		public MessageBubbleCache (MonoTextEditor editor)
		{
			this.editor = editor;
			
			editor.EditorOptionsChanged += HandleEditorEditorOptionsChanged;
			editor.TextArea.LeaveNotifyEvent += HandleLeaveNotifyEvent;
			editor.TextArea.MotionNotifyEvent += HandleMotionNotifyEvent;
			editor.TextArea.BeginHover += HandleBeginHover;
			editor.VAdjustment.ValueChanged += HandleValueChanged;
			editor.HAdjustment.ValueChanged += HandleValueChanged;
			fontDescription = FontService.GetFontDescription ("Pad");
			tooltipFontDescription = FontService.GetFontDescription ("Pad").CopyModified (weight: Pango.Weight.Bold);
			errorCountFontDescription = FontService.GetFontDescription ("Pad").CopyModified (weight: Pango.Weight.Bold);
		}

		void HandleValueChanged (object sender, EventArgs e)
		{
			DestroyPopoverWindow ();
		}

		void HandleMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			if (CurrentSelectedTextMarker == null)
				DestroyPopoverWindow ();
		}

		uint hoverTimeout;

		void CancelHoverTimeout ()
		{
			if (hoverTimeout != 0) {
				GLib.Source.Remove (hoverTimeout);
				hoverTimeout = 0;
			}
		}
		MessageBubblePopoverWindow popoverWindow;

		class MessageBubblePopoverWindow : PopoverWindow
		{
			readonly MessageBubbleCache cache;
			readonly MessageBubbleTextMarker marker;

			public MessageBubblePopoverWindow (MessageBubbleCache cache, MessageBubbleTextMarker marker)
			{
				this.cache = cache;
				this.marker = marker;
				ShowArrow = true;
				Theme.ArrowLength = 7;
				TransientFor = IdeApp.Workbench.RootWindow;
			}

			// Layout constants
			const int verticalTextBorder = 10;
			const int verticalTextSpace  = 7;

			const int textBorder = 12;
			const int iconTextSpacing = 8;

			readonly int maxTextWidth = (int)(260 * Pango.Scale.PangoScale);

			protected override void OnSizeRequested (ref Gtk.Requisition requisition)
			{
				base.OnSizeRequested (ref requisition);
				double y = verticalTextBorder * 2 - verticalTextSpace + (MonoDevelop.Core.Platform.IsWindows ? 10 : 2);

				using (var drawingLayout = new Pango.Layout (this.PangoContext)) {
					drawingLayout.FontDescription = cache.tooltipFontDescription;

					foreach (var msg in marker.Errors) {
						if (marker.Layouts.Count == 1) 
							drawingLayout.Width = maxTextWidth;
						drawingLayout.SetText (msg.FullErrorMessage);
						int w;
						int h;
						drawingLayout.GetPixelSize (out w, out h);
						if (marker.Layouts.Count > 1) 
							w += (int)warningPixbuf.Width + iconTextSpacing;

						requisition.Width = Math.Max (w + textBorder * 2, requisition.Width);
						y += h + verticalTextSpace - 3;
					}
				}

				requisition.Height = (int)y;
			}

			protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
			{
				cache.CancelLeaveDestroyTimeout ();
				return base.OnEnterNotifyEvent (evnt);
			}

			protected override void OnDrawContent (Gdk.EventExpose evnt, Cairo.Context g)
			{
				g.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				g.SetSourceColor (marker.TooltipColor);
				g.Fill ();

				using (var drawingLayout = new Pango.Layout (this.PangoContext)) {
					drawingLayout.FontDescription = cache.tooltipFontDescription;

					double y = verticalTextBorder;
					var showBulletedList = marker.Errors.Count > 1;

					foreach (var msg in marker.Errors) {
						var icon = msg.IsError ? errorPixbuf : warningPixbuf;
						int w, h;

						if (!showBulletedList)
							drawingLayout.Width = maxTextWidth;

						drawingLayout.SetText (msg.FullErrorMessage);
						drawingLayout.GetPixelSize (out w, out h);

						if (showBulletedList) {
							g.Save ();

							g.Translate (textBorder, y + verticalTextSpace / 2 + Math.Max (0, (h - icon.Height) / 2));
							g.DrawImage (this, icon, 0, 0);
							g.Restore ();
						}

						g.Save ();

						g.Translate (showBulletedList ? textBorder + iconTextSpacing + icon.Width: textBorder, y + verticalTextSpace / 2);
						g.SetSourceColor (marker.TagColor2);
						g.ShowLayout (drawingLayout);

						g.Restore ();

						y += h + verticalTextSpace;
					}
				}
			}
		}

		public void StartHover (MessageBubbleTextMarker marker, double bubbleX, double bubbleY, double bubbleWidth, bool isReduced)
		{
			CancelHoverTimeout ();
			if (removedMarker == marker) {
				CurrentSelectedTextMarker = marker;
				return;
			}

			hoverTimeout = GLib.Timeout.Add (200, delegate {
				CurrentSelectedTextMarker = marker;
				editor.QueueDraw ();

				DestroyPopoverWindow ();

				hoverTimeout = 0;
				if (marker.Layouts == null || marker.Layouts.Count < 2 && !isReduced) {
					return false;
				}

				popoverWindow = new MessageBubblePopoverWindow (this, marker);
				popoverWindow.ShowWindowShadow = false;
				popoverWindow.ShowPopup (editor, new Gdk.Rectangle ((int)(bubbleX + editor.TextViewMargin.XOffset), (int)bubbleY, (int)bubbleWidth, (int)editor.LineHeight), PopupPosition.Top);

				return false;
			});
		}

		MessageBubbleTextMarker removedMarker;

		void HandleBeginHover (object sender, EventArgs e)
		{
			CancelHoverTimeout ();
			removedMarker = CurrentSelectedTextMarker;
			if (CurrentSelectedTextMarker == null)
				return;
			CurrentSelectedTextMarker = null;
			editor.QueueDraw ();
		}

		uint leaveDestroyTimeout;

		void CancelLeaveDestroyTimeout ()
		{
			if (leaveDestroyTimeout != 0) {
				GLib.Source.Remove (leaveDestroyTimeout);
				leaveDestroyTimeout = 0;
			}
		}

		void HandleLeaveNotifyEvent (object o, Gtk.LeaveNotifyEventArgs args)
		{
			CancelLeaveDestroyTimeout ();
			leaveDestroyTimeout = GLib.Timeout.Add (100, delegate {
				DestroyPopoverWindow ();
				leaveDestroyTimeout = 0;
				return false;
			}); 

			CancelHoverTimeout ();
			if (CurrentSelectedTextMarker == null)
				return;
			CurrentSelectedTextMarker = null;
			editor.QueueDraw ();
		}

		internal void DestroyPopoverWindow ()
		{
			if (popoverWindow != null) {
				popoverWindow.Destroy ();
				popoverWindow = null;
			}
		}

		public void Dispose ()
		{
			CancelLeaveDestroyTimeout ();
			CancelHoverTimeout ();
			DestroyPopoverWindow ();
			editor.VAdjustment.ValueChanged -= HandleValueChanged;
			editor.HAdjustment.ValueChanged -= HandleValueChanged;
			editor.TextArea.BeginHover -= HandleBeginHover;
			editor.TextArea.LeaveNotifyEvent -= HandleLeaveNotifyEvent;
			editor.TextArea.MotionNotifyEvent -= HandleMotionNotifyEvent;
			editor.EditorOptionsChanged -= HandleEditorEditorOptionsChanged;
			if (textWidthDictionary != null) {
				foreach (var l in textWidthDictionary.Values) {
					l.Layout.Dispose ();
				}
			}
		}

		static string GetFirstLine (string firstLine)
		{
			int idx = firstLine.IndexOfAny (new [] { '\n', '\r' });
			if (idx > 0)
				firstLine = firstLine.Substring (0, idx);
			return firstLine;
		}

		internal LayoutDescriptor CreateLayoutDescriptor (ErrorText errorText)
		{
			LayoutDescriptor result;
			if (!textWidthDictionary.TryGetValue (errorText.ErrorMessage, out result)) {
				Pango.Layout layout = new Pango.Layout (editor.PangoContext);
				layout.FontDescription = fontDescription;
				layout.SetText (GetFirstLine (errorText.ErrorMessage));
				int w, h;
				layout.GetPixelSize (out w, out h);
				textWidthDictionary[errorText.ErrorMessage] = result = new LayoutDescriptor (layout, w, h);
			}
			return result;
		}

		void HandleEditorEditorOptionsChanged (object sender, EventArgs e)
		{
			OnChanged (EventArgs.Empty);
		}	

		internal class LayoutDescriptor
		{
			public Pango.Layout Layout { get; set; }
			public int Width { get; set; }
			public int Height { get; set; }

			public LayoutDescriptor (Pango.Layout layout, int width, int height)
			{
				this.Layout = layout;
				this.Width = width;
				this.Height = height;
			}
		}
	
		protected virtual void OnChanged (EventArgs e)
		{
			EventHandler handler = this.Changed;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler Changed;
	}
}
