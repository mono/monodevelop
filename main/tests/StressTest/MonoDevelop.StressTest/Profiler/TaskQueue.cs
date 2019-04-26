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
			pair.Completion.Wait ();
		}

		public void Enqueue (Action act) => backgroundQueue.StartNew (act).Ignore ();
	}
}
