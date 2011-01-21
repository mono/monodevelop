// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;
using Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;
using DXUtil = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;

namespace WindowsFlag
{
    /// <summary>
    /// This application demonstrates animation using matrix transformations of 1600 cubes
    /// 
    /// Copyright (c) Microsoft Corporation. All rights reserved.
    /// </summary>
    public partial class Window : Form
    {
        #region Fields
        Object viewSync = new object();
        D3DDevice device;
        SwapChain swapChain;
        RenderTargetView renderTargetView;
        Texture2D depthStencil;
        DepthStencilView depthStencilView;
        ColorRgba backColor = new ColorRgba (0.0F, 0.125F, 0.3F, 1.0F );

        InputLayout vertexLayout;
        D3DBuffer vertexBuffer;
        D3DBuffer indexBuffer;

        Effects effects;

        int flagShells = 20;

        Vector3F Eye = new Vector3F(0.0f, 0.0f, -1.0f);
        Vector3F At = new Vector3F(0.0f, 0.0f, 0.0f);
        Vector3F Up = new Vector3F(0.0f, 1.0f, 0.0f);

        Cube cube = new Cube();

        float t = 0f;
        float lastPresentTime = 0f;
        uint lastPresentCount = 0;
        int dwTimeStart = Environment.TickCount;

        bool isDrag = false;
        Point lastLocation = new Point(int.MaxValue, int.MaxValue);
        bool needsResizing;
        #endregion

        #region Window()
        public Window()
        {
            InitializeComponent();
        } 
        #endregion

        #region Window_Load()
        private void Window_Load(object sender, EventArgs e)
        {
            InitializeDevice();
            MoveCameraAroundCenter(-10, -29);
            ScaleCameraDistance(51);
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
        /// Creates Direct3D device and swap chain,
        /// Initializes buffers,
        /// Loads and initializes the shader
        /// </summary>
        protected void InitializeDevice()
        {
            device = D3DDevice.CreateDeviceAndSwapChain(directControl.Handle);
            swapChain = device.SwapChain;

            SetViews();

            effects = new Effects(device);

            InitializeVertexLayout();
            InitializeVertexBuffer();
            InitializeIndexBuffer();

            // Set primitive topology
            device.IA.PrimitiveTopology = PrimitiveTopology.TriangleList;

            effects.ViewMatrix = DXUtil.Camera.MatrixLookAtLH(Eye, At, Up);
            effects.ProjectionMatrix = DXUtil.Camera.MatrixPerspectiveFovLH((float)Math.PI * 0.25f, ((float)this.ClientSize.Width / (float)this.ClientSize.Height), 0.1f, 4000.0f);
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

        #region InitializeVertexLayout()
        private void InitializeVertexLayout()
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

            PassDescription passDesc = effects.Technique.GetPassByIndex(0).Description;

            vertexLayout = device.CreateInputLayout(
                layout,
                passDesc.InputAssemblerInputSignature,
                passDesc.InputAssemblerInputSignatureSize);

            device.IA.InputLayout = vertexLayout;
        }
        #endregion

        #region InitializeVertexBuffer()
        private void InitializeVertexBuffer()
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

        #region InitializeIndexBuffer()
        private void InitializeIndexBuffer()
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
            device.IA.IndexBuffer =new IndexBuffer(indexBuffer, Format.R32UInt, 0);
            Marshal.FreeCoTaskMem(indicesData);
        } 
        #endregion

        #region RenderScene()
        /// <summary>
        /// Render the frame
        /// </summary>
        protected void RenderScene()
        {
            lock (viewSync)
            {
                if (needsResizing)
                {
                    needsResizing = false;
                    renderTargetView.Dispose();
                    SwapChainDescription sd = swapChain.Description;
                    swapChain.ResizeBuffers(sd.BufferCount, (uint)directControl.ClientSize.Width, (uint)directControl.ClientSize.Height, sd.BufferDescription.Format, sd.Options);
                    SetViews();
                    // Update the projection matrix
                    effects.ProjectionMatrix = DXUtil.Camera.MatrixPerspectiveFovLH((float)Math.PI * 0.25f, ((float)this.ClientSize.Width / (float)this.ClientSize.Height), 0.1f, 4000.0f);
                }
                t = (Environment.TickCount - dwTimeStart) / 1000.0f;
                if (lastPresentTime == 0)
                {
                    lastPresentTime = t;
                    lastPresentCount = swapChain.LastPresentCount;
                }

                if (t - lastPresentTime > 1.0f) // if one second has elapsed
                {
                    uint currentPresentCount = swapChain.LastPresentCount;
                    uint presentCount = currentPresentCount - lastPresentCount;
                    float currentframerate = (float)presentCount / (t - lastPresentTime);
                    string fps = String.Format("{0} fps", currentframerate);
                    label1.BeginInvoke(new MethodInvoker(delegate() { label1.Text = fps; }));
                    lastPresentTime = t;
                    lastPresentCount = currentPresentCount;
                }

                // Clear the backbuffer
                device.ClearRenderTargetView(renderTargetView, backColor);

                // Clear the depth buffer to 1.0 (max depth)
                device.ClearDepthStencilView(depthStencilView, ClearOptions.Depth, 1.0f, (byte)0);

                RenderFlag(t, t * 180 / Math.PI, flagShells);
                swapChain.Present(0, PresentOptions.None);
            }
        }

        private void RenderFlag(float t, double a, int shells)
        {
            TechniqueDescription techDesc = effects.Technique.Description;
            for (int x = -shells; x <= shells; x++)
            {
                for (int z = -shells; z <= shells; z++)
                {
                    float height = ((float)Math.Sin(0.5 * (x + 4 * t)) + (float)Math.Cos(0.25 * (z + 2 * t)));
                    Vector4F vBaseColor = new Vector4F( 0.0f, 0.0f, 0.0f, 1.0f );
                    if (x < 0 && z > 0)
                        vBaseColor.X = 0.75f + 0.125f * height; //red
                    else if (x > 0 && z > 0)
                        vBaseColor.Y = 0.75f + 0.125f * height; //green
                    else if (x < 0 && z < 0)
                        vBaseColor.Z = 0.75f + 0.125f * height; //blue
                    else if (x > 0 && z < 0)
                    {//yellow
                        vBaseColor.X= 0.75f + 0.125f * height;
                        vBaseColor.Y = 0.75f + 0.125f * height;
                    }
                    else
                        continue;
                    effects.BaseColor = vBaseColor;
                    
                    float yScale = 5f + 0.5f * height;
                    effects.WorldMatrix = 
                        MatrixMath.MatrixScale(0.35f, yScale, 0.35f) * 
                        MatrixMath.MatrixTranslate(x, yScale - 10 , z);

                    for (uint p = 0; p < techDesc.Passes; ++p)
                    {
                        effects.Technique.GetPassByIndex(p).Apply();
                        device.DrawIndexed(36, 0, 0);
                    }
                }
            }
        }
        #endregion

        #region MoveCameraAroundCenter()
        private void MoveCameraAroundCenter(double leftRight, double topDown)
        {
            // Use WPF maths for camera rotation.
            // It is slower than using Matrix4F and Vector4F,
            // but camera calculations are only done once per camera move
            Transform3DGroup tg = new Transform3DGroup();
            //left/right drags rotate around the camera's up vector
            Vector3D leftRightRotationAxis = new Vector3D(Up.X, Up.Y, Up.Z);
            //top/down drags rotate around the vector that is perpendicular
            //to both Up and Eye (camera location) - their cross product
            Vector3D topDownRotationAxis = Vector3D.CrossProduct(
                leftRightRotationAxis,
                new Vector3D(Eye.X, Eye.Y, Eye.Z));
            tg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(leftRightRotationAxis, leftRight)));
            tg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(topDownRotationAxis, topDown)));
            Vector3D newEye = tg.Transform(new Vector3D(Eye.X, Eye.Y, Eye.Z));
            Vector3D newUp = tg.Transform(new Vector3D(Up.X, Up.Y, Up.Z));
            Eye.X = (float)newEye.X;
            Eye.Y = (float)newEye.Y;
            Eye.Z = (float)newEye.Z;
            Up.X = (float)newUp.X;
            Up.Y = (float)newUp.Y;
            Up.Z = (float)newUp.Z;

            effects.ViewMatrix = DXUtil.Camera.MatrixLookAtLH(Eye, At, Up);
        } 
        #endregion

        #region ScaleCameraDistance()
        private void ScaleCameraDistance(float scale)
        {
            Vector4F eye4 = new Vector4F(Eye.X, Eye.Y, Eye.Z, 0);
            Matrix4x4F transform = MatrixMath.MatrixScale(scale, scale, scale);
            eye4 = MatrixMath.VectorMultiply(transform, eye4);
            Eye = new Vector3F(eye4.X, eye4.Y, eye4.Z);

            effects.ViewMatrix = DXUtil.Camera.MatrixLookAtLH(Eye, At, Up);
        } 
        #endregion

        #region Event handlers for camera control
        #region directControl_MouseMove()
        private void directControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrag)
            {
                lock (viewSync)
                {
                    // Rotate the camera
                    double leftRight = lastLocation.X - e.X;
                    double topDown = lastLocation.Y - e.Y;
                    MoveCameraAroundCenter(leftRight, topDown);
                    lastLocation = e.Location;
                }
            }
        }
        #endregion

        #region OnMouseWheel()
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            lock (viewSync)
            {
                if (e.Delta != 0)
                {
                    float scale;
                    if (e.Delta <= 0)
                        scale = -(0.01f * e.Delta);
                    else
                        scale = 100f / e.Delta;
                    ScaleCameraDistance(scale);
                }
            }
        }
        #endregion

        #region directControl_MouseUp()
        private void directControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDrag = false;
            }
        }
        #endregion

        #region directControl_MouseDown()
        private void directControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDrag = true;
                lastLocation = e.Location;
            }
        }
        #endregion 
        #endregion
    }
}
