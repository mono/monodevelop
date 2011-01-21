Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms

Imports Microsoft.WindowsAPICodePack.Shell

Namespace WindowsFormsGlassDemo
	Partial Public Class Form1
		Inherits GlassForm
		Public Sub New()
			InitializeComponent()

			explorerBrowser1.Navigate(CType(KnownFolders.Desktop, ShellObject))

			AddHandler AeroGlassCompositionChanged, AddressOf Form1_AeroGlassCompositionChanged

			If AeroGlassCompositionEnabled Then
				ExcludeControlFromAeroGlass(panel1)
			Else
				Me.BackColor = Color.Teal
			End If

			' set the state of the Desktop Composition check box.
			compositionEnabled.Checked = AeroGlassCompositionEnabled
		End Sub

        Private Sub Form1_AeroGlassCompositionChanged(ByVal sender As Object, ByVal e As AeroGlassCompositionChangedEventArgs)
            ' When the desktop composition mode changes the window exclusion must be changed appropriately.
            If e.GlassAvailable Then
                compositionEnabled.Checked = True
                ExcludeControlFromAeroGlass(panel1)
                Invalidate()
            Else
                compositionEnabled.Checked = False
                Me.BackColor = Color.Teal
            End If
        End Sub

		Private Sub Form1_Resize(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Resize
			Dim panelRect As Rectangle = ClientRectangle
			panelRect.Inflate(-30, -30)
			panel1.Bounds = panelRect
			ExcludeControlFromAeroGlass(panel1)
		End Sub

		Private Sub compositionEnabled_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) Handles compositionEnabled.CheckedChanged
			' Toggles the desktop composition mode.
			AeroGlassCompositionEnabled = compositionEnabled.Checked
		End Sub
	End Class
End Namespace
