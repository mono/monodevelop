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

Namespace AccelerationMeasurement
	Partial Public Class Form1
		Inherits Form
		Public Sub New()
			InitializeComponent()

			AddHandler SensorManager.SensorsChanged, AddressOf SensorManager_SensorsChanged

			HookUpAccelerometer()
		End Sub

		Private Sub SensorManager_SensorsChanged(ByVal change As SensorsChangedEventArgs)
			' The sensors changed event comes in on a non-UI thread. 
			' Whip up an anonymous delegate to handle the UI update.
			BeginInvoke(New MethodInvoker(Function() AnonymousMethod1()))
		End Sub
		
		Private Function AnonymousMethod1() As Object
			HookUpAccelerometer()
			Return Nothing
		End Function

		Private Sub HookUpAccelerometer()
			Try
				Dim sl As SensorList(Of Accelerometer3D) = SensorManager.GetSensorsByTypeId(Of Accelerometer3D)()
				If sl.Count > 0 Then
					Dim accel As Accelerometer3D = sl(0)
					accel.AutoUpdateDataReport = True
					AddHandler accel.DataReportChanged, AddressOf DataReport_Changed
				End If

				availabilityLabel.Text = "Accelerometers available = " & sl.Count
			Catch e1 As SensorPlatformException
				' This exception will also be hit in the Shown message handler.
			End Try
		End Sub

		Private Sub DataReport_Changed(ByVal sender As Sensor, ByVal e As EventArgs)
			' The data report update comes in on a non-UI thread. 
			' Whip up an anonymous delegate to handle the UI update.
			BeginInvoke(New MethodInvoker(Function() AnonymousMethod2(sender)))
		End Sub
		
		Private Function AnonymousMethod2(ByVal sender As Sensor) As Object
			Dim accel As Accelerometer3D = TryCast(sender, Accelerometer3D)
            accelX.Acceleration = accel.CurrentAcceleration(AccelerationAxis.XAxis)
            accelY.Acceleration = accel.CurrentAcceleration(AccelerationAxis.YAxis)
            accelZ.Acceleration = accel.CurrentAcceleration(AccelerationAxis.ZAxis)
			Return Nothing
		End Function

		Private Sub Form1_Shown(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Shown
			Try
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
