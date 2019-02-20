// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Build.Framework;
using MonoDevelop.Core.Assemblies;

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
		Version msbuildVersion;

		internal SdkResolution (TargetRuntime runtime)
		{
			this.runtime = runtime;
		}

		public static SdkResolution GetResolver (TargetRuntime runtime)
		{
			lock (runtimeResolvers) {
				if (!runtimeResolvers.TryGetValue (runtime, out var resolution))
					runtimeResolvers [runtime] = resolution = new SdkResolution (runtime);
				return resolution;
			}
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
					var context = new SdkResolverContextImpl (buildEngineLogger, projectFile, solutionPath, msbuildVersion);
					var resultFactory = new SdkResultFactoryImpl (sdk);
					try {
						var result = (SdkResultImpl)sdkResolver.Resolve (sdk, context, resultFactory);
						if (result != null && result.Success) {
							LogWarnings (logger, buildEventContext, projectFile, result);
							return result.Path;
						}

						if (result != null)
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
				msbuildVersion = GetMSBuildVersion ();
				_resolvers = LoadResolvers (logger);
			}
		}

		Version GetMSBuildVersion ()
		{
			var msbuildFileName = MSBuildProcessService.GetMSBuildBinPath (runtime);
			if (!File.Exists (msbuildFileName))
				return null;

			var versionInfo = FileVersionInfo.GetVersionInfo (msbuildFileName);
			return new Version (versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart, versionInfo.FilePrivatePart);
		}

		IList<SdkResolver> LoadResolvers (ILoggingService logger)
		{
			// Add the MonoDevelop resolver, which resolves SDKs registered by add-ins.
			// Also add the default resolver.

			var resolvers = new List<SdkResolver> { new MonoDevelop.Projects.MSBuild.Resolver (MSBuildProjectService.FindRegisteredSdks), new DefaultSdkResolver { TargetRuntime = runtime } };
			var binDir = MSBuildProjectService.GetMSBuildBinPath (runtime);
			var potentialResolvers = FindPotentialSdkResolvers (Path.Combine (binDir, "SdkResolvers"), logger);

			if (potentialResolvers.Count == 0) return resolvers;

			foreach (var potentialResolver in potentialResolvers)
				LoadResolvers (potentialResolver, logger, resolvers);

			return resolvers.OrderBy (t => t.Priority).ToList ();
		}

		/// <summary>
		///     Find all files that are to be considered SDK Resolvers. Pattern will match
		///     Root\SdkResolver\(ResolverName)\(ResolverName).dll.
		/// </summary>
		/// <param name="rootFolder"></param>
		/// <returns></returns>
		IList<string> FindPotentialSdkResolvers (string rootFolder, ILoggingService logger)
		{
			// Note: MSBuild throws exceptions here which would prevent other SDK resolvers from being loaded.
			// Exceptions and errors are being logged as warnings instead for MonoDevelop.
			var assembliesList = new List<string> ();

			if (string.IsNullOrEmpty (rootFolder) || !System.IO.Directory.Exists (rootFolder))
				return assembliesList;

			foreach (var subfolder in new DirectoryInfo (rootFolder).GetDirectories ()) {
				var assembly = Path.Combine (subfolder.FullName, $"{subfolder.Name}.dll");
				var manifest = Path.Combine (subfolder.FullName, $"{subfolder.Name}.xml");

				var assemblyAdded = TryAddAssembly (assembly, assembliesList);
				if (!assemblyAdded) {
					assemblyAdded = TryAddAssemblyFromManifest (manifest, subfolder.FullName, assembliesList, logger);
				}

				if (!assemblyAdded) {
					logger.LogWarning ("SDK Resolver folder exists but without an SDK Resolver DLL or manifest file. This may indicate a corrupt or invalid installation of MSBuild. SDK resolver path: " + subfolder.FullName);
				}
			}

			return assembliesList;
		}

		bool TryAddAssemblyFromManifest (string pathToManifest, string manifestFolder, List<string> assembliesList, ILoggingService logger)
		{
			if (!string.IsNullOrEmpty (pathToManifest) && !File.Exists (pathToManifest))
				return false;

			string path = null;

			try {
				// <SdkResolver>
				//   <Path>...</Path>
				// </SdkResolver>
				var manifest = SdkResolverManifest.Load (pathToManifest);

				if (manifest == null || string.IsNullOrEmpty (manifest.Path)) {
					logger.LogWarning ("Could not load SDK Resolver. A manifest file exists, but the path to the SDK Resolver DLL file could not be found. Manifest file path " + pathToManifest);
					return false;
				}

				path = FixFilePath (manifest.Path);
			} catch (SerializationException e) {
				// Note: Not logging e.ToString() as most of the information is not useful, the Message will contain what is wrong with the XML file.
				logger.LogWarning (string.Format ("SDK Resolver manifest file is invalid. This may indicate a corrupt or invalid installation of MSBuild. Manifest file path '{0}'. Message: {1}", pathToManifest, e.Message));
				return false;
			}

			if (!Path.IsPathRooted (path)) {
				path = Path.Combine (manifestFolder, path);
				path = Path.GetFullPath (path);
			}

			if (!TryAddAssembly (path, assembliesList)) {
				logger.LogWarning (string.Format (" Could not load SDK Resolver. A manifest file exists, but the path to the SDK Resolver DLL file could not be found. Manifest file path '{0}'. SDK resolver path: {1}", pathToManifest, path));
				return false;
			}

			return true;
		}

		bool TryAddAssembly (string assemblyPath, List<string> assembliesList)
		{
			if (string.IsNullOrEmpty (assemblyPath) || !File.Exists (assemblyPath))
				return false;

			assembliesList.Add (assemblyPath);
			return true;
		}

		IEnumerable<Type> GetResolverTypes (Assembly assembly)
		{
			return assembly.ExportedTypes
				.Select (type => new { type, info = type.GetTypeInfo () })
				.Where (t => t.info.IsClass && t.info.IsPublic && !t.info.IsAbstract && typeof (SdkResolver).IsAssignableFrom (t.type))
				.Select (t => t.type);
		}

		void LoadResolvers (string resolverPath, ILoggingService logger, List<SdkResolver> resolvers)
		{
			Assembly assembly;
			try {
				assembly = Assembly.LoadFrom (resolverPath);
			} catch (Exception e) {
				logger.LogWarning (string.Format ("The SDK resolver assembly \"{0}\" could not be loaded. {1}", resolverPath, e.Message));
				return;
			}

			foreach (Type type in GetResolverTypes (assembly)) {
				try {
					resolvers.Add ((SdkResolver)Activator.CreateInstance (type));
				} catch (TargetInvocationException e) {
					// .NET wraps the original exception inside of a TargetInvocationException which masks the original message
					// Attempt to get the inner exception in this case, but fall back to the top exception message
					string message = e.InnerException?.Message ?? e.Message;
					logger.LogWarning (string.Format ("The SDK resolver type \"{0}\" failed to load. {1}", type.Name, message));
					return;
				} catch (Exception e) {
					logger.LogWarning (string.Format ("The SDK resolver type \"{0}\" failed to load. {1}", type.Name, e.Message));
					return;
				}
			}
		}

		static void LogWarnings (ILoggingService logger, MSBuildContext bec, string projectFile,
			SdkResultImpl result)
		{
			if (result.Warnings == null) return;

			foreach (var warning in result.Warnings)
				logger.LogWarningFromText (bec, null, null, null, projectFile, warning);
		}

		static string FixFilePath (string path)
		{
			return string.IsNullOrEmpty (path) || Path.DirectorySeparatorChar == '\\' ? path : path.Replace ('\\', '/');
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

			public IEnumerable<string> Errors { get; }

			public IEnumerable<string> Warnings { get; }
		}

		internal class SdkResultFactoryImpl : SdkResultFactory
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
			public SdkResolverContextImpl (SdkLogger logger, string projectFilePath, string solutionPath, Version msbuildVersion)
			{
				Logger = logger;
				ProjectFilePath = projectFilePath;
				SolutionFilePath = solutionPath;
				MSBuildVersion = msbuildVersion;
			}
		}
	}
}