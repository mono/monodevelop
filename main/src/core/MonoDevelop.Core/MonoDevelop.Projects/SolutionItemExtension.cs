//
// SolutionItemExtension.cs
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
using MonoDevelop.Core.StringParsing;
using MonoDevelop.Core.Execution;
using System.Xml;
using MonoDevelop.Projects.MSBuild;
using MonoDevelop.Projects.Extensions;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Immutable;

namespace MonoDevelop.Projects
{
	public class SolutionItemExtension: WorkspaceObjectExtension
	{
		SolutionItemExtension next;

		internal string FlavorGuid { get; set; }
		internal string ProjectCapability { get; set; }
		internal string TypeAlias { get; set; }
		internal string LanguageName { get; set; }

		internal protected override void InitializeChain (ChainedExtension next)
		{
			base.InitializeChain (next);
			this.next = FindNextImplementation<SolutionItemExtension> (next);
		}

		internal protected override bool SupportsObject (WorkspaceObject item)
		{
			if (!base.SupportsObject (item))
				return false;
			
			var p = item as SolutionItem;
			if (p == null)
				return false;

			var pr = item as Project;

			if (pr != null && ProjectCapability != null) {
				if (!pr.IsCapabilityMatch (ProjectCapability))
					return false;
			}
			if (FlavorGuid != null) {
				if (!p.GetItemTypeGuids ().Any (id => id.Equals (FlavorGuid, StringComparison.OrdinalIgnoreCase)))
					return false;
			}

			var dnp = item as DotNetProject;
			if (LanguageName == null || dnp == null)
				return true;
			
			return LanguageName == dnp.LanguageName;
		}

		public SolutionItem Item {
			get { return (SolutionItem) Owner; }
		}

		internal protected virtual void OnInitializeFromTemplate (ProjectCreateInformation projectCreateInfo, XmlElement template)
		{
			next.OnInitializeFromTemplate (projectCreateInfo, template);
		}

		internal void ItemReady ()
		{
			OnItemReady ();
			if (next != null)
				next.ItemReady ();
		}

		protected virtual void OnItemReady ()
		{
		}

		internal void BeginLoad ()
		{
			OnBeginLoad ();
			if (next != null)
				next.BeginLoad ();
		}

		internal void EndLoad ()
		{
			OnEndLoad ();
			if (next != null)
				next.EndLoad ();
		}

		#region Project properties

		internal protected virtual IconId StockIcon {
			get {
				return next.StockIcon;
			}
		}
		#endregion

		#region Project model

		internal protected virtual IEnumerable<IBuildTarget> OnGetExecutionDependencies ()
		{
			return next.OnGetExecutionDependencies ();
		}

		internal protected virtual IEnumerable<SolutionItem> OnGetReferencedItems (ConfigurationSelector configuration)
		{
			return next.OnGetReferencedItems (configuration);
		}

		internal protected virtual void OnSetFormat (MSBuildFileFormat format)
		{
			next.OnSetFormat (format);
		}

		internal protected virtual bool OnGetSupportsFormat (MSBuildFileFormat format)
		{
			return next.OnGetSupportsFormat (format);
		}

		internal protected virtual IEnumerable<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			return next.OnGetItemFiles (includeReferencedFiles);
		}

		internal protected virtual bool ItemFilesChanged {
			get {
				return next.ItemFilesChanged;
			}
		}

		internal protected virtual SolutionItemConfiguration OnCreateConfiguration (string id, ConfigurationKind kind)
		{
			return next.OnCreateConfiguration (id, kind);
		}

		internal protected virtual ProjectFeatures OnGetSupportedFeatures ()
		{
			return next.OnGetSupportedFeatures ();
		}

		#endregion

		#region Building

		internal protected virtual Task<BuildResult> OnClean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			return next.OnClean (monitor, configuration, operationContext);
		}

		internal protected virtual bool OnNeedsBuilding (ConfigurationSelector configuration)
		{
			return next.OnNeedsBuilding (configuration);
		}

		internal protected virtual Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			return next.OnBuild (monitor, configuration, operationContext);
		}

		internal protected virtual DateTime OnGetLastBuildTime (ConfigurationSelector configuration)
		{
			return next.OnGetLastBuildTime (configuration);
		}

		#endregion

		#region Load / Save

		internal protected virtual Task OnLoad (ProgressMonitor monitor)
		{
			return next.OnLoad (monitor);
		}

		internal protected virtual Task OnSave (ProgressMonitor monitor)
		{
			return next.OnSave (monitor);
		}

		protected virtual void OnBeginLoad ()
		{
		}

		protected virtual void OnEndLoad ()
		{
		}

		internal protected virtual void OnReadSolutionData (ProgressMonitor monitor, SlnPropertySet properties)
		{
			next.OnReadSolutionData (monitor, properties);
		}

		internal protected virtual void OnWriteSolutionData (ProgressMonitor monitor, SlnPropertySet properties)
		{
			next.OnWriteSolutionData (monitor, properties);
		}

		internal protected virtual bool OnCheckHasSolutionData ()
		{
			return next.OnCheckHasSolutionData ();
		}

		internal protected virtual Task OnClearCachedData ()
		{
			return next.OnClearCachedData ();
		}

		#endregion

		#region Execution

		[Obsolete ("Use overload that takes a RunConfiguration")]
		internal protected virtual Task OnPrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return next.OnPrepareExecution (monitor, context, configuration, (SolutionItemRunConfiguration)context.RunConfiguration);
		}

		[Obsolete ("Use overload that takes a RunConfiguration")]
		internal protected virtual Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return next.OnExecute (monitor, context, configuration, (SolutionItemRunConfiguration)context.RunConfiguration);
		}

		[Obsolete ("Use overload that takes a RunConfiguration")]
		internal protected virtual bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return next.OnGetCanExecute (context, configuration, (SolutionItemRunConfiguration)context.RunConfiguration);
		}

		internal protected virtual Task OnPrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			context.RunConfiguration = runConfiguration;
#pragma warning disable 618 // Type or member is obsolete
			return OnPrepareExecution (monitor, context, configuration);
#pragma warning restore 618 // Type or member is obsolete
		}

		internal protected virtual Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			context.RunConfiguration = runConfiguration;
#pragma warning disable 618 // Type or member is obsolete
			return OnExecute (monitor, context, configuration);
#pragma warning restore 618 // Type or member is obsolete
		}

		internal protected virtual bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			context.RunConfiguration = runConfiguration;
#pragma warning disable 618 // Type or member is obsolete
			return OnGetCanExecute (context, configuration);
#pragma warning restore 618 // Type or member is obsolete
		}

		internal protected virtual IEnumerable<ExecutionTarget> OnGetExecutionTargets (ConfigurationSelector configuration)
		{
			return next.OnGetExecutionTargets (configuration);
		}

		internal protected virtual IEnumerable<ExecutionTarget> OnGetExecutionTargets (OperationContext ctx, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfig)
		{
			return next.OnGetExecutionTargets (ctx, configuration, runConfig);
		}

		internal protected virtual void OnExecutionTargetsChanged ()
		{
			next.OnExecutionTargetsChanged ();
		}

		internal protected virtual IEnumerable<SolutionItemRunConfiguration> OnGetRunConfigurations (OperationContext ctx)
		{
			return next.OnGetRunConfigurations (ctx);
		}

		internal protected virtual void OnRunConfigurationsChanged (OperationContext ctx)
		{
			next.OnRunConfigurationsChanged (ctx);
		}

		#endregion

		#region Events

		internal protected virtual void OnReloadRequired (SolutionItemEventArgs args)
		{
			next.OnReloadRequired (args);
		}

		internal protected virtual void OnDefaultConfigurationChanged (ConfigurationEventArgs args)
		{
			next.OnDefaultConfigurationChanged (args);
		}

		internal protected virtual void OnBoundToSolution ()
		{
			next.OnBoundToSolution ();
		}

		internal protected virtual void OnUnboundFromSolution ()
		{
			next.OnUnboundFromSolution ();
		}

		internal protected virtual void OnConfigurationAdded (ConfigurationEventArgs args)
		{
			next.OnConfigurationAdded (args);
		}

		internal protected virtual void OnConfigurationRemoved (ConfigurationEventArgs args)
		{
			next.OnConfigurationRemoved (args);
		}

		internal protected virtual void OnModified (SolutionItemModifiedEventArgs args)
		{
			next.OnModified (args);
		}

		internal protected virtual void OnNameChanged (SolutionItemRenamedEventArgs e)
		{
			next.OnNameChanged (e);
		}

		#endregion
	}
}

