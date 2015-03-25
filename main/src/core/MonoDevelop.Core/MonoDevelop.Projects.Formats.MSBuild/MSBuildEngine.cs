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
		public static MSBuildEngine Create ()
		{
			return new MSBuildEngineV4 ();
		}

		public abstract object LoadProjectFromXml (string fileName, string xml);
		public abstract IEnumerable<object> GetAllItems (object project, bool includeImported);
		public abstract string GetItemMetadata (object item, string name);
		public abstract string GetEvaluatedMetadata (object item, string name);
		public abstract IEnumerable<object> GetImports (object project);
		public abstract string GetImportEvaluatedProjectPath (object import);
		public abstract IEnumerable<object> GetEvaluatedItemsIgnoringCondition (object project);
		public abstract IEnumerable<object> GetEvaluatedProperties (object project);

		public abstract IEnumerable<object> GetEvaluatedItems (object project);
		public abstract void GetItemInfo (object item, out string name, out string include, out string finalItemSpec, out bool imported);
		public abstract void GetEvaluatedItemInfo (object item, out string name, out string include, out string finalItemSpec, out bool imported);
		public abstract bool GetItemHasMetadata (object item, string name);
		public abstract void GetPropertyInfo (object property, out string name, out string value, out string finalValue);
	}

#if WINDOWS
	class MSBuildEngineV4: MSBuildEngine
	{
		public override object LoadProjectFromXml (string fileName, string xml)
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

		public override string GetImportEvaluatedProjectPath (object import)
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
	}

#else

	class MSBuildEngineV4: MSBuildEngine
	{
		public override object LoadProjectFromXml (string fileName, string xml)
		{
			Engine e = new Engine ();
			MSProject project = new MSProject (e);
			project.FullFileName = fileName;
			project.LoadXml (xml);
			return project;
		}

		public override IEnumerable<object> GetAllItems (object project, bool includeImported)
		{
			return ((MSProject)project).ItemGroups.Cast<BuildItemGroup> ().SelectMany (g => g.Cast<BuildItem>()).Where (it => includeImported || !it.IsImported);
		}

		public override string GetItemMetadata (object item, string name)
		{
			return ((BuildItem)item).GetMetadata (name);
		}

		public override IEnumerable<object> GetImports (object project)
		{
			return ((MSProject)project).Imports.Cast<object> ();
		}

		public override string GetImportEvaluatedProjectPath (object import)
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
			return ((BuildItem)item).GetEvaluatedMetadata (name);
		}

		public override IEnumerable<object> GetEvaluatedItems (object project)
		{
			return ((MSProject)project).EvaluatedItems.Cast<object> ();
		}

		public override IEnumerable<object> GetEvaluatedItemsIgnoringCondition (object project)
		{
			return ((MSProject)project).EvaluatedItemsIgnoringCondition.Cast<object>();
		}

		public override IEnumerable<object> GetEvaluatedProperties (object project)
		{
			return ((MSProject)project).EvaluatedProperties.Cast<object>();
		}

		public override void GetPropertyInfo (object property, out string name, out string value, out string finalValue)
		{
			var p = (BuildProperty)property;
			name = p.Name;
			value = p.Value;
			finalValue = p.FinalValue;
		}
	}
#endif
}

