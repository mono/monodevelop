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

		public override AppResult Marked (string mark)
		{
			return (AppResult) AutoTestService.CurrentSession.UnsafeSync (delegate {
				if (resultWidget.Name != null && resultWidget.Name.IndexOf (mark) > -1) {
					return this;
				}

				if (resultWidget.GetType ().FullName == mark) {
					return this;
				}

				return null;
			});
		}

		public override AppResult CheckType (Type desiredType)
		{
			if (resultWidget.GetType () == desiredType || resultWidget.GetType ().IsSubclassOf (desiredType)) {
				return this;
			}

			return null;
		}

		public override AppResult Text (string text)
		{
			// Entries and Labels have Text, Buttons have Label.
			// FIXME: Are there other property names?

			return (AppResult) AutoTestService.CurrentSession.UnsafeSync (delegate {
				// Look for a Text property on the widget.
				PropertyInfo pinfo = resultWidget.GetType ().GetProperty ("Text");
				if (pinfo != null) {
					string propText = (string)pinfo.GetValue (resultWidget);
					if (propText == text) {
						return this;
					}
				}

				pinfo = resultWidget.GetType ().GetProperty ("Label");
				if (pinfo != null) {
					string propText = (string)pinfo.GetValue (resultWidget);
					if (propText == text) {
						return this;
					}
				}

				return null;
			});
		}

		public override AppResult Model (string column)
		{
			TreeView tv = resultWidget as TreeView;
			if (tv == null) {
				return null;
			}

			TreeModel model = (TreeModel) AutoTestService.CurrentSession.UnsafeSync (() => tv.Model);
			if (model == null) {
				return null;
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

			//AutoTestService.CurrentSession.SessionDebug.SendDebugMessage ("{0} has {1} column names: {2}", resultWidget, attr.ColumnNames.Length, string.Join (", ", attr.ColumnNames));
			int columnNumber = Array.IndexOf (attr.ColumnNames, column);
			if (columnNumber == -1) {
				return null;
			}

			//AutoTestService.CurrentSession.SessionDebug.SendDebugMessage ("{0} has {1} at column {2}", resultWidget, column, columnNumber);
			return new GtkTreeModelResult (tv, model, columnNumber);
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

		public override bool Select ()
		{
			if (resultWidget.CanFocus == false) {
				return false;
			}

			return (bool) AutoTestService.CurrentSession.UnsafeSync (delegate {
				resultWidget.GrabFocus ();
				return true;
			});
		}

		public override bool Click ()
		{
			Button button = resultWidget as Button;
			if (button == null) {
				return false;
			}

			return (bool) AutoTestService.CurrentSession.UnsafeSync (delegate {
				button.Click ();
				return true;
			});
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
			AutoTestService.CurrentSession.UnsafeSync (delegate {
				SendKeyEvent (resultWidget, (uint)key, state, Gdk.EventType.KeyPress, null);
				return null;
			});
			Thread.Sleep (15);
			AutoTestService.CurrentSession.UnsafeSync (delegate {
				SendKeyEvent (resultWidget, (uint)key, state, Gdk.EventType.KeyRelease, null);
				return null;
			});
			Thread.Sleep (10);
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

		public override bool Toggle (bool active)
		{
			ToggleButton toggleButton = resultWidget as ToggleButton;
			if (toggleButton == null) {
				return false;
			}

			return (bool) AutoTestService.CurrentSession.UnsafeSync (delegate {
				toggleButton.Active = active;
				return true;
			});
		}
	}
}

