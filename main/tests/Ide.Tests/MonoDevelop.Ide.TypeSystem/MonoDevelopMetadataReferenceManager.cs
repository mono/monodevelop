//
// MetadataReferenceCache.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using MonoDevelop.Core;
using Roslyn.Utilities;
using System.Threading;
using System.Reflection;
using System.Globalization;
using MonoDevelop.Ide.TypeSystem.MetadataReferences;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using MonoDevelop.Core.Assemblies;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Reflection.PortableExecutable;
using System.Diagnostics;

namespace MonoDevelop.Ide.TypeSystem
{
	partial class MonoDevelopMetadataReferenceManager : IWorkspaceService
	{
		readonly MetadataCache _metadataCache;
		readonly MetadataReferenceCache _metadataReferenceCache;
		readonly ITemporaryStorageService _temporaryStorageService;
		static readonly ConditionalWeakTable<Metadata, object> lifetimeMap = new ConditionalWeakTable<Metadata, object> ();

		internal MonoDevelopMetadataReferenceManager (ITemporaryStorageService temporaryStorageService)
		{
			_metadataCache = new MetadataCache ();
			_metadataReferenceCache = new MetadataReferenceCache ();

			_temporaryStorageService = temporaryStorageService;
			Debug.Assert (temporaryStorageService != null);
		}

		/// <exception cref="IOException"/>
		/// <exception cref="BadImageFormatException" />
		internal Metadata GetMetadata (string fullPath, DateTime snapshotTimestamp)
		{
			var key = new FileKey (fullPath, snapshotTimestamp);
			// check existing metadata
			if (_metadataCache.TryGetMetadata (key, out var metadata)) {
				return metadata;
			}

			// use temporary storage
			var storages = new List<ITemporaryStreamStorage> ();
			var newMetadata = CreateAssemblyMetadataFromTemporaryStorage (key, storages);

			// don't dispose assembly metadata since it shares module metadata
			if (!_metadataCache.TryGetOrAddMetadata (key, new RecoverableMetadataValueSource (newMetadata, storages, lifetimeMap), out metadata)) {
				newMetadata.Dispose ();
			}

			return metadata;
		}

		internal IEnumerable<ITemporaryStreamStorage> GetStorages (string fullPath, DateTime snapshotTimestamp)
		{
			var key = new FileKey (fullPath, snapshotTimestamp);
			// check existing metadata
			if (_metadataCache.TryGetSource (key, out var source)) {
				if (source is RecoverableMetadataValueSource metadata) {
					return metadata.GetStorages ();
				}
			}

			return null;
		}

		/// <exception cref="IOException"/>
		/// <exception cref="BadImageFormatException" />
		AssemblyMetadata CreateAssemblyMetadataFromTemporaryStorage (FileKey fileKey, List<ITemporaryStreamStorage> storages)
		{
			var moduleMetadata = CreateModuleMetadataFromTemporaryStorage (fileKey, storages);
			return CreateAssemblyMetadata (fileKey, moduleMetadata, storages, CreateModuleMetadataFromTemporaryStorage);
		}

		ModuleMetadata CreateModuleMetadataFromTemporaryStorage (FileKey moduleFileKey, List<ITemporaryStreamStorage> storages)
		{
			GetStorageInfoFromTemporaryStorage (moduleFileKey, out var storage, out var stream, out var pImage);

			var metadata = ModuleMetadata.CreateFromMetadata (pImage, (int)stream.Length);

			// first time, the metadata is created. tie lifetime.
			lifetimeMap.Add (metadata, stream);

			// hold onto storage if requested
			if (storages != null) {
				storages.Add (storage);
			}

			return metadata;
		}

		void GetStorageInfoFromTemporaryStorage (FileKey moduleFileKey, out ITemporaryStreamStorage storage, out Stream stream, out IntPtr pImage)
		{
			int size;
			using (var copyStream = SerializableBytes.CreateWritableStream ()) {
				// open a file and let it go as soon as possible
				using (var fileStream = FileUtilities.OpenRead (moduleFileKey.FullPath)) {
					var headers = new PEHeaders (fileStream);

					var offset = headers.MetadataStartOffset;
					size = headers.MetadataSize;

					// given metadata contains no metadata info.
					// throw bad image format exception so that we can show right diagnostic to user.
					if (size <= 0) {
						throw new BadImageFormatException ();
					}

					StreamCopy (fileStream, copyStream, offset, size);
				}

				// copy over the data to temp storage and let pooled stream go
				storage = _temporaryStorageService.CreateTemporaryStreamStorage (CancellationToken.None);

				copyStream.Position = 0;
				storage.WriteStream (copyStream);
			}

			// get stream that owns direct access memory
			stream = storage.ReadStream (CancellationToken.None);

			// stream size must be same as what metadata reader said the size should be.
			Contract.ThrowIfFalse (stream.Length == size);

			// under VS host, direct access should be supported
			var directAccess = (ISupportDirectMemoryAccess)stream;
			pImage = directAccess.GetPointer ();
		}

		void StreamCopy (Stream source, Stream destination, int start, int length)
		{
			source.Position = start;

			var buffer = SharedPools.ByteArray.Allocate ();

			var read = 0;
			var left = length;
			while ((read = source.Read (buffer, 0, Math.Min (left, buffer.Length))) != 0) {
				destination.Write (buffer, 0, read);
				left -= read;
			}

			SharedPools.ByteArray.Free (buffer);
		}

		/// <exception cref="IOException"/>
		/// <exception cref="BadImageFormatException" />
		AssemblyMetadata CreateAssemblyMetadata (
		   FileKey fileKey, ModuleMetadata manifestModule, List<ITemporaryStreamStorage> storages,
		   Func<FileKey, List<ITemporaryStreamStorage>, ModuleMetadata> moduleMetadataFactory)
		{
			var moduleBuilder = ArrayBuilder<ModuleMetadata>.GetInstance ();

			string assemblyDir = null;
			foreach (string moduleName in manifestModule.GetModuleNames ()) {
				if (moduleBuilder.Count == 0) {
					moduleBuilder.Add (manifestModule);
					assemblyDir = Path.GetDirectoryName (fileKey.FullPath);
				}

				var moduleFileKey = FileKey.Create (PathUtilities.CombineAbsoluteAndRelativePaths (assemblyDir, moduleName));
				var metadata = moduleMetadataFactory (moduleFileKey, storages);

				moduleBuilder.Add (metadata);
			}

			if (moduleBuilder.Count == 0) {
				moduleBuilder.Add (manifestModule);
			}

			return AssemblyMetadata.Create (
				moduleBuilder.ToImmutableAndFree ());
		}

		public PortableExecutableReference GetOrCreateMetadataReferenceSnapshot (string filePath, MetadataReferenceProperties properties)
		{
			return _metadataReferenceCache.GetOrCreate (this, filePath, properties).CurrentSnapshot;
		}

		internal MonoDevelopMetadataReference GetOrCreateMetadataReference (string filePath, MetadataReferenceProperties properties)
		{
			return _metadataReferenceCache.GetOrCreate (this, filePath, properties);
		}

		public void ClearCache ()
		{
			_metadataCache.ClearCache();
			_metadataReferenceCache.ClearCache ();
		}


		// TODO: Figure out if we want to support metadata importers
		// See how metadata importers work in roslyn VS impl. This contains code that handles setting the framework paths
		/*
		ImmutableArray<string> GetRuntimeDirectories ()
		{
			var paths = new HashSet<string> (FilePath.PathComparer) {
				RuntimeEnvironment.GetRuntimeDirectory (),
			};

			if (Core.Platform.IsWindows) {
				// These values don't make sense on non-Windows
				paths.Add (Environment.GetFolderPath (Environment.SpecialFolder.Windows));
				paths.Add (Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles));
				paths.Add (Environment.GetFolderPath (Environment.SpecialFolder.ProgramFilesX86));
			}

			// Add all the runtimes' framework folders which contain assemblies
			// TODO: Maybe listen for runtime initialization events?
			foreach (var runtime in Runtime.SystemAssemblyService.GetTargetRuntimes ()) {
				paths.AddRange (runtime.GetAllFrameworkFolders ());
			}

			// Normalize the paths and return.
			return paths.Select (FileUtilities.NormalizeDirectoryPath).ToImmutableArray ();
		}
		*/

		// This should be added to TargetRuntime.cs
		/*
		public IEnumerable<string> GetAllFrameworkFolders ()
		{
			EnsureInitialized ();
			return frameworkBackends.SelectMany (backend => backend.Value.GetFrameworkFolders ());
		}
		*/
	}
}
