//
// AppResult.cs
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
using System.Xml;
using System.Reflection;
using System.Linq;
using System.Collections.ObjectModel;
using MonoDevelop.Components.AutoTest.Results;

namespace MonoDevelop.Components.AutoTest
{
	public abstract class AppResult : MarshalByRefObject
	{
		//public Gtk.Widget ResultWidget { get; private set; }

		public AppResult ParentNode { get; set; }
		public AppResult FirstChild { get; set; }
		public AppResult PreviousSibling { get; set; }
		public AppResult NextSibling { get; set; }

		public virtual void ToXml (XmlElement element)
		{
		}

		// Operations
		public abstract AppResult Marked (string mark);
		public abstract AppResult CheckType (Type desiredType);
		public abstract AppResult Selected ();
		public abstract AppResult Text (string text, bool exact);
		public abstract AppResult Model (string column);
		public abstract AppResult Property (string propertyName, object value);
		public abstract List<AppResult> NextSiblings ();

		// Actions
		public abstract bool Select ();
		public abstract bool Click ();
		public abstract bool Click (double x, double y);
		public abstract bool TypeKey (char key, string state = "");
		public abstract bool TypeKey (string keyString, string state = "");
		public abstract bool EnterText (string text);
		public abstract bool Toggle (bool active);
		public abstract void Flash ();
		public abstract void SetProperty (string propertyName, object value);

		// More specific actions for complicated widgets

		#region For MacPlatform.MacIntegration.MainToolbar.SelectorView
		public virtual bool SetActiveConfiguration (string configurationName)
		{
			return false;
		}

		public virtual bool SetActiveRuntime (string runtimeName)
		{
			return false;
		}
		#endregion

		// Inspection Operations
		public abstract ObjectProperties Properties ();
		public abstract string GetResultType  ();

		public string SourceQuery { get; set; }

		void AddChildrenToList (List<AppResult> children, AppResult child, bool recursive = true)
		{
			AppResult node = child.FirstChild;
			children.Add (child);

			while (node != null) {
				if (recursive)
					AddChildrenToList (children, node);
				node = node.NextSibling;
			}
		}

		public virtual List<AppResult> Children (bool recursive = true)
		{
			List<AppResult> children = new List<AppResult> ();
			AddChildrenToList (children, FirstChild, recursive);

			return children;
		}

		public void SetProperty (object o, string propertyName, object value)
		{
			// Find the property for the name
			PropertyInfo propertyInfo = o.GetType().GetProperty(propertyName,
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);

			if (propertyInfo != null && propertyInfo.CanRead && !propertyInfo.GetIndexParameters ().Any ()) {
				propertyInfo.SetValue (o, value);
			}
		}

		/// <summary>
		/// Convenience function to add an attribute to an element
		/// </summary>
		/// <param name="element">The element to add the attribute</param>
		/// <param name="name">The name of the attribute</param>
		/// <param name="value">The value of the attribute</param>
		protected void AddAttribute (XmlElement element, string name, string value)
		{
			XmlDocument doc = element.OwnerDocument;
			XmlAttribute attr = doc.CreateAttribute (name);
			attr.Value = value;
			element.Attributes.Append (attr);
		}

		protected object GetPropertyValue (string propertyName, object requestedObject)
		{
			return AutoTestService.CurrentSession.UnsafeSync (delegate {
				PropertyInfo propertyInfo = requestedObject.GetType().GetProperty(propertyName,
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
				if (propertyInfo != null && propertyInfo.CanRead && !propertyInfo.GetIndexParameters ().Any ()) {
					var propertyValue = propertyInfo.GetValue (requestedObject);
					if (propertyValue != null) {
						return propertyValue;
					}
				}

				return null;
			});
		}

		protected ObjectProperties GetProperties (object resultObject)
		{
			var propertiesObject = new ObjectProperties ();
			if (resultObject != null) {
				propertiesObject.Add ("ToString", new ObjectResult (resultObject.ToString ()), null);
				var properties = resultObject.GetType ().GetProperties (
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
				foreach (var property in properties) {
					try {
						var value = GetPropertyValue (property.Name, resultObject);
						AppResult result = null;

						var gtkNotebookValue = value as Gtk.Notebook;
						if (gtkNotebookValue != null)
							result = new GtkNotebookResult (gtkNotebookValue);
						var gtkTreeviewValue = value as Gtk.TreeView;
						if (gtkTreeviewValue != null && result == null)
							result = new GtkTreeModelResult (gtkTreeviewValue, gtkTreeviewValue.Model, 0);
						var gtkWidgetValue = value as Gtk.Widget;
						if (gtkWidgetValue != null && result == null)
							result = new GtkWidgetResult (gtkWidgetValue);
						#if MAC
						var nsObjectValue = value as Foundation.NSObject;
						if (nsObjectValue != null && result == null)
							result = new NSObjectResult (nsObjectValue);
						#endif
						if (result == null)
							result = new ObjectResult (value);
						propertiesObject.Add (property.Name, result, property);
					} catch (Exception e) {
						MonoDevelop.Core.LoggingService.LogInfo ("Failed to fetch property '{0}' on '{1}' with Exception: {2}", property, resultObject, e);
					}
				}
			}

			return propertiesObject;
		}

		protected AppResult MatchProperty (string propertyName, object objectToCompare, object value)
		{
			foreach (var singleProperty in propertyName.Split (new [] { '.' })) {
				objectToCompare = GetPropertyValue (singleProperty, objectToCompare);
			}
			if (objectToCompare != null && value != null &&
				CheckForText (objectToCompare.ToString (), value.ToString (), false)) {
				return this;
			}
			return null;
		}

		protected bool CheckForText (string haystack, string needle, bool exact)
		{
			if (exact) {
				return haystack == needle;
			} else {
				return haystack != null && (haystack.IndexOf (needle, StringComparison.Ordinal) > -1);
			}
		}
	}
}
