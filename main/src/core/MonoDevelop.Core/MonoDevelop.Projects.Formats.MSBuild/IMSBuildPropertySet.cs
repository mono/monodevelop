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
	public interface IMSBuildPropertySet: IPropertySet
	{
		string Label { get; set; }

		new MSBuildProperty GetProperty (string name);
		new IEnumerable<MSBuildProperty> GetProperties ();

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

		static PropInfo[] GetMembers (Type type, bool includeBaseMembers)
		{
			lock (types) {
				PropInfo[] pinfo;
				if (types.TryGetValue (type, out pinfo))
					return pinfo;

				List<PropInfo> list = new List<PropInfo> ();

				foreach (var m in type.GetMembers (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
					if (!includeBaseMembers && m.DeclaringType != type)
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
		}

		public static void WriteObjectProperties (this IPropertySet pset, object ob, Type typeToScan, bool includeBaseMembers = false)
		{
			var props = GetMembers (typeToScan, includeBaseMembers);
			foreach (var prop in props) {
				if (prop.Attribute.IsExternal)
					continue;
				if (prop.ReturnType == typeof(FilePath)) {
					var val = (FilePath)prop.GetValue (ob);
					FilePath def = (string)prop.Attribute.DefaultValue;
					pset.SetValue (prop.Name, val, def, mergeToMainGroup: prop.MergeToMainGroup);
				} else if (prop.Attribute is ProjectPathItemProperty && prop.ReturnType == typeof(string)) {
					FilePath val = (string)prop.GetValue (ob);
					FilePath def = (string)prop.Attribute.DefaultValue;
					pset.SetValue (prop.Name, val, def, mergeToMainGroup: prop.MergeToMainGroup);
				} else if (prop.ReturnType == typeof(string)) {
					pset.SetValue (prop.Name, (string)prop.GetValue (ob), (string)prop.Attribute.DefaultValue, prop.MergeToMainGroup);
				} else {
					pset.SetValue (prop.Name, prop.GetValue (ob), prop.Attribute.DefaultValue, prop.MergeToMainGroup);
				}
			}
		}

		public static void ReadObjectProperties (this IPropertySet pset, object ob, Type typeToScan, bool includeBaseMembers = false)
		{
			var props = GetMembers (typeToScan, includeBaseMembers);
			foreach (var prop in props) {
				if (prop.Attribute.IsExternal)
					continue;
				object readVal = null;
				if (prop.ReturnType == typeof(FilePath)) {
					FilePath def = (string)prop.Attribute.DefaultValue;
					readVal = pset.GetPathValue (prop.Name, def);
				} else if (prop.Attribute is ProjectPathItemProperty && prop.ReturnType == typeof(string)) {
					FilePath def = (string)prop.Attribute.DefaultValue;
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

		static DataContext solutionDataContext = new DataContext ();

		public static void WriteObjectProperties (this SlnPropertySet pset, object ob)
		{
			DataSerializer ser = new DataSerializer (solutionDataContext);
			ser.SerializationContext.BaseFile = pset.ParentFile.FileName;
			ser.SerializationContext.IncludeDeletedValues = true;
			var data = ser.Serialize (ob, ob.GetType()) as DataItem;
			if (data != null)
				WriteDataItem (pset, data);
		}

		public static void ReadObjectProperties (this SlnPropertySet pset, object ob)
		{
			DataSerializer ser = new DataSerializer (solutionDataContext);
			ser.SerializationContext.BaseFile = pset.ParentFile.FileName;
			var data = ReadDataItem (pset);	
			ser.Deserialize (ob, data);
		}

		static void WriteDataItem (SlnPropertySet pset, DataItem item)
		{
			HashSet<DataItem> removedItems = new HashSet<DataItem> ();
			Dictionary<DataNode,int> ids = new Dictionary<DataNode, int> ();

			// First of all read the existing data item, since we want to keep data that has not been modified
			// The ids collection is filled with a map of items and their ids
			var currentItem = ReadDataItem (pset, ids);

			// UpdateFromItem will add new data to the item, it will remove the data that has been removed, and
			// will ignore unknown data that has not been set or removed
			currentItem.UpdateFromItem (item, removedItems);

			// List of IDs that are not used anymore and can be reused when writing the item
			var unusedIds = new Queue<int> (removedItems.Select (it => ids[it]).OrderBy (i => i));

			// Calculate the next free id, to be used when adding new items
			var usedIds = ids.Where (p => !removedItems.Contains (p.Key)).Select (p => p.Value).ToArray ();
			int nextId = usedIds.Length > 0 ? usedIds.Max () + 1 : 0;

			// Clear all properties, since we are writing them again
			pset.Clear ();

			foreach (DataNode val in currentItem.ItemData)
				WriteDataNode (pset, "", val, ids, unusedIds, ref nextId);
		}

		static void WriteDataNode (SlnPropertySet pset, string prefix, DataNode node, Dictionary<DataNode,int> ids, Queue<int> unusedIds, ref int id)
		{
			string name = node.Name;
			string newPrefix = prefix.Length > 0 ? prefix + "." + name: name;

			if (node is DataValue) {
				DataValue val = (DataValue) node;
				string value = EncodeString (val.Value);
				pset.SetValue (newPrefix, value);
			}
			else {
				DataItem it = (DataItem) node;
				int newId;
				if (!ids.TryGetValue (node, out newId))
					newId = unusedIds.Count > 0 ? unusedIds.Dequeue () : (id++);
				pset.SetValue (newPrefix, "$" + newId);
				newPrefix = "$" + newId;
				foreach (DataNode cn in it.ItemData)
					WriteDataNode (pset, newPrefix, cn, ids, unusedIds, ref id);
			}
		}

		static string EncodeString (string val)
		{
			if (val.Length == 0)
				return val;

			int i = val.IndexOfAny (new char[] {'\n','\r','\t'});
			if (i != -1 || val [0] == '@') {
				StringBuilder sb = new StringBuilder ();
				if (i != -1) {
					int fi = val.IndexOf ('\\');
					if (fi != -1 && fi < i) i = fi;
					sb.Append (val.Substring (0,i));
				} else
					i = 0;
				for (int n = i; n < val.Length; n++) {
					char c = val [n];
					if (c == '\r')
						sb.Append (@"\r");
					else if (c == '\n')
						sb.Append (@"\n");
					else if (c == '\t')
						sb.Append (@"\t");
					else if (c == '\\')
						sb.Append (@"\\");
					else
						sb.Append (c);
				}
				val = "@" + sb.ToString ();
			}
			char fc = val [0];
			char lc = val [val.Length - 1];
			if (fc == ' ' || fc == '"' || fc == '$' || lc == ' ')
				val = "\"" + val + "\"";
			return val;
		}

		static string DecodeString (string val)
		{
			val = val.Trim (' ', '\t');
			if (val.Length == 0)
				return val;
			if (val [0] == '\"')
				val = val.Substring (1, val.Length - 2);
			if (val [0] == '@') {
				StringBuilder sb = new StringBuilder (val.Length);
				for (int n = 1; n < val.Length; n++) {
					char c = val [n];
					if (c == '\\') {
						c = val [++n];
						if (c == 'r') c = '\r';
						else if (c == 'n') c = '\n';
						else if (c == 't') c = '\t';
					}
					sb.Append (c);
				}
				return sb.ToString ();
			}
			else
				return val;
		}

		static DataItem ReadDataItem (SlnPropertySet pset)
		{
			return ReadDataItem (pset, null);
		}

		static DataItem ReadDataItem (SlnPropertySet pset, Dictionary<DataNode,int> ids)
		{
			DataItem it = new DataItem ();

			var lines = pset.ToArray ();

			int lineNum = 0;
			int lastLine = lines.Length - 1;
			while (lineNum <= lastLine) {
				if (!ReadDataNode (it, lines, lastLine, "", ids, ref lineNum))
					lineNum++;
			}
			return it;
		}

		static bool ReadDataNode (DataItem item, KeyValuePair<string,string>[] lines, int lastLine, string prefix, Dictionary<DataNode,int> ids, ref int lineNum)
		{
			var s = lines [lineNum];

			string name = s.Key;
			if (name.Length == 0) {
				lineNum++;
				return true;
			}

			// Check if the line belongs to the current item
			if (prefix.Length > 0) {
				if (!s.Key.StartsWith (prefix + ".", StringComparison.Ordinal))
					return false;
				name = s.Key.Substring (prefix.Length + 1);
			} else {
				if (s.Key.StartsWith ("$", StringComparison.Ordinal))
					return false;
			}

			string value = s.Value;
			if (value.StartsWith ("$", StringComparison.Ordinal)) {
				// New item
				DataItem child = new DataItem ();
				child.Name = name;
				int id;
				if (ids != null && int.TryParse (value.Substring (1), out id))
					ids [child] = id;
				lineNum++;
				while (lineNum <= lastLine) {
					if (!ReadDataNode (child, lines, lastLine, value, ids, ref lineNum))
						break;
				}
				item.ItemData.Add (child);
			} else {
				value = DecodeString (value);
				DataValue val = new DataValue (name, value);
				item.ItemData.Add (val);
				lineNum++;
			}
			return true;
		}

		internal static void WriteExternalProjectProperties (this MSBuildProject project, object ob, Type typeToScan, bool includeBaseMembers = false)
		{
			var props = GetMembers (typeToScan, includeBaseMembers);
			XmlConfigurationWriter writer = null;
			foreach (var prop in props) {
				if (!prop.Attribute.IsExternal)
					continue;
				var val = prop.GetValue (ob);
				if (val != null) {
					DataSerializer ser = new DataSerializer (Services.ProjectService.DataContext);
					var data = ser.Serialize (val, prop.ReturnType);
					if (data != null) {
						data.Name = prop.Name;
						if (writer == null)
							writer = new XmlConfigurationWriter { Namespace = MSBuildProject.Schema };
						var elem = writer.Write (project.Document, data);
						project.SetMonoDevelopProjectExtension (prop.Name, elem);
						continue;
					}
				}
				project.RemoveMonoDevelopProjectExtension (prop.Name);
			}
		}

		internal static void ReadExternalProjectProperties (this MSBuildProject project, object ob, Type typeToScan, bool includeBaseMembers = false)
		{
			var props = GetMembers (typeToScan, includeBaseMembers);
			foreach (var prop in props) {
				if (!prop.Attribute.IsExternal)
					continue;
				var sec = project.GetMonoDevelopProjectExtension (prop.Name);
				if (sec != null) {
					var data = XmlConfigurationReader.DefaultReader.Read (sec);
					DataSerializer ser = new DataSerializer (Services.ProjectService.DataContext);
					var val = ser.Deserialize (prop.ReturnType, data);
					prop.SetValue (ob, val);
				}
			}
		}
	}
}
