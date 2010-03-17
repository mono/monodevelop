//
// GtkColors.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using Cairo;
using Gtk;

namespace MonoDevelop.Components.Theming
{
	public enum GtkColorClass
	{
		Light,
		Mid,
		Dark,
		Base,
		Text,
		Background,
		Foreground
	}

	public class GtkColors
	{
		private Cairo.Color[] gtk_colors;
		private Widget widget;
		private bool refreshing = false;

		public event EventHandler Refreshed;

		public Widget Widget {
			get { return widget; }
			set {
				if (widget == value) {
					return;
				} else if (widget != null) {
					widget.Realized -= OnWidgetRealized;
					widget.StyleSet -= OnWidgetStyleSet;
				}
				
				widget = value;
				
				if (widget.IsRealized) {
					RefreshColors ();
				}
				
				widget.Realized += OnWidgetRealized;
				widget.StyleSet += OnWidgetStyleSet;
			}
		}

		public GtkColors ()
		{
		}

		private void OnWidgetRealized (object o, EventArgs args)
		{
			RefreshColors ();
		}

		private void OnWidgetStyleSet (object o, StyleSetArgs args)
		{
			RefreshColors ();
		}

		public Cairo.Color GetWidgetColor (GtkColorClass @class, StateType state)
		{
			if (gtk_colors == null) {
				RefreshColors ();
			}
			
			return gtk_colors[(int)@class * ((int)StateType.Insensitive + 1) + (int)state];
		}

		public void RefreshColors ()
		{
			if (refreshing) {
				return;
			}
			
			refreshing = true;
			
			int sn = (int)StateType.Insensitive + 1;
			int cn = (int)GtkColorClass.Foreground + 1;
			
			if (gtk_colors == null) {
				gtk_colors = new Cairo.Color[sn * cn];
			}
			
			for (int c = 0, i = 0; c < cn; c++) {
				for (int s = 0; s < sn; s++,i++) {
					Gdk.Color color = Gdk.Color.Zero;
					
					if (widget != null && widget.IsRealized) {
						switch ((GtkColorClass)c) {
						case GtkColorClass.Light:
							color = widget.Style.LightColors[s];
							break;
						case GtkColorClass.Mid:
							color = widget.Style.MidColors[s];
							break;
						case GtkColorClass.Dark:
							color = widget.Style.DarkColors[s];
							break;
						case GtkColorClass.Base:
							color = widget.Style.BaseColors[s];
							break;
						case GtkColorClass.Text:
							color = widget.Style.TextColors[s];
							break;
						case GtkColorClass.Background:
							color = widget.Style.Backgrounds[s];
							break;
						case GtkColorClass.Foreground:
							color = widget.Style.Foregrounds[s];
							break;
						}
					} else {
						color = new Gdk.Color (0, 0, 0);
					}
					
					gtk_colors[c * sn + s] = CairoExtensions.GdkColorToCairoColor (color);
				}
			}
			
			OnRefreshed ();
			
			refreshing = false;
		}

		protected virtual void OnRefreshed ()
		{
			EventHandler handler = Refreshed;
			if (handler != null) {
				handler (this, EventArgs.Empty);
			}
		}
	}
}
