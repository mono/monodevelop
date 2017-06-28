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
using System.Collections.Immutable;

namespace MonoDevelop.Projects.MSBuild
{
	public class MSBuildPropertyGroup: MSBuildElement, IMSBuildPropertySet, IMSBuildEvaluatedPropertyCollection
	{
		Dictionary<string,MSBuildProperty> properties = new Dictionary<string, MSBuildProperty> (StringComparer.OrdinalIgnoreCase);
		internal List<MSBuildProperty> PropertiesAttributeOrder { get; } = new List<MSBuildProperty> ();
		public MSBuildPropertyGroup ()
		{
		}

		internal override ImmutableList<MSBuildNode> ChildNodes {
			get {
				if (ParentNode is MSBuildItem)
					return ((MSBuildItem)ParentNode).ChildNodes;
				return base.ChildNodes;
			}
			set {
				if (ParentNode is MSBuildItem)
					((MSBuildItem)ParentNode).ChildNodes = value;
				base.ChildNodes = value;
			}
		}

		internal MSBuildObject PropertiesParent {
			get {
				return (MSBuildObject) (ParentNode as MSBuildItem) ?? this;
			}
		}

		internal override void ReadUnknownAttribute (MSBuildXmlReader reader, string lastAttr)
		{
			MSBuildProperty prevSameName;
			if (properties.TryGetValue (reader.LocalName, out prevSameName))
				prevSameName.Overwritten = true;

			var prop = new MSBuildProperty ();
			prop.ParentNode = PropertiesParent;
			prop.Owner = this;
			prop.ReadUnknownAttribute (reader, lastAttr);
			ChildNodes = ChildNodes.Add (prop);
			properties [prop.Name] = prop; // If a property is defined more than once, we only care about the last registered value
			PropertiesAttributeOrder.Add (prop);
		}

		internal override void ReadChildElement (MSBuildXmlReader reader)
		{
			MSBuildProperty prevSameName;
			if (properties.TryGetValue (reader.LocalName, out prevSameName))
				prevSameName.Overwritten = true;

			var prop = new MSBuildProperty ();
			prop.ParentNode = PropertiesParent;
			prop.Owner = this;
			prop.Read (reader);
			ChildNodes = ChildNodes.Add (prop);
			properties [prop.Name] = prop; // If a property is defined more than once, we only care about the last registered value
		}

		internal override string GetElementName ()
		{
			return "PropertyGroup";
		}

		internal override void OnProjectSet ()
		{
			base.OnProjectSet ();
			foreach (var p in ChildNodes.OfType<MSBuildProperty> ())
				p.ResolvePath ();
		}

		internal void CopyFrom (MSBuildPropertyGroup other)
		{
			AssertCanModify ();
			foreach (var node in other.ChildNodes) {
				var prop = node as MSBuildProperty;
				if (prop != null) {
					var cp = prop.Clone ();
					var currentPropIndex = ChildNodes.FindIndex (p => (p is MSBuildProperty) && ((MSBuildProperty)p).Name == prop.Name);
					if (currentPropIndex != -1) {
						var currentProp = (MSBuildProperty) ChildNodes [currentPropIndex];
						ChildNodes = ChildNodes.SetItem (currentPropIndex, cp);
					} else {
						ChildNodes = ChildNodes.Add (cp);
					}
					properties [cp.Name] = cp;
					cp.ParentNode = PropertiesParent;
					cp.Owner = this;
				} else
					ChildNodes = ChildNodes.Add (node);
			}
			foreach (var prop in ChildNodes.OfType<MSBuildProperty> ().ToArray ()) {
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
			return GetProperties ();
		}

		IEnumerable<IMSBuildPropertyEvaluated> IMSBuildPropertyGroupEvaluated.GetProperties ()
		{
			return GetProperties ();
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
			if (!string.IsNullOrEmpty (condition) && prop != null && prop.Condition != condition) {
				// There may be more than one property with the same name and different condition. Try to find the correct one.
				prop = ChildNodes.OfType<MSBuildProperty> ().FirstOrDefault (pr => pr.Name == name && pr.Condition == condition) ?? prop;
			}
			return prop;
		}
		
		public IEnumerable<MSBuildProperty> GetProperties ()
		{
			foreach (var node in ChildNodes) {
				var prop = node as MSBuildProperty;
				if (prop != null)
					yield return prop;
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

		MSBuildProperty AddProperty (string name, string condition = null)
		{
			AssertCanModify ();
			int i = propertyOrder.IndexOf (name);
			int insertIndex = -1;
			if (i != -1) {
				var foundProp = FindExistingProperty (i - 1, -1);
				if (foundProp != null) {
					insertIndex = ChildNodes.IndexOf (foundProp) + 1;
				} else {
					foundProp = FindExistingProperty (i + 1, 1);
					if (foundProp != null)
						insertIndex = ChildNodes.IndexOf (foundProp);
				}
			}

			var prop = new MSBuildProperty (name);
			prop.IsNew = true;
			prop.ParentNode = PropertiesParent;
			prop.Owner = this;
			properties [name] = prop;

			if (insertIndex != -1)
				ChildNodes = ChildNodes.Insert (insertIndex, prop);
			else
				ChildNodes = ChildNodes.Add (prop);

			if (condition != null)
				prop.Condition = condition;
			
			prop.ResetIndent (false);

			if (PropertyGroupListener != null)
				PropertyGroupListener.PropertyAdded (prop);

			NotifyChanged ();
			return prop;
		}

		internal IPropertyGroupListener PropertyGroupListener { get; set; }

		internal void UnlinkFromProjectInstance ()
		{
			PropertyGroupListener = null;
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

		public void SetValue (string name, string value, string defaultValue = null, bool preserveExistingCase = false, bool mergeToMainGroup = false, string condition = null, MSBuildValueType valueType = null)
		{
			AssertCanModify ();

			// If no value type is specified, use the default
			if (valueType == null)
				valueType = preserveExistingCase ? MSBuildValueType.DefaultPreserveCase : MSBuildValueType.Default;
			
			if (value == null && defaultValue == "")
				value = "";
			
			var prop = GetProperty (name, condition);
			var isDefault = value == defaultValue;
			if (isDefault && !mergeToMainGroup) {
				// if the value is default, only remove the property if it was not already the default
				// to avoid unnecessary project file churn
				if (prop != null && valueType.Equals (defaultValue ?? "", prop.Value))
					return;
				if (!IgnoreDefaultValues) {
					if (prop != null)
						RemoveProperty (prop);
					return;
				}
			}
			if (prop == null)
				prop = AddProperty (name, condition);
			prop.SetValue (value, preserveExistingCase, mergeToMainGroup, valueType);
			prop.HasDefaultValue = isDefault;
		}

		public void SetValue (string name, FilePath value, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath), bool mergeToMainGroup = false, string condition = null)
		{
			AssertCanModify ();
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
			AssertCanModify ();
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
			AssertCanModify ();
			if (PropertyGroupListener != null)
				PropertyGroupListener.PropertyRemoved (prop);
			prop.RemoveIndent ();
			properties.Remove (prop.Name);
			ChildNodes = ChildNodes.Remove (prop);
			NotifyChanged ();
		}

		internal void ResetIsNewFlags ()
		{
			foreach (MSBuildProperty prop in GetProperties ()) {
				prop.IsNew = false;
				prop.Modified = false;
			}
		}

		internal void PurgeDefaultProperties ()
		{
			// Remove properties that have been modified and have the default value. Usually such properties
			// would be removed when assigning the value, but that won't happen if IgnoreDefaultValues=true
			foreach (MSBuildProperty prop in GetProperties ()) {
				if ((prop.Modified && prop.HasDefaultValue && !prop.EvaluatedValueModified) || (!prop.Modified && prop.IsNew))
					RemoveProperty (prop.Name);
				prop.Modified = false;
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
			AssertCanModify ();
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

		IEnumerable<IMSBuildPropertyEvaluated> GetProperties ();
	}
}
