// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.ComponentModel;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using System.Text;

namespace D2DShapes
{
    internal class TextLayoutShape : TextShape
    {
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public TextLayout TextLayout { get; set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public Point2F Point0 { get; set; }

        public TextLayoutShape(RenderTarget initialRenderTarget, Random random, D2DFactory d2DFactory, D2DBitmap bitmap, DWriteFactory dwriteFactory)
            : base(initialRenderTarget, random, d2DFactory, bitmap, dwriteFactory)
        {
            RandomizeTextLayout();
            Point0 = RandomPoint();
        }

        private void RandomizeTextLayout()
        {
            TextLayout = dwriteFactory.CreateTextLayout(
                Text,
                TextFormat,
                Random.Next(50, Math.Max(100, CanvasWidth - (int)Point0.X)),
                Random.Next(50, Math.Max(100, CanvasHeight - (int)Point0.Y)));
            if (CoinFlip)
                TextLayout.SetUnderline(true, RandomTextRange());
            if (CoinFlip)
                TextLayout.SetStrikethrough(true, RandomTextRange());
            if (CoinFlip)
                TextLayout.LineSpacing = RandomLineSpacing(TextFormat.FontSize);
            if (NiceGabriola)
            {
                TypographySettingCollection t = dwriteFactory.CreateTypography();
                t.Add(new FontFeature(FontFeatureTag.StylisticSet07, 1));
                TextLayout.SetTypography(t, new TextRange(0, (uint)Text.Length));
            }
        }

        private TextRange RandomTextRange()
        {
            var start = Random.Next(0, Text.Length - 5);
            var length = Random.Next(1, Text.Length - start);
            return new TextRange((uint)start, (uint)length);
        }

        protected internal override void Draw(RenderTarget renderTarget)
        {
            DrawingStateBlock stateBlock = d2DFactory.CreateDrawingStateBlock();
            renderTarget.SaveDrawingState(stateBlock);
            renderTarget.TextRenderingParams = RenderingParams;

            if (Options.HasValue)
            {
                renderTarget.DrawTextLayout(
                    Point0, TextLayout, FillBrush, Options.Value);
            }
            else
                renderTarget.DrawTextLayout(
                    Point0, TextLayout, FillBrush);
            renderTarget.RestoreDrawingState(stateBlock);
            stateBlock.Dispose();
        }

        public override bool HitTest(Point2F point)
        {
            //bool isTrailingHit, isInside;
            //TextLayout.HitTestPoint(point.X, point.Y, out isTrailingHit, out isInside);
            //return (isTrailingHit || isInside);
            //the method below checks the layout box hit test instead of the DirectWrite method
            return point.X >= Point0.X &&
                point.Y >= Point0.Y &&
                point.X <= Point0.X + TextLayout.MaxWidth &&
                point.Y <= Point0.Y + TextLayout.MaxHeight;
        }

        public override void Dispose()
        {
            if (TextLayout != null)
                TextLayout.Dispose();
            TextLayout = null;
            base.Dispose();
        }
    }
}
