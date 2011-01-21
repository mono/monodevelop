Imports Microsoft.VisualBasic
Imports System
Namespace Microsoft.WindowsAPICodePack.Samples.ShellObjectCFDBrowser
	Partial Public Class SubItemsForm
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

		#Region "Windows Form Designer generated code"

		''' <summary>
		''' Required method for Designer support - do not modify
		''' the contents of this method with the code editor.
		''' </summary>
		Private Sub InitializeComponent()
			Me.components = New System.ComponentModel.Container()
			Me.listView1 = New System.Windows.Forms.ListView()
			Me.imageList1 = New System.Windows.Forms.ImageList(Me.components)
			Me.SuspendLayout()
			' 
			' listView1
			' 
			Me.listView1.Dock = System.Windows.Forms.DockStyle.Fill
			Me.listView1.Location = New System.Drawing.Point(0, 0)
			Me.listView1.Name = "listView1"
			Me.listView1.Size = New System.Drawing.Size(286, 340)
			Me.listView1.TabIndex = 0
			Me.listView1.UseCompatibleStateImageBehavior = False
			' 
			' imageList1
			' 
			Me.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit
			Me.imageList1.ImageSize = New System.Drawing.Size(32, 32)
			Me.imageList1.TransparentColor = System.Drawing.Color.Transparent
			' 
			' SubItemsForm
			' 
			Me.AutoScaleDimensions = New System.Drawing.SizeF(6F, 13F)
			Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
			Me.ClientSize = New System.Drawing.Size(286, 340)
			Me.Controls.Add(Me.listView1)
			Me.Name = "SubItemsForm"
			Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
			Me.Text = "SubItems"
			Me.ResumeLayout(False)

		End Sub

		#End Region

		Private listView1 As System.Windows.Forms.ListView
		Private imageList1 As System.Windows.Forms.ImageList
	End Class
End Namespace