//
// CairoExtensions.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

using Gdk;
using Cairo;

namespace MonoDevelop.Components
{
    [Flags]
    public enum CairoCorners
    {
        None = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomLeft = 4,
        BottomRight = 8,
        All = 15
    }

    public static class CairoExtensions
    {
        public static Pango.Layout CreateLayout (Gtk.Widget widget, Cairo.Context cairo_context)
        {
            Pango.Layout layout = PangoCairoHelper.CreateLayout (cairo_context);
            layout.FontDescription = widget.PangoContext.FontDescription.Copy ();

            double resolution = widget.Screen.Resolution;
            if (resolution != -1) {
                Pango.Context context = PangoCairoHelper.LayoutGetContext (layout);
                PangoCairoHelper.ContextSetResolution (context, resolution);
                context.Dispose ();
            }

            return layout;
        }

        public static Surface CreateSurfaceForPixbuf (Cairo.Context cr, Gdk.Pixbuf pixbuf)
        {
            Surface surface = cr.Target.CreateSimilar (cr.Target.Content, pixbuf.Width, pixbuf.Height);
            Cairo.Context surface_cr = new Context (surface);
            Gdk.CairoHelper.SetSourcePixbuf (surface_cr, pixbuf, 0, 0);
            surface_cr.Paint ();
            ((IDisposable)surface_cr).Dispose ();
            return surface;
        }

        public static Cairo.Color AlphaBlend (Cairo.Color ca, Cairo.Color cb, double alpha)
        {
            return new Cairo.Color (
                (1.0 - alpha) * ca.R + alpha * cb.R,
                (1.0 - alpha) * ca.G + alpha * cb.G,
                (1.0 - alpha) * ca.B + alpha * cb.B);
        }
		public static Gdk.Color CairoColorToGdkColor(Cairo.Color color)
        {
            return new Gdk.Color ((byte)(color.R * 255), (byte)(color.G * 255), (byte)(color.B * 255));
        }

        public static Cairo.Color GdkColorToCairoColor(Gdk.Color color)
        {
            return GdkColorToCairoColor(color, 1.0);
        }

        public static Cairo.Color GdkColorToCairoColor(Gdk.Color color, double alpha)
        {
            return new Cairo.Color(
                (double)(color.Red >> 8) / 255.0,
                (double)(color.Green >> 8) / 255.0,
                (double)(color.Blue >> 8) / 255.0,
                alpha);
        }

        public static Cairo.Color RgbToColor (uint rgbColor)
        {
            return RgbaToColor ((rgbColor << 8) | 0x000000ff);
        }

        public static Cairo.Color RgbaToColor (uint rgbaColor)
        {
            return new Cairo.Color (
                (byte)(rgbaColor >> 24) / 255.0,
                (byte)(rgbaColor >> 16) / 255.0,
                (byte)(rgbaColor >> 8) / 255.0,
                (byte)(rgbaColor & 0x000000ff) / 255.0);
        }

        public static bool ColorIsDark (Cairo.Color color)
        {
            double h, s, b;
            HsbFromColor (color, out h, out s, out b);
            return b < 0.5;
        }

        public static void HsbFromColor(Cairo.Color color, out double hue,
            out double saturation, out double brightness)
        {
            double min, max, delta;
            double red = color.R;
            double green = color.G;
            double blue = color.B;

            hue = 0;
            saturation = 0;
            brightness = 0;

            if(red > green) {
                max = Math.Max(red, blue);
                min = Math.Min(green, blue);
            } else {
                max = Math.Max(green, blue);
                min = Math.Min(red, blue);
            }

            brightness = (max + min) / 2;

            if(Math.Abs(max - min) < 0.0001) {
                hue = 0;
                saturation = 0;
            } else {
                saturation = brightness <= 0.5
                    ? (max - min) / (max + min)
                    : (max - min) / (2 - max - min);

                delta = max - min;

                if(red == max) {
                    hue = (green - blue) / delta;
                } else if(green == max) {
                    hue = 2 + (blue - red) / delta;
                } else if(blue == max) {
                    hue = 4 + (red - green) / delta;
                }

                hue *= 60;
                if(hue < 0) {
                    hue += 360;
                }
            }
        }

        private static double Modula(double number, double divisor)
        {
            return ((int)number % divisor) + (number - (int)number);
        }

        public static Cairo.Color ColorFromHsb(double hue, double saturation, double brightness)
        {
            int i;
            double [] hue_shift = { 0, 0, 0 };
            double [] color_shift = { 0, 0, 0 };
            double m1, m2, m3;

            m2 = brightness <= 0.5
                ? brightness * (1 + saturation)
                : brightness + saturation - brightness * saturation;

            m1 = 2 * brightness - m2;

            hue_shift[0] = hue + 120;
            hue_shift[1] = hue;
            hue_shift[2] = hue - 120;

            color_shift[0] = color_shift[1] = color_shift[2] = brightness;

            i = saturation == 0 ? 3 : 0;

            for(; i < 3; i++) {
                m3 = hue_shift[i];

                if(m3 > 360) {
                    m3 = Modula(m3, 360);
                } else if(m3 < 0) {
                    m3 = 360 - Modula(Math.Abs(m3), 360);
                }

                if(m3 < 60) {
                    color_shift[i] = m1 + (m2 - m1) * m3 / 60;
                } else if(m3 < 180) {
                    color_shift[i] = m2;
                } else if(m3 < 240) {
                    color_shift[i] = m1 + (m2 - m1) * (240 - m3) / 60;
                } else {
                    color_shift[i] = m1;
                }
            }

            return new Cairo.Color(color_shift[0], color_shift[1], color_shift[2]);
        }

        public static Cairo.Color ColorShade (Cairo.Color @base, double ratio)
        {
            double h, s, b;

            HsbFromColor (@base, out h, out s, out b);

            b = Math.Max (Math.Min (b * ratio, 1), 0);
            s = Math.Max (Math.Min (s * ratio, 1), 0);

            Cairo.Color color = ColorFromHsb (h, s, b);
            color.A = @base.A;
            return color;
        }

        public static Cairo.Color ColorAdjustBrightness(Cairo.Color @base, double br)
        {
            double h, s, b;
            HsbFromColor(@base, out h, out s, out b);
            b = Math.Max(Math.Min(br, 1), 0);
            return ColorFromHsb(h, s, b);
        }

        public static string ColorGetHex (Cairo.Color color, bool withAlpha)
        {
            if (withAlpha) {
                return String.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", (byte)(color.R * 255), (byte)(color.G * 255),
                    (byte)(color.B * 255), (byte)(color.A * 255));
            } else {
                return String.Format("#{0:x2}{1:x2}{2:x2}", (byte)(color.R * 255), (byte)(color.G * 255),
                    (byte)(color.B * 255));
            }
        }

        public static void RoundedRectangle(Cairo.Context cr, double x, double y, double w, double h, double r)
        {
            RoundedRectangle(cr, x, y, w, h, r, CairoCorners.All, false);
        }

        public static void RoundedRectangle(Cairo.Context cr, double x, double y, double w, double h,
            double r, CairoCorners corners)
        {
            RoundedRectangle(cr, x, y, w, h, r, corners, false);
        }

        public static void RoundedRectangle(Cairo.Context cr, double x, double y, double w, double h,
            double r, CairoCorners corners, bool topBottomFallsThrough)
        {
            if(topBottomFallsThrough && corners == CairoCorners.None) {
                cr.MoveTo(x, y - r);
                cr.LineTo(x, y + h + r);
                cr.MoveTo(x + w, y - r);
                cr.LineTo(x + w, y + h + r);
                return;
            } else if(r < 0.0001 || corners == CairoCorners.None) {
                cr.Rectangle(x, y, w, h);
                return;
            }

            if((corners & (CairoCorners.TopLeft | CairoCorners.TopRight)) == 0 && topBottomFallsThrough) {
                y -= r;
                h += r;
                cr.MoveTo(x + w, y);
            } else {
                if((corners & CairoCorners.TopLeft) != 0) {
                    cr.MoveTo(x + r, y);
                } else {
                    cr.MoveTo(x, y);
                }

                if((corners & CairoCorners.TopRight) != 0) {
                    cr.Arc(x + w - r, y + r, r, Math.PI * 1.5, Math.PI * 2);
                } else {
                    cr.LineTo(x + w, y);
                }
            }

            if((corners & (CairoCorners.BottomLeft | CairoCorners.BottomRight)) == 0 && topBottomFallsThrough) {
                h += r;
                cr.LineTo(x + w, y + h);
                cr.MoveTo(x, y + h);
                cr.LineTo(x, y + r);
                cr.Arc(x + r, y + r, r, Math.PI, Math.PI * 1.5);
            } else {
                if((corners & CairoCorners.BottomRight) != 0) {
                    cr.Arc(x + w - r, y + h - r, r, 0, Math.PI * 0.5);
                } else {
                    cr.LineTo(x + w, y + h);
                }

                if((corners & CairoCorners.BottomLeft) != 0) {
                    cr.Arc(x + r, y + h - r, r, Math.PI * 0.5, Math.PI);
                } else {
                    cr.LineTo(x, y + h);
                }

                if((corners & CairoCorners.TopLeft) != 0) {
                    cr.Arc(x + r, y + r, r, Math.PI, Math.PI * 1.5);
                } else {
                    cr.LineTo(x, y);
                }
            }
        }

        public static void DisposeContext (Cairo.Context cr)
        {
            ((IDisposable)cr.Target).Dispose ();
            ((IDisposable)cr).Dispose ();
        }

        private struct CairoInteropCall
        {
            public string Name;
            public MethodInfo ManagedMethod;
            public bool CallNative;

            public CairoInteropCall (string name)
            {
                Name = name;
                ManagedMethod = null;
                CallNative = false;
            }
        }

        private static bool CallCairoMethod (Cairo.Context cr, ref CairoInteropCall call)
        {
            if (call.ManagedMethod == null && !call.CallNative) {
                MemberInfo [] members = typeof (Cairo.Context).GetMember (call.Name, MemberTypes.Method,
                    BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public);

                if (members != null && members.Length > 0 && members[0] is MethodInfo) {
                    call.ManagedMethod = (MethodInfo)members[0];
                } else {
                    call.CallNative = true;
                }
            }

            if (call.ManagedMethod != null) {
                call.ManagedMethod.Invoke (cr, null);
                return true;
            }

            return false;
        }

        private static bool native_push_pop_exists = true;

        [DllImport ("libcairo-2.dll")]
        private static extern void cairo_push_group (IntPtr ptr);
        private static CairoInteropCall cairo_push_group_call = new CairoInteropCall ("PushGroup");

        public static void PushGroup (Cairo.Context cr)
        {
            if (!native_push_pop_exists) {
                return;
            }

            try {
                if (!CallCairoMethod (cr, ref cairo_push_group_call)) {
                    cairo_push_group (cr.Handle);
                }
            } catch {
                native_push_pop_exists = false;
            }
        }

        [DllImport ("libcairo-2.dll")]
        private static extern void cairo_pop_group_to_source (IntPtr ptr);
        private static CairoInteropCall cairo_pop_group_to_source_call = new CairoInteropCall ("PopGroupToSource");

        public static void PopGroupToSource (Cairo.Context cr)
        {
            if (!native_push_pop_exists) {
                return;
            }

            try {
                if (!CallCairoMethod (cr, ref cairo_pop_group_to_source_call)) {
                    cairo_pop_group_to_source (cr.Handle);
                }
            } catch (EntryPointNotFoundException) {
                native_push_pop_exists = false;
            }
        }
    }
}
