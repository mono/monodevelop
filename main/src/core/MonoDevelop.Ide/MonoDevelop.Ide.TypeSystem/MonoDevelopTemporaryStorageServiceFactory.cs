// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core.Text;
using System.Reflection;
using MonoDevelop.Core;
using System.Collections.Generic;

namespace MonoDevelop.Ide.TypeSystem
{
	[ExportWorkspaceServiceFactory (typeof (ITemporaryStorageService), MonoDevelopWorkspace.ServiceLayer), Shared]
	sealed class MonoDevelopTemporaryStorageServiceFactory : IWorkspaceServiceFactory
	{
		static IWorkspaceServiceFactory microsoftFactory = new Microsoft.CodeAnalysis.Host.TemporaryStorageServiceFactory ();

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			return microsoftFactory.CreateService (workspaceServices);
		}
	}
}

