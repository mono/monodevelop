' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Threading
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.DirectX
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports Microsoft.WindowsAPICodePack.DirectX.DirectWrite
Imports Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics

Namespace D2DShapes
	''' <summary>
	''' A control for managing a list and rendering DrawingShape objects
	''' </summary>
	Public NotInheritable Class D2DShapesControl
		Inherits UserControl
		#Region "enum RenderModes"
		Public Enum RenderModes
			''' <summary>
            ''' Draw through device context in OnPaint (when the window gets invalidated)
			''' </summary>
			DCRenderTarget = 0
			''' <summary>
            ''' Use BitmapRenderTarget to draw on updates, copy the bitmap to DCRenderTarget in OnPaint (when the window gets invalidated)
			''' </summary>
			BitmapRenderTargetOnPaint
			''' <summary>
            ''' Use BitmapRenderTarget to draw on updates, copy the bitmap to HwndRenderTarget in real time
			''' </summary>
			BitmapRenderTargetRealTime
			''' <summary>
            ''' Draw directly on window in real time
			''' </summary>
			HwndRenderTarget
		End Enum
		#End Region

		#Region "Fields"
		'fields for shared factory use
		Friend Shared sharedD2DFactory As D2DFactory
		Friend Shared sharedWicFactory As ImagingFactory
		Friend Shared sharedDwriteFactory As DWriteFactory
		Friend Shared sharedSyncObject As New Object()
		Friend Shared sharedRefCount As Integer

		'object used for synchronization, so shape list changes, (de)initialization, configuration changes and rendering are not done concurrently
		Private ReadOnly renderSyncObject As Object

		'factory objects
		Friend d2DFactory As D2DFactory
		Friend wicFactory As ImagingFactory
		Friend dwriteFactory As DWriteFactory

		'common random object
        Friend random As New Random(Environment.TickCount + seedDelta)
        'used to have different random objects for all instances of this class
		Private Shared seedDelta As Integer

		'render target used in real time rendering modes (can also be used OnPaint, but does not use a device context)
		Private hwndRenderTarget As HwndRenderTarget
		'compatible bitmap that is used in cached modes, in which only changes to the image are drawn to the bitmap and the bitmap is drawn to screen when needed
		Private bitmapRenderTarget As BitmapRenderTarget
		'device context (DC) render target - used with the Graphics object to render to DC
		Private dcRenderTarget As DCRenderTarget

		'shapes to be drawn
		Private ReadOnly drawingShapes As New List(Of DrawingShape)()
		'stack of shapes taken off the drawingShapes list that could be thrown back there
		Private ReadOnly peelings As New Stack(Of DrawingShape)()

		'fields for FPS calculations
		Private lastTickCount As Integer
		Private frameCount As Integer

		'various statistics that invoke StatsChanged when they are updated
		Private ReadOnly stats As New Dictionary(Of String, Integer)()
		'statistics as a string
        Public StatsString As String = ""

        'GDI brushes used to draw background and stub text on a non-initialized control
        Private backgroundBrush As System.Drawing.Brush = SystemBrushes.Control
        Private foregroundBrush As System.Drawing.Brush = SystemBrushes.ControlText

        'object state management variables
        Private _isInitialized As Boolean
        Private isInitializing As Boolean
        Private Shadows disposed As Boolean

        'background render thread
        Private ReadOnly renderThread As Thread
        'reset event for ending the render thread
        Private ReadOnly killThread As ManualResetEvent
        'delegate for rendering in background
        Public Delegate Sub RenderHandler()
#End Region

        #Region "Properties"
#Region "Render"
        Private _render As RenderHandler
        ''' <summary>
        ''' Gets or sets the handler of the render event for background rendering thread in real time modes.
        ''' </summary>
        ''' <value>The render.</value>
        Public Property Render() As RenderHandler
            Get
                SyncLock renderSyncObject
                    Return _render
                End SyncLock
            End Get
            Set(ByVal value As RenderHandler)
                SyncLock renderSyncObject
                    _render = value
                End SyncLock
            End Set
        End Property
#End Region

#Region "IsInitialized"
        ''' <summary>
        ''' Gets a value indicating whether this instance is initialized.
        ''' </summary>
        ''' <value>
        ''' 	<c>true</c> if this instance is initialized; otherwise, <c>false</c>.
        ''' </value>
        Public ReadOnly Property IsInitialized() As Boolean
            Get
                Return _isInitialized AndAlso d2DFactory IsNot Nothing AndAlso d2DFactory.NativeInterface <> IntPtr.Zero
            End Get
        End Property
#End Region

#Region "StatsChanged"
        Private Event _statsChanged As EventHandler
        ''' <summary>
        ''' Occurs after statistics change (eg. when shapes are added/removed)
        ''' </summary>
        Public Custom Event StatsChanged As EventHandler
            AddHandler(ByVal value As EventHandler)
                AddHandler _statsChanged, value
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
                RemoveHandler _statsChanged, value
            End RemoveHandler
            RaiseEvent(ByVal sender As System.Object, ByVal e As System.EventArgs)
            End RaiseEvent
        End Event
#End Region

#Region "RenderMode"
        Private _renderMode As RenderModes
        ''' <summary>
        ''' Gets or sets the render mode.
        ''' See RenderModes enum description for descriptions of particular modes implemented
        ''' </summary>
        ''' <value>The render mode.</value>
        Public Property RenderMode() As RenderModes
            Get
                Return _renderMode
            End Get
            Set(ByVal value As RenderModes)
                SetRenderMode(value)
            End Set
        End Property
#End Region

#Region "UsingCompatibleRenderTarget"
        ''' <summary>
        ''' Gets a value indicating whether a compatible render target bitmap is used for cached rendering.
        ''' </summary>
        ''' <value>
        ''' 	<c>true</c> if [using compatible render target]; otherwise, <c>false</c>.
        ''' </value>
        Private ReadOnly Property UsingCompatibleRenderTarget() As Boolean
            Get
                Return _renderMode = RenderModes.BitmapRenderTargetOnPaint OrElse _renderMode = RenderModes.BitmapRenderTargetRealTime
            End Get
        End Property
#End Region

#Region "RenderTarget"
        ''' <summary>
        ''' render target to use when drawing shapes
        ''' </summary>
        Friend ReadOnly Property RenderTarget() As RenderTarget
            Get
                SyncLock renderSyncObject
                    Select Case _renderMode
                        Case RenderModes.BitmapRenderTargetOnPaint, RenderModes.BitmapRenderTargetRealTime
                            Return bitmapRenderTarget
                        Case RenderModes.DCRenderTarget
                            Return dcRenderTarget
                        Case Else
                            Return hwndRenderTarget
                    End Select
                End SyncLock
            End Get
        End Property
#End Region

#Region "BackColorF"
        Private _backColorF As ColorF
        ''' <summary>
        ''' Gets the (float) back color used for clearing the background - it is dependent on the BackColor property.
        ''' </summary>
        ''' <value>The back color F.</value>
        Private ReadOnly Property BackColorF() As ColorF
            Get
                Return _backColorF
            End Get
        End Property
#End Region

#Region "Bitmap"
        Private _bitmap As D2DBitmap
        ''' <summary>
        ''' Gets the bitmap shared among shape objects. Loads the bitmap if not initialized and rendertarget and wicFactory are available.
        ''' Set bitmap to null to reload it after changing the render target
        ''' </summary>
        ''' <value>The bitmap.</value>
        Friend ReadOnly Property Bitmap() As D2DBitmap
            Get
                If _bitmap Is Nothing AndAlso RenderTarget IsNot Nothing AndAlso wicFactory IsNot Nothing Then
                    Using stream As Stream = GetType(D2DShapesControl).Assembly.GetManifestResourceStream("Peacock.jpg")
                        _bitmap = BitmapUtilities.LoadBitmapFromStream(RenderTarget, wicFactory, stream)
                    End Using
                End If
                Return _bitmap
            End Get
        End Property
#End Region

#Region "Fps"
        ''' <summary>
        ''' Gets or sets the number of frames drawn per second.
        ''' Is updated when the image is redrawn after at least a second since the last calculation.
        ''' See CalculateFPS().
        ''' </summary>
        ''' <value>The FPS.</value>
        Private privateFps As Single
        Public Property Fps() As Single
            Get
                Return privateFps
            End Get
            Private Set(ByVal value As Single)
                privateFps = value
            End Set
        End Property
#End Region

#Region "FpsChanged"
        Private Event _fpsChanged As EventHandler
        ''' <summary>
        ''' Occurs when Fps property value changes.
        ''' </summary>
        Public Custom Event FpsChanged As EventHandler
            AddHandler(ByVal value As EventHandler)
                AddHandler _fpsChanged, value
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
                RemoveHandler _fpsChanged, value
            End RemoveHandler
            RaiseEvent(ByVal sender As System.Object, ByVal e As System.EventArgs)
            End RaiseEvent
        End Event
#End Region
#End Region

        #Region "D2DShapesControl() - CTOR"
        Public Sub New(ByVal components As IContainer)
            seedDelta = seedDelta + 1
            components.Add(Me)
            SetStyle(ControlStyles.UserPaint, True)
            SetStyle(ControlStyles.AllPaintingInWmPaint, True)
            UpdateStyles()

            _backColorF = New ColorF(BackColor.R / 256.0F, BackColor.G / 256.0F, BackColor.B / 256.0F, 1.0F)

            'Initialize the background render thread and synchronization objects
            renderSyncObject = New Object()
            killThread = New ManualResetEvent(False)
            Dim ts As ThreadStart = AddressOf RenderThreadProcedure
            renderThread = New Thread(ts)
            renderThread.Start()
        End Sub
#End Region

        #Region "Methods"
#Region "WndProc()"
        Protected Overrides Sub WndProc(ByRef m As Message)
            'kill and wait for render thread to complete when window gets destroyed
            If m.Msg = &H2 Then
                killThread.Set()
                renderThread.Join()
            End If
            MyBase.WndProc(m)
        End Sub
#End Region

#Region "OnParentChanged()"
        ''' <summary>
        ''' Stop rendering if removed from a parent control
        ''' </summary>
        ''' <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        Protected Overrides Sub OnParentChanged(ByVal e As EventArgs)
            SyncLock renderSyncObject
                If Parent Is Nothing Then
                    _render = Nothing
                End If
            End SyncLock
            MyBase.OnParentChanged(e)
        End Sub
#End Region

#Region "Dispose()"
        ''' <summary>
        ''' Dispose of resources (IDisposable implementation)
        ''' </summary>
        ''' <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing AndAlso (Not disposed) Then
                SyncLock renderSyncObject
                    _render = Nothing

                    SyncLock sharedSyncObject
                        If sharedD2DFactory IsNot Nothing AndAlso d2DFactory Is sharedD2DFactory Then
                            sharedRefCount -= 1
                        End If

                        If d2DFactory IsNot Nothing AndAlso d2DFactory IsNot sharedD2DFactory Then
                            d2DFactory.Dispose()
                        End If
                        d2DFactory = Nothing

                        If dwriteFactory IsNot Nothing AndAlso dwriteFactory IsNot sharedDwriteFactory Then
                            dwriteFactory.Dispose()
                        End If
                        dwriteFactory = Nothing

                        If wicFactory IsNot Nothing AndAlso wicFactory IsNot sharedWicFactory Then
                            wicFactory.Dispose()
                        End If
                        wicFactory = Nothing

                        If sharedRefCount = 0 Then
                            If sharedD2DFactory IsNot Nothing Then
                                sharedD2DFactory.Dispose()
                            End If
                            sharedD2DFactory = Nothing

                            If sharedDwriteFactory IsNot Nothing Then
                                sharedDwriteFactory.Dispose()
                            End If
                            sharedDwriteFactory = Nothing

                            If sharedWicFactory IsNot Nothing Then
                                sharedWicFactory.Dispose()
                            End If
                            sharedWicFactory = Nothing
                        End If
                    End SyncLock

                    For Each shape As DrawingShape In drawingShapes
                        shape.Dispose()
                    Next shape

                    If _bitmap IsNot Nothing Then
                        _bitmap.Dispose()
                    End If
                    _bitmap = Nothing

                    If dcRenderTarget IsNot Nothing Then
                        dcRenderTarget.Dispose()
                    End If
                    dcRenderTarget = Nothing
                    If bitmapRenderTarget IsNot Nothing Then
                        bitmapRenderTarget.Dispose()
                    End If
                    bitmapRenderTarget = Nothing
                    If hwndRenderTarget IsNot Nothing Then
                        hwndRenderTarget.Dispose()
                    End If
                    hwndRenderTarget = Nothing


                    disposed = True
                End SyncLock
            End If
            MyBase.Dispose(disposing)
        End Sub
#End Region

#Region "Initialize()"
        ''' <summary>
        ''' Initializes rendering.
        ''' </summary>
        Public Sub Initialize()
            SyncLock renderSyncObject
                isInitializing = True
                CreateFactories()
                CreateDeviceResources()
                isInitializing = False
                _isInitialized = True
            End SyncLock
        End Sub
#End Region

#Region "CreateFactories()"
        Private Sub CreateFactories()
            'reuse factories except for random cases
            If random.NextDouble() < 0.5 Then
                SyncLock sharedSyncObject
                    If sharedD2DFactory Is Nothing Then
                        ' Create the D2D Factory
                        sharedD2DFactory = d2DFactory.CreateFactory(D2DFactoryType.MultiThreaded)

                        ' Create the DWrite Factory
                        sharedDwriteFactory = dwriteFactory.CreateFactory()

                        ' Create the WIC Factory
                        sharedWicFactory = ImagingFactory.Create()

                        Debug.Assert(sharedD2DFactory.NativeInterface <> IntPtr.Zero)
                        Debug.Assert(sharedDwriteFactory.NativeInterface <> IntPtr.Zero)
                        Debug.Assert(sharedWicFactory.NativeInterface <> IntPtr.Zero)
                    End If
                    sharedRefCount += 1
                End SyncLock
                d2DFactory = sharedD2DFactory
                dwriteFactory = sharedDwriteFactory
                wicFactory = sharedWicFactory
                Debug.Assert(d2DFactory.NativeInterface <> IntPtr.Zero)
                Debug.Assert(dwriteFactory.NativeInterface <> IntPtr.Zero)
                Debug.Assert(wicFactory.NativeInterface <> IntPtr.Zero)
            Else
                ' Create the D2D Factory
                d2DFactory = d2DFactory.CreateFactory(D2DFactoryType.MultiThreaded)

                ' Create the DWrite Factory
                dwriteFactory = dwriteFactory.CreateFactory()

                ' Create the WIC Factory
                wicFactory = ImagingFactory.Create()
                Debug.Assert(d2DFactory.NativeInterface <> IntPtr.Zero)
                Debug.Assert(dwriteFactory.NativeInterface <> IntPtr.Zero)
                Debug.Assert(wicFactory.NativeInterface <> IntPtr.Zero)
            End If
        End Sub

#End Region

#Region "CreateDeviceResources()"
        ''' <summary>
        ''' This method creates the render target and associated D2D and DWrite resources
        ''' </summary>
        Private Sub CreateDeviceResources()
            ' Only calls if resources have not been initialize before
            'if (RenderTarget != null)
            '    return;
            SetRenderMode(_renderMode)
        End Sub
#End Region

#Region "SetRenderMode()"
        Private Sub SetRenderMode(ByVal rm As RenderModes)
            SyncLock renderSyncObject
                _renderMode = rm
                If (Not IsInitialized) AndAlso (Not isInitializing) Then
                    Return
                End If

                'clean up objects that will be invalid after RenderTarget change
                If dcRenderTarget IsNot Nothing Then
                    dcRenderTarget.Dispose()
                    dcRenderTarget = Nothing
                End If
                If hwndRenderTarget IsNot Nothing Then
                    hwndRenderTarget.Dispose()
                    hwndRenderTarget = Nothing
                End If
                If bitmapRenderTarget IsNot Nothing Then
                    bitmapRenderTarget.Dispose()
                    bitmapRenderTarget = Nothing
                End If
                peelings.Clear()
                _bitmap = Nothing 'the bitmap created in dc render target can't be used in hwnd render target

                ' Create the screen render target
                Dim size = New SizeU(CUInt(ClientSize.Width), CUInt(ClientSize.Height))
                Dim props = New RenderTargetProperties With {.PixelFormat = New PixelFormat(Format.B8G8R8A8UNorm, AlphaMode.Ignore), .Usage = RenderTargetUsages.GdiCompatible}

                If _renderMode = RenderModes.DCRenderTarget OrElse _renderMode = RenderModes.BitmapRenderTargetOnPaint Then
                    dcRenderTarget = d2DFactory.CreateDCRenderTarget(props)
                    If _renderMode = RenderModes.BitmapRenderTargetOnPaint Then
                        bitmapRenderTarget = dcRenderTarget.CreateCompatibleRenderTarget(CompatibleRenderTargetOptions.GdiCompatible, New Microsoft.WindowsAPICodePack.DirectX.Direct2D1.SizeF(ClientSize.Width, ClientSize.Height))
                    End If
                    _render = Nothing
                Else
                    hwndRenderTarget = d2DFactory.CreateHwndRenderTarget(props, New HwndRenderTargetProperties(Handle, size, Direct2D1.PresentOptions.RetainContents))
                    If _renderMode = RenderModes.BitmapRenderTargetRealTime Then
                        bitmapRenderTarget = hwndRenderTarget.CreateCompatibleRenderTarget(CompatibleRenderTargetOptions.GdiCompatible, New Microsoft.WindowsAPICodePack.DirectX.Direct2D1.SizeF(ClientSize.Width, ClientSize.Height))
                    End If
                    _render = AddressOf RenderSceneInBackground
                End If

                'move all shapes to new rendertarget and refresh
                For Each shape In drawingShapes
                    shape.Bitmap = Bitmap
                    shape.RenderTarget = RenderTarget
                Next shape
                RefreshAll()
            End SyncLock
        End Sub
#End Region

#Region "OnResize()"
        Protected Overrides Sub OnResize(ByVal e As EventArgs)
            SyncLock renderSyncObject
                If RenderTarget IsNot Nothing Then
                    ' Resize the render targrt to the actual host size
                    Dim size = New SizeU(CUInt(ClientSize.Width), CUInt(ClientSize.Height))
                    If hwndRenderTarget IsNot Nothing Then
                        hwndRenderTarget.Resize(size) 'need to resize hwndRenderTarget to make its size same as the window's size
                    End If
                    If _renderMode = RenderModes.BitmapRenderTargetOnPaint Then
                        bitmapRenderTarget.Dispose()
                        bitmapRenderTarget = dcRenderTarget.CreateCompatibleRenderTarget(CompatibleRenderTargetOptions.GdiCompatible, New Microsoft.WindowsAPICodePack.DirectX.Direct2D1.SizeF(ClientSize.Width, ClientSize.Height))
                        _bitmap = Nothing 'the bitmap created in dc render target can't be used in hwnd render target
                        For Each shape In drawingShapes
                            shape.Bitmap = Bitmap
                            shape.RenderTarget = RenderTarget
                        Next shape
                        RefreshAll()
                    ElseIf _renderMode = RenderModes.BitmapRenderTargetRealTime Then
                        Debug.Assert(hwndRenderTarget IsNot Nothing) 'this should never be null considering the above
                        bitmapRenderTarget.Dispose()
                        bitmapRenderTarget = hwndRenderTarget.CreateCompatibleRenderTarget(CompatibleRenderTargetOptions.GdiCompatible, New Microsoft.WindowsAPICodePack.DirectX.Direct2D1.SizeF(ClientSize.Width, ClientSize.Height))
                        _bitmap = Nothing 'the bitmap created in dc render target can't be used in hwnd render target
                        For Each shape In drawingShapes
                            shape.Bitmap = Bitmap
                            shape.RenderTarget = RenderTarget
                        Next shape
                        RefreshAll()
                    End If
                End If
            End SyncLock
            MyBase.OnResize(e)
        End Sub
#End Region

#Region "OnBackColorChanged()"
        Protected Overrides Sub OnBackColorChanged(ByVal e As EventArgs)
            MyBase.OnBackColorChanged(e)
            _backColorF = New ColorF(BackColor.R / 256.0F, BackColor.G / 256.0F, BackColor.B / 256.0F, 1.0F)
            backgroundBrush = New SolidBrush(BackColor)
        End Sub
#End Region

#Region "OnForeColorChanged()"
        Protected Overrides Sub OnForeColorChanged(ByVal e As EventArgs)
            MyBase.OnForeColorChanged(e)
            foregroundBrush = New SolidBrush(ForeColor)
        End Sub
#End Region

#Region "OnPaintBackground()"
        Protected Overrides Sub OnPaintBackground(ByVal e As PaintEventArgs)
            If (Not IsInitialized) OrElse DesignMode Then
                e.Graphics.FillRectangle(backgroundBrush, ClientRectangle)
            End If
        End Sub
#End Region

#Region "OnPaint()"
        ''' <summary>
        ''' for use with DcRenderTarget (though it would also work with HwndRenderTarget if GDI interop was not used)
        ''' </summary>
        ''' <param name="e"></param>
        Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
            If (Not IsInitialized) OrElse DesignMode Then
                e.Graphics.DrawString("D2DShapesControl", Font, foregroundBrush, CSng(Width) / 2, CSng(Height) / 2)
            Else
                SyncLock renderSyncObject
                    If _renderMode = RenderModes.DCRenderTarget OrElse _renderMode = RenderModes.BitmapRenderTargetOnPaint Then
                        If dcRenderTarget Is Nothing Then
                            Return
                        End If
                        CalculateFPS()
                        If _renderMode = RenderModes.DCRenderTarget Then
                            'render scene directly to DC
                            dcRenderTarget.BindDC(e.Graphics.GetHdc(), New Rect(Left, Top, Right, Bottom))
                            RenderScene(dcRenderTarget)
                            e.Graphics.ReleaseHdc()
                            e.Graphics.DrawString(String.Format("OnPaint({0}) direct DC draw", DateTime.Now), Font, Brushes.White, 0, 2)
                            e.Graphics.DrawString(String.Format("OnPaint({0}) direct DC draw", DateTime.Now), Font, Brushes.Black, 1, 2)
                        ElseIf _renderMode = RenderModes.BitmapRenderTargetOnPaint Then
                            'draw bitmap cache of the shapes to DC
                            dcRenderTarget.BindDC(e.Graphics.GetHdc(), New Rect(Left, Top, Right, Bottom))
                            dcRenderTarget.BeginDraw()
                            dcRenderTarget.DrawBitmap(bitmapRenderTarget.Bitmap, 1.0F, BitmapInterpolationMode.NearestNeighbor, New RectF(0, 0, Width, Height))
                            dcRenderTarget.EndDraw()
                            e.Graphics.ReleaseHdc()
                            e.Graphics.DrawString(String.Format("OnPaint({0}) DC DrawBitmap", DateTime.Now), Font, Brushes.White, 0, 2)
                            e.Graphics.DrawString(String.Format("OnPaint({0}) DC DrawBitmap", DateTime.Now), Font, Brushes.Black, 1, 2)
                        End If
                    End If
                End SyncLock
            End If
        End Sub
#End Region

#Region "RenderThreadProcedure()"
        ''' <summary>
        ''' The render thread procedure - calls.
        ''' </summary>
        Private Sub RenderThreadProcedure()
            Do
                SyncLock renderSyncObject
                    If _render IsNot Nothing Then
                        _render()
                    End If
                End SyncLock
                If _render Is Nothing Then
                    Thread.Sleep(1)
                End If
            Loop While killThread.WaitOne(0) = False
        End Sub
#End Region

#Region "RenderSceneInBackground()"
        'used with HwndRenderTarget
        Private Sub RenderSceneInBackground()
            If Parent Is Nothing OrElse _render Is Nothing Then
                Return
            End If
            If _renderMode = RenderModes.HwndRenderTarget OrElse _renderMode = RenderModes.BitmapRenderTargetRealTime Then
                If hwndRenderTarget Is Nothing OrElse hwndRenderTarget.IsOccluded Then
                    Return
                End If
                CalculateFPS()
                If _renderMode = RenderModes.HwndRenderTarget Then
                    'render scene directly on the control
                    RenderScene(hwndRenderTarget)
                ElseIf _renderMode = RenderModes.BitmapRenderTargetRealTime Then
                    'draw bitmap cache of the shapes to control
                    hwndRenderTarget.BeginDraw()
                    hwndRenderTarget.DrawBitmap(bitmapRenderTarget.Bitmap, 1.0F, BitmapInterpolationMode.NearestNeighbor, New RectF(0, 0, Width, Height))
                    hwndRenderTarget.EndDraw()
                End If
            End If
        End Sub
#End Region

#Region "RenderScene()"
        ''' <summary>
        ''' Renders the scene to the given render target.
        ''' Clears the scene, then draws all shapes
        ''' </summary>
        ''' <param name="renderTarget">The render target.</param>
        Private Sub RenderScene(ByVal renderTarget As RenderTarget)
            Dim c As Cursor = Nothing
            If _renderMode <> RenderModes.HwndRenderTarget Then
                c = Cursor
                Cursor = Cursors.WaitCursor
            End If
            renderTarget.BeginDraw()
            renderTarget.Clear(BackColorF)

            For i As Integer = 0 To drawingShapes.Count - 1
                Dim shape As DrawingShape = drawingShapes(i)
                'tag with shape index for debugging
                renderTarget.Tags = New Tags(CULng(i), 0)
                shape.Draw(renderTarget)
            Next i
            Dim tags As Tags
            Dim errorCode As ErrorCode
            If Not renderTarget.TryEndDraw(tags, errorCode) Then
                Debug.WriteLine(String.Format("Failed EndDraw. Error: {0}, tag1: {1}, tag2: {2}, shape[{1}]: {3}", errorCode, tags.Tag1, tags.Tag2, If(CInt(Fix(tags.Tag1)) < drawingShapes.Count, drawingShapes(CInt(Fix(tags.Tag1))).ToString(), "<none>")))
            End If
            If _renderMode <> RenderModes.HwndRenderTarget Then
                Cursor = c
            End If
        End Sub
#End Region

#Region "RefreshCompatibleRenderTarget()"
        ''' <summary>
        ''' Refreshes the compatible render target - the bitmap used for caching the scene 
        ''' for quick rendering when the control is redrawn
        ''' </summary>
        Private Sub RefreshCompatibleRenderTarget()
            RenderScene(bitmapRenderTarget)
        End Sub
#End Region

#Region "AddToCompatibleRenderTarget()"
        ''' <summary>
        ''' Adds a shape to compatible render target - avoids the need to redraw all shapes.
        ''' </summary>
        ''' <param name="shape">The shape.</param>
        Private Sub AddToCompatibleRenderTarget(ByVal shape As DrawingShape)
            SyncLock renderSyncObject
                bitmapRenderTarget.BeginDraw()
                shape.Draw(bitmapRenderTarget)
                bitmapRenderTarget.EndDraw()
            End SyncLock
        End Sub
#End Region

#Region "CalculateFPS()"
        ''' <summary>
        ''' Calculates Frames Per Second if at least a second passed since previous update.
        ''' Should be called whenever a frame is drawn on the control
        ''' </summary>
        Private Sub CalculateFPS()
            Dim currentTickCount As Integer = Environment.TickCount
            Dim ticks As Integer = currentTickCount - lastTickCount
            If ticks >= 1000 Then
                Fps = CSng(frameCount) * 1000 / ticks
                frameCount = 0
                lastTickCount = currentTickCount
                BeginInvoke(New MethodInvoker(Function() AnonymousMethod1()))
            End If
            frameCount += 1
        End Sub

        Private Function AnonymousMethod1() As Object
            RaiseEvent _fpsChanged(Me, EventArgs.Empty)
            Return Nothing
        End Function
#End Region

#Region "ClearShapes()"
        ''' <summary>
        ''' Clears all the shapes and invalidates the control and bitmap cache if one exists.
        ''' </summary>
        Public Sub ClearShapes()
            SyncLock renderSyncObject
                drawingShapes.Clear()
                stats.Clear()
                StatsString = ""
                RaiseEvent _statsChanged(Me, EventArgs.Empty)
                RefreshAll()
            End SyncLock
        End Sub
#End Region

#Region "PeelShape()"
        ''' <summary>
        ''' Peels the shape from the end(top) of the list,
        ''' updates the render targets and puts the peeled shape to the stack of peelings.
        ''' </summary>
        ''' <returns></returns>
        Friend Function PeelShape() As DrawingShape
            SyncLock renderSyncObject
                If drawingShapes.Count = 0 Then
                    Return Nothing
                End If
                Dim statName As String = drawingShapes(drawingShapes.Count - 1).GetType().Name.Replace("Shape", " count")
                UpdateStats(statName, -1)
                peelings.Push(drawingShapes(drawingShapes.Count - 1))
                drawingShapes.RemoveAt(drawingShapes.Count - 1)
                RefreshAll()
                Return peelings.Peek()
            End SyncLock
        End Function

        ''' <summary>
        ''' Peels the specific shape.
        ''' </summary>
        ''' <param name="shape">The shape.</param>
        Friend Sub PeelShape(ByVal shape As DrawingShape)
            Debug.Assert(shape IsNot Nothing)
            SyncLock renderSyncObject
                Dim statName As String = shape.GetType().Name.Replace("Shape", " count")
                UpdateStats(statName, -1)
                peelings.Push(shape)
                PeelShape(shape, drawingShapes)
                RefreshAll()
            End SyncLock
        End Sub

        ''' <summary>
        ''' Peels the shape recursively.
        ''' </summary>
        ''' <param name="shape">The shape.</param>
        ''' <param name="shapes">The shapes.</param>
        ''' <returns></returns>
        Private Shared Function PeelShape(ByVal shape As DrawingShape, ByVal shapes As ICollection(Of DrawingShape)) As Boolean
            For Each s In shapes
                If s Is shape Then
                    shapes.Remove(shape)
                    Return True
                End If
                If s.ChildShapes IsNot Nothing AndAlso PeelShape(shape, s.ChildShapes) Then
                    Return True
                End If
            Next s
            Return False
        End Function
#End Region

#Region "PeelAt()"
        ''' <summary>
        ''' Peels the top shape at a given point,
        ''' updates the render targets and puts the peeled shape to the stack of peelings.
        ''' </summary>
        ''' <param name="point"></param>
        ''' <returns></returns>
        Friend Function PeelAt(ByVal point As Point2F) As DrawingShape
            SyncLock renderSyncObject
                Return PeelAtRecursive(point, drawingShapes)
            End SyncLock
        End Function
#End Region

#Region "PeelAtRecursive()"
        ''' <summary>
        ''' Peels the top shape at a given point,
        ''' updates the render targets and puts the peeled shape to the stack of peelings.
        ''' Used to enable removing a child shape - eg. a top shape in a layer
        ''' </summary>
        ''' <param name="point"></param>
        ''' <param name="shapes"></param>
        ''' <returns></returns>
        Private Function PeelAtRecursive(ByVal point As Point2F, ByVal shapes As IList(Of DrawingShape)) As DrawingShape
            For i As Integer = shapes.Count - 1 To 0 Step -1
                If shapes(i).HitTest(point) Then
                    If shapes(i).ChildShapes Is Nothing Then
                        Dim statName As String = shapes(i).GetType().Name.Replace("Shape", " count")
                        UpdateStats(statName, -1)
                        peelings.Push(shapes(i))
                        shapes.RemoveAt(i)
                        RefreshAll()
                        Return peelings.Peek()
                    End If
                    Dim shapePeeled As DrawingShape = PeelAtRecursive(point, shapes(i).ChildShapes)
                    If shapePeeled Is Nothing Then
                        Dim statName As String = shapes(i).GetType().Name.Replace("Shape", " count")
                        UpdateStats(statName, -1)
                        peelings.Push(shapes(i))
                        shapes.RemoveAt(i)
                        RefreshAll()
                        Return peelings.Peek()
                    End If
                    Return shapePeeled
                End If
            Next i
            Return Nothing
        End Function
#End Region

#Region "UnpeelShape()"
        ''' <summary>
        ''' Puts a shape from the stack of peelings back to the list of shapes and invalidates the render targets.
        ''' </summary>
        ''' <returns></returns>
        Friend Function UnpeelShape() As DrawingShape
            SyncLock renderSyncObject
                If peelings.Count = 0 Then
                    Return Nothing
                End If
                Dim shape As DrawingShape = peelings.Peek()
                Dim statName As String = shape.GetType().Name.Replace("Shape", " count")
                UpdateStats(statName, 1)
                drawingShapes.Add(peelings.Pop())
                If UsingCompatibleRenderTarget Then
                    AddToCompatibleRenderTarget(shape)
                End If
                InvalidateClientRectangle()
                Return shape
            End SyncLock
        End Function
#End Region

#Region "AddShape~()"
#Region "AddShape()"
        ''' <summary>
        ''' Adds the shape to the list and updates the render targets.
        ''' </summary>
        ''' <param name="shape">The shape.</param>
        ''' <returns></returns>
        Private Function AddShape(ByVal shape As DrawingShape) As DrawingShape
            SyncLock renderSyncObject
                drawingShapes.Add(shape)
                Dim statName As String = shape.GetType().Name.Replace("Shape", " count")
                UpdateStats(statName, 1)
                If UsingCompatibleRenderTarget Then
                    AddToCompatibleRenderTarget(shape)
                End If
                InvalidateClientRectangle()
                Return shape
            End SyncLock
        End Function
#End Region

#Region "AddRandomShape()"
        ''' <summary>
        ''' Adds a random shape.
        ''' </summary>
        ''' <returns></returns>
        Public Function AddRandomShape() As DrawingShape
            Dim which As Double = random.NextDouble()
            If which < 0.1 Then
                Return AddLine()
            End If
            If which < 0.2 Then
                Return AddRectangle()
            End If
            If which < 0.3 Then
                Return AddRoundRect()
            End If
            If which < 0.4 Then
                Return AddEllipse()
            End If
            If which < 0.5 Then
                Return AddText()
            End If
            If which < 0.6 Then
                Return AddBitmap()
            End If
            If which < 0.7 Then
                Return AddGeometry()
            End If
            If which < 0.8 Then
                Return AddMesh()
            End If
            If which < 0.9 Then
                Return AddGDIEllipses(5)
            End If
            Return AddLayer(5)
        End Function
#End Region

#Region "AddRandomShapes(count)"
        ''' <summary>
        ''' Adds [count] random shapes.
        ''' </summary>
        ''' <param name="count">The count.</param>
        Public Sub AddRandomShapes(ByVal count As Integer)
            For i As Integer = 0 To count - 1
                AddRandomShape()
            Next i
        End Sub
#End Region

#Region "AddLine()"
        ''' <summary>
        ''' Adds a random line.
        ''' </summary>
        ''' <returns></returns>
        Public Function AddLine() As DrawingShape
            Return AddShape(New LineShape(RenderTarget, random, d2DFactory, Bitmap))
        End Function
#End Region

#Region "AddRectangle()"
        ''' <summary>
        ''' Adds a random rectangle.
        ''' </summary>
        ''' <returns></returns>
        Public Function AddRectangle() As DrawingShape
            Return AddShape(New RectangleShape(RenderTarget, random, d2DFactory, Bitmap))
        End Function
#End Region

#Region "AddRoundRect()"
        ''' <summary>
        ''' Adds a random round rect.
        ''' </summary>
        ''' <returns></returns>
        Public Function AddRoundRect() As DrawingShape
            Return AddShape(New RoundRectangleShape(RenderTarget, random, d2DFactory, Bitmap))
        End Function
#End Region

#Region "AddEllipse()"
        ''' <summary>
        ''' Adds a random ellipse.
        ''' </summary>
        ''' <returns></returns>
        Public Function AddEllipse() As DrawingShape
            Return AddShape(New EllipseShape(RenderTarget, random, d2DFactory, Bitmap))
        End Function
#End Region

#Region "AddText()"
        ''' <summary>
        ''' Adds a random text.
        ''' </summary>
        ''' <returns></returns>
        Public Function AddText() As DrawingShape
            If random.NextDouble() < 0.5 Then
                Return AddShape(New TextShape(RenderTarget, random, d2DFactory, Bitmap, dwriteFactory))
            End If
            Return AddShape(New TextLayoutShape(RenderTarget, random, d2DFactory, Bitmap, dwriteFactory))
        End Function
#End Region

#Region "AddBitmap()"
        ''' <summary>
        ''' Adds a random bitmap.
        ''' </summary>
        ''' <returns></returns>
        Public Function AddBitmap() As DrawingShape
            Return AddShape(New BitmapShape(RenderTarget, random, d2DFactory, Bitmap))
        End Function
#End Region

#Region "AddGeometry()"
        ''' <summary>
        ''' Adds a random geometry.
        ''' </summary>
        ''' <returns></returns>
        Public Function AddGeometry() As DrawingShape
            Return AddShape(New GeometryShape(RenderTarget, random, d2DFactory, Bitmap))
        End Function
#End Region

#Region "AddMesh()"
        ''' <summary>
        ''' Adds a random mesh.
        ''' </summary>
        ''' <returns></returns>
        Public Function AddMesh() As DrawingShape
            Return AddShape(New MeshShape(RenderTarget, random, d2DFactory, Bitmap))
        End Function
#End Region

#Region "AddGDIEllipses()"
        ''' <summary>
        ''' Adds [count] random GDI drawings (as a single shape).
        ''' </summary>
        ''' <param name="count">The count.</param>
        ''' <returns></returns>
        Public Function AddGDIEllipses(ByVal count As Integer) As DrawingShape
            Return AddShape(New GDIEllipsesShape(RenderTarget, random, d2DFactory, Bitmap, count))
        End Function
#End Region

#Region "AddLayer()"
        ''' <summary>
        ''' Adds a random layer with [count] random shapes in it.
        ''' </summary>
        ''' <param name="count">The count.</param>
        ''' <returns></returns>
        Public Function AddLayer(ByVal count As Integer) As DrawingShape
            Return AddShape(New LayerShape(RenderTarget, random, d2DFactory, Bitmap, count))
        End Function
#End Region
#End Region

#Region "UpdateStats()"
        ''' <summary>
        ''' Updates the statistics
        ''' </summary>
        ''' <param name="stat">The stat to update</param>
        ''' <param name="added">The value by which to change the stat</param>
        Private Sub UpdateStats(ByVal stat As String, ByVal added As Integer)
            If stats.ContainsKey(stat) Then
                stats(stat) += added
            Else
                stats.Add(stat, added)
            End If
            StatsString = ""
            For Each s In stats.Keys
                StatsString = StatsString & s & ": " & stats(s) & Environment.NewLine
            Next s
            RaiseEvent _statsChanged(Me, EventArgs.Empty)
        End Sub
#End Region

#Region "GetTreeAt()"
        ''' <summary>
        ''' Gets the tree of shapes at the given point
        ''' </summary>
        ''' <param name="point">The point.</param>
        ''' <returns></returns>
        Friend Function GetTreeAt(ByVal point As Point2F) As TreeNode
            SyncLock renderSyncObject
                Dim root = New TreeNode("/")
                AddChildShapesToTree(root, drawingShapes, point)
                Return root
            End SyncLock
        End Function
#End Region

#Region "AddChildShapesToTree()"
        ''' <summary>
        ''' Adds the child shapes to tree.
        ''' </summary>
        ''' <param name="parent">The parent node.</param>
        ''' <param name="shapes">The list of shapes to add.</param>
        ''' <param name="point">The point at which child shapes should be checked.</param>
        Private Shared Sub AddChildShapesToTree(ByVal parent As TreeNode, ByVal shapes As IList(Of DrawingShape), ByVal point As Point2F)
            For i As Integer = shapes.Count - 1 To 0 Step -1
                If shapes(i).HitTest(point) Then
                    Dim child = New TreeNode(shapes(i).ToString()) With {.Tag = shapes(i)}
                    child.Expand()
                    parent.Nodes.Add(child)
                    If shapes(i).ChildShapes IsNot Nothing Then
                        AddChildShapesToTree(child, shapes(i).ChildShapes, point)
                    End If
                End If
            Next i
        End Sub
#End Region

#Region "RefreshAll()"
        Public Sub RefreshAll()
            If UsingCompatibleRenderTarget Then
                RefreshCompatibleRenderTarget()
            End If
            InvalidateClientRectangle()
        End Sub
#End Region

#Region "InvalidateClientRectangle()"
        Private Sub InvalidateClientRectangle()
            Invalidate(ClientRectangle, True)
        End Sub
#End Region
#End Region
	End Class
End Namespace
