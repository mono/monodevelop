
using System;
using System.IO;
using NUnit.Framework;
using Mono.Addins;
using SimpleApp;

namespace UnitTests
{
	public class TestBase
	{
		[TestFixtureSetUp]
		public virtual void Setup ()
		{
			AddinManager.AddinLoadError += OnLoadError;
			AddinManager.AddinLoaded += OnLoad;
			AddinManager.AddinUnloaded += OnUnload;
			
			string dir = new Uri (GetType().Assembly.CodeBase).LocalPath;
			AddinManager.Initialize (Path.GetDirectoryName (dir));
			AddinManager.Registry.ResetConfiguration ();
			AddinManager.Registry.Update (new ConsoleProgressStatus (true));
		}
		
		[TestFixtureTearDown]
		public virtual void Teardown ()
		{
			AddinManager.AddinLoadError -= OnLoadError;
			AddinManager.AddinLoaded -= OnLoad;
			AddinManager.AddinUnloaded -= OnUnload;
			AddinManager.Shutdown ();
		}
		
		void OnLoadError (object s, AddinErrorEventArgs args)
		{
			Console.WriteLine ("Add-in error: " + args.Message);
			Console.WriteLine (args.AddinId);
		}
		
		void OnLoad (object s, AddinEventArgs args)
		{
			Console.WriteLine ("Add-in loaded: " + args.AddinId);
		}
		
		void OnUnload (object s, AddinEventArgs args)
		{
			Console.WriteLine ("Add-in unloaded: " + args.AddinId);
		}
	}
}
