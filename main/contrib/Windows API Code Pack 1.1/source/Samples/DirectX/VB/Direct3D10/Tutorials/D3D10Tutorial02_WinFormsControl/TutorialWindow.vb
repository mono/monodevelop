' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Collections.ObjectModel
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D

Namespace D3D10Tutorial02_WinFormsControl
	''' <summary>
	''' This application displays a triangle using Direct3D 10
	''' 
	''' http://msdn.microsoft.com/en-us/library/bb172486(VS.85).aspx
	''' http://msdn.microsoft.com/en-us/library/bb172487(VS.85).aspx
	''' 
	''' Copyright (c) Microsoft Corporation. All rights reserved.
	''' </summary>
	Partial Public Class TutorialWindow
		Inherits Form
		#Region "Fields"
		Private device As D3DDevice
		Private swapChain As SwapChain
		Private renderTargetView As RenderTargetView
		Private backColor_Renamed As New ColorRgba(0.0F, 0.125F, 0.3F, 1.0F)

		Private effect As Effect
		Private technique As EffectTechnique
		Private vertexLayout As InputLayout
		Private vertexBuffer As D3DBuffer
		Private needsResizing As Boolean
		#End Region

		#Region "Structs"
		<StructLayout(LayoutKind.Sequential)> _
		Private Class SimpleVertexArray
			' An array of 3 Vectors
			<MarshalAs(UnmanagedType.ByValArray, SizeConst := 3)> _
			Public vertices() As Vector3F = { New Vector3F() With {.X = 0.0F, .Y = 0.5F, .Z = 0.5F}, New Vector3F() With {.X = 0.5F, .Y = -0.5F, .Z = 0.5F}, New Vector3F() With {.X = -0.5F, .Y = -0.5F, .Z = 0.5F} }
		End Class
		#End Region

		#Region "TutorialWindow()"
		''' <summary>
		''' Initializes a new instance of the <see cref="TutorialWindow"/> class.
		''' </summary>
		Public Sub New()
			InitializeComponent()
		End Sub
		#End Region

		#Region "TutorialWindow_Load()"
		Private Sub TutorialWindow_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
			InitDevice()
			directControl.Render = AddressOf Me.RenderScene
		End Sub
		#End Region

		#Region "directControl_SizeChanged()"
		Private Sub directControl_SizeChanged(ByVal sender As Object, ByVal e As EventArgs) Handles directControl.SizeChanged
			needsResizing = True
		End Sub
		#End Region

		#Region "TutorialWindow_FormClosing()"
		Private Sub TutorialWindow_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles MyBase.FormClosing
			directControl.Render = Nothing
			device.ClearState()
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

			' Create the effect
			Using effectStream As FileStream = File.OpenRead("Tutorial02.fxo")
				effect = device.CreateEffectFromCompiledBinary(New BinaryReader(effectStream))
			End Using

			' Obtain the technique
			technique = effect.GetTechniqueByName("Render")

			' Define the input layout
            Dim layout() As InputElementDescription = _
            { _
                New InputElementDescription() With _
                { _
                    .SemanticName = "POSITION", _
                    .SemanticIndex = 0, _
                    .Format = Format.R32G32B32Float, _
                    .InputSlot = 0, _
                    .AlignedByteOffset = 0, _
                    .InputSlotClass = InputClassification.PerVertexData, _
                    .InstanceDataStepRate = 0 _
                } _
            }

			Dim passDesc As PassDescription = technique.GetPassByIndex(0).Description

			vertexLayout = device.CreateInputLayout(layout, passDesc.InputAssemblerInputSignature, passDesc.InputAssemblerInputSignatureSize)

            device.IA.InputLayout = vertexLayout

			Dim vertex As New SimpleVertexArray()

            Dim bd As New BufferDescription() With {.Usage = Usage.Default, .ByteWidth = CUInt(Marshal.SizeOf(vertex)), .BindingOptions = BindingOptions.VertexBuffer, .CpuAccessOptions = CpuAccessOptions.None, .MiscellaneousResourceOptions = MiscellaneousResourceOptions.None}

			Dim vertexData As IntPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(vertex))
			Marshal.StructureToPtr(vertex, vertexData, False)

            Dim InitData As New SubresourceData() With {.SystemMemory = vertexData, .SystemMemoryPitch = 0, .SystemMemorySlicePitch = 0}

			'D3DBuffer buffer = null;
			vertexBuffer = device.CreateBuffer(bd, InitData)

			' Set vertex buffer
			Dim stride As UInteger = CUInt(Marshal.SizeOf(GetType(Vector3F)))
			Dim offset As UInteger = 0
			device.IA.SetVertexBuffers(0, New Collection(Of D3DBuffer) (New D3DBuffer() {vertexBuffer}), New UInteger() { stride }, New UInteger() { offset })

			' Set primitive topology
            device.IA.PrimitiveTopology = PrimitiveTopology.TriangleList
			Marshal.FreeCoTaskMem(vertexData)
		End Sub
		#End Region

		#Region "SetViews()"
		Private Sub SetViews()
			' Create a render target view
			Using pBuffer As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
				renderTargetView = device.CreateRenderTargetView(pBuffer)
			End Using

            device.OM.RenderTargets = New OutputMergerRenderTargets(New RenderTargetView() {renderTargetView}, Nothing)

			' Setup the viewport
			Dim vp As New Viewport() With {.Width = CUInt(directControl.ClientSize.Width), .Height = CUInt(directControl.ClientSize.Height), .MinDepth = 0.0f, .MaxDepth = 1.0f, .TopLeftX = 0, .TopLeftY = 0}

            device.RS.Viewports = New Viewport() {vp}
		End Sub
		#End Region

		#Region "RenderScene()"
		''' <summary>
		''' Render the frame
		''' </summary>
		Protected Sub RenderScene()
			If needsResizing Then
				needsResizing = False
				renderTargetView.Dispose()
				Dim sd As SwapChainDescription = swapChain.Description
                swapChain.ResizeBuffers(sd.BufferCount, CUInt(directControl.ClientSize.Width), CUInt(directControl.ClientSize.Height), sd.BufferDescription.Format, sd.Options)
				SetViews()
			End If
			' Just clear the backbuffer
			device.ClearRenderTargetView(renderTargetView, backColor_Renamed)

			Dim techDesc As TechniqueDescription = technique.Description

            For p As UInteger = 0 To techDesc.Passes - 1UI
                technique.GetPassByIndex(p).Apply()
                device.Draw(3, 0)
            Next p

            swapChain.Present(0, PresentOptions.None)
		End Sub
		#End Region
	End Class
End Namespace
