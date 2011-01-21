'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Interop
Imports System.Windows.Threading
Imports Microsoft.WindowsAPICodePack.ApplicationServices
Imports Microsoft.WindowsAPICodePack.Shell

Namespace Microsoft.WindowsAPICodePack.Samples.PowerMgmtDemoApp
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	''' 
	Partial Public Class Window1
		Inherits Window
		<DllImport("user32.dll")> _
		Private Shared Function SendMessage(ByVal hWnd As Integer, ByVal hMsg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer
		End Function


		Public Delegate Sub MethodInvoker()

		Private settings As MyPowerSettings
		Private backgroundWorker As New BackgroundWorker()
		Private cancelReason As String = String.Empty
		Private TimerClock As System.Windows.Threading.DispatcherTimer

		Private Shared WM_SYSCOMMAND As Integer = &H0112

		'Using the system pre-defined MSDN constants that can be used by the SendMessage() function 
		Private Shared SC_MONITORPOWER As Integer = &HF170

		Public Sub New()
			InitializeComponent()
			settings = CType(Me.FindResource("powerSettings"), MyPowerSettings)

			backgroundWorker.WorkerReportsProgress = True
			backgroundWorker.WorkerSupportsCancellation = True
			AddHandler backgroundWorker.DoWork, AddressOf backgroundWorker_DoWork
			AddHandler backgroundWorker.RunWorkerCompleted, AddressOf backgroundWorker_RunWorkerCompleted

			' Start a timer/clock so we can periodically ping for the power settings.
			TimerClock = New DispatcherTimer()
			TimerClock.Interval = New TimeSpan(0, 0, 5)
			TimerClock.IsEnabled = True
			AddHandler TimerClock.Tick, AddressOf TimerClock_Tick

		End Sub

		Private Sub TimerClock_Tick(ByVal sender As Object, ByVal e As EventArgs)
			GetPowerSettings()
		End Sub

		Private Sub backgroundWorker_RunWorkerCompleted(ByVal sender As Object, ByVal e As RunWorkerCompletedEventArgs)
			' Once the thread is finished / i.e. indexing is done,
			' update our labels
			If String.IsNullOrEmpty(cancelReason) Then
				SetLabelButtonStatus(IndexerCurrentFileLabel, "Indexing completed!")
				SetLabelButtonStatus(IndexerStatusLabel, "Click ""Start Search Indexer"" to run the indexer again.")
				SetLabelButtonStatus(StartStopIndexerButton, "Start Search Indexer!")
			End If

			' Clear our the cancel reason as the operation has completed.
			cancelReason = ""
		End Sub

		Private Sub backgroundWorker_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)
			SetLabelButtonStatus(IndexerCurrentFileLabel, "Running search indexer ....")

			Dim docs As IKnownFolder

			If ShellLibrary.IsPlatformSupported Then
				docs = KnownFolders.DocumentsLibrary
			Else
				docs = KnownFolders.Documents
			End If

			Dim docsContainer As ShellContainer = TryCast(docs, ShellContainer)

			For Each so As ShellObject In docs
				RecurseDisplay(so)

				If backgroundWorker.CancellationPending Then
					SetLabelButtonStatus(StartStopIndexerButton, "Start Search Indexer")
					SetLabelButtonStatus(IndexerStatusLabel, "Click ""Start Search Indexer"" to run the indexer")
					SetLabelButtonStatus(IndexerCurrentFileLabel,If((cancelReason = "powerSourceChanged"), "Indexing cancelled due to a change in power source", "Indexing cancelled by the user"))

					Return
				End If

				Thread.Sleep(1000) ' sleep a second to indicate indexing the file
			Next so
		End Sub

		Private Sub Window_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
			CapturePowerManagementEvents()
			GetPowerSettings()
		End Sub

		' Get the current property values from PowerManager.
		' This method is called on startup.
		Private Sub GetPowerSettings()
			settings.PowerPersonality = PowerManager.PowerPersonality.ToString()
			settings.PowerSource = PowerManager.PowerSource.ToString()
			settings.BatteryPresent = PowerManager.IsBatteryPresent
			settings.UpsPresent = PowerManager.IsUpsPresent
			settings.MonitorOn = PowerManager.IsMonitorOn
			settings.MonitorRequired = PowerManager.MonitorRequired

			If PowerManager.IsBatteryPresent Then
				settings.BatteryShortTerm = PowerManager.IsBatteryShortTerm
				settings.BatteryLifePercent = PowerManager.BatteryLifePercent

				Dim batteryState As BatteryState = PowerManager.GetCurrentBatteryState()

                Dim batteryStateStr As String = String.Format("ACOnline: {1}{0}Max Charge: {2} mWh{0}Current Charge: {3} mWh{0}Charge Rate: {4} {0}Estimated Time Remaining: {5}{0}Suggested Critical Battery Charge: {6} mWh{0}Suggested Battery Warning Charge: {7} mWh{0}", Environment.NewLine, batteryState.ACOnline, batteryState.MaxCharge, batteryState.CurrentCharge, If(batteryState.ACOnline = True, "N/A", batteryState.ChargeRate.ToString() & " mWh"), If(batteryState.ACOnline = True, "N/A", batteryState.EstimatedTimeRemaining.ToString()), batteryState.SuggestedCriticalBatteryCharge, batteryState.SuggestedBatteryWarningCharge)

				settings.BatteryState = batteryStateStr
			End If
		End Sub

		' Adds event handlers for PowerManager events.
		Private Sub CapturePowerManagementEvents()
			AddHandler PowerManager.IsMonitorOnChanged, AddressOf MonitorOnChanged
			AddHandler PowerManager.PowerPersonalityChanged, AddressOf PowerPersonalityChanged
			AddHandler PowerManager.PowerSourceChanged, AddressOf PowerSourceChanged
			If PowerManager.IsBatteryPresent Then
				AddHandler PowerManager.BatteryLifePercentChanged, AddressOf BatteryLifePercentChanged

				' Set the label for the battery life
				SetLabelButtonStatus(batteryLifePercentLabel, String.Format("{0}%", PowerManager.BatteryLifePercent.ToString()))
			End If

			AddHandler PowerManager.SystemBusyChanged, AddressOf SystemBusyChanged
		End Sub

		' PowerManager event handlers.

		Private Sub MonitorOnChanged(ByVal sender As Object, ByVal e As EventArgs)
			settings.MonitorOn = PowerManager.IsMonitorOn
			AddEventMessage(String.Format("Monitor status changed (new status: {0})",If(PowerManager.IsMonitorOn, "On", "Off")))
		End Sub

		Private Sub PowerPersonalityChanged(ByVal sender As Object, ByVal e As EventArgs)
			settings.PowerPersonality = PowerManager.PowerPersonality.ToString()
			AddEventMessage(String.Format("Power Personality changed (current setting: {0})", PowerManager.PowerPersonality.ToString()))
		End Sub

		Private Sub PowerSourceChanged(ByVal sender As Object, ByVal e As EventArgs)
			settings.PowerSource = PowerManager.PowerSource.ToString()
			AddEventMessage(String.Format("Power source changed (current source: {0})", PowerManager.PowerSource.ToString()))

			'
			If backgroundWorker.IsBusy Then
				If PowerManager.PowerSource = PowerSource.Battery Then
					' for now just stop
					cancelReason = "powerSourceChanged"
					backgroundWorker.CancelAsync()
				Else
					' If we are currently on AC or UPS and switch to UPS or AC, just ignore.
				End If
			Else
				If PowerManager.PowerSource = PowerSource.AC OrElse PowerManager.PowerSource = PowerSource.Ups Then
					SetLabelButtonStatus(IndexerStatusLabel, "Click ""Start Search Indexer"" to run the indexer")
				End If
			End If
		End Sub

		Private Sub BatteryLifePercentChanged(ByVal sender As Object, ByVal e As EventArgs)
			settings.BatteryLifePercent = PowerManager.BatteryLifePercent
			AddEventMessage(String.Format("Battery life percent changed (new value: {0})", PowerManager.BatteryLifePercent))

			' Set the label for the battery life
			SetLabelButtonStatus(batteryLifePercentLabel, String.Format("{0}%", PowerManager.BatteryLifePercent.ToString()))
		End Sub

		' The event handler must use the window's Dispatcher
		' to update the UI directly. This is necessary because
		' the event handlers are invoked on a non-UI thread.
		Private Sub SystemBusyChanged(ByVal sender As Object, ByVal e As EventArgs)
			AddEventMessage(String.Format("System busy changed at {0}", DateTime.Now.ToLongTimeString()))
		End Sub

		Private Sub AddEventMessage(ByVal message As String)
			Me.Dispatcher.Invoke(DispatcherPriority.Normal, CType(Function() AnonymousMethod1(message), Window1.MethodInvoker))
		End Sub
		
		Private Function AnonymousMethod1(ByVal message As String) As Object
			Dim lbi As New ListBoxItem()
			lbi.Content = message
			messagesListBox.Items.Add(lbi)
			messagesListBox.ScrollIntoView(lbi)
			Return Nothing
		End Function

		Private Sub StartIndexer(ByVal sender As Object, ByVal e As RoutedEventArgs)
			If backgroundWorker.IsBusy AndAlso (CType(sender, Button)).Content.ToString() = "Stop Indexer" Then
				cancelReason = "userCancelled"
				backgroundWorker.CancelAsync()
				SetLabelButtonStatus(IndexerStatusLabel, "Click ""Start Search Indexer"" to run the indexer")
				Return
			End If

			' If running on battery, don't start the indexer
			If PowerManager.PowerSource <> PowerSource.Battery Then
				backgroundWorker.RunWorkerAsync()
				SetLabelButtonStatus(IndexerStatusLabel, "Indexer running....")
				SetLabelButtonStatus(StartStopIndexerButton, "Stop Indexer")
			Else
				SetLabelButtonStatus(IndexerCurrentFileLabel, "Running on battery. Not starting the indexer")
			End If
		End Sub

		Private Sub RecurseDisplay(ByVal so As ShellObject)
			If backgroundWorker.CancellationPending Then
				Return
			End If

			SetLabelButtonStatus(IndexerCurrentFileLabel, String.Format("Current {0}: {1}",If(TypeOf so Is ShellContainer, "Folder", "File"), so.ParsingName))

			' Loop through this object's child items if it's a container
			Dim container As ShellContainer = TryCast(so, ShellContainer)

			If container IsNot Nothing Then
				For Each child As ShellObject In container
					RecurseDisplay(child)
				Next child
			End If
		End Sub

		Private Sub SetLabelButtonStatus(ByVal control As ContentControl, ByVal status As String)
			Me.Dispatcher.Invoke(DispatcherPriority.Normal, CType(Function() AnonymousMethod2(control, status), Window1.MethodInvoker))
		End Sub
		
		Private Function AnonymousMethod2(ByVal control As ContentControl, ByVal status As String) As Object
			control.Content = status
			Return Nothing
		End Function
    End Class
End Namespace
