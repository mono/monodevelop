//
// GtkWorkarounds.cs
//
// Authors: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (C) 2011 Xamarin Inc.
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
//

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Mono.TextEditor
{
	public static class GtkWorkarounds
	{
		const string LIBOBJC ="/usr/lib/libobjc.dylib";
		
		[DllImport (LIBOBJC, EntryPoint = "sel_registerName")]
		static extern IntPtr sel_registerName (string selector);
		
		[DllImport (LIBOBJC, EntryPoint = "objc_getClass")]
		static extern IntPtr objc_getClass (string klass);
		
		[DllImport (LIBOBJC, EntryPoint = "objc_msgSend")]
		static extern IntPtr objc_msgSend_IntPtr (IntPtr klass, IntPtr selector);
		
		[DllImport (LIBOBJC, EntryPoint = "objc_msgSend")]
		static extern void objc_msgSend_void_bool (IntPtr klass, IntPtr selector, bool arg);
		
		[DllImport (LIBOBJC, EntryPoint = "objc_msgSend")]
		static extern bool objc_msgSend_bool (IntPtr klass, IntPtr selector);
		
		[DllImport (LIBOBJC, EntryPoint = "objc_msgSend")]
		static extern bool objc_msgSend_int_int (IntPtr klass, IntPtr selector, int arg);
		
		[DllImport (LIBOBJC, EntryPoint = "objc_msgSend_stret")]
		static extern void objc_msgSend_RectangleF (out RectangleF rect, IntPtr klass, IntPtr selector);
		
		static IntPtr cls_NSScreen;
		static IntPtr sel_screens, sel_objectEnumerator, sel_nextObject, sel_frame, sel_visibleFrame,
			sel_activateIgnoringOtherApps, sel_isActive, sel_requestUserAttention;
		static IntPtr sharedApp;
		
		const int NSCriticalRequest = 0;
		const int NSInformationalRequest = 10;
		
		static System.Reflection.MethodInfo glibObjectGetProp, glibObjectSetProp;
		
		public static int GtkMinorVersion = 12, GtkMicroVersion = 0;
		static bool oldMacKeyHacks = false;
		
		static GtkWorkarounds ()
		{
			if (Platform.IsMac) {
				InitMac ();
			}
			
			var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
			glibObjectSetProp = typeof (GLib.Object).GetMethod ("SetProperty", flags);
			glibObjectGetProp = typeof (GLib.Object).GetMethod ("GetProperty", flags);
			
			foreach (int i in new [] { 24, 22, 20, 18, 16, 14 }) {
				if (Gtk.Global.CheckVersion (2, (uint)i, 0) == null) {
					GtkMinorVersion = i;
					break;
				}
			}
			
			for (int i = 1; i < 20; i++) {
				if (Gtk.Global.CheckVersion (2, (uint)GtkMinorVersion, (uint)i) == null) {
					GtkMicroVersion = i;
				} else {
					break;
				}
			}
			
			//opt into the fixes on GTK+ >= 2.24.8
			if (Platform.IsMac) {
				try {
					gdk_quartz_set_fix_modifiers (true);
				} catch (EntryPointNotFoundException) {
					oldMacKeyHacks = true;
				}
			}
			
			keymap.KeysChanged += delegate {
				mappedKeys.Clear ();
			};
		}

		static void InitMac ()
		{
			cls_NSScreen = objc_getClass ("NSScreen");
			sel_screens = sel_registerName ("screens");
			sel_objectEnumerator = sel_registerName ("objectEnumerator");
			sel_nextObject = sel_registerName ("nextObject");
			sel_visibleFrame = sel_registerName ("visibleFrame");
			sel_frame = sel_registerName ("frame");
			sel_activateIgnoringOtherApps = sel_registerName ("activateIgnoringOtherApps:");
			sel_isActive = sel_registerName ("isActive");
			sel_requestUserAttention = sel_registerName ("requestUserAttention:");
			sharedApp = objc_msgSend_IntPtr (objc_getClass ("NSApplication"), sel_registerName ("sharedApplication"));
		}
		
		static Gdk.Rectangle MacGetUsableMonitorGeometry (Gdk.Screen screen, int monitor)
		{
			IntPtr array = objc_msgSend_IntPtr (cls_NSScreen, sel_screens);
			IntPtr iter = objc_msgSend_IntPtr (array, sel_objectEnumerator);
			Gdk.Rectangle ygeometry = screen.GetMonitorGeometry (monitor);
			Gdk.Rectangle xgeometry = screen.GetMonitorGeometry (0);
			RectangleF visible, frame;
			IntPtr scrn;
			int i = 0;
			
			while ((scrn = objc_msgSend_IntPtr (iter, sel_nextObject)) != IntPtr.Zero && i < monitor)
				i++;
			
			if (scrn == IntPtr.Zero)
				return screen.GetMonitorGeometry (monitor);
			
			objc_msgSend_RectangleF (out visible, scrn, sel_visibleFrame);
			objc_msgSend_RectangleF (out frame, scrn, sel_frame);
			
			// Note: Frame and VisibleFrame rectangles are relative to monitor 0, but we need absolute
			// coordinates.
			visible.X += xgeometry.X;
			frame.X += xgeometry.X;
			
			// VisibleFrame.Y is the height of the Dock if it is at the bottom of the screen, so in order
			// to get the menu height, we just figure out the difference between the visibleFrame height
			// and the actual frame height, then subtract the Dock height.
			//
			// We need to swap the Y offset with the menu height because our callers expect the Y offset
			// to be from the top of the screen, not from the bottom of the screen.
			float x, y, width, height;
			
			if (visible.Height < frame.Height) {
				float dockHeight = visible.Y - frame.Y;
				float menubarHeight = (frame.Height - visible.Height) - dockHeight;
				
				height = frame.Height - menubarHeight - dockHeight;
				y = ygeometry.Y + menubarHeight;
			} else {
				height = frame.Height;
				y = ygeometry.Y;
			}
			
			// Takes care of the possibility of the Dock being positioned on the left or right edge of the screen.
			width = System.Math.Min (visible.Width, frame.Width);
			x = System.Math.Max (visible.X, frame.X);
			
			return new Gdk.Rectangle ((int) x, (int) y, (int) width, (int) height);
		}
		
		static void MacRequestAttention (bool critical)
		{
			int kind = critical?  NSCriticalRequest : NSInformationalRequest;
			objc_msgSend_int_int (sharedApp, sel_requestUserAttention, kind);
		}
		
		public static Gdk.Rectangle GetUsableMonitorGeometry (this Gdk.Screen screen, int monitor)
		{
			if (Platform.IsMac)
				return MacGetUsableMonitorGeometry (screen, monitor);
			
			return screen.GetMonitorGeometry (monitor);
		}
		
		public static int RunDialogWithNotification (Gtk.Dialog dialog)
		{
			if (Platform.IsMac)
				MacRequestAttention (dialog.Modal);
			
			return dialog.Run ();
		}
		
		public static void PresentWindowWithNotification (this Gtk.Window window)
		{
			window.Present ();
			
			if (Platform.IsMac) {
				var dialog = window as Gtk.Dialog;
				MacRequestAttention (dialog == null? false : dialog.Modal);
			}
		}
		
		public static GLib.Value GetProperty (this GLib.Object obj, string name)
		{
			return (GLib.Value) glibObjectGetProp.Invoke (obj, new object[] { name });
		}
		
		public static void SetProperty (this GLib.Object obj, string name, GLib.Value value)
		{
			glibObjectSetProp.Invoke (obj, new object[] { name, value });
		}
		
		public static bool TriggersContextMenu (this Gdk.EventButton evt)
		{
			return evt.Type == Gdk.EventType.ButtonPress && IsContextMenuButton (evt);
		}
		
		public static bool IsContextMenuButton (this Gdk.EventButton evt)
		{
			if (evt.Button == 3 &&
					(evt.State & (Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button2Mask)) == 0)
				return true;
			
			if (Platform.IsMac) {
				if (!oldMacKeyHacks &&
					evt.Button == 1 &&
					(evt.State & Gdk.ModifierType.ControlMask) != 0 &&
					(evt.State & (Gdk.ModifierType.Button2Mask | Gdk.ModifierType.Button3Mask)) == 0)
				{
					return true;
				}
			}
			
			return false;
		}
		
		public static void GetPageScrollPixelDeltas (this Gdk.EventScroll evt, double pageSizeX, double pageSizeY,
			out double deltaX, out double deltaY)
		{
			if (!GetEventScrollDeltas (evt, out deltaX, out deltaY)) {
				var direction = evt.Direction;
				deltaX = deltaY = 0;
				if (pageSizeY != 0 && (direction == Gdk.ScrollDirection.Down || direction == Gdk.ScrollDirection.Up)) {
					deltaY = System.Math.Pow (pageSizeY, 2.0 / 3.0);
					deltaX = 0.0;
					if (direction == Gdk.ScrollDirection.Up)
						deltaY = -deltaY;
				} else if (pageSizeX != 0) {
					deltaX = System.Math.Pow (pageSizeX, 2.0 / 3.0);
					deltaY = 0.0;
					if (direction == Gdk.ScrollDirection.Left)
						deltaX = -deltaX;
				}
			}
		}
		
		public static void AddValueClamped (this Gtk.Adjustment adj, double value)
		{
			adj.Value = System.Math.Max (adj.Lower, System.Math.Min (adj.Value + value, adj.Upper - adj.PageSize));
		}
		
		[DllImport (PangoUtil.LIBGTK)]
		extern static bool gdk_event_get_scroll_deltas (IntPtr eventScroll, out double deltaX, out double deltaY);
		static bool scrollDeltasNotSupported;
		
		public static bool GetEventScrollDeltas (Gdk.EventScroll evt, out double deltaX, out double deltaY)
		{
			if (!scrollDeltasNotSupported) {
				try {
					return gdk_event_get_scroll_deltas (evt.Handle, out deltaX, out deltaY);
				} catch (EntryPointNotFoundException) {
					scrollDeltasNotSupported = true;
				}
			}
			deltaX = deltaY = 0;
			return false;
		}
		
		/// <summary>Shows a context menu.</summary>
		/// <param name='menu'>The menu.</param>
		/// <param name='parent'>The parent widget.</param>
		/// <param name='evt'>The mouse event. May be null if triggered by keyboard.</param>
		/// <param name='caret'>The caret/selection position within the parent, if the EventButton is null.</param>
		public static void ShowContextMenu (Gtk.Menu menu, Gtk.Widget parent, Gdk.EventButton evt, Gdk.Rectangle caret)
		{
			Gtk.MenuPositionFunc posFunc = null;
			
			if (parent != null) {
				menu.AttachToWidget (parent, null);
				posFunc = delegate (Gtk.Menu m, out int x, out int y, out bool pushIn) {
					Gdk.Window window = evt != null? evt.Window : parent.GdkWindow;
					window.GetOrigin (out x, out y);
					var alloc = parent.Allocation;
					if (evt != null) {
						x += (int) evt.X;
						y += (int) evt.Y;
					} else if (caret.X >= alloc.X && caret.Y >= alloc.Y) {
						x += caret.X;
						y += caret.Y + caret.Height;
					} else {
						x += alloc.X;
						y += alloc.Y;
					}
					Gtk.Requisition request = m.SizeRequest ();
					var screen = parent.Screen;
					Gdk.Rectangle geometry = GetUsableMonitorGeometry (screen, screen.GetMonitorAtPoint (x, y));
					
					//whether to push or flip menus that would extend offscreen
					//FIXME: this is the correct behaviour for mac, check other platforms
					bool flip_left = true;
					bool flip_up   = false;
					
					if (x + request.Width > geometry.Right) {
						if (flip_left) {
							x -= request.Width;
						} else {
							x = geometry.Right - request.Width;
						}
						
						if (x < geometry.Left)
							x = geometry.Left;
					}
					
					if (y + request.Height > geometry.Bottom) {
						if (flip_up) {
							y -= request.Height;
						} else {
							y = geometry.Bottom - request.Height;
						}
						
						if (y < geometry.Top)
							y = geometry.Top;
					}
					
					pushIn = false;
				};
			}
			
			uint time;
			uint button;
			
			if (evt == null) {
				time = Gtk.Global.CurrentEventTime;
				button = 0;
			} else {
				time = evt.Time;
				button = evt.Button;
			}
			
			//HACK: work around GTK menu issues on mac when passing button to menu.Popup
			//some menus appear and immediately hide, and submenus don't activate
			if (Platform.IsMac) {
				button = 0;
			}
			
			menu.Popup (null, null, posFunc, button, time);
		}
		
		public static void ShowContextMenu (Gtk.Menu menu, Gtk.Widget parent, Gdk.EventButton evt)
		{
			ShowContextMenu (menu, parent, evt, Gdk.Rectangle.Zero);
		}
		
		public static void ShowContextMenu (Gtk.Menu menu, Gtk.Widget parent, Gdk.Rectangle caret)
		{
			ShowContextMenu (menu, parent, null, caret);
		}
		
		struct MappedKeys
		{
			public Gdk.Key Key;
			public Gdk.ModifierType State;
			public KeyboardShortcut[] Accels;
		}
		
		//introduced in GTK 2.20
		[DllImport (PangoUtil.LIBGDK)]
		extern static bool gdk_keymap_add_virtual_modifiers (IntPtr keymap, ref Gdk.ModifierType state);
		
		//Custom patch in Mono Mac w/GTK+ 2.24.8+
		[DllImport (PangoUtil.LIBGDK)]
		extern static bool gdk_quartz_set_fix_modifiers (bool fix);
		
		static Gdk.Keymap keymap = Gdk.Keymap.Default;
		static Dictionary<ulong,MappedKeys> mappedKeys = new Dictionary<ulong,MappedKeys> ();
		
		/// <summary>Map raw GTK key input to work around platform bugs and decompose accelerator keys</summary>
		/// <param name='evt'>The raw key event</param>
		/// <param name='key'>The composed key</param>
		/// <param name='mod'>The composed modifiers</param>
		/// <param name='accels'>All the key/modifier decompositions that can be used as accelerators</param>
		public static void MapKeys (Gdk.EventKey evt, out Gdk.Key key, out Gdk.ModifierType state,
			out KeyboardShortcut[] accels)
		{
			//this uniquely identifies the raw key
			ulong id;
			unchecked {
				id = (((ulong)(uint)evt.State) | (((ulong)evt.HardwareKeycode) << 32) | (((ulong)evt.Group) << 48));
			}
			
			MappedKeys mapped;
			if (!mappedKeys.TryGetValue (id, out mapped)) {
				mappedKeys[id] = mapped = MapKeys (evt);
			}
			accels = mapped.Accels;
			key = mapped.Key;
			state = mapped.State;
		}
		
		static MappedKeys MapKeys (Gdk.EventKey evt)
		{
			MappedKeys mapped;
			ushort keycode = evt.HardwareKeycode;
			Gdk.ModifierType modifier = evt.State;
			byte grp = evt.Group;
			
			if (GtkMinorVersion >= 20) {
				gdk_keymap_add_virtual_modifiers (keymap.Handle, ref modifier);
			}
			
			//full key mapping
			uint keyval;
			int effectiveGroup, level;
			Gdk.ModifierType consumedModifiers;
			TranslateKeyboardState (keycode, modifier, grp, out keyval, out effectiveGroup,
				out level, out consumedModifiers);
			mapped.Key = (Gdk.Key)keyval;
			mapped.State = FixMacModifiers (evt.State & ~consumedModifiers, grp);
			
			//decompose the key into accel combinations
			var accelList = new List<KeyboardShortcut> ();
			
			const Gdk.ModifierType accelMods = Gdk.ModifierType.ShiftMask | Gdk.ModifierType.Mod1Mask
				| Gdk.ModifierType.ControlMask | Gdk.ModifierType.SuperMask |Gdk.ModifierType.MetaMask;
			
			//all accels ignore the lock key
			modifier &= ~Gdk.ModifierType.LockMask;
			
			//fully decomposed
			TranslateKeyboardState (evt.HardwareKeycode, Gdk.ModifierType.None, 0,
				out keyval, out effectiveGroup, out level, out consumedModifiers);
			accelList.Add (new KeyboardShortcut ((Gdk.Key)keyval, FixMacModifiers (modifier, grp) & accelMods));
			
			//with shift composed
			if ((modifier & Gdk.ModifierType.ShiftMask) != 0) {
				keymap.TranslateKeyboardState (evt.HardwareKeycode, Gdk.ModifierType.ShiftMask, 0,
					out keyval, out effectiveGroup, out level, out consumedModifiers);
				var m = FixMacModifiers ((modifier & ~consumedModifiers), grp) & accelMods;
				AddIfNotDuplicate (accelList, new KeyboardShortcut ((Gdk.Key)keyval, m));
			}
			
			//with group 1 composed
			if (grp == 1) {
				TranslateKeyboardState (evt.HardwareKeycode, modifier & ~Gdk.ModifierType.ShiftMask, 1,
					out keyval, out effectiveGroup, out level, out consumedModifiers);
				//somehow GTK on mac manages to consume a shift that we don't even pass to it
				if (oldMacKeyHacks) {
					consumedModifiers &= ~Gdk.ModifierType.ShiftMask;
				}
				var m = FixMacModifiers ((modifier & ~consumedModifiers), 0) & accelMods;
				AddIfNotDuplicate (accelList, new KeyboardShortcut ((Gdk.Key)keyval, m));
			}
			
			//with group 1 and shift composed
			if (grp == 1 && (modifier & Gdk.ModifierType.ShiftMask) != 0) {
				TranslateKeyboardState (evt.HardwareKeycode, modifier, 1,
					out keyval, out effectiveGroup, out level, out consumedModifiers);
				var m = FixMacModifiers ((modifier & ~consumedModifiers), 0) & accelMods;
				AddIfNotDuplicate (accelList, new KeyboardShortcut ((Gdk.Key)keyval, m));
			}
			
			//and also allow the fully mapped key as an accel
			AddIfNotDuplicate (accelList, new KeyboardShortcut (mapped.Key, mapped.State & accelMods));
			
			mapped.Accels = accelList.ToArray ();
			return mapped;
		}
		
		// Workaround for bug "Bug 688247 - Ctrl+Alt key not work on windows7 with bootcamp on a Mac Book Pro"
		// Ctrl+Alt should behave like right alt key - unfortunately TranslateKeyboardState doesn't handle it. 
		static void TranslateKeyboardState (uint hardware_keycode, Gdk.ModifierType state, int group, out uint keyval,
			out int effective_group, out int level, out Gdk.ModifierType consumed_modifiers)
		{
			if (Platform.IsWindows) {
				const Gdk.ModifierType ctrlAlt = Gdk.ModifierType.ControlMask | Gdk.ModifierType.Mod1Mask;
				if ((state & ctrlAlt) == ctrlAlt) {
					state = (state & ~ctrlAlt) | Gdk.ModifierType.Mod2Mask;
					group = 1;
				}
			}
			
			keymap.TranslateKeyboardState (hardware_keycode, state, group, out keyval, out effective_group,
				out level, out consumed_modifiers);
		}
		
		static Gdk.ModifierType FixMacModifiers (Gdk.ModifierType mod, byte grp)
		{
			if (!oldMacKeyHacks)
				return mod;
			
			// Mac GTK+ maps the command key to the Mod1 modifier, which usually means alt/
			// We map this instead to meta, because the Mac GTK+ has mapped the cmd key
			// to the meta key (yay inconsistency!). IMO super would have been saner.
			if ((mod & Gdk.ModifierType.Mod1Mask) != 0) {
				mod ^= Gdk.ModifierType.Mod1Mask;
				mod |= Gdk.ModifierType.MetaMask;
			}
			
			//some versions of GTK map opt as mod5, which converts to the virtual super modifier
			if ((mod & (Gdk.ModifierType.Mod5Mask | Gdk.ModifierType.SuperMask)) != 0) {
				mod ^= (Gdk.ModifierType.Mod5Mask | Gdk.ModifierType.SuperMask);
				mod |= Gdk.ModifierType.Mod1Mask;
			}
			
			// When opt modifier is active, we need to decompose this to make the command appear correct for Mac.
			// In addition, we can only inspect whether the opt/alt key is pressed by examining
			// the key's "group", because the Mac GTK+ treats opt as a group modifier and does
			// not expose it as an actual GDK modifier.
			if (grp == (byte) 1) {
				mod |= Gdk.ModifierType.Mod1Mask;
			}
			
			return mod;
		}
		
		static void AddIfNotDuplicate<T> (List<T> list, T item) where T : IEquatable<T>
		{
			for (int i = 0; i < list.Count; i++) {
				if (list[i].Equals (item))
					return;
			}
			list.Add (item);
		}
		
		/// <summary>Map raw GTK key input to work around platform bugs and decompose accelerator keys</summary>
		/// <param name='evt'>The raw key event</param>
		/// <param name='key'>The decomposed accelerator key</param>
		/// <param name='mod'>The decomposed accelerator modifiers</param>
		/// <param name='keyval'>The fully mapped keyval</param>
		[Obsolete ("Use MapKeys")]
		public static void MapRawKeys (Gdk.EventKey evt, out Gdk.Key key, out Gdk.ModifierType mod, out uint keyval)
		{
			Gdk.Key mappedKey;
			Gdk.ModifierType mappedMod;
			KeyboardShortcut[] accels;
			MapKeys (evt, out mappedKey, out mappedMod, out accels);
			
			keyval = (uint) mappedKey;
			key = accels[0].Key;
			mod = accels[0].Modifier;
		}

		[System.Runtime.InteropServices.DllImport ("libgdk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gdk_win32_drawable_get_handle (IntPtr drawable);
		
		enum DwmWindowAttribute
		{
			NcRenderingEnabled = 1,
			NcRenderingPolicy,
			TransitionsForceDisabled,
			AllowNcPaint,
			CaptionButtonBounds,
			NonClientRtlLayout,
			ForceIconicRepresentation,
			Flip3DPolicy,
			ExtendedFrameBounds,
			HasIconicBitmap,
			DisallowPeek,
			ExcludedFromPeek,
			Last,
		}
		
		struct Win32Rect
		{
			public int Left, Top, Right, Bottom;
			
			public Win32Rect (int left, int top, int right, int bottom)
			{
				this.Left = left;
				this.Top = top;
				this.Right = right;
				this.Bottom = bottom;
			}
		}
		
		static int win32RectMarshalSize = Marshal.SizeOf (typeof (Win32Rect));
		
		[DllImport ("dwmapi.dll")]
		static extern int DwmGetWindowAttribute (IntPtr hwnd, DwmWindowAttribute attribute, out Win32Rect value, int valueSize);
		
		[DllImport ("dwmapi.dll")]
		static extern int DwmIsCompositionEnabled (out bool enabled);
		
		[DllImport ("User32.dll")]
		static extern bool GetWindowRect (IntPtr hwnd, out Win32Rect rect);
		
		public static void SetImCursorLocation (Gtk.IMContext ctx, Gdk.Window clientWindow, Gdk.Rectangle cursor)
		{
			// work around GTK+ Bug 663096 - Windows IME position is wrong when Aero glass is enabled
			// https://bugzilla.gnome.org/show_bug.cgi?id=663096
			if (Platform.IsWindows && System.Environment.OSVersion.Version.Major >= 6) {
				bool enabled;
				if (DwmIsCompositionEnabled (out enabled) == 0 && enabled) {
					var hwnd = gdk_win32_drawable_get_handle (clientWindow.Toplevel.Handle);
					Win32Rect rect;
					// this module gets the WINVER=6 version of GetWindowRect, which returns the correct value
					if (GetWindowRect (hwnd, out rect)) {
						int x, y;
						clientWindow.Toplevel.GetPosition (out x, out y);
						cursor.X = cursor.X - x + rect.Left;
						cursor.Y = cursor.Y - y + rect.Top - cursor.Height;
					}
				}
			}
			ctx.CursorLocation = cursor;
		}
	}
	
	public struct KeyboardShortcut : IEquatable<KeyboardShortcut>
	{
		Gdk.Key key;
		Gdk.ModifierType mod;
		
		public KeyboardShortcut (Gdk.Key key, Gdk.ModifierType mod)
		{
			this.key = key;
			this.mod = mod;
		}
		
		public Gdk.Key Key {
			get { return key; }
		}
		
		public Gdk.ModifierType Modifier {
			get { return mod; }
		}
		
		public override bool Equals (object obj)
		{
			return obj is KeyboardShortcut && this.Equals ((KeyboardShortcut)obj);
		}
		
		public override int GetHashCode ()
		{
			//FIXME: we're only using a few bits of mod and mostly the lower bits of key - distribute it better
			return (int)key ^ (int)mod;
		}
		
		public bool Equals (KeyboardShortcut other)
		{
			return other.key == key && other.mod == mod;
		}
	}
}
