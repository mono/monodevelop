// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using System.IO;

namespace D2DShapes
{
    internal class BitmapUtilities
    {
        internal static D2DBitmap LoadBitmapFromFile(
            RenderTarget renderTarget,
            ImagingFactory wicFactory,
            string fileName)
        {
            BitmapDecoder decoder = wicFactory.CreateDecoderFromFileName(fileName, DesiredAccess.Read, DecodeMetadataCacheOption.OnLoad);
            D2DBitmap ret = CreateBitmapFromDecoder(renderTarget, wicFactory, decoder);
            decoder.Dispose();
            return ret;
        }

        internal static D2DBitmap LoadBitmapFromStream(
            RenderTarget renderTarget,
            ImagingFactory wicFactory,
            Stream ioStream)
        {
            BitmapDecoder decoder = wicFactory.CreateDecoderFromStream(ioStream, DecodeMetadataCacheOption.OnLoad);
            D2DBitmap ret = CreateBitmapFromDecoder(renderTarget, wicFactory, decoder);
            decoder.Dispose();
            return ret;
        }

        private static D2DBitmap CreateBitmapFromDecoder(RenderTarget renderTarget, ImagingFactory wicFactory, BitmapDecoder decoder)
        {
            // Create the initial frame.
            BitmapFrameDecode source = decoder.GetFrame(0);
            // Convert the image format to 32bppPBGRA -- which Direct2D expects.
            FormatConverter converter = wicFactory.CreateFormatConverter();
            converter.Initialize(
                source.ToBitmapSource(),
                PixelFormats.Pbgra32Bpp,
                BitmapDitherType.None,
                BitmapPaletteType.MedianCut
                );

            // Create a Direct2D bitmap from the WIC bitmap.
            D2DBitmap ret = renderTarget.CreateBitmapFromWicBitmap(converter.ToBitmapSource());

            converter.Dispose();
            source.Dispose();

            return ret;
        }
    }
}
