// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using MonoDevelop.Core;

namespace Microsoft.Build.BackEnd
{
    /// <summary>
    ///     Default SDK resolver for compatibility with VS2017 RTM.
    /// <remarks>
    ///     Default Sdk folder will to:
    ///         1) MSBuildSDKsPath environment variable if defined
    ///         2) When in Visual Studio, (VSRoot)\MSBuild\Sdks\
    ///         3) Outside of Visual Studio (MSBuild Root)\Sdks\
    /// </remarks>
    /// </summary>
    internal class DefaultSdkResolver : SdkResolver
    {
        public override string Name => "DefaultSdkResolver";

        public override int Priority => 10000;

        public override SdkResult Resolve(SdkReference sdk, SdkResolverContext context, SdkResultFactory factory)
        {
            var sdkPath = Path.Combine(BuildEnvironmentHelper.Instance.MSBuildSDKsPath, sdk.Name, "Sdk");

            // Note: On failure MSBuild will log a generic message, no need to indicate a failure reason here.
            return System.IO.Directory.Exists(sdkPath)
                ? factory.IndicateSuccess(sdkPath, string.Empty)
                : factory.IndicateFailure(null);
        }
    }

	interface ILoggingService
	{
		void LogErrorFromText (MSBuildContext buildEventContext, object subcategoryResourceName, object errorCode, object helpKeyword, string file, string message);
		void LogFatalBuildError (MSBuildContext buildEventContext, Exception e, string projectFile);
		void LogWarningFromText (MSBuildContext bec, object p1, object p2, object p3, string projectFile, string warning);
		void LogCommentFromText (MSBuildContext buildEventContext, MessageImportance messageImportance, string message);
		void LogWarning (MSBuildContext buildEventContext, string empty, string projectFile, string v, string message);
	}

	class CustomLoggingService : ILoggingService
	{
		public void LogCommentFromText (MSBuildContext buildEventContext, MessageImportance messageImportance, string message)
		{
			LoggingService.LogInfo ("[MSBuild] " + message);
		}

		public void LogErrorFromText (MSBuildContext buildEventContext, object subcategoryResourceName, object errorCode, object helpKeyword, string file, string message)
		{
			LoggingService.LogError ("[MSBuild] " + message);
		}

		public void LogFatalBuildError (MSBuildContext buildEventContext, Exception e, string projectFile)
		{
			LoggingService.LogError ("[MSBuild]", e);
		}

		public void LogWarning (MSBuildContext buildEventContext, string empty, string projectFile, string v, string message)
		{
			LoggingService.LogWarning ("[MSBuild] " + message);
		}

		public void LogWarningFromText (MSBuildContext bec, object p1, object p2, object p3, string projectFile, string warning)
		{
			LoggingService.LogWarning ("[MSBuild] " + warning);
		}
	}

	class MSBuildContext
	{
		
	}

	public class BuildEnvironmentHelper
	{
		public static BuildEnvironmentHelper Instance = new BuildEnvironmentHelper ();

		public BuildEnvironmentHelper ()
		{
		}

		public string MSBuildSDKsPath = "/Library/Frameworks/Mono.framework/Versions/5.2.0/lib/mono/msbuild/15.0/bin/Sdks";
		public string MSBuildToolsDirectory32 = "/Library/Frameworks/Mono.framework/Versions/5.2.0/lib/mono/msbuild/15.0/bin";
	}
}