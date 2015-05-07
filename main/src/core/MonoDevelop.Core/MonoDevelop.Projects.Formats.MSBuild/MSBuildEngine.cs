//
// MSBuildEngine.cs
//
// Author:
//       lluis <>
//
// Copyright (c) 2015 lluis
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

#if WINDOWS
using Microsoft.Build.Evaluation;
using MSProject = Microsoft.Build.Evaluation.Project;
using MSProjectItem = Microsoft.Build.Evaluation.ProjectItem;

#else
using Microsoft.Build.BuildEngine;
using MSProject = Microsoft.Build.BuildEngine.Project;
#endif

namespace MonoDevelop.Projects.Formats.MSBuild
{
	abstract class MSBuildEngine
	{
		public static MSBuildEngine Create (bool supportsMSBuild)
		{
			if (!supportsMSBuild)
				return new DefaultMSBuildEngine ();
			else
				return new MSBuildEngineV4 ();
		}

		public abstract object LoadProjectFromXml (MSBuildProject project, string fileName, string xml);

		public abstract IEnumerable<object> GetAllItems (object project, bool includeImported);

		public abstract string GetItemMetadata (object item, string name);

		public abstract string GetEvaluatedMetadata (object item, string name);

		public abstract IEnumerable<object> GetImports (object project);

		public abstract string GetImportEvaluatedProjectPath (object project, object import);

		public abstract IEnumerable<object> GetEvaluatedItemsIgnoringCondition (object project);

		public abstract IEnumerable<object> GetEvaluatedProperties (object project);

		public abstract IEnumerable<object> GetEvaluatedItems (object project);

		public abstract void GetItemInfo (object item, out string name, out string include, out string finalItemSpec, out bool imported);

		public abstract void GetEvaluatedItemInfo (object item, out string name, out string include, out string finalItemSpec, out bool imported);

		public abstract bool GetItemHasMetadata (object item, string name);

		public abstract void GetPropertyInfo (object property, out string name, out string value, out string finalValue);

		public abstract IEnumerable<MSBuildTarget> GetTargets (object project);
	}

	#if WINDOWS
	class MSBuildEngineV4: MSBuildEngine
	{
	public override object LoadProjectFromXml (MSBuildProject project, string fileName, string xml)
		{
			var d = Environment.CurrentDirectory;
			Environment.CurrentDirectory = Path.GetDirectoryName (fileName);
			try {
				ProjectCollection col = new ProjectCollection ();
				var p = col.LoadProject (new XmlTextReader (new StringReader (xml)));
				p.FullPath = fileName;
				return p;
			} finally {
				Environment.CurrentDirectory = d;
			}
		}

		public override IEnumerable<object> GetAllItems (object project, bool includeImported)
		{
			return ((MSProject)project).Items.Where (it => includeImported || !it.IsImported);
		}

		public override string GetItemMetadata (object item, string name)
		{
			var m = ((MSProjectItem)item).GetMetadata(name);
			if (m != null)
				return m.UnevaluatedValue;
			else
				return null;
		}

		public override IEnumerable<object> GetImports (object project)
		{
			return ((MSProject)project).Imports.Cast<object> ();
		}

		public override string GetImportEvaluatedProjectPath (object project, object import)
		{
			return ((ResolvedImport)import).ImportedProject.FullPath;
		}

		public override void GetItemInfo (object item, out string name, out string include, out string finalItemSpec, out bool imported)
		{
			var it = (MSProjectItem)item;
			name = it.ItemType;
			include = it.UnevaluatedInclude;
			if (it.UnevaluatedInclude.Contains ("*"))
				// MSBuild expands wildcards in the evaluated include. We don't want that, unless we are getting evaluated item info.
				finalItemSpec = include;
			else
				finalItemSpec = it.EvaluatedInclude;
			imported = it.IsImported;
		}

		public override void GetEvaluatedItemInfo (object item, out string name, out string include, out string finalItemSpec, out bool imported)
		{
			var it = (MSProjectItem)item;
			name = it.ItemType;
			include = it.UnevaluatedInclude;
			finalItemSpec = it.EvaluatedInclude;
			imported = it.IsImported;
		}

		public override bool GetItemHasMetadata (object item, string name)
		{
			return ((MSProjectItem)item).HasMetadata (name);
		}

		public override string GetEvaluatedMetadata (object item, string name)
		{
			return ((MSProjectItem)item).GetMetadataValue (name);
		}

		public override IEnumerable<object> GetEvaluatedItems (object project)
		{
			return ((MSProject)project).AllEvaluatedItems;
		}

		public override IEnumerable<object> GetEvaluatedItemsIgnoringCondition (object project)
		{
			return ((MSProject)project).ItemsIgnoringCondition;
		}

		public override IEnumerable<object> GetEvaluatedProperties (object project)
		{
			return ((MSProject)project).AllEvaluatedProperties;
		}

		public override void GetPropertyInfo (object property, out string name, out string value, out string finalValue)
		{
			var p = (ProjectProperty)property;
			name = p.Name;
			value = p.UnevaluatedValue;
			finalValue = p.EvaluatedValue;
		}

		public override IEnumerable<MSBuildTarget> GetTargets (object project)
		{
			var doc = new XmlDocument ();
			var p = (MSProject)project;
			foreach (var t in p.Targets) {
				var te = doc.CreateElement (t.Key, MSBuildProject.Schema);
				te.SetAttribute ("Name", t.Key);
				if (!string.IsNullOrEmpty (t.Value.Condition))
					te.SetAttribute ("Condition", t.Value.Condition);
				foreach (var task in t.Value.Tasks) {
					var tke = doc.CreateElement (task.Name, MSBuildProject.Schema);
					tke.SetAttribute ("Name", task.Name);
					if (!string.IsNullOrEmpty (task.Condition))
						tke.SetAttribute ("Condition", task.Condition);
					te.AppendChild (tke);
				}
				yield return new MSBuildTarget (te) {
					IsImported = t.Value.Location.File == p.FullPath
				};
			}
		}
	}


#else

	class MSBuildEngineV4: MSBuildEngine
	{
		public override object LoadProjectFromXml (MSBuildProject p, string fileName, string xml)
		{
			Engine e = new Engine ();
			MSProject project = new MSProject (e);
			project.FullFileName = fileName;
			project.LoadXml (xml);
			return project;
		}

		public override IEnumerable<object> GetAllItems (object project, bool includeImported)
		{
			return ((MSProject)project).ItemGroups.Cast<BuildItemGroup> ().SelectMany (g => g.Cast<BuildItem> ()).Where (it => includeImported || !it.IsImported);
		}

		public override string GetItemMetadata (object item, string name)
		{
			return ((BuildItem)item).GetMetadata (name);
		}

		public override IEnumerable<object> GetImports (object project)
		{
			return ((MSProject)project).Imports.Cast<object> ();
		}

		public override string GetImportEvaluatedProjectPath (object project, object import)
		{
			return ((Import)import).EvaluatedProjectPath;
		}

		public override void GetItemInfo (object item, out string name, out string include, out string finalItemSpec, out bool imported)
		{
			var it = (BuildItem)item;
			name = it.Name;
			include = it.Include;
			finalItemSpec = it.FinalItemSpec;
			imported = it.IsImported;
		}

		public override void GetEvaluatedItemInfo (object item, out string name, out string include, out string finalItemSpec, out bool imported)
		{
			GetItemInfo (item, out name, out include, out finalItemSpec, out imported);
		}

		public override bool GetItemHasMetadata (object item, string name)
		{
			return ((BuildItem)item).HasMetadata (name);
		}

		public override string GetEvaluatedMetadata (object item, string name)
		{
			var it = (BuildItem)item;
			var val = it.GetEvaluatedMetadata (name);
			if (val.IndexOf ('%') != -1)
				return ReplaceMetadata (it, val);
			else
				return val;
		}

		string ReplaceMetadata (BuildItem it, string value)
		{
			// Workaround to xbuild bug. xbuild does not replace well known item metadata

			var sb = new StringBuilder ();
			int i = value.IndexOf ("%(", StringComparison.Ordinal);
			int lastPos = 0;
			while (i != -1) {
				int j = value.IndexOf (')', i + 3);
				if (j != -1) {
					var val = EvaluateMetadata (it, value.Substring (i + 2, j - (i + 2)));
					if (val != null) {
						sb.Append (value, lastPos, i - lastPos);
						sb.Append (val);
						lastPos = j + 1;
					}
				}
				i = value.IndexOf ("%(", i + 2, StringComparison.Ordinal);
			}
			sb.Append (value, lastPos, value.Length - lastPos);
			return sb.ToString ();
		}

		string EvaluateMetadata (BuildItem it, string metadata)
		{
			var d = it.GetEvaluatedMetadata (metadata);
			if (string.IsNullOrEmpty (d))
				return null;
			else
				return d;
		}

		public override IEnumerable<object> GetEvaluatedItems (object project)
		{
			return ((MSProject)project).EvaluatedItems.Cast<object> ();
		}

		public override IEnumerable<object> GetEvaluatedItemsIgnoringCondition (object project)
		{
			return ((MSProject)project).EvaluatedItemsIgnoringCondition.Cast<object> ();
		}

		public override IEnumerable<object> GetEvaluatedProperties (object project)
		{
			return ((MSProject)project).EvaluatedProperties.Cast<object> ();
		}

		public override void GetPropertyInfo (object property, out string name, out string value, out string finalValue)
		{
			var p = (BuildProperty)property;
			name = p.Name;
			value = p.Value;
			finalValue = p.FinalValue;
		}

		public override IEnumerable<MSBuildTarget> GetTargets (object project)
		{
			var doc = new XmlDocument ();
			var p = (MSProject)project;
			foreach (var t in p.Targets.Cast<Target> ()) {
				var te = doc.CreateElement ("Target", MSBuildProject.Schema);
				te.SetAttribute ("Name", t.Name);
				if (!string.IsNullOrEmpty (t.Condition))
					te.SetAttribute ("Condition", t.Condition);
				foreach (var task in t.OfType<BuildTask> ()) {
					var tke = doc.CreateElement (task.Name, MSBuildProject.Schema);
					tke.SetAttribute ("Name", task.Name);
					if (!string.IsNullOrEmpty (task.Condition))
						tke.SetAttribute ("Condition", task.Condition);
					te.AppendChild (tke);
				}
				yield return new MSBuildTarget (te) {
					IsImported = t.IsImported
				};
			}
		}
	}
	#endif


}

