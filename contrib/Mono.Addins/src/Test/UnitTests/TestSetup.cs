
using System;
using System.IO;
using NUnit.Framework;
using Mono.Addins;
using SimpleApp;

namespace UnitTests
{
	[TestFixture]
	public class TestSetup
	{
		[Test]
		public void TestInitialize ()
		{
			string dir = new Uri (GetType().Assembly.CodeBase).LocalPath;
			AddinManager.Initialize (Path.GetDirectoryName (dir));
			AddinManager.Registry.ResetConfiguration ();
			AddinManager.Registry.Update (new ConsoleProgressStatus (true));
		}
		
		[Test]
		public void TestShutdown ()
		{
			AddinManager.Shutdown ();
		}
	}
}
