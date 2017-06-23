//
// MonoDevelopPersistentStorageServiceFactory.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SQLite;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.SolutionSize;
using MonoDevelop.Core;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace MonoDevelop.Ide.TypeSystem
{
	[ExportWorkspaceServiceFactory(typeof(IPersistentStorageService), MonoDevelopWorkspace.ServiceLayer), Shared]
	class PersistenceStorageServiceFactory : IWorkspaceServiceFactory
	{
		SolutionSizeTracker solutionSizeTracker;

		[ImportingConstructor]
		public PersistenceStorageServiceFactory (SolutionSizeTracker solutionSizeTracker)
		{
			this.solutionSizeTracker = solutionSizeTracker;
		}

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			var optionService = workspaceServices.GetService<IOptionService> ();
			return new SQLitePersistentStorageService (optionService, solutionSizeTracker);
		}
	}

	[ExportWorkspaceService (typeof (IPersistentStorageLocationService), ServiceLayer.Host), Shared]
	class PersistentStorageLocationService : IPersistentStorageLocationService
	{
		public bool IsSupported (Workspace workspace) => workspace is MonoDevelopWorkspace;

		public string GetStorageLocation (Solution solution)
		{
			var vsWorkspace = solution.Workspace as MonoDevelopWorkspace;
			return solution.FilePath != null ? Path.GetDirectoryName (solution.FilePath) : null;
		}

	}
}
