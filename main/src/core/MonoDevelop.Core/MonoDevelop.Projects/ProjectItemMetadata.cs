//
// ProjectItemMetadata.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core;
using MonoDevelop.Projects.MSBuild;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

namespace MonoDevelop.Projects
{
	public class ProjectItemMetadata: IPropertySet
	{
		Dictionary<string,MSBuildProperty> properties;
		List<MSBuildProperty> propertyList = new List<MSBuildProperty> ();
		int initialMetadataCount;
		MSBuildProject project;

		internal ProjectItemMetadata ()
		{
		}

		internal ProjectItemMetadata (MSBuildProject project)
		{
			this.project = project;
		}

		internal ProjectItemMetadata (ProjectItemMetadata other)
		{
			this.project = other.project;
			if (other.properties != null) {
				properties = new Dictionary<string, MSBuildProperty> (other.properties.Count, StringComparer.OrdinalIgnoreCase);
				foreach (var p in other.propertyList) {
					var pc = p.Clone ();
					propertyList.Add (pc);
					properties [p.Name] = pc;
				}
			}
			initialMetadataCount = other.initialMetadataCount;
		}

		internal void OnLoaded ()
		{
			initialMetadataCount = propertyList.Count;
		}

		internal void SetProject (MSBuildProject project)
		{
			foreach (var p in propertyList) {
				p.ParentProject = project;
				p.ResolvePath ();
			}
		}

/*		internal void LoadProperties (XmlElement element)
		{
			foreach (var pelem in element.ChildNodes.OfType<XmlElement> ()) {
				MSBuildProperty prevSameName;
				if (properties != null && properties.TryGetValue (pelem.Name, out prevSameName))
					prevSameName.Overwritten = true;

				if (properties == null) {
					properties = new Dictionary<string,MSBuildProperty> ();
					propertyList = new List<MSBuildProperty> ();
				}

				var prop = new MSBuildProperty (project, pelem);
				propertyList.Add (prop);
				properties [pelem.Name] = prop; // If a property is defined more than once, we only care about the last registered value
			}
			initialMetadataCount = properties.Count;
		}*/

		internal bool PropertyCountHasChanged {
			get { return initialMetadataCount != (properties != null ? properties.Count : 0); }
		}

		public IMetadataProperty GetProperty (string name)
		{
			if (properties == null)
				return null;
			MSBuildProperty prop;
			properties.TryGetValue (name, out prop);
			return prop;
		}

		public IEnumerable<IMetadataProperty> GetProperties ()
		{
			if (propertyList == null)
				return new IMetadataProperty [0];
			return propertyList.Where (p => !p.Overwritten);
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

		MSBuildProperty AddProperty (string name, string condition = null)
		{
			if (properties == null) {
				properties = new Dictionary<string, MSBuildProperty> (StringComparer.OrdinalIgnoreCase);
				propertyList = new List<MSBuildProperty> ();
			}
			int i = propertyOrder != null ? propertyOrder.IndexOf (name) : -1;
			int insertIndex = -1;
			if (i != -1) {
				var foundProp = FindExistingProperty (i - 1, -1);
				if (foundProp != null) {
					insertIndex = propertyList.IndexOf (foundProp) + 1;
				} else {
					foundProp = FindExistingProperty (i + 1, 1);
					if (foundProp != null)
						insertIndex = propertyList.IndexOf (foundProp) - 1;
				}
			}

			var prop = new ItemMetadataProperty (name);
			prop.ParentProject = project;
			properties [name] = prop;

			if (insertIndex != -1)
				propertyList.Insert (insertIndex, prop);
			else
				propertyList.Add (prop);
			return prop;
		}

		internal void AddProperty (ItemMetadataProperty prop)
		{
			if (properties == null) {
				properties = new Dictionary<string, MSBuildProperty> (StringComparer.OrdinalIgnoreCase);
				propertyList = new List<MSBuildProperty> ();
			}
			prop.ParentProject = project;
			properties [prop.Name] = prop;
			propertyList.Add (prop);
		}

		MSBuildProperty FindExistingProperty (int index, int inc)
		{
			while (index >= 0 && index < propertyOrder.Count) {
				var ep = (MSBuildProperty) GetProperty (propertyOrder[index]);
				if (ep != null)
					return ep;
				index += inc;
			}
			return null;
		}

		void IPropertySet.SetValue (string name, string value, string defaultValue, bool preserveExistingCase, bool mergeToMainGroup, string condition, MSBuildValueType valueType)
		{
			SetValue (name, value, defaultValue, preserveExistingCase, valueType);
		}

		public void SetValue (string name, string value, string defaultValue = null, bool preserveExistingCase = false, MSBuildValueType valueType = null)
		{
			// If no value type is specified, use the default
			if (valueType == null)
				valueType = preserveExistingCase ? MSBuildValueType.DefaultPreserveCase : MSBuildValueType.Default;
			
			if (value == null && defaultValue == "")
				value = "";
			var prop = (MSBuildProperty) GetProperty (name);
			var isDefault = value == defaultValue;
			if (isDefault) {
				// if the value is default, only remove the property if it was not already the default
				// to avoid unnecessary project file churn
				if (prop != null && (defaultValue == null || !valueType.Equals (defaultValue, prop.Value)))
					RemoveProperty (prop);
				return;
			}
			if (prop == null)
				prop = AddProperty (name);
			prop.SetValue (value, valueType:valueType);
			prop.HasDefaultValue = isDefault;
		}

		void IPropertySet.SetValue (string name, FilePath value, FilePath defaultValue, bool relativeToProject, FilePath relativeToPath, bool mergeToMainGroup, string condition)
		{
			SetValue (name, value, defaultValue, relativeToProject, relativeToPath);
		}

		public void SetValue (string name, FilePath value, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath))
		{
			var prop = (MSBuildProperty) GetProperty (name);
			var isDefault = value.CanonicalPath == defaultValue.CanonicalPath;
			if (isDefault) {
				// if the value is default, only remove the property if it was not already the default
				// to avoid unnecessary project file churn
				if (prop != null && (defaultValue == null || defaultValue != prop.GetPathValue (relativeToProject, relativeToPath)))
					RemoveProperty (prop);
				return;
			}
			if (prop == null)
				prop = AddProperty (name);
			prop.SetValue (value, relativeToProject, relativeToPath);
			prop.HasDefaultValue = isDefault;
		}

		void IPropertySet.SetValue (string name, object value, object defaultValue, bool mergeToMainGroup, string condition)
		{
			SetValue (name, value, defaultValue);
		}

		public void SetValue (string name, object value, object defaultValue = null)
		{
			var prop = (MSBuildProperty) GetProperty (name);
			var isDefault = object.Equals (value, defaultValue);
			if (isDefault) {
				// if the value is default, only remove the property if it was not already the default
				// to avoid unnecessary project file churn
				if (prop != null && (defaultValue == null || !object.Equals (defaultValue, prop.GetValue (defaultValue.GetType ()))))
					RemoveProperty (prop);
				return;
			}
			if (prop == null)
				prop = AddProperty (name);
			prop.SetValue (value);
			prop.HasDefaultValue = isDefault;
		}

		public bool RemoveProperty (string name)
		{
			MSBuildProperty prop = (MSBuildProperty) GetProperty (name);
			if (prop != null) {
				RemoveProperty (prop);
				return true;
			}
			return false;
		}

		public void RemoveProperty (MSBuildProperty prop)
		{
			if (properties != null) {
				properties.Remove (prop.Name);
				propertyList.Remove (prop);
			}
		}

		public override string ToString()
		{
			string s = "[ProjectItemMetadata:";
			foreach (MSBuildProperty prop in GetProperties ())
				s += " " + prop.Name + "=" + prop.Value;
			return s + "]";
		}

		public bool HasProperty (string name)
		{
			return properties != null && properties.ContainsKey (name);
		}

		List<string> propertyOrder;

		public void SetPropertyOrder (params string[] propertyNames)
		{
			if (propertyOrder == null)
				propertyOrder = new List<string> ();
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

		public void RemoveAllProperties ()
		{
			if (properties != null) {
				properties.Clear ();
				propertyList.Clear ();
			}
		}
	}
}

