﻿//
// NSViewResult.cs
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
#if MAC
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using AppKit;
using Foundation;

namespace MonoDevelop.Components.AutoTest.Results
{
	public class NSObjectResult : AppResult
	{
		NSObject ResultObject;

		public NSObjectResult (NSObject resultObject)
		{
			ResultObject = resultObject;
		}

		void AddAttribute (XmlElement childElement, string name, string value)
		{
			XmlDocument doc = childElement.OwnerDocument;
			XmlAttribute attr = doc.CreateAttribute (name);
			attr.Value = value;
			childElement.Attributes.Append (attr);
		}

		public override void ToXml (XmlElement element)
		{
			AddAttribute (element, "type", ResultObject.GetType ().ToString ());
			AddAttribute (element, "fulltype", ResultObject.GetType ().FullName);

			NSView view = ResultObject as NSView;
			if (view == null) {
				return;
			}

			if (view.Identifier != null) {
				AddAttribute (element, "name", view.Identifier);
			}

			AddAttribute (element, "visible", view.Hidden.ToString ());
			AddAttribute (element, "allocation", view.Frame.ToString ());
		}

		public override AppResult Marked (string mark)
		{
			if (ResultObject is NSView) {
				if (((NSView)ResultObject).Identifier == mark) {
					return this;
				}

				if (ResultObject.GetType ().FullName == mark) {
					return this;
				}
			}
			return null;
		}

		public override AppResult CheckType (Type desiredType)
		{
			if (ResultObject.GetType () == desiredType || ResultObject.GetType ().IsSubclassOf (desiredType)) {
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
			if (ResultObject is NSControl) {
				NSControl control = (NSControl)ResultObject;
				string value = control.StringValue;
				if (CheckForText (value, text, exact)) {
					return this;
				}
			}

			return null;
		}

		public override AppResult Model (string column)
		{
			return null;
		}

		object GetPropertyValue (string propertyName)
		{
			return AutoTestService.CurrentSession.UnsafeSync (delegate {
				PropertyInfo propertyInfo = ResultObject.GetType().GetProperty(propertyName);
				if (propertyInfo != null) {
					var propertyValue = propertyInfo.GetValue (ResultObject);
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
			return null;
		}

		public override bool Select ()
		{
			return false;
		}

		public override bool Click ()
		{
			NSControl control = ResultObject as NSControl;
			if (control == null) {
				return false;
			}

			control.PerformClick (null);
			return true;
		}

		NSEvent MakeEvent (string c, NSEventType type, double epochTime, nint winID)
		{
			return NSEvent.KeyEvent (type, CoreGraphics.CGPoint.Empty, 
									 (NSEventModifierMask) 0, epochTime, winID, 
									 NSGraphicsContext.CurrentContext, 
									 c, c, false, 0);
		}

		void RealTypeKey (char c)
		{
			// FIXME: Do we need to pass a real keyCode?
			double epochTime = (DateTime.UtcNow - new DateTime (1970, 1, 1)).TotalSeconds;
			nint winID = NSApplication.SharedApplication.MainWindow.WindowNumber;
			string s = c.ToString ();
			NSEvent ev = MakeEvent (s, NSEventType.KeyDown, epochTime, winID);
			NSApplication.SharedApplication.SendEvent (ev);

			ev = MakeEvent (s, NSEventType.KeyUp, epochTime, winID);
			NSApplication.SharedApplication.SendEvent (ev);
		}

		public override bool EnterText (string text)
		{
			NSControl control = ResultObject as NSControl;
			if (control == null) {
				return false;
			}

			control.Window.MakeFirstResponder (control);
			foreach (var c in text) {
				RealTypeKey (c);
			}

			return true;
		}

		public override bool TypeKey (char key, string state)
		{
			RealTypeKey (key);
			return true;
		}

		public override bool Toggle (bool active)
		{
			NSButton button = ResultObject as NSButton;
			if (button == null) {
				return false;
			}

			button.State = active ? NSCellStateValue.On : NSCellStateValue.Off;
			return true;
		}
	}
}

#endif