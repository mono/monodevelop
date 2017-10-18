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
using MonoDevelop.Core;

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
		internal const string LIBCAIRO = "libcairo-2.dll";
		public static Cairo.Rectangle ToCairoRect (this Gdk.Rectangle rect)
		{
			return new Cairo.Rectangle (rect.X, rect.Y, rect.Width, rect.Height);
		}

        public static Surface CreateSurfaceForPixbuf (Context cr, Pixbuf pixbuf)
        {
			Surface surface;
			using (var t = cr.GetTarget ()) {
				surface = t.CreateSimilar (t.Content, pixbuf.Width, pixbuf.Height);
			}
			using (Context surface_cr = new Context (surface)) {
				CairoHelper.SetSourcePixbuf (surface_cr, pixbuf, 0, 0);
				surface_cr.Paint ();
				surface_cr.Dispose ();
			}
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

		public static Cairo.Color InterpolateColors (Cairo.Color start, Cairo.Color end, float amount)
		{
			return new Cairo.Color (start.R + (end.R - start.R) * amount,
			                        start.G + (end.G - start.G) * amount,
			                        start.B + (end.B - start.B) * amount,
			                        start.A + (end.A - start.A) * amount);
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

        public static string ColorGetHex (Cairo.Color color, bool withAlpha = false)
        {
            if (withAlpha) {
                return String.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", (byte)(color.R * 255), (byte)(color.G * 255),
                    (byte)(color.B * 255), (byte)(color.A * 255));
            } else {
                return String.Format("#{0:x2}{1:x2}{2:x2}", (byte)(color.R * 255), (byte)(color.G * 255),
                    (byte)(color.B * 255));
            }
        }

        public static void RoundedRectangle(this Cairo.Context cr, double x, double y, double w, double h, double r)
        {
            RoundedRectangle(cr, x, y, w, h, r, CairoCorners.All, false);
        }

		public static void RoundedRectangle(this Cairo.Context cr, double x, double y, double w, double h,
            double r, CairoCorners corners)
        {
            RoundedRectangle(cr, x, y, w, h, r, corners, false);
        }

		public static void RoundedRectangle(this Cairo.Context cr, double x, double y, double w, double h,
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

		static void ShadowGradient (Cairo.Gradient lg, double strength)
		{
			lg.AddColorStop (0, new Cairo.Color (0, 0, 0, strength));
			lg.AddColorStop (1.0/6.0, new Cairo.Color (0, 0, 0, .85 * strength));
			lg.AddColorStop (2.0/6.0, new Cairo.Color (0, 0, 0, .54 * strength));
			lg.AddColorStop (3.0/6.0, new Cairo.Color (0, 0, 0, .24 * strength));
			lg.AddColorStop (4.0/6.0, new Cairo.Color (0, 0, 0, .07 * strength));
			lg.AddColorStop (5.0/6.0, new Cairo.Color (0, 0, 0, .01 * strength));
			lg.AddColorStop (1, new Cairo.Color (0, 0, 0, 0));
		}

		// VERY SLOW, only use on cached renders
		public static void RenderOuterShadow (this Cairo.Context self, Gdk.Rectangle area, int size, int rounding, double strength)
		{
			area.Inflate (-1, -1);
			size++;

			int doubleRounding = rounding * 2;
			// left side
			self.Rectangle (area.X - size, area.Y + rounding, size, area.Height - doubleRounding - 1);
			using (var lg = new LinearGradient (area.X, 0, area.X - size, 0)) {
				ShadowGradient (lg, strength);
				self.SetSource (lg);
				self.Fill ();
			}

			// right side
			self.Rectangle (area.Right, area.Y + rounding, size, area.Height - doubleRounding - 1);
			using (var lg = new LinearGradient (area.Right, 0, area.Right + size, 0)) {
				ShadowGradient (lg, strength);
				self.SetSource (lg);
				self.Fill ();
			}

			// top side
			self.Rectangle (area.X + rounding, area.Y - size, area.Width - doubleRounding - 1, size);
			using (var lg = new LinearGradient (0, area.Y, 0, area.Y - size)) {
				ShadowGradient (lg, strength);
				self.SetSource (lg);
				self.Fill ();
			}

			// bottom side
			self.Rectangle (area.X + rounding, area.Bottom, area.Width - doubleRounding - 1, size);
			using (var lg = new LinearGradient (0, area.Bottom, 0, area.Bottom + size)) {
				ShadowGradient (lg, strength);
				self.SetSource (lg);
				self.Fill ();
			}

			// top left corner
			self.Rectangle (area.X - size, area.Y - size, size + rounding, size + rounding);
			using (var rg = new RadialGradient (area.X + rounding, area.Y + rounding, rounding, area.X + rounding, area.Y + rounding, size + rounding)) {
				ShadowGradient (rg, strength);
				self.SetSource (rg);
				self.Fill ();
			}

			// top right corner
			self.Rectangle (area.Right - rounding, area.Y - size, size + rounding, size + rounding);
			using (var rg = new RadialGradient (area.Right - rounding, area.Y + rounding, rounding, area.Right - rounding, area.Y + rounding, size + rounding)) {
				ShadowGradient (rg, strength);
				self.SetSource (rg);
				self.Fill ();
			}

			// bottom left corner
			self.Rectangle (area.X - size, area.Bottom - rounding, size + rounding, size + rounding);
			using (var rg = new RadialGradient (area.X + rounding, area.Bottom - rounding, rounding, area.X + rounding, area.Bottom - rounding, size + rounding)) {
				ShadowGradient (rg, strength);
				self.SetSource (rg);
				self.Fill ();
			}

			// bottom right corner
			self.Rectangle (area.Right - rounding, area.Bottom - rounding, size + rounding, size + rounding);
			using (var rg = new RadialGradient (area.Right - rounding, area.Bottom - rounding, rounding, area.Right - rounding, area.Bottom - rounding, size + rounding)) {
				ShadowGradient (rg, strength);
				self.SetSource (rg);
				self.Fill ();
			}
		}

		[DllImport (LIBCAIRO, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr cairo_pattern_set_extend(IntPtr pattern, CairoExtend extend);

		[DllImport (LIBCAIRO, CallingConvention=CallingConvention.Cdecl)]
		internal static extern IntPtr cairo_get_source (IntPtr cr);

		enum CairoExtend {
			CAIRO_EXTEND_NONE,
			CAIRO_EXTEND_REPEAT,
			CAIRO_EXTEND_REFLECT,
			CAIRO_EXTEND_PAD
		}

		public static void RenderTiled (this Cairo.Context self, Gtk.Widget target, Xwt.Drawing.Image source, Gdk.Rectangle area, Gdk.Rectangle clip, double opacity = 1)
		{
			var ctx = Xwt.Toolkit.CurrentEngine.WrapContext (target, self);
			ctx.Save ();
			ctx.Rectangle (clip.X, clip.Y, clip.Width, clip.Height);
			ctx.Clip ();
			ctx.Pattern = new Xwt.Drawing.ImagePattern (source);
			ctx.Rectangle (area.X, area.Y, area.Width, area.Height);
			ctx.Fill ();
			ctx.Restore ();
		}

        public static void DisposeContext (Cairo.Context cr)
        {
            cr.Dispose ();
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

		[DllImport (LIBCAIRO, CallingConvention = CallingConvention.Cdecl)]
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

		[DllImport (LIBCAIRO, CallingConvention = CallingConvention.Cdecl)]
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

		public static Cairo.Color ParseColor (string s, double alpha = 1)
		{
			if (s.StartsWith ("#"))
				s = s.Substring (1);
			if (s.Length == 3)
				s = "" + s[0]+s[0]+s[1]+s[1]+s[2]+s[2];
			double r = ((double) int.Parse (s.Substring (0,2), System.Globalization.NumberStyles.HexNumber)) / 255;
			double g = ((double) int.Parse (s.Substring (2,2), System.Globalization.NumberStyles.HexNumber)) / 255;
			double b = ((double) int.Parse (s.Substring (4,2), System.Globalization.NumberStyles.HexNumber)) / 255;
			return new Cairo.Color (r, g, b, alpha);
		}

		public static ImageSurface LoadImage (Assembly assembly, string resource)
		{
			byte[] buffer;
			using (var stream = assembly.GetManifestResourceStream (resource)) {
				buffer = new byte [stream.Length];
				stream.Read (buffer, 0, (int)stream.Length);
			}
/* This should work, but doesn't:
			using (var px = new Gdk.Pixbuf (buffer)) 
				return new ImageSurface (px.Pixels, Format.Argb32, px.Width, px.Height, px.Rowstride);*/

			// Workaround: loading from file name.
			var tmp = System.IO.Path.GetTempFileName ();
			System.IO.File.WriteAllBytes (tmp, buffer);
			var img = new ImageSurface (tmp);
			try {
				System.IO.File.Delete (tmp);
			} catch (Exception e) {
				LoggingService.LogWarning ($"Unable to delete {tmp} due to exception {e}");

				// Only want to dispose when the Delete failed
				img.Dispose ();
				throw;
			}
			return img;
		}

		public static Cairo.Color WithAlpha (Cairo.Color c, double alpha)
		{
			return new Cairo.Color (c.R, c.G, c.B, alpha);
		}

		public static Cairo.Color MultiplyAlpha (this Cairo.Color self, double alpha)
		{
			return new Cairo.Color (self.R, self.G, self.B, self.A * alpha);
		}

		public static void CachedDraw (this Cairo.Context self, ref SurfaceWrapper surface, Gdk.Point position, Gdk.Size size, 
		                               object parameters = null, float opacity = 1.0f, Action<Cairo.Context, float> draw = null, double? forceScale = null)
		{
			self.CachedDraw (ref surface, new Gdk.Rectangle (position, size), parameters, opacity, draw, forceScale);
		}

		public static void CachedDraw (this Cairo.Context self, ref SurfaceWrapper surface, Gdk.Rectangle region, 
		                               object parameters = null, float opacity = 1.0f, Action<Cairo.Context, float> draw = null, double? forceScale = null)
		{
			double displayScale = forceScale.HasValue ? forceScale.Value : QuartzSurface.GetRetinaScale (self);
			int targetWidth = (int) (region.Width * displayScale);
			int targetHeight = (int) (region.Height * displayScale);

			bool redraw = false;
			if (surface == null || surface.Width != targetWidth || surface.Height != targetHeight) {
				if (surface != null)
					surface.Dispose ();
				surface = new SurfaceWrapper (self, targetWidth, targetHeight);
				redraw = true;
			} else if ((surface.Data == null && parameters != null) || (surface.Data != null && !surface.Data.Equals (parameters))) {
				redraw = true;
			}


			if (redraw) {
				surface.Data = parameters;
				using (var context = new Cairo.Context (surface.Surface)) {
					context.Operator = Operator.Clear;
					context.Paint();
					context.Operator = Operator.Over;
					context.Save ();
					context.Scale (displayScale, displayScale);
					draw(context, 1.0f);
					context.Restore ();
				}
			}

			self.Save ();
			self.Translate (region.X, region.Y);
			self.Scale (1 / displayScale, 1 / displayScale);
			self.SetSourceSurface (surface.Surface, 0, 0);
			self.PaintWithAlpha (opacity);
			self.Restore ();
		}
	}

	public class SurfaceWrapper : IDisposable
	{
		public Cairo.Surface Surface { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }
		public object Data { get; set; }

		public SurfaceWrapper (Cairo.Context similar, int width, int height)
		{
			if (Platform.IsMac) {
				Surface = new QuartzSurface (Cairo.Format.ARGB32, width, height);
			} else if (Platform.IsWindows) {
				using (var target = similar.GetTarget ()) {
					Surface = target.CreateSimilar (Cairo.Content.ColorAlpha, width, height);
				}
			} else {
				Surface = new ImageSurface (Cairo.Format.ARGB32, width, height);
			}
			Width = width;
			Height = height;
		}

		public SurfaceWrapper (Cairo.Context similar, Gdk.Pixbuf source)
		{
			Cairo.Surface surface;
			// There is a bug in Cairo for OSX right now that prevents creating additional accellerated surfaces.
			if (Platform.IsMac) {
				surface = new QuartzSurface (Format.ARGB32, source.Width, source.Height);
			} else if (Platform.IsWindows) {
				using (var t = similar.GetTarget ()) {
					surface = t.CreateSimilar (Content.ColorAlpha, source.Width, source.Height);
				}
			} else {
				surface = new ImageSurface (Format.ARGB32, source.Width, source.Height);
			}

			using (Context context = new Context (surface)) {
				Gdk.CairoHelper.SetSourcePixbuf (context, source, 0, 0);
				context.Paint ();
			}

			Surface = surface;
			Width = source.Width;
			Height = source.Height;
		}

		public void Dispose ()
		{
			if (Surface != null) {
				((IDisposable)Surface).Dispose ();
			}
		}
	}

	public class QuartzSurface : Cairo.Surface
	{
		const string CoreGraphics = "/System/Library/Frameworks/ApplicationServices.framework/Frameworks/CoreGraphics.framework/CoreGraphics";

		[DllImport (CairoExtensions.LIBCAIRO, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr cairo_quartz_surface_create (Cairo.Format format, uint width, uint height);

		[DllImport (CairoExtensions.LIBCAIRO, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr cairo_quartz_surface_get_cg_context (IntPtr surface);

		[DllImport (CairoExtensions.LIBCAIRO, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr cairo_get_target (IntPtr context);

		[DllImport (CoreGraphics, EntryPoint="CGContextConvertRectToDeviceSpace", CallingConvention = CallingConvention.Cdecl)]
		static extern CGRect32 CGContextConvertRectToDeviceSpace32 (IntPtr contextRef, CGRect32 cgrect);

		[DllImport (CoreGraphics, EntryPoint="CGContextConvertRectToDeviceSpace", CallingConvention = CallingConvention.Cdecl)]
		static extern CGRect64 CGContextConvertRectToDeviceSpace64 (IntPtr contextRef, CGRect64 cgrect);

		public static double GetRetinaScale (Cairo.Context context)  {
			if (!Platform.IsMac)
				return 1;

			// Use C call to avoid dispose bug in cairo bindings for OSX
			var cgContext = cairo_quartz_surface_get_cg_context (cairo_get_target (context.Handle));

			if (IntPtr.Size == 8)
				return CGContextConvertRectToDeviceSpace64 (cgContext, CGRect64.Unit).X;

			return CGContextConvertRectToDeviceSpace32 (cgContext, CGRect32.Unit).X;
		}

		struct CGRect32
		{
			public CGRect32 (float x, float y, float width, float height)
			{
				this.X = x;
				this.Y = y;
				this.Width = width;
				this.Height = height;
			}

			public float X, Y, Width, Height;

			public static CGRect32 Unit {
				get {
					return new CGRect32 (1, 1, 1, 1);
				}
			}
		}

		struct CGRect64
		{
			public CGRect64 (double x, double y, double width, double height)
			{
				this.X = x;
				this.Y = y;
				this.Width = width;
				this.Height = height;
			}

			public double X, Y, Width, Height;

			public static CGRect64 Unit {
				get {
					return new CGRect64 (1, 1, 1, 1);
				}
			}
		}

		public QuartzSurface (Cairo.Format format, int width, int height)
			: base (cairo_quartz_surface_create (format, (uint)width, (uint)height), true)
		{
		}
	}
}
