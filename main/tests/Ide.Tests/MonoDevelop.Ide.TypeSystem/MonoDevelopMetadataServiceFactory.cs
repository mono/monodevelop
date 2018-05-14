//
// MonoDevelopMetadataServiceFactory.cs
//
// Author:
//       David Karlaš <david.karlas@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corp
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
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace MonoDevelop.Ide.TypeSystem.MetadataReferences
{
	[ExportWorkspaceServiceFactory (typeof (IMetadataService), ServiceLayer.Host), Shared]
	class MonoDevelopMetadataServiceFactory : IWorkspaceServiceFactory
	{
		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			return new Service (workspaceServices);
		}

		sealed class Service : IMetadataService
		{
			readonly Lazy<MonoDevelopMetadataReferenceManager> _manager;

			public Service (HostWorkspaceServices workspaceServices)
			{
				// We will defer creation of this reference manager until we have to to avoid it being constructed too
				// early and potentially causing deadlocks.
				_manager = new Lazy<MonoDevelopMetadataReferenceManager> (
					() => workspaceServices.GetRequiredService<MonoDevelopMetadataReferenceManager> ());
			}

			public PortableExecutableReference GetReference (string resolvedPath, MetadataReferenceProperties properties)
			{
				return _manager.Value.GetOrCreateMetadataReferenceSnapshot (resolvedPath, properties);
			}
		}
	}

	// TODO: Remove this type. This factory is needed just to instantiate a singleton of VisualStudioMetadataReferenceProvider.
	// We should be able to MEF-instantiate a singleton of VisualStudioMetadataReferenceProvider without creating this factory.
	[ExportWorkspaceServiceFactory (typeof (MonoDevelopMetadataReferenceManager), ServiceLayer.Host), Shared]
	class MonoDevelopMetadataReferenceManagerFactory : IWorkspaceServiceFactory
	{
		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			var temporaryStorage = workspaceServices.GetService<ITemporaryStorageService> ();
			return new MonoDevelopMetadataReferenceManager (temporaryStorage);
		}
	}
}
