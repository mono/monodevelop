//
// MSBuildPropertyGroupMerged.cs
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

namespace MonoDevelop.Projects.Formats.MSBuild
{
	
	class MSBuildPropertyGroupMerged: IMSBuildPropertySet
	{
		List<IMSBuildPropertySet> groups = new List<IMSBuildPropertySet> ();
		MSBuildFileFormat format;

		public MSBuildPropertyGroupMerged (MSBuildProject project, MSBuildFileFormat format)
		{
			this.format = format;
			Project = project;
		}

		public MSBuildProject Project { get; private set; }

		public string Label { get; set; }
		
		public void Add (IMSBuildPropertySet g)
		{
			groups.Add (g);
		}
		
		public int GroupCount {
			get { return groups.Count; }
		}
		
		public MSBuildProperty GetProperty (string name)
		{
			// Find property in reverse order, since the last set
			// value is the good one
			for (int n=groups.Count - 1; n >= 0; n--) {
				var g = groups [n];
				MSBuildProperty p = g.GetProperty (name);
				if (p != null)
					return p;
			}
			return null;
		}

		public IMSBuildPropertySet GetGroupForProperty (string name)
		{
			// Find property in reverse order, since the last set
			// value is the good one
			for (int n=groups.Count - 1; n >= 1; n--) {
				var g = groups [n];
				MSBuildProperty p = g.GetProperty (name);
				if (p != null)
					return g;
			}
			return groups[0];
		}

		Dictionary<Type,object> customDataObjects = new Dictionary<Type, object> ();

		public T GetObject<T> () where T:IMSBuildDataObject, new()
		{
			object ob;
			if (!customDataObjects.TryGetValue (typeof(T), out ob)) {
				customDataObjects [typeof(T)] = ob = new T ();
				((IMSBuildDataObject)ob).Read (this, format);
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
				ob.Write (this, format);
		}

		public void SetPropertyValue (string name, string value, bool preserveExistingCase)
		{
			MSBuildProperty p = GetProperty (name);
			if (p != null) {
				if (!preserveExistingCase || !string.Equals (value, p.Value, StringComparison.OrdinalIgnoreCase)) {
					p.SetValue (value);
				}
				return;
			}
			groups [0].SetValue (name, value, preserveExistingCase:preserveExistingCase);
		}

		public void SetValue (string name, string value, string defaultValue = null, bool preserveExistingCase = false, bool mergeToMainGroup = false, string condition = null)
		{
			GetGroupForProperty (name).SetValue (name, value, defaultValue, preserveExistingCase, mergeToMainGroup, condition);
		}

		public void SetValue (string name, FilePath value, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath), bool mergeToMainGroup = false, string condition = null)
		{
			GetGroupForProperty (name).SetValue (name, value, defaultValue, relativeToProject, relativeToPath, mergeToMainGroup, condition);
		}

		public void SetValue (string name, object value, object defaultValue = null, bool mergeToMainGroup = false, string condition = null)
		{
			GetGroupForProperty (name).SetValue (name, value, defaultValue, mergeToMainGroup, condition);
		}

		public string GetValue (string name, string defaultValue = null)
		{
			return GetGroupForProperty (name).GetValue (name, defaultValue);
		}

		public string GetPathValue (string name, FilePath defaultValue = default(FilePath), bool relativeToFile = true)
		{
			return GetGroupForProperty (name).GetPathValue (name, defaultValue, relativeToFile);
		}

		public T GetValue<T> (string name)
		{
			return GetGroupForProperty (name).GetValue<T> (name);
		}

		public T GetValue<T> (string name, T defaultValue)
		{
			return GetGroupForProperty (name).GetValue<T> (name, defaultValue);
		}

		public object GetValue (string name, Type type, object defaultValue)
		{
			return GetGroupForProperty (name).GetValue (name, type, defaultValue);
		}

		public bool RemoveProperty (string name)
		{
			bool found = false;
			foreach (var g in groups) {
				if (g.RemoveProperty (name)) {
					Prune ((MSBuildPropertyGroup)g);
					found = true;
				}
			}
			return found;
		}

		public void RemoveAllProperties ()
		{
			foreach (var g in groups) {
				g.RemoveAllProperties ();
				Prune ((MSBuildPropertyGroup)g);
			}
		}

		public void UnMerge (IMSBuildPropertySet baseGrp, ISet<string> propertiesToExclude)
		{
			foreach (var g in groups) {
				((MSBuildPropertyGroup)g).UnMerge (baseGrp, propertiesToExclude);
			}
		}

		public IEnumerable<MSBuildProperty> Properties {
			get {
				foreach (var g in groups) {
					foreach (var p in g.Properties)
						yield return p;
				}
			}
		}
		
		void Prune (MSBuildPropertyGroup g)
		{
			if (g != groups [0] && !g.Properties.Any()) {
				// Remove this group since it's now empty
				g.Project.RemoveGroup (g);
			}
		}

		public void SetObject<T> (T t) where T : IMSBuildDataObject
		{
			throw new NotImplementedException ();
		}

		public bool HasProperty (string name)
		{
			throw new NotImplementedException ();
		}

		public FilePath GetPathValue (string name, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath))
		{
			return GetGroupForProperty (name).GetPathValue (name, defaultValue, relativeToProject, relativeToPath);
		}

		public bool TryGetPathValue (string name, out FilePath value, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath))
		{
			return GetGroupForProperty (name).TryGetPathValue (name, out value, defaultValue, relativeToProject, relativeToPath);
		}

		public void SetPropertyOrder (params string[] propertyNames)
		{
			foreach (var g in groups)
				g.SetPropertyOrder (propertyNames);
		}
	}
	
}
