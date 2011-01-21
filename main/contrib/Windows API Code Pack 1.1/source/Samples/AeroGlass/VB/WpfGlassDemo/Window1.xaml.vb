Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Data
Imports System.Windows.Documents
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports System.Windows.Navigation
Imports System.Windows.Shapes

Imports System.Windows.Threading

Imports System.Drawing

Imports Microsoft.WindowsAPICodePack.Shell

Namespace WpfGlassDemo
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	Partial Public Class Window1
		Inherits GlassWindow
		Public Sub New()
			InitializeComponent()
		End Sub

		Private Sub GlassWindow_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
			' update GlassRegion on window size change
			AddHandler SizeChanged, AddressOf Window1_SizeChanged

			' update background color on change of desktop composition mode
			AddHandler AeroGlassCompositionChanged, AddressOf Window1_AeroGlassCompositionChanged

			' Set the window background color
			If AeroGlassCompositionEnabled Then
				' exclude the GDI rendered controls from the initial GlassRegion
				ExcludeElementFromAeroGlass(eb1)
				SetAeroGlassTransparency()
			Else
				Me.Background = System.Windows.Media.Brushes.Teal
			End If

			' initialize the explorer browser control
			eb1.NavigationTarget = CType(KnownFolders.Computer, ShellObject)

			' set the state of the Desktop Composition check box.
			EnableCompositionCheck.IsChecked = AeroGlassCompositionEnabled
		End Sub

        Private Sub Window1_AeroGlassCompositionChanged(ByVal sender As Object, ByVal e As AeroGlassCompositionChangedEventArgs)
            ' When the desktop composition mode changes the background color  and window exclusion must be changed appropriately.
            If e.GlassAvailable Then
                Me.EnableCompositionCheck.IsChecked = True
                SetAeroGlassTransparency()
                ExcludeElementFromAeroGlass(eb1)
                InvalidateVisual()
            Else
                Me.EnableCompositionCheck.IsChecked = False
                Me.Background = System.Windows.Media.Brushes.Teal
            End If
        End Sub

		Private Sub Window1_SizeChanged(ByVal sender As Object, ByVal e As SizeChangedEventArgs)
			ExcludeElementFromAeroGlass(eb1)
		End Sub

		Private Sub CheckBox_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			' Toggles the desktop composition mode.
			AeroGlassCompositionEnabled = EnableCompositionCheck.IsChecked.Value
		End Sub

	End Class
End Namespace
