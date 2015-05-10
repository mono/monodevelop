//
// DefaultMSBuildEngine.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Linq;
using System.Xml;
using System.IO;
using System.Text;
using Microsoft.Build.BuildEngine;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	class DefaultMSBuildEngine: MSBuildEngine
	{
		internal static DefaultMSBuildEngine Instance = new DefaultMSBuildEngine ();

		class ProjectInfo
		{
			public MSBuildProject Project;
			public List<IMSBuildItemEvaluated> EvaluatedItemsIgnoringCondition = new List<IMSBuildItemEvaluated> ();
			public List<IMSBuildItemEvaluated> EvaluatedItems = new List<IMSBuildItemEvaluated> ();
			public Dictionary<string,PropertyInfo> Properties = new Dictionary<string, PropertyInfo> ();
			public Dictionary<MSBuildImport,string> Imports = new Dictionary<MSBuildImport, string> ();
			public Dictionary<string,string> GlobalProperties = new Dictionary<string, string> ();
		}

		class PropertyInfo
		{
			public string Name;
			public string Value;
			public string FinalValue;
		}

		#region implemented abstract members of MSBuildEngine

		public override object LoadProject (MSBuildProject project, string fileName)
		{
			var pi = new ProjectInfo {
				Project = project
			};

			return pi;
		}

		public override void UnloadProject (object project)
		{
			
		}

		public override object CreateProjectInstance (object project)
		{
			return project;
		}

		public override void Evaluate (object project)
		{
			var pi = (ProjectInfo)project;

			pi.EvaluatedItemsIgnoringCondition.Clear ();
			pi.EvaluatedItems.Clear ();
			pi.Properties.Clear ();
			pi.Imports.Clear ();

			var context = new MSBuildEvaluationContext ();
			foreach (var p in pi.GlobalProperties) {
				context.SetPropertyValue (p.Key, p.Value);
				pi.Properties [p.Key] = new PropertyInfo { Name = p.Key, Value = p.Value, FinalValue = p.Value };
			}

			context.InitEvaluation (pi.Project);
			foreach (var pg in pi.Project.PropertyGroups)
				Evaluate (pi, context, pg);
			foreach (var pg in pi.Project.ItemGroups)
				Evaluate (pi, context, pg);
			foreach (var i in pi.Project.Imports)
				Evaluate (pi, context, i);
		}

		void Evaluate (ProjectInfo project, MSBuildEvaluationContext context, MSBuildPropertyGroup group)
		{
			if (!string.IsNullOrEmpty (group.Condition)) {
				string cond;
				if (!context.Evaluate (group.Condition, out cond)) {
					// The condition could not be evaluated. Clear all properties that this group defines
					// since we don't know if they will have a value or not
					foreach (var prop in group.GetProperties ())
						context.ClearPropertyValue (prop.Name);
					return;
				}
				if (!ConditionParser.ParseAndEvaluate (cond, context))
					return;
			}

			foreach (var prop in group.Element.ChildNodes.OfType<XmlElement> ())
				Evaluate (project, context, prop);
		}

		void Evaluate (ProjectInfo project, MSBuildEvaluationContext context, MSBuildItemGroup items)
		{
			bool conditionIsTrue = true;

			if (!string.IsNullOrEmpty (items.Condition)) {
				string cond;
				if (context.Evaluate (items.Condition, out cond))
					conditionIsTrue = ConditionParser.ParseAndEvaluate (cond, context);
			}

			foreach (var item in items.Items) {
				var it = Evaluate (project, context, item);
				var trueCond = conditionIsTrue && (string.IsNullOrEmpty (it.Condition) || ConditionParser.ParseAndEvaluate (it.Condition, context));
				if (it.Include.IndexOf ('*') != -1) {
					var path = it.Include;
					if (path == "**" || path.EndsWith ("\\**"))
						path = path + "/*";
					var subpath = path.Split ('\\');
					foreach (var eit in ExpandWildcardFilePath (project.Project, context, item, project.Project.BaseDirectory, FilePath.Null, false, subpath, 0)) {
						project.EvaluatedItemsIgnoringCondition.Add (eit);
						if (trueCond)
							project.EvaluatedItems.Add (eit);
					}
				} else {
					project.EvaluatedItemsIgnoringCondition.Add (it);
					if (trueCond)
						project.EvaluatedItems.Add (it);
				}
			}
		}

		static IEnumerable<IMSBuildItemEvaluated> ExpandWildcardFilePath (MSBuildProject project, MSBuildEvaluationContext context, MSBuildItem sourceItem, FilePath basePath, FilePath baseRecursiveDir, bool recursive, string[] filePath, int index)
		{
			var res = Enumerable.Empty<IMSBuildItemEvaluated> ();

			if (index >= filePath.Length)
				return res;

			var path = filePath [index];

			if (path == "..")
				return ExpandWildcardFilePath (project, context, sourceItem, basePath.ParentDirectory, baseRecursiveDir, recursive, filePath, index + 1);
			
			if (path == ".")
				return ExpandWildcardFilePath (project, context, sourceItem, basePath, baseRecursiveDir, recursive, filePath, index + 1);

			if (!Directory.Exists (basePath))
				return res;

			if (path == "**") {
				// if this is the last component of the path, there isn't any file specifier, so there is no possible match
				if (index + 1 >= filePath.Length)
					return res;
				
				// If baseRecursiveDir has already been set, don't overwrite it.
				if (baseRecursiveDir.IsNullOrEmpty)
					baseRecursiveDir = basePath;
				
				return ExpandWildcardFilePath (project, context, sourceItem, basePath, baseRecursiveDir, true, filePath, index + 1);
			}

			if (recursive) {
				// Recursive search. Try to match the remaining subpath in all subdirectories.
				foreach (var dir in Directory.GetDirectories (basePath))
					res = res.Concat (ExpandWildcardFilePath (project, context, sourceItem, dir, baseRecursiveDir, true, filePath, index));
			}

			if (index == filePath.Length - 1) {
				// Last path component. It has to be a file specifier.
				string baseDir = basePath.ToRelative (project.BaseDirectory).ToString().Replace ('/','\\');
				res = res.Concat (Directory.GetFiles (basePath, path).Select (f => {
					context.SetItemContext (f, basePath.ToRelative (baseRecursiveDir));
					XmlElement e;
					context.Evaluate (sourceItem.Element, out e);
					var ev = baseDir + "\\" + Path.GetFileName (f);
					return CreateEvaluatedItem (project, sourceItem, e, ev);
				}));
			}
			else {
				// Directory specifier
				// Look for matching directories.
				// The search here is non-recursive, not matter what the 'recursive' parameter says, since we are trying to match a subpath.
				// The recursive search is done below.

				if (path.IndexOfAny (wildcards) != -1) {
					foreach (var dir in Directory.GetDirectories (basePath, path))
						res = res.Concat (ExpandWildcardFilePath (project, context, sourceItem, dir, baseRecursiveDir, false, filePath, index + 1));
				} else
					res = res.Concat (ExpandWildcardFilePath (project, context, sourceItem, basePath.Combine (path), baseRecursiveDir, false, filePath, index + 1));
			}

			return res;
		}

		static MSBuildItemEvaluated CreateEvaluatedItem (MSBuildProject project, MSBuildItem sourceItem, XmlElement e, string include)
		{
			var it = new MSBuildItemEvaluated (project, e.LocalName, sourceItem.Include, include);
			var md = new Dictionary<string,MSBuildPropertyEvaluated> ();
			foreach (var c in e.ChildNodes.OfType<XmlElement> ()) {
				var cm = sourceItem.Element.ChildNodes.OfType<XmlElement> ().FirstOrDefault (me => me.LocalName == c.LocalName);
				md [c.LocalName] = new MSBuildPropertyEvaluated (project, c.LocalName, cm.InnerXml, c.InnerXml);
			}
			((MSBuildPropertyGroupEvaluated)it.Metadata).SetProperties (md);
			it.SourceItem = sourceItem;
			it.Condition = sourceItem.Condition;
			return it;
		}

		static char[] wildcards = { '*', '%' };

		void Evaluate (ProjectInfo project, MSBuildEvaluationContext context, XmlElement prop)
		{
			XmlElement e;
			context.Evaluate (prop, out e);
			var ep = new MSBuildProperty (project.Project, e);
			if (string.IsNullOrEmpty (ep.Condition) || ConditionParser.ParseAndEvaluate (ep.Condition, context)) {
				project.Properties [ep.Name] = new PropertyInfo { Name = ep.Name, Value = prop.Value, FinalValue = ep.Value };
				context.SetPropertyValue (prop.Name, ep.Value);
			}
		}

		MSBuildItemEvaluated Evaluate (ProjectInfo project, MSBuildEvaluationContext context, MSBuildItem item)
		{
			XmlElement e;
			context.Evaluate (item.Element, out e);
			return CreateEvaluatedItem (project.Project, item, e, e.GetAttribute ("Include"));
		}

		void Evaluate (ProjectInfo project, MSBuildEvaluationContext context, MSBuildImport import)
		{
			project.Imports [import] = context.EvaluateString (import.Project);
		}

		public override bool GetItemHasMetadata (object item, string name)
		{
			var it = item as MSBuildItem;
			if (it != null)
				return it.Metadata.HasProperty (name);
			return ((IMSBuildItemEvaluated) item).Metadata.HasProperty (name);
		}

		public override string GetItemMetadata (object item, string name)
		{
			var it = item as MSBuildItem;
			if (it != null)
				return it.Metadata.GetValue (name);
			return ((IMSBuildItemEvaluated)item).Metadata.GetValue (name);
		}

		public override string GetEvaluatedItemMetadata (object item, string name)
		{
			IMSBuildItemEvaluated it = (IMSBuildItemEvaluated) item;
			return it.Metadata.GetValue (name);
		}

		public override IEnumerable<object> GetImports (object project)
		{
			return ((ProjectInfo)project).Project.Imports;
		}

		public override string GetImportEvaluatedProjectPath (object project, object import)
		{
			return ((ProjectInfo)project).Imports [(MSBuildImport)import];
		}

		public override IEnumerable<object> GetAllItems (object project, bool includeImported)
		{
			return ((ProjectInfo)project).Project.GetAllItems ();
		}

		public override IEnumerable<object> GetEvaluatedItems (object project)
		{
			return ((ProjectInfo)project).EvaluatedItems;
		}

		public override IEnumerable<object> GetEvaluatedItemsIgnoringCondition (object project)
		{
			return ((ProjectInfo)project).EvaluatedItemsIgnoringCondition;
		}

		public override IEnumerable<object> GetEvaluatedProperties (object project)
		{
			return ((ProjectInfo)project).Properties.Values;
		}

		public override void GetItemInfo (object item, out string name, out string include, out string finalItemSpec, out bool imported)
		{
			var it = (MSBuildItem) item;
			name = it.Name;
			include = it.Include;
			finalItemSpec = it.Include;
			imported = false;
		}

		public override void GetEvaluatedItemInfo (object item, out string name, out string include, out string finalItemSpec, out bool imported)
		{
			var it = (IMSBuildItemEvaluated) item;
			name = it.Name;
			include = it.Include;
			finalItemSpec = it.Include;
			imported = false;
		}

		public override void GetPropertyInfo (object property, out string name, out string value, out string finalValue)
		{
			var prop = (PropertyInfo)property;
			name = prop.Name;
			value = prop.Value;
			finalValue = prop.FinalValue;
		}

		public override IEnumerable<MSBuildTarget> GetTargets (object project)
		{
			var doc = ((ProjectInfo)project).Project.Document;
			foreach (XmlElement elem in doc.DocumentElement.SelectNodes ("tns:Target", MSBuildProject.XmlNamespaceManager))
				yield return new MSBuildTarget (elem);

			// Return dummy Build and Clean targets

			foreach (var t in new [] { "Build", "Clean" }) {
				var te = doc.CreateElement ("Target", MSBuildProject.Schema);
				te.SetAttribute ("Name", t);
				yield return new MSBuildTarget (te);
			}
		}

		public override void SetGlobalProperty (object project, string property, string value)
		{
			var pi = (ProjectInfo)project;
			pi.GlobalProperties [property] = value;
		}

		public override void RemoveGlobalProperty (object project, string property)
		{
			var pi = (ProjectInfo)project;
			pi.GlobalProperties.Remove (property);
		}

		#endregion
	}
}
