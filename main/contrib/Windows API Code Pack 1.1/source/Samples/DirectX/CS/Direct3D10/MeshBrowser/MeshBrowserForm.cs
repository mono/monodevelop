// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;
using Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;
using dxUtil = Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;

namespace MeshBrowser
{
    /// <summary>
    /// This application demonstrates how to use the library to implement a useful utility application
    /// 
    /// Copyright (c) Microsoft Corporation. All rights reserved.
    /// </summary>
    public partial class MeshBrowserForm : Form
    {
        #region Fields
        D3DDevice device;
        SwapChain swapChain;
        RenderTargetView renderTargetView;
        Texture2D depthStencil;
        DepthStencilView depthStencilView;
        ColorRgba backColor = new ColorRgba ( 0.0F, 0.125F, 0.3F, 1.0F );

        XMesh mesh;
        XMeshManager meshManager;

        Matrix4x4F worldMatrix = Matrix4x4F.Identity;
        XDocument knownFiles;
        int dwLastTime = Environment.TickCount;

        bool isDrag = false;
        System.Drawing.Point lastLocation = new System.Drawing.Point(int.MaxValue, int.MaxValue);
        System.Drawing.Brush myBrush;
        private object meshLock = new object();
        bool needsResizing;
        #endregion

        #region MeshBrowserForm()
        /// <summary>
        /// Initializes a new instance of the <see cref="MeshBrowserForm"/> class.
        /// </summary>
        public MeshBrowserForm()
        {
            InitializeComponent();
        } 
        #endregion

        #region Window_Load()
        private void Window_Load(object sender, EventArgs e)
        {
            myBrush = new System.Drawing.SolidBrush(listBoxValid.ForeColor);
            InitDevice();
            directControl.Render = this.RenderScene;
            string dxsdkdir = Environment.GetEnvironmentVariable("DXSDK_DIR");
            if (!string.IsNullOrEmpty(dxsdkdir))
                openFileDialog1.InitialDirectory = System.IO.Path.Combine(dxsdkdir, "Samples\\Media");
            if (File.Exists("knownFiles.xml"))
                LoadKnown();
            else
            {
                knownFiles = new XDocument(
                    new XElement("KnownFiles",
                        new XElement("Valid"),
                        new XElement("Invalid")));
                SaveKnown();
            }
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
            Vector3F Eye = new Vector3F(0.0f, 3.0f, -15.0f);
            Vector3F At = new Vector3F(0.0f, 0.0f, 0.0f);
            Vector3F Up = new Vector3F(0.0f, 1.0f, 0.0f);

            Matrix4x4F viewMatrix;
            Matrix4x4F projectionMatrix;
            viewMatrix = dxUtil.Camera.MatrixLookAtLH(Eye, At, Up);

            // Initialize the projection matrix
            projectionMatrix = dxUtil.Camera.MatrixPerspectiveFovLH(
                (float)Math.PI * 0.25f,
                (float)directControl.ClientSize.Width / (float)directControl.ClientSize.Height,
                0.5f, 1000.0f);

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
                swapChain.ResizeBuffers(
                    sd.BufferCount,
                    (uint)directControl.ClientSize.Width,
                    (uint)directControl.ClientSize.Height,
                    sd.BufferDescription.Format,
                    sd.Options);
                SetViews();
                // Update the projection matrix
                InitMatrices();
            }
            int dwCurrentTime = Environment.TickCount;
            float t = (dwCurrentTime - dwLastTime) / 1000f;
            dwLastTime = dwCurrentTime;

            // Clear the backbuffer
            device.ClearRenderTargetView(renderTargetView, backColor);

            // Clear the depth buffer to 1.0 (max depth)
            device.ClearDepthStencilView(depthStencilView, ClearOptions.Depth, 1.0f, (byte)0);

            lock (meshLock)
            {
                if (mesh != null)
                {
                    if (cbRotate.Checked)
                        worldMatrix *= MatrixMath.MatrixRotationY(-t);
                    mesh.Render(worldMatrix);
                }
            }

            Microsoft.WindowsAPICodePack.DirectX.ErrorCode error;
            swapChain.TryPresent(1, PresentOptions.None, out error);
        }
        #endregion

        #region Mesh loading
        #region LoadMeshAndUpdateKnownFiles()
        private void LoadMeshAndUpdateKnownFiles(string filename)
        {
            LoadMeshAndUpdateKnownFiles(filename, true);
        }

        private void LoadMeshAndUpdateKnownFiles(string filename, bool showException)
        {
            try
            {
                LoadMesh(filename);
                MarkFileValid(filename);
            }
            catch (Exception ex)
            {
                if (showException)
                    ShowTextInDialog(ex.ToString(), "Could not load mesh");
                MarkFileInvalid(filename);
            }
        }
        #endregion

        #region LoadMesh()
        private void LoadMesh(string filename)
        {
            lock (meshLock)
            {
                if (mesh != null)
                {
                    mesh.Dispose();
                    mesh = null;
                }

                worldMatrix = Matrix4x4F.Identity;

                XMesh meshT = meshManager.Open(filename);

                meshT.ShowWireFrame = cbWireframe.Checked;

                mesh = meshT;
            };
        }
        #endregion
        #endregion

        #region Known files list handling
        #region MarkFileValid()
        private void MarkFileValid(string filename)
        {
            var q1 = knownFiles.Root.XPathSelectElements("./Invalid/File")
                     .Where(file => (string)file.Attribute("path") == filename);

            if (q1.Count() > 0)
            {
                q1.Remove();
            }

            var q2 = knownFiles.Root.XPathSelectElements("./Valid/File")
                     .Where(file => (string)file.Attribute("path") == filename);

            if (q2.Count() == 0)
            {
                knownFiles.Root.XPathSelectElement("./Valid").Add(
                    new XElement("File",
                        new XAttribute("path", filename)));
            }

            knownFiles.Save("knownFiles.xml");

            if (!listBoxValid.Items.Contains(filename))
                listBoxValid.Items.Add(filename);
            if (listBoxInvalid.Items.Contains(filename))
                listBoxInvalid.Items.Remove(filename);
        }
        #endregion

        #region MarkFileInvalid()
        private void MarkFileInvalid(string filename)
        {
            var q1 = knownFiles.Root.XPathSelectElements("./Valid/File")
                     .Where(file => (string)file.Attribute("path") == filename);

            if (q1.Count() > 0)
            {
                q1.Remove();
            }

            var q2 = knownFiles.Root.XPathSelectElements("./Invalid/File")
                     .Where(file => (string)file.Attribute("path") == filename);

            if (q2.Count() == 0)
            {
                knownFiles.Root.XPathSelectElement("./Invalid").Add(
                    new XElement("File",
                        new XAttribute("path", filename)));
            }

            knownFiles.Save("knownFiles.xml");

            if (!listBoxInvalid.Items.Contains(filename))
                listBoxInvalid.Items.Add(filename);
            if (listBoxValid.Items.Contains(filename))
                listBoxValid.Items.Remove(filename);
        }
        #endregion

        #region SaveKnown()
        private void SaveKnown()
        {
            knownFiles.Save("knownFiles.xml");
        }
        #endregion

        #region LoadKnown()
        private void LoadKnown()
        {
            knownFiles = XDocument.Load("knownFiles.xml");
            listBoxInvalid.Items.Clear();
            listBoxValid.Items.Clear();
            foreach (XElement file in knownFiles.Root.XPathSelectElements("./Invalid/File"))
                listBoxInvalid.Items.Add(file.Attribute("path").Value);
            foreach (XElement file in knownFiles.Root.XPathSelectElements("./Valid/File"))
                listBoxValid.Items.Add(file.Attribute("path").Value);
        }
        #endregion
        #endregion

        #region event handlers
        #region Mesh loading events
        #region buttonOpen_Click()
        private void buttonOpen_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                LoadMeshAndUpdateKnownFiles(openFileDialog1.FileName);
            }
        }
        #endregion

        #region buttonScanDXSDK_Click()
        private void buttonScanDXSDK_Click(object sender, EventArgs e)
        {
            string dxsdkdir = Environment.GetEnvironmentVariable("DXSDK_DIR");
            if (string.IsNullOrEmpty(dxsdkdir))
            {
                buttonScanDXSDK.Enabled = false;
                MessageBox.Show("DirectX SDK not installed or environment variable DXSDK_DIR not set");
            }
            else
            {
                string[] files = Directory.GetFiles(System.IO.Path.Combine(dxsdkdir, "Samples\\Media"), "*.x", SearchOption.AllDirectories);
                foreach (string file in files)
                    LoadMeshAndUpdateKnownFiles(file, false);
            }
        }
        #endregion

        #region listBoxValid_SelectedIndexChanged()
        private void listBoxValid_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxValid.SelectedIndex != -1)
            {
                LoadMeshAndUpdateKnownFiles(listBoxValid.SelectedItem.ToString());
                listBoxInvalid.SelectedIndex = -1;
            }
        }
        #endregion

        #region listBoxInvalid_SelectedIndexChanged()
        private void listBoxInvalid_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxInvalid.SelectedIndex != -1)
            {
                LoadMeshAndUpdateKnownFiles(listBoxInvalid.SelectedItem.ToString());
                listBoxValid.SelectedIndex = -1;
            }
        }
        #endregion
        #endregion

        #region Camera operation events
        #region directControl_MouseUp()
        private void directControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDrag = true;
                lastLocation = e.Location;
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

        #region directControl_MouseMove()
        private void directControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrag)
            {
                worldMatrix *= MatrixMath.MatrixRotationX(0.01f * (lastLocation.Y - e.Y));
                worldMatrix *= MatrixMath.MatrixRotationY( 0.01f * (lastLocation.X - e.X) );
                lastLocation = e.Location;
                cbRotate.Checked = false;
            }
        }
        #endregion

        #region OnMouseWheel()
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta != 0)
            {
                float scale;
                if (e.Delta > 0)
                    scale = (0.01f * e.Delta);
                else
                    scale = -100f / e.Delta;
                worldMatrix *= MatrixMath.MatrixScale( scale, scale, scale );
            }
        }
        #endregion
        #endregion

        #region listBox_DrawItem()
        /// <summary>
        /// Handles the DrawItem event of the listBox control.
        /// Displays file names only instead of full file paths for known meshes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.DrawItemEventArgs"/> instance containing the event data.</param>
        private void listBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            ListBox lb = sender as ListBox;
            e.DrawBackground();
            e.Graphics.DrawString(Path.GetFileName((string)lb.Items[e.Index]),
                e.Font, myBrush, e.Bounds, System.Drawing.StringFormat.GenericDefault);
            e.DrawFocusRectangle();
        }
        #endregion

        #region cbWireframe_CheckedChanged()
        private void cbWireframe_CheckedChanged(object sender, EventArgs e)
        {
            lock (meshLock)
            {
                if (mesh != null)
                    mesh.ShowWireFrame = cbWireframe.Checked;
            }
        }
        #endregion
        #endregion

        #region ShowTextInDialog()
        public static Form ShowTextInDialog(string text, string caption)
        {
            Form form = new Form()
            {
                WindowState = FormWindowState.Maximized,
                Text = caption
            };
            TextBox box = new TextBox()
            {
                Dock = DockStyle.Fill,
                AcceptsReturn = true,
                AcceptsTab = true,
                Multiline = true,
                Parent = form,
                Text = text,
                ScrollBars = ScrollBars.Both
            };
            form.ShowDialog();
            return form;
        }
        #endregion
    }
}
