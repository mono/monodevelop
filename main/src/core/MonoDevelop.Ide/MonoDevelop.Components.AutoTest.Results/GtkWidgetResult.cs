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
using System.Threading;
using Gtk;

namespace MonoDevelop.Components.AutoTest.Results
{
	public class GtkWidgetResult : AppResult
	{
		Widget resultWidget;

		public GtkWidgetResult (Widget widget)
		{
			resultWidget = widget;
		}

		public override string ToString ()
		{
			return String.Format ("{0} - {1} - {2} - {3}, - {4}", resultWidget, resultWidget.Allocation, resultWidget.Name, resultWidget.GetType ().FullName, resultWidget.Toplevel.Name);
		}

		public override AppResult Marked (string mark)
		{
			if (resultWidget.Name != null && resultWidget.Name.IndexOf (mark) > -1) {
				return this;
			}

			if (resultWidget.GetType ().FullName == mark) {
				return this;
			}

			return null;
		}

		public override AppResult CheckType (Type desiredType)
		{
			if (resultWidget.GetType () == desiredType || resultWidget.GetType ().IsSubclassOf (desiredType)) {
				return this;
			}

			return null;
		}

		bool CheckForText (string haystack, string needle, bool exact)
		{
			if (exact) {
				return haystack == needle;
			} else {
				return (haystack.IndexOf (needle) > -1);
			}
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
				if (CheckForText (propText, text, exact)) {
					return this;
				}
			}

			return null;
		}

		TreeModel ModelFromWidget (Widget widget)
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
				return new GtkTreeModelResult (resultWidget, model, 0);
			}

			// Check if the class has the SemanticModelAttribute
			Type modelType = model.GetType ();
			SemanticModelAttribute attr = modelType.GetCustomAttribute<SemanticModelAttribute> ();

			if (attr == null) {
				// Check if the instance has the attributes
				AttributeCollection attrs = TypeDescriptor.GetAttributes (model);
				attr = (SemanticModelAttribute)attrs [typeof(SemanticModelAttribute)];

				if (attr == null) {
					return null;
				}
			}

			int columnNumber = Array.IndexOf (attr.ColumnNames, column);
			if (columnNumber == -1) {
				return null;
			}

			return new GtkTreeModelResult (resultWidget, model, columnNumber);
		}

		object GetPropertyValue (string propertyName)
		{
			return AutoTestService.CurrentSession.UnsafeSync (delegate {
				PropertyInfo propertyInfo = resultWidget.GetType().GetProperty(propertyName);
				if (propertyInfo != null) {
					var propertyValue = propertyInfo.GetValue (resultWidget);
					if (propertyValue != null) {
						return propertyValue;
					}
				}

				return null;
			});
		}

		public override AppResult Property (string propertyName, object value)
		{
			return (GetPropertyValue (propertyName) == value) ? this : null;			
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

				siblingResults.Add (new GtkWidgetResult (child));
			}

			return siblingResults;
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
			if (button == null) {
				return false;
			}

			button.Click ();
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

		void RealTypeKey (Gdk.Key key, Gdk.ModifierType state)
		{
			SendKeyEvent (resultWidget, (uint)key, state, Gdk.EventType.KeyPress, null);
			SendKeyEvent (resultWidget, (uint)key, state, Gdk.EventType.KeyRelease, null);
		}

		public override bool TypeKey (char key, string state)
		{
			Gdk.Key realKey;

			if (key == '\n')
				realKey = Gdk.Key.Return;
			else
				realKey = (Gdk.Key) Gdk.Global.UnicodeToKeyval ((uint)key);

			// FIXME: Parse @state into a Gdk.ModifierType
			RealTypeKey (realKey, Gdk.ModifierType.None);

			return true;
		}

		public override bool EnterText (string text)
		{
			foreach (var c in text) {
				TypeKey (c, null);
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
	}
}

