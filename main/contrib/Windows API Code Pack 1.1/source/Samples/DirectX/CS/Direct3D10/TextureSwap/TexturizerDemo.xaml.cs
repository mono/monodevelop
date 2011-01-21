// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Controls;
using Microsoft.WindowsAPICodePack.DirectX.Controls;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.DirectX;

namespace TextureSwap
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class TexturizerDemo : Window
    {
        #region instance data
        D3DDevice device;
        SwapChain swapChain;
        RenderTargetView renderTargetView;
        Texture2D depthStencil;
        DepthStencilView depthStencilView;
        ColorRgba backgroundColor = new ColorRgba(GetColorValues(System.Windows.Media.Colors.LightSteelBlue));
        PerspectiveCamera camera = null;

        XMeshManager meshManager;
        Texturizer mesh;

        Transform3DGroup modelTransformGroup = new Transform3DGroup();
        AxisAngleRotation3D xAxisRotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0);
        AxisAngleRotation3D yAxisRotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
        AxisAngleRotation3D zAxisRotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);
        ScaleTransform3D modelZoom = new ScaleTransform3D();

        DispatcherTimer timer = new DispatcherTimer();
        #endregion

        private static float[] GetColorValues(System.Windows.Media.Color color)
        {
            return new float[] { color.ScR, color.ScG, color.ScB, color.ScA };
        }

        #region construction
        public TexturizerDemo()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(Window1_Loaded);
            this.SizeChanged += new SizeChangedEventHandler(Window1_SizeChanged);
        }
        #endregion

        #region D3D Device Initialization
        void InitDevice()
        {
            // create Direct 3D device
            device = D3DDevice.CreateDeviceAndSwapChain(renderHost.Handle);
            swapChain = device.SwapChain;

            // Create a render target view
            using (Texture2D pBuffer = swapChain.GetBuffer<Texture2D>(0))
            {
                renderTargetView = device.CreateRenderTargetView(pBuffer);
            }

            // Create depth stencil texture
            Texture2DDescription descDepth = new Texture2DDescription()
            {
                Width = (uint)renderHost.ActualWidth,
                Height = (uint)renderHost.ActualHeight,
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

            // bind the views to the device
            device.OM.RenderTargets = new OutputMergerRenderTargets(new RenderTargetView[] { renderTargetView }, depthStencilView);

            // Setup the viewport
            Viewport vp = new Viewport()
            {
                Width = (uint)renderHost.ActualWidth,
                Height = (uint)renderHost.ActualHeight,
                MinDepth = 0.0f,
                MaxDepth = 1.0f,
                TopLeftX = 0,
                TopLeftY = 0
            };
            
            device.RS.Viewports = new Viewport[] { vp };
        }
        #endregion

        #region Scene Initialization
        void InitScene()
        {
            // load mesh
            meshManager = new XMeshManager(device);
            mesh = meshManager.Open<Texturizer>(@"Resources\airplane 2.x");
            // initialize camera
            camera = new PerspectiveCamera(
                new Point3D(0, 0, -10),
                new Vector3D(0, 1, 0),
                new Vector3D(0, 1, 0),
                45);
            camera.NearPlaneDistance = .1;
            camera.FarPlaneDistance = 500;

            // initialize camera transforms
            modelTransformGroup.Children.Add(modelZoom);
            modelTransformGroup.Children.Add(new RotateTransform3D(yAxisRotation));
            modelTransformGroup.Children.Add(new RotateTransform3D(xAxisRotation));
            modelTransformGroup.Children.Add(new RotateTransform3D(zAxisRotation));
        }
        #endregion

        #region Rendering
        void timer_Tick(object sender, EventArgs e)
        {
            RenderScene();
        }
        
        protected void RenderScene()
        {
            // update view variables 
            xAxisRotation.Angle = XAxisSlider.Value;
            yAxisRotation.Angle = -YAxisSlider.Value;
            zAxisRotation.Angle = ZAxisSlider.Value;
            modelZoom.ScaleX = ZoomSlider.Value / 2;
            modelZoom.ScaleY = ZoomSlider.Value / 2;
            modelZoom.ScaleZ = ZoomSlider.Value / 2;

            // update view 
            meshManager.SetViewAndProjection(
                camera.ToViewLH().ToMatrix4x4F(), 
                camera.ToPerspectiveLH(renderHost.ActualWidth / renderHost.ActualHeight).ToMatrix4x4F());

            // clear render target
            device.ClearRenderTargetView(renderTargetView, backgroundColor);

            // Clear the depth buffer to 1.0 (max depth)
            device.ClearDepthStencilView(depthStencilView, ClearOptions.Depth, 1.0f, (byte)0);

            // render mesh
            mesh.LightIntensity = 2.5f;
            mesh.Render(modelTransformGroup.Value);

            // present back buffer
            swapChain.Present(1, PresentOptions.None);
        }
        #endregion

        #region UI event handlers
        void Window1_Loaded(object sender, RoutedEventArgs e)
        {
            InitDevice();
            InitScene();
            PartsList.ItemsSource = mesh.GetParts();
            PartsList.SelectedIndex = 0;

            TextureBrowser.NavigationPane = PaneVisibilityState.Show;
            TextureBrowser.NavigationTarget = (ShellObject)KnownFolders.PicturesLibrary;

            timer.Interval = TimeSpan.FromMilliseconds(30);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();

            ShowOneTextureCheck.IsChecked = true;
        }

        void Window1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(device != null)
            {
                //need to remove the reference to the swapchain's backbuffer to enable ResizeBuffers() call
                renderTargetView.Dispose();
                SwapChainDescription sd = swapChain.Description;
                swapChain.ResizeBuffers(
                    sd.BufferCount,
                    (uint)renderHost.ActualWidth,
                    (uint)renderHost.ActualHeight,
                    sd.BufferDescription.Format,
                    sd.Options);

                using(Texture2D pBuffer = swapChain.GetBuffer<Texture2D>(0))
                {
                    renderTargetView = device.CreateRenderTargetView(pBuffer);
                }

                // Create depth stencil texture
                Texture2DDescription descDepth = new Texture2DDescription()
                {
                    Width = (uint)renderHost.ActualWidth,
                    Height = (uint)renderHost.ActualHeight,
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

                // bind the views to the device
                device.OM.RenderTargets = new OutputMergerRenderTargets(new RenderTargetView[ ] { renderTargetView }, depthStencilView);

                // Setup the viewport
                Viewport vp = new Viewport()
                {
                    Width = (uint)renderHost.ActualWidth,
                    Height = (uint)renderHost.ActualHeight,
                    MinDepth = 0.0f,
                    MaxDepth = 1.0f,
                    TopLeftX = 0,
                    TopLeftY = 0
                };

                device.RS.Viewports = new Viewport[ ] { vp };
            }
        }

        private void PartsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mesh.PartToTexture((string)PartsList.SelectedItem);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (TextureBrowser.SelectedItems.Count > 0)
            {
                ShellObject item = TextureBrowser.SelectedItems[0];
                try
                {
                    ShellFile file = (ShellFile)item;
                    if (file.Path == null)
                    {
                        MessageBox.Show("Unable to obtain file path.");
                    }
                    mesh.SwapTexture((string)PartsList.SelectedItem, file.Path);
                }
                catch(InvalidCastException castException)
                {
                    MessageBox.Show(castException.Message, "Invalid Object selected.");
                }
            }
        }

        private void RevertTextures_Click(object sender, RoutedEventArgs e)
        {
            mesh.RevertTextures();
        }

        private void ShowOneTexture_Click(object sender, RoutedEventArgs e)
        {
            mesh.ShowOneTexture = (bool)ShowOneTextureCheck.IsChecked;
        }
        #endregion
    }
}
