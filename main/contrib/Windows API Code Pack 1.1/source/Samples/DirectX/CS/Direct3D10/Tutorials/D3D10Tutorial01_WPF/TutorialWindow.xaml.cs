// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Windows;
using Microsoft.WindowsAPICodePack.DirectX.Controls;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;

namespace D3D10Tutorial01_WPF
{
    /// <summary>
    /// This application demonstrates creating a Direct3D 10 device
    /// 
    /// http://msdn.microsoft.com/en-us/library/bb172485(VS.85).aspx
    /// 
    /// Copyright (c) Microsoft Corporation. All rights reserved.
    /// </summary>
    public partial class TutorialWindow : Window
    {
        #region Fields
        DirectHost host;
        D3DDevice device;
        SwapChain swapChain;
        RenderTargetView renderTargetView;
        ColorRgba backColor = new ColorRgba ( 0.0F, 0.125F, 0.3F, 1.0F ); 
        #endregion

        #region TutorialWindow()
        /// <summary>
        /// Initializes a new instance of the <see cref="TutorialWindow"/> class.
        /// </summary>
        public TutorialWindow()
        {
            InitializeComponent();
            host = new DirectHost(/*ControlHostElement.ActualWidth, ControlHostElement.ActualHeight*/);
            ControlHostElement.Child = host;
        } 
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

        #region InitDevice()
        /// <summary>
        /// Create Direct3D device and swap chain
        /// </summary>
        public void InitDevice()
        {
            device = D3DDevice.CreateDeviceAndSwapChain(host.Handle);
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
