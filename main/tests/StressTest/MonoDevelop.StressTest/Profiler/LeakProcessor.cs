using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using MonoDevelop.StressTest.MonoDevelop.StressTest.Profiler;
using QuickGraph.Algorithms.Search;
using QuickGraph.Algorithms.Observers;
using QuickGraph;
using QuickGraph.Graphviz;

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
				serializer.Serialize (sw, this);
			}
		}

		public void Process (Heapshot heapshot, bool isCleanup, string iterationName)
		{
			if (heapshot == null)
				return;

			var previousData = result.Iterations.LastOrDefault ();
			var leakedObjects = DetectLeakedObjects (heapshot, isCleanup, previousData, iterationName);
			var leakResult = new ResultIterationData (iterationName, leakedObjects) {
				//MemoryStats = memoryStats,
			};

			result.Iterations.Add (leakResult);
		}

		Dictionary<string, LeakItem> DetectLeakedObjects (Heapshot heapshot, bool isCleanup, ResultIterationData previousData, string iterationName)
		{
			if (ProfilerOptions.Type == ProfilerOptions.ProfilerType.Disabled)
				return new Dictionary<string, LeakItem> ();

			var trackedLeaks = scenario.GetLeakAttributes (isCleanup);
			if (trackedLeaks.Count == 0)
				return new Dictionary<string, LeakItem> ();

			Directory.CreateDirectory (graphsDirectory);

			Console.WriteLine ("Live objects count per type:");
			var leakedObjects = new Dictionary<string, LeakItem> (trackedLeaks.Count);

			foreach (var kvp in trackedLeaks) {
				var name = kvp.Key;

				if (heapshot.ObjectCounts.TryGetValue (name, out var tuple)) {
					var (count, typeId) = tuple;

					var resultFile = ReportPathsToRoots (heapshot, typeId, iterationName);

					// We need to check if the root is finalizer or ephemeron, and not report the value.
					leakedObjects.Add (name, new LeakItem (name, count, resultFile));
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

		string ReportPathsToRoots(Heapshot heapshot, long typeId, string iterationName)
		{
			var rootTypeName = heapshot.ClassInfos[typeId].Name;
			var objects = heapshot.TypeToObjectList[typeId];

			// TODO: Iterate a finite number of objects and group by similar retention
			var obj = objects.First ();

			var objectGraph = heapshot.Graph.GetObjectGraph (obj);

			var graphviz = new GraphvizAlgorithm<long, SReversedEdge<long, Edge<long>>> (objectGraph);
			graphviz.FormatVertex += (sender, e) => {
				var currentObj = e.Vertex;

				// Look up the object and set its type name.
				var currentType = heapshot.ObjectToType[currentObj];
				var typeName = heapshot.ClassInfos[currentType].Name;
				e.VertexFormatter.Label = typeName;

				// Append root information.
				if (heapshot.Roots.TryGetValue(currentObj, out var rootRegisterEvent)) {
					e.VertexFormatter.Label += "\n" + rootRegisterEvent.Source.ToString ();
					e.VertexFormatter.Shape = QuickGraph.Graphviz.Dot.GraphvizVertexShape.Box;
				}
			};

			var outputPath = Path.Combine (graphsDirectory, iterationName + "_" + rootTypeName + ".dot");
			File.WriteAllText ("graph.dot", outputPath);

			return outputPath;
		}
	}
}
