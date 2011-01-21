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

        public Form1()
        {
            InitializeComponent();
        }

        D3DDevice device;
        DeviceContext deviceContext;
        RenderTargetView renderTargetView;
        PixelShader pixelShader;
        VertexShader vertexShader;
        D3DBuffer vertexBuffer;
        SwapChain swapChain;
        bool needResizing;

        #region Fps
        public float Fps { get; private set; }
        #endregion

        #region FpsChanged
        private event EventHandler fpsChanged;
        public event EventHandler FpsChanged
        {
            add
            {
                fpsChanged += value;
            }
            remove
            {
                fpsChanged -= value;
            }
        }
        #endregion

        private int lastTickCount;
        private int frameCount;

        private void directControl_Load(object sender, EventArgs e)
        {
            InitDevice();
            directControl.Render = RenderScene;
            fpsChanged += new EventHandler(Form1_fpsChanged);
        }

        void Form1_fpsChanged(object sender, EventArgs e)
        {
            this.BeginInvoke(new MethodInvoker(delegate() {
            label1.Text = "FPS: " + Fps;}));
        }

        private void InitDevice()
        {
            // device creation
            //device = D3DDevice.CreateDeviceAndSwapChain(
            //    null,
            //    DriverType.Hardware,
            //    null,
            //    CreateDeviceFlag.Default,
            //    new []{FeatureLevel.FeatureLevel_10_1},
            //    new SwapChainDescription {
            //        BufferCount = 1
            //    },
            //    out swapChain);
            device = D3DDevice.CreateDeviceAndSwapChain(directControl.Handle);
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
            inputElementDescription.Format = Format.R32G32B32Float ;
            inputElementDescription.InputSlot = 0;
            inputElementDescription.AlignedByteOffset = 0;
            inputElementDescription.InputSlotClass = InputClassification.PerVertexData;
            inputElementDescription.InstanceDataStepRate = 0;

            InputLayout inputLayout;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.WindowsAPICodePack.Samples.Direct3D11.Render.vs"))
            {
                inputLayout = device.CreateInputLayout(new [] { inputElementDescription }, stream);
            }
            deviceContext.IA.InputLayout = inputLayout;

            // Open precompiled pixel shader
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


            deviceContext.IA.SetVertexBuffers(0, new [] { vertexBuffer }, new uint[] { 12 }, new uint[] { 0 });
            deviceContext.IA.PrimitiveTopology = PrimitiveTopology.TriangleList;

            Marshal.FreeCoTaskMem(vertexData);
        }

        private void SetViews()
        {
            Texture2D texture2D = swapChain.GetBuffer<Texture2D>(0);
            renderTargetView = device.CreateRenderTargetView(texture2D);
            deviceContext.OM.RenderTargets = new OutputMergerRenderTargets(new[] {renderTargetView});
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

            deviceContext.RS.Viewports = new[] { viewport };
        }

        private void RenderScene()
        {
            if (needResizing)
            {
                needResizing = false;
                renderTargetView.Dispose();
                SwapChainDescription sd = swapChain.Description;
                swapChain.ResizeBuffers(sd.BufferCount, (uint)directControl.ClientSize.Width, (uint)directControl.ClientSize.Height, sd.BufferDescription.Format, sd.Options);
                SetViews();
            }
            deviceContext.ClearRenderTargetView(renderTargetView, new ColorRgba(0.2f, 0.125f, 0.3f, 1.0f));

            deviceContext.Draw(3, 0);

            swapChain.Present(0, 0);
            CalculateFPS();
        }

        #region CalculateFPS()
        private void CalculateFPS()
        {
            int currentTickCount = Environment.TickCount;
            int ticks = currentTickCount - lastTickCount;
            if (ticks >= 1000)
            {
                Fps = (float)frameCount * 1000 / ticks;
                frameCount = 0;
                lastTickCount = currentTickCount;
                BeginInvoke(new MethodInvoker(delegate
                {
                    if (fpsChanged != null)
                        fpsChanged(this, EventArgs.Empty);
                }));
            }
            frameCount++;
        }
        #endregion

        private void directControl_SizeChanged(object sender, EventArgs e)
        {
            needResizing = true;
        }
    }
}
