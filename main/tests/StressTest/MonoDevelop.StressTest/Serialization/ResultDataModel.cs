using System;
using System.Collections.Generic;

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
		/// Leak information
		/// </summary>
		public LeakIterationData LeakData { get; set; }

		// TODO: Make this serializable in MD.
		//public Components.AutoTest.AutoTestSession.MemoryStats MemoryStats { get; set; }

		// TODO: add more.
	}

	[Serializable]
	public class LeakIterationData
	{
		/// <summary>
		/// Leak iteration id
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Each individual leak item
		/// </summary>
		public List<LeakItem> Leaks { get; }

		public LeakIterationData(string id)
		{
			Id = id;
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

		public LeakItem (string className, int count)
		{
			ClassName = className;
			Count = count;
		}
	}
}
