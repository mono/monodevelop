// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D11;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;

namespace Microsoft.WindowsAPICodePack.Samples.Direct3D11
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        private class SimpleVertexArray
        {
            // An array of 3 Vectors
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public Vector3F[] vertices = 
            {
                new Vector3F()
                {
                    X= 0.0F,
                    Y = 0.5F,
                    Z = 0.5F
                },
                new Vector3F()
                {
                    X = 0.5F,
                    Y = -0.5F,
                    Z = 0.5F
                },
                new Vector3F()
                {
                    X = -0.5F,
                    Y = -0.5F,
                    Z = 0.5F
                }
            };
        }
        #endregion

        public Window1()
        {
            InitializeComponent();
        }

        #region Private Fields
        
        D3DDevice device;
        DeviceContext deviceContext;
        RenderTargetView renderTargetView;
        PixelShader pixelShader;
        VertexShader vertexShader;
        D3DBuffer vertexBuffer;
        SwapChain swapChain;
        bool needResizing;
        
        #endregion

        #region Window_Loaded()
        /// <summary>
        /// Handles the Loaded event of the window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitDevice();
            host.Render = RenderScene;
        }
        #endregion

        #region Init device

        /// <summary>
        /// Init device and required resources
        /// </summary>
        private void InitDevice()
        {
            // device creation
            device = D3DDevice.CreateDeviceAndSwapChain(host.Handle);
            swapChain = device.SwapChain;
            deviceContext = device.ImmediateContext;

            SetViews();

            // vertex shader & layout            
            // Open precompiled vertex shader
            // This file was compiled using: fxc Render.hlsl /T vs_4_0 /EVertShader /FoRender.vs
            using (Stream stream = Application.ResourceAssembly.GetManifestResourceStream("Microsoft.WindowsAPICodePack.Samples.Direct3D11.Render.vs"))
            {
                vertexShader = device.CreateVertexShader(stream);
                deviceContext.VS.Shader = vertexShader;

                // input layout is for the vert shader
                InputElementDescription inputElementDescription = new InputElementDescription();
                inputElementDescription.SemanticName = "POSITION";
                inputElementDescription.SemanticIndex = 0;
                inputElementDescription.Format = Format.R32G32B32Float;
                inputElementDescription.InputSlot = 0;
                inputElementDescription.AlignedByteOffset = 0;
                inputElementDescription.InputSlotClass = InputClassification.PerVertexData;
                inputElementDescription.InstanceDataStepRate = 0;
                stream.Position = 0;
                InputLayout inputLayout = device.CreateInputLayout(
                    new InputElementDescription[] { inputElementDescription },
                    stream);
                deviceContext.IA.InputLayout = inputLayout;
            }

            // Open precompiled vertex shader
            // This file was compiled using: fxc Render.hlsl /T ps_4_0 /EPixShader /FoRender.ps
            using (Stream stream = Application.ResourceAssembly.GetManifestResourceStream("Microsoft.WindowsAPICodePack.Samples.Direct3D11.Render.ps"))
            {
                pixelShader = device.CreatePixelShader(stream);
            }
            deviceContext.PS.SetShader(pixelShader, null);

            // create some geometry to draw (1 triangle)
            SimpleVertexArray vertex = new SimpleVertexArray();

            // put the vertices into a vertex buffer

            BufferDescription bufferDescription = new BufferDescription();
            bufferDescription.Usage = Usage.Default;
            bufferDescription.ByteWidth = (uint)Marshal.SizeOf(vertex);
            bufferDescription.BindingOptions = BindingOptions.VertexBuffer;

            SubresourceData subresourceData = new SubresourceData();

            IntPtr vertexData = Marshal.AllocCoTaskMem(Marshal.SizeOf(vertex));
            Marshal.StructureToPtr(vertex, vertexData, false);

            subresourceData.SystemMemory = vertexData;
            vertexBuffer = device.CreateBuffer(bufferDescription, subresourceData);


            deviceContext.IA.SetVertexBuffers(0, new D3DBuffer[] { vertexBuffer }, new uint[] { 12 }, new uint[] { 0 });
            deviceContext.IA.PrimitiveTopology = PrimitiveTopology.TriangleList;

            Marshal.FreeCoTaskMem(vertexData);
        }

        private void SetViews()
        {
            Texture2D texture2D = swapChain.GetBuffer<Texture2D>(0);
            renderTargetView = device.CreateRenderTargetView(texture2D);
            deviceContext.OM.RenderTargets = new OutputMergerRenderTargets(new RenderTargetView[] { renderTargetView });
            texture2D.Dispose();

            // viewport
            SwapChainDescription desc = swapChain.Description;
            Viewport viewport = new Viewport();
            viewport.Width = desc.BufferDescription.Width;
            viewport.Height = desc.BufferDescription.Height;
            viewport.MinDepth = 0.0f;
            viewport.MaxDepth = 1.0f;
            viewport.TopLeftX = 0;
            viewport.TopLeftY = 0;

            deviceContext.RS.Viewports = new Viewport[] { viewport };
        }

        #endregion

        #region Render Scene
        /// <summary>
        /// Draw scene
        /// </summary>
        private void RenderScene()
        {
            if (needResizing)
            {
                needResizing = false;
                renderTargetView.Dispose();
                SwapChainDescription sd = swapChain.Description;
                swapChain.ResizeBuffers(sd.BufferCount, (uint)host.ActualWidth, (uint)host.ActualHeight, sd.BufferDescription.Format, sd.Options);
                SetViews();
            }
            deviceContext.ClearRenderTargetView(renderTargetView, new ColorRgba(0.2f, 0.125f, 0.3f, 1.0f));

            deviceContext.Draw(3, 0);

            swapChain.Present(0, 0);
        }
        #endregion

        #region host_SizeChanged()
        private void host_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            needResizing = true;
        } 
        #endregion
    }
}
