using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MonoDevelop.StressTest.Attributes;
using Newtonsoft.Json;

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

			var trackedLeaks = GetAttributesForScenario (isCleanup);
			if (trackedLeaks.Count == 0)
				return new Dictionary<string, LeakItem> ();

			Console.WriteLine ("Live objects count per type:");
			var leakedObjects = new Dictionary<string, LeakItem> (trackedLeaks.Count);

			foreach (var kvp in trackedLeaks) {
				var name = kvp.Key;

				if (heapshot.ObjectCounts.TryGetValue (name, out var count)) {
					// We need to check if the root is finalizer or ephemeron, and not report the value.
					leakedObjects.Add (name, new LeakItem (name, count));
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

		Dictionary<string, NoLeakAttribute> GetAttributesForScenario (bool isCleanup)
		{
			var scenarioType = scenario.GetType ();

			// If it's targeting the class, check on cleanup iteration, otherwise, check the run method.
			var member = isCleanup ? scenarioType : (MemberInfo)scenarioType.GetMethod ("Run");

			var attributes = member.GetCustomAttributes<NoLeakAttribute> (true).ToDictionary (x => x.TypeName, x => x);

			// TODO: Ensure that we don't leak, so add GtkWidgetResult results, as they can cause retention of UI widgets.
			// attributes.Add("MonoDevelop.Components.AutoTest.Results.GtkWidgetResult

			return attributes;
		}
	}
}
