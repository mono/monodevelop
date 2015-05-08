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

namespace MonoDevelop.Projects.Formats.MSBuild
{
	class DefaultMSBuildEngine: MSBuildEngine
	{
		class ProjectInfo
		{
			public MSBuildProject Project;
			public MSBuildEvaluationContext Context;
			public List<MSBuildItem> EvaluatedItemsIgnoringCondition = new List<MSBuildItem> ();
			public List<MSBuildItem> EvaluatedItems = new List<MSBuildItem> ();
			public Dictionary<string,PropertyInfo> Properties = new Dictionary<string, PropertyInfo> ();
			public Dictionary<MSBuildImport,string> Imports = new Dictionary<MSBuildImport, string> ();
		}

		class PropertyInfo
		{
			public string Name;
			public string Value;
			public string FinalValue;
		}

		#region implemented abstract members of MSBuildEngine

		public override object LoadProjectFromXml (MSBuildProject project, string fileName, string xml)
		{
			var context = new MSBuildEvaluationContext ();
			var pi = new ProjectInfo {
				Project = project,
				Context = context
			};

			return pi;
		}

		public override void Evaluate (object project)
		{
			var pi = (ProjectInfo)project;
			pi.Context.InitEvaluation (pi.Project);
			foreach (var pg in pi.Project.PropertyGroups)
				Evaluate (pi, pi.Context, pg);
			foreach (var pg in pi.Project.ItemGroups)
				Evaluate (pi, pi.Context, pg);
			foreach (var i in pi.Project.Imports)
				Evaluate (pi, pi.Context, i);
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

			foreach (var prop in group.GetProperties ())
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
				if (conditionIsTrue && (string.IsNullOrEmpty (it.Condition) || ConditionParser.ParseAndEvaluate (it.Condition, context)))
					project.EvaluatedItems.Add (it);
				project.EvaluatedItemsIgnoringCondition.Add (it);
			}
		}

		void Evaluate (ProjectInfo project, MSBuildEvaluationContext context, MSBuildProperty prop)
		{
			XmlElement e;
			context.Evaluate (prop.Element, out e);
			var ep = new MSBuildProperty (project.Project, e);
			if (string.IsNullOrEmpty (ep.Condition) || ConditionParser.ParseAndEvaluate (ep.Condition, context)) {
				project.Properties [ep.Name] = new PropertyInfo { Name = ep.Name, Value = prop.Element.Value, FinalValue = ep.Value };
				context.SetPropertyValue (prop.Name, ep.Value);
			}
		}

		MSBuildItem Evaluate (ProjectInfo project, MSBuildEvaluationContext context, MSBuildItem item)
		{
			XmlElement e;
			context.Evaluate (item.Element, out e);
			return new MSBuildItem (project.Project, e);
		}

		void Evaluate (ProjectInfo project, MSBuildEvaluationContext context, MSBuildImport import)
		{
			project.Imports [import] = context.EvaluateString (import.Project);
		}

		public override string GetItemMetadata (object item, string name)
		{
			MSBuildItem it = (MSBuildItem) item;
			return it.Metadata.GetValue (name);
		}

		public override string GetEvaluatedMetadata (object item, string name)
		{
			MSBuildItem it = (MSBuildItem) item;
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
			var it = (MSBuildItem) item;
			name = it.Name;
			include = it.Include;
			finalItemSpec = it.Include;
			imported = false;
		}

		public override bool GetItemHasMetadata (object item, string name)
		{
			var it = (MSBuildItem) item;
			return it.Metadata.HasProperty (name);
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
			pi.Context.SetPropertyValue (property, value);
			pi.Properties [property] = new PropertyInfo { Name = property, Value = value, FinalValue = value };
		}

		public override void RemoveGlobalProperty (object project, string property)
		{
			var pi = (ProjectInfo)project;
			pi.Context.ClearPropertyValue (property);
			pi.Properties.Remove (property);
		}

		#endregion
	}
}
