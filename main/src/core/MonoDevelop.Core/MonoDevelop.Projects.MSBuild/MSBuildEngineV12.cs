//
// MSBuildEngineV12.cs
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

using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using MSProject = Microsoft.Build.Evaluation.Project;
using MSProjectItem = Microsoft.Build.Evaluation.ProjectItem;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.MSBuild
{

	class MSBuildEngineV12: MSBuildEngine
	{
		ProjectCollection projects;

		public MSBuildEngineV12 (MSBuildEngineManager manager): base (manager)
		{
			projects = new ProjectCollection ();
		}

		public override object LoadProject (MSBuildProject project, string xml, FilePath fileName)
		{
			var d = Environment.CurrentDirectory;
			Environment.CurrentDirectory = Path.GetDirectoryName (fileName);
			try {
				var p = projects.LoadProject (new XmlTextReader (new StringReader (xml)));
				p.FullPath = fileName;
				return p;
			} finally {
				Environment.CurrentDirectory = d;
			}
		}

		public override void UnloadProject (object project)
		{
			projects.UnloadProject ((MSProject)project);
		}

		public override object CreateProjectInstance (object project)
		{
			return project;
		}

		public override void DisposeProjectInstance (object projectInstance)
		{
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

		public override string GetEvaluatedItemMetadata (object item, string name)
		{
			return ((MSProjectItem)item).GetMetadataValue (name);
		}

		public override IEnumerable<string> GetItemMetadataNames (object item)
		{
			return ((MSProjectItem)item).Metadata.Select (m => m.Name);
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

		public override void GetPropertyInfo (object property, out string name, out string value, out string finalValue, out bool definedMultipleTimes)
		{
			var p = (ProjectProperty)property;
			name = p.Name;
			value = p.UnevaluatedValue;
			finalValue = p.EvaluatedValue;
			definedMultipleTimes = true;
		}

		public override IEnumerable<MSBuildTarget> GetTargets (object project)
		{
			var p = (MSProject)project;
			foreach (var t in p.Targets) {
				List<MSBuildTask> tasks = new List<MSBuildTask> ();
				foreach (var task in t.Value.Tasks)
					tasks.Add (new MSBuildTask (task.Name) { Condition = task.Condition });
				yield return new MSBuildTarget (t.Key, tasks) {
					IsImported = t.Value.Location.File == p.FullPath,
					Condition = t.Value.Condition
				};
			}
		}

		public override IEnumerable<MSBuildTarget> GetTargetsIgnoringCondition (object projectInstance)
		{
			throw new NotImplementedException ();
		}

		public override void SetGlobalProperty (object project, string property, string value)
		{
			var p = (MSProject)project;
			p.SetGlobalProperty (property, value);
		}

		public override void RemoveGlobalProperty (object project, string property)
		{
			var p = (MSProject)project;
			p.RemoveGlobalProperty (property);
		}

		public override ConditionedPropertyCollection GetConditionedProperties (object project)
		{
			throw new NotImplementedException ();
		}

		public override IEnumerable<MSBuildItem> FindGlobItemsIncludingFile (object projectInstance, string filePath)
		{
			throw new NotImplementedException ();
		}

		internal override IEnumerable<MSBuildItem> FindUpdateGlobItemsIncludingFile (object projectInstance, string include, MSBuildItem globItem)
		{
			throw new NotImplementedException ();
		}
	}

	#if !WINDOWS
	
	#endif
}
