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
using System.Collections.Generic;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Tasks;

namespace MonoDevelop.Components.AutoTest
{
	public class AutoTestSession: MarshalByRefObject
	{
		object currentObject;
		
		readonly ManualResetEvent syncEvent = new ManualResetEvent (false);
		
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

			if (DispatchService.IsGuiThread)
				return SafeObject (del ());

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
				var type = ob as Type;
				if (type != null)
					return Invoke (null, type, name.Substring (i + 1), args);
				return Invoke (ob, ob.GetType (), name.Substring (i + 1), args);
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
				var type = ob as Type;
				if (type != null)
					SetValue (null, type, name.Substring (i + 1), value);
				else
					SetValue (ob, ob.GetType (), name.Substring (i+1), value);
				return null;
			});
		}
		
		public void TypeText (string text)
		{
			foreach (char c in text) {
				Gdk.Key key;
				if (c == '\n')
					key = Gdk.Key.Return;
				else
					key = (Gdk.Key) Gdk.Global.UnicodeToKeyval ((uint)c);

				SendKeyPress (key, Gdk.ModifierType.None);
			}
		}

		//TODO: expose ATK API over the session, instead of exposing specific widgets
		public bool SelectTreeviewItem (string treeViewName, string name, string after = null)
		{
			return (bool)Sync (delegate {
				var itemSelected = SelectWidget (treeViewName, true);
				if (!itemSelected)
					return false;

				bool parentFound = after == null;
				var treeView = currentObject as Gtk.TreeView;
				bool result = false;
				if (treeView != null) {
					treeView.Model.Foreach ((model, path, iter) => {
						var iterName = (string)model.GetValue (iter, 0);
						parentFound = parentFound || iterName.Contains (after);
						if (parentFound && string.Equals (name, iterName)) {
							treeView.SetCursor (path, treeView.Columns [0], false);

							result = true;
							return true;
						}
						return result;
					});
				}

				return result;
			});
		}

		public string[] GetTreeviewCells ()
		{
			var accessible = ((Gtk.Widget)currentObject).Accessible;
			return GetAccessibleChildren (accessible)
				.Where (c => c.Role == Atk.Role.TableCell)
				.Select (c => c.Name)
				.ToArray ();
		}

		IEnumerable<Atk.Object> GetAccessibleChildren (Atk.Object obj, bool recursive = false)
		{
			var childrenObjects = new List<Atk.Object> ();
			var count = obj.NAccessibleChildren;
			for (int i = 0; i < count; i++) {
				var child = obj.RefAccessibleChild (i);
				childrenObjects.Add (child);
				if (recursive)
					childrenObjects.AddRange (GetAccessibleChildren (child));
			}

			return childrenObjects;
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
				return null;
			});
		}
		
		public void SelectActiveWidget ()
		{
			Sync (delegate {
				currentObject = GetActiveWidget ();
				return null;
			});
		}

		public bool SelectWidget (string name, bool focus)
		{
			return (bool) Sync (delegate {
				var widget = GetWidget (GetFocusedWindow (), name);
				currentObject = widget;
				return widget != null && (!focus || FocusWidget (widget));
			});
		}

		public bool IsBuildSuccessful ()
		{
			return TaskService.Errors.Count (x => x.Severity == TaskSeverity.Error) == 0;
		}

		bool FocusWidget (Gtk.Widget widget)
		{
			if (widget.HasFocus)
				return true;
			if (widget.CanFocus) {
				widget.GrabFocus ();
				return true;
			}
			var container = widget as Gtk.Container;
			if (container != null) {
				var chain = container.FocusChain;
				System.Collections.IEnumerable children = chain.Length > 0 ? chain : container.AllChildren;
				foreach (Gtk.Widget child in children)
					if (FocusWidget (child))
						return true;
			}
			return false;
		}
		
		object CurrentObject {
			get { return currentObject; }
		}

		static Gtk.Window GetFocusedWindow (bool throwIfNotFound = true)
		{
			Gtk.Window win = null;
			foreach (Gtk.Window w in Gtk.Window.ListToplevels ())
				if (w.Visible && w.HasToplevelFocus)
					win = w;

			if (win == null && throwIfNotFound)
				throw new Exception ("No window is focused");

			return win;
		}

		Gtk.Widget GetActiveWidget ()
		{
			var win = GetFocusedWindow ();

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

		Gtk.Widget GetWidget (Gtk.Container container, string name)
		{
			foreach (Gtk.Widget child in container.Children) {
				if (child.Name == name)
					return child;
				var childContainer = child as Gtk.Container;
				if (childContainer != null) {
					var c = GetWidget (childContainer, name);
					if (c != null)
						return c;
				}
			}
			return null;
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

		public object GetPropertyValue (string propertyName)
		{
			return Sync (delegate {
				PropertyInfo propertyInfo = CurrentObject.GetType().GetProperty(propertyName);
				if (propertyInfo != null) {
					var propertyValue = propertyInfo.GetValue (CurrentObject);
					if (propertyValue != null)
						return propertyValue;
				}

				return false;
			});
		}

		public bool SetPropertyValue (string propertyName, object value, object[] index = null)
		{
			return (bool)Sync (delegate {
				PropertyInfo propertyInfo = CurrentObject.GetType().GetProperty(propertyName);
				if (propertyInfo != null)
					propertyInfo.SetValue (CurrentObject, value, index);

				return propertyInfo != null;
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
			Type type = null;
			var asms = AppDomain.CurrentDomain.GetAssemblies ();
			do {
				i = name.IndexOf ('.', i);
				if (i == -1)
					i = name.Length;
				string cname = name.Substring (0, i);
				foreach (var a in asms) {
					type = a.GetType (cname);
					if (type != null)
						break;
				}
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
			
			var nativeEvent = new NativeEventKeyStruct {
				type = eventType,
				send_event = 1,
				window = win.Handle,
				state = (uint)state,
				keyval = keyval,
				group = (byte)keyms [0].Group,
				hardware_keycode = (ushort)keyms [0].Keycode,
				length = 0,
				time = Gtk.Global.CurrentEventTime
			};
			
			IntPtr ptr = GLib.Marshaller.StructureToPtrAlloc (nativeEvent); 
			try {
				Gdk.EventHelper.Put (new Gdk.EventKey (ptr));
			} finally {
				Marshal.FreeHGlobal (ptr);
			}
		}

		public void WaitForWindow (string windowName, int timeout)
		{
			const int pollTime = 100;
			syncEvent.Reset ();

			GLib.Timeout.Add ((uint) pollTime, () => {
				var window = GetFocusedWindow (false);
				if (window != null && window.GetType ().FullName == windowName) {
					syncEvent.Set ();
					return false;
				}
				timeout -= pollTime;
				return timeout > 0;
			});

			if (!syncEvent.WaitOne (timeout))
				throw new Exception ("Timeout while executing synchronized call");
		}

		public AppQuery CreateNewQuery ()
		{
			return new AppQuery (Gtk.Window.ListToplevels ());
		}

		public AppResult[] ExecuteQuery (AppQuery query)
		{
			return query.Execute ();
		}
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
	// Analysis restore InconsistentNaming
}

