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

namespace MonoDevelop.Ide.TypeSystem
{
	//FIXME: this mechanism is not correct, we should be implementing IMetadataService instead
	static class MetadataReferenceCache
	{
		static Dictionary<string, MetadataReferenceCacheItem> cache = new Dictionary<string, MetadataReferenceCacheItem> ();

		public static MetadataReference LoadReference (ProjectId projectId, string path)
		{
			lock (cache) {
				MetadataReferenceCacheItem result;
				if (!cache.TryGetValue (path, out result)) {
					result = new MetadataReferenceCacheItem (path);
					cache.Add (path, result);
				}
				result.InUseBy.Add (projectId);
				return result.Reference;
			}
		}

		//TODO: This should be called when reference is actually removed and not on
		//project reload because if this is only project that has this reference... Cache will be
		//invalidated and when reload comes back in few miliseconds it will need to reload reference again
		public static void RemoveReference (ProjectId projectId, string path)
		{
			lock (cache) {
				MetadataReferenceCacheItem result;
				if (cache.TryGetValue (path, out result)) {
					result.InUseBy.Remove (projectId);
					if (result.InUseBy.Count == 0) {
						cache.Remove (path);
					}
				}
			}
		}

		public static void RemoveReferences (ProjectId id)
		{
			lock (cache) {
				var toRemove = new List<string> ();
				foreach (var val in cache) {
					val.Value.InUseBy.Remove (id);
					if (val.Value.InUseBy.Count == 0) {
						toRemove.Add (val.Key);
					}
				}
				toRemove.ForEach ((k) => cache.Remove (k));
			}
		}

		public static void Clear ()
		{
			lock (cache) {
				cache.Clear ();
			}
		}

		#pragma warning disable 414
		static Timer timer;
		#pragma warning restore 414

		static MetadataReferenceCache ()
		{
			timer = new Timer ((o) => Runtime.RunInMainThread ((Action)CheckForChanges), null, 5000, 5000);
		}

		//TODO: Call this method when focus returns to MD or even better use FileSystemWatcher
		public static void CheckForChanges ()
		{
			lock (cache) {
				foreach (var value in cache.Values) {
					value.CheckForChange ();
				}
			}
		}

		class MetadataReferenceCacheItem
		{
			public HashSet<ProjectId> InUseBy { get; private set; }

			public MetadataReference Reference { get; private set; }

			readonly string path;

			DateTime timeStamp;

			public MetadataReferenceCacheItem (string path)
			{
				this.path = path;
				CreateNewReference ();
				InUseBy = new HashSet<ProjectId> ();
			}

			public void CheckForChange ()
			{
				if (timeStamp != File.GetLastWriteTimeUtc (path)) {
					if (Reference != null) {
						foreach (var solution in IdeApp.Workspace.GetAllSolutions ()) {
							var workspace = TypeSystemService.GetWorkspace (solution);
							foreach (var projId in InUseBy) {
								while (true) {
									var project = workspace.CurrentSolution.GetProject (projId);
									if (project == null)
										break;
									if (workspace.TryApplyChanges (project.RemoveMetadataReference (Reference).Solution))
										break;
								}
							}
						}
					}
					CreateNewReference ();
					if (Reference != null) {
						foreach (var solution in IdeApp.Workspace.GetAllSolutions ()) {
							var workspace = TypeSystemService.GetWorkspace (solution);
							foreach (var projId in InUseBy) {
								while (true) {
									var project = workspace.CurrentSolution.GetProject (projId);
									if (project == null)
										break;
									if (workspace.TryApplyChanges (project.AddMetadataReference (Reference).Solution))
										break;
								}
							}
						}
					}
				}
			}

			readonly static DateTime NonExistentFile = new DateTime (1601, 1, 1);

			void CreateNewReference ()
			{
				timeStamp = File.GetLastWriteTimeUtc (path);
				if (timeStamp == NonExistentFile) {
					Reference = null;
				} else {
					try {
						DocumentationProvider provider = null;
						try {
							string xmlName = Path.ChangeExtension (path, ".xml");
							if (File.Exists (xmlName)) {
								provider = Microsoft.CodeAnalysis.XmlDocumentationProvider.CreateFromFile (xmlName);
							} else {
								provider = RoslynDocumenentationProvider.Instance;
							}
						} catch (Exception e) {
							LoggingService.LogError ("Error while creating xml documentation provider for: " + path, e);
						}
						Reference = MetadataReference.CreateFromFile (path, MetadataReferenceProperties.Assembly, provider);
					} catch (Exception e) {
						LoggingService.LogError ("Error while loading reference " + path + ": " + e.Message, e); 
					}
				}
			}


			class RoslynDocumenentationProvider : DocumentationProvider
			{
				internal static readonly DocumentationProvider Instance = new RoslynDocumenentationProvider ();

				RoslynDocumenentationProvider ()
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
		}
	}
}
