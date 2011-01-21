// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;

namespace D3D10Tutorial01_WinFormsWindow
{
    /// <summary>
    /// This application demonstrates creating a Direct3D 10 device
    /// 
    /// http://msdn.microsoft.com/en-us/library/bb172485(VS.85).aspx
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
        bool active = false;
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
        /// <summary>
        /// Handles the Load event of the form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void TutorialWindow_Load(object sender, EventArgs e)
        {
            if (!active)
            {
                InitDevice();
                active = true;
            }
        }
        #endregion

        #region TutorialWindow_FormClosing()
        /// <summary>
        /// Handles the FormClosing event of the form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.FormClosingEventArgs"/> instance containing the event data.</param>
        private void TutorialWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            device.ClearState();
        }
        #endregion

        #region WndProc()
        /// <summary>
        /// The Window Procedure (message loop callback).
        /// </summary>
        /// <param name="m">The m.</param>
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            Invalidate();
            base.WndProc(ref m);
        } 
        #endregion

        #region OnPaintBackground()
        /// <summary>
        /// Paints the background of the control.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.</param>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Leave empty so that invalidate does not redraw the background causing flickering
        } 
        #endregion

        #region OnPaint()
        /// <summary>
        /// Handles painting of the window
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (active)
                RenderScene();
        } 
        #endregion

        #region InitDevice()
        /// <summary>
        /// Create Direct3D device and swap chain
        /// </summary>
        protected void InitDevice()
        {
            device = D3DDevice.CreateDeviceAndSwapChain(this.Handle);
            swapChain = device.SwapChain;

            // Create a render target view
            using (Texture2D pBuffer = swapChain.GetBuffer<Texture2D>(0))
            {
                renderTargetView = device.CreateRenderTargetView(pBuffer);
            }
            device.OM.RenderTargets = new OutputMergerRenderTargets(new RenderTargetView[] { renderTargetView }, null);

            // Setup the viewport
            Viewport vp = new Viewport()
            {
                Width = (uint)this.ClientSize.Width,
                Height = (uint)this.ClientSize.Height,
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
            // Just clear the backbuffer
            device.ClearRenderTargetView(renderTargetView, backColor);
            swapChain.Present(0, PresentOptions.None);
        } 
        #endregion
    }
}
