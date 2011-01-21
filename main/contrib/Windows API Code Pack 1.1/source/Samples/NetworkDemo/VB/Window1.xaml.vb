'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System.Windows
Imports System.Windows.Controls
Imports System.Text
Imports Microsoft.WindowsAPICodePack.Net

Namespace Microsoft.WindowsAPICodePack.Samples.NetworkDemo
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	Partial Public Class Window1
		Inherits Window
		Public Sub New()
			InitializeComponent()

			LoadNetworkConnections()
		End Sub

		Private Sub LoadNetworkConnections()
			Dim networks As NetworkCollection = NetworkListManager.GetNetworks(NetworkConnectivityLevels.All)

			For Each n As Network In networks
				' Create a tab
				Dim tabItem As New TabItem()
				tabItem.Header = String.Format("Network {0} ({1})", tabControl1.Items.Count, n.Name)
				tabControl1.Items.Add(tabItem)

				'
				Dim stackPanel2 As New StackPanel()
				stackPanel2.Orientation = Orientation.Vertical

				' List all the properties
				AddProperty("Name: ", n.Name, stackPanel2)
				AddProperty("Description: ", n.Description, stackPanel2)
				AddProperty("Domain type: ", n.DomainType.ToString(), stackPanel2)
				AddProperty("Is connected: ", n.IsConnected.ToString(), stackPanel2)
				AddProperty("Is connected to the internet: ", n.IsConnectedToInternet.ToString(), stackPanel2)
				AddProperty("Network ID: ", n.NetworkId.ToString(), stackPanel2)
				AddProperty("Category: ", n.Category.ToString(), stackPanel2)
				AddProperty("Created time: ", n.CreatedTime.ToString(), stackPanel2)
				AddProperty("Connected time: ", n.ConnectedTime.ToString(), stackPanel2)
				AddProperty("Connectivity: ", n.Connectivity.ToString(), stackPanel2)

				'
				Dim s As New StringBuilder()
				s.AppendLine("Network Connections:")
				Dim connections As NetworkConnectionCollection = n.Connections
				For Each nc As NetworkConnection In connections
					s.AppendFormat(Constants.vbLf + Constants.vbTab & "Connection ID: {0}" & Constants.vbLf + Constants.vbTab & "Domain: {1}" & Constants.vbLf + Constants.vbTab & "Is connected: {2}" & Constants.vbLf + Constants.vbTab & "Is connected to internet: {3}" & Constants.vbLf, nc.ConnectionId, nc.DomainType, nc.IsConnected, nc.IsConnectedToInternet)
					s.AppendFormat(Constants.vbTab & "Adapter ID: {0}" & Constants.vbLf + Constants.vbTab & "Connectivity: {1}" & Constants.vbLf, nc.AdapterId, nc.Connectivity)
				Next nc
				s.AppendLine()

				Dim label As New Label()
				label.Content = s.ToString()

				stackPanel2.Children.Add(label)
				tabItem.Content = stackPanel2
			Next n

		End Sub

		Private Sub AddProperty(ByVal propertyName As String, ByVal propertyValue As String, ByVal parent As StackPanel)
			Dim panel As New StackPanel()
			panel.Orientation = Orientation.Horizontal

			Dim propertyNameLabel As New Label()
			propertyNameLabel.Content = propertyName
			panel.Children.Add(propertyNameLabel)

			Dim propertyValueLabel As New Label()
			propertyValueLabel.Content = propertyValue
			panel.Children.Add(propertyValueLabel)

			parent.Children.Add(panel)
		End Sub
	End Class
End Namespace
