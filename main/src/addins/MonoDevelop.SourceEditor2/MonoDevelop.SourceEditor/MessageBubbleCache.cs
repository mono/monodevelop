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

namespace MonoDevelop.SourceEditor
{
	class MessageBubbleCache : IDisposable
	{
		internal Xwt.Drawing.Image errorPixbuf;
		internal Xwt.Drawing.Image warningPixbuf;
		
		internal Dictionary<string, LayoutDescriptor> textWidthDictionary = new Dictionary<string, LayoutDescriptor> ();
		internal Dictionary<DocumentLine, double> lineWidthDictionary = new Dictionary<DocumentLine, double> ();
		
		internal TextEditor editor;

		internal Pango.FontDescription fontDescription;

		public MessageBubbleTextMarker CurrentSelectedTextMarker;

		public MessageBubbleCache (TextEditor editor)
		{
			this.editor = editor;
			errorPixbuf = ImageService.GetIcon ("md-bubble-error", Gtk.IconSize.Menu);
			warningPixbuf = ImageService.GetIcon ("md-bubble-warning", Gtk.IconSize.Menu);
			
			editor.EditorOptionsChanged += HandleEditorEditorOptionsChanged;
			editor.LeaveNotifyEvent += HandleLeaveNotifyEvent;
			editor.MotionNotifyEvent += HandleMotionNotifyEvent;
			editor.TextArea.BeginHover += HandleBeginHover;
			fontDescription = FontService.GetFontDescription ("MessageBubbles");
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
				Opacity = 0.9;
			}

			protected override void OnSizeRequested (ref Gtk.Requisition requisition)
			{
				base.OnSizeRequested (ref requisition);
				double y = 0;
				foreach (var layout in marker.Layouts) {
					requisition.Width = Math.Max (layout.Width + 8, requisition.Width);
					y += layout.Height;
				}
				requisition.Height = (int)y + 12;
			}

			protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
			{
				cache.DestroyPopoverWindow ();
				return base.OnEnterNotifyEvent (evnt);
			}

			protected override void OnDrawContent (Gdk.EventExpose evnt, Cairo.Context g)
			{
				Theme.BorderColor = marker.TagColor.Color;
				g.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				g.Color = marker.TagColor.Color;
				g.Fill ();

				double y = 8;
				double x = 4;
				foreach (var layout in marker.Layouts) {
					g.Save ();
					g.Translate (x, y);
					g.Color = marker.TagColor.SecondColor;
					g.ShowLayout (layout.Layout);
					g.Restore ();
					y += layout.Height;
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

				if (marker.Layouts == null || marker.Layouts.Count < 2 && !isReduced)
					return false;
				popoverWindow = new MessageBubblePopoverWindow (this, marker);
				popoverWindow.ShowPopup (editor, new Gdk.Rectangle ((int)(bubbleX + editor.TextViewMargin.XOffset), (int)bubbleY, (int)bubbleWidth, (int)editor.LineHeight) ,PopupPosition.Top);
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

		void HandleLeaveNotifyEvent (object o, Gtk.LeaveNotifyEventArgs args)
		{
			DestroyPopoverWindow ();
			CancelHoverTimeout ();
			if (CurrentSelectedTextMarker == null)
				return;
			CurrentSelectedTextMarker = null;
			editor.QueueDraw ();
		}

		public bool RemoveLine (DocumentLine line)
		{
			if (!lineWidthDictionary.ContainsKey (line))
				return false;
			lineWidthDictionary.Remove (line);
			return true;
		}

		void DestroyPopoverWindow ()
		{
			if (popoverWindow != null) {
				popoverWindow.Destroy ();
				popoverWindow = null;
			}
		}

		public void Dispose ()
		{
			CancelHoverTimeout ();
			DestroyPopoverWindow ();
			editor.TextArea.BeginHover -= HandleBeginHover;
			editor.LeaveNotifyEvent -= HandleLeaveNotifyEvent;
			editor.MotionNotifyEvent -= HandleMotionNotifyEvent;
			editor.EditorOptionsChanged -= HandleEditorEditorOptionsChanged;
			if (textWidthDictionary != null) {
				foreach (var l in textWidthDictionary.Values) {
					l.Layout.Dispose ();
				}
			}
		}

		static string GetFirstLine (ErrorText errorText)
		{
			string firstLine = errorText.ErrorMessage ?? "";
			int idx = firstLine.IndexOfAny (new [] {'\n', '\r'});
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
				layout.SetText (GetFirstLine (errorText));
				int w, h;
				layout.GetPixelSize (out w, out h);
				textWidthDictionary[errorText.ErrorMessage] = result = new LayoutDescriptor (layout, w, h);
			}
			return result;
		}

		void HandleEditorEditorOptionsChanged (object sender, EventArgs e)
		{
			lineWidthDictionary.Clear ();
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

