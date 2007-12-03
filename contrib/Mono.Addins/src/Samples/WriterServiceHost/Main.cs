using System;
using WriterService;
using Mono.Addins;

namespace SampleAddinHost
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			AddinManager.Initialize (".");
			AddinManager.Registry.Update (null);
			
			Console.WriteLine ("Normal writers:");
			WriterManager manager = new WriterManager (new string[0]);
			foreach (IWriter w in manager.GetWriters ())
				Console.WriteLine (w.Write ());
			
			Console.WriteLine ("Including debug writers:");
			WriterManager debugManager = new WriterManager (new string[] { "debug" });
			foreach (IWriter w in debugManager.GetWriters ())
				Console.WriteLine (w.Write ());
		}
	}
}