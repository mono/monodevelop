// CompositionManager.cs
//
// Author:
//   Kirill Osenkov <https://github.com/KirillOsenkov>
//
// Copyright (c) 2017 Microsoft
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
//
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.Ide.Composition
{
	/// <summary>
	/// The host of the MonoDevelop MEF composition. Uses https://github.com/Microsoft/vs-mef.
	/// </summary>
	public partial class CompositionManager
	{
		static Task<CompositionManager> creationTask;
		static CompositionManager instance;

		static readonly Resolver StandardResolver = Resolver.DefaultInstance;
		static readonly PartDiscovery Discovery = PartDiscovery.Combine (
			new AttributedPartDiscoveryV1 (StandardResolver),
			new AttributedPartDiscovery (StandardResolver, true));

		public static CompositionManager Instance {
			get {
				if (instance == null) {
					var task = InitializeAsync ();
					if (!task.IsCompleted && Runtime.IsMainThread) {
						LoggingService.LogInfo ("UI thread queried MEF while it was still being built:{0}{1}", Environment.NewLine, Environment.StackTrace);
					}
					instance = task.Result;
				}

				return instance;
			}
		}

		/// <summary>
		/// Starts initializing the MEF composition on a background thread. Thread-safe.
		/// </summary>
		public static Task<CompositionManager> InitializeAsync ()
		{
			if (creationTask == null) {
				lock (typeof (CompositionManager)) {
					if (creationTask == null) {
						creationTask = Task.Run (() => CreateInstanceAsync ());
					}
				}
			}

			return creationTask;
		}

		/// <summary>
		/// Returns an instance of type T that is exported by some composition part. The instance is shared (singleton).
		/// </summary>
		public static T GetExportedValue<T> () => Instance.ExportProvider.GetExportedValue<T> ();

		/// <summary>
		/// Returns all instances of type T that are exported by some composition part. The instances are shared (singletons).
		/// </summary>
		public static IEnumerable<T> GetExportedValues<T> () => Instance.ExportProvider.GetExportedValues<T> ();

		/// <summary>
		/// Returns a lazy holding the instance of type T that is exported by some composition part. The instance is shared (singleton).
		/// </summary>
		public static Lazy<T> GetExport<T> () => new Lazy<T> (() => Instance.ExportProvider.GetExportedValue<T> ());

		/// <summary>
		/// Returns a lazy holding all instances of type T that are exported by some composition part. The instances are shared (singletons).
		/// </summary>
		public static Lazy<IEnumerable<T>> GetExports<T> () => new Lazy<IEnumerable<T>> (() => Instance.ExportProvider.GetExportedValues<T> ());

		public RuntimeComposition RuntimeComposition { get; private set; }
		public IExportProviderFactory ExportProviderFactory { get; private set; }
		public ExportProvider ExportProvider { get; private set; }
		public HostServices HostServices { get; private set; }
		public System.ComponentModel.Composition.Hosting.ExportProvider ExportProviderV1 { get; private set; }

		internal CompositionManager ()
		{
		}

		static async Task<CompositionManager> CreateInstanceAsync ()
		{
			var compositionManager = new CompositionManager ();
			await compositionManager.InitializeInstanceAsync ();
			return compositionManager;
		}

		async Task InitializeInstanceAsync ()
		{
			var timings = new Dictionary<string, long> ();
			var metadata = new CompositionLoadMetadata (timings);

			using (var timer = Counters.CompositionLoad.BeginTiming (metadata)) {
				var fullTimer = System.Diagnostics.Stopwatch.StartNew ();
				var stepTimer = System.Diagnostics.Stopwatch.StartNew ();

				var mefAssemblies = ReadAssembliesFromAddins (timer);
				timings ["ReadFromAddins"] = stepTimer.ElapsedMilliseconds;
				stepTimer.Restart ();

				var caching = new Caching (mefAssemblies);

				// Try to use cached MEF data

				var canUse = metadata.ValidCache = caching.CanUse ();
				if (canUse) {
					LoggingService.LogInfo ("Creating MEF composition from cache");
					RuntimeComposition = await TryCreateRuntimeCompositionFromCache (caching);
				}
				timings ["LoadFromCache"] = stepTimer.ElapsedMilliseconds;
				stepTimer.Restart ();

				// Otherwise fallback to runtime discovery.
				if (RuntimeComposition == null) {
					LoggingService.LogInfo ("Creating MEF composition from runtime");
					var (runtimeComposition, catalog) = await CreateRuntimeCompositionFromDiscovery (caching, timer);
					RuntimeComposition = runtimeComposition;

					CachedComposition cacheManager = new CachedComposition ();
					caching.Write (RuntimeComposition, catalog, cacheManager).Ignore ();
				}
				timings ["LoadRuntimeComposition"] = stepTimer.ElapsedMilliseconds;
				stepTimer.Restart ();

				ExportProviderFactory = RuntimeComposition.CreateExportProviderFactory ();
				ExportProvider = ExportProviderFactory.CreateExportProvider ();
				HostServices = MefV1HostServices.Create (ExportProvider.AsExportProvider ());
				ExportProviderV1 = NetFxAdapters.AsExportProvider (ExportProvider);

				timings ["CreateServices"] = stepTimer.ElapsedMilliseconds;
				metadata.Duration = fullTimer.ElapsedMilliseconds;
			}
		}

		internal static async Task<RuntimeComposition> TryCreateRuntimeCompositionFromCache (Caching caching)
		{
			var cacheManager = new CachedComposition ();

			try {
				using (var cacheStream = caching.OpenCacheStream ()) {
					return await cacheManager.LoadRuntimeCompositionAsync (cacheStream, StandardResolver);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Could not deserialize MEF cache", ex);
				caching.DeleteFiles ();
			}
			return null;
		}

		internal static async Task<(RuntimeComposition, ComposableCatalog)> CreateRuntimeCompositionFromDiscovery (Caching caching, ITimeTracker timer = null)
		{
			var parts = await Discovery.CreatePartsAsync (caching.MefAssemblies);
			timer?.Trace ("Composition parts discovered");

			ComposableCatalog catalog = ComposableCatalog.Create (StandardResolver)
				.WithCompositionService ()
				.AddParts (parts);

			var discoveryErrors = catalog.DiscoveredParts.DiscoveryErrors;
			if (!discoveryErrors.IsEmpty) {
				foreach (var error in discoveryErrors) {
					LoggingService.LogInfo ("MEF discovery error", error);
				}

				// throw new ApplicationException ("MEF discovery errors");
			}

			CompositionConfiguration configuration = CompositionConfiguration.Create (catalog);

			if (!configuration.CompositionErrors.IsEmpty) {
				// capture the errors in an array for easier debugging
				var errors = configuration.CompositionErrors.SelectMany (e => e).ToArray ();
				foreach (var error in errors) {
					LoggingService.LogInfo ("MEF composition error: " + error.Message);
				}

				// For now while we're still transitioning to VSMEF it's useful to work
				// even if the composition has some errors. TODO: re-enable this.
				//configuration.ThrowOnErrors ();
			}
			timer?.Trace ("Composition configured");

			var runtimeComposition = RuntimeComposition.CreateRuntimeComposition (configuration);
			timer?.Trace ("Composition created");

			return (runtimeComposition, catalog);
		}

		internal static HashSet<Assembly> ReadAssembliesFromAddins (ITimeTracker<CompositionLoadMetadata> timer = null)
		{
			var readAssemblies = new HashSet<Assembly> ();

			timer?.Trace ("Start: reading assemblies");
			ReadAssemblies (readAssemblies, "/MonoDevelop/Ide/TypeService/PlatformMefHostServices");
			ReadAssemblies (readAssemblies, "/MonoDevelop/Ide/TypeService/MefHostServices");
			ReadAssemblies (readAssemblies, "/MonoDevelop/Ide/Composition");
			timer?.Trace ("Start: end reading assemblies");

			return readAssemblies;

			void ReadAssemblies (HashSet<Assembly> assemblies, string extensionPath)
			{
				foreach (var node in AddinManager.GetExtensionNodes (extensionPath)) {
					if (node is AssemblyExtensionNode assemblyNode) {
						try {
							string id = assemblyNode.Addin.Id;
							string assemblyName = assemblyNode.FileName;
							// Make sure the add-in that registered the assembly is loaded, since it can bring other
							// other assemblies required to load this one

							AddinManager.LoadAddin (null, id);

							var assemblyFilePath = assemblyNode.Addin.GetFilePath (assemblyNode.FileName);
							var assembly = Runtime.LoadAssemblyFrom (assemblyFilePath);
							assemblies.Add (assembly);
						} catch (Exception e) {
							LoggingService.LogError ("Composition can't load assembly: " + assemblyNode.FileName, e);
						}
					}
				}
			}
		}
	}
}