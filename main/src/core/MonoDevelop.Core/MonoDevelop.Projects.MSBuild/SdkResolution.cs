// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace Microsoft.Build.BackEnd
{
    /// <summary>
    ///     Component responsible for resolving an SDK to a file path. Loads and coordinates
    ///     with <see cref="SdkResolver" /> plug-ins.
    /// </summary>
    internal class SdkResolution
    {
        private readonly object _lockObject = new object();
        private readonly SdkResolverLoader _sdkResolverLoader;
        private IList<SdkResolver> _resolvers;

        /// <summary>
        ///     Create an instance with a specified resolver assembly loading strategy. Used
        ///     for testing purposes.
        /// </summary>
        /// <param name="sdkResolverLoader">Resolver loading strategy.</param>
        internal SdkResolution(SdkResolverLoader sdkResolverLoader)
        {
            _sdkResolverLoader = sdkResolverLoader;
        }

        internal static SdkResolution Instance { get; } = new SdkResolution(new SdkResolverLoader());

        /// <summary>
        ///     Get path on disk to the referenced SDK.
        /// </summary>
        /// <param name="sdk">SDK referenced by the Project.</param>
        /// <param name="logger">Logging service.</param>
        /// <param name="buildEventContext">Build event context for logging.</param>
        /// <param name="projectFile">Location of the element within the project which referenced the SDK.</param>
        /// <param name="solutionPath">Path to the solution if known.</param>
        /// <returns>Path to the root of the referenced SDK.</returns>
        internal string GetSdkPath(SdkReference sdk, ILoggingService logger, MSBuildContext buildEventContext,
            string projectFile, string solutionPath)
        {

            if (_resolvers == null) Initialize(logger, buildEventContext, projectFile);

            var results = new List<SdkResultImpl>();

            try
            {
                var buildEngineLogger = new SdkLoggerImpl(logger, buildEventContext);
                foreach (var sdkResolver in _resolvers)
                {
                    var context = new SdkResolverContextImpl(buildEngineLogger, projectFile, solutionPath);
                    var resultFactory = new SdkResultFactoryImpl(sdk);
                    try
                    {
                        var result = (SdkResultImpl)sdkResolver.Resolve(sdk, context, resultFactory);
                        if (result != null && result.Success)
                        {
                            LogWarnings(logger, buildEventContext, projectFile, result);
                            return result.Path;
                        }

                        results.Add(result);
                    }
                    catch (Exception e)
                    {
                        logger.LogFatalBuildError(buildEventContext, e, projectFile);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogFatalBuildError(buildEventContext, e, projectFile);
                throw;
            }

            foreach (var result in results)
            {
                LogWarnings(logger, buildEventContext, projectFile, result);

                if (result.Errors != null)
                {
                    foreach (var error in result.Errors)
                    {
                        logger.LogErrorFromText(buildEventContext, subcategoryResourceName: null, errorCode: null,
                            helpKeyword: null, file: projectFile, message: error);
                    }
                }
            }

            return null;
        }

        private void Initialize(ILoggingService logger, MSBuildContext buildEventContext, string projectFile)
        {
            lock (_lockObject)
            {
                if (_resolvers != null) return;
                _resolvers = _sdkResolverLoader.LoadResolvers(logger, buildEventContext, projectFile);
            }
        }

        private static void LogWarnings(ILoggingService logger, MSBuildContext bec, string projectFile,
            SdkResultImpl result)
        {
            if (result.Warnings == null) return;

            foreach (var warning in result.Warnings)
                logger.LogWarningFromText(bec, null, null, null, projectFile, warning);
        }

        private class SdkLoggerImpl : SdkLogger
        {
            private readonly MSBuildContext _buildEventContext;
            private readonly ILoggingService _loggingService;

            public SdkLoggerImpl(ILoggingService loggingService, MSBuildContext buildEventContext)
            {
                _loggingService = loggingService;
                _buildEventContext = buildEventContext;
            }

            public override void LogMessage(string message, MessageImportance messageImportance = MessageImportance.Low)
            {
                _loggingService.LogCommentFromText(_buildEventContext, messageImportance, message);
            }
        }

        private class SdkResultImpl : SdkResult
        {
            public SdkResultImpl(SdkReference sdkReference, IEnumerable<string> errors, IEnumerable<string> warnings)
            {
                Success = false;
                Sdk = sdkReference;
                Errors = errors;
                Warnings = warnings;
            }

            public SdkResultImpl(SdkReference sdkReference, string path, string version, IEnumerable<string> warnings)
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

        private class SdkResultFactoryImpl : SdkResultFactory
        {
            private readonly SdkReference _sdkReference;

            internal SdkResultFactoryImpl(SdkReference sdkReference)
            {
                _sdkReference = sdkReference;
            }

            public override SdkResult IndicateSuccess(string path, string version, IEnumerable<string> warnings = null)
            {
                return new SdkResultImpl(_sdkReference, path, version, warnings);
            }

            public override SdkResult IndicateFailure(IEnumerable<string> errors, IEnumerable<string> warnings = null)
            {
                return new SdkResultImpl(_sdkReference, errors, warnings);
            }
        }

        private sealed class SdkResolverContextImpl : SdkResolverContext
        {
            public SdkResolverContextImpl(SdkLogger logger, string projectFilePath, string solutionPath)
            {
                Logger = logger;
                ProjectFilePath = projectFilePath;
                SolutionFilePath = solutionPath;
            }
        }
    }
}