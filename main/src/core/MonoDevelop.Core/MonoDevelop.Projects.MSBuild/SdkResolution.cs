// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.MSBuild
{
	/// <summary>
	///     Component responsible for resolving an SDK to a file path. Loads and coordinates
	///     with <see cref="SdkResolver" /> plug-ins.
	/// </summary>
	class SdkResolution
	{
		static Dictionary<TargetRuntime, SdkResolution> runtimeResolvers = new Dictionary<TargetRuntime, SdkResolution> ();

		readonly object _lockObject = new object ();
		IList<SdkResolver> _resolvers;
		TargetRuntime runtime;

		internal SdkResolution (TargetRuntime runtime)
		{
			this.runtime = runtime;
		}

		public static SdkResolution GetResolver (TargetRuntime runtime)
		{
			if (!runtimeResolvers.TryGetValue (runtime, out var resolution))
				runtimeResolvers [runtime] = resolution = new SdkResolution (runtime);
			return resolution;
		}

		/// <summary>
		///     Get path on disk to the referenced SDK.
		/// </summary>
		/// <param name="sdk">SDK referenced by the Project.</param>
		/// <param name="logger">Logging service.</param>
		/// <param name="buildEventContext">Build event context for logging.</param>
		/// <param name="projectFile">Location of the element within the project which referenced the SDK.</param>
		/// <param name="solutionPath">Path to the solution if known.</param>
		/// <returns>Path to the root of the referenced SDK.</returns>
		internal string GetSdkPath (SdkReference sdk, ILoggingService logger, MSBuildContext buildEventContext,
			string projectFile, string solutionPath)
		{
			if (_resolvers == null) Initialize (logger);

			var results = new List<SdkResultImpl> ();

			try {
				var buildEngineLogger = new SdkLoggerImpl (logger, buildEventContext);
				foreach (var sdkResolver in _resolvers) {
					var context = new SdkResolverContextImpl (buildEngineLogger, projectFile, solutionPath);
					var resultFactory = new SdkResultFactoryImpl (sdk);
					try {
						var result = (SdkResultImpl)sdkResolver.Resolve (sdk, context, resultFactory);
						if (result != null && result.Success) {
							LogWarnings (logger, buildEventContext, projectFile, result);
							return result.Path;
						}

						results.Add (result);
					} catch (Exception e) {
						logger.LogFatalBuildError (buildEventContext, e, projectFile);
					}
				}
			} catch (Exception e) {
				logger.LogFatalBuildError (buildEventContext, e, projectFile);
				throw;
			}

			foreach (var result in results) {
				LogWarnings (logger, buildEventContext, projectFile, result);

				if (result.Errors != null) {
					foreach (var error in result.Errors) {
						logger.LogErrorFromText (buildEventContext, subcategoryResourceName: null, errorCode: null,
							helpKeyword: null, file: projectFile, message: error);
					}
				}
			}

			return null;
		}

		void Initialize (ILoggingService logger)
		{
			lock (_lockObject) {
				if (_resolvers != null) return;
				_resolvers = LoadResolvers (logger);
			}
		}

		IList<SdkResolver> LoadResolvers (ILoggingService logger)
		{
			// Always add the default resolver
			var resolvers = new List<SdkResolver> { new DefaultSdkResolver { TargetRuntime = runtime } };
			MSBuildProjectService.GetNewestInstalledToolsVersion (runtime, true, out var binDir);
			var potentialResolvers = FindPotentialSdkResolvers (Path.Combine (binDir, "SdkResolvers"));

			if (potentialResolvers.Count == 0) return resolvers;

			foreach (var potentialResolver in potentialResolvers)
				try {
					var assembly = Assembly.LoadFrom (potentialResolver);

					resolvers.AddRange (assembly.ExportedTypes
						.Select (type => new { type, info = type.GetTypeInfo () })
						.Where (t => t.info.IsClass && t.info.IsPublic && typeof (SdkResolver).IsAssignableFrom (t.type))
						.Select (t => (SdkResolver)Activator.CreateInstance (t.type)));
				} catch (Exception e) {
					logger.LogWarning (e.Message);
				}

			return resolvers.OrderBy (t => t.Priority).ToList ();
		}

		/// <summary>
		///     Find all files that are to be considered SDK Resolvers. Pattern will match
		///     Root\SdkResolver\(ResolverName)\(ResolverName).dll.
		/// </summary>
		/// <param name="rootFolder"></param>
		/// <returns></returns>
		IList<string> FindPotentialSdkResolvers (string rootFolder)
		{
			if (string.IsNullOrEmpty (rootFolder) || !System.IO.Directory.Exists (rootFolder))
				return new List<string> ();

			return new DirectoryInfo (rootFolder).GetDirectories ()
				.Select (subfolder => Path.Combine (subfolder.FullName, $"{subfolder.Name}.dll"))
				.Where (System.IO.File.Exists)
				.ToList ();
		}

		static void LogWarnings (ILoggingService logger, MSBuildContext bec, string projectFile,
			SdkResultImpl result)
		{
			if (result.Warnings == null) return;

			foreach (var warning in result.Warnings)
				logger.LogWarningFromText (bec, null, null, null, projectFile, warning);
		}

		class SdkLoggerImpl : SdkLogger
		{
			readonly MSBuildContext _buildEventContext;
			readonly ILoggingService _loggingService;

			public SdkLoggerImpl (ILoggingService loggingService, MSBuildContext buildEventContext)
			{
				_loggingService = loggingService;
				_buildEventContext = buildEventContext;
			}

			public override void LogMessage (string message, MessageImportance messageImportance = MessageImportance.Low)
			{
				_loggingService.LogCommentFromText (_buildEventContext, messageImportance, message);
			}
		}

		class SdkResultImpl : SdkResult
		{
			public SdkResultImpl (SdkReference sdkReference, IEnumerable<string> errors, IEnumerable<string> warnings)
			{
				Success = false;
				Sdk = sdkReference;
				Errors = errors;
				Warnings = warnings;
			}

			public SdkResultImpl (SdkReference sdkReference, string path, string version, IEnumerable<string> warnings)
			{
				Success = true;
				Sdk = sdkReference;
				Path = path;
				Version = version;
				Warnings = warnings;
			}

			public SdkReference Sdk { get; }

			public string Path { get; }

			public string Version { get; }

			public IEnumerable<string> Errors { get; }

			public IEnumerable<string> Warnings { get; }
		}

		class SdkResultFactoryImpl : SdkResultFactory
		{
			readonly SdkReference _sdkReference;

			internal SdkResultFactoryImpl (SdkReference sdkReference)
			{
				_sdkReference = sdkReference;
			}

			public override SdkResult IndicateSuccess (string path, string version, IEnumerable<string> warnings = null)
			{
				return new SdkResultImpl (_sdkReference, path, version, warnings);
			}

			public override SdkResult IndicateFailure (IEnumerable<string> errors, IEnumerable<string> warnings = null)
			{
				return new SdkResultImpl (_sdkReference, errors, warnings);
			}
		}

		sealed class SdkResolverContextImpl : SdkResolverContext
		{
			public SdkResolverContextImpl (SdkLogger logger, string projectFilePath, string solutionPath)
			{
				Logger = logger;
				ProjectFilePath = projectFilePath;
				SolutionFilePath = solutionPath;
			}
		}
	}
}