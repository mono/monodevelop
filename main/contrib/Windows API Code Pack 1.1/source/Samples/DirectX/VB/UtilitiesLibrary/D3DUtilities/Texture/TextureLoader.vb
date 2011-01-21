' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Runtime.InteropServices


Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent

Namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
	Public Class TextureLoader
		''' <summary>
		''' Creates a ShaderResourceView from a bitmap in a Stream. 
		''' </summary>
		''' <param name="device">The Direct3D device that will own the ShaderResourceView</param>
		''' <param name="stream">Any Windows Imaging Component decodable image</param>
		''' <returns></returns>
		Public Shared Function LoadTexture(ByVal device As D3DDevice, ByVal stream As Stream) As ShaderResourceView
            Dim factory As ImagingFactory

            factory = ImagingFactory.Create()

            Dim bitmapDecoder As BitmapDecoder = factory.CreateDecoderFromStream(stream, DecodeMetadataCacheOption.OnDemand)

			If bitmapDecoder.FrameCount = 0 Then
				Throw New ArgumentException("Image file successfully loaded, but it has no image frames.")
			End If

			Dim bitmapFrameDecode As BitmapFrameDecode = bitmapDecoder.GetFrame(0)
			Dim bitmapSource As BitmapSource = bitmapFrameDecode.ToBitmapSource()

			' create texture description
            Dim textureDescription As New Texture2DDescription() With _
            { _
                .Width = bitmapSource.Size.Width, _
                .Height = bitmapSource.Size.Height, _
                .MipLevels = 1, _
                .ArraySize = 1, _
                .Format = Format.R8G8B8A8UNorm, _
                .SampleDescription = New SampleDescription() With _
                { _
                    .Count = 1, _
                    .Quality = 0 _
                }, _
                .Usage = Usage.Dynamic, _
                .BindingOptions = BindingOptions.ShaderResource, _
                .CpuAccessOptions = CpuAccessOptions.Write, _
                .MiscellaneousResourceOptions = MiscellaneousResourceOptions.None _
            }

			' create texture
			Dim texture As Texture2D = device.CreateTexture2D(textureDescription)

			' Create a format converter
            Dim converter As FormatConverter = factory.CreateFormatConverter()
            converter.Initialize(bitmapSource, PixelFormats.Prgba32Bpp, BitmapDitherType.None, BitmapPaletteType.Custom)

			' get bitmap data
			Dim buffer() As Byte = converter.CopyPixels()

			' Copy bitmap data to texture
            Dim texmap As MappedTexture2D = texture.Map(0, Map.WriteDiscard, Direct3D10.MapOptions.None)
			Marshal.Copy(buffer, 0, texmap.Data, buffer.Length)
			texture.Unmap(0)

			' create shader resource view description
            Dim srvDescription As New ShaderResourceViewDescription() With _
            { _
                .Format = textureDescription.Format, _
                .ViewDimension = ShaderResourceViewDimension.Texture2D, _
                .Texture2D = New Texture2DShaderResourceView() With _
                { _
                    .MipLevels = textureDescription.MipLevels, _
                    .MostDetailedMip = 0 _
                } _
            }

			' create shader resource view from texture
			Return device.CreateShaderResourceView(texture, srvDescription)
		End Function
	End Class
End Namespace
