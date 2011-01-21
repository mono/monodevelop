// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;
using Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;

using DXUtil = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;

namespace D3D10Tutorial09_WinFormsControl
{
    /// <summary>
    /// This application demonstrates the use of meshes
    /// 
    /// http://msdn.microsoft.com/en-us/library/bb172493(VS.85).aspx
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

        XMesh mesh;
        XMeshManager meshManager;

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

            meshManager = new XMeshManager(device);
            mesh = meshManager.Open("Media\\Tiger\\tiger.x");

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

        #region InitMatrices()
        private void InitMatrices()
        {
            // Initialize the view matrix
            Vector3F Eye = new Vector3F( 0.0f, 1.0f, -5.0f );
            Vector3F At = new Vector3F( 0.0f, 0.0f, 0.0f );
            Vector3F Up = new Vector3F( 0.0f, 1.0f, 0.0f );

            Matrix4x4F viewMatrix;
            Matrix4x4F projectionMatrix;
            viewMatrix = DXUtil.Camera.MatrixLookAtLH(Eye, At, Up);

            // Initialize the projection matrix
            projectionMatrix = DXUtil.Camera.MatrixPerspectiveFovLH((float)Math.PI * 0.25f, ((float)this.ClientSize.Width / (float)this.ClientSize.Height), 0.5f, 1000.0f);

            meshManager.SetViewAndProjection( viewMatrix, projectionMatrix ); 
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
                InitMatrices();
            }
            t = (Environment.TickCount - dwTimeStart) / 1000.0f;

            //WPF transforms used here use degrees as opposed to D3DX which uses radians in the native tutorial
            //360 degrees == 2 * Math.PI
            //world matrix rotates the first cube by t degrees
            RotateTransform3D rt1 = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), t * 60));

            // Clear the backbuffer
            device.ClearRenderTargetView(renderTargetView, backColor);

            // Clear the depth buffer to 1.0 (max depth)
            device.ClearDepthStencilView(depthStencilView, ClearOptions.Depth, 1.0f, (byte)0);

            mesh.Render( rt1.Value.ToMatrix4x4F() );

            Microsoft.WindowsAPICodePack.DirectX.ErrorCode error;
            swapChain.TryPresent(1, PresentOptions.None, out error);
        } 
        #endregion
    }
}
