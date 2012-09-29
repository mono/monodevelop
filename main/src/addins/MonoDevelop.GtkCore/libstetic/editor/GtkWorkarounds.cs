// 
// GtkWorkarounds.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Stetic.Editor
{
	//from Mono.TextEditor/GtkWorkarounds.cs
	public static class GtkWorkarounds
	{
		public static bool TriggersContextMenu (Gdk.EventButton evt)
		{
			return evt.Type == Gdk.EventType.ButtonPress && IsContextMenuButton (evt);
		}
		
		public static bool IsContextMenuButton (Gdk.EventButton evt)
		{
			if (evt.Button == 3 &&
					(evt.State & (Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button2Mask)) == 0)
				return true;
			
			if (Platform.IsMac) {
				if (evt.Button == 1 &&
					(evt.State & Gdk.ModifierType.ControlMask) != 0 &&
					(evt.State & (Gdk.ModifierType.Button2Mask | Gdk.ModifierType.Button3Mask)) == 0)
				return true;
			}
			
			return false;
		}
		

		[DllImport ("gtksharpglue-2", CallingConvention = CallingConvention.Cdecl)]
		static extern void gtksharp_container_leak_fixed_marker ();

		static HashSet<Type> fixedContainerTypes;
		static Dictionary<IntPtr,ForallDelegate> forallCallbacks;
		static bool containerLeakFixed;
		
		// Works around BXC #3801 - Managed Container subclasses are incorrectly resurrected, then leak.
		// It does this by registering an alternative callback for gtksharp_container_override_forall, which
		// ignores callbacks if the wrapper no longer exists. This means that the objects no longer enter a
		// finalized->release->dispose->re-wrap resurrection cycle.
		// We use a dynamic method to access internal/private GTK# API in a performant way without having to track
		// per-instance delegates.
		public static void FixContainerLeak (Gtk.Container c)
		{
			if (containerLeakFixed) {
				return;
			}
			FixContainerLeak (c.GetType ());
		}

		static void FixContainerLeak (Type t)
		{
			if (containerLeakFixed) {
				return;
			}

			if (fixedContainerTypes == null) {
				try {
					gtksharp_container_leak_fixed_marker ();
					containerLeakFixed = true;
					return;
				} catch (EntryPointNotFoundException) {
				}
				fixedContainerTypes = new HashSet<Type>();
				forallCallbacks = new Dictionary<IntPtr, ForallDelegate> ();
			}

			if (!fixedContainerTypes.Add (t)) {
				return;
			}

			//need to fix the callback for the type and all the managed supertypes
			var lookupGType = typeof (GLib.Object).GetMethod ("LookupGType", BindingFlags.Static | BindingFlags.NonPublic);
			do {
				var gt = (GLib.GType) lookupGType.Invoke (null, new[] { t });
				var cb = CreateForallCallback (gt.Val);
				forallCallbacks[gt.Val] = cb;
				gtksharp_container_override_forall (gt.Val, cb);
				t = t.BaseType;
			} while (fixedContainerTypes.Add (t) && t.Assembly != typeof (Gtk.Container).Assembly);
		}

		static ForallDelegate CreateForallCallback (IntPtr gtype)
		{
			var dm = new DynamicMethod (
				"ContainerForallCallback",
				typeof(void),
				new Type[] { typeof(IntPtr), typeof(bool), typeof(IntPtr), typeof(IntPtr) },
				typeof(GtkWorkarounds).Module,
				true);
			
			var invokerType = typeof(Gtk.Container.CallbackInvoker);
			
			//this was based on compiling a similar method and disassembling it
			ILGenerator il = dm.GetILGenerator ();
			var IL_002b = il.DefineLabel ();
			var IL_003f = il.DefineLabel ();
			var IL_0060 = il.DefineLabel ();
			var label_return = il.DefineLabel ();

			var loc_container = il.DeclareLocal (typeof(Gtk.Container));
			var loc_obj = il.DeclareLocal (typeof(object));
			var loc_invoker = il.DeclareLocal (invokerType);
			var loc_ex = il.DeclareLocal (typeof(Exception));

			//check that the type is an exact match
			// prevent stack overflow, because the callback on a more derived type will handle everything
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Call, typeof(GLib.ObjectManager).GetMethod ("gtksharp_get_type_id", BindingFlags.Static | BindingFlags.NonPublic));

			il.Emit (OpCodes.Ldc_I8, gtype.ToInt64 ());
			il.Emit (OpCodes.Newobj, typeof (IntPtr).GetConstructor (new Type[] { typeof (Int64) }));
			il.Emit (OpCodes.Call, typeof (IntPtr).GetMethod ("op_Equality", BindingFlags.Static | BindingFlags.Public));
			il.Emit (OpCodes.Brfalse, label_return);

			il.BeginExceptionBlock ();
			il.Emit (OpCodes.Ldnull);
			il.Emit (OpCodes.Stloc, loc_container);
			il.Emit (OpCodes.Ldsfld, typeof (GLib.Object).GetField ("Objects", BindingFlags.Static | BindingFlags.NonPublic));
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Box, typeof (IntPtr));
			il.Emit (OpCodes.Callvirt, typeof (System.Collections.Hashtable).GetProperty ("Item").GetGetMethod ());
			il.Emit (OpCodes.Stloc, loc_obj);
			il.Emit (OpCodes.Ldloc, loc_obj);
			il.Emit (OpCodes.Brfalse, IL_002b);

			var tref = typeof (GLib.Object).Assembly.GetType ("GLib.ToggleRef");
			il.Emit (OpCodes.Ldloc, loc_obj);
			il.Emit (OpCodes.Castclass, tref);
			il.Emit (OpCodes.Callvirt, tref.GetProperty ("Target").GetGetMethod ());
			il.Emit (OpCodes.Isinst, typeof (Gtk.Container));
			il.Emit (OpCodes.Stloc, loc_container);
			
			il.MarkLabel (IL_002b);
			il.Emit (OpCodes.Ldloc, loc_container);
			il.Emit (OpCodes.Brtrue, IL_003f);
			
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ldarg_1);
			il.Emit (OpCodes.Ldarg_2);
			il.Emit (OpCodes.Ldarg_3);
			il.Emit (OpCodes.Call, typeof (Gtk.Container).GetMethod ("gtksharp_container_base_forall", BindingFlags.Static | BindingFlags.NonPublic));
			il.Emit (OpCodes.Br, IL_0060);
			
			il.MarkLabel (IL_003f);
			il.Emit (OpCodes.Ldloca_S, 2);
			il.Emit (OpCodes.Ldarg_2);
			il.Emit (OpCodes.Ldarg_3);
			il.Emit (OpCodes.Call, invokerType.GetConstructor (
				BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof (IntPtr), typeof (IntPtr) }, null));
			il.Emit (OpCodes.Ldloc, loc_container);
			il.Emit (OpCodes.Ldarg_1);
			il.Emit (OpCodes.Ldloc, loc_invoker);
			il.Emit (OpCodes.Box, invokerType);
			il.Emit (OpCodes.Ldftn, invokerType.GetMethod ("Invoke"));
			il.Emit (OpCodes.Newobj, typeof (Gtk.Callback).GetConstructor (
				BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof (object), typeof (IntPtr) }, null));
			var forallMeth = typeof (Gtk.Container).GetMethod ("ForAll",
				BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof (bool), typeof (Gtk.Callback) }, null);
			il.Emit (OpCodes.Callvirt, forallMeth);
			
			il.MarkLabel (IL_0060);
			
			il.BeginCatchBlock (typeof (Exception));
			il.Emit (OpCodes.Stloc, loc_ex);
			il.Emit (OpCodes.Ldloc, loc_ex);
			il.Emit (OpCodes.Ldc_I4_0);
			il.Emit (OpCodes.Call, typeof (GLib.ExceptionManager).GetMethod ("RaiseUnhandledException"));
			il.Emit (OpCodes.Leave, label_return);
			il.EndExceptionBlock ();
			
			il.MarkLabel (label_return);
			il.Emit (OpCodes.Ret);
			
			return (ForallDelegate) dm.CreateDelegate (typeof (ForallDelegate));
		}
		
		[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
		delegate void ForallDelegate (IntPtr container, bool include_internals, IntPtr cb, IntPtr data);
		
		[DllImport("gtksharpglue-2", CallingConvention = CallingConvention.Cdecl)]
		static extern void gtksharp_container_override_forall (IntPtr gtype, ForallDelegate cb);
	}
	
	//from Mono.TextEditor/Platform.cs
	public static class Platform
	{
		static Platform ()
		{
 			IsWindows = System.IO.Path.DirectorySeparatorChar == '\\';
 			IsMac = !IsWindows && IsRunningOnMac();
			IsX11 = !IsMac && System.Environment.OSVersion.Platform == PlatformID.Unix;
		}
		
		static Gdk.Keymap keymap = Gdk.Keymap.Default;
		
		public static bool IsMac { get; private set; }
		public static bool IsX11 { get; private set; }
		public static bool IsWindows { get; private set; }
		
		//From Managed.Windows.Forms/XplatUI
		static bool IsRunningOnMac ()
		{
			IntPtr buf = IntPtr.Zero;
			try {
				buf = Marshal.AllocHGlobal (8192);
				// This is a hacktastic way of getting sysname from uname ()
				if (uname (buf) == 0) {
					string os = Marshal.PtrToStringAnsi (buf);
					if (os == "Darwin")
						return true;
				}
			} catch {
			} finally {
				if (buf != IntPtr.Zero)
					Marshal.FreeHGlobal (buf);
			}
		
			return false;
		}
		
		[DllImport ("libc")]
		static extern int uname (IntPtr buf);
	}
}
