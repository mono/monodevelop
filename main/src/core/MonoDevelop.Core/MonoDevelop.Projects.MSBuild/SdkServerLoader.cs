// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.BackEnd.Logging;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;

namespace Microsoft.Build.BackEnd
{
    internal class SdkResolverLoader
    {
        internal virtual IList<SdkResolver> LoadResolvers(ILoggingService logger, MSBuildContext buildEventContext,
            string projectFile)
        {
            // Always add the default resolver
            var resolvers = new List<SdkResolver> { new DefaultSdkResolver() };
            var potentialResolvers = FindPotentialSdkResolvers(
                Path.Combine(BuildEnvironmentHelper.Instance.MSBuildToolsDirectory32, "SdkResolvers"));

            if (potentialResolvers.Count == 0) return resolvers;

            foreach (var potentialResolver in potentialResolvers)
                try
                {
                    var assembly = Assembly.LoadFrom(potentialResolver);

                    resolvers.AddRange(assembly.ExportedTypes
                        .Select(type => new { type, info = type.GetTypeInfo() })
                        .Where(t => t.info.IsClass && t.info.IsPublic && typeof(SdkResolver).IsAssignableFrom(t.type))
                        .Select(t => (SdkResolver)Activator.CreateInstance(t.type)));
                }
                catch (Exception e)
                {
                    logger.LogWarning(buildEventContext, string.Empty, projectFile,
                        "CouldNotLoadSdkResolver", e.Message);
                }

            return resolvers.OrderBy(t => t.Priority).ToList();
        }

        /// <summary>
        ///     Find all files that are to be considered SDK Resolvers. Pattern will match
        ///     Root\SdkResolver\(ResolverName)\(ResolverName).dll.
        /// </summary>
        /// <param name="rootFolder"></param>
        /// <returns></returns>
        internal virtual IList<string> FindPotentialSdkResolvers(string rootFolder)
        {
            if (string.IsNullOrEmpty(rootFolder) || !System.IO.Directory.Exists(rootFolder))
                return new List<string>();

            return new DirectoryInfo(rootFolder).GetDirectories()
                .Select(subfolder => Path.Combine(subfolder.FullName, $"{subfolder.Name}.dll"))
                .Where(System.IO.File.Exists)
                .ToList();
        }
    }
}