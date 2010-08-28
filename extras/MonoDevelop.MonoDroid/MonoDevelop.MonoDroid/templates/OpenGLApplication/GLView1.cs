using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES11;
using OpenTK.Platform;
using OpenTK.Platform.Android;

using Android.Views;
using Android.Content;

namespace ${Namespace}
{
	class GLView1 : AndroidGameView
	{
		public GLView1 (Context context) : base (context)
		{
		}

		// This gets called when the drawing surface is ready
		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			// Run the render loop
			Run ();
		}

		// This gets called on each frame render
		protected override void OnRenderFrame (FrameEventArgs e)
		{
			base.OnRenderFrame (e);

			MakeCurrent ();

			GL.MatrixMode (All.Projection);
			GL.LoadIdentity ();
			GL.Ortho (-1.0f, 1.0f, -1.5f, 1.5f, -1.0f, 1.0f);
			GL.MatrixMode (All.Modelview);
			GL.Rotate (3.0f, 0.0f, 0.0f, 1.0f);

			GL.ClearColor (0.5f, 0.5f, 0.5f, 1.0f);
			GL.Clear ((uint)All.ColorBufferBit);

			GL.VertexPointer (2, All.Float, 0, square_vertices);
			GL.EnableClientState (All.VertexArray);
			GL.ColorPointer (4, All.UnsignedByte, 0, square_colors);
			GL.EnableClientState (All.ColorArray);

			GL.DrawArrays (All.TriangleStrip, 0, 4);

			SwapBuffers ();
		}

		float[] square_vertices = {
			-0.5f, -0.5f,
			0.5f, -0.5f,
			-0.5f, 0.5f, 
			0.5f, 0.5f,
		};

		byte[] square_colors = {
			255, 255,   0, 255,
			0,   255, 255, 255,
			0,     0,    0,  0,
			255,   0,  255, 255,
		};
	}
}
