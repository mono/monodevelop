//
// MSBuildPropertySet.cs
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
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Text;

using Microsoft.Build.BuildEngine;
using MonoDevelop.Core;
using System.Xml.Linq;
using System.Reflection;
using MonoDevelop.Core.Serialization;
using System.Globalization;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	
	public interface IMSBuildPropertySet
	{
		string Label { get; set; }
		T GetObject<T> () where T:IMSBuildDataObject, new();
		void SetObject<T> (T t) where T:IMSBuildDataObject;

		bool HasProperty (string name);
		MSBuildProperty GetProperty (string name);
		IEnumerable<MSBuildProperty> Properties { get; }

		void SetValue (string name, string value, string defaultValue = null, bool preserveExistingCase = false, bool mergeToMainGroup = false, string condition = null);
		void SetValue (string name, FilePath value, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath), bool mergeToMainGroup = false, string condition = null);
		void SetValue (string name, object value, object defaultValue = null, bool mergeToMainGroup = false, string condition = null);

		string GetValue (string name, string defaultValue = null);
		FilePath GetPathValue (string name, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath));
		bool TryGetPathValue (string name, out FilePath value, FilePath defaultValue = default(FilePath), bool relativeToProject = true, FilePath relativeToPath = default(FilePath));
		T GetValue<T> (string name);
		T GetValue<T> (string name, T defaultValue);
		object GetValue (string name, Type type, object defaultValue);

		bool RemoveProperty (string name);
		void RemoveAllProperties ();

		void SetPropertyOrder (params string[] propertyNames);

		MSBuildProject Project { get; }
	}

	public static class IMSBuildPropertySetExtensions
	{
		class PropInfo
		{
			public MemberInfo Member;
			public ItemPropertyAttribute Attribute;
			public bool MergeToMainGroup;

			public Type ReturnType {
				get { return (Member is FieldInfo) ? ((FieldInfo)Member).FieldType : ((PropertyInfo)Member).PropertyType; }
			}
			public object GetValue (object instance)
			{
				if (Member is FieldInfo)
					return ((FieldInfo)Member).GetValue (instance);
				else
					return ((PropertyInfo)Member).GetValue (instance, null);
			}
			public void SetValue (object instance, object value)
			{
				if (Member is FieldInfo)
					((FieldInfo)Member).SetValue (instance, value);
				else
					((PropertyInfo)Member).SetValue (instance, value, null);
			}
			public string Name {
				get { return Attribute.Name ?? Member.Name; }
			}
		}

		static Dictionary<Type,PropInfo[]> types = new Dictionary<Type, PropInfo[]> ();

		static PropInfo[] GetMembers (Type type)
		{
			PropInfo[] pinfo;
			if (types.TryGetValue (type, out pinfo))
				return pinfo;

			List<PropInfo> list = new List<PropInfo> ();

			foreach (var m in type.GetMembers (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
				if (m.DeclaringType != type)
					continue;
				if (!(m is FieldInfo) && !(m is PropertyInfo))
					continue;
				var attr = Attribute.GetCustomAttribute (m, typeof(ItemPropertyAttribute)) as ItemPropertyAttribute;
				if (attr == null)
					continue;
				bool merge = Attribute.IsDefined (m, typeof(MergeToProjectAttribute));

				list.Add (new PropInfo {
					Member = m,
					Attribute = attr,
					MergeToMainGroup = merge
				});
			}
			return types [type] = list.ToArray ();
		}

		public static void WriteObjectProperties (this IMSBuildPropertySet pset, object ob, Type typeToScan)
		{
			var props = GetMembers (typeToScan);
			foreach (var prop in props) {
				if (prop.ReturnType == typeof(FilePath)) {
					var val = (FilePath)prop.GetValue (ob);
					FilePath def = prop.Attribute.DefaultValue != null ? (string)prop.Attribute.DefaultValue : (string)null;
					pset.SetValue (prop.Name, val, def, mergeToMainGroup: prop.MergeToMainGroup);
				} else if (prop.Attribute is ProjectPathItemProperty && prop.ReturnType == typeof(string)) {
					FilePath val = (string)prop.GetValue (ob);
					FilePath def = prop.Attribute.DefaultValue != null ? (string)prop.Attribute.DefaultValue : (string)null;
					pset.SetValue (prop.Name, val, def, mergeToMainGroup: prop.MergeToMainGroup);
				} else if (prop.ReturnType == typeof(string)) {
					pset.SetValue (prop.Name, (string)prop.GetValue (ob), (string)prop.Attribute.DefaultValue, prop.MergeToMainGroup);
				} else {
					pset.SetValue (prop.Name, prop.GetValue (ob), prop.Attribute.DefaultValue, prop.MergeToMainGroup);
				}
			}
		}

		public static void ReadObjectProperties (this IMSBuildPropertySet pset, object ob, Type typeToScan)
		{
			var props = GetMembers (typeToScan);
			foreach (var prop in props) {
				object readVal = null;
				if (prop.ReturnType == typeof(FilePath)) {
					FilePath def = prop.Attribute.DefaultValue != null ? (string)prop.Attribute.DefaultValue : (string)null;
					readVal = pset.GetPathValue (prop.Name, def);
				} else if (prop.Attribute is ProjectPathItemProperty && prop.ReturnType == typeof(string)) {
					FilePath def = prop.Attribute.DefaultValue != null ? (string)prop.Attribute.DefaultValue : (string)null;
					readVal = pset.GetPathValue (prop.Name, def);
					readVal = readVal.ToString ();
				} else if (prop.ReturnType == typeof(string)) {
					readVal = pset.GetValue (prop.Name, (string)prop.Attribute.DefaultValue);
				} else {
					readVal = pset.GetValue (prop.Name, prop.ReturnType, prop.Attribute.DefaultValue);
				}
				prop.SetValue (ob, readVal);
			}
		}
	}
}
