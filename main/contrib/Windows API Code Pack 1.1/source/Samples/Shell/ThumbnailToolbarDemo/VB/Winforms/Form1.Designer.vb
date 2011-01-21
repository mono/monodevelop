Imports Microsoft.VisualBasic
Imports System
Namespace Microsoft.WindowsAPICodePack.Samples.ImageViewerDemoWinforms
	Partial Public Class Form1
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
            Me.components = New System.ComponentModel.Container
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
            Me.listView1 = New System.Windows.Forms.ListView
            Me.pictureBox1 = New System.Windows.Forms.PictureBox
            Me.toolStrip1 = New System.Windows.Forms.ToolStrip
            Me.toolStripButtonFirst = New System.Windows.Forms.ToolStripButton
            Me.toolStripButtonPrevious = New System.Windows.Forms.ToolStripButton
            Me.toolStripButtonNext = New System.Windows.Forms.ToolStripButton
            Me.toolStripButtonLast = New System.Windows.Forms.ToolStripButton
            Me.imageList1 = New System.Windows.Forms.ImageList(Me.components)
            CType(Me.pictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.toolStrip1.SuspendLayout()
            Me.SuspendLayout()
            '
            'listView1
            '
            Me.listView1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                        Or System.Windows.Forms.AnchorStyles.Left) _
                        Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.listView1.Location = New System.Drawing.Point(0, 42)
            Me.listView1.Name = "listView1"
            Me.listView1.Size = New System.Drawing.Size(165, 520)
            Me.listView1.TabIndex = 0
            Me.listView1.UseCompatibleStateImageBehavior = False
            '
            'pictureBox1
            '
            Me.pictureBox1.Dock = System.Windows.Forms.DockStyle.Right
            Me.pictureBox1.Location = New System.Drawing.Point(165, 0)
            Me.pictureBox1.Name = "pictureBox1"
            Me.pictureBox1.Size = New System.Drawing.Size(619, 562)
            Me.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
            Me.pictureBox1.TabIndex = 1
            Me.pictureBox1.TabStop = False
            '
            'toolStrip1
            '
            Me.toolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.toolStripButtonFirst, Me.toolStripButtonPrevious, Me.toolStripButtonNext, Me.toolStripButtonLast})
            Me.toolStrip1.Location = New System.Drawing.Point(0, 0)
            Me.toolStrip1.Name = "toolStrip1"
            Me.toolStrip1.Size = New System.Drawing.Size(165, 25)
            Me.toolStrip1.TabIndex = 2
            Me.toolStrip1.Text = "toolStrip1"
            '
            'toolStripButtonFirst
            '
            Me.toolStripButtonFirst.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
            Me.toolStripButtonFirst.Enabled = False
            Me.toolStripButtonFirst.Image = CType(resources.GetObject("toolStripButtonFirst.Image"), System.Drawing.Image)
            Me.toolStripButtonFirst.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None
            Me.toolStripButtonFirst.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.toolStripButtonFirst.Name = "toolStripButtonFirst"
            Me.toolStripButtonFirst.Size = New System.Drawing.Size(23, 22)
            Me.toolStripButtonFirst.Text = "First Image"
            '
            'toolStripButtonPrevious
            '
            Me.toolStripButtonPrevious.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
            Me.toolStripButtonPrevious.Enabled = False
            Me.toolStripButtonPrevious.Image = CType(resources.GetObject("toolStripButtonPrevious.Image"), System.Drawing.Image)
            Me.toolStripButtonPrevious.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None
            Me.toolStripButtonPrevious.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.toolStripButtonPrevious.Name = "toolStripButtonPrevious"
            Me.toolStripButtonPrevious.Size = New System.Drawing.Size(23, 22)
            Me.toolStripButtonPrevious.Text = "Previous Image"
            '
            'toolStripButtonNext
            '
            Me.toolStripButtonNext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
            Me.toolStripButtonNext.Enabled = False
            Me.toolStripButtonNext.Image = CType(resources.GetObject("toolStripButtonNext.Image"), System.Drawing.Image)
            Me.toolStripButtonNext.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None
            Me.toolStripButtonNext.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.toolStripButtonNext.Name = "toolStripButtonNext"
            Me.toolStripButtonNext.Size = New System.Drawing.Size(23, 22)
            Me.toolStripButtonNext.Text = "Next Image"
            '
            'toolStripButtonLast
            '
            Me.toolStripButtonLast.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
            Me.toolStripButtonLast.Enabled = False
            Me.toolStripButtonLast.Image = CType(resources.GetObject("toolStripButtonLast.Image"), System.Drawing.Image)
            Me.toolStripButtonLast.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None
            Me.toolStripButtonLast.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.toolStripButtonLast.Name = "toolStripButtonLast"
            Me.toolStripButtonLast.Size = New System.Drawing.Size(23, 22)
            Me.toolStripButtonLast.Text = "Last Image"
            '
            'imageList1
            '
            Me.imageList1.ImageStream = CType(resources.GetObject("imageList1.ImageStream"), System.Windows.Forms.ImageListStreamer)
            Me.imageList1.TransparentColor = System.Drawing.Color.Transparent
            Me.imageList1.Images.SetKeyName(0, "first.ico")
            Me.imageList1.Images.SetKeyName(1, "prevArrow.ico")
            Me.imageList1.Images.SetKeyName(2, "nextArrow.ico")
            Me.imageList1.Images.SetKeyName(3, "last.ico")
            '
            'Form1
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(784, 562)
            Me.Controls.Add(Me.listView1)
            Me.Controls.Add(Me.toolStrip1)
            Me.Controls.Add(Me.pictureBox1)
            Me.Name = "Form1"
            Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
            Me.Text = "Image Viewer Winforms Demo (with Taskbar Thumbnail toolbar)"
            CType(Me.pictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
            Me.toolStrip1.ResumeLayout(False)
            Me.toolStrip1.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

		#End Region

		Private listView1 As System.Windows.Forms.ListView
        Private WithEvents pictureBox1 As System.Windows.Forms.PictureBox
		Private toolStrip1 As System.Windows.Forms.ToolStrip
		Private WithEvents toolStripButtonFirst As System.Windows.Forms.ToolStripButton
		Private WithEvents toolStripButtonPrevious As System.Windows.Forms.ToolStripButton
		Private WithEvents toolStripButtonNext As System.Windows.Forms.ToolStripButton
		Private WithEvents toolStripButtonLast As System.Windows.Forms.ToolStripButton
		Private imageList1 As System.Windows.Forms.ImageList
	End Class
End Namespace

