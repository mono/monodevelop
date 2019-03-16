using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace MonoDevelop.StressTest
{
	/// <summary>
	/// Model which is used to report result data in CI
	/// </summary>
	[Serializable]
	public class ResultDataModel
	{
		/// <summary>
		/// Gets the leak information for each iteration
		/// </summary>
		/// <value>The iterations.</value>
		public List<ResultIterationData> Iterations { get; } = new List<ResultIterationData> ();
	}

	/// <summary>
	/// Model for a <see cref="ITestScenario"/> full run/>
	/// </summary>
	[Serializable]
	public class ResultIterationData
	{
		/// <summary>
		/// Leak iteration id
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Leak information
		/// </summary>
		/// <summary>
		/// Each individual leak item
		/// </summary>
		public Dictionary<string, LeakItem> Leaks { get; }

		public Components.AutoTest.AutoTestSession.MemoryStats MemoryStats { get; }

		// TODO: add more.

		public ResultIterationData (string id, Dictionary<string, LeakItem> leaks, Components.AutoTest.AutoTestSession.MemoryStats memoryStats)
		{
			Id = id;
			Leaks = leaks;
			MemoryStats = memoryStats;
		}
	}

	[Serializable]
	public class LeakItem
	{
		/// <summary>
		/// Gets the name of the leaked class.
		/// </summary>
		/// <value>The name of the class.</value>
		public string ClassName { get; }

		/// <summary>
		/// Gets the leaked object count.
		/// </summary>
		/// <value>The count.</value>
		public int Count { get; }

		/// <summary>
		/// Gets the name of the dot file generated for the graph.
		/// </summary>
		/// <value>The name of the graph file.</value>
		public string GraphFileName { get; }

		public LeakItem (string className, int count, string graphFileName)
		{
			ClassName = className;
			Count = count;
			GraphFileName = graphFileName;
		}
	}
}
