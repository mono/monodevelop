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

namespace MonoDevelop.TextEditor.PopupWindow
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
			this.TypeHint = TooltipTypeHint;
			this.AllowShrink = false;
			this.AllowGrow = false;
			
			//fake widget name for stupid theme engines
			if (Gtk.Global.CheckVersion (2, 12, 0) == null)
				this.Name = "gtk-tooltip";
			else
				this.Name = "gtk-tooltips";
			
			label = new FixedWidthWrapLabel ();
			label.Wrap = Pango.WrapMode.WordChar;
			label.Indent = -20;
			label.BreakOnCamelCasing = true;
			label.BreakOnPunctuation = true;
			this.BorderWidth = 3;
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
		
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			base.OnExposeEvent (args);
			
			int winWidth, winHeight;
			this.GetSize (out winWidth, out winHeight);
			this.GdkWindow.DrawRectangle (this.Style.ForegroundGC (StateType.Insensitive), false, 0, 0, winWidth-1, winHeight-1);
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
				
				if (nudgeHorizontal) {
					int screenW = Screen.Width;
					if (allocation.Width <= screenW && x + allocation.Width >= screenW - edgeGap)
						x = (screenW - allocation.Height - edgeGap);
					if (x <= 0)
						x = 0;
				}
				
				if (nudgeVertical) {
					int screenH = Screen.Height;
					if (allocation.Height <= screenH && y + allocation.Height >= screenH - edgeGap)
						y = (screenH - allocation.Height - edgeGap);
					if (y <= 0)
						y = 0;
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
		
		//this is GTK+ >= 2.10 only, so reflect it
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
		}
		
		static int tooltipTypeHint = -1;
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
				
				layout = new Pango.Layout (PangoContext);
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
		
		public class WindowTransparencyDecorator
	{
		Gtk.Window window;
		bool semiTransparent;
		bool snooperInstalled;
		uint snooperID;
		const double opacity = 0.2;
		Delegate snoopFunc;
		
		WindowTransparencyDecorator (Gtk.Window window)
		{
			this.window = window;
			snoopFunc = TryBindGtkInternals (this);
			
			//FIXME: access this property directly when we use GTK# 2.12
			if (CanSetOpacity && snoopFunc != null) {
				window.Shown += ShownHandler;
				window.Hidden += HiddenHandler;
				window.Destroyed += DestroyedHandler;
			} else {
				snoopFunc = null;
				window = null;
			}
		}
		
		public static WindowTransparencyDecorator Attach (Gtk.Window window)
		{
			return new WindowTransparencyDecorator (window);
		}
		
		public void Detach ()
		{
			if (window == null)
			return;
			
			//remove the snooper
			HiddenHandler (null,  null);
			
			//annul allreferences between this and the window
			window.Shown -= ShownHandler;
			window.Hidden -= HiddenHandler;
			window.Destroyed -= DestroyedHandler;
			snoopFunc = null;
			window = null;
		}
		
		void ShownHandler (object sender, EventArgs args)
		{
			if (!snooperInstalled)
				snooperID = InstallSnooper (snoopFunc);
			snooperInstalled = true;
		}
		
		void HiddenHandler (object sender, EventArgs args)
		{
			if (snooperInstalled)
				RemoveSnooper (snooperID);
			
			snooperInstalled = false;
			SemiTransparent = false;
		}
		
		void DestroyedHandler (object sender, EventArgs args)
		{
			Detach ();
		}
		
		#pragma warning disable 0169
		
		int TransparencyKeySnooper (IntPtr widget, IntPtr rawEvnt, IntPtr data)
		{
			if (rawEvnt != IntPtr.Zero) {
				Gdk.EventKey evnt = new Gdk.EventKey (rawEvnt);
				if (evnt != null && evnt.Key == Gdk.Key.Control_L || evnt.Key == Gdk.Key.Control_R)
					SemiTransparent = (evnt.Type == Gdk.EventType.KeyPress);
			}
			return 0; //gboolean FALSE
		}
		
		#pragma warning restore 0169
		
		bool SemiTransparent {
			set {
				if (semiTransparent != value) {
					semiTransparent = value;
					TrySetTransparency (window, semiTransparent? opacity : 1.0);
				}
			}
		}
		
		#region Workaround for GTK# crasher bug where GC collects internal wrapper delegates
		
		static WindowTransparencyDecorator ()
		{
			snooper_install = typeof (Gtk.Key).GetMethod ("gtk_key_snooper_install", BindingFlags.NonPublic | BindingFlags.Static);
			snooper_remove = typeof (Gtk.Key).GetMethod ("gtk_key_snooper_remove", BindingFlags.NonPublic | BindingFlags.Static);
		}
		
		static MethodInfo snooper_install;
		static MethodInfo snooper_remove;
		
		delegate int GtkKeySnoopFunc (IntPtr widget, IntPtr rawEvnt, IntPtr func_data);
		
		static uint InstallSnooper (Delegate del)
		{
			return (uint) snooper_install.Invoke (null, new object[] { del, IntPtr.Zero} );
		}
		
		static void RemoveSnooper (uint id)
		{
			snooper_remove.Invoke (null, new object[] { id });
		}
		
		static bool internalBindingWorks = true;
		static bool internalBindingTried = false;
		
		static Delegate TryBindGtkInternals (WindowTransparencyDecorator instance)
		{
			if (internalBindingTried) {
				if (!internalBindingWorks)
					return null;
			} else {
				internalBindingTried = true;
			}
			
			try {
				Type delType = typeof(Gtk.Widget).Assembly.GetType ("GtkSharp.KeySnoopFuncNative");
				System.Reflection.MethodInfo met = typeof (WindowTransparencyDecorator).GetMethod ("TransparencyKeySnooper", 
				    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
				Delegate ret = Delegate.CreateDelegate (delType, instance, met);
				if (ret != null)
					return ret;
			} catch {}
			
			internalBindingWorks = false;
			Console.WriteLine ("GTK# API has changed, and control-transparency will not be available for popups");
			return null;
		}
		
		#endregion
			
		#region Static setter reflecting code -- property is only in GTK+ 2.12
		
		[DllImport("libgobject-2.0.so.0")]
		static extern IntPtr g_type_class_peek (IntPtr gtype);

		[DllImport("libgobject-2.0.so.0")]
		static extern IntPtr g_object_class_find_property (IntPtr klass, string name);

		[DllImport("libgobject-2.0.so.0")]
		static extern void g_object_set (IntPtr obj, string property, double value, IntPtr nullarg);
		
		static bool triedToFindSetters = false;
		
		//if we have GTK# 2.12 we can use Mono reflection
		static System.Reflection.PropertyInfo opacityProp = null;
		
		//if we have GTK+ 2.12 but an older GTK#, we can use GObject reflection
		static IntPtr opacityMeth = IntPtr.Zero;
		
		static bool CanSetOpacity {
			get {
				if (triedToFindSetters)
					return (opacityMeth != IntPtr.Zero || opacityProp != null);
				
				triedToFindSetters = true;
				
				opacityProp = typeof (Gtk.Window).GetProperty ("Opacity");
				if (opacityProp != null)
					return true;
				
				GLib.GType gtype = (GLib.GType) typeof (Gtk.Window);
				try {
					IntPtr klass = g_type_class_peek (gtype.Val);
					opacityMeth = g_object_class_find_property (klass, "opacity");
				} catch (DllNotFoundException) {}
				
				return opacityMeth != IntPtr.Zero;
			}
		}
		
		static void TrySetTransparency (Gtk.Window window, double opacity)
		{
			if (opacityMeth != IntPtr.Zero)
				g_object_set (window.Handle, "opacity", opacity, IntPtr.Zero);
			else if (opacityProp != null)
				opacityProp.SetValue (window, opacity, null);
		}
		
		#endregion
	}
	}
}
