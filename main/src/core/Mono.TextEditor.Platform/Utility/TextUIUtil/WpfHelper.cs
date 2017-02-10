// Copyright (c) Microsoft Corporation
// All rights reserved

#pragma warning disable 1634, 1691

namespace Microsoft.VisualStudio.Text.Utilities
{
    using Microsoft.Win32.SafeHandles;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;

    /// <summary>
    /// Helpful utilities related to WPF and the windows platform, including input method editor support.
    /// </summary>
    public static class WpfHelper
    {
        public const int WM_IME_STARTCOMPOSITION = 0x010D;
        public const int WM_IME_ENDCOMPOSITION = 0x010E;
        public const int WM_IME_COMPOSITION = 0x010F;
        public const int WM_IME_SETCONTEXT = 0x0281;
        public const int WM_IME_NOTIFY = 0x0282;
        public const int WM_IME_CONTROL = 0x0283;
        public const int WM_IME_COMPOSITIONFULL = 0x0284;
        public const int WM_IME_SELECT = 0x0285;
        public const int WM_IME_CHAR = 0x0286;
        public const int WM_IME_REQUEST = 0x0288;
        public const int WM_IME_KEYDOWN = 0x0290;

        public const int WM_KEYDOWN = 0x0100;

        public const int GCS_COMPSTR = 0x0008;
        public const int GCS_RESULTSTR = 0x0800;

        public const int VK_HANJA = 0x19;

        public const int IMR_RECONVERTSTRING = 0x0004;
        public const int IMR_CONFIRMRECONVERTSTRING = 0x0005;

        public const int LCID_KOREAN = 1042;

        [ThreadStatic]	//Unit tests run each test in its own thread and we can't reused thread managers across threads.
        static NativeMethods.ITfThreadMgr _threadMgr;

        [ThreadStatic]
        static bool _threadMgrFailed = false;

        public static readonly double DeviceScaleX;
        public static readonly double DeviceScaleY;

        static WpfHelper()
        {
            //Get the device DPI (which can only be changed via a restart so we
            //can save the result in a static).
            IntPtr dc = NativeMethods.GetDC(IntPtr.Zero);
            if (dc != IntPtr.Zero)
            {
                const double LogicalDpi = 96.0;
                DeviceScaleX = LogicalDpi / NativeMethods.GetDeviceCaps(dc, NativeMethods.LOGPIXELSX);
                DeviceScaleY = LogicalDpi / NativeMethods.GetDeviceCaps(dc, NativeMethods.LOGPIXELSY);

                NativeMethods.ReleaseDC(IntPtr.Zero, dc);
            }
            else
            {
                DeviceScaleX = 1.0;
                DeviceScaleY = 1.0;
            }
        }

#if false
        /// <summary>
        /// Given a point relative to a visual, gets the Screen co-ordinates
        /// </summary>
        public static Point GetScreenCoordinates(Point point, Visual relativeTo)
        {
            // Validate
            if (relativeTo == null)
                throw new ArgumentNullException("relativeTo");

            Visual root = GetRootVisual(relativeTo);
            Point rootTranslatedPoint = relativeTo.TransformToAncestor(root).Transform(point);

            // Get the Hwnd for this visual
            HwndSource hwndSource = GetHwndSource(relativeTo);
            if (hwndSource != null)
            {
                NativeMethods.POINT pt = new NativeMethods.POINT();
                pt.x = (int)rootTranslatedPoint.X;
                pt.y = (int)rootTranslatedPoint.Y;
                NativeMethods.ClientToScreen(hwndSource.Handle, ref pt);
                return new Point(pt.x, pt.y);
            }

            return rootTranslatedPoint;
        }

        /// <summary>
        /// Gets the screen rectangle that contains the point relative to the given visual
        /// </summary>
        public static Rect GetScreenRect(Point pt, Visual relativeTo)
        {
            // Validate
            if (relativeTo == null)
                throw new ArgumentNullException("relativeTo");

            Point screenCoordinates = GetScreenCoordinates(pt, relativeTo);
            NativeMethods.POINT screenPoint = new NativeMethods.POINT();
            screenPoint.x = (int)screenCoordinates.X;
            screenPoint.y = (int)screenCoordinates.Y;
            IntPtr monitor = NativeMethods.MonitorFromPoint(screenPoint, NativeMethods.MONITOR_DEFAULTTONEAREST);

            NativeMethods.MONITORINFO monitorInfo = new NativeMethods.MONITORINFO();
            monitorInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(monitorInfo);
            if (NativeMethods.GetMonitorInfo(monitor, ref monitorInfo))
                return new Rect(new Point(monitorInfo.rcWork.left, monitorInfo.rcWork.top), new Point(monitorInfo.rcWork.right, monitorInfo.rcWork.bottom));
            else
                return SystemParameters.WorkArea;
        }
#endif
        /// <summary>
        /// Gets the screen rectangle that contains given screen point.
        /// </summary>
        public static Rect GetScreenRect(Point screenCoordinates)
        {
            NativeMethods.POINT screenPoint = new NativeMethods.POINT();
            screenPoint.x = (int)screenCoordinates.X;
            screenPoint.y = (int)screenCoordinates.Y;
            IntPtr monitor = NativeMethods.MonitorFromPoint(screenPoint, NativeMethods.MONITOR_DEFAULTTONEAREST);

            NativeMethods.MONITORINFO monitorInfo = new NativeMethods.MONITORINFO();
            monitorInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(monitorInfo);
            if (NativeMethods.GetMonitorInfo(monitor, ref monitorInfo))
                return new Rect(new Point(monitorInfo.rcWork.left, monitorInfo.rcWork.top), new Point(monitorInfo.rcWork.right, monitorInfo.rcWork.bottom));
            else
                return SystemParameters.WorkArea;
        }

        /// <summary>
        /// Get the Root of the ancestral hierarchy for the given WPF visual.
        /// </summary>
        public static Visual GetRootVisual(Visual visual)
        {
            // Validate
            if (visual == null)
                throw new ArgumentNullException("visual");

            DependencyObject current = visual;
            Visual root = visual;

            while ((current = VisualTreeHelper.GetParent(current)) != null)
            {
                Visual v = current as Visual;
                if (v != null)
                    root = v;
            }

            return root;
        }

        /// <summary>
        /// Determine whether two brushes are equal.
        /// </summary>
        /// <param name="brush">The first brush.</param>
        /// <param name="other">The second brush.</param>
        /// <returns><c>true</c> if the two are equal, <c>false</c> otherwise.</returns>
        /// <remarks>internal for testability</remarks>
        public static bool BrushesEqual(Brush brush, Brush other)
        {
            if (brush == null || other == null)
            {
                return object.ReferenceEquals(brush, other);
            }
            else
            {
                if (brush.Opacity == 0 && other.Opacity == 0)
                    return true;

                SolidColorBrush colorBrush1 = brush as SolidColorBrush;
                SolidColorBrush colorBrush2 = other as SolidColorBrush;

                // If both brushes are SolidColorBrushes check the color of each
                if (colorBrush1 != null && colorBrush2 != null)
                {
                    if (colorBrush1.Color.A == 0 && colorBrush2.Color.A == 0)
                        return true;

                    return colorBrush1.Color == colorBrush2.Color &&
                           Math.Abs(colorBrush1.Opacity - colorBrush2.Opacity) < 0.01;
                }

                // as a last resort try brush.Equals (which pretty much is the equivalent of returning false here since
                // it doesn't compare any of the core properties of brushes)
                return brush.Equals(other);
            }
        }

        public static bool TypefacesEqual(Typeface typeface, Typeface other)
        {
            if (typeface == null)
                return (other == null);
            else
                return typeface.Equals(other);  // .Equals is crucial here! We won't see reference equality.
        }

        public static string GetImmCompositionString(IntPtr immContext, int dwIndex)
        {
            if (immContext == IntPtr.Zero)
                return null;

            // get buffer size
            int size = NativeMethods.ImmGetCompositionStringW(immContext, dwIndex, null, 0);
            if (size < 0)
            {
                return null;
            }

            //Get the string in an appropriately sized buffer
            StringBuilder result = new StringBuilder(size / 2);   //We get the size in bytes.
            size = NativeMethods.ImmGetCompositionStringW(immContext, dwIndex, result, size);
            if (size < 0)
            {
                Debug.Assert(false); //This should never happen? why did we succeed the first time?

                //But handle it gracefully.
                return null;
            }

            return result.ToString().Substring(0, size / 2);
        }

        public static void FlushImmCompositionString(IntPtr immContext)
        {
            const int GCS_COMPREADSTR = 0x0001;
            const int SCS_SETSTR = (GCS_COMPREADSTR | GCS_COMPSTR);

            if (immContext != IntPtr.Zero)
            {
                StringBuilder empty = new StringBuilder();
                NativeMethods.ImmSetCompositionStringW(immContext, SCS_SETSTR, empty, 0, empty, 0);
            }
        }

        public static bool ImmNotifyIME(IntPtr immContext, int dwAction, int dwIndex, int dwValue)
        {
            return NativeMethods.ImmNotifyIME(immContext, dwAction, dwIndex, dwValue);
        }

        public static bool HanjaConversion(IntPtr context, IntPtr keyboardLayout, char selection)
        {
            const int IME_ESC_HANJA_MODE = 0x1008;

            if (context != IntPtr.Zero)
            {
                IntPtr charsPtr = Marshal.StringToHGlobalUni(new string(selection, 1));

                IntPtr hr = NativeMethods.ImmEscapeW(keyboardLayout, context, IME_ESC_HANJA_MODE, charsPtr);

                // Free the allocated memory
                Marshal.FreeHGlobal(charsPtr);

                if (hr != IntPtr.Zero)
                    return true;
            }

            return false;
        }

        public static void SetNoTopmost(Visual visual)
        {
            if (visual != null)
            {
                HwndSource source = PresentationSource.FromVisual(visual) as HwndSource;
                if (source != null)
                {
                    const int SWP_NOMOVE = 0x02;
                    const int SWP_NOSIZE = 0x01;
                    const int SWP_NOACTIVATE = 0x10;
                    const int HWND_NOTOPMOST = -2;
                    NativeMethods.SetWindowPos(source.Handle, (IntPtr)HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
            }
        }

        private static SnapshotSpan GetSelectionContext(SnapshotSpan selection)
        {
            const int padding = 20; //Consistent with Win7 code.
            SnapshotPoint start = new SnapshotPoint(selection.Snapshot,
                                                    Math.Max(0, selection.Start.Position - padding));
            SnapshotPoint end = new SnapshotPoint(selection.Snapshot,
                                                  Math.Min(selection.Snapshot.Length, selection.End.Position + padding));
            return new SnapshotSpan(start, end);

        }

        /// <summary>
        /// Size or fill-in a RECONVERTSTRING structure that contains the selection (with padding)
        /// </summary>
        public static IntPtr ReconvertString(IntPtr lParam, SnapshotSpan selection)
        {
            SnapshotSpan selectionContext = GetSelectionContext(selection);
            int sizeofRCS = Marshal.SizeOf(typeof(NativeMethods.RECONVERTSTRING));

            if (lParam != IntPtr.Zero)
            {
                NativeMethods.RECONVERTSTRING reconvertString = (NativeMethods.RECONVERTSTRING)(Marshal.PtrToStructure(lParam, typeof(NativeMethods.RECONVERTSTRING)));

                if (selection.Length >= ((reconvertString.dwSize - sizeofRCS) / 2))
                {
                    //We didn't get space for all the characters we requested.
                    return IntPtr.Zero;
                }

                Marshal.WriteInt32((IntPtr)((long)lParam + (long)Marshal.OffsetOf(typeof(NativeMethods.RECONVERTSTRING), "dwStrLen")), selectionContext.Length);
                Marshal.WriteInt32((IntPtr)((long)lParam + (long)Marshal.OffsetOf(typeof(NativeMethods.RECONVERTSTRING), "dwStrOffset")), sizeofRCS);

                Marshal.WriteInt32((IntPtr)((long)lParam + (long)Marshal.OffsetOf(typeof(NativeMethods.RECONVERTSTRING), "dwCompStrLen")), selection.Length);
                Marshal.WriteInt32((IntPtr)((long)lParam + (long)Marshal.OffsetOf(typeof(NativeMethods.RECONVERTSTRING), "dwCompStrOffset")), (selection.Start.Position - selectionContext.Start.Position) * 2);

                Marshal.WriteInt32((IntPtr)((long)lParam + (long)Marshal.OffsetOf(typeof(NativeMethods.RECONVERTSTRING), "dwTargetStrLen")), selection.Length);
                Marshal.WriteInt32((IntPtr)((long)lParam + (long)Marshal.OffsetOf(typeof(NativeMethods.RECONVERTSTRING), "dwTargetStrOffset")), (selection.Start.Position - selectionContext.Start.Position) * 2);

                Marshal.Copy(selection.Snapshot.GetText(selectionContext).ToCharArray(), 0, (IntPtr)((long)lParam + (long)sizeofRCS), selectionContext.Length);
                Marshal.WriteInt16((IntPtr)((long)lParam + (long)(sizeofRCS + (selectionContext.Length * 2))), 0);
            }

            return new IntPtr(sizeofRCS + ((selectionContext.Length + 1) * 2));
        }

        /// <summary>
        /// Generate a selection from a RECONVERTSTRING block.
        /// </summary>
        public static SnapshotSpan ConfirmReconvertString(IntPtr lParam, SnapshotSpan selection)
        {
            if (lParam != IntPtr.Zero)
            {
                SnapshotSpan selectionContext = GetSelectionContext(selection);

                NativeMethods.RECONVERTSTRING reconvertString = (NativeMethods.RECONVERTSTRING)(Marshal.PtrToStructure(lParam, typeof(NativeMethods.RECONVERTSTRING)));

                return new SnapshotSpan(selectionContext.Start + (reconvertString.dwCompStrOffset / 2), reconvertString.dwCompStrLen);
            }

            return new SnapshotSpan(selection.Snapshot, 0, 0);
        }

        private static class CompositionFontMapper
        {
            private static IDictionary<int, LanguageFontMapping> _languageMap = new Dictionary<int, LanguageFontMapping>(9);
            private static IDictionary<string, FontSizeMapping> _fontMap = new Dictionary<string, FontSizeMapping>(25);
            private static IDictionary<string, FontSizeMapping> _consolasFontMap = new Dictionary<string, FontSizeMapping>(6);
            private static IDictionary<string, FontSizeMapping> _courierNewFontMap = new Dictionary<string, FontSizeMapping>(6);

            static CompositionFontMapper()
            {
                LanguageFontMapping simplifiedChinese = new LanguageFontMapping("SimSun", "Microsoft YaHei");
                LanguageFontMapping traditionalChinese = new LanguageFontMapping("MingLiU", "Microsoft JhengHei");
                LanguageFontMapping japanese = new LanguageFontMapping("MS Gothic", "Meiryo");

                _languageMap.Add(0x0004, simplifiedChinese);         //zh-CHS    Chinese-China (Simplified)
                _languageMap.Add(0x0804, simplifiedChinese);         //zh-CN     Chinese-China
                _languageMap.Add(0x1004, simplifiedChinese);         //zh-SG     Chinese-Singapore

                _languageMap.Add(0x7c04, traditionalChinese);        //zh-CHT    Chinese-China (Traditional)
                _languageMap.Add(0x0c04, traditionalChinese);        //zh-HK     Chinese-Hong Kong SAR
                _languageMap.Add(0x1404, traditionalChinese);        //zh-MO     Chinese-Macau
                _languageMap.Add(0x0404, traditionalChinese);        //zh-TW     Chinese-Taiwan

                _languageMap.Add(0x0011, japanese);                  //ja        Japanese
                _languageMap.Add(0x0411, japanese);                  //ja-JP     Japanese - Japan

                //This is a map of conversions from a Consolas base font to the specified composition font used in the composition window
                _consolasFontMap.Add("SimSun", new FontSizeMapping(-2.0, 2.0, -2.0));
                _consolasFontMap.Add("SimSun-ExtB", new FontSizeMapping(-2.0, 2.0, -2.0));
                _consolasFontMap.Add("Microsoft YaHei", new FontSizeMapping(1.0, 2.0, 2.0));
                _consolasFontMap.Add("MingLiU", new FontSizeMapping(-2.0, 2.0, -3.0));
                _consolasFontMap.Add("Microsoft JhengHei", new FontSizeMapping(2.0, 2.0, 3.0));
                _consolasFontMap.Add("MS Gothic", new FontSizeMapping(-1.0, 2.0, -2.0));
                _consolasFontMap.Add("Meiryo", new FontSizeMapping(1.0, 2.0, 4.0));

                //This is a map of conversions from a Courier New base font to the specified composition font used in the composition window
                _courierNewFontMap.Add("SimSun", new FontSizeMapping(-2.0, 2.0, -3.0));
                _courierNewFontMap.Add("SimSun-ExtB", new FontSizeMapping(-2.0, 2.0, -3.0));
                _courierNewFontMap.Add("Microsoft YaHei", new FontSizeMapping(1.0, 2.0, 2.0));
                _courierNewFontMap.Add("MingLiU", new FontSizeMapping(-2.0, 2.0, -4.0));
                _courierNewFontMap.Add("Microsoft JhengHei", new FontSizeMapping(2.0, 2.0, 2.0));
                _courierNewFontMap.Add("MS Gothic", new FontSizeMapping(-1.0, 2.0, -3.0));
                _courierNewFontMap.Add("Meiryo", new FontSizeMapping(2.0, 2.0, 4.0));

                //This is a map of conversion factors intended for use when the same font is used as the base font and in the composition window
                //but we'll use it whenever the base font isn't Consola or Courier New.
                _fontMap.Add("MS Gothic", new FontSizeMapping(0.0, 2.0, 0.0));
                _fontMap.Add("MS PGothic", new FontSizeMapping(0.0, 2.0, 0.0));
                _fontMap.Add("MS UI Gothic", new FontSizeMapping(0.0, 2.0, 0.0));
                _fontMap.Add("Meiryo", new FontSizeMapping(0.0, 2.0, 0.0));
                _fontMap.Add("Arial Unicode MS", new FontSizeMapping(0.0, 2.0, 0.0));
                _fontMap.Add("MS Mincho", new FontSizeMapping(0.0, 2.0, 0.0));
                _fontMap.Add("MS PMincho", new FontSizeMapping(0.0, 2.0, 0.0));

                _fontMap.Add("Dotum", new FontSizeMapping(0.0, 2.0, 1.0));
                _fontMap.Add("DotumChe", new FontSizeMapping(0.0, 2.0, 1.0));
                _fontMap.Add("Malgun Gothic", new FontSizeMapping(1.0, 2.0, 0.0));
                _fontMap.Add("Batang", new FontSizeMapping(0.0, 2.0, -1.0));
                _fontMap.Add("BatangChe", new FontSizeMapping(0.0, 2.0, -1.0));
                _fontMap.Add("Gulim", new FontSizeMapping(-1.0, 2.0, -1.0));
                _fontMap.Add("GulimChe", new FontSizeMapping(-1.0, 2.0, -1.0));
                _fontMap.Add("Gungsuh", new FontSizeMapping(-1.0, 2.0, -1.0));
                _fontMap.Add("GungsuhChe", new FontSizeMapping(-1.0, 2.0, -1.0));

                _fontMap.Add("SimSun", new FontSizeMapping(-1.0, 2.0, -2.0));
                _fontMap.Add("SimSun-ExtB", new FontSizeMapping(-1.0, 2.0, -2.0));
                _fontMap.Add("NSimSun", new FontSizeMapping(-1.0, 2.0, -2.0));
                _fontMap.Add("Microsoft YaHei", new FontSizeMapping(-1.0, 2.0, 0.0));
                _fontMap.Add("SimHei", new FontSizeMapping(-1.0, 2.0, -2.0));
                _fontMap.Add("KaiTi", new FontSizeMapping(-1.0, 2.0, -1.0));
                _fontMap.Add("FangSong", new FontSizeMapping(-1.0, 2.0, -2.0));

                _fontMap.Add("MingLiU", new FontSizeMapping(-2.0, 1.0, -3.0));
                _fontMap.Add("PMingLiU", new FontSizeMapping(-2.0, 1.0, -3.0));
                _fontMap.Add("Microsoft JhengHei", new FontSizeMapping(-1.0, 2.0, 0.0));
            }

            public static string GetCompositionFont(string baseFont)
            {
                if (_fontMap.ContainsKey(baseFont))
                    return baseFont;

                int lcid = InputLanguageManager.Current.CurrentInputLanguage.LCID;
                LanguageFontMapping mapping;
                if (_languageMap.TryGetValue(lcid, out mapping))
                {
                    return mapping.GetCompositionFont(Environment.OSVersion.Version.Major);
                }

                return string.Empty;
            }

            public static void GetSizeAdjustments(string baseFont, string compositionFont, out double topPadding, out double bottomPadding, out double heightPadding)
            {
                IDictionary<string, FontSizeMapping> map;
                if (baseFont == "Consolas")
                    map = _consolasFontMap;
                else if (baseFont == "Courier New")
                    map = _courierNewFontMap;
                else
                    map = _fontMap;

                FontSizeMapping mapping;
                if (map.TryGetValue(compositionFont, out mapping))
                {
                    topPadding = mapping.TopPadding;
                    bottomPadding = mapping.BottomPadding;
                    heightPadding = mapping.HeightPadding;
                }
                else
                {
                    topPadding = 0.0;
                    bottomPadding = 2.0;
                    heightPadding = -2.0;
                }
            }

            private struct FontSizeMapping
            {
                public readonly double TopPadding;
                public readonly double BottomPadding;
                public readonly double HeightPadding;

                public FontSizeMapping(double topPadding, double bottomPadding, double unadjustedHeightPadding)
                {
                    this.TopPadding = topPadding;
                    this.BottomPadding = bottomPadding;
                    this.HeightPadding = unadjustedHeightPadding - (topPadding + bottomPadding);
                }
            }

            private class LanguageFontMapping
            {
                public readonly string OldFallbackFont;
                public readonly string NewFallbackFont;

                public LanguageFontMapping(string oldFallbackFont, string newFallbackFont)
                {
                    this.OldFallbackFont = oldFallbackFont;
                    this.NewFallbackFont = newFallbackFont;
                }

                public string GetCompositionFont(int majorVersion)
                {
                    return (majorVersion >= 6) ? this.NewFallbackFont : this.OldFallbackFont;
                }
            }
        }

        /// <summary>
        /// Set the composition window position.
        /// </summary>
        public static bool SetCompositionPositionAndHeight(HwndSource source, IntPtr immContext, string baseFont,
                                                           string compositionFont, double topPaddingOverride, double bottomPaddingOverride, double heightPaddingOverride,
                                                           Point compositionTopLeft, double textHeight,
                                                           Visual relativeTo, Point viewTopLeft, Point viewBottomRight)
        {
            if (immContext == IntPtr.Zero)
                throw new ArgumentNullException("immContext");
            if (relativeTo == null)
                throw new ArgumentNullException("relativeTo");

            Visual rootVisual = GetRootVisual(relativeTo);
            if (rootVisual == null)
                return false;

            // Get the point relative to Root Visual
            GeneralTransform rootTransform = relativeTo.TransformToAncestor(rootVisual);

            if (string.IsNullOrEmpty(compositionFont))
                compositionFont = CompositionFontMapper.GetCompositionFont(baseFont);
                
            double topPadding;
            double bottomPadding;
            double heightPadding;
            CompositionFontMapper.GetSizeAdjustments(baseFont, compositionFont, out topPadding, out bottomPadding, out heightPadding);

            topPadding = double.IsNaN(topPaddingOverride) ? topPadding : topPaddingOverride;
            bottomPadding = double.IsNaN(bottomPaddingOverride) ? bottomPadding : bottomPaddingOverride;
            heightPadding = double.IsNaN(heightPaddingOverride) ? heightPadding : heightPaddingOverride;

            //There is no way to transform a distance, so get the point that corresponds to the bottom left of the desired
            //composition window and transform that (and do it before we transform the coordinates of the top left).
            Point compositionBottomLeft = rootTransform.Transform(new Point(compositionTopLeft.X, compositionTopLeft.Y + textHeight + bottomPadding));
            compositionTopLeft = rootTransform.Transform(new Point(compositionTopLeft.X, compositionTopLeft.Y - topPadding));

            viewTopLeft = rootTransform.Transform(viewTopLeft);
            viewBottomRight = rootTransform.Transform(viewBottomRight);

            //Transform the point to device coordinates.
            if ((source != null) && (source.CompositionTarget != null))
            {
                Matrix deviceTransform = source.CompositionTarget.TransformToDevice;
                compositionTopLeft = deviceTransform.Transform(compositionTopLeft);
                compositionBottomLeft = deviceTransform.Transform(compositionBottomLeft);

                viewTopLeft = deviceTransform.Transform(viewTopLeft);
                viewBottomRight = deviceTransform.Transform(viewBottomRight);
            }

            bool ret;
            {
                NativeMethods.LOGFONT logFont = new NativeMethods.LOGFONT();

                //The +2 is a fudge factor to compensate between the text size (from top to
                //bottom black pixel) and the corresponding Win32 font height.
                logFont.lfHeight = (int)Math.Round(compositionBottomLeft.Y - compositionTopLeft.Y + heightPadding);

                logFont.lfFaceName = compositionFont;

                // Allocate unsafe memory
                IntPtr fontPtr = Marshal.AllocHGlobal(Marshal.SizeOf(logFont));
                try
                {
                    Marshal.StructureToPtr(logFont, fontPtr, true);
                    ret = NativeMethods.ImmSetCompositionFontW(immContext, fontPtr);
                }
                finally
                {
                    // Free the allocated memory
                    Marshal.FreeHGlobal(fontPtr);
                }
            }

            {
                NativeMethods.COMPOSITIONFORM compositionForm = new NativeMethods.COMPOSITIONFORM();
                compositionForm.dwStyle = NativeMethods.CFS_RECT;
                compositionForm.ptCurrentPos = new NativeMethods.POINT();
                compositionForm.ptCurrentPos.x = Math.Max(0, (int)(compositionTopLeft.X));  //Make sure the position is all positive
                compositionForm.ptCurrentPos.y = Math.Max(0, (int)(compositionTopLeft.Y));  //(win32 doesn't deal with negative coordinates well).

                //Make sure the placement area contains the current position (otherwise the ImmSetCompositionWindow silently fails).
                compositionForm.rcArea.left = Math.Min((int)(viewTopLeft.X), compositionForm.ptCurrentPos.x);
                compositionForm.rcArea.top = Math.Min((int)(viewTopLeft.Y), compositionForm.ptCurrentPos.y);
                compositionForm.rcArea.right = Math.Max((int)(viewBottomRight.X), compositionForm.ptCurrentPos.x);
                compositionForm.rcArea.bottom = Math.Max((int)(viewBottomRight.Y), compositionForm.ptCurrentPos.y);

                // Allocate Unsafe Memory
                IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(compositionForm));
                try
                {
                    Marshal.StructureToPtr(compositionForm, ptr, true);
                    ret = NativeMethods.ImmSetCompositionWindow(immContext, ptr) && ret;
                }
                finally
                {
                    // Free the allocated memory
                    Marshal.FreeHGlobal(ptr);
                }
            }

            return ret;
        }

        public static IntPtr GetDefaultIMEWnd()
        {
            return NativeMethods.ImmGetDefaultIMEWnd(IntPtr.Zero);
        }

        public static IntPtr GetImmContext(IntPtr hwnd)
        {
            if (hwnd != IntPtr.Zero)
                return NativeMethods.ImmGetContext(hwnd);
            else
                return IntPtr.Zero;
        }

        /// <summary>
        /// Release the IMM Context.
        /// </summary>
        public static bool ReleaseContext(IntPtr hwnd, IntPtr immContext)
        {
            if ((hwnd != IntPtr.Zero) && (immContext != IntPtr.Zero))
                return NativeMethods.ImmReleaseContext(hwnd, immContext);
            else
                return false;
        }

        public static IntPtr AttachContext(HwndSource hwndSource, IntPtr imeContext)
        {
            if (hwndSource == null)
                throw new ArgumentNullException("hwndSource");

            return NativeMethods.ImmAssociateContext(hwndSource.Handle, imeContext);
        }

        public static void EnableImmComposition()
        {

            if (!_threadMgrFailed)
            {
                // Create a Thread manager if it doesn't exist
                if (_threadMgr == null)
                {
                    NativeMethods.TF_CreateThreadMgr(out _threadMgr);
                    if (_threadMgr == null)
                    {
                        _threadMgrFailed = true;
                        return;
                    }
                }

                _threadMgr.SetFocus(IntPtr.Zero);
            }
        }

        public static IntPtr GetKeyboardLayout()
        {
            return NativeMethods.GetKeyboardLayout(0);
        }

        public static bool ImmIsIME(IntPtr hkl)
        {
            return NativeMethods.ImmIsIME(hkl);
        }

        /// <summary>
        /// This method is needed because standard WPF new Cursor(stream) doesn't 
        /// support multi-image cursor files - whatever system DPI is, it will
        /// always use first size in the cursor file.
        /// This method loads cursor from a stream and if the cursor file contains more than 
        /// one size, an appropriate size will be used based on system DPI setting.
        /// </summary>
        public static Cursor LoadCursorDPIAware(Stream cursorStream)
        {
            // Copied from wpf\src\core\csharp\system\windows\input\cursor.cs

            // Using our Path.GetRandomFileName() since Path.GetTempFileName() will only generate 65535 files before it starts throwing exceptions.  
            // Since anyone can call Path.GetTempFileName() and not clean up properly, we could technically end up in that situation and this code
            // will fail to execute.  Since deleting temp files not owned by editor may cause other programs/features to fail, we create our own unique file name.
            string filePath = string.Empty;
            try
            {
                using (BinaryReader reader = new BinaryReader(cursorStream))
                {
                    using (FileStream fileStream = GetRandomFileNameStream(Path.GetTempPath(), out filePath))
                    {
                        cursorStream.CopyTo(fileStream);
                    }
                }

                IntPtr handle = NativeMethods.LoadImage(IntPtr.Zero,
                                                     filePath,
                                                     NativeMethods.IMAGE_TYPE.IMAGE_CURSOR,
                                                     0, 0,
                                                     NativeMethods.IMAGE_FORMAT_REQUEST.LR_DEFAULTCOLOR |
                                                     NativeMethods.IMAGE_FORMAT_REQUEST.LR_DEFAULTSIZE |
                                                     NativeMethods.IMAGE_FORMAT_REQUEST.LR_LOADFROMFILE);

                SafeCursor cursorHandle = new SafeCursor(handle);
                if (cursorHandle.IsInvalid)
                {
                    return null;
                }
                return CursorInteropHelper.Create(cursorHandle);
            }
            catch (Exception)
            {
                Debug.Fail("Could not load cursor resource.");
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            return null;
        }

        /// <summary>
        /// Generate a new FileStream with a unique random name.
        /// </summary>
        /// <param name="fileDirectory">Directory where the file will live.</param>
        /// <param name="filePath">Path to the file created.</param>
        /// <returns>A file stream with a random file name.</returns>
        private static FileStream GetRandomFileNameStream(string fileDirectory, out string filePath)
        {
            int count = 0;
            filePath = string.Empty;
            while (count++ < 2)
            {
                string fileName = Path.GetRandomFileName();
                filePath = Path.Combine(fileDirectory, fileName + "~");   //The ~ suffix hides the temporary file from GIT.
                if (!File.Exists(filePath))
                {
                    try
                    {
                        return new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    }
                    catch (Exception)
                    {
                        Debug.Fail("Creating random file failed.");
                    }
                }
            }

            throw new IOException(filePath + " exists");
        }
    }

    /// <summary>
    /// SafeHandle wrapper for the cursor image
    /// </summary>
    internal sealed class SafeCursor : SafeHandle
    {
        public SafeCursor()
            : base(IntPtr.Zero, true)
        {
        }

        public SafeCursor(IntPtr hCursor)
            : base(hCursor, true)
        {
        }

        public override bool IsInvalid
        {
            get
            {
                return this.handle == IntPtr.Zero;
            }
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.DestroyCursor(this.handle);
        }
    }



    static class NativeMethods
    {
        public const int LOGPIXELSX = 88;
        public const int LOGPIXELSY = 90;

        #region Win32 Interop

        /// <summary>
        /// A point structure to match the Win32 POINT
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        };

        /// <summary>
        /// A rect structure to match the Win32 RECT
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        };

        /// <summary>
        /// Win32 MONITORINFO Struct
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        };

        /// <summary>
        /// Win32 COMPOSITIONFORM struct
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct COMPOSITIONFORM
        {
            public int dwStyle;
            public POINT ptCurrentPos;
            public RECT rcArea;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECONVERTSTRING
        {
            public int dwSize;
            public int dwVersion;
            public int dwStrLen;
            public int dwStrOffset;
            public int dwCompStrLen;
            public int dwCompStrOffset;
            public int dwTargetStrLen;
            public int dwTargetStrOffset;
        }

        /// <summary>
        /// Win32 LOGFONT struct
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class LOGFONT
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
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string lfFaceName;
        }

        /// <summary>
        /// Win32 WINDOWPOS struct
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public class WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        /// <summary></summary>
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("aa80e801-2021-11d2-93e0-0060b067b86e")]
        internal interface ITfThreadMgr
        {
            // <summary></summary>
            //HRESULT Activate([out] TfClientId *ptid);
            /// <SecurityNote>
            ///     Critical: This code calls into an unmanaged COM function which is not
            ///     safe since it elevates
            /// </SecurityNote>
            [SecurityCritical]
            [SuppressUnmanagedCodeSecurity]
            void Activate(out int clientId);

            // <summary></summary>
            //HRESULT Deactivate();
            /// <SecurityNote>
            ///     Critical: This code calls into an unmanaged COM function which is not
            ///     safe since it elevates
            /// </SecurityNote>
            [SecurityCritical]
            [SuppressUnmanagedCodeSecurity]
            void Deactivate();

            // <summary></summary>
            //HRESULT CreateDocumentMgr([out] ITfDocumentMgr **ppdim);
            /// <SecurityNote>
            ///     Critical: This code calls into an unmanaged COM function which is not
            ///     safe since it elevates
            /// </SecurityNote>
            [SecurityCritical]
            [SuppressUnmanagedCodeSecurity]
            void CreateDocumentMgr(out object docMgr);

            /// <summary></summary>
            //HRESULT EnumDocumentMgrs([out] IEnumTfDocumentMgrs **ppEnum);
            void EnumDocumentMgrs(out object enumDocMgrs);

            /// <summary></summary>
            //HRESULT GetFocus([out] ITfDocumentMgr **ppdimFocus);
            void GetFocus(out IntPtr docMgr);

            // <summary></summary>
            //HRESULT SetFocus([in] ITfDocumentMgr *pdimFocus);
            /// <SecurityNote>
            ///     Critical: This code calls into an unmanaged COM function which is not
            ///     safe since it elevates
            /// </SecurityNote>
            [SecurityCritical]
            [SuppressUnmanagedCodeSecurity]
            void SetFocus(IntPtr docMgr);

            /// <summary></summary>
            //HRESULT AssociateFocus([in] HWND hwnd,
            //                       [in, unique] ITfDocumentMgr *pdimNew,
            //                       [out] ITfDocumentMgr **ppdimPrev);
            void AssociateFocus(IntPtr hwnd, object newDocMgr, out object prevDocMgr);

            /// <summary></summary>
            //HRESULT IsThreadFocus([out] BOOL *pfThreadFocus);
            void IsThreadFocus([MarshalAs(UnmanagedType.Bool)] out bool isFocus);

            //HRESULT GetFunctionProvider([in] REFCLSID clsid,
            //                            [out] ITfFunctionProvider **ppFuncProv);
            /// <summary></summary>
            /// <SecurityNote>
            ///     Critical: This code calls into an unmanaged COM function which is not
            ///     safe since it elevates
            /// </SecurityNote>
            [SecurityCritical]
            [SuppressUnmanagedCodeSecurity]
            [PreserveSig]
            int GetFunctionProvider(ref Guid classId, out object funcProvider);

            /// <summary></summary>
            //HRESULT EnumFunctionProviders([out] IEnumTfFunctionProviders **ppEnum);
            void EnumFunctionProviders(out object enumProviders);

            //HRESULT GetGlobalCompartment([out] ITfCompartmentMgr **ppCompMgr);
            /// <summary></summary>
            /// <SecurityNote>
            ///     Critical: This code calls into an unmanaged COM function which is not
            ///     safe since it elevates
            /// </SecurityNote>
            [SecurityCritical]
            [SuppressUnmanagedCodeSecurity]
            void GetGlobalCompartment(out object compartmentMgr);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyCursor(IntPtr hCursor);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);

        [DllImport("User32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReleaseDC(IntPtr hWnd, IntPtr hdc);

        [DllImport("Gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int index);

        [DllImport("user32.dll")]
        public extern static IntPtr GetKeyboardLayout(int dwThread);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public extern static bool ClientToScreen(IntPtr hWnd, ref POINT point);

        [DllImport("user32.dll")]
        public extern static IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        [DllImport("user32.dll")]
        public extern static IntPtr MonitorFromPoint(POINT pt, int dwFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public extern static bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr LoadImage(IntPtr hinst, string lpszName, IMAGE_TYPE uType, int cxDesired, int cyDesired, IMAGE_FORMAT_REQUEST fuLoad);

        [DllImport("msctf.dll")]
        internal static extern int TF_CreateThreadMgr(out ITfThreadMgr threadMgr);

        [DllImport("imm32.dll")]
        internal static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

        [DllImport("imm32.dll")]
        internal static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("imm32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ImmSetCompositionWindow(IntPtr hIMC, IntPtr ptr);

        [DllImport("imm32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

        [DllImport("imm32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ImmSetCompositionFontW(IntPtr hIMC, IntPtr lplf);

        [DllImport("imm32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.I4)]
        internal static extern int ImmGetCompositionStringW(IntPtr hIMC, int dwIndex, StringBuilder lpBuf, int dwBufLen);

        [DllImport("imm32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.I4)]
        internal static extern int ImmSetCompositionStringW(IntPtr hIMC, int dwIndex, StringBuilder lpComp, int dwCompLen, StringBuilder lpBuf, int dwBufLen);

        [DllImport("imm32.dll")]
        internal static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);

        [DllImport("imm32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ImmNotifyIME(IntPtr immContext, int dwAction, int dwIndex, int dwValue);

        [DllImport("imm32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr ImmEscapeW(IntPtr hkl, IntPtr himc, int esc, IntPtr lpBuf);

        [DllImport("imm32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool ImmIsIME(IntPtr hkl);

        public const int MONITOR_DEFAULTTONEAREST = 0x00000002;
        public const int CFS_RECT = 0x0001;

        public enum IMAGE_TYPE
        {
            IMAGE_BITMAP = 0,
            IMAGE_ICON = 1,
            IMAGE_CURSOR = 2,
            IMAGE_ENHMETAFILE = 3
        }

        [Flags]
        public enum IMAGE_FORMAT_REQUEST
        {
            LR_DEFAULTCOLOR = 0x0000,
            LR_MONOCHROME = 0x0001,
            LR_COPYRETURNORG = 0x0004,
            LR_COPYDELETEORG = 0x0008,
            LR_LOADFROMFILE = 0x0010,
            LR_DEFAULTSIZE = 0x0040,
            LR_LOADMAP3DCOLORS = 0x1000,
            LR_CREATEDIBSECTION = 0x2000,
            LR_COPYFROMRESOURCE = 0x4000,
            LR_SHARED = 0x8000
        }

        #endregion // Win32 Interop
    }
}
