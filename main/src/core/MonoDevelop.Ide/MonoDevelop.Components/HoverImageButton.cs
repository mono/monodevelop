/***************************************************************************
 *  HoverImageButton.cs
 *
 *  Copyright (C) 2007 Novell, Inc.
 *  Written by Aaron Bockover <abockover@novell.com>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW:
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),
 *  to deal in the Software without restriction, including without limitation
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,
 *  and/or sell copies of the Software, and to permit persons to whom the
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using Gtk;
using MonoDevelop.Ide;

namespace MonoDevelop.Components
{
    class HoverImageButton : EventBox
    {
        private static Gdk.Cursor hand_cursor = new Gdk.Cursor(Gdk.CursorType.Hand1);

        private IconSize icon_size = IconSize.Menu;
        private string [] icon_names = { "image-missing", Stock.MissingImage };
        private Gdk.Pixbuf normal_pixbuf;
        private Gdk.Pixbuf active_pixbuf;
        private Image image;
        private bool is_hovering;
        private bool is_pressed;

        private bool draw_focus = true;

        private event EventHandler clicked;

        public event EventHandler Clicked {
            add { clicked += value; }
            remove { clicked -= value; }
        }

        public HoverImageButton()
        {
			Gtk.Alignment al = new Alignment (0.5f, 0.5f, 0f, 0f);
			al.Show ();
            CanFocus = true;
			VisibleWindow = false;
            image = new Image();
            image.Show();
			al.Add (image);
            Add(al);
        }

        public HoverImageButton(IconSize size, string icon_name) : this(size, new string [] { icon_name })
        {
        }

        public HoverImageButton(IconSize size, params string [] icon_names) : this()
        {
            this.icon_size = size;
            this.icon_names = icon_names;
        }

        public new void Activate()
        {
            EventHandler handler = clicked;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }

        private bool changing_style = false;
        protected override void OnStyleSet(Style previous_style)
        {
            if(changing_style) {
                return;
            }

            changing_style = true;
			if (normal_pixbuf == null)
	            LoadPixbufs();
            changing_style = false;
        }

        protected override bool OnEnterNotifyEvent(Gdk.EventCrossing evnt)
        {
            image.GdkWindow.Cursor = hand_cursor;
            is_hovering = true;
            UpdateImage();
            return base.OnEnterNotifyEvent(evnt);
        }

        protected override bool OnLeaveNotifyEvent(Gdk.EventCrossing evnt)
        {
			image.GdkWindow.Cursor = null;
			is_hovering = false;
            UpdateImage();
            return base.OnLeaveNotifyEvent(evnt);
        }

        protected override bool OnFocusInEvent(Gdk.EventFocus evnt)
        {
            bool ret = base.OnFocusInEvent(evnt);
            UpdateImage();
            return ret;
        }

        protected override bool OnFocusOutEvent(Gdk.EventFocus evnt)
        {
            bool ret = base.OnFocusOutEvent(evnt);
            UpdateImage();
            return ret;
        }

        protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
        {
            if(evnt.Button != 1) {
                return base.OnButtonPressEvent(evnt);
            }

            HasFocus = true;
            is_pressed = true;
            QueueDraw();

            return base.OnButtonPressEvent(evnt);
        }

        protected override bool OnButtonReleaseEvent(Gdk.EventButton evnt)
        {
            if(evnt.Button != 1) {
                return base.OnButtonReleaseEvent(evnt);
            }

            is_pressed = false;
            QueueDraw();
            Activate();

            return base.OnButtonReleaseEvent(evnt);
        }

        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            base.OnExposeEvent(evnt);

            if(HasFocus && draw_focus) {
                Style.PaintFocus(Style, GdkWindow, StateType.Normal, evnt.Area, this, "button",
                    0, 0, Allocation.Width, Allocation.Height);
            }

            return true;
        }

        private void UpdateImage()
        {
            image.Pixbuf = is_hovering || is_pressed || HasFocus
                ? active_pixbuf : normal_pixbuf;
        }

        private void LoadPixbufs()
        {
            int width, height;
            Icon.SizeLookup(icon_size, out width, out height);

            if(normal_pixbuf != null) {
                normal_pixbuf.Dispose();
                normal_pixbuf = null;
            }

            if(active_pixbuf != null) {
                active_pixbuf.Dispose();
                active_pixbuf = null;
            }

            for(int i = 0; i < icon_names.Length; i++) {
                try {
					normal_pixbuf = ImageService.GetPixbuf (icon_names[i], icon_size);
					active_pixbuf = ColorShiftPixbuf(normal_pixbuf, 30);
                    break;
                } catch {
                }
            }

            UpdateImage();
        }
		
		public Gdk.Pixbuf Pixbuf {
			get { return this.normal_pixbuf; }
			set { 
				this.normal_pixbuf = value; 
				active_pixbuf = ColorShiftPixbuf(normal_pixbuf, 30); 
				UpdateImage();
			}
		}
		

        private static byte PixelClamp(int val)
        {
            return (byte)Math.Max(0, Math.Min(255, val));
        }

        private unsafe Gdk.Pixbuf ColorShiftPixbuf(Gdk.Pixbuf src, byte shift)
        {
            Gdk.Pixbuf dest = new Gdk.Pixbuf(src.Colorspace, src.HasAlpha, src.BitsPerSample, src.Width, src.Height);

            byte *src_pixels_orig = (byte *)src.Pixels;
            byte *dest_pixels_orig = (byte *)dest.Pixels;

            for(int i = 0; i < src.Height; i++) {
                byte *src_pixels = src_pixels_orig + i * src.Rowstride;
                byte *dest_pixels = dest_pixels_orig + i * dest.Rowstride;

                for(int j = 0; j < src.Width; j++) {
                    *(dest_pixels++) = PixelClamp(*(src_pixels++) - shift);
                    *(dest_pixels++) = PixelClamp(*(src_pixels++) - shift);
                    *(dest_pixels++) = PixelClamp(*(src_pixels++) - shift);

                    if(src.HasAlpha) {
                        *(dest_pixels++) = *(src_pixels++);
                    }
                }
            }

            return dest;
        }

        public string [] IconNames {
            get { return icon_names; }
            set {
                icon_names = value;
                LoadPixbufs();
            }
        }

        public IconSize IconSize {
            get { return icon_size; }
            set {
                icon_size = value;
                LoadPixbufs();
            }
        }

        public Image Image {
            get { return image; }
        }

        public bool DrawFocus {
            get { return draw_focus; }
            set {
                draw_focus = value;
                QueueDraw();
            }
        }
    }
}
