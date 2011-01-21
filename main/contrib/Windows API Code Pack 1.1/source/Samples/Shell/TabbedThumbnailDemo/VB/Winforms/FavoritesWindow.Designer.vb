Imports Microsoft.VisualBasic
Imports System
Imports Microsoft.WindowsAPICodePack.Controls.WindowsForms
Namespace Microsoft.WindowsAPICodePack.Samples.TabbedThumbnailDemo
	Partial Public Class FavoritesWindow
		''' <summary>
		''' Required designer variable.
		''' </summary>
		Private components As System.ComponentModel.IContainer = Nothing

		''' <summary>
		''' Clean up any resources being used.
		''' </summary>
		''' <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		Protected Overrides Sub Dispose(ByVal disposing As Boolean)
			If disposing AndAlso (components IsNot Nothing) Then
				components.Dispose()
			End If
			MyBase.Dispose(disposing)
		End Sub



        ''' <summary>
        ''' Required method for Designer support - do not modify
        ''' the contents of this method with the code editor.
        ''' </summary>
        Private Sub InitializeComponent()
            Me.explorerBrowser1 = New ExplorerBrowser()
            Me.SuspendLayout()
            ' 
            ' explorerBrowser1
            ' 
            Me.explorerBrowser1.Dock = System.Windows.Forms.DockStyle.Fill
            Me.explorerBrowser1.Location = New System.Drawing.Point(0, 0)
            Me.explorerBrowser1.Name = "explorerBrowser1"
            Me.explorerBrowser1.Size = New System.Drawing.Size(215, 378)
            Me.explorerBrowser1.TabIndex = 0
            Me.explorerBrowser1.Text = "explorerBrowser1"
            ' 
            ' FavoritesWindow
            ' 
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0F, 13.0F)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(215, 378)
            Me.Controls.Add(Me.explorerBrowser1)
            Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow
            Me.Name = "FavoritesWindow"
            Me.Text = "Favorites"
            Me.TopMost = True
            Me.ResumeLayout(False)

        End Sub



        Private explorerBrowser1 As Microsoft.WindowsAPICodePack.Controls.WindowsForms.ExplorerBrowser
    End Class
End Namespace