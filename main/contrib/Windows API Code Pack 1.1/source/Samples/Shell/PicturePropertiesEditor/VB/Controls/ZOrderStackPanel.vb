' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Windows.Controls
Imports System.Windows
Imports System.Windows.Media

Namespace Microsoft.WindowsAPICodePack.Samples.PicturePropertiesEditor
	Public Class ZOrderStackPanel
		Inherits Panel

		#Region "Private Members"

		Private rnd As New Random()

		#End Region

		#Region "Public Constructors"

		Public Sub New()
			MyBase.New()
		End Sub

		#End Region

		#Region "MaxOffset Property"

		Public Shared ReadOnly MaxOffsetProperty As DependencyProperty = DependencyProperty.Register("MaxOffset", GetType(Integer), GetType(ZOrderStackPanel))

		Public Property MaxOffset() As Integer
			Get
				Return CInt(Fix(GetValue(MaxOffsetProperty)))
			End Get
			Set(ByVal value As Integer)
				SetValue(MaxOffsetProperty, value)
			End Set
		End Property

		#End Region

		#Region "MaxRotation Property"

		Public Shared ReadOnly MaxRotationProperty As DependencyProperty = DependencyProperty.Register("MaxRotation", GetType(Double), GetType(ZOrderStackPanel))

		Public Property MaxRotation() As Double
			Get
				Return CDbl(GetValue(MaxRotationProperty))
			End Get
			Set(ByVal value As Double)
				SetValue(MaxRotationProperty, value)
			End Set
		End Property

		#End Region

		Protected Overrides Function MeasureOverride(ByVal availableSize As Size) As Size
			Dim resultSize As New Size(0, 0)

			For Each child As UIElement In Children
				child.Measure(availableSize)
				resultSize.Width = Math.Max(resultSize.Width, child.DesiredSize.Width)
				resultSize.Height = Math.Max(resultSize.Height, child.DesiredSize.Height)
			Next child

			resultSize.Width = If(Double.IsPositiveInfinity(availableSize.Width), resultSize.Width, availableSize.Width)
			resultSize.Height = If(Double.IsPositiveInfinity(availableSize.Height), resultSize.Height, availableSize.Height)

			Return resultSize

		End Function

		Protected Overrides Function ArrangeOverride(ByVal finalSize As Size) As Size
			For Each child As UIElement In Children
				Dim childX As Double = finalSize.Width / 2 - child.DesiredSize.Width / 2
				Dim childY As Double = finalSize.Height / 2 - child.DesiredSize.Height / 2
				child.Arrange(New Rect(childX, childY, child.DesiredSize.Width, child.DesiredSize.Height))

				RotateAndOffsetChild(child)
			Next child


			Return finalSize
		End Function

		Private Sub RotateAndOffsetChild(ByVal child As UIElement)
			If MaxRotation <> 0 AndAlso MaxOffset <> 0 Then
				' create a new Random var called rnd

				Dim randomNumber As Double = rnd.NextDouble()

				Dim xOffset As Double = MaxOffset * (2 * rnd.NextDouble() - 1)
				Dim yOffset As Double = MaxOffset * (2 * rnd.NextDouble() - 1)
				Dim angle As Double = MaxRotation * (2 * rnd.NextDouble() - 1)

				Dim offsetTF As New TranslateTransform(xOffset, yOffset)
				Dim rotateRT As New RotateTransform(angle, child.DesiredSize.Width / 2, child.DesiredSize.Height / 2)

				Dim tfg As New TransformGroup()
				tfg.Children.Add(offsetTF)
				tfg.Children.Add(rotateRT)
				child.RenderTransform = tfg
			End If
		End Sub
	End Class
End Namespace
