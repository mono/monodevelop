// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent;
using Microsoft.WindowsAPICodePack.DirectX;
using System.Globalization;
using System.Drawing;
using Brush = Microsoft.WindowsAPICodePack.DirectX.Direct2D1.Brush;

namespace D2DPaint
{
    enum Shape
    {
        None,
        Line,
        Bitmap,
        Rectangle,
        RoundedRectangle,
        Ellipse,
        Text,
        Geometry
    }

    public partial class Paint2DForm : Form
    {
        #region Fields
        internal List<Brush> brushes = new List<Brush>();
        internal int currentBrushIndex = -1;

        private Point2F startPoint;
        private Point2F endPoint;
        private BrushDialog brushDialog = null;
        private List<DrawingShape> drawingShapes = new List<DrawingShape>();
        private D2DBitmap currentBitmap;
        private Shape currentShapeType = Shape.None;
        private DrawingShape currentShape = null;
        private bool fill = false;

        internal StrokeStyle TextBoxStroke;

        internal D2DFactory d2dFactory;
        internal ImagingFactory wicFactory;
        internal DWriteFactory dwriteFactory;
        private HwndRenderTarget renderTarget;

        private bool isDrawing = false;
        private bool isDragging = false;

        private float currentStrokeSize = 2;
        private float currentTransparency = 1;

        private readonly ColorF WhiteBackgroundColor = new ColorF(Color.White.ToArgb());
        private TextDialog textDialog;
        private OpenFileDialog bitmapDialog;

        ToolStripButton currentButton = null;

        RenderTargetProperties renderProps = new RenderTargetProperties
        {
            PixelFormat = new PixelFormat(
                Microsoft.WindowsAPICodePack.DirectX.Graphics.Format.B8G8R8A8UNorm,
                AlphaMode.Ignore),
            Usage = RenderTargetUsages.None,
            RenderTargetType = RenderTargetType.Software // Software type is required to allow resource 
                                             // sharing between hardware (HwndRenderTarget) 
                                             // and software (WIC Bitmap render Target).
        };

        #endregion

        #region Paint2DForm()
        public Paint2DForm()
        {
            InitializeComponent();
            for (int i = 0; i < traparencyList.Items.Count; i++)
                traparencyList.Items[i] = ((string)traparencyList.Items[i]).Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator);
            strokeWidths.SelectedItem = "2";
            traparencyList.SelectedIndex = 0;
        } 
        #endregion

        #region renderControl_SizeChanged()
        void renderControl_SizeChanged(object sender, EventArgs e)
        {
            if (renderTarget != null)
            {
                // Resize the render targrt to the actual host size
                SizeU size = new SizeU((uint)renderControl.ClientSize.Width, (uint)renderControl.ClientSize.Height);
                renderTarget.Resize(size);
            }
        } 
        #endregion

        #region CreateDeviceResources()
        /// <summary>
        /// This method creates the render target and associated D2D and DWrite resources
        /// </summary>
        void CreateDeviceResources()
        {
            // Only calls if resources have not been initialize before
            if (renderTarget == null)
            {
                // Create the render target
                SizeU size = new SizeU((uint)renderControl.ClientSize.Width, (uint)renderControl.ClientSize.Height);
                HwndRenderTargetProperties hwndProps = new HwndRenderTargetProperties(renderControl.Handle, size, PresentOptions.RetainContents);
                renderTarget = d2dFactory.CreateHwndRenderTarget(renderProps, hwndProps);

                // Create an initial black brush
                brushes.Add(renderTarget.CreateSolidColorBrush(new ColorF(Color.Black.ToArgb())));
                currentBrushIndex = 0;
            }
        } 
        #endregion

        #region RenderScene()
        private void RenderScene()
        {
            CreateDeviceResources();

            if (renderTarget.IsOccluded)
                return;

            renderTarget.BeginDraw();

            renderTarget.Clear(WhiteBackgroundColor);

            foreach (DrawingShape shape in drawingShapes)
            {
                shape.Draw(renderTarget);
            }

            renderTarget.EndDraw();
        } 
        #endregion

        #region renderControl_Load()
        private void renderControl_Load(object sender, EventArgs e)
        {
            LoadDeviceIndependentResource();
            renderControl.Render = RenderScene;
            currentButton = arrowButton;
        }

        private void LoadDeviceIndependentResource()
        {
            // Create the D2D Factory
            // This really needs to be set to type MultiThreaded if rendering is to be performed by multiple threads,
            // such as if used in a control similar to DirectControl sample control where rendering is done by a dedicated render thread,
            // especially if multiple such controls are used in one application, but also when multiple applications use D2D Factories.
            //
            // In this sample - SingleThreaded type is used because rendering is only done by the main/UI thread and only when required
            // (when the surface gets invalidated) making the risk of synchronization problems - quite low.
            d2dFactory = D2DFactory.CreateFactory(D2DFactoryType.Multithreaded);

            // Create the DWrite Factory
            dwriteFactory = DWriteFactory.CreateFactory();

            // Create the WIC Factory
            wicFactory = ImagingFactory.Create();

            TextBoxStroke = d2dFactory.CreateStrokeStyle(
                new StrokeStyleProperties(
                    CapStyle.Flat, CapStyle.Flat, CapStyle.Round,
                    LineJoin.Miter, 5.0f, DashStyle.Dash, 3f),
                    null);

        }
        #endregion

        #region renderControl_MouseDown()
        private void renderControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (!isDrawing)
                return;

            isDragging = true;
            startPoint.X = e.X;
            startPoint.Y = e.Y;
            endPoint = startPoint;

            switch (currentShapeType)
            {
                case Shape.Line:
                    currentShape = new LineShape(this, startPoint, startPoint, currentStrokeSize, currentBrushIndex);
                    drawingShapes.Add(currentShape);
                    break;
                case Shape.Bitmap:
                    currentShape = new BitmapShape(
                            this,
                            new RectF(startPoint.X, startPoint.Y, startPoint.X + 5, startPoint.Y + 5),
                            currentBitmap, currentTransparency);
                    drawingShapes.Add(currentShape);
                    break;
                case Shape.RoundedRectangle:
                    currentShape = new RoundRectangleShape(this,
                            new RoundedRect(
                                new RectF(startPoint.X, startPoint.Y, startPoint.X, startPoint.Y),
                                20f, 20f),
                                currentStrokeSize,
                                currentBrushIndex, fill);
                    drawingShapes.Add(currentShape);
                    break;
                case Shape.Rectangle:
                    currentShape = new RectangleShape(this,
                                new RectF(startPoint.X, startPoint.Y, startPoint.X, startPoint.Y),
                                currentStrokeSize,
                                currentBrushIndex, fill);
                    drawingShapes.Add(currentShape);
                    break;
                case Shape.Ellipse:
                    currentShape = new EllipseShape(this,
                                new Ellipse(startPoint, 0, 0),
                                currentStrokeSize,
                                currentBrushIndex, fill);
                    drawingShapes.Add(currentShape);
                    break;
                case Shape.Text:
                    currentShape = new TextShape(this, textDialog.TextLayout, startPoint, 100, 100, currentBrushIndex);
                    drawingShapes.Add(currentShape);
                    break;
                case Shape.Geometry:
                    currentShape = new GeometryShape(this, startPoint, currentStrokeSize, currentBrushIndex, fill);
                    drawingShapes.Add(currentShape);
                    break;
            }
            Invalidate();
        } 
        #endregion

        #region renderControl_MouseMove()
        private void renderControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDrawing || !isDragging)
                return;

            endPoint.X = e.X;
            endPoint.Y = e.Y;

            currentShape.EndPoint = endPoint;
            renderControl.Invalidate();
        } 
        #endregion

        #region renderControl_MouseUp()
        private void renderControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isDragging || !isDrawing)
                return;
            currentShape.EndDraw();
            isDragging = false;
            renderControl.Invalidate();
        } 
        #endregion

        #region SwitchDrawMode()
        private void SwitchDrawMode(object currentModeButton)
        {

            isDrawing = true;

            // Unselect the previous button
            if (currentButton != null)
                currentButton.Checked = false;

            // Select the new button
            currentButton = currentModeButton as ToolStripButton;
            if (currentButton != null)
                currentButton.Checked = true;
        } 
        #endregion

        #region lineButton_Click()
        private void lineButton_Click(object sender, EventArgs e)
        {
            currentShapeType = Shape.Line;
            SwitchDrawMode(sender);
        } 
        #endregion

        #region rectButton_Click()
        private void rectButton_Click(object sender, EventArgs e)
        {
            currentShapeType = Shape.Rectangle;
            SwitchDrawMode(sender);
        }
        #endregion

        #region roundrectButton_Click()
        private void roundrectButton_Click(object sender, EventArgs e)
        {
            currentShapeType = Shape.RoundedRectangle;
            SwitchDrawMode(sender);
        } 
        #endregion

        #region ellipseButton_Click()
        private void ellipseButton_Click(object sender, EventArgs e)
        {
            currentShapeType = Shape.Ellipse;
            SwitchDrawMode(sender);
        }
        #endregion

        #region bitmapButton_Click()
        private void bitmapButton_Click(object sender, EventArgs e)
        {
            if (bitmapDialog == null)
            {
                bitmapDialog = new OpenFileDialog();
                bitmapDialog.DefaultExt = "*.jpg;*.png";
            }
            if (bitmapDialog.ShowDialog() == DialogResult.OK)
            {
                string filename = bitmapDialog.FileName;
                currentBitmap = BitmapUtilities.LoadBitmapFromFile(renderTarget, wicFactory, filename);

                currentShapeType = Shape.Bitmap;
                SwitchDrawMode(sender);
            }
        }
        #endregion

        #region textButton_Click()
        private void textButton_Click(object sender, EventArgs e)
        {
            if (textDialog == null)
            {
                textDialog = new TextDialog(this);
            }

            if (textDialog.ShowDialog() == DialogResult.OK)
            {
                currentShapeType = Shape.Text;
                SwitchDrawMode(sender);
            }
        } 
        #endregion

        #region geometryButton_Click()
        private void geometryButton_Click(object sender, EventArgs e)
        {
            currentShapeType = Shape.Geometry;
            SwitchDrawMode(sender);
        } 
        #endregion

        #region brushButton_Click()
        private void brushButton_Click(object sender, EventArgs e)
        {
            if (brushDialog == null || brushDialog.IsDisposed)
                brushDialog = new BrushDialog(this, renderTarget);

            brushDialog.Show();
            brushDialog.Activate();
        } 
        #endregion

        #region fillButton_Click()
        private void fillButton_Click(object sender, EventArgs e)
        {
            fill = !fill;
        } 
        #endregion

        #region strokeWidths_SelectedIndexChanged()
        private void strokeWidths_SelectedIndexChanged(object sender, EventArgs e)
        {
            float f;
            if (float.TryParse(strokeWidths.Text as string, out f))
            {
                this.currentStrokeSize = f;
            }
        } 
        #endregion

        #region traparencyList_SelectedIndexChanged()
        private void traparencyList_SelectedIndexChanged(object sender, EventArgs e)
        {
            float f;
            if (float.TryParse(traparencyList.Text as string, out f))
            {
                this.currentTransparency = f;
            }
        } 
        #endregion

        #region clearButton_Click()
        private void clearButton_Click(object sender, EventArgs e)
        {
            drawingShapes.Clear();
            renderControl.Invalidate();
        } 
        #endregion

        private void toolStrip1_MouseEnter(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        private void renderControl_MouseEnter(object sender, EventArgs e)
        {
            if (isDrawing)
            {
                this.Cursor = Cursors.Cross;
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            this.isDrawing = false;
            this.currentShape = null;

            if (currentButton != null)
                currentButton.Checked = false;

            currentButton = arrowButton;
            arrowButton.Checked = true;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (renderTarget == null)
            {
                // Should not happen
                MessageBox.Show("Unable to save file.");
                return;
            }

            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "Bitmap image (*.bmp)|*.bmp|Png image (*.png)|*.png|Jpeg image (*.jpg)|*.jpg|Gif image (*.gif)|*.gif";
            if (DialogResult.OK == saveDlg.ShowDialog())
            {
                SizeU size = new SizeU((uint)ClientSize.Width, (uint)ClientSize.Height);

                ImagingBitmap wicBitmap = wicFactory.CreateImagingBitmap(
                    size.Width,
                    size.Height,
                    PixelFormats.Bgr32Bpp,
                    BitmapCreateCacheOption.CacheOnLoad);

                D2DBitmap d2dBitmap = renderTarget.CreateBitmap(size, new BitmapProperties(new PixelFormat(Microsoft.WindowsAPICodePack.DirectX.Graphics.Format.B8G8R8A8UNorm, AlphaMode.Ignore), renderTarget.Dpi.X, renderTarget.Dpi.Y));
                d2dBitmap.CopyFromRenderTarget(renderTarget);

                RenderTarget wicRenderTarget = 
                    d2dFactory.CreateWicBitmapRenderTarget(wicBitmap, renderProps);

                wicRenderTarget.BeginDraw();

                wicRenderTarget.DrawBitmap(d2dBitmap);
                wicRenderTarget.EndDraw();
                
                Guid fileType;
                switch (saveDlg.FilterIndex)
                {
                    case 1: fileType = ContainerFormats.Bmp;
                            break;
                    case 2: fileType = ContainerFormats.Png;
                            break;
                    case 3: fileType = ContainerFormats.Jpeg;
                            break;
                    case 4: fileType = ContainerFormats.Gif;
                            break;
                    default: fileType = ContainerFormats.Bmp; // default to bitmap files
                            break;
                }

                wicBitmap.SaveToFile(wicFactory, fileType, saveDlg.FileName);
            }
            
        }

    }
}
