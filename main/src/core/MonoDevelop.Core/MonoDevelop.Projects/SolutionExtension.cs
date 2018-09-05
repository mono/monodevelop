//
// SolutionExtension.cs
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
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	public class SolutionExtension: WorkspaceItemExtension
	{
		SolutionExtension next;

		internal protected override void InitializeChain (ChainedExtension next)
		{
			base.InitializeChain (next);
			this.next = FindNextImplementation<SolutionExtension> (next);
		}

		protected Solution Solution {
			get { return (Solution) base.Item; }
		}

		internal protected override bool SupportsItem (WorkspaceItem item)
		{
			return item is Solution;
		}

		internal protected virtual Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			return next.Build (monitor, configuration, operationContext);
		}

		internal protected virtual Task<BuildResult> Clean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			return next.Clean (monitor, configuration, operationContext);
		}

		[Obsolete("Use the overload that takes a SolutionRunConfiguration argument")]
		internal protected virtual Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return next.Execute (monitor, context, configuration, (SolutionRunConfiguration)context.RunConfiguration);
		}

		[Obsolete ("Use the overload that takes a SolutionRunConfiguration argument")]
		internal protected virtual Task PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return next.PrepareExecution (monitor, context, configuration, (SolutionRunConfiguration)context.RunConfiguration);
		}

		[Obsolete ("Use the overload that takes a SolutionRunConfiguration argument")]
		internal protected virtual bool CanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return next.CanExecute (context, configuration, (SolutionRunConfiguration)context.RunConfiguration);
		}

		internal protected virtual Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
		{
			context.RunConfiguration = runConfiguration;
#pragma warning disable 618 // Type or member is obsolete
			return Execute (monitor, context, configuration);
#pragma warning restore 618 // Type or member is obsolete
		}

		internal protected virtual Task PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
		{
			context.RunConfiguration = runConfiguration;
#pragma warning disable 618 // Type or member is obsolete
			return PrepareExecution (monitor, context, configuration);
#pragma warning restore 618 // Type or member is obsolete
		}

		internal protected virtual bool CanExecute (ExecutionContext context, ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
		{
			context.RunConfiguration = runConfiguration;
#pragma warning disable 618 // Type or member is obsolete
			return CanExecute (context, configuration);
#pragma warning restore 618 // Type or member is obsolete
		}

		internal protected virtual IEnumerable<ExecutionTarget> GetExecutionTargets (Solution solution, ConfigurationSelector configuration)
		{
			return next.GetExecutionTargets (solution, configuration);
		}

		internal protected virtual IEnumerable<ExecutionTarget> GetExecutionTargets (Solution solution, ConfigurationSelector configuration, SolutionRunConfiguration runConfiguration)
		{
			return next.GetExecutionTargets (solution, configuration, runConfiguration);
		}

		internal protected virtual IEnumerable<IBuildTarget> OnGetExecutionDependencies ()
		{
			return next.OnGetExecutionDependencies ();
		}

		internal protected virtual IEnumerable<SolutionRunConfiguration> OnGetRunConfigurations ()
		{
			return next.OnGetRunConfigurations ();
		}

		[Obsolete("This method will be removed in future releases")]
		internal protected virtual bool NeedsBuilding (ConfigurationSelector configuration)
		{
			return next.NeedsBuilding (configuration);
		}

		internal protected virtual void OnReadSolution (ProgressMonitor monitor, SlnFile file)
		{
			var secAttribute = (SolutionDataSectionAttribute) Attribute.GetCustomAttribute (GetType(), typeof(SolutionDataSectionAttribute));
			if (secAttribute != null && secAttribute.ProcessOrder == SlnSectionType.PreProcess) {
				var sec = file.Sections.GetSection (secAttribute.SectionName, SlnSectionType.PreProcess);
				if (sec != null)
					sec.ReadObjectProperties (this);
			}

			next.OnReadSolution (monitor, file);

			if (secAttribute != null && secAttribute.ProcessOrder == SlnSectionType.PostProcess) {
				var sec = file.Sections.GetSection (secAttribute.SectionName, SlnSectionType.PostProcess);
				if (sec != null)
					sec.ReadObjectProperties (this);
			}
		}

		internal protected virtual void OnReadSolutionFolderItemData (ProgressMonitor monitor, SlnPropertySet properties, SolutionFolderItem item)
		{
			next.OnReadSolutionFolderItemData (monitor, properties, item);
		}

		internal protected virtual void OnReadConfigurationData (ProgressMonitor monitor, SlnPropertySet properties, SolutionConfiguration configuration)
		{
			next.OnReadConfigurationData (monitor, properties, configuration);
		}

		internal protected virtual void OnWriteSolution (ProgressMonitor monitor, SlnFile file)
		{
			var secAttribute = (SolutionDataSectionAttribute) Attribute.GetCustomAttribute (GetType(), typeof(SolutionDataSectionAttribute));
			if (secAttribute != null && secAttribute.ProcessOrder == SlnSectionType.PreProcess) {
				var sec = file.Sections.GetOrCreateSection (secAttribute.SectionName, SlnSectionType.PreProcess);
				sec.SkipIfEmpty = true;
				sec.WriteObjectProperties (this);
			}

			next.OnWriteSolution (monitor, file);

			if (secAttribute != null && secAttribute.ProcessOrder == SlnSectionType.PostProcess) {
				var sec = file.Sections.GetOrCreateSection (secAttribute.SectionName, SlnSectionType.PostProcess);
				sec.SkipIfEmpty = true;
				sec.WriteObjectProperties (this);
			}
		}

		internal protected virtual void OnWriteSolutionFolderItemData (ProgressMonitor monitor, SlnPropertySet properties, SolutionFolderItem item)
		{
			next.OnWriteSolutionFolderItemData (monitor, properties, item);
		}

		internal protected virtual void OnWriteConfigurationData (ProgressMonitor monitor, SlnPropertySet properties, SolutionConfiguration configuration)
		{
			next.OnWriteConfigurationData (monitor, properties, configuration);
		}

		internal protected virtual void OnSetFormat (MSBuildFileFormat value)
		{
			next.OnSetFormat (value);
		}

		internal protected virtual bool OnGetSupportsFormat (MSBuildFileFormat format)
		{
			return OnGetSupportsFormat (format);
		}

		internal protected virtual Task OnBeginBuildOperation (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			return next.OnBeginBuildOperation (monitor, configuration, operationContext);
		}

		internal protected virtual Task OnEndBuildOperation (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext, BuildResult result)
		{
			return next.OnEndBuildOperation (monitor, configuration, operationContext, result);
		}
	}
}

