' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports System.Windows.Media.Media3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities

Namespace WindowsFlag
	''' <summary>
	''' This application demonstrates animation using matrix transformations of 1600 cubes
	''' 
	''' Copyright (c) Microsoft Corporation. All rights reserved.
	''' </summary>
	Partial Public Class Window
		Inherits Form
		#Region "Fields"
		Private viewSync As New Object()
		Private device As D3DDevice
		Private swapChain As SwapChain
		Private renderTargetView As RenderTargetView
		Private depthStencil As Texture2D
		Private depthStencilView As DepthStencilView
        Private backColor_Renamed As New ColorRgba(0.0F, 0.125F, 0.3F, 1.0F)

		Private vertexLayout As InputLayout
		Private vertexBuffer As D3DBuffer
		Private indexBuffer As D3DBuffer

		Private effects As Effects

		Private flagShells As Integer = 20

		Private Eye As New Vector3F(0.0f, 0.0f, -1.0f)
		Private At As New Vector3F(0.0f, 0.0f, 0.0f)
		Private Up As New Vector3F(0.0f, 1.0f, 0.0f)

		Private cube As New Cube()

		Private t As Single = 0f
		Private lastPresentTime As Single = 0f
		Private lastPresentCount As UInteger = 0
		Private dwTimeStart As Integer = Environment.TickCount

		Private isDrag As Boolean = False
		Private lastLocation As New System.Drawing.Point(Integer.MaxValue, Integer.MaxValue)
        Private needsResizing As Boolean
        #End Region

		#Region "Window()"
		Public Sub New()
			InitializeComponent()
		End Sub
		#End Region

		#Region "Window_Load()"
		Private Sub Window_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
			InitializeDevice()
			MoveCameraAroundCenter(-10, -29)
			ScaleCameraDistance(51)
			directControl.Render = AddressOf Me.RenderScene
		End Sub
		#End Region

        #Region "directControl_SizeChanged()"
        Private Sub directControl_SizeChanged(ByVal sender As Object, ByVal e As EventArgs) Handles directControl.SizeChanged
            needsResizing = True
        End Sub
        #End Region

        #Region "InitDevice()"
        ''' <summary>
        ''' Creates Direct3D device and swap chain,
        ''' Initializes buffers,
        ''' Loads and initializes the shader
        ''' </summary>
        Protected Sub InitializeDevice()
            device = D3DDevice.CreateDeviceAndSwapChain(directControl.Handle)
            swapChain = device.SwapChain

            SetViews()

            effects = New Effects(device)

            InitializeVertexLayout()
            InitializeVertexBuffer()
            InitializeIndexBuffer()

            ' Set primitive topology
            device.IA.PrimitiveTopology = PrimitiveTopology.TriangleList

            effects.ViewMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixLookAtLH(Eye, At, Up)
            effects.ProjectionMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixPerspectiveFovLH(CSng(Math.PI) * 0.25F, (CSng(Me.ClientSize.Width) / CSng(Me.ClientSize.Height)), 0.1F, 4000.0F)
        End Sub
#End Region

        #Region "SetViews()"
        Private Sub SetViews()
            ' Create a render target view
            Using pBuffer As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
                renderTargetView = device.CreateRenderTargetView(pBuffer)
            End Using

            ' Create depth stencil texture
            Dim descDepth As New Texture2DDescription() With {.Width = CUInt(directControl.ClientSize.Width), .Height = CUInt(directControl.ClientSize.Height), .MipLevels = 1, .ArraySize = 1, .Format = Format.D32Float, .SampleDescription = New SampleDescription() With {.Count = 1, .Quality = 0}, .BindingOptions = BindingOptions.DepthStencil}

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

        #Region "InitializeVertexLayout()"
        Private Sub InitializeVertexLayout()
            ' Define the input layout
            ' The layout determines the stride in the vertex buffer,
            ' so changes in layout need to be reflected in SetVertexBuffers
            Dim layout() As InputElementDescription = {New InputElementDescription() With {.SemanticName = "POSITION", .SemanticIndex = 0, .Format = Format.R32G32B32Float, .InputSlot = 0, .AlignedByteOffset = 0, .InputSlotClass = InputClassification.PerVertexData, .InstanceDataStepRate = 0}, New InputElementDescription() With {.SemanticName = "NORMAL", .SemanticIndex = 0, .Format = Format.R32G32B32Float, .InputSlot = 0, .AlignedByteOffset = 12, .InputSlotClass = InputClassification.PerVertexData, .InstanceDataStepRate = 0}}

            Dim passDesc As PassDescription = effects.Technique.GetPassByIndex(0).Description

            vertexLayout = device.CreateInputLayout(layout, passDesc.InputAssemblerInputSignature, passDesc.InputAssemblerInputSignatureSize)

            device.IA.InputLayout = vertexLayout
        End Sub
        #End Region

        #Region "InitializeVertexBuffer()"
        Private Sub InitializeVertexBuffer()
            Dim verticesData As IntPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(cube.Vertices))
            Marshal.StructureToPtr(cube.Vertices, verticesData, True)

            Dim bufferDesc As New BufferDescription() With {.Usage = Usage.Default, .ByteWidth = CUInt(Marshal.SizeOf(cube.Vertices)), .BindingOptions = BindingOptions.VertexBuffer, .CpuAccessOptions = CpuAccessOptions.None, .MiscellaneousResourceOptions = MiscellaneousResourceOptions.None}

            Dim InitData As New SubresourceData() With {.SystemMemory = verticesData}

            'D3DBuffer buffer = null;
            vertexBuffer = device.CreateBuffer(bufferDesc, InitData)

            ' Set vertex buffer
            Dim stride As UInteger = CUInt(Marshal.SizeOf(GetType(SimpleVertex)))
            Dim offset As UInteger = 0
            device.IA.SetVertexBuffers(0, New D3DBuffer() {vertexBuffer}, New UInteger() {stride}, New UInteger() {offset})
            Marshal.FreeCoTaskMem(verticesData)
        End Sub
#End Region

        #Region "InitializeIndexBuffer()"
        Private Sub InitializeIndexBuffer()
            Dim indicesData As IntPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(cube.Indices))
            Marshal.StructureToPtr(cube.Indices, indicesData, True)

            Dim bufferDesc As New BufferDescription() With {.Usage = Usage.Default, .ByteWidth = CUInt(Marshal.SizeOf(cube.Indices)), .BindingOptions = BindingOptions.IndexBuffer, .CpuAccessOptions = CpuAccessOptions.None, .MiscellaneousResourceOptions = MiscellaneousResourceOptions.None}

            Dim initData As New SubresourceData() With {.SystemMemory = indicesData}

            indexBuffer = device.CreateBuffer(bufferDesc, initData)
            device.IA.IndexBuffer = New IndexBuffer(indexBuffer, Format.R32UInt, 0)
            Marshal.FreeCoTaskMem(indicesData)
        End Sub
#End Region

        #Region "RenderScene()"
        ''' <summary>
        ''' Render the frame
        ''' </summary>
        Protected Sub RenderScene()
            SyncLock viewSync
                t = (Environment.TickCount - dwTimeStart) / 1000.0F
                If lastPresentTime = 0 Then
                    lastPresentTime = t
                    lastPresentCount = swapChain.LastPresentCount
                End If

                If t - lastPresentTime > 1.0F Then ' if one second has elapsed
                    Dim currentPresentCount As UInteger = swapChain.LastPresentCount
                    Dim presentCount As UInteger = currentPresentCount - lastPresentCount
                    Dim currentframerate As Single = CSng(presentCount) / (t - lastPresentTime)
                    Dim fps As String = String.Format("{0} fps", currentframerate)
                    label1.BeginInvoke(New MethodInvoker(Function() AnonymousMethod1(fps)))
                    lastPresentTime = t
                    lastPresentCount = currentPresentCount
                End If

                If (needsResizing) Then
                    needsResizing = False
                    renderTargetView.Dispose()
                    Dim sd As SwapChainDescription = swapChain.Description
                    swapChain.ResizeBuffers(sd.BufferCount, CType(directControl.ClientSize.Width, UInteger), CType(directControl.ClientSize.Height, UInteger), sd.BufferDescription.Format, sd.Options)
                    SetViews()
                    effects.ProjectionMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixPerspectiveFovLH(CSng(Math.PI) * 0.25F, (CSng(Me.ClientSize.Width) / CSng(Me.ClientSize.Height)), 0.1F, 4000.0F)
                End If

                ' Clear the backbuffer
                device.ClearRenderTargetView(renderTargetView, backColor_Renamed)

                ' Clear the depth buffer to 1.0 (max depth)
                device.ClearDepthStencilView(depthStencilView, ClearOptions.Depth, 1.0F, CByte(0))

                RenderFlag(t, t * 180 / Math.PI, flagShells)
                swapChain.Present(0, PresentOptions.None)
            End SyncLock
        End Sub

        Private Function AnonymousMethod1(ByVal fps As String) As Object
            label1.Text = fps
            Return Nothing
        End Function

        Private Sub RenderFlag(ByVal t As Single, ByVal a As Double, ByVal shells As Integer)
            Dim techDesc As TechniqueDescription = effects.Technique.Description
            For x As Integer = -shells To shells
                For z As Integer = -shells To shells
                    Dim height As Single = (CSng(Math.Sin(0.5 * (x + 4 * t))) + CSng(Math.Cos(0.25 * (z + 2 * t))))
                    Dim vBaseColor As Vector4F = New Vector4F(0.0F, 0.0F, 0.0F, 1.0F)
                    If x < 0 AndAlso z > 0 Then
                        vBaseColor.X = 0.75F + 0.125F * height 'red
                    ElseIf x > 0 AndAlso z > 0 Then
                        vBaseColor.Y = 0.75F + 0.125F * height 'green
                    ElseIf x < 0 AndAlso z < 0 Then
                        vBaseColor.Z = 0.75F + 0.125F * height 'blue
                    ElseIf x > 0 AndAlso z < 0 Then
                        vBaseColor.X = 0.75F + 0.125F * height
                        vBaseColor.Y = 0.75F + 0.125F * height
                    Else
                        Continue For
                    End If
                    effects.BaseColor = vBaseColor

                    Dim yScale As Single = 5.0F + 0.5F * height
                    effects.WorldMatrix = MatrixMath.MatrixScale(0.35F, yScale, 0.35F) * MatrixMath.MatrixTranslate(x, yScale - 10, z)

                    For p As UInteger = 0 To techDesc.Passes - 1UI
                        effects.Technique.GetPassByIndex(p).Apply()
                        device.DrawIndexed(36, 0, 0)
                    Next p
                Next z
            Next x
        End Sub
        #End Region

        #Region "MoveCameraAroundCenter()"
        Private Sub MoveCameraAroundCenter(ByVal leftRight As Double, ByVal topDown As Double)
            ' Use WPF maths for camera rotation.
            ' It is slower than using Matrix4F and Vector4F,
            ' but camera calculations are only done once per camera move
            Dim tg As New Transform3DGroup()
            'left/right drags rotate around the camera's up vector
            Dim leftRightRotationAxis As New Vector3D(Up.x, Up.y, Up.z)
            'top/down drags rotate around the vector that is perpendicular
            'to both Up and Eye (camera location) - their cross product
            Dim topDownRotationAxis As Vector3D = Vector3D.CrossProduct(leftRightRotationAxis, New Vector3D(Eye.x, Eye.y, Eye.z))
            tg.Children.Add(New RotateTransform3D(New AxisAngleRotation3D(leftRightRotationAxis, leftRight)))
            tg.Children.Add(New RotateTransform3D(New AxisAngleRotation3D(topDownRotationAxis, topDown)))
            Dim newEye As Vector3D = tg.Transform(New Vector3D(Eye.x, Eye.y, Eye.z))
            Dim newUp As Vector3D = tg.Transform(New Vector3D(Up.x, Up.y, Up.z))
            Eye.x = CSng(newEye.X)
            Eye.y = CSng(newEye.Y)
            Eye.z = CSng(newEye.Z)
            Up.x = CSng(newUp.X)
            Up.y = CSng(newUp.Y)
            Up.z = CSng(newUp.Z)

            effects.ViewMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixLookAtLH(Eye, At, Up)
        End Sub
#End Region

        #Region "ScaleCameraDistance()"
        Private Sub ScaleCameraDistance(ByVal scale As Single)
            Dim eye4 As New Vector4F(Eye.x, Eye.y, Eye.z, 0)
            Dim transform As Matrix4x4F = MatrixMath.MatrixScale(scale, scale, scale)
            eye4 = MatrixMath.VectorMultiply(transform, eye4)
            Eye = New Vector3F(eye4.x, eye4.y, eye4.z)

            effects.ViewMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixLookAtLH(Eye, At, Up)
        End Sub
#End Region

        #Region "Event handlers for camera control"
        #Region "directControl_MouseMove()"
        Private Sub directControl_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs) Handles directControl.MouseMove, MyBase.MouseMove
            If isDrag Then
                SyncLock viewSync
                    ' Rotate the camera
                    Dim leftRight As Double = lastLocation.X - e.X
                    Dim topDown As Double = lastLocation.Y - e.Y
                    MoveCameraAroundCenter(leftRight, topDown)
                    lastLocation = e.Location
                End SyncLock
            End If
        End Sub
#End Region

        #Region "OnMouseWheel()"
        Protected Overrides Sub OnMouseWheel(ByVal e As MouseEventArgs)
            MyBase.OnMouseWheel(e)
            SyncLock viewSync
                If e.Delta <> 0 Then
                    Dim scale As Single
                    If e.Delta <= 0 Then
                        scale = -(0.01F * e.Delta)
                    Else
                        scale = 100.0F / e.Delta
                    End If
                    ScaleCameraDistance(scale)
                End If
            End SyncLock
        End Sub
#End Region

        #Region "directControl_MouseUp()"
        Private Sub directControl_MouseUp(ByVal sender As Object, ByVal e As MouseEventArgs) Handles directControl.MouseUp
            If e.Button = MouseButtons.Left Then
                isDrag = False
            End If
        End Sub
#End Region

        #Region "directControl_MouseDown()"
        Private Sub directControl_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs) Handles directControl.MouseDown
            If e.Button = MouseButtons.Left Then
                isDrag = True
                lastLocation = e.Location
            End If
        End Sub
#End Region
        #End Region
    End Class
End Namespace
