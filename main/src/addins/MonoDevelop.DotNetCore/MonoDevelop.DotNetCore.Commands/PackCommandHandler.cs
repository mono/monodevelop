using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore.Commands
{
	class PackCommandHandler : CommandHandler
	{
		protected override void Run (object dataItem)
		{
			var project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			if (project == null)
				return;

			var buildTarget = new PackProjectBuildTarget (project);
			IdeApp.ProjectOperations.Build (buildTarget);
		}

		protected override void Update (CommandInfo info)
		{
			var project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			info.Enabled = info.Visible = project != null && IsDotNetCoreProject (project);
		}

		bool IsDotNetCoreProject (Project project)
		{
			return project.MSBuildProject.GetReferencedSDKs ().Any ();
		}
	}

	class PackProjectBuildTarget : IBuildTarget
	{
		DotNetProject project;

		public PackProjectBuildTarget (DotNetProject project)
		{
			this.project = project;
		}

		public string Name {
			get { return project.Name; }
		}

		public async Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector configuration, bool buildReferencedTargets = false, OperationContext operationContext = null)
		{
			var result = await project.Build (monitor, configuration, buildReferencedTargets, operationContext);
			if (result.Failed)
				return result;

			// Run the "Pack" target on the project
			var packResult = (await project.RunTarget (monitor, "Pack", configuration, new TargetEvaluationContext (operationContext))).BuildResult;
			return result.Append (packResult);
		}

		public bool CanBuild (ConfigurationSelector configuration)
		{
			throw new NotImplementedException ();
		}

		public bool CanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			throw new NotImplementedException ();
		}

		public Task<BuildResult> Clean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext = null)
		{
			throw new NotImplementedException ();
		}

		public Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			throw new NotImplementedException ();
		}

		public IEnumerable<IBuildTarget> GetExecutionDependencies ()
		{
			throw new NotImplementedException ();
		}

		public bool NeedsBuilding (ConfigurationSelector configuration)
		{
			throw new NotImplementedException ();
		}

		public Task PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			throw new NotImplementedException ();
		}
	}

}
