//
// IMSBuildProject.cs
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
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

namespace MonoDevelop.Projects.Formats.MSBuildInternal
{
	public abstract class MSBuildProject
	{
		Dictionary<object,Dictionary<Type,IMSBuildDataObject>> customDataObjects = new Dictionary<object,Dictionary<Type,IMSBuildDataObject>> ();
		Dictionary<string, MSBuildItemGroup> bestGroups;

		public abstract FilePath FileName { get; }

		public FilePath BaseDirectory {
			get { return FileName.ParentDirectory; }
		}

		public abstract void Load (FilePath file);

		public abstract void Save (string fileName);

		public abstract string SaveToString ();

		public MonoDevelop.Projects.Formats.MSBuild.MSBuildFileFormat Format {
			get;
			set;
		}

		public abstract string DefaultTargets { get; set; }

		public abstract string ToolsVersion { get; set; }

		public T GetObject<T> () where T : IMSBuildDataObject, new()
		{
			return GetObject<T> (this);
		}

		public void SetObject<T> (T t) where T : IMSBuildDataObject
		{
			SetObject<T> (this, t);
		}

		internal T GetObject<T> (object owner) where T : IMSBuildDataObject, new()
		{
			Dictionary<Type,IMSBuildDataObject> col;
			if (!customDataObjects.TryGetValue (owner, out col))
				return default(T);
			IMSBuildDataObject res;
			col.TryGetValue (typeof(T), out res);
			return (T)res;
		}

		internal void SetObject<T> (object owner, T t) where T : IMSBuildDataObject
		{
			Dictionary<Type,IMSBuildDataObject> col;
			if (!customDataObjects.TryGetValue (owner, out col))
				col = customDataObjects [owner] = new Dictionary<Type, IMSBuildDataObject> ();
			col [typeof(T)] = t;
		}

		internal IEnumerable<IMSBuildDataObject> GetDataObjects (object owner)
		{
			Dictionary<Type,IMSBuildDataObject> col;
			if (!customDataObjects.TryGetValue (owner, out col))
				return new IMSBuildDataObject[0];
			else
				return col.Values;
		}

		public abstract void AddNewImport (string name, MSBuildImport beforeImport = null);

		public abstract void RemoveImport (MSBuildImport import);

		public abstract IEnumerable<MSBuildImport> Imports { get; }

		public MSBuildPropertyGroup GetGlobalPropertyGroup ()
		{
			return PropertyGroups.FirstOrDefault (p => string.IsNullOrEmpty (p.Condition));
		}

		public abstract MSBuildPropertyGroup AddNewPropertyGroup (MSBuildPropertyGroup beforeGroup = null);

		public abstract void RemovePropertyGroup (MSBuildPropertyGroup grp);

		public abstract IEnumerable<MSBuildItem> GetAllItems ();

		public abstract IEnumerable<MSBuildItem> GetAllItems (params string[] names);

		public abstract IEnumerable<MSBuildPropertyGroup> PropertyGroups { get; }

		public abstract IEnumerable<MSBuildItemGroup> ItemGroups { get; }

		public abstract MSBuildItemGroup AddNewItemGroup ();

		public abstract MSBuildItem AddNewItem (string name, string include);

		public MSBuildItemGroup FindBestGroupForItem (string itemName)
		{
			MSBuildItemGroup group;

			if (bestGroups == null)
				bestGroups = new Dictionary<string, MSBuildItemGroup> ();
			else {
				if (bestGroups.TryGetValue (itemName, out group))
					return group;
			}

			foreach (MSBuildItemGroup grp in ItemGroups) {
				foreach (MSBuildItem it in grp.Items) {
					if (it.Name == itemName) {
						bestGroups [itemName] = grp;
						return grp;
					}
				}
			}
			group = AddNewItemGroup ();
			bestGroups [itemName] = group;
			return group;
		}

		public abstract XmlElement GetProjectExtensions (string section);

		public abstract void SetProjectExtensions (string section, string value);

		public abstract void RemoveProjectExtensions (string section);

		public abstract void RemoveItem (MSBuildItem item);
	}

	public abstract class MSBuildItemGroup
	{
		internal MSBuildItemGroup (MSBuildProject parent)
		{
			Project = parent;
		}

		public abstract MSBuildItem AddNewItem (string name, string include);

		public abstract IEnumerable<MSBuildItem> Items { get; }

		public MSBuildProject Project { get; private set; }
	}

	public abstract class MSBuildPropertyGroup: MSBuildPropertySet
	{
		MSBuildProject project;

		internal MSBuildPropertyGroup (MSBuildProject project): base (project)
		{
			this.project = project;
		}

		public abstract string Label { get; set; }
		public abstract string Condition { get; set; }
	}

	internal interface IPropertySetImpl
	{
		bool HasProperty (string name);
		MSBuildProperty GetProperty (string name, string condition = null);
		MSBuildProperty AddProperty (string name, string condition = null);
		bool RemoveProperty (MSBuildProperty prop);
		bool RemoveProperty (string name);
		void RemoveAllProperties ();
		IEnumerable<MSBuildProperty> Properties { get; }
	}

	public abstract class MSBuildPropertySet: IMSBuildPropertySet
	{
		MSBuildProject project;
		Dictionary<Type,IMSBuildDataObject> customDataObjects = new Dictionary<Type,IMSBuildDataObject> ();

		internal MSBuildPropertySet (MSBuildProject project)
		{
			this.project = project;
		}

		internal abstract IPropertySetImpl PropertySet { get; }

		public T GetObject<T> () where T : IMSBuildDataObject, new()
		{
			IMSBuildDataObject res;
			customDataObjects.TryGetValue (typeof(T), out res);
			return (T)res;
		}

		public void SetObject<T> (T t) where T : IMSBuildDataObject
		{
			customDataObjects [typeof(T)] = t;
		}

		internal void WriteDataObjects ()
		{
			foreach (IMSBuildDataObject ob in customDataObjects.Values)
				ob.Write (this, project.Format);
		}

		public bool HasProperty (string name)
		{
			return PropertySet.HasProperty (name);
		}

		public MSBuildProperty GetProperty (string name, string condition = null)
		{
			return PropertySet.GetProperty (name, condition);
		}

		public MSBuildProperty AddProperty (string name, string condition = null)
		{
			return PropertySet.AddProperty (name, condition);
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

		public void SetValue (string name, string value, string defaultValue = null, bool preserveExistingCase = false, bool isXmlValue = false, bool mergeToMainGroup = false, string condition = null)
		{
			var prop = SafeGetProperty (name, condition);
			if (value == defaultValue) {
				RemoveProperty (prop);
				return;
			}
			prop.SetValue (value, preserveExistingCase, isXmlValue, mergeToMainGroup);
		}

		public void SetValue (string name, XmlElement elem, bool mergeToMainGroup = false, string condition = null)
		{
			SafeGetProperty (name, condition).SetValue (elem, mergeToMainGroup);
		}

		public void SetValue (string name, XElement elem, bool mergeToMainGroup = false, string condition = null)
		{
			SafeGetProperty (name, condition).SetValue (elem, mergeToMainGroup);
		}

		public void SetValue (string name, FilePath value, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath), bool mergeToMainGroup = false, string condition = null)
		{
			SafeGetProperty (name, condition).SetValue (value, relativeToProject, relativeToPath, mergeToMainGroup);
		}

		public void SetValue (string name, bool value, bool? defaultValue = false, bool mergeToMainGroup = false, string condition = null)
		{
			SafeGetProperty (name, condition).SetValue (value, mergeToMainGroup);
		}

		public string GetValue (string name, string defaultValue = null)
		{
			var prop = GetProperty (name);
			if (prop != null)
				return prop.GetValue ();
			else
				return defaultValue;
		}

		public XElement GetXmlValue (string name)
		{
			var prop = GetProperty (name);
			if (prop != null)
				return XElement.Parse (prop.GetValue ());
			else
				return null;
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

		public bool GetBoolValue (string name, bool defaultValue = false)
		{
			var prop = GetProperty (name);
			if (prop != null)
				return prop.GetBoolValue ();
			else
				return defaultValue;
		}

		public virtual bool RemoveProperty (string name)
		{
			return PropertySet.RemoveProperty (name);
		}

		public bool RemoveProperty (MSBuildProperty prop)
		{
			return PropertySet.RemoveProperty (prop);
		}

		public void RemoveAllProperties ()
		{
			PropertySet.RemoveAllProperties ();
		}

		public IEnumerable<MSBuildProperty> Properties {
			get { return PropertySet.Properties; }
		}
	}

	public interface IMSBuildPropertySet
	{
		T GetObject<T> () where T:IMSBuildDataObject, new();
		void SetObject<T> (T t) where T:IMSBuildDataObject;

		bool HasProperty (string name);
		MSBuildProperty GetProperty (string name, string condition = null);
		IEnumerable<MSBuildProperty> Properties { get; }

		void SetValue (string name, string value, string defaultValue = null, bool preserveExistingCase = false, bool isXmlValue = false, bool mergeToMainGroup = false, string condition = null);
		void SetValue (string name, XmlElement elem, bool mergeToMainGroup = false, string condition = null);
		void SetValue (string name, XElement elem, bool mergeToMainGroup = false, string condition = null);
		void SetValue (string name, FilePath value, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath), bool mergeToMainGroup = false, string condition = null);
		void SetValue (string name, bool value, bool? defaultValue = false, bool mergeToMainGroup = false, string condition = null);

		string GetValue (string name, string defaultValue = null);
		XElement GetXmlValue (string name);
		FilePath GetPathValue (string name, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath));
		bool TryGetPathValue (string name, out FilePath value, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath));
		bool GetBoolValue (string name, bool defaultValue = false);

		bool RemoveProperty (string name);
		void RemoveAllProperties ();
	}

	public abstract class MSBuildImport
	{
		public abstract string Target { get; set; }

		public abstract string Label { get; set; }

		public abstract string Condition { get; set; }
	}

	public abstract class MSBuildProperty
	{
		MSBuildProject project;

		internal MSBuildProperty (MSBuildProject project)
		{
			this.project = project;
		}

		public abstract string Name {
			get;
		}

		public abstract string Condition { get; set; }

		public void SetValue (string value, bool preserveExistingCase = false, bool isXmlValue = false, bool mergeToMainGroup = false)
		{
			if (value == null)
				value = String.Empty;

			if (preserveExistingCase) {
				var current = GetPropertyValue ();
				if (current != null) {
					if (current.Equals (value, preserveExistingCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture))
						return;
				}
			}
			SetPropertyValue (value, isXmlValue);
		}

		public void SetValue (XmlElement elem, bool mergeToMainGroup = false)
		{
			SetPropertyValue (elem.OuterXml, true);
		}

		public void SetValue (XElement elem, bool mergeToMainGroup = false)
		{
			SetPropertyValue (elem.ToString (), true);
		}

		public void SetValue (FilePath value, bool relativeToProject = true, FilePath relativeToPath = default(FilePath), bool mergeToMainGroup = false)
		{
			string baseDir = null;
			if (relativeToProject) {
				if (relativeToPath != default(FilePath))
					throw new ArgumentException ("relativeToPath argument can't be used together with relativeToProject");
				baseDir = project.BaseDirectory;
			} else if (relativeToPath != null) {
				baseDir = relativeToPath;
			}
			SetPropertyValue (MonoDevelop.Projects.Formats.MSBuild.MSBuildProjectService.ToMSBuildPath (baseDir, value), false);
		}

		public void SetValue (bool value, bool mergeToMainGroup = false)
		{
			SetPropertyValue (value ? "True" : "False", false);
		}

		public string GetValue ()
		{
			return GetPropertyValue ();
		}

		public XElement GetXmlValue ()
		{
			var val = GetPropertyValue ();
			if (val == null)
				return null;
			return XElement.Parse (val);
		}

		public FilePath GetPathValue (bool relativeToProject = true, FilePath relativeToPath = default(FilePath))
		{
			var val = GetPropertyValue ();
			string baseDir = null;
			if (relativeToProject) {
				if (relativeToPath != default(FilePath))
					throw new ArgumentException ("relativeToPath argument can't be used together with relativeToProject");
				baseDir = project.BaseDirectory;
			} else if (relativeToPath != null) {
				baseDir = relativeToPath;
			}
			return MonoDevelop.Projects.Formats.MSBuild.MSBuildProjectService.FromMSBuildPath (baseDir, val);
		}

		public bool TryGetPathValue (out FilePath value, bool relativeToProject = true, FilePath relativeToPath = default(FilePath))
		{
			var val = GetPropertyValue ();
			string baseDir = null;
			if (relativeToProject) {
				if (relativeToPath != default(FilePath))
					throw new ArgumentException ("relativeToPath argument can't be used together with relativeToProject");
				baseDir = project.BaseDirectory;
			} else if (relativeToPath != null) {
				baseDir = relativeToPath;
			}
			string path;
			var res = MonoDevelop.Projects.Formats.MSBuild.MSBuildProjectService.FromMSBuildPath (baseDir, val, out path);
			value = path;
			return res;
		}

		public bool GetBoolValue ()
		{
			var val = GetPropertyValue ();
			return val.Equals ("true", StringComparison.InvariantCultureIgnoreCase);
		}

		protected abstract void SetPropertyValue (string value, bool isXml);
		protected abstract string GetPropertyValue ();
	}

	public abstract class MSBuildItem: MSBuildPropertySet
	{
		MSBuildProject project;

		internal MSBuildItem (MSBuildProject project): base (project)
		{
			this.project = project;
		}

		public abstract string Include { get; }

		public abstract string UnevaluatedInclude { get; }

		public abstract string Name { get; }
	}

	public interface IMSBuildDataObject
	{
		void Read (IMSBuildPropertySet pset, MonoDevelop.Projects.Formats.MSBuild.MSBuildFileFormat format);
		void Write (IMSBuildPropertySet pset, MonoDevelop.Projects.Formats.MSBuild.MSBuildFileFormat format);
	}
}

