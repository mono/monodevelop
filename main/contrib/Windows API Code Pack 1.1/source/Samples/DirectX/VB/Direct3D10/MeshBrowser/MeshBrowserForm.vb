' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Linq
Imports System.Xml.Linq
Imports System.Xml.XPath
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities

Namespace MeshBrowser
	''' <summary>
	''' This application demonstrates how to use the library to implement a useful utility application
	''' 
	''' Copyright (c) Microsoft Corporation. All rights reserved.
	''' </summary>
	Partial Public Class MeshBrowserForm
		Inherits Form
		#Region "Fields"
		Private device As D3DDevice
		Private swapChain As SwapChain
		Private renderTargetView As RenderTargetView
		Private depthStencil As Texture2D
		Private depthStencilView As DepthStencilView
        Private backColor_Renamed As New ColorRgba(0.0F, 0.125F, 0.3F, 1.0F)

		Private mesh As XMesh
		Private meshManager As XMeshManager

        Private worldMatrix As Matrix4x4F = Matrix4x4F.Identity
		Private knownFiles As XDocument
		Private dwLastTime As Integer = Environment.TickCount

		Private isDrag As Boolean = False
		Private lastLocation As New System.Drawing.Point(Integer.MaxValue, Integer.MaxValue)
		Private myBrush As System.Drawing.Brush
        Private meshLock As New Object()
        Private needsResizing As Boolean

		#End Region

		#Region "MeshBrowserForm()"
		''' <summary>
		''' Initializes a new instance of the <see cref="MeshBrowserForm"/> class.
		''' </summary>
		Public Sub New()
			InitializeComponent()
		End Sub
		#End Region

		#Region "Window_Load()"
		Private Sub Window_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
			myBrush = New System.Drawing.SolidBrush(listBoxValid.ForeColor)
			InitDevice()
			directControl.Render = AddressOf Me.RenderScene
			Dim dxsdkdir As String = Environment.GetEnvironmentVariable("DXSDK_DIR")
			If Not String.IsNullOrEmpty(dxsdkdir) Then
				openFileDialog1.InitialDirectory = System.IO.Path.Combine(dxsdkdir, "Samples\Media")
			End If
			If File.Exists("knownFiles.xml") Then
				LoadKnown()
			Else
				knownFiles = New XDocument(New XElement("KnownFiles", New XElement("Valid"), New XElement("Invalid")))
				SaveKnown()
			End If
		End Sub
		#End Region

		#Region "InitDevice()"
		''' <summary>
		''' Create Direct3D device and swap chain
		''' </summary>
		Protected Sub InitDevice()
            device = D3DDevice.CreateDeviceAndSwapChain(directControl.Handle)
            swapChain = device.SwapChain

            SetViews()

			meshManager = New XMeshManager(device)

			InitMatrices()
		End Sub
		#End Region

        #Region "SetViews()"
        Private Sub SetViews()
            ' Create a render target view
            Using pBuffer As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
                renderTargetView = device.CreateRenderTargetView(pBuffer)
            End Using

            ' Create depth stencil texture
            Dim descDepth As New Texture2DDescription() With _
            { _
                .Width = CUInt(directControl.ClientSize.Width), _
                .Height = CUInt(directControl.ClientSize.Height), _
                .MipLevels = 1, _
                .ArraySize = 1, _
                .Format = Format.D32Float, _
                .SampleDescription = New SampleDescription() With {.Count = 1, .Quality = 0}, _
                .BindingOptions = BindingOptions.DepthStencil _
            }

            depthStencil = device.CreateTexture2D(descDepth)

            ' Create the depth stencil view
            Dim depthStencilViewDesc As New DepthStencilViewDescription() With {.Format = descDepth.Format, .ViewDimension = DepthStencilViewDimension.Texture2D}
            depthStencilView = device.CreateDepthStencilView(depthStencil, depthStencilViewDesc)

            'bind the views to the device
            device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {renderTargetView}, depthStencilView)

            ' Setup the viewport
            Dim vp As New Viewport() With {.Width = CUInt(directControl.ClientSize.Width), .Height = CUInt(directControl.ClientSize.Height), .MinDepth = 0.0F, .MaxDepth = 1.0F, .TopLeftX = 0, .TopLeftY = 0}

            device.RS.Viewports = New Viewport() {vp}
        End Sub
        #End Region

        #Region "InitMatrices()"
        Private Sub InitMatrices()
            ' Initialize the view matrix
            Dim Eye As New Vector3F(0.0F, 3.0F, -15.0F)
            Dim At As New Vector3F(0.0F, 0.0F, 0.0F)
            Dim Up As New Vector3F(0.0F, 1.0F, 0.0F)

            Dim viewMatrix As Matrix4x4F
            Dim projectionMatrix As Matrix4x4F
            viewMatrix = Camera.MatrixLookAtLH(Eye, At, Up)

            ' Initialize the projection matrix
            projectionMatrix = Camera.MatrixPerspectiveFovLH( _
                CSng(Math.PI) * 0.25F, _
                CSng(directControl.ClientSize.Width) / CSng(directControl.ClientSize.Height), _
                0.5F, 1000.0F)

            meshManager.SetViewAndProjection(viewMatrix, projectionMatrix)
        End Sub
        #End Region

        #Region "RenderScene()"
        ''' <summary>
        ''' Render the frame
        ''' </summary>
        Protected Sub RenderScene()
            Dim dwCurrentTime As Integer = Environment.TickCount
            Dim t As Single = (dwCurrentTime - dwLastTime) / 1000.0F
            dwLastTime = dwCurrentTime

            If (needsResizing) Then
                needsResizing = False
                renderTargetView.Dispose()
                Dim sd As SwapChainDescription = swapChain.Description
                swapChain.ResizeBuffers(sd.BufferCount, CType(directControl.ClientSize.Width, UInteger), CType(directControl.ClientSize.Height, UInteger), sd.BufferDescription.Format, sd.Options)
                SetViews()
                InitMatrices()
            End If

            ' Clear the backbuffer
            device.ClearRenderTargetView(renderTargetView, backColor_Renamed)

            ' Clear the depth buffer to 1.0 (max depth)
            device.ClearDepthStencilView(depthStencilView, ClearOptions.Depth, 1.0F, CByte(0))

            SyncLock meshLock
                If mesh IsNot Nothing Then
                    If cbRotate.Checked Then
                        worldMatrix *= MatrixMath.MatrixRotationY(-t)
                    End If
                    mesh.Render(worldMatrix)
                End If
            End SyncLock

            Dim [error] As Microsoft.WindowsAPICodePack.DirectX.ErrorCode
            swapChain.TryPresent(1, PresentOptions.None, [error])
        End Sub
        #End Region

        #Region "Mesh loading"
        #Region "LoadMeshAndUpdateKnownFiles()"
        Private Sub LoadMeshAndUpdateKnownFiles(ByVal filename As String)
            LoadMeshAndUpdateKnownFiles(filename, True)
        End Sub

        Private Sub LoadMeshAndUpdateKnownFiles(ByVal filename As String, ByVal showException As Boolean)
            Try
                LoadMesh(filename)
                MarkFileValid(filename)
            Catch ex As Exception
                If showException Then
                    ShowTextInDialog(ex.ToString(), "Could not load mesh")
                End If
                MarkFileInvalid(filename)
            End Try
        End Sub
#End Region

        #Region "LoadMesh()"
        Private Sub LoadMesh(ByVal filename As String)
            SyncLock meshLock
                If mesh IsNot Nothing Then
                    mesh.Dispose()
                    mesh = Nothing
                End If
                worldMatrix = Matrix4x4F.Identity

                Try
                    mesh = meshManager.Open(filename)
                    mesh.ShowWireFrame = cbWireframe.Checked
                Catch
                    mesh = Nothing
                    Throw
                End Try
            End SyncLock
        End Sub
#End Region
        #End Region

        #Region "Known files list handling"
        #Region "MarkFileValid()"
        Private Sub MarkFileValid(ByVal filename As String)
            Dim nowKnown As New XDocument(New XElement("KnownFiles", New XElement("Valid"), New XElement("Invalid")))
            Dim q1 = From files In knownFiles.Root.XPathSelectElements("./Invalid/File") _
                     Where CStr(files.Attribute("path")) = filename _
                     Select files
            q1.Remove()
            Dim q2 = From files In knownFiles.Root.XPathSelectElements("./Valid/File") _
                     Where CStr(files.Attribute("path")) = filename _
                     Select files
            q2.Remove()
            knownFiles.Root.XPathSelectElement("./Valid").Add(New XElement("File", New XAttribute("path", filename)))
            knownFiles.Save("knownFiles.xml")

            If Not listBoxValid.Items.Contains(filename) Then
                listBoxValid.Items.Add(filename)
            End If
            If Not listBoxInvalid.Items.Contains(filename) Then
                listBoxInvalid.Items.Remove(filename)
            End If
        End Sub
#End Region

        #Region "MarkFileInvalid()"
        Private Sub MarkFileInvalid(ByVal filename As String)
            Dim q1 = From files In knownFiles.Root.XPathSelectElements("./Invalid/File") _
                     Where CStr(files.Attribute("path")) = filename _
                     Select files
            q1.Remove()
            Dim q2 = From files In knownFiles.Root.XPathSelectElements("./Valid/File") _
                     Where CStr(files.Attribute("path")) = filename _
                     Select files
            q2.Remove()
            knownFiles.Root.XPathSelectElement("./Invalid").Add(New XElement("File", New XAttribute("path", filename)))
            knownFiles.Save("knownFiles.xml")

            If Not listBoxInvalid.Items.Contains(filename) Then
                listBoxInvalid.Items.Add(filename)
            End If
            If Not listBoxValid.Items.Contains(filename) Then
                listBoxValid.Items.Remove(filename)
            End If
        End Sub
#End Region

        #Region "SaveKnown()"
        Private Sub SaveKnown()
            knownFiles.Save("knownFiles.xml")
        End Sub
#End Region

        #Region "LoadKnown()"
        Private Sub LoadKnown()
            knownFiles = XDocument.Load("knownFiles.xml")
            listBoxInvalid.Items.Clear()
            listBoxValid.Items.Clear()
            For Each file As XElement In knownFiles.Root.XPathSelectElements("./Invalid/File")
                listBoxInvalid.Items.Add(file.Attribute("path").Value)
            Next file
            For Each file As XElement In knownFiles.Root.XPathSelectElements("./Valid/File")
                listBoxValid.Items.Add(file.Attribute("path").Value)
            Next file
        End Sub
#End Region
#End Region

        #Region "event handlers"
        #Region "Mesh loading events"
        #Region "buttonOpen_Click()"
        Private Sub buttonOpen_Click(ByVal sender As Object, ByVal e As EventArgs) Handles buttonOpen.Click
            If openFileDialog1.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
                LoadMeshAndUpdateKnownFiles(openFileDialog1.FileName)
            End If
        End Sub
#End Region

        #Region "buttonScanDXSDK_Click()"
        Private Sub buttonScanDXSDK_Click(ByVal sender As Object, ByVal e As EventArgs) Handles buttonScanDXSDK.Click
            Dim dxsdkdir As String = Environment.GetEnvironmentVariable("DXSDK_DIR")
            If String.IsNullOrEmpty(dxsdkdir) Then
                buttonScanDXSDK.Enabled = False
                MessageBox.Show("DirectX SDK not installed or environment variable DXSDK_DIR not set")
            Else
                Dim files() As String = Directory.GetFiles(System.IO.Path.Combine(dxsdkdir, "Samples\Media"), "*.x", SearchOption.AllDirectories)
                For Each file As String In files
                    LoadMeshAndUpdateKnownFiles(file, False)
                Next file
            End If
        End Sub
#End Region

        #Region "listBoxValid_SelectedIndexChanged()"
        Private Sub listBoxValid_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles listBoxValid.SelectedIndexChanged
            If listBoxValid.SelectedIndex <> -1 Then
                LoadMeshAndUpdateKnownFiles(listBoxValid.SelectedItem.ToString())
                listBoxInvalid.SelectedIndex = -1
            End If
        End Sub
#End Region

        #Region "listBoxInvalid_SelectedIndexChanged()"
        Private Sub listBoxInvalid_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles listBoxInvalid.SelectedIndexChanged
            If listBoxInvalid.SelectedIndex <> -1 Then
                LoadMeshAndUpdateKnownFiles(listBoxInvalid.SelectedItem.ToString())
                listBoxValid.SelectedIndex = -1
            End If
        End Sub
#End Region
        #End Region

        #Region "Camera operation events"
        #Region "directControl_MouseUp()"
        Private Sub directControl_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs) Handles directControl.MouseDown
            If e.Button = MouseButtons.Left Then
                isDrag = True
                lastLocation = e.Location
            End If
        End Sub
#End Region

        #Region "directControl_MouseUp()"
        Private Sub directControl_MouseUp(ByVal sender As Object, ByVal e As MouseEventArgs) Handles directControl.MouseUp
            If e.Button = MouseButtons.Left Then
                isDrag = False
            End If
        End Sub
#End Region

        #Region "directControl_MouseMove()"
        Private Sub directControl_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs) Handles directControl.MouseMove
            If isDrag Then
                worldMatrix *= MatrixMath.MatrixRotationX(0.01F * (lastLocation.Y - e.Y))
                worldMatrix *= MatrixMath.MatrixRotationY(0.01F * (lastLocation.X - e.X))
                lastLocation = e.Location
                cbRotate.Checked = False
            End If
        End Sub
#End Region

        #Region "OnMouseWheel()"
        Protected Overrides Sub OnMouseWheel(ByVal e As MouseEventArgs)
            MyBase.OnMouseWheel(e)
            If e.Delta <> 0 Then
                Dim scale As Single
                If e.Delta > 0 Then
                    scale = (0.01F * e.Delta)
                Else
                    scale = -100.0F / e.Delta
                End If
                worldMatrix *= MatrixMath.MatrixScale(scale, scale, scale)
            End If
        End Sub
#End Region
        #End Region

        #Region "listBox_DrawItem()"
        ''' <summary>
        ''' Handles the DrawItem event of the listBox control.
        ''' Displays file names only instead of full file paths for known meshes.
        ''' </summary>
        ''' <param name="sender">The source of the event.</param>
        ''' <param name="e">The <see cref="System.Windows.Forms.DrawItemEventArgs"/> instance containing the event data.</param>
        Private Sub listBox_DrawItem(ByVal sender As Object, ByVal e As DrawItemEventArgs) Handles listBoxValid.DrawItem, listBoxInvalid.DrawItem
            Dim lb As ListBox = TryCast(sender, ListBox)
            e.DrawBackground()
            e.Graphics.DrawString(Path.GetFileName(CStr(lb.Items(e.Index))), e.Font, myBrush, e.Bounds, System.Drawing.StringFormat.GenericDefault)
            e.DrawFocusRectangle()
        End Sub
        #End Region

        #Region "cbWireframe_CheckedChanged()"
        Private Sub cbWireframe_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) Handles cbRotate.CheckedChanged, cbWireframe.CheckedChanged
            SyncLock meshLock
                If mesh IsNot Nothing Then
                    mesh.ShowWireFrame = cbWireframe.Checked
                End If
            End SyncLock
        End Sub
        #End Region
        #End Region

        #Region "ShowTextInDialog()"
        Public Shared Function ShowTextInDialog(ByVal text As String, ByVal caption As String) As Form
            Dim form As New Form() With {.WindowState = FormWindowState.Maximized, .Text = caption}
            Dim box As New TextBox() With {.Dock = DockStyle.Fill, .AcceptsReturn = True, .AcceptsTab = True, .Multiline = True, .Parent = form, .Text = text, .ScrollBars = ScrollBars.Both}
            form.ShowDialog()
            Return form
        End Function
        #End Region

        #Region "directControl_SizeChanged()"
        Private Sub directControl_SizeChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles directControl.SizeChanged
            needsResizing = True
        End Sub
        #End Region
    End Class
End Namespace
