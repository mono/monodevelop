// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.DirectX;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;

namespace D2DShapes
{
    /// <summary>
    /// A control for managing a list and rendering DrawingShape objects
    /// </summary>
    public sealed class D2DShapesControl : UserControl
    {
        #region enum RenderModes
        public enum RenderModes
        {
            /// <summary>
            /// Draw through device context in OnPaint (when the window gets invalidated)
            /// </summary>
            DCRenderTarget = 0,
            /// <summary>
            /// Use BitmapRenderTarget to draw on updates, copy the bitmap to DCRenderTarget in OnPaint (when the window gets invalidated)
            /// </summary>
            BitmapRenderTargetOnPaint,
            /// <summary>
            /// Use BitmapRenderTarget to draw on updates, copy the bitmap to HwndRenderTarget in real time
            /// </summary>
            BitmapRenderTargetRealTime,
            /// <summary>
            /// Draw directly on window in real time
            /// </summary>
            HwndRenderTarget,
        }; 
        #endregion

        #region Fields
        //fields for shared factory use
        internal static D2DFactory sharedD2DFactory;
        internal static ImagingFactory sharedWicFactory;
        internal static DWriteFactory sharedDwriteFactory;
        internal static object sharedSyncObject = new object();
        internal static int sharedRefCount;

        //object used for synchronization, so shape list changes, (de)initialization, configuration changes and rendering are not done concurrently
        private readonly Object renderSyncObject;

        //factory objects
        internal D2DFactory d2DFactory;
        internal ImagingFactory wicFactory;
        internal DWriteFactory dwriteFactory;

        //common random object
        internal Random random = new Random(Environment.TickCount + seedDelta++);
        //used to have different random objects for all instances of this class
        private static int seedDelta;

        //render target used in real time rendering modes (can also be used OnPaint, but does not use a device context)
        private HwndRenderTarget hwndRenderTarget;
        //compatible bitmap that is used in cached modes, in which only changes to the image are drawn to the bitmap and the bitmap is drawn to screen when needed
        private BitmapRenderTarget bitmapRenderTarget;
        //device context (DC) render target - used with the Graphics object to render to DC
        private DCRenderTarget dcRenderTarget;

        //shapes to be drawn
        private readonly List<DrawingShape> drawingShapes = new List<DrawingShape>();
        //stack of shapes taken off the drawingShapes list that could be thrown back there
        private readonly Stack<DrawingShape> peelings = new Stack<DrawingShape>();

        //fields for FPS calculations
        private int lastTickCount;
        private int frameCount;

        //various statistics that invoke StatsChanged when they are updated
        private readonly Dictionary<string, int> stats = new Dictionary<string, int>();
        //statistics as a string
        public string Stats = "";

        //GDI brushes used to draw background and stub text on a non-initialized control
        private System.Drawing.Brush backgroundBrush = SystemBrushes.Control;
        private System.Drawing.Brush foregroundBrush = SystemBrushes.ControlText;

        //object state management variables
        private bool isInitialized;
        private bool isInitializing;
        private bool disposed;

        //background render thread
        private readonly Thread renderThread;
        //reset event for ending the render thread
        private readonly ManualResetEvent killThread;
        //delegate for rendering in background
        public delegate void RenderHandler();
        #endregion

        #region Properties
        #region Render
        private RenderHandler render;
        /// <summary>
        /// Gets or sets the handler of the render event for background rendering thread in real time modes.
        /// </summary>
        /// <value>The render.</value>
        public RenderHandler Render
        {
            get
            {
                lock (renderSyncObject)
                {
                    return render;
                }
            }
            set
            {
                lock (renderSyncObject)
                {
                    render = value;
                }
            }
        }
        #endregion

        #region IsInitialized
        /// <summary>
        /// Gets a value indicating whether this instance is initialized.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is initialized; otherwise, <c>false</c>.
        /// </value>
        public bool IsInitialized
        {
            get { return isInitialized &&
                d2DFactory != null &&
                d2DFactory.NativeInterface != IntPtr.Zero;
            }
        } 
        #endregion

        #region StatsChanged
        private event EventHandler statsChanged;
        /// <summary>
        /// Occurs after statistics change (eg. when shapes are added/removed)
        /// </summary>
        public event EventHandler StatsChanged
        {
            add
            {
                statsChanged += value;
            }
            remove
            {
                statsChanged -= value;
            }
        } 
        #endregion

        #region RenderMode
        private RenderModes renderMode;
        /// <summary>
        /// Gets or sets the render mode.
        /// See RenderModes enum description for descriptions of particular modes implemented
        /// </summary>
        /// <value>The render mode.</value>
        public RenderModes RenderMode
        {
            get { return renderMode; }
            set
            {
                SetRenderMode(value);
            }
        }
        #endregion

        #region UsingCompatibleRenderTarget
        /// <summary>
        /// Gets a value indicating whether a compatible render target bitmap is used for cached rendering.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [using compatible render target]; otherwise, <c>false</c>.
        /// </value>
        private bool UsingCompatibleRenderTarget
        {
            get
            {
                return renderMode == RenderModes.BitmapRenderTargetOnPaint ||
                       renderMode == RenderModes.BitmapRenderTargetRealTime;
            }
        } 
        #endregion

        #region RenderTarget
        /// <summary>
        /// render target to use when drawing shapes
        /// </summary>
        internal RenderTarget RenderTarget
        {
            get
            {
                lock (renderSyncObject)
                {
                    switch (renderMode)
                    {
                        case RenderModes.BitmapRenderTargetOnPaint:
                        case RenderModes.BitmapRenderTargetRealTime:
                            return bitmapRenderTarget;
                        case RenderModes.DCRenderTarget:
                            return dcRenderTarget;
                        default:
                            return hwndRenderTarget;
                    }
                }
            }
        }
        #endregion

        #region BackColorF
        private ColorF backColorF;
        /// <summary>
        /// Gets the (float) back color used for clearing the background - it is dependent on the BackColor property.
        /// </summary>
        /// <value>The back color F.</value>
        private ColorF BackColorF
        {
            get
            {
                return backColorF;
            }
        }
        #endregion

        #region Bitmap
        private D2DBitmap bitmap;
        /// <summary>
        /// Gets the bitmap shared among shape objects. Loads the bitmap if not initialized and rendertarget and wicFactory are available.
        /// Set bitmap to null to reload it after changing the render target
        /// </summary>
        /// <value>The bitmap.</value>
        internal D2DBitmap Bitmap
        {
            get
            {
                if (bitmap == null && RenderTarget != null && wicFactory != null)
                {
                    using (Stream stream = typeof(D2DShapesControl).Assembly.GetManifestResourceStream("D2DShapes.Peacock.jpg"))
                        bitmap = BitmapUtilities.LoadBitmapFromStream(RenderTarget, wicFactory, stream);
                }
                return bitmap;
            }
        }
        #endregion

        #region Fps
        /// <summary>
        /// Gets or sets the number of frames drawn per second.
        /// Is updated when the image is redrawn after at least a second since the last calculation.
        /// See CalculateFPS().
        /// </summary>
        /// <value>The FPS.</value>
        public float Fps { get; private set; }
        #endregion

        #region FpsChanged
        private event EventHandler fpsChanged;
        /// <summary>
        /// Occurs when Fps property value changes.
        /// </summary>
        public event EventHandler FpsChanged
        {
            add
            {
                fpsChanged += value;
            }
            remove
            {
                fpsChanged -= value;
            }
        }
        #endregion
        #endregion

        #region D2DShapesControl() - CTOR
        public D2DShapesControl(IContainer components)
        {
            components.Add(this);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();

            backColorF = new ColorF(
                BackColor.R / 256f,
                BackColor.G / 256f,
                BackColor.B / 256f,
                1.0f);

            //Initialize the background render thread and synchronization objects
            renderSyncObject = new object();
            killThread = new ManualResetEvent(false);
			ThreadStart ts = RenderThreadProcedure;
			renderThread = new Thread( ts );
			renderThread.Start();
        } 
        #endregion

        #region Methods
        #region WndProc()
        protected override void WndProc(ref Message m)
        {
            //kill and wait for render thread to complete when window gets destroyed
            if (m.Msg == 0x0002/*WM_DESTROY*/ )
            {
                killThread.Set();
                renderThread.Join();
            }
            base.WndProc(ref m);
        }
        #endregion

        #region OnParentChanged()
        /// <summary>
        /// Stop rendering if removed from a parent control
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnParentChanged(EventArgs e)
        {
            lock (renderSyncObject)
            {
                if (Parent == null)
                    render = null;
            }
            base.OnParentChanged(e);
        }
        #endregion

        #region Dispose()
        /// <summary>
        /// Dispose of resources (IDisposable implementation)
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                lock (renderSyncObject)
                {
                    render = null;

                    lock (sharedSyncObject)
                    {
                        if (sharedD2DFactory != null && d2DFactory == sharedD2DFactory)
                            sharedRefCount--;

                        if (d2DFactory != null && d2DFactory != sharedD2DFactory)
                            d2DFactory.Dispose();
                        d2DFactory = null;

                        if (dwriteFactory != null && dwriteFactory != sharedDwriteFactory)
                            dwriteFactory.Dispose();
                        dwriteFactory = null;

                        if (wicFactory != null && wicFactory != sharedWicFactory)
                            wicFactory.Dispose();
                        wicFactory = null;

                        if (sharedRefCount == 0)
                        {
                            if (sharedD2DFactory != null)
                                sharedD2DFactory.Dispose();
                            sharedD2DFactory = null;

                            if (sharedDwriteFactory != null)
                                sharedDwriteFactory.Dispose();
                            sharedDwriteFactory = null;

                            if (sharedWicFactory != null)
                                sharedWicFactory.Dispose();
                            sharedWicFactory = null;
                        }
                    }

                    foreach (DrawingShape shape in drawingShapes)
                    {
                        shape.Dispose();
                    }

                    if (bitmap != null)
                        bitmap.Dispose();
                    bitmap = null;

                    if (dcRenderTarget != null)
                        dcRenderTarget.Dispose();
                    dcRenderTarget = null;
                    if (bitmapRenderTarget != null)
                        bitmapRenderTarget.Dispose();
                    bitmapRenderTarget = null;
                    if (hwndRenderTarget != null)
                        hwndRenderTarget.Dispose();
                    hwndRenderTarget = null;


                    disposed = true;
                }
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Initialize()
        /// <summary>
        /// Initializes rendering.
        /// </summary>
        public void Initialize()
        {
            lock (renderSyncObject)
            {
                isInitializing = true;
                CreateFactories();
                CreateDeviceResources();
                isInitializing = false;
                isInitialized = true;
            }
        }
        #endregion

        #region CreateFactories()
        private void CreateFactories()
        {
            //reuse factories except for random cases
            if (random.NextDouble() < 0.5)
            {
                lock (sharedSyncObject)
                {
                    if (sharedD2DFactory == null)
                    {
                        // Create the D2D Factory
                        sharedD2DFactory = D2DFactory.CreateFactory(D2DFactoryType.SingleThreaded);

                        // Create the DWrite Factory
                        sharedDwriteFactory = DWriteFactory.CreateFactory();

                        // Create the WIC Factory
                        sharedWicFactory = ImagingFactory.Create();

                        Debug.Assert(sharedD2DFactory.NativeInterface != IntPtr.Zero);
                        Debug.Assert(sharedDwriteFactory.NativeInterface != IntPtr.Zero);
                        Debug.Assert(sharedWicFactory.NativeInterface != IntPtr.Zero);
                    }
                    sharedRefCount++;
                }
                d2DFactory = sharedD2DFactory;
                dwriteFactory = sharedDwriteFactory;
                wicFactory = sharedWicFactory;
                Debug.Assert(d2DFactory.NativeInterface != IntPtr.Zero);
                Debug.Assert(dwriteFactory.NativeInterface != IntPtr.Zero);
                Debug.Assert(wicFactory.NativeInterface != IntPtr.Zero);
            }
            else
            {
                // Create the D2D Factory
                d2DFactory = D2DFactory.CreateFactory(D2DFactoryType.Multithreaded);

                // Create the DWrite Factory
                dwriteFactory = DWriteFactory.CreateFactory();

                // Create the WIC Factory
                wicFactory = ImagingFactory.Create();
                Debug.Assert(d2DFactory.NativeInterface != IntPtr.Zero);
                Debug.Assert(dwriteFactory.NativeInterface != IntPtr.Zero);
                Debug.Assert(wicFactory.NativeInterface != IntPtr.Zero);
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
            //if (RenderTarget != null)
            //    return;
            SetRenderMode(renderMode);
        }
        #endregion

        #region SetRenderMode()
        private void SetRenderMode(RenderModes rm)
        {
            lock (renderSyncObject)
            {
                renderMode = rm;
                if (!IsInitialized && !isInitializing)
                    return;

                //clean up objects that will be invalid after RenderTarget change
                if (dcRenderTarget != null)
                {
                    dcRenderTarget.Dispose();
                    dcRenderTarget = null;
                }
                if (hwndRenderTarget != null)
                {
                    hwndRenderTarget.Dispose();
                    hwndRenderTarget = null;
                }
                if (bitmapRenderTarget != null)
                {
                    bitmapRenderTarget.Dispose();
                    bitmapRenderTarget = null;
                }
                peelings.Clear();
                bitmap = null; //the bitmap created in dc render target can't be used in hwnd render target

                // Create the screen render target
                var size = new SizeU((uint)ClientSize.Width, (uint)ClientSize.Height);
                var props = new RenderTargetProperties
                {
                    PixelFormat = new PixelFormat(
                        Format.B8G8R8A8UNorm,
                        AlphaMode.Ignore),
                    Usage = RenderTargetUsages.GdiCompatible
                };

                if (renderMode == RenderModes.DCRenderTarget || renderMode == RenderModes.BitmapRenderTargetOnPaint)
                {
                    dcRenderTarget = d2DFactory.CreateDCRenderTarget(props);
                    if (renderMode == RenderModes.BitmapRenderTargetOnPaint)
                    {
                        bitmapRenderTarget =
                            dcRenderTarget.CreateCompatibleRenderTarget(
                            CompatibleRenderTargetOptions.GdiCompatible,
                            new Microsoft.WindowsAPICodePack.DirectX.Direct2D1.SizeF(ClientSize.Width, ClientSize.Height));
                    }
                    render = null;
                }
                else
                {
                    hwndRenderTarget = d2DFactory.CreateHwndRenderTarget(
                        props,
                        new HwndRenderTargetProperties(Handle, size, Microsoft.WindowsAPICodePack.DirectX.Direct2D1.PresentOptions.RetainContents));
                    if (renderMode == RenderModes.BitmapRenderTargetRealTime)
                    {
                        bitmapRenderTarget =
                            hwndRenderTarget.CreateCompatibleRenderTarget(
                            CompatibleRenderTargetOptions.GdiCompatible,
                            new Microsoft.WindowsAPICodePack.DirectX.Direct2D1.SizeF(ClientSize.Width, ClientSize.Height));
                    }
                    render = RenderSceneInBackground;
                }

                //move all shapes to new rendertarget and refresh
                foreach (var shape in drawingShapes)
                {
                    shape.Bitmap = Bitmap;
                    shape.RenderTarget = RenderTarget;
                }
                RefreshAll();
            }
        }
        #endregion

        #region OnResize()
        protected override void OnResize(EventArgs e)
        {
            lock (renderSyncObject)
            {
                if (RenderTarget != null)
                {
                    // Resize the render targrt to the actual host size
                    var size = new SizeU((uint)ClientSize.Width, (uint)ClientSize.Height);
                    if (hwndRenderTarget != null)
                        hwndRenderTarget.Resize(size); //need to resize hwndRenderTarget to make its size same as the window's size
                    if (renderMode == RenderModes.BitmapRenderTargetOnPaint)
                    {
                        bitmapRenderTarget.Dispose();
                        bitmapRenderTarget = dcRenderTarget.CreateCompatibleRenderTarget(CompatibleRenderTargetOptions.GdiCompatible, new Microsoft.WindowsAPICodePack.DirectX.Direct2D1.SizeF(ClientSize.Width, ClientSize.Height));
                        bitmap = null; //the bitmap created in dc render target can't be used in hwnd render target
                        foreach (var shape in drawingShapes)
                        {
                            shape.Bitmap = Bitmap;
                            shape.RenderTarget = RenderTarget;
                        }
                        RefreshAll();
                    }
                    else if (renderMode == RenderModes.BitmapRenderTargetRealTime)
                    {
                        Debug.Assert(hwndRenderTarget != null);//this should never be null considering the above
                        bitmapRenderTarget.Dispose();
                        bitmapRenderTarget = hwndRenderTarget.CreateCompatibleRenderTarget(CompatibleRenderTargetOptions.GdiCompatible, new Microsoft.WindowsAPICodePack.DirectX.Direct2D1.SizeF(ClientSize.Width, ClientSize.Height));
                        bitmap = null; //the bitmap created in dc render target can't be used in hwnd render target
                        foreach (var shape in drawingShapes)
                        {
                            shape.Bitmap = Bitmap;
                            shape.RenderTarget = RenderTarget;
                        }
                        RefreshAll();
                    }
                }
            }
            base.OnResize(e);
        }
        #endregion

        #region OnBackColorChanged()
        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            backColorF = new ColorF(
                BackColor.R / 256f,
                BackColor.G / 256f,
                BackColor.B / 256f,
                1.0f);
            backgroundBrush = new SolidBrush(BackColor);
        }
        #endregion

        #region OnForeColorChanged()
        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            foregroundBrush = new SolidBrush(ForeColor);
        }
        #endregion

        #region OnPaintBackground()
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (!IsInitialized || DesignMode)
            {
                e.Graphics.FillRectangle(backgroundBrush, ClientRectangle);
            }
        }
        #endregion

        #region OnPaint()
        /// <summary>
        /// for use with DcRenderTarget (though it would also work with HwndRenderTarget if GDI interop was not used)
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!IsInitialized || DesignMode)
            {
                e.Graphics.DrawString("D2DShapesControl", Font, foregroundBrush, (float)Width / 2, (float)Height / 2);
            }
            else
            {
                lock (renderSyncObject)
                {
                    if (renderMode == RenderModes.DCRenderTarget ||
                        renderMode == RenderModes.BitmapRenderTargetOnPaint)
                    {
                        if (dcRenderTarget == null)
                            return;
                        CalculateFPS();
                        if (renderMode == RenderModes.DCRenderTarget)
                        {
                            //render scene directly to DC
                            dcRenderTarget.BindDC(e.Graphics.GetHdc(), new Rect(Left, Top, Right, Bottom));
                            RenderScene(dcRenderTarget);
                            e.Graphics.ReleaseHdc();
                            e.Graphics.DrawString(string.Format("OnPaint({0}) direct DC draw", DateTime.Now), Font, Brushes.White, 0, 2);
                            e.Graphics.DrawString(string.Format("OnPaint({0}) direct DC draw", DateTime.Now), Font, Brushes.Black, 1, 2);
                        }
                        else if (renderMode == RenderModes.BitmapRenderTargetOnPaint)
                        {
                            //draw bitmap cache of the shapes to DC
                            dcRenderTarget.BindDC(e.Graphics.GetHdc(), new Rect(Left, Top, Right, Bottom));
                            dcRenderTarget.BeginDraw();
                            dcRenderTarget.DrawBitmap(bitmapRenderTarget.Bitmap, 1.0f,
                                                      BitmapInterpolationMode.NearestNeighbor,
                                                      new RectF(0, 0, Width, Height));
                            dcRenderTarget.EndDraw();
                            e.Graphics.ReleaseHdc();
                            e.Graphics.DrawString(string.Format("OnPaint({0}) DC DrawBitmap", DateTime.Now), Font, Brushes.White, 0, 2);
                            e.Graphics.DrawString(string.Format("OnPaint({0}) DC DrawBitmap", DateTime.Now), Font, Brushes.Black, 1, 2);
                        }
                    }
                }
            }
        }
        #endregion

        #region RenderThreadProcedure()
        /// <summary>
        /// The render thread procedure - calls.
        /// </summary>
        private void RenderThreadProcedure()
        {
            do
            {
                lock (renderSyncObject)
                {
                    if (Render != null)
                        Render();
                }
                if (Render == null)
                    Thread.Sleep(1);
            }
            while (killThread.WaitOne(0) == false);
        }
        #endregion

        #region RenderSceneInBackground()
        //used with HwndRenderTarget
        private void RenderSceneInBackground()
        {
            if (Parent == null || render == null)
                return;
            if (renderMode == RenderModes.HwndRenderTarget ||
                renderMode == RenderModes.BitmapRenderTargetRealTime)
            {
                if (hwndRenderTarget == null || hwndRenderTarget.IsOccluded)
                    return;
                CalculateFPS();
                if (renderMode == RenderModes.HwndRenderTarget)
                {
                    //render scene directly on the control
                    RenderScene(hwndRenderTarget);
                }
                else if (renderMode == RenderModes.BitmapRenderTargetRealTime)
                {
                    //draw bitmap cache of the shapes to control
                    hwndRenderTarget.BeginDraw();
                    hwndRenderTarget.DrawBitmap(bitmapRenderTarget.Bitmap, 1.0f,
                                                BitmapInterpolationMode.NearestNeighbor,
                                                new RectF(0, 0, Width, Height));
                    hwndRenderTarget.EndDraw();
                }
            }
        }
        #endregion

        #region RenderScene()
        /// <summary>
        /// Renders the scene to the given render target.
        /// Clears the scene, then draws all shapes
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        private void RenderScene(RenderTarget renderTarget)
        {
            Cursor c = null;
            if (renderMode != RenderModes.HwndRenderTarget)
            {
                c = Cursor;
                Cursor = Cursors.WaitCursor;
            }
            renderTarget.BeginDraw();
            renderTarget.Clear(BackColorF);

            for (int i = 0; i < drawingShapes.Count; i++)
            {
                DrawingShape shape = drawingShapes[i];
                //tag with shape index for debugging
                renderTarget.Tags = new Tags((ulong)i, 0);
                shape.Draw(renderTarget);
            }
            Tags tags;
            ErrorCode errorCode;
            if (!renderTarget.TryEndDraw(out tags, out errorCode))
            {
                Debug.WriteLine(String.Format("Failed EndDraw. Error: {0}, tag1: {1}, tag2: {2}, shape[{1}]: {3}",
                    errorCode, tags.Tag1, tags.Tag2,
                    (int)tags.Tag1 < drawingShapes.Count ? drawingShapes[(int)tags.Tag1].ToString() : "<none>"));
            }
            if (renderMode != RenderModes.HwndRenderTarget)
                Cursor = c;
        }
        #endregion

        #region RefreshCompatibleRenderTarget()
        /// <summary>
        /// Refreshes the compatible render target - the bitmap used for caching the scene 
        /// for quick rendering when the control is redrawn
        /// </summary>
        private void RefreshCompatibleRenderTarget()
        {
            RenderScene(bitmapRenderTarget);
        }
        #endregion

        #region AddToCompatibleRenderTarget()
        /// <summary>
        /// Adds a shape to compatible render target - avoids the need to redraw all shapes.
        /// </summary>
        /// <param name="shape">The shape.</param>
        private void AddToCompatibleRenderTarget(DrawingShape shape)
        {
            lock (renderSyncObject)
            {
                bitmapRenderTarget.BeginDraw();
                shape.Draw(bitmapRenderTarget);
                bitmapRenderTarget.EndDraw();
            }
        }
        #endregion

        #region CalculateFPS()
        /// <summary>
        /// Calculates Frames Per Second if at least a second passed since previous update.
        /// Should be called whenever a frame is drawn on the control
        /// </summary>
        private void CalculateFPS()
        {
            int currentTickCount = Environment.TickCount;
            int ticks = currentTickCount - lastTickCount;
            if (ticks >= 1000)
            {
                Fps = (float)frameCount * 1000 / ticks;
                frameCount = 0;
                lastTickCount = currentTickCount;
                BeginInvoke(new MethodInvoker(delegate
                                                  {
                                                      if (fpsChanged != null)
                                                          fpsChanged(this, EventArgs.Empty);
                                                  }));
            }
            frameCount++;
        }
        #endregion

        #region ClearShapes()
        /// <summary>
        /// Clears all the shapes and invalidates the control and bitmap cache if one exists.
        /// </summary>
        public void ClearShapes()
        {
            lock (renderSyncObject)
            {
                drawingShapes.Clear();
                stats.Clear();
                Stats = "";
                if (statsChanged != null)
                    statsChanged(this, EventArgs.Empty);
                RefreshAll();
            }
        }
        #endregion

        #region PeelShape()
        /// <summary>
        /// Peels the shape from the end(top) of the list,
        /// updates the render targets and puts the peeled shape to the stack of peelings.
        /// </summary>
        /// <returns></returns>
        internal DrawingShape PeelShape()
        {
            lock (renderSyncObject)
            {
                if (drawingShapes.Count == 0)
                    return null;
                string statName = drawingShapes[drawingShapes.Count - 1].GetType().Name.Replace("Shape", " count");
                UpdateStats(statName, -1);
                peelings.Push(drawingShapes[drawingShapes.Count - 1]);
                drawingShapes.RemoveAt(drawingShapes.Count - 1);
                RefreshAll();
                return peelings.Peek();
            }
        }

        /// <summary>
        /// Peels the specific shape.
        /// </summary>
        /// <param name="shape">The shape.</param>
        internal void PeelShape(DrawingShape shape)
        {
            Debug.Assert(shape != null);
            lock (renderSyncObject)
            {
                string statName = shape.GetType().Name.Replace("Shape", " count");
                UpdateStats(statName, -1);
                peelings.Push(shape);
                PeelShape(shape, drawingShapes);
                RefreshAll();
            }
        }

        /// <summary>
        /// Peels the shape recursively.
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="shapes">The shapes.</param>
        /// <returns></returns>
        private static bool PeelShape(DrawingShape shape, ICollection<DrawingShape> shapes)
        {
            foreach (var s in shapes)
            {
                if (s == shape)
                {
                    shapes.Remove(shape);
                    return true;
                }
                if (s.ChildShapes != null && PeelShape(shape, s.ChildShapes))
                    return true;
            }
            return false;
        }
        #endregion

        #region PeelAt()
        /// <summary>
        /// Peels the top shape at a given point,
        /// updates the render targets and puts the peeled shape to the stack of peelings.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        internal DrawingShape PeelAt(Point2F point)
        {
            lock (renderSyncObject)
            {
                return PeelAtRecursive(point, drawingShapes);
            }
        }
        #endregion

        #region PeelAtRecursive()
        /// <summary>
        /// Peels the top shape at a given point,
        /// updates the render targets and puts the peeled shape to the stack of peelings.
        /// Used to enable removing a child shape - eg. a top shape in a layer
        /// </summary>
        /// <param name="point"></param>
        /// <param name="shapes"></param>
        /// <returns></returns>
        private DrawingShape PeelAtRecursive(Point2F point, IList<DrawingShape> shapes)
        {
            for (int i = shapes.Count - 1; i >= 0; i--)
            {
                if (shapes[i].HitTest(point))
                {
                    if (shapes[i].ChildShapes == null)
                    {
                        string statName = shapes[i].GetType().Name.Replace("Shape", " count");
                        UpdateStats(statName, -1);
                        peelings.Push(shapes[i]);
                        shapes.RemoveAt(i);
                        RefreshAll();
                        return peelings.Peek();
                    }
                    DrawingShape shapePeeled = PeelAtRecursive(point, shapes[i].ChildShapes);
                    if (shapePeeled == null)
                    {
                        string statName = shapes[i].GetType().Name.Replace("Shape", " count");
                        UpdateStats(statName, -1);
                        peelings.Push(shapes[i]);
                        shapes.RemoveAt(i);
                        RefreshAll();
                        return peelings.Peek();
                    }
                    return shapePeeled;
                }
            }
            return null;
        }
        #endregion

        #region UnpeelShape()
        /// <summary>
        /// Puts a shape from the stack of peelings back to the list of shapes and invalidates the render targets.
        /// </summary>
        /// <returns></returns>
        internal DrawingShape UnpeelShape()
        {
            lock (renderSyncObject)
            {
                if (peelings.Count == 0)
                    return null;
                DrawingShape shape = peelings.Peek();
                string statName = shape.GetType().Name.Replace("Shape", " count");
                UpdateStats(statName, 1);
                drawingShapes.Add(peelings.Pop());
                if (UsingCompatibleRenderTarget)
                    AddToCompatibleRenderTarget(shape);
                InvalidateClientRectangle();
                return shape;
            }
        }
        #endregion

        #region AddShape~()
        #region AddShape()
        /// <summary>
        /// Adds the shape to the list and updates the render targets.
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <returns></returns>
        private DrawingShape AddShape(DrawingShape shape)
        {
            lock (renderSyncObject)
            {
                drawingShapes.Add(shape);
                string statName = shape.GetType().Name.Replace("Shape", " count");
                UpdateStats(statName, 1);
                if (UsingCompatibleRenderTarget)
                    AddToCompatibleRenderTarget(shape);
                InvalidateClientRectangle();
                return shape;
            }
        }
        #endregion

        #region AddRandomShape()
        /// <summary>
        /// Adds a random shape.
        /// </summary>
        /// <returns></returns>
        public DrawingShape AddRandomShape()
        {
            double which = random.NextDouble();
            if (which < 0.1)
                return AddLine();
            if (which < 0.2)
                return AddRectangle();
            if (which < 0.3)
                return AddRoundRect();
            if (which < 0.4)
                return AddEllipse();
            if (which < 0.5)
                return AddText();
            if (which < 0.6)
                return AddBitmap();
            if (which < 0.7)
                return AddGeometry();
            if (which < 0.8)
                return AddMesh();
            if (which < 0.9)
                return AddGDIEllipses(5);
            return AddLayer(5);
        }
        #endregion

        #region AddRandomShapes(count)
        /// <summary>
        /// Adds [count] random shapes.
        /// </summary>
        /// <param name="count">The count.</param>
        public void AddRandomShapes(int count)
        {
            for (int i = 0; i < count; i++)
                AddRandomShape();
        }
        #endregion

        #region AddLine()
        /// <summary>
        /// Adds a random line.
        /// </summary>
        /// <returns></returns>
        public DrawingShape AddLine()
        {
            return AddShape(new LineShape(RenderTarget, random, d2DFactory, Bitmap));
        }
        #endregion

        #region AddRectangle()
        /// <summary>
        /// Adds a random rectangle.
        /// </summary>
        /// <returns></returns>
        public DrawingShape AddRectangle()
        {
            return AddShape(new RectangleShape(RenderTarget, random, d2DFactory, Bitmap));
        }
        #endregion

        #region AddRoundRect()
        /// <summary>
        /// Adds a random round rect.
        /// </summary>
        /// <returns></returns>
        public DrawingShape AddRoundRect()
        {
            return AddShape(new RoundRectangleShape(RenderTarget, random, d2DFactory, Bitmap));
        }
        #endregion

        #region AddEllipse()
        /// <summary>
        /// Adds a random ellipse.
        /// </summary>
        /// <returns></returns>
        public DrawingShape AddEllipse()
        {
            return AddShape(new EllipseShape(RenderTarget, random, d2DFactory, Bitmap));
        }
        #endregion

        #region AddText()
        /// <summary>
        /// Adds a random text.
        /// </summary>
        /// <returns></returns>
        public DrawingShape AddText()
        {
            if (random.NextDouble() < 0.5)
                return AddShape(new TextShape(RenderTarget, random, d2DFactory, Bitmap, dwriteFactory));
            return AddShape(new TextLayoutShape(RenderTarget, random, d2DFactory, Bitmap, dwriteFactory));
        }
        #endregion

        #region AddBitmap()
        /// <summary>
        /// Adds a random bitmap.
        /// </summary>
        /// <returns></returns>
        public DrawingShape AddBitmap()
        {
            return AddShape(new BitmapShape(RenderTarget, random, d2DFactory, Bitmap));
        }
        #endregion

        #region AddGeometry()
        /// <summary>
        /// Adds a random geometry.
        /// </summary>
        /// <returns></returns>
        public DrawingShape AddGeometry()
        {
            return AddShape(new GeometryShape(RenderTarget, random, d2DFactory, Bitmap));
        }
        #endregion

        #region AddMesh()
        /// <summary>
        /// Adds a random mesh.
        /// </summary>
        /// <returns></returns>
        public DrawingShape AddMesh()
        {
            return AddShape(new MeshShape(RenderTarget, random, d2DFactory, Bitmap));
        }
        #endregion

        #region AddGDIEllipses()
        /// <summary>
        /// Adds [count] random GDI drawings (as a single shape).
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public DrawingShape AddGDIEllipses(int count)
        {
            return AddShape(new GDIEllipsesShape(RenderTarget, random, d2DFactory, Bitmap, count));
        }
        #endregion

        #region AddLayer()
        /// <summary>
        /// Adds a random layer with [count] random shapes in it.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public DrawingShape AddLayer(int count)
        {
            return AddShape(new LayerShape(RenderTarget, random, d2DFactory, Bitmap, count));
        }
        #endregion
        #endregion

        #region UpdateStats()
        /// <summary>
        /// Updates the statistics
        /// </summary>
        /// <param name="stat">The stat to update</param>
        /// <param name="added">The value by which to change the stat</param>
        void UpdateStats(string stat, int added)
        {
            if (stats.ContainsKey(stat))
                stats[stat] += added;
            else
                stats.Add(stat, added);
            Stats = "";
            foreach (var s in stats.Keys)
                Stats = Stats + s + ": " + stats[s] + Environment.NewLine;
            if (statsChanged != null)
                statsChanged(this, EventArgs.Empty);
        }
        #endregion

        #region GetTreeAt()
        /// <summary>
        /// Gets the tree of shapes at the given point
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns></returns>
        internal TreeNode GetTreeAt(Point2F point)
        {
            lock (renderSyncObject)
            {
                var root = new TreeNode("/");
                AddChildShapesToTree(root, drawingShapes, point);
                return root;
            }
        }
        #endregion

        #region AddChildShapesToTree()
        /// <summary>
        /// Adds the child shapes to tree.
        /// </summary>
        /// <param name="parent">The parent node.</param>
        /// <param name="shapes">The list of shapes to add.</param>
        /// <param name="point">The point at which child shapes should be checked.</param>
        private static void AddChildShapesToTree(TreeNode parent, IList<DrawingShape> shapes, Point2F point)
        {
            for (int i = shapes.Count - 1; i >= 0; i--)
            {
                if (shapes[i].HitTest(point))
                {
                    var child = new TreeNode(shapes[i].ToString()) { Tag = shapes[i] };
                    child.Expand();
                    parent.Nodes.Add(child);
                    if (shapes[i].ChildShapes != null)
                        AddChildShapesToTree(child, shapes[i].ChildShapes, point);
                }
            }
        }
        #endregion

        #region RefreshAll()
        public void RefreshAll()
        {
            if (UsingCompatibleRenderTarget)
                RefreshCompatibleRenderTarget();
            InvalidateClientRectangle();
        }
        #endregion 

        #region InvalidateClientRectangle()
        private void InvalidateClientRectangle()
        {
            Invalidate(ClientRectangle, true);
        }
        #endregion
        #endregion
    }
}
