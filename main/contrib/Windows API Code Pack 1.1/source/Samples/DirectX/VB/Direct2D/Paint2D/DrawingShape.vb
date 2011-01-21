' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1

Namespace D2DPaint
	Friend MustInherit Class DrawingShape
		Protected _parent As Paint2DForm
		Protected _fill As Boolean

		Protected Friend MustOverride Sub Draw(ByVal renderTarget As RenderTarget)
		Protected Friend Overridable Sub EndDraw()
		End Sub

		Protected Friend MustOverride WriteOnly Property EndPoint() As Point2F

		Protected Sub New(ByVal parent As Paint2DForm)
			Me._parent = parent
		End Sub

		Protected Sub New(ByVal parent As Paint2DForm, ByVal fill As Boolean)
			Me.New(parent)
			Me._fill = fill
		End Sub
	End Class
End Namespace