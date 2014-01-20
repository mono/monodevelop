//
// MSBuildPropertyGroup.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using System.Linq;
using System.Collections.Generic;
using System.Xml;

using MonoDevelop.Core;
using System.Xml.Linq;
using Microsoft.Build.BuildEngine;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildPropertyGroup: MSBuildObject, IMSBuildPropertySet, IMSBuildPropertyGroupEvaluated
	{
		Dictionary<string,MSBuildProperty> properties;
		List<MSBuildProperty> propertyList = new List<MSBuildProperty> ();
		MSBuildProject parent;
		
		public MSBuildPropertyGroup (MSBuildProject parent, XmlElement elem): base (elem)
		{
			this.parent = parent;
		}

		void InitProperties ()
		{
			if (properties != null)
				return;

			properties = new Dictionary<string,MSBuildProperty> ();
			propertyList = new List<MSBuildProperty> ();

			foreach (var pelem in Element.ChildNodes.OfType<XmlElement> ()) {
				MSBuildProperty prevSameName;
				if (properties.TryGetValue (pelem.Name, out prevSameName))
					prevSameName.Overwritten = true;

				var prop = new MSBuildProperty (parent, pelem);
				prop.Owner = this;
				propertyList.Add (prop);
				properties [pelem.Name] = prop; // If a property is defined more than once, we only care about the last registered value
			}
		}

		public bool IsImported {
			get;
			set;
		}
		
		public MSBuildProject Project {
			get {
				return this.parent;
			}
		}

		internal bool IgnoreDefaultValues { get; set; }

		internal bool UppercaseBools { get; set; }

		IMSBuildPropertyEvaluated IMSBuildPropertyGroupEvaluated.GetProperty (string name)
		{
			return GetProperty (name);
		}

		public MSBuildProperty GetProperty (string name)
		{
			return GetProperty (name, null);
		}

		public MSBuildProperty GetProperty (string name, string condition)
		{
			InitProperties ();
			MSBuildProperty prop;
			properties.TryGetValue (name, out prop);
			return prop;
		}
		
		public IEnumerable<MSBuildProperty> Properties {
			get {
				InitProperties ();
				return propertyList.Where (p => !p.Overwritten);
			}
		}

		public string GetValue (string name, string defaultValue = null)
		{
			var prop = GetProperty (name);
			if (prop != null)
				return prop.Value;
			else
				return defaultValue;
		}

		public FilePath GetPathValue (string name, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath))
		{
			var prop = GetProperty (name);
			if (prop != null)
				return prop.GetPathValue (relativeToProject, relativeToPath);
			else
				return defaultValue;
		}

		public bool TryGetPathValue (string name, out FilePath value, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath))
		{
			var prop = GetProperty (name);
			if (prop != null)
				return prop.TryGetPathValue (out value, relativeToProject, relativeToPath);
			else {
				value = defaultValue;
				return value != default(FilePath);
			}
		}

		public T GetValue<T> (string name)
		{
			var prop = GetProperty (name);
			if (prop != null)
				return prop.GetValue<T> ();
			else
				return default(T);
		}

		public T GetValue<T> (string name, T defaultValue)
		{
			var prop = GetProperty (name);
			if (prop != null)
				return prop.GetValue<T> ();
			else
				return defaultValue;
		}

		public object GetValue (string name, Type type, object defaultValue)
		{
			var prop = GetProperty (name);
			if (prop != null)
				return prop.GetValue (type);
			else
				return defaultValue;
		}

		Dictionary<Type,object> customDataObjects = new Dictionary<Type, object> ();

		public T GetObject<T> () where T:IMSBuildDataObject, new()
		{
			object ob;
			if (!customDataObjects.TryGetValue (typeof(T), out ob)) {
				customDataObjects [typeof(T)] = ob = new T ();
				((IMSBuildDataObject)ob).Read (this, parent.Format);
			}
			return (T)ob;
		}

		public void SetObject<T> (T t, bool persist = true) where T:IMSBuildDataObject
		{
			customDataObjects [typeof(T)] = t;
		}

		public void WriteDataObjects ()
		{
			foreach (IMSBuildDataObject ob in customDataObjects.Values)
				ob.Write (this, parent.Format);
		}

		MSBuildProperty AddProperty (string name, string condition = null)
		{
			InitProperties ();
			int i = propertyOrder.IndexOf (name);
			var pelem = Element.OwnerDocument.CreateElement (null, name, MSBuildProject.Schema);
			int insertIndex = -1;
			if (i != -1) {
				var foundProp = FindExistingProperty (i - 1, -1);
				if (foundProp != null) {
					Element.InsertAfter (pelem, foundProp.Element);
					insertIndex = propertyList.IndexOf (foundProp) + 1;
				} else {
					foundProp = FindExistingProperty (i + 1, 1);
					if (foundProp != null) {
						Element.InsertBefore (pelem, foundProp.Element);
						insertIndex = propertyList.IndexOf (foundProp) - 1;
					}
					else
						Element.AppendChild (pelem);
				}
			} else
				Element.AppendChild (pelem);

			var prop = new MSBuildProperty (parent, pelem);
			prop.Owner = this;
			properties [name] = prop;

			if (insertIndex != -1)
				propertyList.Insert (insertIndex, prop);
			else
				propertyList.Add (prop);

			if (condition != null)
				prop.Condition = condition;
			return prop;
		}

		MSBuildProperty FindExistingProperty (int index, int inc)
		{
			while (index >= 0 && index < propertyOrder.Count) {
				var ep = GetProperty (propertyOrder[index]);
				if (ep != null)
					return ep;
				index += inc;
			}
			return null;
		}

		MSBuildProperty SafeGetProperty (string name, string condition)
		{
			var prop = GetProperty (name, condition);
			if (prop == null) {
				prop = GetProperty (name);
				if (prop != null) {
					prop.Condition = condition;
					return prop;
				}
			}
			return AddProperty (name, condition);
		}

		public void SetValue (string name, string value, string defaultValue = null, bool preserveExistingCase = false, bool mergeToMainGroup = false, string condition = null)
		{
			if (value == null && defaultValue == "")
				value = "";
			var prop = GetProperty (name, condition);
			var isDefault = value == defaultValue;
			if (isDefault && !mergeToMainGroup && !IgnoreDefaultValues) {
				// if the value is default, only remove the property if it was not already the default
				// to avoid unnecessary project file churn
				if (prop != null && (defaultValue == null || !string.Equals (defaultValue, prop.Value, preserveExistingCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
					RemoveProperty (prop);
				return;
			}
			if (prop == null)
				prop = AddProperty (name, condition);
			prop.SetValue (value, preserveExistingCase, mergeToMainGroup);
			prop.HasDefaultValue = isDefault;
		}

		public void SetValue (string name, FilePath value, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath), bool mergeToMainGroup = false, string condition = null)
		{
			var prop = GetProperty (name, condition);
			var isDefault = value.CanonicalPath == defaultValue.CanonicalPath;
			if (isDefault && !mergeToMainGroup && !IgnoreDefaultValues) {
				// if the value is default, only remove the property if it was not already the default
				// to avoid unnecessary project file churn
				if (prop != null && (defaultValue == null || defaultValue != prop.GetPathValue (relativeToProject, relativeToPath)))
					RemoveProperty (prop);
				return;
			}
			if (prop == null)
				prop = AddProperty (name, condition);
			prop.SetValue (value, relativeToProject, relativeToPath, mergeToMainGroup);
			prop.HasDefaultValue = isDefault;
		}

		public void SetValue (string name, object value, object defaultValue = null, bool mergeToMainGroup = false, string condition = null)
		{
			var prop = GetProperty (name, condition);
			var isDefault = object.Equals (value, defaultValue);
			if (isDefault && !mergeToMainGroup && !IgnoreDefaultValues) {
				// if the value is default, only remove the property if it was not already the default
				// to avoid unnecessary project file churn
				if (prop != null && (defaultValue == null || !object.Equals (defaultValue, prop.GetValue (defaultValue.GetType ()))))
					RemoveProperty (prop);
				return;
			}
			if (prop == null)
				prop = AddProperty (name, condition);
			prop.SetValue (value, mergeToMainGroup);
			prop.HasDefaultValue = isDefault;
		}

		public bool RemoveProperty (string name)
		{
			MSBuildProperty prop = GetProperty (name);
			if (prop != null) {
				RemoveProperty (prop);
				return true;
			}
			return false;
		}

		public void RemoveProperty (MSBuildProperty prop)
		{
			InitProperties ();
			properties.Remove (prop.Name);
			propertyList.Remove (prop);
			Element.RemoveChild (prop.Element);
		}

		public void RemoveAllProperties ()
		{
			InitProperties ();
			List<XmlNode> toDelete = new List<XmlNode> ();
			foreach (XmlNode node in Element.ChildNodes) {
				if (node is XmlElement)
					toDelete.Add (node);
			}
			foreach (XmlNode node in toDelete)
				Element.RemoveChild (node);
			properties.Clear ();
			propertyList.Clear ();
		}

		public void UnMerge (IMSBuildPropertySet baseGrp, ISet<string> propsToExclude)
		{
			HashSet<string> baseProps = new HashSet<string> ();
			foreach (MSBuildProperty prop in baseGrp.Properties) {
				if (propsToExclude != null && propsToExclude.Contains (prop.Name))
					continue;
				baseProps.Add (prop.Name);
				MSBuildProperty thisProp = GetProperty (prop.Name);
				if (thisProp != null && prop.Value.Equals (thisProp.Value, StringComparison.OrdinalIgnoreCase))
					RemoveProperty (prop.Name);
			}

			// Remove properties which have the default value and which are not defined in the main group
			foreach (var p in Properties.ToArray ()) {
				if (baseProps.Contains (p.Name))
					continue;
				if (p.HasDefaultValue)
					RemoveProperty (p);
			}
		}

		public override string ToString()
		{
			string s = "[MSBuildPropertyGroup:";
			foreach (MSBuildProperty prop in Properties)
				s += " " + prop.Name + "=" + prop.Value;
			return s + "]";
		}

		public void SetObject<T> (T t) where T : IMSBuildDataObject
		{
			throw new NotImplementedException ();
		}

		public bool HasProperty (string name)
		{
			InitProperties ();
			return properties.ContainsKey (name);
		}

		List<string> propertyOrder = new List<string> ();

		public void SetPropertyOrder (params string[] propertyNames)
		{
			int i = 0;
			foreach (var name in propertyNames) {
				if (i < propertyOrder.Count) {
					var pos = propertyOrder.IndexOf (name, i);
					if (pos != -1) {
						i = pos + 1;
						continue;
					} else {
						propertyOrder.Insert (i, name);
						i++;
						continue;
					}
				}
				propertyOrder.Add (name);
				i++;
			}
		}
	}

	public interface IMSBuildPropertyGroupEvaluated
	{
		bool HasProperty (string name);

		IMSBuildPropertyEvaluated GetProperty (string name);

		string GetValue (string name, string defaultValue = null);

		FilePath GetPathValue (string name, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath));

		bool TryGetPathValue (string name, out FilePath value, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath));

		T GetValue<T> (string name);

		T GetValue<T> (string name, T defaultValue);

		object GetValue (string name, Type type, object defaultValue);
	}
}
