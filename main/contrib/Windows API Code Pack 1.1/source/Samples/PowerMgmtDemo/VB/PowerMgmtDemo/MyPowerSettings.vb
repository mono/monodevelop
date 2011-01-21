'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.ComponentModel

Namespace Microsoft.WindowsAPICodePack.Samples.PowerMgmtDemoApp
	Friend Class MyPowerSettings
		Implements INotifyPropertyChanged
        Private powerPersonality_Renamed As String
        Private powerSource_Renamed As String
        Private batteryPresent_Renamed As Boolean
        Private upsPresent_Renamed As Boolean
        Private monitorOn_Renamed As Boolean
        Private batteryShortTerm_Renamed As Boolean
        Private batteryLifePercent_Renamed As Integer
		Private batteryStateACOnline As String
        Private monitorRequired_Renamed As Boolean

		#Region "INotifyPropertyChanged Members"

		Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

		#End Region

		Public Property PowerPersonality() As String
			Get
				Return powerPersonality_Renamed
			End Get
			Set(ByVal value As String)
				If powerPersonality_Renamed <> value Then
					powerPersonality_Renamed = value
					OnPropertyChanged("PowerPersonality")
				End If
			End Set
		End Property

		Public Property PowerSource() As String
			Get
				Return powerSource_Renamed
			End Get
			Set(ByVal value As String)
				If powerSource_Renamed <> value Then
					powerSource_Renamed = value
					OnPropertyChanged("PowerSource")
				End If
			End Set
		End Property
		Public Property BatteryPresent() As Boolean
			Get
				Return batteryPresent_Renamed
			End Get
			Set(ByVal value As Boolean)
				If batteryPresent_Renamed <> value Then
					batteryPresent_Renamed = value
					OnPropertyChanged("BatteryPresent")
				End If
			End Set
		End Property
		Public Property UpsPresent() As Boolean
			Get
				Return upsPresent_Renamed
			End Get
			Set(ByVal value As Boolean)
				If upsPresent_Renamed <> value Then
					upsPresent_Renamed = value
					OnPropertyChanged("UPSPresent")
				End If
			End Set
		End Property

		Public Property MonitorOn() As Boolean
			Get
				Return monitorOn_Renamed
			End Get
			Set(ByVal value As Boolean)
				If monitorOn_Renamed <> value Then
					monitorOn_Renamed = value
					OnPropertyChanged("MonitorOn")
				End If
			End Set
		End Property
		Public Property BatteryShortTerm() As Boolean
			Get
				Return batteryShortTerm_Renamed
			End Get
			Set(ByVal value As Boolean)
				If batteryShortTerm_Renamed <> value Then
					batteryShortTerm_Renamed = value
					OnPropertyChanged("BatteryShortTerm")
				End If
			End Set
		End Property
		Public Property BatteryLifePercent() As Integer
			Get
				Return batteryLifePercent_Renamed
			End Get
			Set(ByVal value As Integer)
				If batteryLifePercent_Renamed <> value Then
					batteryLifePercent_Renamed = value
					OnPropertyChanged("BatteryLifePercent")
				End If
			End Set
		End Property
		Public Property BatteryState() As String
			Get
				Return batteryStateACOnline
			End Get
			Set(ByVal value As String)
				If batteryStateACOnline <> value Then
					batteryStateACOnline = value
					OnPropertyChanged("BatteryState")
				End If
			End Set
		End Property
		Public Property MonitorRequired() As Boolean
			Get
				Return monitorRequired_Renamed
			End Get
			Set(ByVal value As Boolean)
				If monitorRequired_Renamed <> value Then
					monitorRequired_Renamed = value
					OnPropertyChanged("MonitorRequired")
				End If
			End Set
		End Property

		' Create the OnPropertyChanged method to raise the event

		Protected Sub OnPropertyChanged(ByVal name As String)
			Dim handler As PropertyChangedEventHandler = PropertyChangedEvent

			If handler IsNot Nothing Then
				handler(Me, New PropertyChangedEventArgs(name))
			End If
		End Sub
	End Class
End Namespace
