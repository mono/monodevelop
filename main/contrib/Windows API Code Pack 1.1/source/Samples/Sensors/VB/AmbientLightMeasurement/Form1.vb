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
Imports Microsoft.WindowsAPICodePack
Imports Microsoft.WindowsAPICodePack.Sensors
Imports Microsoft.WindowsAPICodePack.Dialogs

Namespace AmbientLightMeasurement
	Partial Public Class Form1
		Inherits Form
		Private sensorMap As New Dictionary(Of Guid, ProgressBar)()
		Private Const maxIntensity As Integer = 200

		Public Sub New()
			InitializeComponent()

			AddHandler SensorManager.SensorsChanged, AddressOf SensorManager_SensorsChanged

			PopulatePanel()
		End Sub

		Private Sub PopulatePanel()
			Try
				Dim alsList As SensorList(Of AmbientLightSensor) = SensorManager.GetSensorsByTypeId(Of AmbientLightSensor)()

				panel.Controls.Clear()

				Dim ambientLightSensors As Integer = 0
				For Each sensor As AmbientLightSensor In alsList
					' Create a new progress bar to monitor light level.
					Dim pb As New ProgressBar()
					pb.Width = 300
					pb.Height = 20
					pb.Top = 10 + 40 * ambientLightSensors
					pb.Left = 10
					pb.Maximum = maxIntensity

					' Identify the control the bar represents.
					Dim label As New Label()
					label.Text = "SensorId = " & sensor.SensorId.ToString()
					label.Top = pb.Top
					label.Left = pb.Right + 20
					label.Height = pb.Height
					label.Width = 300

					' Add controls to panel.
					panel.Controls.AddRange(New Control() { pb, label })

					' Map sensor id to progress bar for lookup in data report handler.
					sensorMap(sensor.SensorId.Value) = pb

					' set intial progress bar value
					sensor.TryUpdateData()
					Dim current As Single = sensor.CurrentLuminousIntensity.Intensity
					pb.Value = Math.Min(CInt(Fix(current)), maxIntensity)

					' Set up automatc data report handling.
					sensor.AutoUpdateDataReport = True
					AddHandler sensor.DataReportChanged, AddressOf DataReportChanged

					ambientLightSensors += 1
				Next sensor

				If ambientLightSensors = 0 Then
					Dim label As New Label()
					label.Text = "No Sensors Found"
					label.Top = 10
					label.Left = 10
					label.Height = 20
					label.Width = 300
					panel.Controls.Add(label)
				End If
			Catch e1 As SensorPlatformException
				' This exception will also be hit in the Shown message handler.
			End Try
		End Sub

		Private Sub SensorManager_SensorsChanged(ByVal change As SensorsChangedEventArgs)
			' The sensors changed event comes in on a non-UI thread. 
			' Whip up an anonymous delegate to handle the UI update.
			BeginInvoke(New MethodInvoker(Function() AnonymousMethod3()))
		End Sub
		
		Private Function AnonymousMethod3() As Object
			PopulatePanel()
			Return Nothing
		End Function

		Private Sub DataReportChanged(ByVal sender As Sensor, ByVal e As EventArgs)
			Dim als As AmbientLightSensor = TryCast(sender, AmbientLightSensor)

			' The data report update comes in on a non-UI thread. 
			' Whip up an anonymous delegate to handle the UI update.
				' find the progress bar for this sensor
				' report data (clamp value to progress bar maximum )
			BeginInvoke(New MethodInvoker(Function() AnonymousMethod4(sender, als)))
		End Sub
		
		Private Function AnonymousMethod4(ByVal sender As Sensor, ByVal als As AmbientLightSensor) As Object
			Dim pb As ProgressBar = sensorMap(sender.SensorId.Value)
			Dim current As Single = als.CurrentLuminousIntensity.Intensity
			pb.Value = Math.Min(CInt(Fix(current)), maxIntensity)
			Return Nothing
		End Function

		Private Sub Form1_Shown(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Shown
			Try
				' ask for sensor permission if needed
				Dim sl As SensorList(Of Sensor) = SensorManager.GetAllSensors()
				SensorManager.RequestPermission(Me.Handle, True, sl)
			Catch spe As SensorPlatformException
				Dim dialog As New TaskDialog()
				dialog.InstructionText = spe.Message
				dialog.Text = "This application will now exit."
				dialog.StandardButtons = TaskDialogStandardButtons.Close
				dialog.Show()
				Application.Exit()
			End Try
		End Sub



	End Class
End Namespace
