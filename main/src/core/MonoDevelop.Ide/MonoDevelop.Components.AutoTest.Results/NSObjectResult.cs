//
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
using System.Linq;
using System.Reflection;
using System.Xml;

using AppKit;
using Foundation;

using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Core;

namespace MonoDevelop.Components.AutoTest.Results
{
	public class NSObjectResult : AppResult
	{
		NSObject ResultObject;
		int index = -1;

		internal NSObjectResult (NSObject resultObject)
		{
			ResultObject = resultObject;
		}

		internal NSObjectResult (NSObject resultObject, int index)
		{
			ResultObject = resultObject;
			this.index = index;
		}

		public override string ToString ()
		{
			return string.Format ("NSObject: Type: {0}", ResultObject.GetType ().FullName);
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

			// In Cocoa the attribute is Hidden as opposed to Gtk's Visible.
			AddAttribute (element, "visible", (!view.Hidden).ToString ());
			AddAttribute (element, "allocation", view.Frame.ToString ());
		}

		public override string GetResultType  ()
		{
			return ResultObject.GetType ().FullName;
		}

		public override AppResult Marked (string mark)
		{
			if (CheckForText (ResultObject.GetType ().FullName, mark, true)) {
				return this;
			}

			if (ResultObject is NSView) {
				if (CheckForText (((NSView)ResultObject).Identifier, mark, true)) {
					return this;
				}
			}

			if (ResultObject is NSWindow) {
				if (CheckForText (((NSWindow)ResultObject).Title,  mark, true)) {
					return this;
				}
			}
			return null;
		}

		public override AppResult Selected ()
		{
			return null;
		}

		public override AppResult CheckType (Type desiredType)
		{
			if (ResultObject.GetType () == desiredType || ResultObject.GetType ().IsSubclassOf (desiredType)) {
				return this;
			}

			return null;
		}

		public override AppResult Text (string text, bool exact)
		{
			if (ResultObject is NSControl) {
				NSControl control = (NSControl)ResultObject;
				string value = control.StringValue;
				if (CheckForText (value, text, exact)) {
					return this;
				}

				if (ResultObject is NSButton) {
					var nsButton = (NSButton)ResultObject;
					if (CheckForText (nsButton.Title, text, exact)) {
						return this;
					}
				}
			}

			if(ResultObject is NSSegmentedControl){
				NSSegmentedControl control = (NSSegmentedControl)ResultObject;
				string value = control.GetLabel (this.index);
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
			if (ResultObject is NSSegmentedControl) {
				NSSegmentedControl control = (NSSegmentedControl)ResultObject;
				if (this.index >= 0 && propertyName == "Sensitive" || propertyName == "Visible") {
					return control.IsEnabled (this.index) == (bool)value ? this : null;
				}
			}
			return MatchProperty (propertyName, ResultObject, value);
		}

		public override List<AppResult> NextSiblings ()
		{
			return null;
		}

		public override ObjectProperties Properties ()
		{
			return GetProperties (ResultObject);
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

			using (var nsObj = new NSObject ())
				control.PerformClick (nsObj);
			return true;
		}

		public override bool Click (double x, double y)
		{
			return Click ();
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

		public override bool TypeKey (char key, string state = "")
		{
			RealTypeKey (key);
			return true;
		}

		public override bool TypeKey (string keyString, string state = "")
		{
			throw new NotImplementedException ();
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

		public override void Flash ()
		{
			
		}

		public override void SetProperty (string propertyName, object value)
		{
			SetProperty (ResultObject, propertyName, value);
		}

#region MacPlatform.MacIntegration.MainToolbar.SelectorView
		public override bool SetActiveConfiguration (string configurationName)
		{
			Type type = ResultObject.GetType ();
			PropertyInfo pinfo = type.GetProperty ("ConfigurationModel");
			if (pinfo == null) {
				return false;
			}

			IEnumerable<IConfigurationModel> model = (IEnumerable<IConfigurationModel>)pinfo.GetValue (ResultObject, null);
			var configuration = model.FirstOrDefault (c => c.DisplayString == configurationName);
			if (configuration == null) {
				return false;
			}

			pinfo = type.GetProperty ("ActiveConfiguration");
			if (pinfo == null) {
				return false;
			}

			pinfo.SetValue (ResultObject, configuration);
			return true;
		}

		public override bool SetActiveRuntime (string runtimeName)
		{
			Type type = ResultObject.GetType ();
			PropertyInfo pinfo = type.GetProperty ("RuntimeModel");
			if (pinfo == null) {
				return false;
			}

			IEnumerable<IRuntimeModel> model = (IEnumerable<IRuntimeModel>)pinfo.GetValue (ResultObject, null);

			var runtime = model.FirstOrDefault (r => r.GetMutableModel ().FullDisplayString == runtimeName);
			if (runtime == null) {
				return false;
			}

			pinfo = type.GetProperty ("ActiveRuntime");
			if (pinfo == null) {
				return false;
			}

			pinfo.SetValue (ResultObject, runtime);
			return true;
		}
#endregion
	}
}

#endif
