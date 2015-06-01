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
	public class MSBuildPropertyGroup: MSBuildObject, IMSBuildPropertySet, IMSBuildEvaluatedPropertyCollection
	{
		Dictionary<string,MSBuildProperty> properties = new Dictionary<string, MSBuildProperty> ();
		List<MSBuildProperty> propertyList = new List<MSBuildProperty> ();

		public MSBuildPropertyGroup ()
		{
		}

		internal override void Read (XmlReader reader, ReadContext context)
		{
			base.Read (reader, context);

			if (reader.IsEmptyElement) {
				reader.Skip ();
				return;
			}
			reader.Read ();
			while (reader.NodeType != XmlNodeType.EndElement) {
				if (reader.NodeType == XmlNodeType.Element) {
					
					MSBuildProperty prevSameName;
					if (properties.TryGetValue (reader.LocalName, out prevSameName))
						prevSameName.Overwritten = true;
					
					var prop = new MSBuildProperty ();
					prop.ParentObject = this;
					prop.Read (reader, context);
					propertyList.Add (prop);
					properties [prop.Name] = prop; // If a property is defined more than once, we only care about the last registered value
				}
				else
					reader.Read ();
			}
			reader.Read ();
		}

		internal override IEnumerable<MSBuildObject> GetChildren ()
		{
			return propertyList;
		}

		internal override void OnProjectSet ()
		{
			base.OnProjectSet ();
			foreach (var p in propertyList)
				p.ResolvePath ();
		}

		internal void CopyFrom (MSBuildPropertyGroup other)
		{
			foreach (var prop in other.propertyList) {
				var cp = prop.Clone ();
				var currentPropIndex = propertyList.FindIndex (p => p.Name == prop.Name);
				if (currentPropIndex != -1) {
					var currentProp = propertyList [currentPropIndex];
					propertyList [currentPropIndex] = cp;
				} else {
					propertyList.Add (cp);
				}
				properties [cp.Name] = cp;
				cp.ResetIndent (false);
			}
			foreach (var prop in propertyList.ToArray ()) {
				if (!other.HasProperty (prop.Name))
					RemoveProperty (prop);
			}

			NotifyChanged ();
		}

		public bool IsImported {
			get;
			set;
		}
		
		internal bool IgnoreDefaultValues { get; set; }

		internal bool UppercaseBools { get; set; }

		IMetadataProperty IPropertySet.GetProperty (string name)
		{
			return GetProperty (name);
		}

		IEnumerable<IMetadataProperty> IPropertySet.GetProperties ()
		{
			return GetProperties ().Cast<IMetadataProperty> ();
		}

		IMSBuildPropertyEvaluated IMSBuildPropertyGroupEvaluated.GetProperty (string name)
		{
			return GetProperty (name);
		}

		IEnumerable<IMSBuildPropertyEvaluated> IMSBuildEvaluatedPropertyCollection.Properties {
			get { return GetProperties (); }
		}

		public MSBuildProperty GetProperty (string name)
		{
			return GetProperty (name, null);
		}

		public MSBuildProperty GetProperty (string name, string condition)
		{
			MSBuildProperty prop;
			properties.TryGetValue (name, out prop);
			return prop;
		}
		
		public IEnumerable<MSBuildProperty> GetProperties ()
		{
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
			int i = propertyOrder.IndexOf (name);
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

			var prop = new MSBuildProperty ();
			prop.ParentObject = this;
			properties [name] = prop;

			if (insertIndex != -1)
				propertyList.Insert (insertIndex, prop);
			else
				propertyList.Add (prop);

			if (condition != null)
				prop.Condition = condition;
			
			prop.ResetIndent (false);

			NotifyChanged ();
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
			if (isDefault && !mergeToMainGroup) {
				// if the value is default, only remove the property if it was not already the default
				// to avoid unnecessary project file churn
				if (prop != null && string.Equals (defaultValue ?? "", prop.Value, preserveExistingCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
					return;
				if (!IgnoreDefaultValues) {
					if (prop != null)
						RemoveProperty (prop);
					return;
				}
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
			if (isDefault && !mergeToMainGroup) {
				// if the value is default, only remove the property if it was not already the default
				// to avoid unnecessary project file churn
				if (prop != null && defaultValue != null && defaultValue == prop.GetPathValue (relativeToProject, relativeToPath))
					return;
				if (!IgnoreDefaultValues) {
					if (prop != null)
						RemoveProperty (prop);
					return;
				}
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
			if (isDefault && !mergeToMainGroup) {
				// if the value is default, only remove the property if it was not already the default
				// to avoid unnecessary project file churn
				if (prop != null && defaultValue != null && object.Equals (defaultValue, prop.GetValue (defaultValue.GetType ())))
					return;
				if (!IgnoreDefaultValues) {
					if (prop != null)
						RemoveProperty (prop);
					return;
				}
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
			prop.RemoveIndent ();
			properties.Remove (prop.Name);
			propertyList.Remove (prop);
			NotifyChanged ();
		}

		public void RemoveAllProperties ()
		{
			foreach (var p in propertyList)
				p.RemoveIndent ();
			properties.Clear ();
			propertyList.Clear ();
			NotifyChanged ();
		}

		public void UnMerge (IMSBuildPropertySet baseGrp, ISet<string> propsToExclude)
		{
			HashSet<string> baseProps = new HashSet<string> ();
			foreach (MSBuildProperty prop in baseGrp.GetProperties ()) {
				if (propsToExclude != null && propsToExclude.Contains (prop.Name))
					continue;
				baseProps.Add (prop.Name);
				MSBuildProperty thisProp = GetProperty (prop.Name);
				if (thisProp != null && thisProp.ValueType.Equals (prop.Value, thisProp.Value))
					RemoveProperty (prop.Name);
			}

			// Remove properties which have the default value and which are not defined in the main group
			foreach (var p in GetProperties ().ToArray ()) {
				if (baseProps.Contains (p.Name))
					continue;
				if (p.HasDefaultValue)
					RemoveProperty (p);
			}
		}

		public override string ToString()
		{
			string s = "[MSBuildPropertyGroup:";
			foreach (MSBuildProperty prop in GetProperties ())
				s += " " + prop.Name + "=" + prop.Value;
			return s + "]";
		}

		public bool HasProperty (string name)
		{
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

	public interface IMSBuildPropertyGroupEvaluated: IReadOnlyPropertySet
	{
		bool HasProperty (string name);

		IMSBuildPropertyEvaluated GetProperty (string name);
	}
}
