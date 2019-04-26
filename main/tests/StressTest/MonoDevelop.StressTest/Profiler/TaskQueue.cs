using System;
using System.Threading.Tasks;

namespace MonoDevelop.StressTest
{
	public class TaskQueue
	{
		readonly ConcurrentExclusiveSchedulerPair pair = new ConcurrentExclusiveSchedulerPair ();
		readonly TaskFactory backgroundQueue;

		public TaskQueue () => backgroundQueue = new TaskFactory (pair.ExclusiveScheduler);

		public void Complete ()
		{
			pair.Complete ();

			var task = pair.Completion;
			if (!task.IsCompleted)
				Console.WriteLine ("Still processing task queue...");

			task.Wait ();
		}

		public void Enqueue (Func<Task> creator) => backgroundQueue.StartNew (creator);
	}
}
