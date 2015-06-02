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
using MonoDevelop.Core;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace MonoDevelop.Ide.TypeSystem
{
	[ExportWorkspaceServiceFactory(typeof(IPersistentStorageService), ServiceLayer.Host), Shared]
	class PersistenceServiceFactory : IWorkspaceServiceFactory
	{
		static readonly IPersistentStorage NoOpPersistentStorageInstance = new NoOpPersistentStorage();
		static readonly IPersistentStorageService singleton = new PersistentStorageService ();

		public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
		{
			return singleton;
		}

		class NoOpPersistentStorage : IPersistentStorage
		{
			static Task<Stream> defaultStreamTask = Task.FromResult (default(Stream));
			static Task<bool> defaultBoolTask = Task.FromResult (false);

			public void Dispose()
			{
			}

			public Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default(CancellationToken))
			{
				return defaultStreamTask;
			}

			public Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default(CancellationToken))
			{
				return defaultStreamTask;
			}

			public Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
			{
				return defaultStreamTask;
			}

			public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
			{
				return defaultBoolTask;
			}

			public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
			{
				return defaultBoolTask;
			}

			public Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
			{
				return defaultBoolTask;
			}
		}

		class PersistentStorageService : IPersistentStorageService
		{
			Dictionary<SolutionId, IPersistentStorage> storages = new Dictionary<SolutionId, IPersistentStorage> ();
			/// <summary>
			/// threshold to start to use esent (50MB)
			/// </summary>
			const int SolutionSizeThreshold = 50 * 1024 * 1024;

			public IPersistentStorage GetStorage(Solution solution)
			{
				// check whether the solution actually exist on disk
				if (!File.Exists(solution.FilePath))
					return NoOpPersistentStorageInstance;

				// get working folder path
				string workingFolderPath;
				lock (getStorageLock) {
					workingFolderPath = TypeSystemService.GetCacheDirectory (solution.FilePath, true);
					if (workingFolderPath == null) {
						// we don't have place to save. don't use caching
						return NoOpPersistentStorageInstance;
					}
				}

				return GetStorage(solution, workingFolderPath);
			}

			object getStorageLock = new object ();
			object storageLock = new object ();

			IPersistentStorage GetStorage (Solution solution, string workingFolderPath)
			{
				lock (storageLock) {
					IPersistentStorage storage;
					if (storages.TryGetValue (solution.Id, out storage))
						return storage;
					if (!SolutionSizeAboveThreshold (solution)) {
						storage = NoOpPersistentStorageInstance;
					} else {
						storage = new PersistentStorage (workingFolderPath);
					}
					storages.Add (solution.Id, storage);
					return storage;
				}
			}

			bool SolutionSizeAboveThreshold(Solution solution)
			{
				var size = SolutionSizeTracker.GetSolutionSizeAsync(solution.Workspace, solution.Id, CancellationToken.None).Result;
				return size > SolutionSizeThreshold;
			}
		}

		class PersistentStorage : IPersistentStorage
		{
			static Task<Stream> defaultStreamTask = Task.FromResult (default(Stream));
			static MD5 md5 = MD5.Create (); 

			string workingFolderPath;

			public PersistentStorage (string workingFolderPath)
			{
				this.workingFolderPath = workingFolderPath;
			}

			public void Dispose()
			{
			}

			public static string GetMD5 (string data)
			{
				var result = new StringBuilder();
				foreach (var b in md5.ComputeHash (Encoding.ASCII.GetBytes (data))) {
					result.Append(b.ToString("X2"));
				}
				return result.ToString();
			}

			const string dataFileExtension = ".dat";

			static string GetFileName (string name)
			{
				return GetMD5 (name) + dataFileExtension;
			}

			static string GetDocumentDataFileName (Document document, string name)
			{
				return GetMD5 (document.FilePath + "_" + name) + dataFileExtension;
			}

			static string GetProjectDataFileName (Project project, string name)
			{
				return GetMD5 (project.FilePath + "_" + name) + dataFileExtension;
			}

			public Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default(CancellationToken))
			{
				string fileName = Path.Combine (workingFolderPath, GetDocumentDataFileName (document, name));
				if (!File.Exists (fileName))
					return defaultStreamTask;
				return Task.FromResult ((Stream)File.OpenRead (fileName));
			}

			public Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default(CancellationToken))
			{
				string fileName = Path.Combine (workingFolderPath, GetProjectDataFileName (project, name));
				if (!File.Exists (fileName))
					return defaultStreamTask;
				return Task.FromResult ((Stream)File.OpenRead (fileName));
			}

			public Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
			{
				string fileName = Path.Combine (workingFolderPath, GetFileName (name));
				if (!File.Exists (fileName))
					return defaultStreamTask;
				return Task.FromResult ((Stream)File.OpenRead (fileName));
			}

			public async Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
			{
				string fileName = Path.Combine (workingFolderPath, GetDocumentDataFileName (document, name));
				using (var newStream = File.OpenWrite (fileName)) {
					await stream.CopyToAsync (newStream, 81920, cancellationToken);
				}
				return true;
			}

			public async Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
			{
				string fileName = Path.Combine (workingFolderPath, GetProjectDataFileName (project, name));
				using (var newStream = File.OpenWrite (fileName)) {
					await stream.CopyToAsync (newStream, 81920, cancellationToken);
				}
				return true;
			}

			public async Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
			{
				string fileName = Path.Combine (workingFolderPath, GetFileName (name));
				using (var newStream = File.OpenWrite (fileName)) {
					await stream.CopyToAsync (newStream, 81920, cancellationToken);
				}
				return true;
			}
		}
	}
}

