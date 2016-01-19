//
// MSBuildProjectFromFile.cs
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
using MSProject = Microsoft.Build.BuildEngine.Project;
using Microsoft.Build.BuildEngine;
using System.Collections.Generic;

namespace MonoDevelop.Projects.Formats.MSBuildInternal
{
	public class MSBuildProjectFromFile: MSBuildProject
	{
		MSProject project;
		List<ImportData> imports = new List<ImportData> ();
		List<MSBuildPropertyGroup> propertyGroups = new List<MSBuildPropertyGroup> ();

		class ImportData: MSBuildImport
		{
			#region implemented abstract members of MSBuildImport

			public override string Target { get; set; }

			public override string Label { get; set; }

			public override string Condition { get; set; }

			#endregion
		}

		class ProperyGroupData: MSBuildPropertyGroup, IPropertySetImpl
		{
			public List<MSBuildProperty> Properties = new List<MSBuildProperty> ();

			public ProperyGroupData (MSBuildProject project) : base (project)
			{
			}

			internal override IPropertySetImpl PropertySet {
				get {
					throw new NotImplementedException ();
				}
			}

			public override string Label { get; set; }

			public override string Condition { get; set; }
		}

		class ItemGroupData: MSBuildItemGroup
		{
			public List<MSBuildItem> GroupItems = new List<MSBuildItem> ();

			public ItemGroupData (MSBuildProject project): base (project)
			{
			}

			public override MSBuildItem AddNewItem (string name, string include)
			{
				var it = new ItemData (Project, name, include, include);
				GroupItems.Add (it);
				return it;
			}

			public override IEnumerable<MSBuildItem> Items {
				get {
					return GroupItems;
				}
			}
		}

		class ItemData: MSBuildItem
		{
			string name;
			string unevaluatedInclude;
			string include;

			public ItemData (MSBuildProject project, string name, string include, string unevaluatedInclude): base (project)
			{
				this.name = name;
				this.include = include;
				this.unevaluatedInclude = unevaluatedInclude;
			}

			internal override IPropertySetImpl PropertySet {
				get {
					throw new NotImplementedException ();
				}
			}

			public override string Include {
				get { return include; }
			}

			public override string UnevaluatedInclude {
				get {
					return unevaluatedInclude;
				}
			}

			public override string Name {
				get {
					return name;
				}
			}
		}

		class PropertyData: MSBuildProperty
		{
			public string Value;
			public string RawValue;
			string name;

			public PropertyData (string name, MSBuildProject project) : base (project)
			{
				this.name = name;
			}

			public override string Name {
				get { return name; }
			}

			protected override void SetPropertyValue (string value, bool isXml)
			{
				Value = value;
			}

			protected override string GetPropertyValue ()
			{
				return Value;
			}

			public override string Condition { get; set; }
		}

		public MSBuildProjectFromFile ()
		{
		}

		#region IMSBuildProjectImpl implementation

		public override void Load (MonoDevelop.Core.FilePath file)
		{
			foreach (Import im in project.Imports) {
				imports.Add (new ImportData {
					Condition = im.Condition
				});
			}
			foreach (BuildPropertyGroup pg in project.PropertyGroups) {
				var g = new ProperyGroupData (this) {
					Condition = pg.Condition
				};
				foreach (BuildProperty p in pg) {
					var prop = new PropertyData (p.Name, this) {
						Value = p.FinalValue,
						RawValue = p.Value
					};
					g.Properties.Add (prop);
				}
				propertyGroups.Add (g);
			}
			foreach (BuildItemGroup ig in project.ItemGroups) {
				
			}
		}

		public override void AddNewImport (string name, MSBuildImport beforeImport = null)
		{
			var data = new ImportData {
				Target = name
			};
			if (beforeImport != null) {
				var other = (ImportData)beforeImport;
				int i = imports.IndexOf (other);
				if (i != -1) {
					imports.Insert (i, data);
					return;
				}
			}
			imports.Add (data);
		}

		public override void RemoveImport (MSBuildImport import)
		{
			var data = (ImportData)import;
			imports.Remove (data);
		}

		public override void Save (string fileName)
		{
			throw new NotImplementedException ();
		}

		public override string SaveToString ()
		{
			throw new NotImplementedException ();
		}

		public override MSBuildPropertyGroup AddNewPropertyGroup (MSBuildPropertyGroup beforeGroup = null)
		{
			var g = new ProperyGroupData (this);
			if (beforeGroup != null) {
				var i = propertyGroups.IndexOf (beforeGroup);
				if (i != -1) {
					propertyGroups.Insert (i, g);
					return g;
				}
			}
			propertyGroups.Add (g);
			return g;
		}

		public override void RemovePropertyGroup (MSBuildPropertyGroup grp)
		{
			propertyGroups.Remove (grp);
		}

		public override IEnumerable<MSBuildItem> GetAllItems ()
		{
			throw new NotImplementedException ();
		}

		public override IEnumerable<MSBuildItem> GetAllItems (params string[] names)
		{
			throw new NotImplementedException ();
		}

		public override MSBuildItemGroup AddNewItemGroup ()
		{
			throw new NotImplementedException ();
		}

		public override MSBuildItem AddNewItem (string name, string include)
		{
			throw new NotImplementedException ();
		}

		public override System.Xml.XmlElement GetProjectExtensions (string section)
		{
			throw new NotImplementedException ();
		}

		public override void SetProjectExtensions (string section, string value)
		{
			throw new NotImplementedException ();
		}

		public override void RemoveProjectExtensions (string section)
		{
			throw new NotImplementedException ();
		}

		public override void RemoveItem (MSBuildItem item)
		{
			throw new NotImplementedException ();
		}

		public override MonoDevelop.Core.FilePath FileName {
			get {
				throw new NotImplementedException ();
			}
		}

		public override string DefaultTargets {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public override string ToolsVersion {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public override IEnumerable<MSBuildImport> Imports {
			get {
				throw new NotImplementedException ();
			}
		}

		public override IEnumerable<MSBuildPropertyGroup> PropertyGroups {
			get {
				throw new NotImplementedException ();
			}
		}

		public override IEnumerable<MSBuildItemGroup> ItemGroups {
			get {
				throw new NotImplementedException ();
			}
		}

		#endregion
	}
}

