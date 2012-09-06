// 
// WrapLabel.cs
// 
// Author:
//    Aaron Bockover <abockover@novell.com>
//    Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using Gtk;

namespace MonoDevelop.Components
{
	[System.ComponentModel.Category("MonoDevelop.Components")]
	[System.ComponentModel.ToolboxItem(true)]
	public class FixedWidthWrapLabel : Widget
	{
		string text;
		bool use_markup = false;
		Pango.Layout layout;
		int indent;
		int width = int.MaxValue;
		
		bool breakOnPunctuation;
		bool breakOnCamelCasing;
		string brokentext;
		
		Pango.WrapMode wrapMode = Pango.WrapMode.Word;
		
		Pango.FontDescription fontDescription;
		public Pango.FontDescription FontDescription { 
			get {
				return fontDescription;
			}
			set {
				fontDescription = value;
			}
			
		}
		public FixedWidthWrapLabel ()
		{
			WidgetFlags |= WidgetFlags.NoWindow;
		}
		
		public FixedWidthWrapLabel (string text)
			: this ()
		{
			this.text = text;
		}
		
		public FixedWidthWrapLabel (string text, int width)
			: this (text)
		{
			this.width = width;
		}
		
		void CreateLayout ()
		{
			if (layout != null) {
				layout.Dispose ();
			}
			
			layout = new Pango.Layout (PangoContext);
			if (FontDescription != null)
				layout.FontDescription = FontDescription;
			if (use_markup) {
				layout.SetMarkup (brokentext != null? brokentext : (text ?? string.Empty));
			} else {
				layout.SetText (brokentext != null? brokentext : (text ?? string.Empty));
			}
			layout.Indent = (int) (indent * Pango.Scale.PangoScale);
			layout.Wrap = wrapMode;
			if (width >= 0)
				layout.Width = (int)(width * Pango.Scale.PangoScale);
			else
				layout.Width = int.MaxValue;
			QueueResize ();
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
		}
		
		void UpdateLayout ()
		{
			if (layout == null) {
				CreateLayout ();
			}
		}
		
		public int MaxWidth {
			get { return width; }
			set {
				width = value;
				if (layout != null) {
					if (width >= 0)
						layout.Width = (int)(width * Pango.Scale.PangoScale);
					else
						layout.Width = int.MaxValue;
					QueueResize ();
				}
			}
		}
		
		public int RealWidth {
			get {
				UpdateLayout ();
				int lw, lh;
				layout.GetPixelSize (out lw, out lh);
				return lw;
			}
		}
		
		protected override void OnStyleSet (Style previous_style)
		{
			CreateLayout ();
			base.OnStyleSet (previous_style);
		}
		
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			UpdateLayout ();
			int lw, lh;
			layout.GetPixelSize (out lw, out lh);
			requisition.Height = lh;
			requisition.Width = lw;
		}
		
//		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
//		{
//			//wrap to allocation and set automatic height if MaxWidth is -1
//			if (width < 0) {
//				int lw, lh;
//				layout.Width = (int)(allocation.Width * Pango.Scale.PangoScale);
//				layout.GetPixelSize (out lw, out lh);
//				HeightRequest = lh;
//			}
//			base.OnSizeAllocated (allocation);
//		}

		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			UpdateLayout ();
			if (evnt.Window != GdkWindow) {
				return base.OnExposeEvent (evnt);
			}
            
			Gtk.Style.PaintLayout (Style, GdkWindow, State, false, evnt.Area, 
			    this, null, Allocation.X, Allocation.Y, layout);
			
			return true;
		}
        
		public string Markup {
			get { return text; }
			set {
				use_markup = true;
				text = value;
				breakText ();
			}
		}
        
		public string Text {
			get { return text; }
			set {
				use_markup = false;
				text = value;
				breakText ();
			}
		}  
		
		public int Indent {
			get { return indent; }
			set {
				indent = value;
				if (layout != null) {
					layout.Indent = (int) (indent * Pango.Scale.PangoScale);
					QueueResize ();
				}
			}
		}
		
		public bool BreakOnPunctuation {
			get { return breakOnPunctuation; }
			set {
				breakOnPunctuation = value;
				breakText ();
			}
		}
		
		public bool BreakOnCamelCasing {
			get { return breakOnCamelCasing; }
			set {
				breakOnCamelCasing = value;
				breakText ();
			}
		}
		
		public Pango.WrapMode Wrap {
			get { return wrapMode; }
			set {
				wrapMode = value;
				if (layout != null) {
					layout.Wrap = wrapMode;
					QueueResize ();
				}
			}
		}
		
		void breakText ()
		{
			brokentext = null;
			if ((!breakOnCamelCasing && !breakOnPunctuation) || string.IsNullOrEmpty (text)) {
				QueueResize ();
				return;
			}
			
			System.Text.StringBuilder sb = new System.Text.StringBuilder (text.Length);
			
			bool prevIsLower = false;
			bool inMarkup = false;
			bool inEntity = false;
			int lineLength = 0;

			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				
				//ignore markup
				if (use_markup) {
					switch (c) {
					case '<':
						inMarkup = true;
						sb.Append (c);
						continue;
					case '>':
						inMarkup = false;
						sb.Append (c);
						continue;
					case '&':
						inEntity = true;
						sb.Append (c);
						continue;
					case ';':
						if (inEntity) {
							inEntity = false;
							sb.Append (c);
							continue;
						}
						break;
					}
				}
				if (inMarkup || inEntity) {
					sb.Append (c);
					continue;
				}

				// Camel case breaks before the next word start
				if (lineLength > 0) {
					//insert breaks using zero-width space unicode char
					if (breakOnCamelCasing && prevIsLower && char.IsUpper (c))
						sb.Append ('\u200b');
				}

				sb.Append (c);

				// Punctuation breaks after the punctuation
				if (lineLength > 0) {
					//insert breaks using zero-width space unicode char
					if (breakOnPunctuation && char.IsPunctuation (c)) 
						sb.Append ('\u200b');
				}
				if (c == '\n' || c == '\r') {
					lineLength = 0;
				} else {
					lineLength++;
				}

				if (breakOnCamelCasing)
					prevIsLower = char.IsLower (c);
			}
			brokentext = sb.ToString ();

			if (layout != null) {
				if (use_markup) {
					layout.SetMarkup (brokentext != null? brokentext : (text ?? string.Empty));
				} else {
					layout.SetText (brokentext != null? brokentext : (text ?? string.Empty));
				}
			}
			QueueResize ();
		}
	}
}
