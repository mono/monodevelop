//
// GtkGestures.cs
//
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.InteropServices;
using Gdk;

namespace Mono.TextEditor
{
	public static class GtkGestures
	{
		const int GDK_GESTURE_MAGNIFY = 37;
		const int GDK_GESTURE_ROTATE  = 38;
		const int GDK_GESTURE_SWIPE   = 39;

		[DllImport (PangoUtil.LIBGDK, CallingConvention = CallingConvention.Cdecl)]
		static extern bool gdk_quartz_supports_gesture_events ();

		static bool isSupported;

		static GtkGestures ()
		{
			if (Platform.IsMac) {
				try {
					isSupported = gdk_quartz_supports_gesture_events ();
				} catch (EntryPointNotFoundException) {
				}
			}
		}

		public static bool IsSupported { get { return isSupported; } }

		public static void AddGestureMagnifyHandler (this Gtk.Widget widget, EventHandler<GestureMagnifyEventArgs> handler)
		{
			if (!isSupported)
				throw new NotSupportedException ();
			var signal = GLib.Signal.Lookup (widget, "gesture-magnify-event", typeof(GestureMagnifyEventArgs));
			signal.AddDelegate (new EventHandler<GestureMagnifyEventArgs> (handler));
		}

		public static void AddGestureRotateHandler (this Gtk.Widget widget, EventHandler<GestureRotateEventArgs> handler)
		{
			if (!isSupported)
				throw new NotSupportedException ();
			var signal = GLib.Signal.Lookup (widget, "gesture-rotate-event", typeof(GestureRotateEventArgs));
			signal.AddDelegate (new EventHandler<GestureRotateEventArgs> (handler));
		}

		public static void AddGestureSwipeHandler (this Gtk.Widget widget, EventHandler<GestureSwipeEventArgs> handler)
		{
			if (!isSupported)
				throw new NotSupportedException ();
			var signal = GLib.Signal.Lookup (widget, "gesture-swipe-event", typeof(GestureSwipeEventArgs));
			signal.AddDelegate (new EventHandler<GestureSwipeEventArgs> (handler));
		}
	}

	public unsafe class GestureMagnifyEventArgs : GLib.SignalArgs
	{
		//have to force pack=4, or Mono aligns doubles to 8 bytes
		[StructLayout (LayoutKind.Sequential, Pack=4)]
		struct GdkEventGestureMagnify
		{
			public Gdk.EventType type;
			public IntPtr window;
			public sbyte send_event;
			public uint time;
			public double x, y;
			public uint state;
			public double magnification;
			public IntPtr device;
			public double x_root, y_root;
		}

		// I tried to mimic the GTK# pattern of having a subclassed Event object on the EventArgs, but I gave up on
		// figuring out how to get GTK# to marshal the event to a custom GestureMagnifyEvent class. Instead we just
		// lift all the accessors up to the args class and dereference the handle directly.
		GdkEventGestureMagnify *evt {
			get {
				var handle = ((Event)Args[0]).Handle;
				return (GdkEventGestureMagnify *) handle;
			}
		}

		public Gdk.Window Window {
			get {
				return (Gdk.Window) GLib.Object.GetObject (evt->window);
			}
		}

		public Device Device {
			get {
				return (Device) GLib.Object.GetObject (evt->device);
			}
		}

		public uint Time { get { return evt->time; } }
		public double X { get { return evt->x; } }
		public double Y { get { return evt->y; } }
		public ModifierType State { get { return (ModifierType) evt->state; } }
		public double Magnification { get { return evt->magnification; } }
		public double XRoot { get { return evt->x_root; } }
		public double YRoot { get { return evt->y_root; } }
	}

	public unsafe class GestureRotateEventArgs : GLib.SignalArgs
	{
		[StructLayout (LayoutKind.Sequential, Pack=4)]
		struct GdkEventGestureRotate
		{
			public Gdk.EventType type;
			public IntPtr window;
			public sbyte send_event;
			public uint time;
			public double x, y;
			public uint state;
			public double rotation;
			public IntPtr device;
			public double x_root, y_root;
		}

		GdkEventGestureRotate *evt {
			get {
				var handle = ((Event)Args[0]).Handle;
				return (GdkEventGestureRotate *) handle;
			}
		}

		public Gdk.Window Window {
			get {
				return (Gdk.Window) GLib.Object.GetObject (evt->window);
			}
		}

		public Device Device {
			get {
				return (Device) GLib.Object.GetObject (evt->device);
			}
		}

		public uint Time { get { return evt->time; } }
		public double X { get { return evt->x; } }
		public double Y { get { return evt->y; } }
		public ModifierType State { get { return (ModifierType) evt->state; } }
		public double Rotation { get { return evt->rotation; } }
		public double XRoot { get { return evt->x_root; } }
		public double YRoot { get { return evt->y_root; } }
	}

	public unsafe class GestureSwipeEventArgs : GLib.SignalArgs
	{
		[StructLayout (LayoutKind.Sequential, Pack=4)]
		struct GdkEventGestureSwipe
		{
			public Gdk.EventType type;
			public IntPtr window;
			public sbyte send_event;
			public uint time;
			public double x, y;
			public uint state;
			public double delta_x, delta_y;
			public IntPtr device;
			public double x_root, y_root;
		}

		GdkEventGestureSwipe *evt {
			get {
				var handle = ((Event)Args[0]).Handle;
				return (GdkEventGestureSwipe *) handle;
			}
		}

		public Gdk.Window Window {
			get {
				return (Gdk.Window) GLib.Object.GetObject (evt->window);
			}
		}

		public Device Device {
			get {
				return (Device) GLib.Object.GetObject (evt->device);
			}
		}

		public uint Time { get { return evt->time; } }
		public double X { get { return evt->x; } }
		public double Y { get { return evt->y; } }
		public ModifierType State { get { return (ModifierType) evt->state; } }
		public double DeltaX { get { return evt->delta_x; } }
		public double DeltaY { get { return evt->delta_y; } }
		public double XRoot { get { return evt->x_root; } }
		public double YRoot { get { return evt->y_root; } }
	}

	/*
	void PrintOffsets ()
	{
		GdkEventGestureMagnify *v = (GdkEventGestureMagnify *)0;
		Console.WriteLine ("type          {0}", (int)&(v->type));
		Console.WriteLine ("window        {0}", (int)&(v->window));
		Console.WriteLine ("send_event    {0}", (int)&(v->send_event));
		Console.WriteLine ("time          {0}", (int)&(v->time));
		Console.WriteLine ("x             {0}", (int)&(v->x));
		Console.WriteLine ("y             {0}", (int)&(v->y));
		Console.WriteLine ("state         {0}", (int)&(v->state));
		Console.WriteLine ("magnification {0}", (int)&(v->magnification));
		Console.WriteLine ("x_root        {0}", (int)&(v->x_root));
		Console.WriteLine ("y_root        {0}", (int)&(v->y_root));
	}

	// gcc -m32 test.c `pkg-config --cflags gtk+-2.0`
	#include <gtk/gtk.h>

	int main (int argc, char* argv)
	{
		GdkEventGestureMagnify *v = (GdkEventGestureMagnify *)0;
		printf ("type          %d\n", (int)&(v->type));
		printf ("window        %d\n", (int)&(v->window));
		printf ("send_event    %d\n", (int)&(v->send_event));
		printf ("time          %d\n", (int)&(v->time));
		printf ("x             %d\n", (int)&(v->x));
		printf ("y             %d\n", (int)&(v->y));
		printf ("state         %d\n", (int)&(v->state));
		printf ("magnification %d\n", (int)&(v->magnification));
		printf ("x_root        %d\n", (int)&(v->x_root));
		printf ("y_root        %d\n", (int)&(v->y_root));
	}
	*/
}
