//
// DotNetCoreProjectExtension.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.DotNetCore
{
	[ExportProjectModelExtension]
	public class DotNetCoreProjectExtension: DotNetProjectExtension
	{
		List<string> targetFrameworks;
		bool outputTypeDefined;

		public DotNetCoreProjectExtension ()
		{
		}

		protected override bool SupportsObject (WorkspaceObject item)
		{
			return base.SupportsObject (item) && IsDotNetCoreProject ((DotNetProject)item);
		}

		protected override void Initialize ()
		{
			RequiresMicrosoftBuild = true;
			base.Initialize ();
		}

		protected override bool OnGetSupportsFramework (Core.Assemblies.TargetFramework framework)
		{
			if (framework.Id.Identifier == ".NETCoreApp" ||
			    framework.Id.Identifier == ".NETStandard")
				return true;
			return base.OnGetSupportsFramework (framework);
		}

		bool IsDotNetCoreProject (DotNetProject project)
		{
			var properties = project.MSBuildProject.EvaluatedProperties;
			return properties.HasProperty ("TargetFramework") ||
				properties.HasProperty ("TargetFrameworks");
		}

		protected override void OnReadProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			base.OnReadProject (monitor, msproject);

			outputTypeDefined = IsOutputTypeDefined (msproject);
			if (!outputTypeDefined)
				Project.CompileTarget = CompileTarget.Library;

			targetFrameworks = GetTargetFrameworks (msproject).ToList ();

			Project.UseAdvancedGlobSupport = true;
		}

		static bool IsOutputTypeDefined (MSBuildProject msproject)
		{
			var globalPropertyGroup = msproject.GetGlobalPropertyGroup ();
			if (globalPropertyGroup != null)
				return globalPropertyGroup.HasProperty ("OutputType");

			return false;
		}

		static IEnumerable<string> GetTargetFrameworks (MSBuildProject msproject)
		{
			var properties = msproject.EvaluatedProperties;
			if (properties != null) {
				string targetFramework = properties.GetValue ("TargetFramework");
				if (targetFramework != null) {
					return new [] { targetFramework };
				}

				string targetFrameworks = properties.GetValue ("TargetFrameworks");
				if (targetFrameworks != null) {
					return targetFrameworks.Split (';');
				}
			}

			return new string[0];
		}

		protected override void OnWriteProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			base.OnWriteProject (monitor, msproject);

			var globalPropertyGroup = msproject.GetGlobalPropertyGroup ();
			globalPropertyGroup.RemoveProperty ("ProjectGuid");

			if (!outputTypeDefined) {
				globalPropertyGroup.RemoveProperty ("OutputType");
			}

			msproject.DefaultTargets = null;
		}

		protected override ExecutionCommand OnCreateExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			return CreateDotNetCoreExecutionCommand (configSel, configuration, runConfiguration);
		}

		DotNetCoreExecutionCommand CreateDotNetCoreExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			FilePath outputDirectory = GetOutputDirectory (configuration);
			FilePath outputFileName = outputDirectory.Combine (Project.Name + ".dll");
			return new DotNetCoreExecutionCommand (
				Project.BaseDirectory,
				outputFileName
			);
		}

		FilePath GetOutputDirectory (DotNetProjectConfiguration configuration)
		{
			string targetFramework = targetFrameworks.FirstOrDefault ();
			FilePath outputDirectory = configuration.OutputDirectory;

			if (outputDirectory.IsAbsolute)
				return outputDirectory;
			else if (outputDirectory == "./")
				outputDirectory = Path.Combine ("bin", configuration.Name);

			return Project.BaseDirectory.Combine (outputDirectory.ToString (), targetFramework);
		}

		protected async override Task<TargetEvaluationResult> OnRunTarget (ProgressMonitor monitor, string target, ConfigurationSelector configuration, TargetEvaluationContext context)
		{
			string dotnetBuildCommand = GetDotNetBuildCommand (target);
			if (dotnetBuildCommand != null) {
				var config = Project.GetConfiguration (configuration) as DotNetProjectConfiguration;
				using (var builder = new DotNetCoreProjectBuilder (Project, monitor)) {
					BuildResult result = await builder.BuildAsnc (config, dotnetBuildCommand);
					return new TargetEvaluationResult (result);
				}
			}
			return await base.OnRunTarget (monitor, target, configuration, context);
		}

		static string GetDotNetBuildCommand (string target)
		{
			if (target == ProjectService.BuildTarget)
				return "build --no-dependencies";
			else if (target == ProjectService.CleanTarget)
				return "clean";

			return null;
		}
	}
}
