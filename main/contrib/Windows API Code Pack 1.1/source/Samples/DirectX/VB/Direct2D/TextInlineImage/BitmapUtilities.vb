' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports System.IO

Namespace Microsoft.WindowsAPICodePack.Samples
	Friend Class BitmapUtilities
		Friend Shared Function LoadBitmapFromFile(ByVal renderTarget As RenderTarget, ByVal wicFactory As ImagingFactory, ByVal fileName As String) As D2DBitmap

            Dim decoder As BitmapDecoder = wicFactory.CreateDecoderFromFileName(fileName, DesiredAccess.Read, DecodeMetadataCacheOption.OnLoad)
			Return CreateBitmapFromDecoder(renderTarget, wicFactory, decoder)
		End Function

		Friend Shared Function LoadBitmapFromStream(ByVal renderTarget As RenderTarget, ByVal wicFactory As ImagingFactory, ByVal ioStream As Stream) As D2DBitmap
            Dim decoder As BitmapDecoder = wicFactory.CreateDecoderFromStream(ioStream, DecodeMetadataCacheOption.OnLoad)
			Return CreateBitmapFromDecoder(renderTarget, wicFactory, decoder)
		End Function

		Private Shared Function CreateBitmapFromDecoder(ByVal renderTarget As RenderTarget, ByVal wicFactory As ImagingFactory, ByVal decoder As BitmapDecoder) As D2DBitmap
			Dim source As BitmapFrameDecode
            Dim converter As FormatConverter
			' Create the initial frame.
			source = decoder.GetFrame(0)

			' Convert the image format to 32bppPBGRA -- which Direct2D expects.
			converter = wicFactory.CreateFormatConverter()
            converter.Initialize(source.ToBitmapSource(), PixelFormats.Pbgra32Bpp, BitmapDitherType.None, BitmapPaletteType.MedianCut)

			' Create a Direct2D bitmap from the WIC bitmap.
			Return renderTarget.CreateBitmapFromWicBitmap(converter.ToBitmapSource())
		End Function
	End Class
End Namespace
