using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Profiler.Log;
using QuickGraph;

namespace MonoDevelop.StressTest
{
	public class NativeHeapshot
	{
		public readonly Dictionary<long, HeapRootRegisterEvent> Roots = new Dictionary<long, HeapRootRegisterEvent> ();
		public readonly Dictionary<long, int> ObjectsPerClassCounter = new Dictionary<long, int> ();
		public Dictionary<long, ClassLoadEvent> ClassInfos;
		public readonly Dictionary<long, long> ObjectToType = new Dictionary<long, long> ();
		public readonly Dictionary<long, List<long>> TypeToObjectList = new Dictionary<long, List<long>> ();

		public AdjacencyGraph<long, Edge<long>> Graph = new AdjacencyGraph<long, Edge<long>> ();
	}

	public class Heapshot
	{
		public readonly Dictionary<long, HeapRootRegisterEvent> Roots;
		public readonly Dictionary<long, ClassLoadEvent> ClassInfos;
		public readonly Dictionary<long, long> ObjectToType;
		public readonly Dictionary<long, List<long>> TypeToObjectList;
		public readonly Dictionary<string, (int, long)> ObjectCounts;

		public readonly BidirectionAdapterGraph<long, Edge<long>> Graph;

		public Heapshot (NativeHeapshot nativeHeapshot)
		{
			Roots = nativeHeapshot.Roots;
			ClassInfos = nativeHeapshot.ClassInfos;
			ObjectToType = nativeHeapshot.ObjectToType;
			TypeToObjectList = nativeHeapshot.TypeToObjectList;

			ObjectCounts = CreateObjectCountMap (nativeHeapshot);

			// Construct the in-edge graph, so we can trace an object's retention path.
			Graph = new BidirectionAdapterGraph<long, Edge<long>> (nativeHeapshot.Graph);
			// TODO: Create inversed gra
		}

		static Dictionary<string, (int, long)> CreateObjectCountMap (NativeHeapshot nativeHeapshot)
		{
			var result = new Dictionary<string, (int, long)> ();

			foreach (var kvp in nativeHeapshot.ObjectsPerClassCounter) {
				long classId = kvp.Key;
				string name = nativeHeapshot.ClassInfos[classId].Name;
				int count = kvp.Value;

				if (count > 0)
					result[name] = (count, classId);
			}

			return result;
		}


		//public static IEnumerable<(LogHeapRootSource, long[])> SearchRoots(this Heapshot heapshot, long objAddr)
		//{
		//	var queue = new Queue<long[]> ();
		//	var visited = new HashSet<long> ();
		//	visited.Add (objAddr);

		//	var lessImportantRoots = new List<(LogHeapRootSource, long[])> ();
		//	queue.Enqueue (new long[] { objAddr });

		//	while (queue.Any ()) {
		//		var cur = queue.Dequeue ();

		//		var node = cur[cur.Length - 1];
		//		if (heapshot.Roots.TryGetValue (node, out var root)) {
		//			if (root.Source == LogHeapRootSource.Ephemeron || root.Source == LogHeapRootSource.FinalizerQueue)
		//				lessImportantRoots.Add ((root.Source, cur));
		//			else
		//				yield return (root.Source, cur);
		//		}

		//		foreach (var child in GetReferencedFrom (node)) {
		//			if (!visited.Add (child))
		//				continue;

		//			var newPath = new long[cur.Length + 1];
		//			Array.Copy (cur, 0, newPath, 0, cur.Length);
		//			newPath[cur.Length] = child;
		//			queue.Enqueue (newPath);
		//		}
		//	}

		//	foreach (var lir in lessImportantRoots) {
		//		yield return lir;
		//	}
		//}

	}
}
