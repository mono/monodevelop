Imports Microsoft.VisualBasic
Imports System.Windows
Imports Microsoft.WindowsAPICodePack.DirectX.Controls
Imports System.Runtime.InteropServices
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports System.Collections.ObjectModel
Imports System
Imports Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
Imports System.IO
Imports System.Windows.Media.Media3D
Namespace D3D10Tutorial07_WPF
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
    Partial Public Class TutorialWindow
        Inherits Window
#Region "Fields"
        Private device As D3DDevice
        Private swapChain As SwapChain
        Private renderTargetView As RenderTargetView
        Private textureRV As ShaderResourceView
        Private backColor As New ColorRgba(0.0F, 0.125F, 0.3F, 1.0F)
        Private meshColor As New Vector4F() With {.x = 0.7F, .y = 0.7F, .z = 0.7F, .w = 1.0F}

        'INSTANT VB NOTE: The variable effect was renamed since Visual Basic does not allow class members with the same name:
        Private effect_Renamed As Effect
        Private technique As EffectTechnique

        Private vertexLayout As InputLayout
        Private vertexBuffer As D3DBuffer
        Private indexBuffer As D3DBuffer

        'variables from the .fx file
        Private worldVariable As EffectMatrixVariable
        Private viewVariable As EffectMatrixVariable
        Private projectionVariable As EffectMatrixVariable

        Private meshColorVariable As EffectVectorVariable
        Private diffuseVariable As EffectShaderResourceVariable

        Private cube As New Cube()

        Private viewMatrix As Matrix4x4F
        Private projectionMatrix As Matrix4x4F

        Private currentTime As Single = 0.0F
        Private startTime As UInteger = CUInt(Environment.TickCount)
        Private needsResizing As Boolean

#End Region

#Region "Constructor"

        Public Sub New()
            InitializeComponent()
            AddHandler host.Loaded, AddressOf host_Loaded
        End Sub

#End Region

#Region "Event Handlers"

        Private Sub host_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
            InitDevice()
            host.Render = AddressOf RenderScene
        End Sub

#End Region

#Region "InitDevice()"
        ''' <summary>
        ''' Create Direct3D device and swap chain
        ''' </summary>
        Protected Sub InitDevice()
            device = D3DDevice1.CreateDeviceAndSwapChain1(host.Handle)
            swapChain = device.SwapChain

            SetViews()
            ' Create the effect
            Using effectStream As FileStream = File.OpenRead("Tutorial07.fxo")
                effect_Renamed = device.CreateEffectFromCompiledBinary(New BinaryReader(effectStream))
            End Using

            ' Obtain the technique
            technique = effect_Renamed.GetTechniqueByName("Render")

            ' Obtain the variables
            worldVariable = effect_Renamed.GetVariableByName("World").AsMatrix()
            viewVariable = effect_Renamed.GetVariableByName("View").AsMatrix()
            projectionVariable = effect_Renamed.GetVariableByName("Projection").AsMatrix()
            meshColorVariable = effect_Renamed.GetVariableByName("vMeshColor").AsVector()
            diffuseVariable = effect_Renamed.GetVariableByName("txDiffuse").AsShaderResource()

            InitVertexLayout()
            InitVertexBuffer()
            InitIndexBuffer()

            ' Set primitive topology
            device.IA.PrimitiveTopology = PrimitiveTopology.TriangleList

            ' Load the Texture
            Using stream As FileStream = File.OpenRead("seafloor.png")
                textureRV = TextureLoader.LoadTexture(device, stream)
            End Using

            InitMatrices()

            diffuseVariable.Resource = textureRV
        End Sub
#End Region

#Region "SetViews()"
        Private Sub SetViews()
            ' Create a render target view
            Using buffer As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
                renderTargetView = device.CreateRenderTargetView(buffer)
            End Using

            'bind the views to the device
            device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {renderTargetView})

            ' Setup the viewport
            Dim vp As New Viewport() With {.Width = CUInt(host.ActualWidth), .Height = CUInt(host.ActualHeight), .MinDepth = 0.0F, .MaxDepth = 1.0F, .TopLeftX = 0, .TopLeftY = 0}

            device.RS.Viewports = New Viewport() {vp}
        End Sub
#End Region

#Region "InitVertexLayout()"
        Private Sub InitVertexLayout()
            ' Define the input layout
            ' The layout determines the stride in the vertex buffer,
            ' so changes in layout need to be reflected in SetVertexBuffers
            Dim layout() As InputElementDescription = {New InputElementDescription() With {.SemanticName = "POSITION", .SemanticIndex = 0, .Format = Format.R32G32B32Float, .InputSlot = 0, .AlignedByteOffset = 0, .InputSlotClass = InputClassification.PerVertexData, .InstanceDataStepRate = 0}, New InputElementDescription() With {.SemanticName = "TEXCOORD", .SemanticIndex = 0, .Format = Format.R32G32Float, .InputSlot = 0, .AlignedByteOffset = 12, .InputSlotClass = InputClassification.PerVertexData, .InstanceDataStepRate = 0}}

            Dim passDesc As PassDescription = technique.GetPassByIndex(0).Description

            vertexLayout = device.CreateInputLayout(layout, passDesc.InputAssemblerInputSignature, passDesc.InputAssemblerInputSignatureSize)

            device.IA.InputLayout = vertexLayout
        End Sub
#End Region

#Region "InitVertexBuffer()"
        Private Sub InitVertexBuffer()
            Dim verticesData As IntPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(cube.Vertices))
            Marshal.StructureToPtr(cube.Vertices, verticesData, True)

            Dim bufferDesc As New BufferDescription() With {.Usage = Usage.Default, .ByteWidth = CUInt(Marshal.SizeOf(cube.Vertices)), .BindingOptions = BindingOptions.VertexBuffer, .CpuAccessOptions = CpuAccessOptions.None, .MiscellaneousResourceOptions = MiscellaneousResourceOptions.None}

            Dim InitData As New SubresourceData() With {.SystemMemory = verticesData}

            vertexBuffer = device.CreateBuffer(bufferDesc, InitData)

            ' Set vertex buffer
            Dim stride As UInteger = CUInt(Marshal.SizeOf(GetType(SimpleVertex)))
            Dim offset As UInteger = 0
            device.IA.SetVertexBuffers(0, New D3DBuffer() {vertexBuffer}, New UInteger() {stride}, New UInteger() {offset})
            Marshal.FreeCoTaskMem(verticesData)
        End Sub
#End Region

#Region "InitIndexBuffer()"
        Private Sub InitIndexBuffer()
            Dim indicesData As IntPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(cube.Indices))
            Marshal.StructureToPtr(cube.Indices, indicesData, True)

            Dim bufferDesc As New BufferDescription() With {.Usage = Usage.Default, .ByteWidth = CUInt(Marshal.SizeOf(cube.Indices)), .BindingOptions = BindingOptions.IndexBuffer, .CpuAccessOptions = CpuAccessOptions.None, .MiscellaneousResourceOptions = MiscellaneousResourceOptions.None}

            Dim initData As New SubresourceData() With {.SystemMemory = indicesData}

            indexBuffer = device.CreateBuffer(bufferDesc, initData)
            device.IA.IndexBuffer = New IndexBuffer(indexBuffer, Format.R32UInt, 0)
            Marshal.FreeCoTaskMem(indicesData)
        End Sub
#End Region

#Region "InitMatrices()"
        Private Sub InitMatrices()
            ' Initialize the view matrix
            Dim Eye As New Vector3F(0.0F, 3.0F, -6.0F)
            Dim At As New Vector3F(0.0F, 0.0F, 0.0F)
            Dim Up As New Vector3F(0.0F, 1.0F, 0.0F)

            viewMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixLookAtLH(Eye, At, Up)

            ' Initialize the projection matrix
            projectionMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixPerspectiveFovLH(CSng(Math.PI) * 0.25F, (CSng(host.ActualWidth) / CSng(host.ActualHeight)), 0.5F, 100.0F)

            ' Update Variables that never change
            viewVariable.Matrix = viewMatrix
            projectionVariable.Matrix = projectionMatrix
        End Sub
#End Region

#Region "RenderScene"
        ''' <summary>
        ''' Render the frame
        ''' </summary>
        Protected Sub RenderScene()
            If (needsResizing) Then
                renderTargetView.Dispose()
                Dim sd As SwapChainDescription = swapChain.Description
                swapChain.ResizeBuffers(sd.BufferCount, CUInt(host.ActualWidth), CUInt(host.ActualHeight), sd.BufferDescription.Format, sd.Options)
                SetViews()
                InitMatrices()
            End If
            Dim worldMatrix As Matrix4x4F

            currentTime = (Environment.TickCount - startTime) / 1000.0F

            'WPF transforms used here use degrees as opposed to D3DX which uses radians in the native tutorial
            '360 degrees == 2 * Math.PI
            'world matrix rotates the first cube by t degrees
            Dim rotateTransform As New RotateTransform3D(New AxisAngleRotation3D(New Vector3D(0, 1, 0), currentTime * 30))
            worldMatrix = rotateTransform.Value.ToMatrix4x4F()

            ' Modify the color
            meshColor.X = (CSng(Math.Sin(currentTime * 1.0F)) + 1.0F) * 0.5F
            meshColor.Y = (CSng(Math.Cos(currentTime * 3.0F)) + 1.0F) * 0.5F
            meshColor.Z = (CSng(Math.Sin(currentTime * 5.0F)) + 1.0F) * 0.5F

            ' Clear the backbuffer
            device.ClearRenderTargetView(renderTargetView, backColor)

            '
            ' Update variables that change once per frame
            '
            worldVariable.Matrix = worldMatrix
            meshColorVariable.FloatVector = meshColor

            '
            ' Render the cube
            '
            Dim techDesc As TechniqueDescription = technique.Description
            For p As UInteger = 0 To techDesc.Passes - 1UI
                technique.GetPassByIndex(p).Apply()
                device.DrawIndexed(36, 0, 0)
            Next p

            swapChain.Present(0, PresentOptions.None)
        End Sub

#End Region

        Private Sub host_SizeChanged(ByVal sender As System.Object, ByVal e As System.Windows.SizeChangedEventArgs)
            needsResizing = True
        End Sub
    End Class
End Namespace
