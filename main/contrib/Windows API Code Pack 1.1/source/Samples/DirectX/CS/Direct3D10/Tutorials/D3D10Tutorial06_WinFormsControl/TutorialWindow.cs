// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;
using Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;
using DXUtil = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;

namespace D3D10Tutorial06_WinFormsControl
{
    /// <summary>
    /// This application demonstrates simple lighting in the vertex shader
    /// 
    /// http://msdn.microsoft.com/en-us/library/bb172490(VS.85).aspx
    /// 
    /// Copyright (c) Microsoft Corporation. All rights reserved.
    /// </summary>
    public partial class TutorialWindow : Form
    {
        #region Fields
        D3DDevice device;
        SwapChain swapChain;
        RenderTargetView renderTargetView;
        Texture2D depthStencil;
        DepthStencilView depthStencilView;
        ColorRgba backColor = new ColorRgba(0.0F, 0.125F, 0.3F, 1.0F);

        Effect effect;
        EffectTechnique technique;
        EffectTechnique techniqueLight;

        InputLayout vertexLayout;
        D3DBuffer vertexBuffer;
        D3DBuffer indexBuffer;

        //variables from the .fx file
        EffectMatrixVariable worldVariable;
        EffectMatrixVariable viewVariable;
        EffectMatrixVariable projectionVariable;

        EffectVectorVariable lightDirVariable;
        EffectVectorVariable lightColorVariable;
        EffectVectorVariable outputColorVariable;

        Cube cube = new Cube();

        Matrix4x4F viewMatrix;
        Matrix4x4F projectionMatrix;

        float t = 0f;
        uint dwTimeStart = (uint)Environment.TickCount;
        bool needsResizing;
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
            using (FileStream effectStream = File.OpenRead("Tutorial06.fxo"))
            {
                effect = device.CreateEffectFromCompiledBinary(new BinaryReader(effectStream));
            }

            // Obtain the technique
            technique = effect.GetTechniqueByName("Render");
            techniqueLight = effect.GetTechniqueByName("RenderLight");

            // Obtain the variables
            worldVariable = effect.GetVariableByName("World").AsMatrix;
            viewVariable = effect.GetVariableByName("View").AsMatrix;
            projectionVariable = effect.GetVariableByName("Projection").AsMatrix;

            lightDirVariable = effect.GetVariableByName("vLightDir").AsVector;
            lightColorVariable = effect.GetVariableByName("vLightColor").AsVector;
            outputColorVariable = effect.GetVariableByName("vOutputColor").AsVector;

            InitVertexLayout();
            InitVertexBuffer();
            InitIndexBuffer();

            // Set primitive topology
            device.IA.PrimitiveTopology = PrimitiveTopology.TriangleList;

            InitMatrices();
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

            // Create depth stencil texture
            Texture2DDescription descDepth = new Texture2DDescription()
            {
                Width = (uint)directControl.ClientSize.Width,
                Height = (uint)directControl.ClientSize.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D32Float,
                SampleDescription = new SampleDescription()
                {
                    Count = 1,
                    Quality = 0
                },
                BindingOptions = BindingOptions.DepthStencil,
            };

            depthStencil = device.CreateTexture2D(descDepth);

            // Create the depth stencil view
            DepthStencilViewDescription depthStencilViewDesc = new DepthStencilViewDescription()
            {
                Format = descDepth.Format,
                ViewDimension = DepthStencilViewDimension.Texture2D
            };
            depthStencilView = device.CreateDepthStencilView(depthStencil, depthStencilViewDesc);

            //bind the views to the device
            device.OM.RenderTargets = new OutputMergerRenderTargets(new RenderTargetView[] { renderTargetView }, depthStencilView);

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

        #region InitVertexLayout()
        private void InitVertexLayout()
        {
            // Define the input layout
            // The layout determines the stride in the vertex buffer,
            // so changes in layout need to be reflected in SetVertexBuffers
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
                },
                new InputElementDescription()
                {
                    SemanticName = "NORMAL",
                    SemanticIndex = 0,
                    Format = Format.R32G32B32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 12,
                    InputSlotClass = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                },
            };

            PassDescription passDesc = technique.GetPassByIndex(0).Description;

            vertexLayout = device.CreateInputLayout(
                layout,
                passDesc.InputAssemblerInputSignature,
                passDesc.InputAssemblerInputSignatureSize);

            device.IA.InputLayout = vertexLayout;
        }
        #endregion

        #region InitVertexBuffer()
        private void InitVertexBuffer()
        {
            IntPtr verticesData = Marshal.AllocCoTaskMem(Marshal.SizeOf(cube.Vertices));
            Marshal.StructureToPtr(cube.Vertices, verticesData, true);

            BufferDescription bufferDesc = new BufferDescription()
            {
                Usage = Usage.Default,
                ByteWidth = (uint)Marshal.SizeOf(cube.Vertices),
                BindingOptions = BindingOptions.VertexBuffer,
                CpuAccessOptions = CpuAccessOptions.None,
                MiscellaneousResourceOptions = MiscellaneousResourceOptions.None
            };

            SubresourceData InitData = new SubresourceData()
            {
                SystemMemory = verticesData
            };

            //D3DBuffer buffer = null;
            vertexBuffer = device.CreateBuffer(bufferDesc, InitData);

            // Set vertex buffer
            uint stride = (uint)Marshal.SizeOf(typeof(SimpleVertex));
            uint offset = 0;
            device.IA.SetVertexBuffers(
                0,
                new D3DBuffer[] { vertexBuffer },
                new uint[] { stride },
                new uint[] { offset });
            Marshal.FreeCoTaskMem(verticesData);
        }
        #endregion

        #region InitIndexBuffer()
        private void InitIndexBuffer()
        {
            IntPtr indicesData = Marshal.AllocCoTaskMem(Marshal.SizeOf(cube.Indices));
            Marshal.StructureToPtr(cube.Indices, indicesData, true);

            BufferDescription bufferDesc = new BufferDescription()
            {
                Usage = Usage.Default,
                ByteWidth = (uint)Marshal.SizeOf(cube.Indices),
                BindingOptions = BindingOptions.IndexBuffer,
                CpuAccessOptions = CpuAccessOptions.None,
                MiscellaneousResourceOptions = MiscellaneousResourceOptions.None
            };

            SubresourceData initData = new SubresourceData()
            {
                SystemMemory = indicesData
            };

            indexBuffer = device.CreateBuffer(bufferDesc, initData);
            device.IA.IndexBuffer = new IndexBuffer(indexBuffer, Format.R32UInt, 0);
            Marshal.FreeCoTaskMem(indicesData);
        } 
        #endregion

        #region InitMatrices()
        private void InitMatrices()
        {
            // Initialize the view matrix
            Vector3F Eye = new Vector3F( 0.0f, 4.0f, -10.0f );
            Vector3F At = new Vector3F( 0.0f, 0.0f, 0.0f );
            Vector3F Up = new Vector3F( 0.0f, 1.0f, 0.0f );

            viewMatrix = DXUtil.Camera.MatrixLookAtLH(Eye, At, Up);

            //// Initialize the projection matrix
            projectionMatrix = DXUtil.Camera.MatrixPerspectiveFovLH((float)Math.PI * 0.25f, ((float)this.ClientSize.Width / (float)this.ClientSize.Height), 0.5f, 100.0f);

            // Update Variables that never change
            viewVariable.Matrix = viewMatrix;
            projectionVariable.Matrix = projectionMatrix;
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
                // Update the projection matrix
                projectionMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixPerspectiveFovLH((float)Math.PI * 0.25f, ((float)directControl.ClientSize.Width / (float)directControl.ClientSize.Height), 0.5f, 100.0f);
                projectionVariable.Matrix = projectionMatrix;
            }
            Matrix4x4F worldMatrix;

            t = (Environment.TickCount - dwTimeStart) / 50.0f;

            //WPF transforms used here use degrees as opposed to D3DX which uses radians in the native tutorial
            //360 degrees == 2 * Math.PI
            //world matrix rotates the first cube by t degrees
            RotateTransform3D rt1 = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), t));
            worldMatrix = rt1.Value.ToMatrix4x4F();

            //Setup our lighting parameters
            Vector4F[] vLightDirs =
            {
                new Vector4F( -0.577f, 0.577f, -0.577f, 1.0f ),
                new Vector4F( 0.0f, 0.0f, -1.0f, 1.0f )
            };
            Vector4F[] vLightColors = 
            {
                new Vector4F ( 0.5f, 0.5f, 0.5f, 1.0f ),
                new Vector4F ( 0.5f, 0.0f, 0.0f, 1.0f )
            };

            //rotate the second light around the origin
            //create a rotation matrix
            RotateTransform3D rt2 = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 2, 0), -t));
            //rotate vLightDirs[1] vector using the rotation matrix
            Vector3D vDir = new Vector3D(vLightDirs[1].X, vLightDirs[1].Y, vLightDirs[1].Z);
            vDir = rt2.Transform(vDir);
            vLightDirs[1].X = (float)vDir.X;
            vLightDirs[1].Y = (float)vDir.Y;
            vLightDirs[1].Z = (float)vDir.Z;

            // Clear the backbuffer
            device.ClearRenderTargetView(renderTargetView, backColor);

            // Clear the depth buffer to 1.0 (max depth)
            device.ClearDepthStencilView(depthStencilView, ClearOptions.Depth, 1.0f, (byte)0);

            //
            // Update variables that change once per frame
            //
            worldVariable.Matrix = worldMatrix;
            lightDirVariable.SetFloatVectorArray(vLightDirs);
            lightColorVariable.SetFloatVectorArray(vLightColors);

            //
            // Render the cube
            //
            TechniqueDescription techDesc = technique.Description;
            for (uint p = 0; p < techDesc.Passes; ++p)
            {
                technique.GetPassByIndex(p).Apply();
                device.DrawIndexed(36, 0, 0);
            }

            //
            // Render each light
            //
            TechniqueDescription techLightDesc = techniqueLight.Description;
            for (int m = 0; m < 2; m++)
            {
                Vector3F vLightPos = new Vector3F(vLightDirs[m].X * 5, vLightDirs[m].Y * 5, vLightDirs[m].Z * 5);
                Transform3DGroup tg = new Transform3DGroup();
                tg.Children.Add(new ScaleTransform3D(0.2, 0.2, 0.2));
                tg.Children.Add(new TranslateTransform3D(vLightPos.X, vLightPos.Y, vLightPos.Z));
                worldVariable.Matrix = tg.Value.ToMatrix4x4F();
                outputColorVariable.FloatVector = new Vector4F (vLightColors[m].X, vLightColors[m].Y, vLightColors[m].Z, vLightColors[m].W);

                for (uint p = 0; p < techLightDesc.Passes; ++p)
                {
                    techniqueLight.GetPassByIndex(p).Apply();
                    device.DrawIndexed(36, 0, 0);
                }
            }

            swapChain.Present(0, PresentOptions.None);
        } 
        #endregion
    }
}
