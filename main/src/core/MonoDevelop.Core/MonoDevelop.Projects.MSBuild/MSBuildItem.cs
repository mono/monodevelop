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


namespace MonoDevelop.Projects.MSBuild
{
	public class MSBuildItem: MSBuildElement
	{
		MSBuildPropertyGroup metadata;
		string name;
		string include;
		string exclude;

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

		static readonly string [] knownAttributes = { "Include", "Exclude", "Condition", "Label" };

		internal override string [] GetKnownAttributes ()
		{
			return knownAttributes;
		}

		internal override void ReadAttribute (string name, string value)
		{
			if (name == "Include")
				include = value;
			else if (name == "Exclude")
				exclude = value;
			else
				base.ReadAttribute (name, value);
		}

		internal override string WriteAttribute (string name)
		{
			if (name == "Include")
				return include;
			else if (name == "Exclude")
				return exclude;
			else
				return base.WriteAttribute (name);
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
			base.Write (writer, context);
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
			get { return EvaluatedItemCount > 1 && (Include.Contains ("*") || Include.Contains (";")); }
		}
	}

	class MSBuildItemEvaluated: IMSBuildItemEvaluated
	{
		MSBuildPropertyGroupEvaluated metadata;
		string evaluatedInclude;
		string include;
		MSBuildItem sourceItem;

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

		public MSBuildItem SourceItem {
			get { return sourceItem; }
			set { sourceItem = value; sourceItem.EvaluatedItemCount++; }
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
	}
}
