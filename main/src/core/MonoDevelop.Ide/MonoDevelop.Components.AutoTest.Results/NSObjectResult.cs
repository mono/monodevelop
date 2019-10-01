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
			return string.Format ("NSObject: Type: {0} {1}", ResultObject.GetType ().FullName, this.index);
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

		public override AppResult CheckType (Type desiredType)
		{
			if (desiredType.IsInstanceOfType (ResultObject)) {
				return this;
			}

			return null;
		}

		protected string[] GetPossibleNSCellValues (NSCell cell) =>
		new [] { cell.StringValue, cell.Title, cell.AccessibilityLabel, cell.Identifier, cell.AccessibilityTitle };
		public override AppResult Text (string text, bool exact)
		{
			if (ResultObject is NSTableView) {
				var control = (NSTableView)ResultObject;
				for (int i = 0; i < control.ColumnCount;i ++)
				{
					var cell = control.GetCell (i, index);
					var possValues = GetPossibleNSCellValues (cell);
					LoggingService.LogInfo ($"Possible values for NSTableView with column {i} and row {index} -> "+string.Join (", ", possValues));
					if (possValues.Any (haystack => CheckForText (text, haystack, exact)))
						return this;
				}
			}
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

		public override List<AppResult> Children (bool recursive = true)
		{
			if (ResultObject is NSTableView) {
				var control = (NSTableView)ResultObject;
				var children = new List<AppResult> ();
				for (int i = 0; i < control.RowCount; i++) {
					LoggingService.LogInfo ($"Found row {i} of NSTableView -  {control.Identifier} - {control.AccessibilityIdentifier}");
					children.Add (DisposeWithResult (new NSObjectResult (control, i)));
				}
				return children;
			}
			return base.Children(recursive);
		}

		public override ObjectProperties Properties ()
		{
			return GetProperties (ResultObject);
		}

		public override bool Select ()
		{
			if (ResultObject is NSTableView) {
				var control = (NSTableView)ResultObject;
				LoggingService.LogInfo($"Found NSTableView with index: {index}");
				if (index >= 0)
				{
					LoggingService.LogInfo ($"Selecting row '{index}' of ");
					control.SelectRow(index, true);
					control.PerformClick(0, index);
				}
				return true;
			}

			return ResultObject is NSView obj && obj.Window != null && obj.Window.MakeFirstResponder (obj);
		}

		public override AppResult Selected ()
		{
			if (ResultObject is NSTableView) {
				var control = (NSTableView)ResultObject;
				if(control.SelectedRow == index || control.SelectedRows.Contains((nuint)index))
					return this;
			}
			return null;
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
			if (ResultObject is NSView view && view.Window != null && view.Window.MakeFirstResponder(view)) {
				foreach (var c in text) {
					RealTypeKey (c);
				}
				return true;
			}

			return false;
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
			LoggingService.LogDebug ($"Set Active configuration with name as '{configurationName}'");
			Type type = ResultObject.GetType ();
			PropertyInfo pinfo = type.GetProperty ("ConfigurationModel");
			if (pinfo == null) {
				return false;
			}

			IEnumerable<IConfigurationModel> model = (IEnumerable<IConfigurationModel>)pinfo.GetValue (ResultObject, null);
			LoggingService.LogDebug (string.Format ("Found configurations: {0}",
				string.Join(", ", model.Select(m => $"['{m.OriginalId}' '{m.DisplayString}']"))));
			var configuration = model.FirstOrDefault (
				c => c.DisplayString.Contains(configurationName) || c.OriginalId.Contains(configurationName));
			if (configuration == null) {
				return false;
			}

			pinfo = type.GetProperty ("ActiveConfiguration");
			if (pinfo == null) {
				return false;
			}

			LoggingService.LogDebug ($"Setting the active configuration as: '{configuration.OriginalId}' '{configuration.DisplayString}'");
			pinfo.SetValue (ResultObject, configuration);

			var activeConfiguration = (IConfigurationModel)pinfo.GetValue (ResultObject);
			if (activeConfiguration != null) {
				LoggingService.LogDebug ($"Checking active configuration is actually set: '{configuration.OriginalId}' '{configuration.DisplayString}'");
				if (configuration.OriginalId == activeConfiguration.OriginalId)
					return true;
			}
			return true;
		}

		public override bool SetActiveRuntime (string runtimeName)
		{
			LoggingService.LogDebug ($"Set Active runtime with name/ID as '{runtimeName}'");
			Type type = ResultObject.GetType ();
			PropertyInfo pinfo = type.GetProperty ("RuntimeModel");
			if (pinfo == null) {
				LoggingService.LogDebug ($"Could not find 'RuntimeModel' property on {type}");
				return false;
			}

			var pObject = pinfo.GetValue (ResultObject, null);
			LoggingService.LogDebug ($"'RuntimeModel' property on '{type}' is '{pObject}' and is of type '{pinfo.PropertyType}'");
			var topRunTimeModels = (IEnumerable<IRuntimeModel>)pObject;
			var model = AllRuntimes (topRunTimeModels);
			model = model.Where (x => !x.IsSeparator);

			var runtime = model.FirstOrDefault (r => {
				var mutableModel = r.GetMutableModel ();
				LoggingService.LogDebug ($"[IRuntimeModel.IRuntimeMutableModel] FullDisplayString: '{mutableModel.FullDisplayString}' | DisplayString: '{mutableModel.DisplayString}'");

				if (string.IsNullOrEmpty (runtimeName))
					return false;

				if (!string.IsNullOrWhiteSpace(mutableModel.FullDisplayString) && mutableModel.FullDisplayString.Contains (runtimeName))
					return true;
				if (!string.IsNullOrWhiteSpace(mutableModel.DisplayString) && mutableModel.DisplayString.Contains (runtimeName))
					return true;

				var execTargetPInfo = r.GetType().GetProperty ("ExecutionTarget");
				if(execTargetPInfo != null) {
					if (execTargetPInfo.GetValue (r) is Core.Execution.ExecutionTarget execTarget) {
						LoggingService.LogDebug ($"[IRuntimeModel.ExecutionTarget] Id: '{execTarget.Id}' | FullName: '{execTarget.FullName}'");
						if (execTarget.Id != null && execTarget.Id.Contains (runtimeName))
							return true;
						if (execTarget.FullName != null && execTarget.FullName.Contains (runtimeName))
							return true;
					}
				}
				return false;
			});
			if (runtime == null) {
				LoggingService.LogDebug ($"Did not find an IRuntimeModel for '{runtimeName}'");
				return false;
			}

			pinfo = type.GetProperty ("ActiveRuntime");
			if (pinfo == null) {
				return false;
			}

			LoggingService.LogDebug ($"Setting ActiveRuntime: Id: '{runtime.GetMutableModel ().FullDisplayString}'");
			pinfo.SetValue (ResultObject, runtime);

			// Now we need to make sure that the ActiveRuntime is actually set
			var activeRuntime = (IRuntimeModel)pinfo.GetValue (ResultObject);
			if(activeRuntime != null) {
				LoggingService.LogDebug ($"Checking ActiveRuntime: Id: '{activeRuntime.GetMutableModel().FullDisplayString}'");
				if (activeRuntime.GetMutableModel ().DisplayString == runtime.GetMutableModel ().DisplayString)
					return true;
			}
			return false;
		}

		IEnumerable<IRuntimeModel> AllRuntimes (IEnumerable<IRuntimeModel> runtimes)
		{
			foreach (var runtime in runtimes) {
				yield return runtime;
				foreach (var childRuntime in AllRuntimes (runtime.Children))
					yield return childRuntime;
			}
		}

		#endregion

		protected override void Dispose (bool disposing)
		{
			ResultObject = null;
			base.Dispose (disposing);
		}
	}
}

#endif
