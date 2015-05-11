//
// MSBuildEngineV4.cs
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
using System.Text;

using Microsoft.Build.BuildEngine;
using MSProject = Microsoft.Build.BuildEngine.Project;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	#if !WINDOWS
	class MSBuildEngineV4: MSBuildEngine
	{
		Engine engine;

		public MSBuildEngineV4 (MSBuildEngineManager manager): base (manager)
		{
			engine = new Engine ();
		}

		public override object LoadProject (MSBuildProject p, XmlDocument doc, FilePath fileName)
		{
			lock (engine) {
				engine.GlobalProperties.Clear ();

				var project = new MSProject (engine);
				project.BuildEnabled = false;
				project.FullFileName = fileName;
				project.LoadXml (doc.OuterXml);
				return project;
			}
		}

		public override void UnloadProject (object project)
		{
		}

		public override object CreateProjectInstance (object project)
		{
			return project;
		}

		public override void DisposeProjectInstance (object projectInstance)
		{
			// Don't unload, since we are using the same instance as the project
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

		public override string GetEvaluatedItemMetadata (object item, string name)
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

		public override void SetGlobalProperty (object project, string property, string value)
		{
			var p = (MSProject)project;
			p.GlobalProperties.SetProperty (property, value);
		}

		public override void RemoveGlobalProperty (object project, string property)
		{
			var p = (MSProject)project;
			p.GlobalProperties.RemoveProperty (property);
		}
	}
	#endif
}
