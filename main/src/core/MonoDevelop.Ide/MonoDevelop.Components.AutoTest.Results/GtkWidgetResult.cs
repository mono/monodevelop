//
// GtkWidgetResult.cs
//
// Author:
//       iain holmes <iain@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc
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
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;
using Gtk;

namespace MonoDevelop.Components.AutoTest.Results
{
	public class GtkWidgetResult : AppResult
	{
		Widget resultWidget;

		internal GtkWidgetResult (Widget widget)
		{
			resultWidget = widget;
		}

		public override string ToString ()
		{
			return String.Format ("{0} - {1} - {2} - {3}, - {4}", resultWidget, resultWidget.Allocation, resultWidget.Name, resultWidget.GetType ().FullName, resultWidget.Toplevel.Name);
		}

		public override void ToXml (XmlElement element)
		{
			AddAttribute (element, "type", resultWidget.GetType ().ToString ());
			AddAttribute (element, "fulltype", resultWidget.GetType ().FullName);

			if (resultWidget.Name != null) {
				AddAttribute (element, "name", resultWidget.Name);
			}

			AddAttribute (element, "visible", resultWidget.Visible.ToString ());
			AddAttribute (element, "sensitive", resultWidget.Sensitive.ToString ());
			AddAttribute (element, "allocation", resultWidget.Allocation.ToString ());
		}

		public override string GetResultType  ()
		{
			return resultWidget.GetType ().FullName;
		}

		public override AppResult Marked (string mark)
		{
			if (resultWidget.Name != null && resultWidget.Name.IndexOf (mark) > -1) {
				return this;
			}

			if (resultWidget.GetType ().FullName == mark) {
				return this;
			}

			var window = resultWidget as Gtk.Window;
			if (window != null) {
				if (window.Title != null && window.Title.IndexOf (mark) > -1) {
					return this;
				}
			}

			Button button = resultWidget as Button;
			if (button != null) {
				if (button.Label != null && button.Label.IndexOf (mark) > -1) {
					return this;
				}
			}

			return null;
		}

		public override AppResult Selected ()
		{
			return resultWidget.HasFocus ? this : null;
		}

		public override AppResult CheckType (Type desiredType)
		{
			if (resultWidget.GetType () == desiredType || resultWidget.GetType ().IsSubclassOf (desiredType)) {
				return desiredType == typeof(Notebook) ? new GtkNotebookResult (resultWidget) : this;
			}

			return null;
		}

		public override AppResult Text (string text, bool exact)
		{
			// Entries and Labels have Text, Buttons have Label.
			// FIXME: Are there other property names?

			// Check for the combobox first and try to use the active text.
			// If the active text fails then
			ComboBox cb = resultWidget as ComboBox;
			if (cb != null) {
				string activeText = cb.ActiveText;
				if (activeText == null) {
					return null;
				}

				return CheckForText (activeText, text, exact) ? this : null;
			}

			// Look for a Text property on the widget.
			PropertyInfo pinfo = resultWidget.GetType ().GetProperty ("Text");
			if (pinfo != null) {
				string propText = (string)pinfo.GetValue (resultWidget);
				if (CheckForText (propText, text, exact)) {
					return this;
				}
			}

			pinfo = resultWidget.GetType ().GetProperty ("Label");
			if (pinfo != null) {
				string propText = (string)pinfo.GetValue (resultWidget);

				Button button = resultWidget as Button;
				// If resultWidget is a button then the label may not be the actual label that is displayed
				// but rather a stock ID. So we may need to translate it
				if (button != null && button.UseStock && propText != null) {
					StockItem item = Stock.Lookup (propText);
					propText = item.Label;
				}

				if (button != null && button.UseUnderline) {
					int indexOfUnderline = propText.IndexOf ("_");
					if (indexOfUnderline > -1) {
						propText = propText.Remove (indexOfUnderline, 1);
					}
				}

				if (CheckForText (propText, text, exact)) {
					return this;
				}
			}

			return null;
		}

		protected TreeModel ModelFromWidget (Widget widget)
		{
			TreeView tv = widget as TreeView;
			if (tv != null) {
				return tv.Model;
			}

			ComboBox cb = widget as ComboBox;
			if (cb == null) {
				return null;
			}

			return cb.Model;
		}

		public override AppResult Model (string column)
		{
			TreeModel model = ModelFromWidget (resultWidget);
			if (model == null) {
				return null;
			}

			if (column == null) {
				return new GtkTreeModelResult (resultWidget, model, 0) { SourceQuery = this.SourceQuery };
			}

			// Check if the class has the SemanticModelAttribute
			var columnNumber = GetColumnNumber (column, model);
			return columnNumber == -1 ? null : new GtkTreeModelResult (resultWidget, model, columnNumber) { SourceQuery = this.SourceQuery };
		}

		protected int GetColumnNumber (string column, TreeModel model)
		{
			Type modelType = model.GetType ();
			SemanticModelAttribute attr = modelType.GetCustomAttribute<SemanticModelAttribute> ();
			if (attr == null) {
				// Check if the instance has the attributes
				AttributeCollection attrs = TypeDescriptor.GetAttributes (model);
				attr = (SemanticModelAttribute)attrs [typeof(SemanticModelAttribute)];
				if (attr == null) {
					return -1;
				}
			}
			return Array.IndexOf (attr.ColumnNames, column);
		}

		public override AppResult Property (string propertyName, object value)
		{
			return MatchProperty (propertyName, resultWidget, value);
		}

		public override List<AppResult> NextSiblings ()
		{
			Widget parent = resultWidget.Parent;
			Gtk.Container container = parent as Gtk.Container;

			// This really shouldn't happen
			if (container == null) {
				return null;
			}

			bool foundSelf = false;
			List<AppResult> siblingResults = new List<AppResult> ();
			foreach (Widget child in container.Children) {
				if (child == resultWidget) {
					foundSelf = true;
					continue;
				}

				if (!foundSelf) {
					continue;
				}

				siblingResults.Add (new GtkWidgetResult (child) { SourceQuery = this.SourceQuery });
			}

			return siblingResults;
		}

		public override ObjectProperties Properties ()
		{
			return GetProperties (resultWidget);
		}

		public override bool Select ()
		{
			if (resultWidget.CanFocus == false) {
				return false;
			}

			resultWidget.GrabFocus ();
			return true;
		}

		public override bool Click ()
		{
			Button button = resultWidget as Button;
			if (button != null) {
				button.Click ();
				return true;
			}
			Label lbl = resultWidget as Label;
			if(lbl != null)
			{
				GLib.Signal.Emit (lbl, "activate-link", new object[]{});
				return true;
			}
			GLib.Signal.Emit (resultWidget, "button-press-event", new object [] { });
			GLib.Signal.Emit (resultWidget, "button-release-event", new object [] { });

			return true;
		}

		void SendButtonEvent (Widget target, Gdk.EventType eventType, double x, double y, Gdk.ModifierType state, uint button)
		{
			Gdk.Window win = target.GdkWindow;

			int rx, ry;
			win.GetRootOrigin (out rx, out ry);

			var nativeEvent = new NativeEventButtonStruct {
				type = eventType,
				send_event = 1,
				window = win.Handle,
				state = (uint)state,
				button = button,
				x = x,
				y = y,
				axes = IntPtr.Zero,
				device = IntPtr.Zero,
				time = Global.CurrentEventTime,
				x_root = x + rx,
				y_root = y + ry
			};

			IntPtr ptr = GLib.Marshaller.StructureToPtrAlloc (nativeEvent);
			try {
				Gdk.EventHelper.Put (new Gdk.EventButton (ptr));
			} finally {
				Marshal.FreeHGlobal (ptr);
			}
		}

		public override bool Click (double x, double y)
		{
			SendButtonEvent (resultWidget, Gdk.EventType.ButtonPress, x, y, 0, 1);
			SendButtonEvent (resultWidget, Gdk.EventType.ButtonRelease, x, y, 0, 1);

			return true;
		}

		void SendKeyEvent (Gtk.Widget target, uint keyval, Gdk.ModifierType state, Gdk.EventType eventType, string subWindow)
		{
			Gdk.KeymapKey[] keyms = Gdk.Keymap.Default.GetEntriesForKeyval (keyval);
			if (keyms.Length == 0)
				throw new Exception ("Keyval not found");

			Gdk.Window win = target.GdkWindow;

			// FIXME: Do we need subwindow for anything?
			/*
			if (subWindow == null) {
				win = target.GdkWindow;
			} else {
				win = (Gdk.Window)GetValue (target, target.GetType (), subWindow);
			}
			*/

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

		internal void RealTypeKey (Gdk.Key key, Gdk.ModifierType state)
		{
			SendKeyEvent (resultWidget, (uint)key, state, Gdk.EventType.KeyPress, null);
			SendKeyEvent (resultWidget, (uint)key, state, Gdk.EventType.KeyRelease, null);
		}

		Gdk.ModifierType ParseModifier (string modifierString)
		{
			string[] modifiers = modifierString.Split ('|');
			Gdk.ModifierType modifier = Gdk.ModifierType.None;

			foreach (var m in modifiers) {
				switch (m) {
				case "Shift":
					modifier |= Gdk.ModifierType.ShiftMask;
					break;

				case "Lock":
					modifier |= Gdk.ModifierType.LockMask;
					break;

				case "Control":
					modifier |= Gdk.ModifierType.ControlMask;
					break;

				case "Mod1":
					modifier |= Gdk.ModifierType.Mod1Mask;
					break;

				case "Mod2":
					modifier |= Gdk.ModifierType.Mod2Mask;
					break;

				case "Mod3":
					modifier |= Gdk.ModifierType.Mod3Mask;
					break;

				case "Mod4":
					modifier |= Gdk.ModifierType.Mod4Mask;
					break;

				case "Mod5":
					modifier |= Gdk.ModifierType.Mod5Mask;
					break;

				case "Super":
					modifier |= Gdk.ModifierType.SuperMask;
					break;

				case "Hyper":
					modifier |= Gdk.ModifierType.HyperMask;
					break;

				case "Meta":
					modifier |= Gdk.ModifierType.MetaMask;
					break;

				default:
					modifier |= Gdk.ModifierType.None;
					break;
				}
			}

			return modifier;
		}

		public override bool TypeKey (char key, string state = "")
		{
			Gdk.Key realKey;

			if (key == '\n')
				realKey = Gdk.Key.Return;
			else
				realKey = (Gdk.Key) Gdk.Global.UnicodeToKeyval ((uint)key);

			RealTypeKey (realKey, ParseModifier (state));

			return true;
		}

		Gdk.Key ParseKeyString (string keyString)
		{
			switch (keyString) {
			case "ESC":
				return Gdk.Key.Escape;

			case "UP":
				return Gdk.Key.Up;

			case "DOWN":
				return Gdk.Key.Down;

			case "LEFT":
				return Gdk.Key.Left;

			case "RIGHT":
				return Gdk.Key.Right;

			case "RETURN":
				return Gdk.Key.Return;

			case "TAB":
				return Gdk.Key.Tab;

			case "BKSP":
				return Gdk.Key.BackSpace;

			case "DELETE":
				return Gdk.Key.Delete;

			default:
				throw new Exception ("Unknown keystring: " + keyString);
			}
		}

		public override bool TypeKey (string keyString, string state = "")
		{
			Gdk.Key realKey = ParseKeyString (keyString);
			RealTypeKey (realKey, ParseModifier (state));
			return true;
		}

		public override bool EnterText (string text)
		{
			foreach (var c in text) {
				TypeKey (c);
			}

			return true;
		}

		public override bool Toggle (bool active)
		{
			ToggleButton toggleButton = resultWidget as ToggleButton;
			if (toggleButton == null) {
				return false;
			}

			toggleButton.Active = active;
			return true;
		}

		bool flashState;

		void OnFlashWidget (object o, ExposeEventArgs args)
		{
			flashState = !flashState;

			if (flashState) {
				return;
			}

			using (var cr = Gdk.CairoHelper.Create (resultWidget.GdkWindow)) {
				cr.SetSourceRGB (1.0, 0.0, 0.0);

				Gdk.Rectangle allocation = resultWidget.Allocation;
				Gdk.CairoHelper.Rectangle (cr, allocation);
				cr.Stroke ();
			}
		}

		public override void Flash ()
		{
			int flashCount = 10;

			flashState = true;
			resultWidget.ExposeEvent += OnFlashWidget;

			GLib.Timeout.Add (1000, () => {
				resultWidget.QueueDraw ();
				flashCount--;

				if (flashCount == 0) {
					resultWidget.ExposeEvent -= OnFlashWidget;
					return false;
				}
				return true;
			});
		}

		public override void SetProperty (string propertyName, object value)
		{
			SetProperty (resultWidget, propertyName, value);
		}
	}
}

