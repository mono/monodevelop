// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows;
using Microsoft.WindowsAPICodePack.DirectX.Controls;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;

using DWrite = Microsoft.WindowsAPICodePack.DirectX.DirectWrite;

namespace Microsoft.WindowsAPICodePack.Samples
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        D2DFactory d2dFactory;
        DWriteFactory dwriteFactory;
        HwndRenderTarget renderTarget;
        SolidColorBrush blackBrush;
        BitmapBrush gridPatternBitmapBrush;
        SolidColorBrush solidBrush1;
        SolidColorBrush solidBrush2;
        SolidColorBrush solidBrush3;
        LinearGradientBrush linearGradientBrush;
        RadialGradientBrush radialGradientBrush;
        TextFormat textFormat;
        TextLayout textLayout;

        int x1 = 70, x2 = 82, x3 = 25, x4 = 75, x5 = 54;

        public Window1()
        {
            InitializeComponent();
            host.Loaded += new RoutedEventHandler(host_Loaded);
            host.SizeChanged += new SizeChangedEventHandler(host_SizeChanged);
        }


        void host_Loaded(object sender, RoutedEventArgs e)
        {
            // Create the D2D Factory
            d2dFactory = D2DFactory.CreateFactory(D2DFactoryType.SingleThreaded);

            // Create the DWrite Factory
            dwriteFactory = DWriteFactory.CreateFactory();

            // Start rendering now!
            host.Render = Render;
            host.InvalidateVisual();
        }

        void host_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (renderTarget != null)
            {
                // Resize the render targrt to the actual host size
                renderTarget.Resize(new SizeU((uint)(host.ActualWidth), (uint)(host.ActualHeight)));
            }
            InvalidateVisual();
        }

        /// <summary>
        /// This method creates the render target and all associated D2D and DWrite resources
        /// </summary>
        void CreateDeviceResources()
        {
            // Only calls if resources have not been initialize before
            if (renderTarget == null)
            {
                // The text format
                textFormat = dwriteFactory.CreateTextFormat("Bodoni MT", 24, DWrite.FontWeight.Normal, DWrite.FontStyle.Italic, DWrite.FontStretch.Normal);

                // Create the render target
                SizeU size = new SizeU((uint)host.ActualWidth, (uint)host.ActualHeight);
                RenderTargetProperties props = new RenderTargetProperties();
                HwndRenderTargetProperties hwndProps = new HwndRenderTargetProperties(host.Handle, size, PresentOptions.None);
                renderTarget = d2dFactory.CreateHwndRenderTarget(props, hwndProps);

                // A black brush to be used for drawing text
                ColorF cf = new ColorF(0, 0, 0, 1);
                blackBrush = renderTarget.CreateSolidColorBrush(cf);

                // Create a linear gradient.
                GradientStop[] stops = 
                { 
                    new GradientStop(1, new ColorF(1f, 0f, 0f, 0.25f)),
                    new GradientStop(0, new ColorF(0f, 0f, 1f, 1f))
                };

                GradientStopCollection pGradientStops = renderTarget.CreateGradientStopCollection(stops, Gamma.Linear, ExtendMode.Wrap);
                LinearGradientBrushProperties gradBrushProps = new LinearGradientBrushProperties(new Point2F(50, 25), new Point2F(25, 50));

                linearGradientBrush = renderTarget.CreateLinearGradientBrush(gradBrushProps, pGradientStops);

                gridPatternBitmapBrush = CreateGridPatternBrush(renderTarget);

                solidBrush1 = renderTarget.CreateSolidColorBrush(new ColorF(0.3F, 0.5F, 0.65F, 0.25F));
                solidBrush2 = renderTarget.CreateSolidColorBrush(new ColorF(0.0F, 0.0F, 0.65F, 0.5F));
                solidBrush3 = renderTarget.CreateSolidColorBrush(new ColorF(0.9F, 0.5F, 0.3F, 0.75F));

                // Create a linear gradient.
                stops[0] = new GradientStop(1, new ColorF(0f, 0f, 0f, 0.25f));
                stops[1] = new GradientStop(0, new ColorF(1f, 1f, 0.2f, 1f));
                GradientStopCollection radiantGradientStops = renderTarget.CreateGradientStopCollection(stops, Gamma.Linear, ExtendMode.Wrap);

                RadialGradientBrushProperties radialBrushProps = new RadialGradientBrushProperties(new Point2F(25, 25), new Point2F(0, 0), 10, 10);
                radialGradientBrush = renderTarget.CreateRadialGradientBrush(radialBrushProps, radiantGradientStops);
            }
        }

        /// <summary>
        /// Create the grid pattern (squares) brush 
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        BitmapBrush CreateGridPatternBrush(RenderTarget target)
        {

            // Create a compatible render target.
            BitmapRenderTarget compatibleRenderTarget =
                target.CreateCompatibleRenderTarget(CompatibleRenderTargetOptions.None, new SizeF(10.0f, 10.0f));

            //// Draw a pattern.
            SolidColorBrush spGridBrush = compatibleRenderTarget.CreateSolidColorBrush(new ColorF(0.93f, 0.94f, 0.96f, 1.0f));
            compatibleRenderTarget.BeginDraw();
            compatibleRenderTarget.FillRectangle(new RectF(0.0f, 0.0f, 10.0f, 1.0f), spGridBrush);
            compatibleRenderTarget.FillRectangle(new RectF(0.0f, 0.1f, 1.0f, 10.0f), spGridBrush);
            compatibleRenderTarget.EndDraw();

            //// Retrieve the bitmap from the render target.
            D2DBitmap spGridBitmap;
            spGridBitmap = compatibleRenderTarget.Bitmap;

            //// Choose the tiling mode for the bitmap brush.
            BitmapBrushProperties brushProperties = new BitmapBrushProperties(ExtendMode.Wrap, ExtendMode.Wrap, BitmapInterpolationMode.Linear);
            //// Create the bitmap brush.
            return renderTarget.CreateBitmapBrush(spGridBitmap, brushProperties);
        }

        private void Render()
        {

            CreateDeviceResources();

            if (renderTarget.IsOccluded)
                return;

            SizeF renderTargetSize = renderTarget.Size;

            renderTarget.BeginDraw();

            renderTarget.Clear(new ColorF(1, 1, 1, 0));

            // Paint a grid background.
            RectF rf = new RectF(0.0f, 0.0f, renderTargetSize.Width, renderTargetSize.Height);
            renderTarget.FillRectangle(rf, gridPatternBitmapBrush);

            float curLeft = 0;

            rf = new RectF(
                curLeft,
                renderTargetSize.Height,
                (curLeft + renderTargetSize.Width / 5.0F),
                renderTargetSize.Height - renderTargetSize.Height * ((float)x1 / 100.0F));

            renderTarget.FillRectangle(rf, solidBrush1);

            textLayout = dwriteFactory.CreateTextLayout(String.Format("  {0}%", x1), textFormat, renderTargetSize.Width / 5.0F, 30);

            renderTarget.DrawTextLayout(
                new Point2F(curLeft, renderTargetSize.Height - 30),
                textLayout,
                blackBrush);

            curLeft = (curLeft + renderTargetSize.Width / 5.0F);
            rf = new RectF(
                curLeft,
                renderTargetSize.Height,
                (curLeft + renderTargetSize.Width / 5.0F),
                renderTargetSize.Height - renderTargetSize.Height * ((float)x2 / 100.0F));
            renderTarget.FillRectangle(rf, radialGradientBrush);
            renderTarget.DrawText(
                String.Format("  {0}%", x2),
                textFormat,
                new RectF(curLeft, renderTargetSize.Height - 30, (curLeft + renderTargetSize.Width / 5.0F), renderTargetSize.Height), blackBrush);

            curLeft = (curLeft + renderTargetSize.Width / 5.0F);
            rf = new RectF(
                curLeft,
                renderTargetSize.Height,
                (curLeft + renderTargetSize.Width / 5.0F),
                renderTargetSize.Height - renderTargetSize.Height * ((float)x3 / 100.0F));
            renderTarget.FillRectangle(rf, solidBrush3);
            renderTarget.DrawText(
                String.Format("  {0}%", x3),
                textFormat,
                new RectF(curLeft, renderTargetSize.Height - 30, (curLeft + renderTargetSize.Width / 5.0F), renderTargetSize.Height), blackBrush);

            curLeft = (curLeft + renderTargetSize.Width / 5.0F);
            rf = new RectF(
                curLeft,
                renderTargetSize.Height,
                (curLeft + renderTargetSize.Width / 5.0F),
                renderTargetSize.Height - renderTargetSize.Height * ((float)x4 / 100.0F));
            renderTarget.FillRectangle(rf, linearGradientBrush);
            renderTarget.DrawText(
                String.Format("  {0}%", x4),
                textFormat,
                new RectF(curLeft, renderTargetSize.Height - 30, (curLeft + renderTargetSize.Width / 5.0F), renderTargetSize.Height), blackBrush);


            curLeft = (curLeft + renderTargetSize.Width / 5.0F);
            rf = new RectF(
                curLeft,
                renderTargetSize.Height,
                (curLeft + renderTargetSize.Width / 5.0F),
                renderTargetSize.Height - renderTargetSize.Height * ((float)x5 / 100.0F));
            renderTarget.FillRectangle(rf, solidBrush2);
            renderTarget.DrawText(
                String.Format("  {0}%", x5),
                textFormat,
                new RectF(curLeft, renderTargetSize.Height - 30, (curLeft + renderTargetSize.Width / 5.0F), renderTargetSize.Height), blackBrush);

            renderTarget.EndDraw();

        }

        private void generate_Data(object sender, RoutedEventArgs e)
        {
            Random rand = new Random((int)Environment.TickCount);
            x1 = rand.Next(100);
            x2 = rand.Next(100);
            x3 = rand.Next(100);
            x4 = rand.Next(100);
            x5 = rand.Next(100);

            Render();
        }

    }
}
