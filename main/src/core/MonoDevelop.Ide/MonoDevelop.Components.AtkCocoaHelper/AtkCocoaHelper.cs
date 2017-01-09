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
using CoreGraphics;
using Foundation;
using ObjCRuntime;
#endif

namespace MonoDevelop.Components.AtkCocoaHelper
{
	// AtkCocoaHelper wraps NSAccessibilityElement to set NSAccessibility properties that aren't supported by Atk
	public static class AtkCocoa
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

		public enum Roles
		{
			AXButton,
			AXCell,
			AXColumn,
			AXGroup,
			AXImage,
			AXMenuButton,
			AXRadioButton,
			AXRow,
			AXRuler,
			AXSplitGroup,
			AXSplitter,
			AXStaticText,
			AXTabGroup,
			AXTextArea
		};

		public enum SubRoles
		{
			AXCloseButton,
		};

		public struct Range
		{
			public int Location { get; set; }
			public int Length { get; set; }
		}
	}

	public class ActionDelegate
	{
		public AtkCocoa.Actions [] Actions { get; set; }

		Atk.Object owner;
		internal Atk.Object Owner {
			set {
				owner = value;

				if (owner.GetType () == typeof (Atk.NoOpObject)) {
					return;
				}

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

	// On anything other than Mac this is just a dummy class to prevent needing to have #ifdefs all over the main code
	public interface IAccessibilityElementProxy
	{
		event EventHandler PerformCancel;
		event EventHandler PerformConfirm;
		event EventHandler PerformDecrement;
		event EventHandler PerformDelete;
		event EventHandler PerformIncrement;
		event EventHandler PerformPick;
		event EventHandler PerformPress;
		event EventHandler PerformRaise;
		event EventHandler PerformShowAlternateUI;
		event EventHandler PerformShowDefaultUI;
		event EventHandler PerformShowPopupMenu;

		void SetRealParent (Gtk.Widget realParent);
		void SetFrameInRealParent (Gdk.Rectangle frame);
		void AddAccessibleChild (IAccessibilityElementProxy child);
		void SetAccessibilityRole (string role, string description = null);
		void SetAccessibilityRole (AtkCocoa.Roles role, string description = null);
		void SetAccessibilityValue (string value);
		void SetAccessibilityTitle (string title);
		void SetAccessibilityLabel (string label);
		void SetAccessibilityIdentifier (string identifier);
		void SetAccessibilityHelp (string help);
		void SetFrameInParent (Gdk.Rectangle rect);
	}

	public interface IAccessibilityNavigableStaticText
	{
		int NumberOfCharacters { get; }
		int InsertionPointLineNumber { get; }
		string Value { get; }

		// Returns frame in Gtk.Widget parent space.
		Gdk.Rectangle GetFrameForRange (AtkCocoa.Range range);
		int GetLineForIndex (int index);
		AtkCocoa.Range GetRangeForLine (int line);
		string GetStringForRange (AtkCocoa.Range range);
		AtkCocoa.Range GetRangeForIndex (int index);
		AtkCocoa.Range GetStyleRangeForIndex (int index);
		AtkCocoa.Range GetRangeForPosition (Gdk.Point position);
	}

	/*
	public abstract class AtkCellRendererProxy : Atk.Object
	{
		public AccessibilityElementProxy Accessible { get; private set; }
		protected AtkCellRendererProxy ()
		{
			Accessible = new AccessibilityElementProxy ();

			// Set the element as secret data on the Atk.Object so AtkCocoa can do something with it
			AtkCocoaHelper.SetNSAccessibilityElement (this, Accessible);
		}
	}
*/
}