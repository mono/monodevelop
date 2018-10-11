//
// MonoDevelopFrameworkAssemblyPathResolver.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.Composition;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.TypeSystem
{
	[ExportWorkspaceServiceFactory (typeof (IFrameworkAssemblyPathResolver), ServiceLayer.Host), Shared]
	class MonoDevelopFrameworkAssemblyPathResolverFactory : IWorkspaceServiceFactory
	{
		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			return new MonoDevelopFrameworkAssemblyPathResolver (workspaceServices.Workspace as MonoDevelopWorkspace);
		}

		class MonoDevelopFrameworkAssemblyPathResolver : IFrameworkAssemblyPathResolver
		{
			readonly MonoDevelopWorkspace workspace;
			public MonoDevelopFrameworkAssemblyPathResolver (MonoDevelopWorkspace workspace)
			{
				this.workspace = workspace;
			}

			public string ResolveAssemblyPath (ProjectId projectId, string assemblyName, string fullyQualifiedName = null)
			{
				if (workspace == null)
					return null;

				if (!(workspace.GetMonoProject (projectId) is DotNetProject monoProject))
					return null;

				string assemblyFile = monoProject.AssemblyContext.GetAssemblyLocation (assemblyName, monoProject.TargetFramework);
				if (assemblyFile != null) {
					//if (string.IsNullOrEmpty(fullyQualifiedName) || CanResolveType(ResolveAssembly (projectId, assemblyName), fullyQualifiedName))
					return assemblyFile;
				}

				return null;
			}

			//static bool CanResolveType (Assembly assembly, string fullyQualifiedTypeName)
			//{
			//	if (fullyQualifiedTypeName == null) {
			//		// nothing to resolve.
			//		return true;
			//	}

			//	// We only get a type name without generic indicators.  So try to few different
			//	// generic versions of the type name in case any of those hit.  it's highly 
			//	// unlikely we'd find something with more than 4 generic parameters, so only try
			//	// up that point.
			//	for (var i = 0; i < 5; i++) {
			//		var name = i == 0
			//			? fullyQualifiedTypeName
			//			: fullyQualifiedTypeName + "`" + i;

			//		try {
			//			var type = assembly.GetType (name, throwOnError: false);
			//			if (type != null) {
			//				return true;
			//			}
			//		} catch (FileNotFoundException) {
			//		} catch (FileLoadException) {
			//		} catch (BadImageFormatException) {
			//		}
			//	}
			//	return false;
			//}

			//Assembly ResolveAssembly (ProjectId projectId, string assemblyLocation)
			//{
			//	Runtime.AssertMainThread ();

			//	try {
			//		return Assembly.LoadFrom (assemblyLocation);
			//	} catch (Exception e) {
			//		// Something wrong with our TFM.  We don't have enough information to 
			//		// properly resolve this assembly name.
			//		LoggingService.LogError ("Error while resolving assembly assemblyName");
			//		return null;
			//	}
			//}
		}
	}
}
