// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using System.IO;

namespace Microsoft.WindowsAPICodePack.DirectX.Samples
{
    internal class BitmapUtilities
    {
        internal static D2DBitmap LoadBitmapFromFile(
            RenderTarget renderTarget,
            ImagingFactory wicFactory,
            string fileName)
        {

            BitmapDecoder decoder = wicFactory.CreateDecoderFromFileName(fileName, DesiredAccess.Read, DecodeMetadataCacheOption.OnLoad);
            return CreateBitmapFromDecoder(renderTarget, wicFactory, decoder);
        }

        internal static D2DBitmap LoadBitmapFromStream(
            RenderTarget renderTarget,
            ImagingFactory wicFactory,
            Stream ioStream)
        {
            BitmapDecoder decoder = wicFactory.CreateDecoderFromStream(ioStream, DecodeMetadataCacheOption.OnLoad);
            return CreateBitmapFromDecoder(renderTarget, wicFactory, decoder);
        }

        private static D2DBitmap CreateBitmapFromDecoder(RenderTarget renderTarget, ImagingFactory wicFactory, BitmapDecoder decoder)
        {
            BitmapFrameDecode source;
            FormatConverter converter;
            // Create the initial frame.
            source = decoder.GetFrame(0);

            // Convert the image format to 32bppPBGRA -- which Direct2D expects.
            converter = wicFactory.CreateFormatConverter();
            converter.Initialize(
                source.ToBitmapSource(),
                PixelFormats.Pbgra32Bpp,
                BitmapDitherType.None,
                BitmapPaletteType.MedianCut
                );

            // Create a Direct2D bitmap from the WIC bitmap.
            return renderTarget.CreateBitmapFromWicBitmap(
                converter.ToBitmapSource());
        }
    }
}
