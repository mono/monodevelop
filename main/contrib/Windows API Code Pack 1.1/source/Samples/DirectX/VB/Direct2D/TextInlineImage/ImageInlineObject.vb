' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports Microsoft.WindowsAPICodePack.DirectX.DirectWrite
Imports Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports Microsoft.WindowsAPICodePack.DirectX
Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.ComTypes
Imports System.IO
Imports System.Windows

Namespace Microsoft.WindowsAPICodePack.Samples
	''' <summary>
	''' This class implements a custom Direct Write Inline Object (ICustomInlineObject)
	''' that displays a given image 
	''' </summary>
	Public Class ImageInlineObject
        Implements ICustomInlineObject
        Private _renderTarget As RenderTarget
        Private _bitmap As D2DBitmap
        Private _bitmapSize As SizeF

        Public Sub New(ByVal renderTarget As RenderTarget, ByVal wicFactory As ImagingFactory, ByVal resourceName As String)
            _renderTarget = renderTarget

            Using stream As Stream = System.Windows.Application.ResourceAssembly.GetManifestResourceStream(resourceName)
                _bitmap = BitmapUtilities.LoadBitmapFromStream(renderTarget, wicFactory, stream)

                ' Save the bitmap size, for faster access
                _bitmapSize = _bitmap.Size
            End Using
        End Sub

        Public ReadOnly Property BreakConditionAfter1() As DirectX.DirectWrite.BreakCondition Implements DirectX.DirectWrite.ICustomInlineObject.BreakConditionAfter
            Get
                Return BreakCondition.Neutral
            End Get
        End Property

        Public ReadOnly Property BreakConditionBefore1() As DirectX.DirectWrite.BreakCondition Implements DirectX.DirectWrite.ICustomInlineObject.BreakConditionBefore
            Get
                Return BreakCondition.Neutral
            End Get
        End Property

        Public Sub Draw(ByVal originX As Single, ByVal originY As Single, ByVal isSideways As Boolean, ByVal isRightToLeft As Boolean, ByVal clientDrawingEffect As DirectX.Direct2D1.Brush) Implements DirectX.DirectWrite.ICustomInlineObject.Draw
            Dim imageRect As New RectF(originX, originY, originX + _bitmapSize.Width, originY + _bitmapSize.Height)
            _renderTarget.DrawBitmap(_bitmap, 1, BitmapInterpolationMode.Linear, imageRect)
        End Sub

        Public ReadOnly Property Metrics() As DirectX.DirectWrite.InlineObjectMetrics Implements DirectX.DirectWrite.ICustomInlineObject.Metrics
            Get
                Return New InlineObjectMetrics(_bitmapSize.Width, _bitmapSize.Height, _bitmapSize.Height, False)
            End Get
        End Property

        Public ReadOnly Property OverhangMetrics() As DirectX.DirectWrite.OverhangMetrics Implements DirectX.DirectWrite.ICustomInlineObject.OverhangMetrics
            Get
                Return New OverhangMetrics(0, 0, 0, 0)
            End Get
        End Property
    End Class
End Namespace
