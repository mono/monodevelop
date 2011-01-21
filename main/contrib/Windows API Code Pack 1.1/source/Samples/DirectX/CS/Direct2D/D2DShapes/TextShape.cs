// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.ComponentModel;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using System.Text;
using System.Globalization;

namespace D2DShapes
{
    internal class TextShape : DrawingShape
    {
        private RectF layoutRect;
        protected internal DWriteFactory dwriteFactory;

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public RenderingParams RenderingParams { get; set; }

        [TypeConverter(typeof (ExpandableObjectConverter))]
        public DrawTextOptions? Options { get; set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public TextFormat TextFormat { get; set; }

        public string Text { get; set; }

        protected bool NiceGabriola;

        public TextShape(RenderTarget initialRenderTarget, Random random, D2DFactory d2DFactory, D2DBitmap bitmap, DWriteFactory dwriteFactory)
            : base(initialRenderTarget, random, d2DFactory, bitmap)
        {
            this.dwriteFactory = dwriteFactory;
            layoutRect = RandomRect(CanvasWidth, CanvasHeight);
            NiceGabriola = Random.NextDouble() < 0.25 && dwriteFactory.SystemFontFamilyCollection.Contains("Gabriola");
            TextFormat = dwriteFactory.CreateTextFormat(
                RandomFontFamily(),
                RandomFontSize(),
                RandomFontWeight(),
                RandomFontStyle(),
                RandomFontStretch(),
                System.Globalization.CultureInfo.CurrentUICulture);
            if (CoinFlip)
                TextFormat.LineSpacing = RandomLineSpacing(TextFormat.FontSize);
            Text = RandomString(Random.Next(1000, 1000));

            FillBrush = RandomBrush();
            RenderingParams = RandomRenderingParams();

            if (CoinFlip)
            {
                Options = DrawTextOptions.None;
                if (CoinFlip)
                    Options |= DrawTextOptions.Clip;
                if (CoinFlip)
                    Options |= DrawTextOptions.NoSnap;
            }
        }

        protected internal RenderingParams RandomRenderingParams()
        {
            RenderingParams rp = dwriteFactory.CreateCustomRenderingParams(
                (float)Math.Max(0.001, Math.Min(1, Random.NextDouble() * 2)), //gamma needs to be nonzero
                (float)Math.Max(0, Math.Min(1, Random.NextDouble() * 3 - 1)), //equal chances for 0, 1 and something in between
                (float)Math.Max(0, Math.Min(1, Random.NextDouble() * 3 - 1)), //equal chances for 0, 1 and something in between
                RandomPixelGeometry(),
                RandomRenderingMode());
            return rp;
        }

        private PixelGeometry RandomPixelGeometry()
        {
            return (PixelGeometry) Random.Next(0, 2);
        }

        private RenderingMode RandomRenderingMode()
        {
            return (RenderingMode)Random.Next(0, 6);
        }

        protected internal LineSpacing RandomLineSpacing(float fontSize)
        {
            LineSpacingMethod method = CoinFlip
                                           ? LineSpacingMethod.Default
                                           : LineSpacingMethod.Uniform;
            var spacing = (float)(Random.NextDouble()*fontSize*4 + 0.5);
            var baseline = (float)Random.NextDouble();
            return new LineSpacing(
                method,
                spacing,
                baseline);
        }

        private string RandomFontFamily()
        {
            if (NiceGabriola)
                return "Gabriola";
            if (CoinFlip)
            {
                //get random font out of the list of installed fonts
                int i = Random.Next(0, dwriteFactory.SystemFontFamilyCollection.Count - 1);
                FontFamily f = dwriteFactory.SystemFontFamilyCollection[i];
                string ret = null;

                if (f.FamilyNames.ContainsKey(CultureInfo.CurrentUICulture))
                    ret = f.FamilyNames[CultureInfo.CurrentUICulture];
                else if (f.FamilyNames.ContainsKey(CultureInfo.InvariantCulture))
                    ret = f.FamilyNames[CultureInfo.InvariantCulture];
                else if (f.FamilyNames.ContainsKey(CultureInfo.GetCultureInfo("EN-us")))
                    ret = f.FamilyNames[CultureInfo.GetCultureInfo("EN-us")];
                else
                {
                    foreach (var c in f.FamilyNames.Keys)
                    {
                        ret = f.FamilyNames[c];
                        break;
                    }
                }
                f.Dispose();
                return ret;
            }
            //get one of the common fonts
            return new[] {"Arial",
                           "Times New Roman",
                           "Courier New",
                           "Impact",
                           "Tahoma",
                           "Calibri",
                           "Consolas",
                           "Segoe",
                           "Cambria"
            }[Random.Next(0, 8)];
        }

        private float RandomFontSize()
        {
            return 6 + (float)(138 * Random.NextDouble() * Random.NextDouble());
        }

        private FontWeight RandomFontWeight()
        {
            return (FontWeight)(Math.Min(950, 100 * Random.Next(1, 10)));
        }

        private FontStyle RandomFontStyle()
        {
            return (FontStyle)Random.Next(0, 2);
        }

        private FontStretch RandomFontStretch()
        {
            return (FontStretch) Random.Next(1, 9);
        }

        private string RandomString(int size)
        {
            var builder = new StringBuilder(size + 1) { Length = size };
            builder[0] = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * Random.NextDouble() + 65)));
            for (int i = 1; i < size - 1; i++)
            {
                builder[i] = Random.NextDouble() < 0.2 ?
                    ' ' :
                    Convert.ToChar(Convert.ToInt32(Math.Floor(26 * Random.NextDouble() + 97)));
            }
            builder[size - 1] = '.';
            return builder.ToString();
        }

        protected internal override void ChangeRenderTarget(RenderTarget newRenderTarget)
        {
            FillBrush = CopyBrushToRenderTarget(FillBrush, newRenderTarget);
        }

        protected internal override void Draw(RenderTarget renderTarget)
        {
            DrawingStateBlock stateBlock = d2DFactory.CreateDrawingStateBlock();
            renderTarget.SaveDrawingState(stateBlock);
            renderTarget.TextRenderingParams = RenderingParams;

            if (Options.HasValue)
            {
                renderTarget.DrawText(Text, TextFormat, layoutRect, FillBrush, Options.Value);
            }
            else
                renderTarget.DrawText(Text, TextFormat, layoutRect, FillBrush);

            renderTarget.RestoreDrawingState(stateBlock);
            stateBlock.Dispose();
        }

        public override bool HitTest(Point2F point)
        {
            return point.X >= layoutRect.Left &&
                point.Y >= layoutRect.Top &&
                point.X <= layoutRect.Right &&
                point.Y <= layoutRect.Bottom;
        }

        public override void Dispose()
        {
            if (TextFormat != null)
                TextFormat.Dispose();
            TextFormat = null;
            if (RenderingParams != null)
                RenderingParams.Dispose();
            RenderingParams = null;
            base.Dispose();
        }
    }
}
