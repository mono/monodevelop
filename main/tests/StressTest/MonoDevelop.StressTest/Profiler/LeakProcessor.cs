using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using QuickGraph.Algorithms.Search;
using QuickGraph.Algorithms.Observers;
using QuickGraph;
using QuickGraph.Graphviz;
using System.Threading.Tasks;
using System.Diagnostics;
using Mono.Profiler.Log;

namespace MonoDevelop.StressTest
{
	public class LeakProcessor
	{
		const string graphsDirectory = "graphs";

		readonly ITestScenario scenario;
		readonly ResultDataModel result = new ResultDataModel ();

		public ProfilerOptions ProfilerOptions { get; }

		public LeakProcessor (ITestScenario scenario, ProfilerOptions options)
		{
			ProfilerOptions = options;
			this.scenario = scenario;
		}

		public void ReportResult ()
		{
			string scenarioName = scenario.GetType ().FullName;
			var serializer = new JsonSerializer {
				NullValueHandling = NullValueHandling.Ignore,
			};

			using (var fs = new FileStream (scenarioName + "_Result.json", FileMode.Create, FileAccess.Write))
			using (var sw = new StreamWriter (fs)) {
				serializer.Serialize (sw, result);
			}
		}

		public void Process (Heapshot heapshot, bool isCleanup, string iterationName, Components.AutoTest.AutoTestSession.MemoryStats memoryStats)
		{
			if (heapshot == null)
				return;

			// TODO: Make this async.

			var previousData = result.Iterations.LastOrDefault ();
			var leakedObjects = DetectLeakedObjects (heapshot, isCleanup, previousData, iterationName);
			var leakResult = new ResultIterationData (iterationName, leakedObjects, memoryStats);

			result.Iterations.Add (leakResult);
		}

		Dictionary<string, LeakItem> DetectLeakedObjects (Heapshot heapshot, bool isCleanup, ResultIterationData previousData, string iterationName)
		{
			if (heapshot == null || ProfilerOptions.Type == ProfilerOptions.ProfilerType.Disabled)
				return new Dictionary<string, LeakItem> ();

			var trackedLeaks = scenario.GetLeakAttributes (isCleanup);
			if (trackedLeaks.Count == 0)
				return new Dictionary<string, LeakItem> ();

			Directory.CreateDirectory (graphsDirectory);

			Console.WriteLine ("Live objects count per type:");
			var leakedObjects = new Dictionary<string, LeakItem> (trackedLeaks.Count);

			foreach (var kvp in trackedLeaks) {
				var name = kvp.Key;

				if (!heapshot.TryGetHeapshotTypeInfo (name, out var heapshotTypeInfo)) {
					continue;
				}

				var resultFile = ReportPathsToRoots (heapshot, heapshotTypeInfo, iterationName, out int objectCount);
				if (resultFile == null) {
					// We have determined the leak is not an actual leak.
					continue;
				}

				// We need to check if the root is finalizer or ephemeron, and not report the value.
				leakedObjects.Add (name, new LeakItem (name, objectCount, resultFile));
			}

			foreach (var kvp in leakedObjects) {
				var leak = kvp.Value;
				int delta = 0;
				if (previousData != null && previousData.Leaks.TryGetValue (kvp.Key, out var previousLeak)) {
					int previousCount = previousLeak.Count;
					delta = leak.Count - previousCount;
				}

				Console.WriteLine ("{0}: {1} {2:+0;-#}", leak.ClassName, leak.Count, delta);
			}
			return leakedObjects;
		}

		bool IsActualLeakSource (LogHeapRootSource rootKind)
		{
			return rootKind == LogHeapRootSource.Static
				|| rootKind == LogHeapRootSource.ContextStatic
				|| rootKind == LogHeapRootSource.GCHandle
				|| rootKind == LogHeapRootSource.ThreadStatic;
		}

		string ReportPathsToRoots(Heapshot heapshot, HeapshotTypeInfo typeInfo, string iterationName, out int objectCount)
		{
			string outputPath = null;

			var rootTypeName = typeInfo.TypeInfo.Name;
			var objects = typeInfo.Objects;
			objectCount = objects.Count;

			// Look for the first object that is definitely leaked.
			foreach (var obj in objects) {
				bool isLeak = false;

				var objectGraph = heapshot.Graph.GetObjectGraph (obj, vertex => {
					isLeak |= (heapshot.Roots.TryGetValue (vertex.Address, out var heapRootRegisterEvent) && IsActualLeakSource (heapRootRegisterEvent.Source));
				});

				if (outputPath == null && isLeak) {
					var graphviz = objectGraph.ToLeakGraphviz (heapshot);

					outputPath = Path.Combine (graphsDirectory, iterationName + "_" + rootTypeName + ".dot");
					File.WriteAllText (outputPath, graphviz.Generate ());
				} else
					objectCount--;
			}

			// We have not found a definite leak if outputPath is null.
			return outputPath;
		}
	}
}
