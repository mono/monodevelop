//
// AtkCocoaHelper.cs
//
// Author:
//       Iain Holmes <iain@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc
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
using System.Collections.Generic;
using System.Runtime.InteropServices;

#if MAC
using AppKit;
using Foundation;
using ObjCRuntime;
#endif

namespace MonoDevelop.Components
{
	// AtkCocoaHelper wraps NSAccessibilityElement to set NSAccessibility properties that aren't supported by Atk
	public static class AtkCocoaHelper
	{
		public enum Actions
		{
			AXCancel,
			AXConfirm,
			AXDecrement,
			AXDelete,
			AXIncrement,
			AXPick,
			AXPress,
			AXRelease,
			AXShowAlternateUI,
			AXShowDefaultUI,
			AXShowMenu
		};

#if MAC
		static NSAccessibilityElement GetNSAccessibilityElement (Atk.Object o)
		{
			IntPtr handle = GtkWorkarounds.GetData (o, "xamarin-private-atkcocoa-nsaccessibility");

			return Runtime.GetNSObject<NSAccessibilityElement> (handle, false);
		}
#endif

		public static void SetAccessibilityLabel (this Atk.Object o, string label)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			nsa.AccessibilityLabel = label;
#endif
		}

		public static void SetAccessibilityShouldIgnore (this Atk.Object o, bool ignore)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			nsa.AccessibilityElement = !ignore;
#endif
		}

		public static void SetAccessibilityTitle (this Atk.Object o, string title)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			nsa.AccessibilityTitle = title;
#endif
		}

		public static void SetAccessibilityValue (this Atk.Object o, string stringValue)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			nsa.AccessibilityValue = new NSString (stringValue);
#endif
		}

		public static void SetAccessibilityURL (this Atk.Object o, string url)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			nsa.AccessibilityUrl = new NSUrl (url);
#endif
		}

		public static void SetAccessibilityRole (this Atk.Object o, string role, string description = null)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			nsa.AccessibilityRole = role;

			if (!string.IsNullOrEmpty (description)) {
				nsa.AccessibilityRoleDescription = description;
			}
#endif
		}

		public static void SetAccessibilitySubRole (this Atk.Object o, string subrole)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			nsa.AccessibilitySubrole = subrole;
#endif
		}

		public static void SetAccessibilityTitleUIElement (this Atk.Object o, Atk.Object title)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			var titleNsa = GetNSAccessibilityElement (title);

			nsa.AccessibilityTitleUIElement = titleNsa;
#endif
		}

		public static void SetAccessibilityAlternateUIVisible (this Atk.Object o, bool visible)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			nsa.AccessibilityAlternateUIVisible = visible;
#endif
		}

		public static void SetAccessibilityTitleFor (this Atk.Object o, params Atk.Object [] objects)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			NSObject [] titleElements = new NSObject [objects.Length];
			int idx = 0;

			foreach (var obj in objects) {
				var nsao = GetNSAccessibilityElement (obj);
				titleElements [idx] = nsao;
				idx++;
			}

			nsa.AccessibilityServesAsTitleForUIElements = titleElements;
#endif
		}

		public static void AccessibilityAddElementToTitle (this Atk.Object title, Atk.Object o)
		{
#if MAC
			var titleNsa = GetNSAccessibilityElement (title);
			var nsa = GetNSAccessibilityElement (o);

			NSObject [] oldElements = titleNsa.AccessibilityServesAsTitleForUIElements;
			int length = oldElements != null ? oldElements.Length : 0;

			if (oldElements != null && oldElements.IndexOf (nsa) != -1) {
				return;
			}

			NSObject [] titleElements = new NSObject [length + 1];
			if (oldElements != null) {
				oldElements.CopyTo (titleElements, 0);
			}
			titleElements [length] = nsa;
#endif
		}

		public static void AccessibilityRemoveElementFromTitle (this Atk.Object title, Atk.Object o)
		{
#if MAC
			var titleNsa = GetNSAccessibilityElement (title);
			var nsa = GetNSAccessibilityElement (o);

			if (titleNsa.AccessibilityServesAsTitleForUIElements == null) {
				return;
			}

			List<NSObject> oldElements = new List<NSObject> (titleNsa.AccessibilityServesAsTitleForUIElements);
			oldElements.Remove (nsa);

			titleNsa.AccessibilityServesAsTitleForUIElements = oldElements.ToArray ();
#endif
		}

		public class ActionDelegate
		{
			public Actions [] Actions { get; set; }

			Atk.Object owner;
			internal Atk.Object Owner {
				set {
					owner = value;

					var signal = GLib.Signal.Lookup (owner, "request-actions", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (RequestActionsHandler));

					signal = GLib.Signal.Lookup (owner, "perform-cancel", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformCancelHandler));
					signal = GLib.Signal.Lookup (owner, "perform-confirm", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformConfirmHandler));
					signal = GLib.Signal.Lookup (owner, "perform-decrement", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformDecrementHandler));
					signal = GLib.Signal.Lookup (owner, "perform-delete", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformDeleteHandler));
					signal = GLib.Signal.Lookup (owner, "perform-increment", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformIncrementHandler));
					signal = GLib.Signal.Lookup (owner, "perform-pick", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformPickHandler));
					signal = GLib.Signal.Lookup (owner, "perform-press", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformPressHandler));
					signal = GLib.Signal.Lookup (owner, "perform-raise", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformRaiseHandler));
					signal = GLib.Signal.Lookup (owner, "perform-show-alternate-ui", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformShowAlternateUIHandler));
					signal = GLib.Signal.Lookup (owner, "perform-show-default-ui", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformShowDefaultUIHandler));
					signal = GLib.Signal.Lookup (owner, "perform-show-menu", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformShowMenuHandler));
				}
			}

			void RequestActionsHandler (object sender, GLib.SignalArgs args)
			{
				// +1 so we can add a NULL to terminate the array
				int actionCount = Actions.Length + 1;
				IntPtr intPtr = Marshal.AllocHGlobal (actionCount * Marshal.SizeOf<IntPtr> ());
				IntPtr [] actions = new IntPtr [actionCount];

				int i = 0;
				foreach (var action in Actions) {
					actions [i] = Marshal.StringToHGlobalAnsi (action.ToString ());
					i++;
				}

				// Terminator
				actions [i] = IntPtr.Zero;

				Marshal.Copy (actions, 0, intPtr, actionCount);

				args.RetVal = intPtr;
			}

			void PerformCancelHandler (object sender, GLib.SignalArgs args)
			{
				PerformCancel?.Invoke (this, args);
			}

			void PerformConfirmHandler (object sender, GLib.SignalArgs args)
			{
				PerformConfirm?.Invoke (this, args);
			}

			void PerformDecrementHandler (object sender, GLib.SignalArgs args)
			{
				PerformDecrement?.Invoke (this, args);
			}

			void PerformDeleteHandler (object sender, GLib.SignalArgs args)
			{
				PerformDelete?.Invoke (this, args);
			}

			void PerformIncrementHandler (object sender, GLib.SignalArgs args)
			{
				PerformIncrement?.Invoke (this, args);
			}

			void PerformPickHandler (object sender, GLib.SignalArgs args)
			{
				PerformPick?.Invoke (this, args);
			}

			void PerformPressHandler (object sender, GLib.SignalArgs args)
			{
				PerformPress?.Invoke (this, args);
			}

			void PerformRaiseHandler (object sender, GLib.SignalArgs args)
			{
				PerformRaise?.Invoke (this, args);
			}

			void PerformShowAlternateUIHandler (object sender, GLib.SignalArgs args)
			{
				PerformShowAlternateUI?.Invoke (this, args);
			}

			void PerformShowDefaultUIHandler (object sender, GLib.SignalArgs args)
			{
				PerformShowDefaultUI?.Invoke (this, args);
			}

			void PerformShowMenuHandler (object sender, GLib.SignalArgs args)
			{
				PerformShowMenu?.Invoke (this, args);
			}

			public event EventHandler PerformCancel;
			public event EventHandler PerformConfirm;
			public event EventHandler PerformDecrement;
			public event EventHandler PerformDelete;
			public event EventHandler PerformIncrement;
			public event EventHandler PerformPick;
			public event EventHandler PerformPress;
			public event EventHandler PerformRaise;
			public event EventHandler PerformShowAlternateUI;
			public event EventHandler PerformShowDefaultUI;
			public event EventHandler PerformShowMenu;
		}

		public static void SetActionDelegate (this Atk.Object o, ActionDelegate ad)
		{
			ad.Owner = o;
		}
	}
}

