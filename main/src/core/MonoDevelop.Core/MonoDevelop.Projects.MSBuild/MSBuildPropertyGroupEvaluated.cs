//
// MSBuildPropertyGroupEvaluated.cs
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
using System.Collections.Generic;
using System.Xml.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.MSBuild
{
	class MSBuildPropertyGroupEvaluated: MSBuildNode, IMSBuildPropertyGroupEvaluated, IMSBuildProjectObject
	{
		protected Dictionary<string,IMSBuildPropertyEvaluated> properties = new Dictionary<string, IMSBuildPropertyEvaluated> (StringComparer.OrdinalIgnoreCase);
		MSBuildEngine engine;

		internal MSBuildPropertyGroupEvaluated (MSBuildProject parent)
		{
			ParentProject = parent;
		}

		internal void Sync (MSBuildEngine engine, object item)
		{
			properties.Clear ();
			this.engine = engine;
			foreach (var propName in engine.GetItemMetadataNames (item)) {
				var prop = new MSBuildPropertyEvaluated (ParentProject, propName, engine.GetItemMetadata (item, propName), engine.GetEvaluatedItemMetadata (item, propName));
				properties [propName] = prop;
			}
		}

		public bool HasProperty (string name)
		{
			return properties.ContainsKey (name);
		}

		public IMSBuildPropertyEvaluated GetProperty (string name)
		{
			IMSBuildPropertyEvaluated prop;
			properties.TryGetValue (name, out prop);
			return prop;
		}

		internal void SetProperty (string key, IMSBuildPropertyEvaluated value)
		{
			properties [key] = value;
		}

		internal void SetProperties (Dictionary<string,IMSBuildPropertyEvaluated> properties)
		{
			this.properties = properties;
		}

		public IEnumerable<IMSBuildPropertyEvaluated> GetProperties ()
		{
			return properties.Values;
		}

		internal bool RemoveProperty (string name)
		{
			return properties.Remove (name);
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
	}

	class MSBuildEvaluatedPropertyCollection: MSBuildPropertyGroupEvaluated, IMSBuildEvaluatedPropertyCollection, IPropertySet, IPropertyGroupListener
	{
		public readonly static MSBuildEvaluatedPropertyCollection Empty = new MSBuildEvaluatedPropertyCollection (null);

		public MSBuildPropertyGroup LinkedGroup { get; set; }

		public MSBuildEvaluatedPropertyCollection (MSBuildProject parent): base (parent)
		{
		}

		internal void SyncCollection (MSBuildEngine e, object project)
		{
			properties.Clear ();
			foreach (var p in e.GetEvaluatedProperties (project)) {
				string name, value, finalValue; bool definedMultipleTimes;
				e.GetPropertyInfo (p, out name, out value, out finalValue, out definedMultipleTimes);
				properties [name] = new MSBuildPropertyEvaluated (ParentProject, name, value, finalValue, definedMultipleTimes);
			}
		}

		public void LinkToGroup (MSBuildPropertyGroup group)
		{
			LinkedGroup = group;
			group.PropertyGroupListener = this;
			foreach (var p in group.GetProperties ()) {
				var ep = (MSBuildPropertyEvaluated) GetProperty (p.Name);
				if (ep == null)
					ep = AddProperty (p.Name);
				ep.LinkToProperty (p);
			}
		}

		public void RemoveRedundantProperties ()
		{
			// Remove properties whose value is the same as the one set in the global group
			// Remove properties which have the default value and which are not defined in the main group

			foreach (MSBuildProperty prop in LinkedGroup.GetProperties ()) {
				if (prop.Modified && prop.HasDefaultValue)
					RemoveProperty (prop.Name);
			}
		}

		IMetadataProperty IPropertySet.GetProperty (string name)
		{
			AssertLinkedToGroup ();
			return (IMetadataProperty) GetProperty (name);
		}

		IEnumerable<IMetadataProperty> IPropertySet.GetProperties ()
		{
			AssertLinkedToGroup ();
			foreach (IMetadataProperty p in Properties)
				yield return p;
		}

		void IPropertySet.SetValue (string name, string value, string defaultValue, bool preserveExistingCase, bool mergeToMainGroup, string condition, MSBuildValueType valueType)
		{
			AssertLinkedToGroup ();
			LinkedGroup.SetValue (name, value, defaultValue, preserveExistingCase, mergeToMainGroup, condition, valueType);
		}

		void IPropertySet.SetValue (string name, FilePath value, FilePath defaultValue, bool relativeToProject, FilePath relativeToPath, bool mergeToMainGroup, string condition)
		{
			AssertLinkedToGroup ();
			LinkedGroup.SetValue (name, value, defaultValue, relativeToProject, relativeToPath, mergeToMainGroup, condition);
		}

		void IPropertySet.SetValue (string name, object value, object defaultValue, bool mergeToMainGroup, string condition)
		{
			AssertLinkedToGroup ();
			LinkedGroup.SetValue (name, value, defaultValue, mergeToMainGroup, condition);
		}

		void IPropertySet.SetPropertyOrder (params string [] propertyNames)
		{
			// When used as IPropertySet, this collection must be linked to a property group
			AssertLinkedToGroup ();
			LinkedGroup.SetPropertyOrder (propertyNames);
		}

		bool IPropertySet.RemoveProperty (string name)
		{
			AssertLinkedToGroup ();
			return LinkedGroup.RemoveProperty (name);
		}

		MSBuildPropertyEvaluated AddProperty (string name)
		{
			var p = new MSBuildPropertyEvaluated (ParentProject, name, null, null);
			p.IsNew = true;
			properties [name] = p;
			return p;
		}

		void AssertLinkedToGroup ()
		{
			if (LinkedGroup == null)
				throw new InvalidOperationException ("MSBuildEvaluatedPropertyCollection not linked to a property group");
		}

		void IPropertyGroupListener.PropertyAdded (MSBuildProperty prop)
		{
			var p = (MSBuildPropertyEvaluated) GetProperty (prop.Name);
			if (p == null || p.LinkedProperty == null) {
				if (p == null)
					p = AddProperty (prop.Name);
				p.LinkToProperty (prop);
			}
		}

		void IPropertyGroupListener.PropertyRemoved (MSBuildProperty prop)
		{
			var ep = (MSBuildPropertyEvaluated) GetProperty (prop.Name);
			if (ep == null)
				return;

			if (ep.LinkedProperty != null) {
				// Unlink the property
				ep.LinkToProperty (null);

				// The corresponding evaluated property instance will be removed id
				// 1) It didn't exist when the project was loaded
				// 2) The property did exist in the property group when the project was loaded,
				//    which means that the evaluated property is actually the result of evaluating
				//    that property group property.
				if (ep.IsNew || !prop.IsNew) {
					ep.IsNew = false;
					properties.Remove (ep.Name);
				}
			}
		}

		public IEnumerable<IMSBuildPropertyEvaluated> Properties {
			get { return properties.Values; }
		}
	}

	public interface IMSBuildEvaluatedPropertyCollection: IMSBuildPropertyGroupEvaluated
	{
		IEnumerable<IMSBuildPropertyEvaluated> Properties { get; }
	}

	interface IPropertyGroupListener
	{
		void PropertyAdded (MSBuildProperty prop);
		void PropertyRemoved (MSBuildProperty prop);
	}
}

