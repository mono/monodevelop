//
// MSBuildItem.cs
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

using System.Xml;
using System.Collections.Generic;
using System;
using System.Collections.Immutable;
using System.Linq;


namespace MonoDevelop.Projects.MSBuild
{
	public class MSBuildItem: MSBuildElement
	{
		MSBuildPropertyGroup metadata;
		string name;
		string include;
		string exclude;
		string remove;
		string update;

		public MSBuildItem ()
		{
			metadata = new MSBuildPropertyGroup ();
			metadata.UppercaseBools = true;
			metadata.ParentNode = this;
		}

		public MSBuildItem (string name): this ()
		{
			this.name = name;
		}

		public bool IsUpdate {
			get { return string.IsNullOrEmpty (include) && !string.IsNullOrEmpty (update); }
		}

		public bool IsRemove {
			get { return string.IsNullOrEmpty (include) && !string.IsNullOrEmpty (remove); }
		}

		public bool IsInclusion {
			get { return !string.IsNullOrEmpty (include); }
		}

		internal static readonly string [] KnownAttributes = { "Include", "Exclude", "Condition", "Label", "Remove", "Update" };

		internal override string [] GetKnownAttributes ()
		{
			var project = ParentProject;
			if (project != null)
				return project.GetKnownItemAttributes (name);

			return KnownAttributes;
		}

		internal override void ReadAttribute (string name, string value)
		{
			if (name == "Include")
				include = value;
			else if (name == "Exclude")
				exclude = value;
			else if (name == "Remove")
				remove = value;
			else if (name == "Update")
				update = value;
			else
				base.ReadAttribute (name, value);
		}

		internal override void ReadUnknownAttribute (MSBuildXmlReader reader, string lastAttr)
		{
			metadata.ReadUnknownAttribute (reader, lastAttr);
		}

		internal override string WriteAttribute (string name)
		{
			if (name == "Include")
				return !string.IsNullOrEmpty (include) || string.IsNullOrEmpty (update) ? include : null;
			else if (name == "Exclude")
				return exclude;
			else if (name == "Remove")
				return remove;
			else if (name == "Update")
				return string.IsNullOrEmpty (include) && !string.IsNullOrEmpty (update) ? update : null;
			else {
				string result = base.WriteAttribute (name);
				if (result != null)
					return result;

				if (!ExistingPropertyAttribute (name))
					return WritePropertyAsAttribute (name);

				return null;
			}
		}

		bool ExistingPropertyAttribute (string propertyName)
		{
			return metadata.PropertiesAttributeOrder.Any (property => property.Name == propertyName);
		}

		string WritePropertyAsAttribute (string propertyName)
		{
			var prop = metadata.GetProperty (propertyName);
			if (prop != null) {
				prop.FromAttribute = true;
				return prop.Value;
			}
			return null;
		}

		internal override void Read (MSBuildXmlReader reader)
		{
			name = reader.LocalName;
			base.Read (reader);
		}

		internal override void ReadChildElement (MSBuildXmlReader reader)
		{
			metadata.ReadChildElement (reader);
		}

		internal override void Write (XmlWriter writer, WriteContext context)
		{
			MSBuildWhitespace.Write (StartWhitespace, writer);

			writer.WriteStartElement (NamespacePrefix, GetElementName (), Namespace);

			var props = metadata.PropertiesAttributeOrder;

			if (props.Count > 0) {
				int propIndex = 0;
				int knownIndex = 0;
				var knownAtts = attributeOrder ?? GetKnownAttributes ();
				string lastAttr = null;
				do {
					if (propIndex < props.Count && (lastAttr == props [propIndex].AfterAttribute || props [propIndex].AfterAttribute == null)) {
						var prop = props [propIndex++];
						writer.WriteAttributeString (prop.Name, prop.Value);
						lastAttr = prop.Name;
					} else if (knownIndex < knownAtts.Length) {
						var aname = knownAtts [knownIndex++];
						lastAttr = aname;
						var val = WriteAttribute (aname);
						if (val != null)
							writer.WriteAttributeString (aname, val);
					} else
						lastAttr = null;
				} while (propIndex < props.Count || knownIndex < knownAtts.Length);
			} else {
				var knownAtts = attributeOrder ?? GetKnownAttributes ();
				for (int i = 0; i < knownAtts.Length; i++) {
					var aname = knownAtts [i];
					var val = WriteAttribute (aname);
					if (val != null)
						writer.WriteAttributeString (aname, val);
				}
			}

			WriteContent (writer, context);

			writer.WriteEndElement ();

			MSBuildWhitespace.Write (EndWhitespace, writer);
			if (context.Evaluating) {
				string id = context.ItemMap.Count.ToString ();
				context.ItemMap [id] = this;
			}
		}

		internal override string GetElementName ()
		{
			return name;
		}

		public MSBuildItemGroup ParentGroup {
			get {
				return (MSBuildItemGroup)ParentObject;
			}
		}

		public string Include {
			get { return include; }
			set {
				AssertCanModify ();
				include = value;
				NotifyChanged ();
			}
		}
		
		public string Exclude {
			get { return exclude; }
			set {
				AssertCanModify ();
				exclude = value;
				NotifyChanged ();
			}
		}

		public string Remove {
			get { return remove; }
			set {
				AssertCanModify ();
				remove = value;
				NotifyChanged ();
			}
		}

		public string Update {
			get { return update; }
			set {
				AssertCanModify ();
				update = value;
				NotifyChanged ();
			}
		}

		public bool IsImported {
			get;
			set;
		}

		public string Name {
			get { return name; }
		}

		public IMSBuildPropertySet Metadata {
			get {
				return metadata; 
			}
		}

		internal int EvaluatedItemCount { get; set; }

		internal bool IsWildcardItem {
			get { return IsInclusion && EvaluatedItemCount > 1 && (Include.Contains ("*") || Include.Contains (";") || Include.StartsWith ("@(")); }
		}

		public void AddExclude (string excludePath)
		{
			if (string.IsNullOrWhiteSpace (exclude))
				exclude = excludePath;
			else if (!exclude.Contains (excludePath)){
				exclude += ";" + excludePath;
			} else {
				if (exclude.Split (new [] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select (e => e.Trim ()).Contains (excludePath))
					exclude += ";" + excludePath;
			}
		}

		public override string ToString()
		{
			return $"<Name='{Name}' Include='{Include}' />";
		}
	}

	class MSBuildItemEvaluated: IMSBuildItemEvaluated
	{
		MSBuildPropertyGroupEvaluated metadata;
		string evaluatedInclude;
		string include;
		object sourceItem;

		internal MSBuildItemEvaluated (MSBuildProject parent, string name, string include, string evaluatedInclude)
		{
			this.include = include;
			this.evaluatedInclude = evaluatedInclude;
			metadata = new MSBuildPropertyGroupEvaluated (parent);
			Name = name;
		}

		public string Label { get; internal set; }

		public string Condition { get; internal set; }

		public string Include {
			get { return evaluatedInclude; }
		}

		public string UnevaluatedInclude {
			get { return include; }
		}

		public bool IsImported {
			get;
			internal set;
		}

		public string Name { get; private set; }

		public IMSBuildPropertyGroupEvaluated Metadata {
			get {
				return metadata; 
			}
		}

		public void AddSourceItem (MSBuildItem item)
		{
			item.EvaluatedItemCount++;
			if (sourceItem == null)
				sourceItem = item;
			else if (sourceItem is MSBuildItem) {
				if (item != (MSBuildItem)sourceItem)
					sourceItem = new MSBuildItem [] { (MSBuildItem)sourceItem, item };
			}
			else {
				var items = (MSBuildItem [])sourceItem;
				if (!items.Contains (item)) {
					var newItems = new MSBuildItem [items.Length + 1];
					Array.Copy (items, newItems, items.Length);
					newItems [newItems.Length - 1] = item;
					sourceItem = newItems;
				}
			}
		}

		public MSBuildItem SourceItem {
			get {
				if (sourceItem is MSBuildItem)
					return (MSBuildItem) sourceItem;
				else
					return SourceItems.LastOrDefault (); 
			}
		}

		public IEnumerable<MSBuildItem> SourceItems {
			get {
				if (sourceItem == null)
					return Enumerable.Empty<MSBuildItem> ();
				if (sourceItem is MSBuildItem)
					return Enumerable.Repeat ((MSBuildItem)sourceItem, 1);
				return (IEnumerable<MSBuildItem>)sourceItem;
			}
		}

		public override string ToString ()
		{
			return string.Format ("<{0} Include='{1}'>", Name, Include);
		}
	}


	public interface IMSBuildItemEvaluated
	{
		string Include { get; }

		string UnevaluatedInclude { get; }

		string Condition { get; }

		bool IsImported { get; }

		string Name { get; }

		IMSBuildPropertyGroupEvaluated Metadata { get; }

		/// <summary>
		/// The project item that generated this item. Null if this item has not been
		/// generated by a project item declared in an ItemGroup.
		/// </summary>
		/// <value>The source item.</value>
		MSBuildItem SourceItem { get; }

		/// <summary>
		/// The project items that generated this item. It can be a combination of Include + Update itens.
		/// Empty if this item has not been generated by a project item declared in an ItemGroup.
		/// </summary>
		/// <value>The source items.</value>
		IEnumerable<MSBuildItem> SourceItems { get; }
	}
}
