//
// DotNetProjectExtension.cs
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
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	public class DotNetProjectExtension : ProjectExtension
	{
		#region Project properties

		DotNetProjectExtension next;

		internal protected override void InitializeChain (ChainedExtension next)
		{
			base.InitializeChain (next);
			this.next = FindNextImplementation<DotNetProjectExtension> (next);
		}

		internal protected override bool SupportsObject (WorkspaceObject item)
		{
			return base.SupportsObject (item) && (item is DotNetProject);
		}

		new public DotNetProject Project {
			get { return (DotNetProject)base.Item; }
		}


		internal protected virtual DotNetProjectFlags OnGetDotNetProjectFlags ()
		{
			return next.OnGetDotNetProjectFlags ();
		}

		#endregion

		internal protected virtual bool OnGetCanReferenceProject (DotNetProject targetProject, out string reason)
		{
			return next.OnGetCanReferenceProject (targetProject, out reason);
		}

		internal protected virtual string OnGetDefaultTargetPlatform (ProjectCreateInformation projectCreateInfo)
		{
			return next.OnGetDefaultTargetPlatform (projectCreateInfo);
		}

		internal protected virtual Task<List<AssemblyReference>> OnGetReferencedAssemblies (ConfigurationSelector configuration)
		{
			return next.OnGetReferencedAssemblies (configuration);
		}

		internal protected virtual Task<List<AssemblyReference>> OnGetReferences (ConfigurationSelector configuration, CancellationToken token)
		{
			return next.OnGetReferences (configuration, token);
		}

		internal protected virtual IEnumerable<DotNetProject> OnGetReferencedAssemblyProjects (ConfigurationSelector configuration)
		{
			return next.OnGetReferencedAssemblyProjects (configuration);
		}

		[Obsolete("User overload that takes a RunConfiguration")]
		internal protected virtual ExecutionCommand OnCreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration)
		{
			return next.OnCreateExecutionCommand (configSel, configuration);
		}

		internal protected virtual ExecutionCommand OnCreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			return next.OnCreateExecutionCommand (configSel, configuration, runConfiguration);
		}

		internal protected virtual void OnReferenceRemovedFromProject (ProjectReferenceEventArgs e)
		{
			next.OnReferenceRemovedFromProject (e);
		}

		internal protected virtual void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
		{
			next.OnReferenceAddedToProject (e);
		}

		internal protected virtual void OnReferencedAssembliesChanged ()
		{
			next.OnReferencedAssembliesChanged ();
		}

		internal protected virtual string OnGetDefaultResourceId (ProjectFile projectFile)
		{
			return next.OnGetDefaultResourceId (projectFile);
		}

		internal protected virtual Task OnExecuteCommand (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, ExecutionCommand executionCommand)
		{
			return next.OnExecuteCommand (monitor, context, configuration, executionCommand);
		}

		#region Framework management

		internal protected virtual TargetFrameworkMoniker OnGetDefaultTargetFrameworkId ()
		{
			return next.OnGetDefaultTargetFrameworkId ();
		}

		internal protected virtual TargetFrameworkMoniker OnGetDefaultTargetFrameworkForFormat (string toolsVersion)
		{
			return next.OnGetDefaultTargetFrameworkForFormat (toolsVersion);
		}

		internal protected virtual bool OnGetSupportsFramework (TargetFramework framework)
		{
			return next.OnGetSupportsFramework (framework);
		}

		internal protected virtual Task<BuildResult> OnCompile (ProgressMonitor monitor, BuildData buildData)
		{
			return next.OnCompile (monitor, buildData);
		}

		#endregion
	}
}

