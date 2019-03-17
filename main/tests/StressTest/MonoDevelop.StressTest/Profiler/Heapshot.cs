using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Profiler.Log;
using QuickGraph;

namespace MonoDevelop.StressTest
{
	public class NativeHeapshot
	{
		public readonly Dictionary<string, long> TrackedTypes = new Dictionary<string, long> ();
		public readonly Dictionary<long, HeapshotTypeInfo> Types = new Dictionary<long, HeapshotTypeInfo> ();
		public readonly Dictionary<long, HeapObject> Objects = new Dictionary<long, HeapObject> ();
		public readonly Dictionary<long, HeapRootRegisterEvent> Roots = new Dictionary<long, HeapRootRegisterEvent> ();
		readonly HashSet<string> trackedTypeNames;

		public AdjacencyGraph<HeapObject, Edge<HeapObject>> Graph = new AdjacencyGraph<HeapObject, Edge<HeapObject>> (allowParallelEdges: false);

		public NativeHeapshot(HashSet<string> trackedTypeNames)
		{
			this.trackedTypeNames = new HashSet<string> (trackedTypeNames);
		}

		public void AddObject (TypeInfo typeInfo, HeapObjectEvent heapObjectEvent)
		{
			if (heapObjectEvent.ObjectSize == 0) {
				// This means it's just reporting references
				// TODO: Validate if we need to handle it.
				return;
			}

			string typeName = typeInfo.Name;
			long typeId = typeInfo.TypeId;
			long address = heapObjectEvent.ObjectPointer;

			if (trackedTypeNames.Remove (typeName)) {
				TrackedTypes.Add (typeName, typeId);
			}

			if (!Types.TryGetValue (typeId, out var heapTypeInfo)) {
				Types[typeId] = heapTypeInfo = new HeapshotTypeInfo (typeInfo);
			}

			var heapObject = GetOrCreateObject (address, heapTypeInfo);

			Graph.AddVertex (heapObject);
			foreach (var reference in heapObjectEvent.References) {
				var referencedObject = GetOrCreateObject (reference.ObjectPointer, null);
				Graph.AddEdge (new Edge<HeapObject> (heapObject, referencedObject));
			}
		}

		HeapObject GetOrCreateObject (long address, HeapshotTypeInfo heapshotTypeInfo = null)
		{
			if (!Objects.TryGetValue(address, out var heapObject)) {
				Objects[address] = heapObject = new HeapObject (address);
			}

			if (heapObject.TypeInfo == null && heapshotTypeInfo != null) {
				heapObject.TypeInfo = heapshotTypeInfo.TypeInfo;
				heapshotTypeInfo.Objects.Add (heapObject);
			}

			return heapObject;
		}

		public void RegisterRoot (long address, HeapRootRegisterEvent heapRootRegisterEvent)
		{
			Roots[address] = heapRootRegisterEvent;
		}
	}

	public class Heapshot
	{
		//public readonly Dictionary<long, HeapObject> Objects; - Possibly not needed.
		readonly Dictionary<string, long> TrackedTypes;
		public readonly Dictionary<long, HeapRootRegisterEvent> Roots;
		public readonly Dictionary<long, HeapshotTypeInfo> Types;

		public readonly ReversedBidirectionalGraph<HeapObject, Edge<HeapObject>> Graph;

		public Heapshot (NativeHeapshot nativeHeapshot)
		{
			//Objects = nativeHeapshot.Objects;
			Roots = nativeHeapshot.Roots;
			TrackedTypes = nativeHeapshot.TrackedTypes;
			Types = nativeHeapshot.Types;

			var graphWithInReferences = new BidirectionAdapterGraph<HeapObject, Edge<HeapObject>> (nativeHeapshot.Graph);
			// Construct the in-edge graph, so we can trace an object's retention path.
			Graph = new ReversedBidirectionalGraph<HeapObject, Edge<HeapObject>> (graphWithInReferences);
		}

		public bool TryGetHeapshotTypeInfo (string name, out HeapshotTypeInfo heapshotTypeInfo)
		{
			heapshotTypeInfo = null;

			return TrackedTypes.TryGetValue (name, out var typeId) && TryGetHeapshotTypeInfo (typeId, out heapshotTypeInfo);
		}

		public bool TryGetHeapshotTypeInfo (long typeId, out HeapshotTypeInfo heapshotTypeInfo)
			=> Types.TryGetValue (typeId, out heapshotTypeInfo);

		public int GetObjectCount (long typeId)
			=> Types.TryGetValue (typeId, out var heapshotTypeInfo) ? heapshotTypeInfo.Objects.Count : 0;
	}

	public class HeapObject : IEquatable<HeapObject>
	{
		public long Address { get; }
		public TypeInfo TypeInfo { get; set; }

		public HeapObject (long address)
		{
			Address = address;
		}

		public bool Equals (HeapObject other) => Address == other.Address;
		public override bool Equals (object obj) => obj is HeapObject other && Equals (other);
		public override int GetHashCode () => Address.GetHashCode ();
	}

	public class HeapshotTypeInfo
	{
		public TypeInfo TypeInfo { get; }
		public List<HeapObject> Objects { get; } = new List<HeapObject> ();

		public HeapshotTypeInfo (TypeInfo typeInfo)
		{
			TypeInfo = typeInfo;
		}
	}

	public class TypeInfo
	{
		public long TypeId { get; }
		public string Name { get; }

		public TypeInfo (long typeId, string name)
		{
			TypeId = typeId;
			Name = name;
		}
	}
}
