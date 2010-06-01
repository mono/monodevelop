// 
// AutoTestServer.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Reflection;
using System.Threading;

namespace MonoDevelop.Components.AutoTest
{
	public class AutoTestSession: MarshalByRefObject
	{
		object currentObject;
		bool trackingActiveWidget;
		
		ManualResetEvent syncEvent = new ManualResetEvent (false);
		
		public AutoTestSession ()
		{
		}
		
		public override object InitializeLifetimeService ()
		{
			return null;
		}
		
		
		public void ExecuteCommand (object cmd)
		{
			Gtk.Application.Invoke (delegate {
				AutoTestService.CommandManager.DispatchCommand (cmd, null, CurrentObject);
			});
		}
		
		object Sync (Func<object> del)
		{
			object res = null;
			Exception error = null;
			
			syncEvent.Reset ();
			Gtk.Application.Invoke (delegate {
				try {
					res = del ();
				} catch (Exception ex) {
					error = ex;
				} finally {
					syncEvent.Set ();
				}
			});
			if (!syncEvent.WaitOne (20000))
				throw new Exception ("Timeout while executing synchronized call");
			if (error != null)
				throw error;
			return SafeObject (res);
		}
		
		public object GlobalInvoke (string name, object[] args)
		{
			return Sync (delegate {
				int i = name.LastIndexOf ('.');
				if (i == -1)
					throw new Exception ("Member name not specified");
				object ob = GetGlobalObject (name.Substring (0, i));
				if (ob == null)
					throw new Exception ("Object not found");
				if (ob is Type)
					return Invoke (null, (Type)ob, name.Substring (i+1), args);
				else
					return Invoke (ob, ob.GetType (), name.Substring (i+1), args);
			});
		}
		
		public object GetGlobalValue (string name)
		{
			return Sync (delegate {
				return GetGlobalObject (name);
			});
		}
		
		public void SetGlobalValue (string name, object value)
		{
			Sync (delegate {
				int i = name.LastIndexOf ('.');
				if (i == -1)
					throw new Exception ("Member name not specified");
				object ob = GetGlobalObject (name.Substring (0, i));
				if (ob == null)
					throw new Exception ("Object not found");
				if (ob is Type)
					SetValue (null, (Type) ob, name.Substring (i+1), value);
				else
					SetValue (ob, ob.GetType (), name.Substring (i+1), value);
				return null;
			});
		}
		
		public void TypeText (string text)
		{
			foreach (char c in text) {
				uint key = Gdk.Global.UnicodeToKeyval ((uint)c);
				SendKeyPress ((Gdk.Key)key, Gdk.ModifierType.None);
			}
		}
		
		public void SendKeyPress (Gdk.Key key, Gdk.ModifierType state)
		{
			SendKeyPress (key, state, null);
		}
		
		public void SendKeyPress (Gdk.Key key, Gdk.ModifierType state, string subWindow)
		{
			Sync (delegate {
				SendKeyEvent ((Gtk.Widget)CurrentObject, (uint)key, state, Gdk.EventType.KeyPress, subWindow);
				return null;
			});
			Thread.Sleep (15);
			Sync (delegate {
				SendKeyEvent ((Gtk.Widget)CurrentObject, (uint)key, state, Gdk.EventType.KeyRelease, subWindow);
				return null;
			});
			Thread.Sleep (10);
		}
		
		public void SelectObject (string name)
		{
			Sync (delegate {
				currentObject = GetGlobalObject (name);
				trackingActiveWidget = false;
				return null;
			});
		}
		
		public void SelectActiveWidget ()
		{
			Sync (delegate {
				trackingActiveWidget = true;
				return null;
			});
		}
		
		object CurrentObject {
			get {
				if (trackingActiveWidget)
					return GetActiveWidget ();
				else
					return currentObject;
			}
		}
		
		object GetActiveWidget ()
		{
			Gtk.Window win = null;
			foreach (Gtk.Window w in Gtk.Window.ListToplevels ()) {
				if (w.Visible && w.HasToplevelFocus)
					win = w;
			}
			
			if (win != null) {
				Gtk.Widget widget = win;
				while (widget is Gtk.Container) {
					Gtk.Widget child = ((Gtk.Container)widget).FocusChild;
					if (child != null)
						widget = child;
					else
						break;
				}
				return widget;
			}
			return win;
		}
		
		public object GetValue (string name)
		{
			return Sync (delegate {
				return GetValue (CurrentObject, CurrentObject.GetType (), name);
			});
		}
		
		public void SetValue (string name, object value)
		{
			Sync (delegate {
				SetValue (CurrentObject, CurrentObject.GetType (), name, value);
				return null;
			});
		}
		
		public object Invoke (string methodName, object[] args)
		{
			return Sync (delegate {
				return Invoke (CurrentObject, CurrentObject.GetType (), methodName, args);
			});
		}
		
		object SafeObject (object ob)
		{
			if (ob == null)
				return null;
			if (ob.GetType ().IsPrimitive || ob.GetType ().IsSerializable)
				return ob;
			if (ob is string)
				return ob;
			return null;
		}
		
		object Invoke (object target, Type type, string methodName, object[] args)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			flags |= BindingFlags.InvokeMethod;
			return type.InvokeMember (methodName, flags, null, target, args);
		}
		
		object GetValue (object target, Type type, string name)
		{
			int i = name.IndexOf ('.');
			string remaining = null;
			if (i != -1) {
				remaining = name.Substring (i+1);
				name = name.Substring (0, i);
			}
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			flags |= BindingFlags.GetField | BindingFlags.GetProperty;
			object res = type.InvokeMember (name, flags, null, target, null);
			
			if (remaining == null)
				return res;
			else
				return GetValue (res, res.GetType (), remaining);
		}
		
		void SetValue (object target, Type type, string name, object value)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			flags |= BindingFlags.SetField | BindingFlags.SetProperty;
			type.InvokeMember (name, flags, null, target, new object[] { value });
		}
		
		object GetGlobalObject (string name)
		{
			int i = 0;
			Type type;
			do {
				i = name.IndexOf ('.', i);
				if (i == -1)
					i = name.Length;
				string cname = name.Substring (0, i);
				type = Type.GetType (cname);
				i++;
			}
			while (type == null && i < name.Length);
			
			if (type == null)
				throw new Exception ("Object '" + name + "' not found");				
			
			if (i >= name.Length)
				return type;
			
			return GetValue (null, type, name.Substring (i));
		}
		
		void SendKeyEvent (Gtk.Widget target, uint keyval, Gdk.ModifierType state, Gdk.EventType eventType, string subWindow)
		{
			Gdk.KeymapKey[] keyms = Gdk.Keymap.Default.GetEntriesForKeyval (keyval);
			if (keyms.Length == 0)
				throw new Exception ("Keyval not found");
			
			Gdk.Window win;
			if (subWindow == null)
				win = target.GdkWindow;
			else
				win = (Gdk.Window) GetValue (target, target.GetType (), subWindow);
			
			NativeEventKeyStruct nativeEvent = new NativeEventKeyStruct (); 
			nativeEvent.type = eventType;
			nativeEvent.send_event = 1;
			nativeEvent.window = win.Handle;
			nativeEvent.state = (uint) state;
			nativeEvent.keyval = keyval;
			nativeEvent.group = (byte) keyms[0].Group;
			nativeEvent.hardware_keycode = (ushort) keyms[0].Keycode;
			nativeEvent.length = 0;
			nativeEvent.time = Gtk.Global.CurrentEventTime;
			
			IntPtr ptr = GLib.Marshaller.StructureToPtrAlloc (nativeEvent); 
			try {
				Gdk.EventKey evnt = new Gdk.EventKey (ptr); 
				Gdk.EventHelper.Put (evnt); 
			} finally {
				Marshal.FreeHGlobal (ptr);
			}
		}
	}

	
	[StructLayout (LayoutKind.Sequential)] 
	struct NativeEventKeyStruct { 
		public Gdk.EventType type; 
		public IntPtr window; 
		public sbyte send_event; 
		public uint time; 
		public uint state; 
		public uint keyval; 
		public int length;
		public IntPtr str;
		public ushort hardware_keycode;
		public byte group;
		public uint is_modifier;
	} 
	
	[StructLayout (LayoutKind.Sequential)] 
	struct NativeEventButtonStruct { 
		public Gdk.EventType type; 
		public IntPtr window; 
		public sbyte send_event; 
		public uint time; 
		public double x; 
		public double y; 
		public IntPtr axes; 
		public uint state; 
		public uint button; 
		public IntPtr device; 
		public double x_root; 
		public double y_root; 
	} 
	
	[StructLayout (LayoutKind.Sequential)] 
	struct NativeEventScrollStruct { 
		public Gdk.EventType type; 
		public IntPtr window; 
		public sbyte send_event; 
		public uint time; 
		public double x; 
		public double y; 
		public uint state; 
		public Gdk.ScrollDirection direction;
		public IntPtr device; 
		public double x_root; 
		public double y_root; 
	} 
}

