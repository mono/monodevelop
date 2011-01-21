// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;
using Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;

namespace D3D10Tutorial02_WinFormsControl
{
    /// <summary>
    /// This application displays a triangle using Direct3D 10
    /// 
    /// http://msdn.microsoft.com/en-us/library/bb172486(VS.85).aspx
    /// http://msdn.microsoft.com/en-us/library/bb172487(VS.85).aspx
    /// 
    /// Copyright (c) Microsoft Corporation. All rights reserved.
    /// </summary>
    public partial class TutorialWindow : Form
    {
        #region Fields
        D3DDevice device;
        SwapChain swapChain;
        RenderTargetView renderTargetView;
        ColorRgba backColor = new ColorRgba(0.0F, 0.125F, 0.3F, 1.0F);

        Effect effect;
        EffectTechnique technique;
        InputLayout vertexLayout;
        D3DBuffer vertexBuffer;
        bool needsResizing;
        #endregion

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

        #region TutorialWindow()
        /// <summary>
        /// Initializes a new instance of the <see cref="TutorialWindow"/> class.
        /// </summary>
        public TutorialWindow()
        {
            InitializeComponent();
        } 
        #endregion

        #region TutorialWindow_Load()
        private void TutorialWindow_Load(object sender, EventArgs e)
        {
            InitDevice();
            directControl.Render = this.RenderScene;
        }
        #endregion

        #region directControl_SizeChanged()
        private void directControl_SizeChanged(object sender, EventArgs e)
        {
            needsResizing = true;
        }
        #endregion

        #region TutorialWindow_FormClosing()
        private void TutorialWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            directControl.Render = null;
            device.ClearState();
        }
        #endregion

        #region InitDevice()
        /// <summary>
        /// Create Direct3D device and swap chain
        /// </summary>
        protected void InitDevice()
        {
            device = D3DDevice.CreateDeviceAndSwapChain(directControl.Handle);
            swapChain = device.SwapChain;

            SetViews();

            // Create the effect
            using (FileStream effectStream = File.OpenRead("Tutorial02.fxo"))
            {
                effect = device.CreateEffectFromCompiledBinary(new BinaryReader(effectStream));
            }

            // Obtain the technique
            technique = effect.GetTechniqueByName("Render");

            // Define the input layout
            InputElementDescription[] layout = 
            {
                new InputElementDescription()
                {
                    SemanticName = "POSITION",
                    SemanticIndex = 0,
                    Format = Format.R32G32B32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 0,
                    InputSlotClass = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                }
            };

            PassDescription passDesc = technique.GetPassByIndex(0).Description;

            vertexLayout = device.CreateInputLayout(
                layout,
                passDesc.InputAssemblerInputSignature,
                passDesc.InputAssemblerInputSignatureSize);

            device.IA.InputLayout = vertexLayout;

            SimpleVertexArray vertex = new SimpleVertexArray();

            BufferDescription bd = new BufferDescription()
            {
                Usage = Usage.Default,
                ByteWidth = (uint)Marshal.SizeOf(vertex),
                BindingOptions = BindingOptions.VertexBuffer,
                CpuAccessOptions = CpuAccessOptions.None,
                MiscellaneousResourceOptions = MiscellaneousResourceOptions.None
            };

            IntPtr vertexData = Marshal.AllocCoTaskMem(Marshal.SizeOf(vertex));
            Marshal.StructureToPtr(vertex, vertexData, false);

            SubresourceData InitData = new SubresourceData()
            {
                SystemMemory = vertexData,
                SystemMemoryPitch = 0,
                SystemMemorySlicePitch = 0
            };

            //D3DBuffer buffer = null;
            vertexBuffer = device.CreateBuffer(bd, InitData);

            // Set vertex buffer
            uint stride = (uint)Marshal.SizeOf(typeof(Vector3F));
            uint offset = 0;
            device.IA.SetVertexBuffers(0, new Collection<D3DBuffer>()
                {
                    vertexBuffer
                },
                new uint[] { stride }, new uint[] { offset });

            // Set primitive topology
            device.IA.PrimitiveTopology = PrimitiveTopology.TriangleList;
            Marshal.FreeCoTaskMem(vertexData);
        }
        #endregion

        #region SetViews()
        private void SetViews()
        {
            // Create a render target view
            using (Texture2D pBuffer = swapChain.GetBuffer<Texture2D>(0))
            {
                renderTargetView = device.CreateRenderTargetView(pBuffer);
            }

            device.OM.RenderTargets = new OutputMergerRenderTargets(new RenderTargetView[] { renderTargetView }, null);

            // Setup the viewport
            Viewport vp = new Viewport()
            {
                Width = (uint)directControl.ClientSize.Width,
                Height = (uint)directControl.ClientSize.Height,
                MinDepth = 0.0f,
                MaxDepth = 1.0f,
                TopLeftX = 0,
                TopLeftY = 0
            };

            device.RS.Viewports = new Viewport[] { vp };
        }
        #endregion

        #region RenderScene()
        /// <summary>
        /// Render the frame
        /// </summary>
        protected void RenderScene()
        {
            if (needsResizing)
            {
                needsResizing = false;
                renderTargetView.Dispose();
                SwapChainDescription sd = swapChain.Description;
                swapChain.ResizeBuffers(sd.BufferCount, (uint)directControl.ClientSize.Width, (uint)directControl.ClientSize.Height, sd.BufferDescription.Format, sd.Options);
                SetViews();
            }
            // Just clear the backbuffer
            device.ClearRenderTargetView(renderTargetView, backColor);

            TechniqueDescription techDesc = technique.Description;

            for (uint p = 0; p < techDesc.Passes; ++p)
            {
                technique.GetPassByIndex(p).Apply();
                device.Draw(3, 0);
            }

            swapChain.Present(0, PresentOptions.None);
        } 
        #endregion
    }
}
