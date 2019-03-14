using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using MonoDevelop.StressTest.MonoDevelop.StressTest.Profiler;
using QuickGraph.Algorithms.Search;
using QuickGraph.Algorithms.Observers;
using QuickGraph;

namespace MonoDevelop.StressTest
{
	public class LeakProcessor
	{
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
				serializer.Serialize (sw, this);
			}
		}

		public void Process (Heapshot heapshot, bool isCleanup, string iterationName)
		{
			if (heapshot == null)
				return;

			var previousData = result.Iterations.LastOrDefault ();
			var leakedObjects = DetectLeakedObjects (heapshot, isCleanup, previousData);
			var leakResult = new ResultIterationData (iterationName, leakedObjects) {
				//MemoryStats = memoryStats,
			};

			result.Iterations.Add (leakResult);
		}

		Dictionary<string, LeakItem> DetectLeakedObjects (Heapshot heapshot, bool isCleanup, ResultIterationData previousData)
		{
			if (ProfilerOptions.Type == ProfilerOptions.ProfilerType.Disabled)
				return new Dictionary<string, LeakItem> ();

			var trackedLeaks = scenario.GetLeakAttributes (isCleanup);
			if (trackedLeaks.Count == 0)
				return new Dictionary<string, LeakItem> ();

			Console.WriteLine ("Live objects count per type:");
			var leakedObjects = new Dictionary<string, LeakItem> (trackedLeaks.Count);

			bool doneOnce = false;

			foreach (var kvp in trackedLeaks) {
				var name = kvp.Key;

				if (heapshot.ObjectCounts.TryGetValue (name, out var tuple)) {
					var (count, typeId) = tuple;
					// We need to check if the root is finalizer or ephemeron, and not report the value.
					leakedObjects.Add (name, new LeakItem (name, count));

					if (!doneOnce) {
						PrintPathsToRoots (heapshot, typeId);
						doneOnce = true;
					}
				}
			}

			foreach (var kvp in leakedObjects) {
				var leak = kvp.Value;
				int delta = 0;
				if (previousData.Leaks.TryGetValue (kvp.Key, out var previousLeak)) {
					int previousCount = previousLeak.Count;
					delta = previousCount - leak.Count;
				}

				Console.WriteLine ("{0}: {1} {2:+0;-#}", leak.ClassName, leak.Count, delta);
			}
			return leakedObjects;
		}

		void PrintPathsToRoots(Heapshot heapshot, long typeId)
		{
			var objects = heapshot.TypeToObjectList[typeId];

			var obj = objects.First ();

			var bfsa = new BreadthFirstSearchAlgorithm<long, Edge<long>> (heapshot.Graph);
			var vis = new VertexPredecessorRecorderObserver<long, Edge<long>> ();
			vis.Attach (bfsa);
			var visitedRoots = new HashSet<long> ();
			bfsa.ExamineVertex += (vertex) => {
				if (heapshot.Roots.ContainsKey (vertex)) {
					visitedRoots.Add (vertex);
					if (visitedRoots.Count == 5) {
						bfsa.Services.CancelManager.Cancel ();
					}
				}
			};
			bfsa.Compute (obj);
			foreach (var root in visitedRoots) {
				Console.WriteLine ("root:");
				if (vis.TryGetPath (root, out var path)) {
					foreach (var edge in path) {
						var source = GetName (heapshot, edge.Source);
						var target = GetName (heapshot, edge.Target);

						Console.WriteLine ("{0} -> {1}", source, target);
					}
				}
			}
		}

		string GetName(Heapshot heapshot, long objectAddr)
		{
			var typeId = heapshot.ObjectToType[objectAddr];
			var name = heapshot.ClassInfos[typeId].Name;

			return name;
		}
	}
}
