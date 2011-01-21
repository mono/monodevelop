// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using DXGI = Microsoft.WindowsAPICodePack.DirectX.Graphics;
using FontFamily=Microsoft.WindowsAPICodePack.DirectX.DirectWrite.FontFamily;
using FontStyle=Microsoft.WindowsAPICodePack.DirectX.DirectWrite.FontStyle;

namespace D2DPaint
{
    public class FontEnumComboBox : ComboBox
    {
        #region Fields
        private readonly CultureInfo enUSCulture = new CultureInfo("en-US");
        private D2DFactory d2DFactory;
        private DWriteFactory dwriteFactory;
        private DCRenderTarget dcRenderTarget;
        private SolidColorBrush brush;
        List<string> primaryNames = new List<string>();
        private Dictionary<string, TextLayout> layouts;
        private float maxHeight;
        #endregion

        #region Properties
        private float dropDownFontSize = 18;
        /// <summary>
        /// Gets or sets the size of the font used in the drop down.
        /// </summary>
        /// <value>The size of the drop down font.</value>
        [DefaultValue(18f)]
        public float DropDownFontSize
        {
            get { return dropDownFontSize; }
            set { dropDownFontSize = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether all items should be of the same height (the height of the tallest font) or whether they should use the minimum size required for each font.
        /// </summary>
        /// <value><c>true</c> if item height should be fixed; otherwise, <c>false</c>.</value>
        [DefaultValue(true)]
        public bool FixedItemHeight { get; set; }
        #endregion

        #region FontEnumComboBox()
        public FontEnumComboBox()
        {
            FixedItemHeight = true;
        }
        #endregion 

        #region Initialize()
        public void Initialize()
        {
            d2DFactory = D2DFactory.CreateFactory(D2DFactoryType.Multithreaded);
            dwriteFactory = DWriteFactory.CreateFactory();
            InitializeRenderTarget();
            FillFontFamilies();
            if (FixedItemHeight)
                DropDownHeight = (int)maxHeight * 10;
            DrawMode = DrawMode.OwnerDrawVariable;
            MeasureItem += FontEnumComboBox_MeasureItem;
            DrawItem += FontEnumComboBox_DrawItem;
        } 
        #endregion

        #region InitializeRenderTarget()
        private void InitializeRenderTarget()
        {
            if (dcRenderTarget == null)
            {
                var props = new RenderTargetProperties
                {
                    PixelFormat = new PixelFormat(
                        Microsoft.WindowsAPICodePack.DirectX.Graphics.Format.B8G8R8A8UNorm,
                        AlphaMode.Ignore),
                    Usage = RenderTargetUsages.GdiCompatible
                };
                dcRenderTarget = d2DFactory.CreateDCRenderTarget(props);
                brush = dcRenderTarget.CreateSolidColorBrush(
                    new ColorF(
                        ForeColor.R / 256f,
                        ForeColor.G / 256f,
                        ForeColor.B / 256f,
                        1));
            }
        }
        #endregion

        #region FillFontFamilies()
        private void FillFontFamilies()
        {
            maxHeight = 0;
            primaryNames = new List<string>();
            layouts = new Dictionary<string, TextLayout>();
            foreach (FontFamily family in dwriteFactory.SystemFontFamilyCollection)
            {
                AddFontFamily(family);
            }
            primaryNames.Sort();
            Items.Clear();
            Items.AddRange(primaryNames.ToArray());
        } 
        #endregion

        #region AddFontFamily()
        private void AddFontFamily(FontFamily family)
        {
            string familyName;
            CultureInfo familyCulture;

            // First try getting a name in the user's language.
            familyCulture = CultureInfo.CurrentUICulture;
            family.FamilyNames.TryGetValue(familyCulture, out familyName);

            if (familyName == null)
            {
                // Fall back to en-US culture. This is somewhat arbitrary, but most fonts have English
                // strings so this at least yields predictable fallback behavior in most cases.
                familyCulture = enUSCulture;
                family.FamilyNames.TryGetValue(familyCulture, out familyName);
            }

            if (familyName == null)
            {
                // As a last resort, use the first name we find. This will just be the name associated
                // with whatever locale name sorts first alphabetically.
                foreach (KeyValuePair<CultureInfo, string> entry in family.FamilyNames)
                {
                    familyCulture = entry.Key;
                    familyName = entry.Value;
                }
            }

            if (familyName == null)
                return;

            //add info to list of structs used as a cache of text layouts
            var displayFormats = new List<TextLayout>();
            var format = dwriteFactory.CreateTextFormat(
                    family.Fonts[0].IsSymbolFont ? Font.FontFamily.Name : familyName,
                    DropDownFontSize,
                    FontWeight.Normal,
                    FontStyle.Normal,
                    FontStretch.Normal,
                    familyCulture);
            format.WordWrapping = WordWrapping.NoWrap;
            var layout = dwriteFactory.CreateTextLayout(
                familyName,
                format,
                10000,
                10000);
            DropDownWidth = Math.Max(DropDownWidth, (int)layout.Metrics.Width);
            maxHeight = Math.Max(maxHeight, layout.Metrics.Height);
            displayFormats.Add(layout);
            //add name to list
            primaryNames.Add(familyName);
            layouts.Add(familyName, layout);
        } 
        #endregion

        #region FontEnumComboBox_MeasureItem()
        void FontEnumComboBox_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            //initialize the DC Render Target and a brush before first use
            InitializeRenderTarget();
            var fontName = (string)Items[e.Index];
            e.ItemWidth = (int)layouts[fontName].Metrics.Width + 10;
            e.ItemHeight = FixedItemHeight ? (int)maxHeight : (int)layouts[fontName].Metrics.Height;
        } 
        #endregion

        #region FontEnumComboBox_DrawItem()
        void FontEnumComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            //initialize the DC Render Target and a brush before first use
            InitializeRenderTarget();

            //draw the background of the combo item
            e.DrawBackground();

            //set section of the DC to draw on
            var subRect = new Rect(
                e.Bounds.Left,
                e.Bounds.Top,
                e.Bounds.Right,
                e.Bounds.Bottom);

            //bind the render target with the DC
            dcRenderTarget.BindDC(e.Graphics.GetHdc(), subRect);

            //draw the text using D2D/DWrite
            dcRenderTarget.BeginDraw();

            var fontName = (string)Items[e.Index];
            //if ((e.State & DrawItemState.Selected & ~DrawItemState.NoFocusRect) != DrawItemState.None)
            dcRenderTarget.DrawTextLayout(
                new Point2F(5, (e.Bounds.Height - layouts[fontName].Metrics.Height) / 2),
                layouts[fontName],
                brush,
                DrawTextOptions.Clip);

            dcRenderTarget.EndDraw();
            //release the DC
            e.Graphics.ReleaseHdc();
            //drow focus rect for a focused item
            e.DrawFocusRectangle();
        } 
        #endregion

        #region Dispose()
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //dispose of all layouts
                while (layouts.Keys.Count > 0)
                    foreach (string key in layouts.Keys)
                    {
                        layouts[key].Dispose();
                        layouts.Remove(key);
                        break;
                    }
                    
                if (brush != null)
                    brush.Dispose();
                brush = null;
                if (dcRenderTarget != null)
                    dcRenderTarget.Dispose();
                dcRenderTarget = null;
                if (dwriteFactory != null)
                    dwriteFactory.Dispose();
                dwriteFactory = null;
                if (d2DFactory != null)
                    d2DFactory.Dispose();
                d2DFactory = null;
            }
            base.Dispose(disposing);
        } 
        #endregion
    }
}
