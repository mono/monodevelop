using System;
using System.Collections.Generic;
using System.Linq;
using static MonoDevelop.StressTest.ProfilerProcessor;

namespace MonoDevelop.StressTest
{
	public static class HeapshotExtensions
	{
		public static IEnumerable<(string, int)> GetObjects(this Heapshot heapshot)
		{
			foreach (var typeWithCount in heapshot.ObjectsPerClassCounter.Where (p => p.Value > 0)) {
				var name = heapshot.ClassInfos[typeWithCount.Key].Name;
				yield return (name, typeWithCount.Value);
			}
		}
	}
}
