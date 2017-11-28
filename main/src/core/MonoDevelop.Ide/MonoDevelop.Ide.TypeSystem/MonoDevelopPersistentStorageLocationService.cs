//
// MonoDevelopPersistentStorageLocationService.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2017 Microsoft Inc.
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
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
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

namespace MonoDevelop.Ide.TypeSystem
{
	[ExportWorkspaceService (typeof (IPersistentStorageLocationService), ServiceLayer.Host), Shared]
	class MonoDevelopPersistentStorageLocationService : IPersistentStorageLocationService
	{
		public bool IsSupported (Workspace workspace) => workspace is MonoDevelopWorkspace;

		// PERF: cache for the solution location. This is needed due to roslyn querying GetStorageLocation a lot of times.
		internal ConditionalWeakTable<SolutionId, string> storageMap = new ConditionalWeakTable<SolutionId, string> ();

		public string GetStorageLocation (Solution solution)
		{
			storageMap.TryGetValue (solution.Id, out var path);
			return path;
		}
	}
}
