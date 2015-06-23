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
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Linq;
using MonoDevelop.Components.AutoTest.Results;
using System.Reflection;

namespace MonoDevelop.Components.AutoTest
{
	public abstract class AppResult : MarshalByRefObject
	{
		//public Gtk.Widget ResultWidget { get; private set; }

		public AppResult ParentNode { get; set; }
		public AppResult FirstChild { get; set; }
		public AppResult PreviousSibling { get; set; }
		public AppResult NextSibling { get; set; }

		// Operations
		public abstract AppResult Marked (string mark);
		public abstract AppResult CheckType (Type desiredType);
		public abstract AppResult Text (string text, bool exact);
		public abstract AppResult Model (string column);
		public abstract AppResult Property (string propertyName, object value);
		public abstract List<AppResult> NextSiblings ();

		public abstract ObjectProperties Properties ();

		// Actions
		public abstract bool Select ();
		public abstract bool Click ();
		public abstract bool TypeKey (char key, string state = "");
		public abstract bool TypeKey (string keyString, string state = "");
		public abstract bool EnterText (string text);
		public abstract bool Toggle (bool active);

		public string SourceQuery { get; set; }

		void AddChildrenToList (List<AppResult> children, AppResult child)
		{
			AppResult node = child.FirstChild;
			children.Add (child);

			while (node != null) {
				AddChildrenToList (children, node);
				node = node.NextSibling;
			}
		}

		public virtual List<AppResult> FlattenChildren ()
		{
			List<AppResult> children = new List<AppResult> ();
			AddChildrenToList (children, FirstChild);

			return children;
		}

		protected object GetPropertyValue (string propertyName, object requestedObject)
		{
			return AutoTestService.CurrentSession.UnsafeSync (delegate {
				PropertyInfo propertyInfo = requestedObject.GetType().GetProperty(propertyName,
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
				if (propertyInfo != null && propertyInfo.CanRead) {
					var propertyValue = propertyInfo.GetValue (requestedObject);
					if (propertyValue != null) {
						return propertyValue;
					}
				}

				return null;
			});
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
				return (haystack.IndexOf (needle) > -1);
			}
		}

		protected ObjectProperties GetProperties (object resultObject)
		{
			var propertiesObject = new ObjectProperties ();
			var properties = resultObject.GetType ().GetProperties (
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			foreach (var property in properties) {
				propertiesObject.Add (property.Name, new ObjectResult (property, GetPropertyValue (property.Name, resultObject)));
			}

			return propertiesObject;
		}
	}

	public class ObjectProperties : MarshalByRefObject
	{
		readonly Dictionary<string,ObjectResult> propertyMap = new Dictionary<string,ObjectResult> ();

		internal ObjectProperties () { }

		internal void Add (string propertyName, ObjectResult propertyValue)
		{
			propertyMap.Add (propertyName, propertyValue);
		}

		public ReadOnlyCollection<string> GetPropertyNames ()
		{
			return propertyMap.Keys.ToList ().AsReadOnly ();
		}

		public ObjectResult this [string key]
		{
			get {
				return propertyMap [key];
			}
		}
	}

	public class PropertyMetaData : MarshalByRefObject
	{
		readonly PropertyInfo propertyInfo;

		internal PropertyMetaData (PropertyInfo propertyInfo)
		{
			this.propertyInfo = propertyInfo;
		}

		public string Name
		{
			get {
				return propertyInfo.Name;
			}
		}

		public bool CanRead
		{
			get {
				return propertyInfo.CanRead;
			}
		}

		public bool CanWrite
		{
			get {
				return propertyInfo.CanWrite;
			}
		}

		public string PropertyType
		{
			get {
				return propertyInfo.PropertyType.FullName;
			}
		}
	}
}

