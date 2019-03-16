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
		// TODO: Make this into simple classId -> classInfo, objectId -> objectInfo
		public readonly Dictionary<long, HeapRootRegisterEvent> Roots;
		public readonly Dictionary<long, ClassLoadEvent> ClassInfos;
		public readonly Dictionary<long, long> ObjectToType;
		public readonly Dictionary<long, List<long>> TypeToObjectList;
		public readonly Dictionary<string, (int, long)> ObjectCounts;

		// TODO: Use a TaggedEdge where we add ObjectInfo
		public readonly ReversedBidirectionalGraph<long, Edge<long>> Graph;

		public Heapshot (NativeHeapshot nativeHeapshot)
		{
			Roots = nativeHeapshot.Roots;
			ClassInfos = nativeHeapshot.ClassInfos;
			ObjectToType = nativeHeapshot.ObjectToType;
			TypeToObjectList = nativeHeapshot.TypeToObjectList;

			ObjectCounts = CreateObjectCountMap (nativeHeapshot);

			var graphWithInReferences = new BidirectionAdapterGraph<long, Edge<long>> (nativeHeapshot.Graph);
			// Construct the in-edge graph, so we can trace an object's retention path.
			Graph = new ReversedBidirectionalGraph<long, Edge<long>> (graphWithInReferences);
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
	}
}
