//
// MSBuildProjectExtension.cs
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
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Threading.Tasks;
using MonoDevelop.Projects.Formats.MSBuild;
using Mono.Addins;
using System.Linq;
using System.Collections.Immutable;

namespace MonoDevelop.Projects
{
	public class ProjectExtension: SolutionItemExtension
	{
		ProjectExtension next;

		public Project Project {
			get { return (Project) base.Item; }
		}

		internal protected override void InitializeChain (ChainedExtension next)
		{
			base.InitializeChain (next);
			this.next = FindNextImplementation<ProjectExtension> (next);
		}

		internal protected override bool SupportsObject (WorkspaceObject item)
		{
			return item is Project && base.SupportsObject (item);
		}

		internal protected virtual bool SupportsFlavor (string guid)
		{
			if (FlavorGuid != null && guid.Equals (FlavorGuid, StringComparison.OrdinalIgnoreCase))
				return true;
			return next.SupportsFlavor (guid);
		}

		internal protected virtual Task<TargetEvaluationResult> OnRunTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			return next.OnRunTarget (monitor, target, configuration, context);
		}

		internal protected virtual bool OnGetSupportsTarget (string target)
		{
			return next.OnGetSupportsTarget (target);
		}

		internal protected virtual void OnGetDefaultImports (List<string> imports)
		{
			next.OnGetDefaultImports (imports);
		}

		internal protected virtual void OnGetTypeTags (HashSet<string> types)
		{
			next.OnGetTypeTags (types);
			if (TypeAlias != null)
				types.Add (TypeAlias);
		}

		/// <summary>
		/// Called just after the MSBuild project is loaded but before it is evaluated.
		/// </summary>
		/// <param name="project">The project</param>
		/// <remarks>
		/// Subclasses can override this method to transform the MSBuild project before it is evaluated.
		/// For example, it can be used to add or remove imports, or to set custom values for properties.
		/// Changes done in the MSBuild files are not saved.
		/// </remarks>
		internal protected virtual void OnPrepareForEvaluation (MSBuildProject project)
		{
			next.OnPrepareForEvaluation (project);
		}

		internal protected virtual void OnReadProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			next.OnReadProject (monitor, msproject);
			msproject.GetGlobalPropertyGroup ().ReadObjectProperties (this, GetType (), true);
			msproject.ReadExternalProjectProperties (this, GetType (), true);
		}

		internal protected virtual void OnReadConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IMSBuildEvaluatedPropertyCollection pset)
		{
			next.OnReadConfiguration (monitor, config, pset);
		}

		internal protected virtual void OnWriteProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			next.OnWriteProject (monitor, msproject);
			msproject.GetGlobalPropertyGroup ().WriteObjectProperties (this, GetType (), true);
			msproject.WriteExternalProjectProperties (this, GetType (), true);
		}

		internal protected virtual void OnWriteConfiguration (ProgressMonitor monitor, ProjectConfiguration config, IMSBuildPropertySet pset)
		{
			next.OnWriteConfiguration (monitor, config, pset);
		}

		#region Building

		internal protected virtual bool OnGetIsCompileable (string fileName)
		{
			return next.OnGetIsCompileable (fileName);
		}

		internal protected virtual bool OnGetIsCompileBuildAction (string buildAction)
		{
			return next.OnGetIsCompileBuildAction (buildAction);
		}

		internal protected virtual string OnGetDefaultBuildAction (string fileName)
		{
			return next.OnGetDefaultBuildAction (fileName);
		}

		internal protected virtual IEnumerable<string> OnGetStandardBuildActions ()
		{
			return next.OnGetStandardBuildActions ();
		}

		internal protected virtual IList<string> OnGetCommonBuildActions ()
		{
			return next.OnGetCommonBuildActions ();
		}

		internal protected virtual ProjectItem OnCreateProjectItem (IMSBuildItemEvaluated item)
		{
			return next.OnCreateProjectItem (item);
		}

		internal protected virtual void OnPopulateSupportFileList (FileCopySet list, ConfigurationSelector configuration)
		{
			next.OnPopulateSupportFileList (list, configuration);
		}

		internal protected virtual void OnPopulateOutputFileList (List<FilePath> list, ConfigurationSelector configuration)
		{
			next.OnPopulateOutputFileList (list, configuration);
		}

		internal protected virtual FilePath OnGetOutputFileName (ConfigurationSelector configuration)
		{
			return next.OnGetOutputFileName (configuration);
		}

		internal protected virtual string[] SupportedLanguages {
			get {
				return next.SupportedLanguages;
			}
		}

		internal protected virtual bool OnFastCheckNeedsBuild (ConfigurationSelector configuration)
		{
			return next.OnFastCheckNeedsBuild (configuration);
		}

		#endregion

		#region Events

		internal protected virtual void OnFileRemovedFromProject (ProjectFileEventArgs e)
		{
			next.OnFileRemovedFromProject (e);
		}

		internal protected virtual void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			next.OnFileAddedToProject (e);
		}

		internal protected virtual void OnFileChangedInProject (ProjectFileEventArgs e)
		{
			next.OnFileChangedInProject (e);
		}

		internal protected virtual void OnFilePropertyChangedInProject (ProjectFileEventArgs e)
		{
			next.OnFilePropertyChangedInProject (e);
		}

		internal protected virtual void OnFileRenamedInProject (ProjectFileRenamedEventArgs e)
		{
			next.OnFileRenamedInProject (e);
		}

		#endregion
	}
}

