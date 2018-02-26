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

using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Projects.MSBuild
{
	public interface IMSBuildPropertySet: IPropertySet
	{
		string Label { get; set; }

		new MSBuildProperty GetProperty (string name);
		new IEnumerable<MSBuildProperty> GetProperties ();

		MSBuildProject ParentProject { get; }
	}

	public static class IMSBuildPropertySetExtensions
	{
		public static void WriteObjectProperties (this IPropertySet pset, object ob, Type typeToScan, bool includeBaseMembers = false)
		{
			DataSerializer ser = new DataSerializer (Services.ProjectService.DataContext);
			var props = Services.ProjectService.DataContext.GetProperties (ser.SerializationContext, ob);
			XmlConfigurationWriter cwriter = null;
			XmlDocument xdoc = null;

			var mso = pset as IMSBuildProjectObject;
			if (mso != null && mso.ParentProject != null)
				ser.SerializationContext.BaseFile = mso.ParentProject.FileName;
			ser.SerializationContext.DirectorySeparatorChar = '\\';
		
			foreach (var prop in props) {
				if (prop.IsExternal)
					continue;
				bool merge = Attribute.IsDefined (prop.Member, typeof(MergeToProjectAttribute));
				if (prop.PropertyType == typeof(FilePath)) {
					var val = (FilePath)prop.GetValue (ob);
					FilePath def = (string)prop.DefaultValue;
					pset.SetValue (prop.Name, val, def, mergeToMainGroup: merge);
				} else if (prop.DataType is PathDataType && prop.PropertyType == typeof(string)) {
					FilePath val = (string)prop.GetValue (ob);
					FilePath def = (string)prop.DefaultValue;
					pset.SetValue (prop.Name, val, def, mergeToMainGroup: merge);
				} else if (prop.PropertyType == typeof(string)) {
					pset.SetValue (prop.Name, (string)prop.GetValue (ob), (string)prop.DefaultValue, mergeToMainGroup:merge);
				} else if (prop.DataType.IsSimpleType) {
					pset.SetValue (prop.Name, prop.GetValue (ob), prop.DefaultValue, mergeToMainGroup:merge);
				} else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition () == typeof(Nullable<>)) {
					pset.SetValue (prop.Name, prop.GetValue (ob), prop.DefaultValue, mergeToMainGroup:merge);
				} else {
					var val = prop.GetValue (ob);
					if (val != null) {
						if (cwriter == null) {
							string xmlns = (mso?.ParentProject != null) ? mso.ParentProject.Namespace : MSBuildProject.Schema;
							cwriter = new XmlConfigurationWriter { Namespace = xmlns, StoreAllInElements = true };
							xdoc = new XmlDocument ();
						}
						var data = prop.Serialize (ser.SerializationContext, ob, val);
						if (data != null) {
							var elem = cwriter.Write (xdoc, data);
							pset.SetValue (prop.Name, prop.WrapObject ? elem.OuterXml : elem.InnerXml);
						} else
							pset.RemoveProperty (prop.Name);
					} else
						pset.RemoveProperty (prop.Name);
				}
			}
		}

		public static void ReadObjectProperties (this IReadOnlyPropertySet pset, object ob, Type typeToScan, bool includeBaseMembers = false)
		{
			DataSerializer ser = new DataSerializer (Services.ProjectService.DataContext);
			var props = Services.ProjectService.DataContext.GetProperties (ser.SerializationContext, ob);

			var mso = pset as IMSBuildProjectObject;
			if (mso != null && mso.ParentProject != null)
				ser.SerializationContext.BaseFile = mso.ParentProject.FileName;
			ser.SerializationContext.DirectorySeparatorChar = '\\';

			foreach (var prop in props) {
				if (prop.IsExternal)
					continue;
				object readVal = null;
				if (prop.PropertyType == typeof(FilePath)) {
					FilePath def = (string)prop.DefaultValue;
					readVal = pset.GetPathValue (prop.Name, def);
				} else if (prop.DataType is PathDataType && prop.PropertyType == typeof(string)) {
					FilePath def = (string)prop.DefaultValue;
					readVal = pset.GetPathValue (prop.Name, def);
					readVal = readVal.ToString ();
				} else if (prop.PropertyType == typeof(string)) {
					readVal = pset.GetValue (prop.Name, (string)prop.DefaultValue);
				} else if (prop.DataType.IsSimpleType) {
					readVal = pset.GetValue (prop.Name, prop.PropertyType, prop.DefaultValue);
				} else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition () == typeof(Nullable<>)) {
					readVal = pset.GetValue (prop.Name, prop.PropertyType, prop.DefaultValue);
				} else {
					var val = pset.GetValue (prop.Name);
					if (!string.IsNullOrEmpty (val)) {
						try {
							if (!prop.WrapObject)
								val = "<a>" + val + "</a>";
							var data = XmlConfigurationReader.DefaultReader.Read (new XmlTextReader (new StringReader (val)));
							if (prop.HasSetter && prop.DataType.CanCreateInstance) {
								readVal = prop.Deserialize (ser.SerializationContext, ob, data);
							} else if (prop.DataType.CanReuseInstance) {
								// Try to deserialize over the existing instance
								prop.Deserialize (ser.SerializationContext, ob, data, prop.GetValue (ob));
								continue;
							} else {
								throw new InvalidOperationException ("The property '" + prop.Name + "' does not have a setter.");
							}
						} catch (Exception ex) {
							LoggingService.LogError ("Cound not read project property '" + prop.Name + "'", ex);
						}
					} else
						continue;
				}
				prop.SetValue (ob, readVal);
			}
		}

		static DataContext solutionDataContext = new DataContext ();

		public static void WriteObjectProperties (this SlnSection pset, object ob)
		{
			DataSerializer ser = new DataSerializer (solutionDataContext);
			ser.SerializationContext.BaseFile = pset.ParentFile.FileName;
			ser.SerializationContext.IncludeDeletedValues = true;
			var data = ser.Serialize (ob, ob.GetType()) as DataItem;
			if (data != null)
				WriteDataItem (pset, data);
		}

		public static void ReadObjectProperties (this SlnSection pset, object ob)
		{
			DataSerializer ser = new DataSerializer (solutionDataContext);
			ser.SerializationContext.BaseFile = pset.ParentFile.FileName;
			var data = ReadDataItem (pset);	
			ser.Deserialize (ob, data);
		}

		static void WriteDataItem (SlnSection pset, DataItem item)
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

			var newSet = new List<KeyValuePair<string, string>> ();

			foreach (DataNode val in currentItem.ItemData)
				WriteDataNode (newSet, "", val, ids, unusedIds, ref nextId);
			
			pset.SetContent (newSet);
		}

		static void WriteDataNode (List<KeyValuePair<string, string>> pset, string prefix, DataNode node, Dictionary<DataNode,int> ids, Queue<int> unusedIds, ref int id)
		{
			string name = node.Name;
			string newPrefix = prefix.Length > 0 ? prefix + "." + name: name;

			if (node is DataValue) {
				DataValue val = (DataValue) node;
				string value = EncodeString (val.Value);
				pset.Add (new KeyValuePair<string, string> (newPrefix, value));
			}
			else {
				DataItem it = (DataItem) node;
				int newId;
				if (!ids.TryGetValue (node, out newId))
					newId = unusedIds.Count > 0 ? unusedIds.Dequeue () : (id++);
				pset.Add (new KeyValuePair<string, string> (newPrefix, "$" + newId));
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
				StringBuilder sb = StringBuilderCache.Allocate ();
				if (i != -1) {
					int fi = val.IndexOf ('\\');
					if (fi != -1 && fi < i) i = fi;
					sb.Append (val, 0,i);
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
				val = "@" + StringBuilderCache.ReturnAndFree (sb);
			}
			char fc = val [0];
			char lc = val [val.Length - 1];
			if (fc == ' ' || fc == '"' || fc == '$' || lc == ' ')
				val = "\"" + val + "\"";
			return val;
		}

		static string DecodeString (string val)
		{
			int start = 0;
			int end = val.Length;

			for (; start < val.Length; ++start) {
				if (val [start] == ' ' || val [start] == '\t')
					continue;
				break;
			}
			for (int i = end - 1; i >= start; --i) {
				if (val [i] == ' ' || val [i] == '\t') {
					end--;
					continue;
				}
				break;
			}

			if (end == start)
				return string.Empty;
			
			if (val [start] == '\"') {
				start++;
				end--;
			}
			if (val [start] == '@') {
				StringBuilder sb = StringBuilderCache.Allocate ();
				for (int n = start + 1; n < end; n++) {
					char c = val [n];
					if (c == '\\') {
						c = val [++n];
						if (c == 'r') c = '\r';
						else if (c == 'n') c = '\n';
						else if (c == 't') c = '\t';
					}
					sb.Append (c);
				}
				return StringBuilderCache.ReturnAndFree (sb);
			}
			else
				return val.Substring (start, end - start);
		}

		static DataItem ReadDataItem (SlnSection pset)
		{
			return ReadDataItem (pset, null);
		}

		static DataItem ReadDataItem (SlnSection pset, Dictionary<DataNode,int> ids)
		{
			DataItem it = new DataItem ();

			var lines = pset.GetContent ().ToArray ();

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
			DataSerializer ser = new DataSerializer (Services.ProjectService.DataContext);
			var props = Services.ProjectService.DataContext.GetProperties (ser.SerializationContext, ob);

			XmlConfigurationWriter writer = null;
			foreach (var prop in props) {
				if (!prop.IsExternal)
					continue;
				var val = prop.GetValue (ob);
				if (val != null) {
					var data = prop.Serialize (ser.SerializationContext, ob, val);
					if (data != null) {
						data.Name = prop.Name;
						if (writer == null)
							writer = new XmlConfigurationWriter { Namespace = project.Namespace };

						XmlDocument doc = new XmlDocument ();
						var elem = writer.Write (doc, data);
						// TODO NPM
						project.SetMonoDevelopProjectExtension (prop.Name, elem);
						continue;
					}
				}
				project.RemoveMonoDevelopProjectExtension (prop.Name);
			}
		}

		internal static void ReadExternalProjectProperties (this MSBuildProject project, object ob, Type typeToScan, bool includeBaseMembers = false)
		{
			DataSerializer ser = new DataSerializer (Services.ProjectService.DataContext);
			var props = Services.ProjectService.DataContext.GetProperties (ser.SerializationContext, ob);

			foreach (var prop in props) {
				if (!prop.IsExternal)
					continue;
				var sec = project.GetMonoDevelopProjectExtension (prop.Name);
				if (sec != null) {
					var data = XmlConfigurationReader.DefaultReader.Read (sec);
					var val = prop.Deserialize (ser.SerializationContext, ob, data);
					prop.SetValue (ob, val);
				}
			}
		}
	}
}
