' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging

Namespace AccelerationMeasurement
	Partial Public Class AccelerationBar
		Inherits Control
		Public Sub New()
			InitializeComponent()
			Me.BackColor = Color.White
		End Sub


		' length og the ticks in pixels
		Private Const tickLength As Integer = 5

		' total number of ticks on each side of 'zero'
		Private Const ticks As Integer = 5

		Protected Overrides Sub OnPaint(ByVal pe As PaintEventArgs)
			MyBase.OnPaint(pe)
			Dim g As Graphics = pe.Graphics

			' draw gauge
			Dim gaugeBox As New Rectangle(ClientRectangle.Left, ClientRectangle.Top + tickLength, ClientRectangle.Width - 2, ClientRectangle.Height - tickLength * 2)
			g.DrawRectangle(Pens.Black, gaugeBox)

			' draw ticks
            g.DrawLine(Pens.Black, Convert.ToInt32(ClientRectangle.Width / 2), 0, Convert.ToInt32(ClientRectangle.Width / 2), Convert.ToInt32(ClientRectangle.Height))


			Dim totalTicks As Integer = (ticks * 2) + 1
			Dim tickSpacing As Single = CSng(ClientRectangle.Width) / (CSng(totalTicks) - 1)
			For n As Integer = 1 To totalTicks - 1

				g.DrawLine(Pens.Black, tickSpacing * CSng(n), CSng(gaugeBox.Bottom), tickSpacing * CSng(n), CSng(ClientRectangle.Bottom))
			Next n

			' draw indicator
			Dim pixelsPerUnit As Single = CSng(gaugeBox.Width) / CSng(totalTicks)
			Dim gaugeCenter As Single = CSng(gaugeBox.Width) / 2f + gaugeBox.Left
			Dim gaugeMiddle As Single = CSng(gaugeBox.Height) / 2f + gaugeBox.Top
			Dim indicatedPosition As Single = gaugeCenter + (pixelsPerUnit * acceleration_Renamed)
			Dim indicatorOffset As Single = Math.Max(Math.Min(indicatedPosition, gaugeBox.Width), gaugeBox.Left)
			Dim indicator() As PointF = { New PointF (indicatorOffset, gaugeBox.Top), New PointF (indicatorOffset + tickLength, gaugeMiddle), New PointF (indicatorOffset, gaugeBox.Bottom), New PointF (indicatorOffset - tickLength, gaugeMiddle), New PointF (indicatorOffset, gaugeBox.Top) }

			Dim fill As Brush = If((indicatorOffset = indicatedPosition), New SolidBrush(Color.Red), New SolidBrush(Color.Gray))
			g.FillPolygon(fill, indicator, FillMode.Winding)
		End Sub

		Public Property Acceleration() As Single
			Set(ByVal value As Single)
				acceleration_Renamed = value
				Invalidate()
			End Set
			Get
				Return acceleration_Renamed
			End Get
		End Property
        Private acceleration_Renamed As Single = 0
	End Class
End Namespace
