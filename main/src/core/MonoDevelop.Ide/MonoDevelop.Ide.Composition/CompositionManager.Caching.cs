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
			internal static bool writeCache;
			readonly ICachingFaultInjector cachingFaultInjector;
			Task saveTask;
			public HashSet<Assembly> Assemblies { get; }
			internal string MefCacheFile { get; }
			internal string MefCacheControlFile { get; }

			public Caching (HashSet<Assembly> assemblies, Func<string, string> getCacheFilePath = null, ICachingFaultInjector cachingFaultInjector = null)
			{
				getCacheFilePath = getCacheFilePath ?? (file => Path.Combine (AddinManager.CurrentAddin.PrivateDataPath, file));

				Assemblies = assemblies;
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

			internal bool CanUse ()
			{
				// If we don't have a control file, bail early
				if (!File.Exists (MefCacheControlFile) || !File.Exists (MefCacheFile))
					return false;

				using (var timer = Counters.CompositionCacheControl.BeginTiming ()) {
					// Read the cache from disk
					var serializer = new JsonSerializer ();
					MefControlCache controlCache;

					try {
						using (var sr = File.OpenText (MefCacheControlFile)) {
							controlCache = (MefControlCache)serializer.Deserialize (sr, typeof(MefControlCache));
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

					var currentAssemblies = new HashSet<string> (Assemblies.Select (asm => asm.Location));

					// Short-circuit on number of assemblies change
					if (controlCache.AssemblyInfos.Length != currentAssemblies.Count)
						return false;

					// Validate that the assemblies match and we have the same time stamps on them.
					foreach (var assemblyInfo in controlCache.AssemblyInfos) {
						cachingFaultInjector?.FaultAssemblyInfo (assemblyInfo);
						if (!currentAssemblies.Contains (assemblyInfo.Location))
							return false;

						if (File.GetLastWriteTimeUtc (assemblyInfo.Location) != assemblyInfo.LastWriteTimeUtc)
							return false;
					}
				}

				return true;
			}

			internal Task Write (RuntimeComposition runtimeComposition, CachedComposition cacheManager)
			{
				return Runtime.RunInMainThread (async () => {
					IdeApp.Exiting += IdeApp_Exiting;

					saveTask = Task.Run (async () => {
						try {
							await WriteMefCache (runtimeComposition, cacheManager);
						} catch (Exception ex) {
							LoggingService.LogError ("Failed to write MEF cache", ex);
						}
					});
					await saveTask;

					IdeApp.Exiting -= IdeApp_Exiting;
					saveTask = null;
				});
			}

			async Task WriteMefCache (RuntimeComposition runtimeComposition, CachedComposition cacheManager)
			{
				using (var timer = Counters.CompositionSave.BeginTiming ()) {
					WriteMefCacheControl (timer);

					// Serialize the MEF cache.
					using (var stream = File.Open (MefCacheFile, FileMode.Create)) {
						await cacheManager.SaveAsync (runtimeComposition, stream);
					}
				}
			}

			void WriteMefCacheControl (ITimeTracker timer)
			{
				// Create cache control data.
				var controlCache = new MefControlCache {
					AssemblyInfos = Assemblies.Select (asm => new MefControlCacheAssemblyInfo {
						Location = asm.Location,
						LastWriteTimeUtc = File.GetLastWriteTimeUtc (asm.Location),
					}).ToArray (),
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
			public MefControlCacheAssemblyInfo [] AssemblyInfos;
		}

		[Serializable]
		internal class MefControlCacheAssemblyInfo
		{
			[JsonRequired]
			public string Location;

			[JsonRequired]
			public DateTime LastWriteTimeUtc;
		}
	}
}
