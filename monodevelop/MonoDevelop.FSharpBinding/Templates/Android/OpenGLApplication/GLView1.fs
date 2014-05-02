namespace ${Namespace}

open System

open OpenTK
open OpenTK.Graphics
open OpenTK.Graphics.ES11
open OpenTK.Platform
open OpenTK.Platform.Android

open Android.Views
open Android.Content
open Android.Util

[<AllowNullLiteral>]
type GLView1 (context:Context) =
  inherit AndroidGameView (context)

  let squareVertices : single[] = [|
      -0.5f; -0.5f;
      0.5f; -0.5f;
      -0.5f; 0.5f;
      0.5f; 0.5f
     |]

  let squareColors : byte[] = [|
      255uy; 255uy;   0uy; 255uy;
      0uy;   255uy; 255uy; 255uy;
      0uy;     0uy;    0uy;  0uy;
      255uy;   0uy;  255uy; 255uy;
     |]

  override x.OnLoad (e:EventArgs) =

    // This gets called when the drawing surface is ready
    base.OnLoad (e);
    // Run the render loop
    x.Run ();

    // This method is called everytime the context needs
    // to be recreated. Use it to set any egl-specific settings
    // prior to context creation
    //
    // In this particular case, we demonstrate how to set
    // the graphics mode and fallback in case the device doesn't
    // support the defaults
    override x.CreateFrameBuffer () =
      // the default GraphicsMode that is set consists of (16, 16, 0, 0, 2, false)
      let attempt1 =
        try
          Log.Verbose ("GLCube", "Loading with default settings") |> ignore

          // if you don't call this, the context won't be created
          base.CreateFrameBuffer ()
          true
        with ex ->
          Log.Verbose ("GLCube", "{0}", ex) |> ignore
          false

      if not attempt1 then
          // this is a graphics setting that sets everything to the lowest mode possible so
          // the device returns a reliable graphics setting.
        let attempt2 =
            try
              Log.Verbose ("GLCube", "Loading with custom Android settings (low mode)") |> ignore
              x.GraphicsMode <- new AndroidGraphicsMode (ColorFormat 0, 0, 0, 0, 0, false)

              // if you don't call this, the context won't be created
              base.CreateFrameBuffer ()
              true
            with ex ->
              Log.Verbose ("GLCube", "{0}", ex) |> ignore
              false

        if not attempt2 then
            failwith "Can't load egl, aborting"

    // This gets called on each frame render
    override x.OnRenderFrame (e:FrameEventArgs) =

      // you only need to call this if you have delegates
      // registered that you want to have called
      base.OnRenderFrame (e)

      GL.MatrixMode (All.Projection)
      GL.LoadIdentity ()
      GL.Ortho (-1.0f, 1.0f, -1.5f, 1.5f, -1.0f, 1.0f)
      GL.MatrixMode (All.Modelview)
      GL.Rotate (3.0f, 0.0f, 0.0f, 1.0f)

      GL.ClearColor (0.5f, 0.5f, 0.5f, 1.0f)
      GL.Clear (ClearBufferMask.ColorBufferBit)

      GL.VertexPointer (2, All.Float, 0, squareVertices)
      GL.EnableClientState (All.VertexArray)
      GL.ColorPointer (4, All.UnsignedByte, 0, squareColors)
      GL.EnableClientState (All.ColorArray)

      GL.DrawArrays (All.TriangleStrip, 0, 4)

      x.SwapBuffers ()
