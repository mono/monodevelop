// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D11;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;

namespace Microsoft.WindowsAPICodePack.Samples.Direct3D11
{
    public partial class Form1 : Form
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
                    X = 0.0F,
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


        #region Fields

        D3DDevice device;
        DeviceContext deviceContext;
        RenderTargetView renderTargetView;
        PixelShader pixelShader;
        VertexShader vertexShader;
        D3DBuffer vertexBuffer;
        SwapChain swapChain;

        #endregion        
        
        public Form1()
        {
            InitializeComponent();
                        
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);            
            UpdateStyles();

        }

        #region OnMouseDoubleClick()
        /// <summary>
        /// Switches full-screen mode
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (swapChain != null)
            {
                swapChain.IsFullScreen = !swapChain.IsFullScreen;
            }
        }

        #endregion

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // device creation
            device = D3DDevice.CreateDeviceAndSwapChain(Handle);
            swapChain = device.SwapChain;
            deviceContext = device.ImmediateContext;

            SetViews();

            // Open precompiled vertex shader
            // This file was compiled using: fxc Render.hlsl /T vs_4_0 /EVertShader /FoRender.vs
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.WindowsAPICodePack.Samples.Direct3D11.Render.vs"))
            {
                vertexShader = device.CreateVertexShader(stream);
            }

            deviceContext.VS.SetShader(vertexShader, null);

            // input layout is for the vert shader
            InputElementDescription inputElementDescription = new InputElementDescription();
            inputElementDescription.SemanticName = "POSITION";
            inputElementDescription.SemanticIndex = 0;
            inputElementDescription.Format = Format.R32G32B32Float;
            inputElementDescription.InputSlot = 0;
            inputElementDescription.AlignedByteOffset = 0;
            inputElementDescription.InputSlotClass = InputClassification.PerVertexData;
            inputElementDescription.InstanceDataStepRate = 0;

            InputLayout inputLayout;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.WindowsAPICodePack.Samples.Direct3D11.Render.vs"))
            {
                inputLayout = device.CreateInputLayout(new InputElementDescription[] { inputElementDescription }, stream);
            }
            deviceContext.IA.InputLayout = inputLayout;

            // Open precompiled vertex shader
            // This file was compiled using: fxc Render.hlsl /T ps_4_0 /EPixShader /FoRender.ps
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.WindowsAPICodePack.Samples.Direct3D11.Render.ps"))
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

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Do not paint to prevent flickering
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Not required unless we need other controls!
            // base.OnPaint(e);  

            deviceContext.ClearRenderTargetView(renderTargetView, new ColorRgba( 0.2f, 0.125f, 0.3f, 1.0f ));            

            deviceContext.Draw(3, 0);

            swapChain.Present(0, 0);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            if (renderTargetView != null)
            {
                renderTargetView.Dispose();
                SwapChainDescription sd = swapChain.Description;
                swapChain.ResizeBuffers(sd.BufferCount, (uint)this.ClientSize.Width, (uint)this.ClientSize.Height, sd.BufferDescription.Format, sd.Options);
                SetViews();
                Invalidate();
            }
            base.OnSizeChanged(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            // dispose all the DirectX bits

            deviceContext.ClearState();
            deviceContext.Flush();


            if (vertexBuffer != null)
            {
                vertexBuffer.Dispose();
            }

            if (vertexShader != null)
            {
                vertexShader.Dispose();
            }

            if (pixelShader != null)
            {
                pixelShader.Dispose();
            }

            if (renderTargetView != null)
            {
                renderTargetView.Dispose();
            }

            if (device != null)
            {
                device.Dispose();
            }
  
            base.OnClosed(e);
        }

    }
}
