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
using System.Threading;
using System.Reflection;
using System.Globalization;
using MonoDevelop.Ide.TypeSystem.MetadataReferences;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MonoDevelop.Ide.TypeSystem
{
	static class MetadataReferenceCache
	{
		static Dictionary<string, Dictionary<MetadataReferenceProperties, MetadataReferenceCacheItem>> cache = new Dictionary<string, Dictionary<MetadataReferenceProperties, MetadataReferenceCacheItem>> ();
		static ConditionalWeakTable<MetadataReference, MetadataReferenceCacheItem> referenceToCacheItem = new ConditionalWeakTable<MetadataReference, MetadataReferenceCacheItem> ();

		public static PortableExecutableReference LoadReference (ProjectId projectId, string path, MetadataReferenceProperties properties)
		{
			lock (cache) {
				if (!cache.TryGetValue (path, out var propDic))
					cache [path] = propDic = new Dictionary<MetadataReferenceProperties, MetadataReferenceCacheItem> ();
				if (!propDic.TryGetValue (properties, out var cacheItem)) {
					cacheItem = new MetadataReferenceCacheItem (path, properties);
					propDic.Add (properties, cacheItem);
				}
				lock (cacheItem.InUseBy)
					if (projectId != default (ProjectId))
						cacheItem.InUseBy.Add (projectId);
				var reference = cacheItem.GetReference (true);
				referenceToCacheItem.Add (reference, cacheItem);
				return reference;
			}
		}

		/// <summary>
		/// What this method does... When Roslyn requests new MetadataReference via MonoDevelopMetadataServiceFactory
		/// we don't know yet for which project this will be used, but later when Roslyn adds it to project via
		/// Workspace.ApplyMetadataReferenceAdded method we can link that metadata to project, reason we want to do this
		/// linking is, that we want to update reference when file changes on hard drive...
		/// </summary>
		public static void LinkProject (MetadataReference metadataReference, ProjectId projectId)
		{
			lock (cache) {
				if (referenceToCacheItem.TryGetValue (metadataReference, out var cacheItem)) {
					lock (cacheItem.InUseBy) {
						cacheItem.InUseBy.Add (projectId);
					}
				}
			}
		}

		public static void RemoveReference (ProjectId projectId, string path)
		{
			lock (cache) {
				if (cache.TryGetValue (path, out var propDic)) {
					foreach (var cacheItem in propDic.Values) {
						lock (cacheItem.InUseBy)
							cacheItem.InUseBy.Remove (projectId);
					}
				}
			}
		}

		public static void RemoveReferences (ProjectId id)
		{
			lock (cache) {
				var toRemove = new List<string> ();
				foreach (var propDic in cache.Values) {
					foreach (var cacheItem in propDic.Values) {
						lock (cacheItem.InUseBy)
							cacheItem.InUseBy.Remove (id);
					}
				}
			}
		}

		public static void Clear ()
		{
			lock (cache) {
				foreach (var propDic in cache.Values) {
					foreach (var cacheItem in propDic.Values) {
						cacheItem.Dispose ();
					}
				}
				cache.Clear ();
			}
		}

		class MetadataReferenceCacheItem : IDisposable
		{
			public HashSet<ProjectId> InUseBy { get; } = new HashSet<ProjectId> ();
			WeakReference<PortableExecutableReference> weakReference;
			public PortableExecutableReference GetReference(bool load)
			{
				if (weakReference != null && weakReference.TryGetTarget (out var target))
					return target;
				if (load)
					return CreateNewReference ();
				else
					return null;
			}

			readonly string path;
			readonly MetadataReferenceProperties properties;

			DateTime timeStamp;
			FileChangeTracker fileChangeTracker;

			public MetadataReferenceCacheItem (string path, MetadataReferenceProperties properties)
			{
				this.path = path;
				this.properties = properties;
				fileChangeTracker = new FileChangeTracker (path);
				fileChangeTracker.UpdatedOnDisk += FileChangeTracker_UpdatedOnDisk;
			}

			void FileChangeTracker_UpdatedOnDisk (object sender, EventArgs e)
			{
				CheckForChange ();
			}

			public void CheckForChange ()
			{
				lock (InUseBy) {
					if (timeStamp != File.GetLastWriteTimeUtc (path)) {
						var oldReference = GetReference (false);
						if (oldReference != null) {
							foreach (var solution in IdeApp.Workspace.GetAllSolutions ()) {
								var workspace = TypeSystemService.GetWorkspace (solution);
								foreach (var projId in InUseBy)
									if (workspace.CurrentSolution.ContainsProject (projId))
										workspace.OnMetadataReferenceRemoved (projId, oldReference);
							}
						}
						var newReference = CreateNewReference ();
						if (newReference != null) {
							foreach (var solution in IdeApp.Workspace.GetAllSolutions ()) {
								var workspace = TypeSystemService.GetWorkspace (solution);
								foreach (var projId in InUseBy)
									if (workspace.CurrentSolution.ContainsProject (projId))
										workspace.OnMetadataReferenceAdded (projId, newReference);
							}
						}
					}
				}
			}

			readonly static DateTime NonExistentFile = new DateTime (1601, 1, 1);

			PortableExecutableReference CreateNewReference ()
			{
				timeStamp = File.GetLastWriteTimeUtc (path);
				PortableExecutableReference newReference;
				if (timeStamp == NonExistentFile) {
					newReference = null;
				} else {
					try {
						DocumentationProvider provider = null;
						try {
							string xmlName = Path.ChangeExtension (path, ".xml");
							if (File.Exists (xmlName)) {
								provider = XmlDocumentationProvider.CreateFromFile (xmlName);
							} else {
								provider = RoslynDocumentationProvider.Instance;
							}
						} catch (Exception e) {
							LoggingService.LogError ("Error while creating xml documentation provider for: " + path, e);
						}
						newReference = MetadataReference.CreateFromFile (path, properties, provider);
					} catch (Exception e) {
						LoggingService.LogError ("Error while loading reference " + path + ": " + e.Message, e);
						newReference = null;
					}
				}
				weakReference = new WeakReference<PortableExecutableReference> (newReference);
				return newReference;
			}

			public void Dispose ()
			{
				if (fileChangeTracker == null)
					return;
				fileChangeTracker.UpdatedOnDisk -= FileChangeTracker_UpdatedOnDisk;
				fileChangeTracker.Dispose ();
				fileChangeTracker = null;
				weakReference = null;
			}

			class RoslynDocumentationProvider : DocumentationProvider
			{
				internal static readonly DocumentationProvider Instance = new RoslynDocumentationProvider ();

				RoslynDocumentationProvider ()
				{
				}

				public override bool Equals (object obj)
				{
					return ReferenceEquals (this, obj);
				}

				public override int GetHashCode ()
				{
					return 42; // singleton
				}

				protected override string GetDocumentationForSymbol (string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default (CancellationToken))
				{
					return MonoDocDocumentationProvider.GetDocumentation (documentationMemberID);
				}
			}

			/// <summary>
			/// Helper class for working with FileSystemWatcher to observe any change on single file.
			/// </summary>
			internal class FileChangeTracker : IDisposable
			{
				public event EventHandler UpdatedOnDisk;

				FileSystemWatcher watcher;

				public string FilePath { get; }

				public FileChangeTracker (string filePath)
				{
					FilePath = filePath;
					watcher = new FileSystemWatcher (Path.GetDirectoryName (filePath), Path.GetFileName (filePath));
					watcher.Changed += OnChanged;
					watcher.Created += OnChanged;
					watcher.Deleted += OnChanged;
					watcher.Renamed += OnRenamed;
					watcher.EnableRaisingEvents = true;
				}

				private void OnRenamed (object sender, RenamedEventArgs e)
				{
					UpdatedOnDisk?.Invoke (this, e);
				}

				private void OnChanged (object sender, FileSystemEventArgs e)
				{
					UpdatedOnDisk?.Invoke (this, e);
				}

				public void Dispose ()
				{
					if (watcher != null) {
						watcher.Changed -= OnChanged;
						watcher.Created -= OnChanged;
						watcher.Deleted -= OnChanged;
						watcher.Renamed -= OnRenamed;
						watcher.Dispose ();
						watcher = null;
					}
				}
			}
		}
	}
}
