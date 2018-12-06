// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// Based on NuGet/NuGet.Client/src/NuGet.Clients/NuGet.CommandLine/MsBuildUtility.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;
using NuGet.Common;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.ProjectModel;

namespace NuGet.CommandLine
{
	static class MSBuildUtility
	{
		static readonly XNamespace MSBuildNamespace = XNamespace.Get ("http://schemas.microsoft.com/developer/msbuild/2003");

		public static async Task<DependencyGraphSpec> GetSolutionRestoreSpec (
			Solution solution,
			IEnumerable<BuildIntegratedNuGetProject> projects,
			ConfigurationSelector configuration,
			ILogger logger,
			CancellationToken cancellationToken)
		{
			logger.Log (LogLevel.Information, GettextCatalog.GetString ("Getting restore information for solution {0}", solution.FileName));

			using (var inputTargetPath = new TempFile (".nugetinputs.targets"))
			using (var resultsPath = new TempFile (".output.dg")) {
				var properties = CreateMSBuildProperties (solution, configuration, resultsPath);

				FilePath msbuildBinDirectory = MSBuildProcessService.GetMSBuildBinDirectory ();
				string restoreTargetPath = msbuildBinDirectory.Combine ("NuGet.targets");

				XDocument inputTargetXML = GetRestoreInputFile (restoreTargetPath, properties, projects);
				inputTargetXML.Save (inputTargetPath);

				string arguments = GetMSBuildArguments (inputTargetPath, solution);

				using (var monitor = new LoggingProgressMonitor ()) {
					var process = MSBuildProcessService.StartMSBuild (
						arguments,
						solution.BaseDirectory,
						monitor.Log,
						monitor.ErrorLog,
						null);
					using (process) {
						process.SetCancellationToken (cancellationToken);
						await process.Task;
						if (process.ExitCode != 0) {
							throw new ApplicationException (GettextCatalog.GetString ("MSBuild exited with code {0}", process.ExitCode));
						}
					}
				}
				return MSBuildPackageSpecCreator.GetDependencyGraph (resultsPath);
			}
		}

		static Dictionary<string, string> CreateMSBuildProperties (Solution solution, ConfigurationSelector configuration, TempFile resultsPath)
		{
			var solutionConfig = configuration.GetConfiguration (solution);

			var properties = new Dictionary<string, string> () {
				{ "RestoreGraphOutputPath", resultsPath },
				{ "RestoreProjectFilterMode", "exclusionlist" }
			};

			if (!string.IsNullOrEmpty (solutionConfig.Name)) {
				properties ["Configuration"] = solutionConfig.Name;
			}
			if (!string.IsNullOrEmpty (solutionConfig.Platform)) {
				properties ["Platform"] = solutionConfig.Platform;
			}

			return properties;
		}

		static string GetMSBuildArguments (
			string inputTargetPath,
			Solution solution)
		{
			var args = new ProcessArgumentBuilder ();

			args.AddQuoted (inputTargetPath);
			args.Add ("/t:GenerateRestoreGraphFile");
			args.Add ("/nologo");
			args.Add ("/nr:false");

			if (MSBuildPackageSpecCreator.VerboseLogging) {
				args.Add ("/v:diagnostic");
			} else {
				args.Add ("/v:q");
			}

			// Disable parallel and use ContinueOnError since this may run on an older
			// version of MSBuild that do not support SkipNonexistentTargets.
			// When BuildInParallel is used with ContinueOnError it does not continue in
			// some scenarios.
			// Allow opt in to msbuild 15.5 behavior with NUGET_RESTORE_MSBUILD_USESKIPNONEXISTENT
			var nonExistFlag = Environment.GetEnvironmentVariable ("NUGET_RESTORE_MSBUILD_USESKIPNONEXISTENT");
			if (!StringComparer.OrdinalIgnoreCase.Equals (nonExistFlag, bool.TrueString)) {
				AddProperty (args, "RestoreBuildInParallel", bool.FalseString);
				AddProperty (args, "RestoreUseSkipNonexistentTargets", bool.FalseString);
			}

			// Add additional args to msbuild if needed
			var msbuildAdditionalArgs = Environment.GetEnvironmentVariable ("NUGET_RESTORE_MSBUILD_ARGS");

			if (!string.IsNullOrEmpty (msbuildAdditionalArgs)) {
				args.Add (msbuildAdditionalArgs);
			}

			return args.ToString ();
		}

		static XDocument GetRestoreInputFile (string restoreTargetPath, Dictionary<string, string> properties, IEnumerable<BuildIntegratedNuGetProject> projects)
		{
			return GenerateMSBuildFile (
				new XElement (MSBuildNamespace + "PropertyGroup", properties.Select (e => new XElement (MSBuildNamespace + e.Key, e.Value))),
				new XElement (MSBuildNamespace + "ItemGroup", projects.Select (project => GetRestoreGraphProjectInputItem (project.MSBuildProjectPath))),
				new XElement (MSBuildNamespace + "Import", new XAttribute (XName.Get ("Project"), restoreTargetPath)));
		}

		static XDocument GenerateMSBuildFile (params XElement [] elements)
		{
			return new XDocument (
				new XDeclaration ("1.0", "utf-8", "no"),
				new XElement (MSBuildNamespace + "Project",
					new XAttribute ("ToolsVersion", "14.0"),
					elements));
		}

		static XElement GetRestoreGraphProjectInputItem (string path)
		{
			return new XElement (MSBuildNamespace + "RestoreGraphProjectInputItems", new XAttribute (XName.Get ("Include"), path));
		}

		static void AddProperty (ProcessArgumentBuilder args, string property, string value)
		{
			if (string.IsNullOrEmpty (value)) {
				throw new ArgumentException (nameof (value));
			}

			AddPropertyIfHasValue (args, property, value);
		}

		static void AddPropertyIfHasValue (ProcessArgumentBuilder args, string property, string value)
		{
			if (!string.IsNullOrEmpty (value)) {
				args.Add (string.Format ("/p:{0}={1}", property, ProcessArgumentBuilder.Quote (value)));
			}
		}
	}
}
