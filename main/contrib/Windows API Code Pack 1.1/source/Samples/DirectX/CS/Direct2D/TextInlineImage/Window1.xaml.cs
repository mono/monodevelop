// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows;
using Microsoft.WindowsAPICodePack.DirectX.Controls;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;

using DWrite = Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent;

namespace Microsoft.WindowsAPICodePack.Samples
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        D2DFactory d2dFactory;
        DWriteFactory dwriteFactory;
        ImagingFactory wicFactory;
        HwndRenderTarget renderTarget;
        SolidColorBrush blackBrush;
        ImageInlineObject inlineImage;
        TextFormat textFormat;
        TextLayout textLayout;

        public Window1()
        {
            InitializeComponent();
            host.Loaded += new RoutedEventHandler(host_Loaded);
            host.SizeChanged += new SizeChangedEventHandler(host_SizeChanged);
        }

        void host_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (renderTarget != null)
            {
                // Resize the render targrt to the actual host size
                renderTarget.Resize(new SizeU((uint)(host.ActualWidth), (uint)(host.ActualHeight)));

                // Resize the text layout max width and height as well
                textLayout.MaxWidth = (float)host.ActualWidth;
                textLayout.MaxHeight = (float)host.ActualHeight;
            }

            InvalidateVisual();
        }

        void host_Loaded(object sender, RoutedEventArgs e)
        {
            CreateDeviceIndependentResources();

            // Start rendering now
            host.Render = Render;
            host.InvalidateVisual();
        }

        private void CreateDeviceIndependentResources()
        {
            // Create the D2D Factory
            d2dFactory = D2DFactory.CreateFactory(D2DFactoryType.SingleThreaded);

            // Create the DWrite Factory
            dwriteFactory = DWriteFactory.CreateFactory();

            wicFactory = ImagingFactory.Create();

            string text = "Inline Object * Sample";

            textFormat = dwriteFactory.CreateTextFormat("Gabriola", 72);

            textFormat.TextAlignment = DWrite.TextAlignment.Center;
            textFormat.ParagraphAlignment = DWrite.ParagraphAlignment.Center;
    
            textLayout = dwriteFactory.CreateTextLayout(
                text,
                textFormat,
                (float) host.ActualWidth,
                (float) host.ActualHeight);
        }


        /// <summary>
        /// This method creates the render target and all associated D2D and DWrite resources
        /// </summary>
        void CreateDeviceResources()
        {
            // Only calls if resources have not been initialize before
            if (renderTarget == null)
            {
                // Create the render target
                SizeU size = new SizeU((uint)host.ActualWidth, (uint)host.ActualHeight);
                RenderTargetProperties props = new RenderTargetProperties();
                HwndRenderTargetProperties hwndProps = new HwndRenderTargetProperties(host.Handle, size, PresentOptions.None);
                renderTarget = d2dFactory.CreateHwndRenderTarget(props, hwndProps);

                // Create the black brush for text
                blackBrush = renderTarget.CreateSolidColorBrush(new ColorF(0 ,0, 0, 1));

                inlineImage = new ImageInlineObject(renderTarget, wicFactory, "TextInlineImage.img1.jpg");

                TextRange textRange = new TextRange(14, 1);
                textLayout.SetInlineObject(inlineImage, textRange);
            }
        }

        private void Render()
        {

            CreateDeviceResources();

            if (renderTarget.IsOccluded)
                return;

            SizeF renderTargetSize = renderTarget.Size;

            renderTarget.BeginDraw();

            renderTarget.Clear(new ColorF(1, 1, 1, 0));

            renderTarget.DrawTextLayout(
                new Point2F(0,0),
                textLayout,
                blackBrush
                );

            renderTarget.EndDraw();

        }

    }
}
