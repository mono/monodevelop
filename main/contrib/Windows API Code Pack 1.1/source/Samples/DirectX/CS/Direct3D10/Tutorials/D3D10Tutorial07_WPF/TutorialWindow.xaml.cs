using System.Windows;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;
using System;
using Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;
using DXUtil = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;
using System.IO;
using System.Windows.Media.Media3D;
namespace D3D10Tutorial07_WPF
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class TutorialWindow : Window
    {
        #region Fields
        D3DDevice device;
        SwapChain swapChain;
        RenderTargetView renderTargetView;
        ShaderResourceView textureRV;
        ColorRgba backColor = new ColorRgba(0.0F, 0.125F, 0.3F, 1.0F);
        Vector4F meshColor = new Vector4F(0.7f, 0.7f, 0.7f, 1.0f );

        Effect effect;
        EffectTechnique technique;

        InputLayout vertexLayout;
        D3DBuffer vertexBuffer;
        D3DBuffer indexBuffer;

        //variables from the .fx file
        EffectMatrixVariable worldVariable;
        EffectMatrixVariable viewVariable;
        EffectMatrixVariable projectionVariable;

        EffectVectorVariable meshColorVariable;
        EffectShaderResourceVariable diffuseVariable;

        Cube cube = new Cube();

        Matrix4x4F viewMatrix;
        Matrix4x4F projectionMatrix;

        float currentTime = 0f;
        uint startTime = (uint)Environment.TickCount;
        bool needsResizing;
        #endregion

        #region Constructor
        public TutorialWindow()
        {
            InitializeComponent();
            host.Loaded += new RoutedEventHandler(host_Loaded);
        }
        #endregion

        #region Event Handlers
        void host_Loaded(object sender, RoutedEventArgs e)
        {
            InitDevice();
            host.Render = RenderScene;
        }

        private void host_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (device != null)
            {
                needsResizing = true;
            }
        } 
        #endregion

        #region InitDevice()
        /// <summary>
        /// Create Direct3D device and swap chain
        /// </summary>
        protected void InitDevice()
        {
            device = D3DDevice1.CreateDeviceAndSwapChain1(host.Handle);
            swapChain = device.SwapChain;

            SetViews();

            // Create the effect
            using (FileStream effectStream = File.OpenRead("Tutorial07.fxo"))
            {
                effect = device.CreateEffectFromCompiledBinary(new BinaryReader(effectStream));
            }

            // Obtain the technique
            technique = effect.GetTechniqueByName("Render");

            // Obtain the variables
            worldVariable = effect.GetVariableByName("World").AsMatrix;
            viewVariable = effect.GetVariableByName("View").AsMatrix;
            projectionVariable = effect.GetVariableByName("Projection").AsMatrix;
            meshColorVariable = effect.GetVariableByName("vMeshColor").AsVector;
            diffuseVariable = effect.GetVariableByName("txDiffuse").AsShaderResource;

            InitVertexLayout();
            InitVertexBuffer();
            InitIndexBuffer();

            // Set primitive topology
            device.IA.PrimitiveTopology = PrimitiveTopology.TriangleList;

            // Load the Texture
            using (FileStream stream = File.OpenRead("seafloor.png"))
            {
                textureRV = TextureLoader.LoadTexture(device, stream);
            }

            InitMatrices();

            diffuseVariable.Resource = textureRV;
            needsResizing = false;
        }
        #endregion

        #region SetViews()
        private void SetViews()
        {
            // Create a render target view
            using (Texture2D buffer = swapChain.GetBuffer<Texture2D>(0))
            {
                renderTargetView = device.CreateRenderTargetView(buffer);
            }

            //bind the views to the device
            device.OM.RenderTargets = new OutputMergerRenderTargets(new RenderTargetView[] { renderTargetView });

            // Setup the viewport
            Viewport vp = new Viewport()
            {
                Width = (uint)host.ActualWidth,
                Height = (uint)host.ActualHeight,
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
                    SemanticName = "TEXCOORD",
                    SemanticIndex = 0,
                    Format = Format.R32G32Float,
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
            Vector3F Eye = new Vector3F(0.0f, 3.0f, -6.0f);
            Vector3F At = new Vector3F(0.0f, 0.0f, 0.0f);
            Vector3F Up = new Vector3F(0.0f, 1.0f, 0.0f);

            viewMatrix = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities.Camera.MatrixLookAtLH(Eye, At, Up);

            // Initialize the projection matrix
            projectionMatrix = DXUtil.Camera.MatrixPerspectiveFovLH((float)Math.PI * 0.25f, ((float)host.ActualWidth / (float)host.ActualHeight), 0.5f, 100.0f);

            // Update Variables that never change
            viewVariable.Matrix = viewMatrix;
            projectionVariable.Matrix = projectionMatrix;
        }
        #endregion

        #region RenderScene
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
                swapChain.ResizeBuffers(sd.BufferCount, (uint)host.ActualWidth, (uint)host.ActualHeight, sd.BufferDescription.Format, sd.Options);
                SetViews();
                // Update the projection matrix
                projectionMatrix = DXUtil.Camera.MatrixPerspectiveFovLH((float)Math.PI * 0.25f, ((float)host.ActualWidth / (float)host.ActualHeight), 0.5f, 100.0f);
                projectionVariable.Matrix = projectionMatrix;
            }
            Matrix4x4F worldMatrix;

            currentTime = (Environment.TickCount - startTime) / 1000.0f;

            //WPF transforms used here use degrees as opposed to D3DX which uses radians in the native tutorial
            //360 degrees == 2 * Math.PI
            //world matrix rotates the first cube by t degrees
            RotateTransform3D rotateTransform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), currentTime * 30));
            worldMatrix = rotateTransform.Value.ToMatrix4x4F();

            // Modify the color
            meshColor.X = ((float)Math.Sin(currentTime * 1.0f) + 1.0f) * 0.5f;
            meshColor.Y = ((float)Math.Cos(currentTime * 3.0f) + 1.0f) * 0.5f;
            meshColor.Z = ((float)Math.Sin(currentTime * 5.0f) + 1.0f) * 0.5f;

            // Clear the backbuffer
            device.ClearRenderTargetView(renderTargetView, backColor);

            //
            // Update variables that change once per frame
            //
            worldVariable.Matrix = worldMatrix;
            meshColorVariable.FloatVector = meshColor;

            //
            // Render the cube
            //
            TechniqueDescription techDesc = technique.Description;
            for (uint p = 0; p < techDesc.Passes; ++p)
            {
                technique.GetPassByIndex(p).Apply();
                device.DrawIndexed(36, 0, 0);
            }

            swapChain.Present(0, PresentOptions.None);
        }

        #endregion
    }
}
