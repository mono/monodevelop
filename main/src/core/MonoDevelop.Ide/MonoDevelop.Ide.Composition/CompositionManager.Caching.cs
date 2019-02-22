//
// CompositionManager.Caching.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using Newtonsoft.Json;

namespace MonoDevelop.Ide.Composition
{
	public partial class CompositionManager
	{
		/// <summary>
		/// Used by tests to inject changes into the cache data in memory so it asserts whether caching can be used.
		/// </summary>
		internal interface ICachingFaultInjector
		{
			void FaultAssemblyInfo (MefControlCacheAssemblyInfo info);
		}

		/// <summary>
		/// Class used to validate whether a MEF cache is re-usable for a given set of assemblies.
		/// </summary>
		internal class Caching
		{
			readonly ICachingFaultInjector cachingFaultInjector;
			Task saveTask;
			readonly HashSet<Assembly> loadedAssemblies;
			public HashSet<Assembly> MefAssemblies { get; }
			internal string MefCacheFile { get; }
			internal string MefCacheControlFile { get; }

			public Caching (HashSet<Assembly> mefAssemblies, Func<string, string> getCacheFilePath = null, ICachingFaultInjector cachingFaultInjector = null)
			{
				getCacheFilePath = getCacheFilePath ?? (file => Path.Combine (AddinManager.CurrentAddin.PrivateDataPath, file));

				// TODO: .NET 4.7.2 has a capacity constructor - https://github.com/mono/monodevelop/issues/7198
				loadedAssemblies = new HashSet<Assembly> ();
				foreach (var asm in AppDomain.CurrentDomain.GetAssemblies ()) {
					if (asm.IsDynamic)
						continue;

					loadedAssemblies.Add (asm);
				}

				MefAssemblies = mefAssemblies;
				MefCacheFile = getCacheFilePath ("mef-cache");
				MefCacheControlFile = getCacheFilePath ("mef-cache-control");
				this.cachingFaultInjector = cachingFaultInjector;
			}

			void IdeApp_Exiting (object sender, ExitEventArgs args)
			{
				// As of the time this code was written, serializing the cache takes 200ms.
				// Maybe show a dialog and progress bar here that we're closing after save.
				// We cannot cancel the save, vs-mef doesn't use the cancellation tokens in the API.
				saveTask?.Wait ();
			}

			internal Stream OpenCacheStream () => File.Open (MefCacheFile, FileMode.Open);

			internal void DeleteFiles ()
			{
				try {
					File.Delete (MefCacheFile);
				} catch (Exception ex) {
					LoggingService.LogError ("Could not delete MEF cache file", ex);
				}

				try {
					File.Delete (MefCacheControlFile);
				} catch (Exception ex) {
					LoggingService.LogError ("Could not delete MEF cache control file", ex);
				}
			}

			internal bool CanUse (bool handleExceptions = true)
			{
				// If we don't have a control file, bail early
				if (!File.Exists (MefCacheControlFile) || !File.Exists (MefCacheFile))
					return false;

				// Read the cache from disk
				var serializer = new JsonSerializer ();
				MefControlCache controlCache;

				try {
					using (var sr = File.OpenText (MefCacheControlFile)) {
						controlCache = (MefControlCache)serializer.Deserialize (sr, typeof (MefControlCache));
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Could not deserialize MEF cache control", ex);
					DeleteFiles ();
					return false;
				}

				//this can return null (if the cache format changed?). clean up and start over.
				if (controlCache == null) {
					LoggingService.LogError ("MEF cache control deserialized as null");
					DeleteFiles ();
					return false;
				}

				try {
					// Short-circuit on number of assemblies change
					if (controlCache.MefAssemblyInfos.Count != MefAssemblies.Count)
						return false;

					if (!ValidateAssemblyCacheListIntegrity (MefAssemblies, controlCache.MefAssemblyInfos, cachingFaultInjector))
						return false;

					if (!ValidateAssemblyCacheListIntegrity (loadedAssemblies, controlCache.AdditionalInputAssemblyInfos, cachingFaultInjector))
						return false;
				} catch (Exception e) {
					if (!handleExceptions)
						throw;

					LoggingService.LogError ("MEF cache validation failed", e);
					return false;
				}

				return true;
			}

			static bool ValidateAssemblyCacheListIntegrity (HashSet<Assembly> assemblies, List<MefControlCacheAssemblyInfo> cachedAssemblyInfos, ICachingFaultInjector cachingFaultInjector)
			{
				var currentAssemblies = new Dictionary<string, Guid> (assemblies.Count);
				foreach (var asm in assemblies)
					currentAssemblies.Add (asm.Location, asm.ManifestModule.ModuleVersionId);

				foreach (var assemblyInfo in cachedAssemblyInfos) {
					cachingFaultInjector?.FaultAssemblyInfo (assemblyInfo);

					if (!currentAssemblies.TryGetValue (assemblyInfo.Location, out var mvid))
						return false;

					if (mvid != assemblyInfo.ModuleVersionId)
						return false;
				}
				return true;
			}

			internal Task Write (RuntimeComposition runtimeComposition, ComposableCatalog catalog, CachedComposition cacheManager)
			{
				return Runtime.RunInMainThread (async () => {
					IdeApp.Exiting += IdeApp_Exiting;

					saveTask = Task.Run (async () => {
						try {
							await WriteMefCache (runtimeComposition, catalog, cacheManager);
						} catch (Exception ex) {
							LoggingService.LogInternalError ("Failed to write MEF cache", ex);
						}
					});
					await saveTask;

					IdeApp.Exiting -= IdeApp_Exiting;
					saveTask = null;
				});
			}

			async Task WriteMefCache (RuntimeComposition runtimeComposition, ComposableCatalog catalog, CachedComposition cacheManager)
			{
				using (var timer = Counters.CompositionSave.BeginTiming ()) {
					WriteMefCacheControl (catalog, timer);

					// Serialize the MEF cache.
					using (var stream = File.Open (MefCacheFile, FileMode.Create)) {
						await cacheManager.SaveAsync (runtimeComposition, stream);
					}
				}
			}

			void WriteMefCacheControl (ComposableCatalog catalog, ITimeTracker timer)
			{
				var mefAssemblyNames = new HashSet<string> ();
				var mefAssemblyInfos = new List<MefControlCacheAssemblyInfo> ();

				foreach (var assembly in MefAssemblies) {
					mefAssemblyNames.Add (assembly.GetName ().ToString ());

					mefAssemblyInfos.Add (new MefControlCacheAssemblyInfo {
						Location = assembly.Location,
						ModuleVersionId = assembly.ManifestModule.ModuleVersionId,
					});
				}

				var additionalInputAssemblies = new List<MefControlCacheAssemblyInfo> ();
				var loadedMap = loadedAssemblies.ToDictionary (x => x.FullName, x => x);

				foreach (var asm in catalog.GetInputAssemblies ()) {
					var assemblyName = asm.ToString ();
					if (mefAssemblyNames.Contains (assemblyName))
						continue;

					bool found = loadedMap.TryGetValue (assemblyName, out var assembly);
					System.Diagnostics.Debug.Assert (found);

					additionalInputAssemblies.Add (new MefControlCacheAssemblyInfo {
						Location = assembly.Location,
						ModuleVersionId = assembly.ManifestModule.ModuleVersionId,
					});
				}

				// Create cache control data.
				var controlCache = new MefControlCache {
					MefAssemblyInfos = mefAssemblyInfos,
					AdditionalInputAssemblyInfos = additionalInputAssemblies,
				};

				var serializer = new JsonSerializer ();

				using (var fs = File.Open (MefCacheControlFile, FileMode.Create))
				using (var sw = new StreamWriter (fs)) {
					serializer.Serialize (sw, controlCache);
				}
				timer.Trace ("Composition control file written");
			}
		}

		[Serializable]
		class MefControlCache
		{
			[JsonRequired]
			public List<MefControlCacheAssemblyInfo> MefAssemblyInfos;

			[JsonRequired]
			public List<MefControlCacheAssemblyInfo> AdditionalInputAssemblyInfos;
		}

		[Serializable]
		internal class MefControlCacheAssemblyInfo
		{
			[JsonRequired]
			public string Location;

			[JsonRequired]
			public Guid ModuleVersionId;
		}
	}
}
