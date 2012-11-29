// 
// TooltipWindow.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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
using System.Reflection;
using System.Runtime.InteropServices;

using Gtk;
using Gdk;

namespace Mono.TextEditor.PopupWindow
{
	public class TooltipWindow : Gtk.Window
	{
		bool nudgeVertical = false;
		bool nudgeHorizontal = false;
		WindowTransparencyDecorator decorator;
		FixedWidthWrapLabel label;
		
		public string Markup {
			get {
				return label.Markup;
			}
			set {
				label.Markup = value;
			}
		}
		
		public TooltipWindow () : base (Gtk.WindowType.Popup)
		{
			this.SkipPagerHint = true;
			this.SkipTaskbarHint = true;
			this.Decorated = false;
			this.BorderWidth = 2;
			this.TypeHint = WindowTypeHint.Tooltip;
			this.AllowShrink = false;
			this.AllowGrow = false;
			
			//fake widget name for stupid theme engines
			this.Name = "gtk-tooltip";
			
			label = new FixedWidthWrapLabel ();
			label.Wrap = Pango.WrapMode.WordChar;
			label.Indent = -20;
			label.BreakOnCamelCasing = true;
			label.BreakOnPunctuation = true;
			this.BorderWidth = 3;
			this.Title = "tooltip";
			Add (label);
			
			EnableTransparencyControl = true;
		}
		
		public int SetMaxWidth (int maxWidth)
		{
			FixedWidthWrapLabel l = (FixedWidthWrapLabel)Child;
			l.MaxWidth = maxWidth;
			return l.RealWidth;
		}
		
		public bool NudgeVertical {
			get { return nudgeVertical; }
			set { nudgeVertical = value; }
		}
		
		public bool NudgeHorizontal {
			get { return nudgeHorizontal; }
			set { nudgeHorizontal = value; }
		}
		
		public bool EnableTransparencyControl {
			get { return decorator != null; }
			set {
				if (value && decorator == null)
					decorator = WindowTransparencyDecorator.Attach (this);
				else if (!value && decorator != null)
					decorator.Detach ();
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			int winWidth, winHeight;
			this.GetSize (out winWidth, out winHeight);
			Gtk.Style.PaintFlatBox (Style, this.GdkWindow, StateType.Normal, ShadowType.Out, evnt.Area, this, "tooltip", 0, 0, winWidth, winHeight);
			foreach (var child in this.Children)
				this.PropagateExpose (child, evnt);
			return false;
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			if (nudgeHorizontal || nudgeVertical) {
				int x, y;
				this.GetPosition (out x, out y);
				int oldY = y, oldX = x;
				const int edgeGap = 2;
				
//				int w = allocation.Width;
//				
//				if (fitWidthToScreen && (x + w >= screenW - edgeGap)) {
//					int fittedWidth = screenW - x - edgeGap;
//					if (fittedWidth < minFittedWidth) {
//						x -= (minFittedWidth - fittedWidth);
//						fittedWidth = minFittedWidth;
//					}
//					LimitWidth (fittedWidth);
//				}
				
				Gdk.Rectangle geometry = Screen.GetUsableMonitorGeometry (Screen.GetMonitorAtPoint (x, y));
				if (nudgeHorizontal) {
					if (allocation.Width <= geometry.Width && x + allocation.Width >= geometry.Left + geometry.Width - edgeGap)
						x = geometry.Left + (geometry.Width - allocation.Width - edgeGap);
					if (x <= geometry.Left + edgeGap)
						x = geometry.Left + edgeGap;
				}
				
				if (nudgeVertical) {
					if (allocation.Height <= geometry.Height && y + allocation.Height >= geometry.Top + geometry.Height - edgeGap)
						y = geometry.Top + (geometry.Height - allocation.Height - edgeGap);
					if (y <= geometry.Top + edgeGap)
						y = geometry.Top + edgeGap;
				}
				
				if (y != oldY || x != oldX)
					Move (x, y);
			}
			
			base.OnSizeAllocated (allocation);
		}
		
//		void LimitWidth (int width)
//		{
//			if (Child is MonoDevelop.Components.FixedWidthWrapLabel) 
//				((MonoDevelop.Components.FixedWidthWrapLabel)Child).MaxWidth = width - 2 * (int)this.BorderWidth;
//			
//			int childWidth = Child.SizeRequest ().Width;
//			if (childWidth < width)
//				WidthRequest = childWidth;
//			else
//				WidthRequest = width;
//		}
		
	/*	//this is GTK+ >= 2.10 only, so reflect it
		static Gdk.WindowTypeHint TooltipTypeHint {
			get {
				if (tooltipTypeHint > -1)
					return (Gdk.WindowTypeHint) tooltipTypeHint;
				
				tooltipTypeHint = (int) Gdk.WindowTypeHint.Dialog;
				
				System.Reflection.FieldInfo fi = typeof (Gdk.WindowTypeHint).GetField ("Tooltip");
				if (fi != null)
					tooltipTypeHint = (int) fi.GetValue (typeof (Gdk.WindowTypeHint));
				
				return (Gdk.WindowTypeHint) tooltipTypeHint;
			}
		}*/
		
		//static int tooltipTypeHint = -1;
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
			
			private void CreateLayout ()
			{
				if (layout != null) {
					layout.Dispose ();
				}
				
				layout = PangoUtil.CreateLayout (this, null);
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
			
			private void UpdateLayout ()
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
						
					//insert breaks using zero-width space unicode char
					if ((breakOnPunctuation && char.IsPunctuation (c))
					    || (breakOnCamelCasing && prevIsLower && char.IsUpper (c)))
						sb.Append ('\u200b');
					
					sb.Append (c);
					
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
}
