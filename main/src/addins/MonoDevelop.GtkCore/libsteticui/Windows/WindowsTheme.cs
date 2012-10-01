using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Stetic.Windows
{
	class WindowsTheme
	{
		IntPtr hWnd;
		IntPtr hTheme;

		public WindowsTheme (Gdk.Window win)
		{
			hWnd = gdk_win32_drawable_get_handle (win.Handle);
			hTheme = OpenThemeData (hWnd, "WINDOW");
		}

		public void Dipose ( )
		{
			CloseThemeData (hTheme);
		}

		public void DrawWindowFrame (Gtk.Widget w, string caption, int x, int y, int width, int height)
		{
			Gdk.Drawable drawable = w.GdkWindow;
			Gdk.Pixmap pix = new Gdk.Pixmap (drawable, width, height, drawable.Depth);

			Gdk.GC gcc = new Gdk.GC (pix);
			gcc.Foreground = w.Style.Backgrounds [(int)Gtk.StateType.Normal];
			pix.DrawRectangle (gcc, true, 0, 0, width, height);

			IntPtr hdc = gdk_win32_hdc_get (pix.Handle, gcc.Handle, 0);

			RECT r = new RECT (0, 0, width, height);
			SIZE size;
			GetThemePartSize (hTheme, hdc, WP_CAPTION, CS_ACTIVE, ref r, 1, out size);

			r.Bottom = size.cy;
			DrawThemeBackground (hTheme, hdc, WP_CAPTION, CS_ACTIVE, ref r, ref r);

			RECT rf = new RECT (FrameBorder, FrameBorder, width - FrameBorder * 2, size.cy);
			LOGFONT lf = new LOGFONT ();
			GetThemeSysFont (hTheme, TMT_CAPTIONFONT, ref lf);
			IntPtr titleFont = CreateFontIndirect (ref lf);
			IntPtr oldFont = SelectObject (hdc, titleFont);
			SetBkMode (hdc, 1 /*TRANSPARENT*/);
			DrawThemeText (hTheme, hdc, WP_CAPTION, CS_ACTIVE, caption, -1, DT_LEFT | DT_SINGLELINE | DT_WORD_ELLIPSIS, 0, ref rf);
			SelectObject (hdc, oldFont);
			DeleteObject (titleFont);

			int ny = r.Bottom;
			r = new RECT (0, ny, width, height);
			DrawThemeBackground (hTheme, hdc, WP_FRAMEBOTTOM, 0, ref r, ref r);

			gdk_win32_hdc_release (pix.Handle, gcc.Handle, 0);

			Gdk.Pixbuf img = Gdk.Pixbuf.FromDrawable (pix, pix.Colormap, 0, 0, 0, 0, width, height);
			drawable.DrawPixbuf (new Gdk.GC (drawable), img, 0, 0, x, y, width, height, Gdk.RgbDither.None, 0, 0);
			drawable.DrawRectangle (w.Style.BackgroundGC (Gtk.StateType.Normal), true, x + FrameBorder, y + size.cy, width - FrameBorder * 2, height - FrameBorder - size.cy);
		}

		public Gdk.Rectangle GetWindowClientArea (Gdk.Rectangle allocation)
		{
			IntPtr hdc = GetDC (hWnd);
			RECT r = new RECT (allocation.X, allocation.Y, allocation.X + allocation.Width, allocation.Y + allocation.Height);
			SIZE size;
			GetThemePartSize (hTheme, hdc, WP_CAPTION, CS_ACTIVE, ref r, 1, out size);
			ReleaseDC (hWnd, hdc);

			return new Gdk.Rectangle (allocation.X + FrameBorder, allocation.Y + size.cy, allocation.Width - FrameBorder * 2, allocation.Height - size.cy - FrameBorder);
		}

		const int FrameBorder = 8;

		const int WP_CAPTION = 1;
		const int WP_FRAMEBOTTOM = 9;
		const int CS_ACTIVE = 1;
		const int TMT_CAPTIONFONT = 801;

		const int DT_LEFT = 0x0;
		const int DT_WORD_ELLIPSIS = 0x40000;
		const int DT_SINGLELINE = 0x20;
		

		[DllImport ("uxtheme", ExactSpelling = true)]
		extern static Int32 DrawThemeBackground (IntPtr hTheme, IntPtr hdc, int iPartId,
		   int iStateId, ref RECT pRect, ref RECT pClipRect);

		[DllImport ("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
		static extern IntPtr OpenThemeData (IntPtr hWnd, String classList);

		[DllImport ("uxtheme.dll", ExactSpelling = true)]
		extern static Int32 CloseThemeData (IntPtr hTheme);

		[DllImport ("uxtheme", ExactSpelling = true)]
		extern static Int32 GetThemePartSize (IntPtr hTheme, IntPtr hdc, int part, int state, ref RECT pRect, int eSize, out SIZE size);

		[DllImport ("uxtheme", ExactSpelling = true)]
		extern static Int32 GetThemeBackgroundExtent (IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, ref RECT pBoundingRect, out RECT pContentRect);

		[DllImport ("uxtheme", ExactSpelling = true)]
		extern static Int32 GetThemeMargins (IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, int iPropId, out MARGINS pMargins);

		[DllImport ("uxtheme", ExactSpelling = true, CharSet = CharSet.Unicode)]
		extern static Int32 DrawThemeText (IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, String text, int textLength, UInt32 textFlags, UInt32 textFlags2, ref RECT pRect);

		[DllImport ("uxtheme", ExactSpelling = true, CharSet = CharSet.Unicode)]
		extern static Int32 GetThemeSysFont (IntPtr hTheme, int iFontId, ref LOGFONT plf);

		[DllImport ("gdi32.dll")]
		static extern IntPtr CreateFontIndirect ([In] ref LOGFONT lplf);

		[DllImport ("gdi32.dll")]
		static extern int SetBkMode (IntPtr hdc, int iBkMode);
	
		[DllImport ("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
		static extern IntPtr SelectObject (IntPtr hdc, IntPtr hgdiobj);

		[DllImport ("gdi32.dll")]
		static extern bool DeleteObject (IntPtr hObject);
	
		[DllImport ("user32.dll")]
		static extern IntPtr GetDC (IntPtr hWnd);

		[DllImport ("user32.dll")]
		static extern int ReleaseDC (IntPtr hWnd, IntPtr hDC);

		[DllImport ("libgdk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gdk_win32_drawable_get_handle (IntPtr raw);

		[DllImport ("libgdk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gdk_win32_hdc_get (IntPtr drawable, IntPtr gc, int usage);

		[DllImport ("libgdk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void gdk_win32_hdc_release (IntPtr drawable, IntPtr gc, int usage);
	}


	[Serializable, StructLayout (LayoutKind.Sequential)]
	public struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;

		public RECT (int left_, int top_, int right_, int bottom_)
		{
			Left = left_;
			Top = top_;
			Right = right_;
			Bottom = bottom_;
		}

		public int Height { get { return Bottom - Top; } }
		public int Width { get { return Right - Left; } }
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct SIZE
	{
		public int cx;
		public int cy;

		public SIZE (int cx, int cy)
		{
			this.cx = cx;
			this.cy = cy;
		}
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct MARGINS
	{
		public int leftWidth;
		public int rightWidth;
		public int topHeight;
		public int bottomHeight;
	}

	[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public struct LOGFONT
	{
		public int lfHeight;
		public int lfWidth;
		public int lfEscapement;
		public int lfOrientation;
		public int lfWeight;
		public byte lfItalic;
		public byte lfUnderline;
		public byte lfStrikeOut;
		public byte lfCharSet;
		public byte lfOutPrecision;
		public byte lfClipPrecision;
		public byte lfQuality;
		public byte lfPitchAndFamily;
		[MarshalAs (UnmanagedType.ByValTStr, SizeConst = 32)]
		public string lfFaceName;
	}
}
