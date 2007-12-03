
using System;
using NUnit.Framework;
using Mono.Addins;
using System.IO;
using System.Threading;
using System.Globalization;
using SimpleApp;

namespace UnitTests
{
	[TestFixture()]
	public class TestLocalization: TestBase
	{
		public override void Setup ()
		{
			base.Setup ();
		}

		[Test]
		public void TestStringTable ()
		{
			GlobalInfoCondition.Value = "testTranslation";
			
			ExtensionContext ctx;
			ExtensionNode node;
			
			// Use a new extension context for every check, since strings are cached in
			// the nodes, and every extension has its own copy of the tree
			
			Thread.CurrentThread.CurrentCulture = new CultureInfo ("en-US");
			ctx = AddinManager.CreateExtensionContext ();
			node = ctx.GetExtensionNode ("/SimpleApp/ExtraWriters/SomeFileTransTest1");
			Assert.IsNotNull (node, "t1.1");
			Assert.AreEqual ("First sample file", node.ToString ());
			node = ctx.GetExtensionNode ("/SimpleApp/ExtraWriters/SomeFileTransTest2");
			Assert.IsNotNull (node, "t1.2");
			Assert.AreEqual ("Second sample file", node.ToString ());
			node = ctx.GetExtensionNode ("/SimpleApp/ExtraWriters/SomeFileTransTest3");
			Assert.IsNotNull (node, "t1.3");
			Assert.AreEqual ("Third sample file", node.ToString ());
			
			Thread.CurrentThread.CurrentCulture = new CultureInfo ("ca-ES");
			ctx = AddinManager.CreateExtensionContext ();
			node = ctx.GetExtensionNode ("/SimpleApp/ExtraWriters/SomeFileTransTest1");
			Assert.IsNotNull (node, "t2.1");
			Assert.AreEqual ("Primer arxiu d'exemple", node.ToString ());
			node = ctx.GetExtensionNode ("/SimpleApp/ExtraWriters/SomeFileTransTest2");
			Assert.IsNotNull (node, "t2.2");
			Assert.AreEqual ("Segon arxiu d'exemple", node.ToString ());
			node = ctx.GetExtensionNode ("/SimpleApp/ExtraWriters/SomeFileTransTest3");
			Assert.IsNotNull (node, "t2.3");
			Assert.AreEqual ("Tercer arxiu d'exemple", node.ToString ());
		}
		
		[Test]
		public void TestStringResource ()
		{
			ExtensionContext ctx;
			InstanceExtensionNode node;
			IWriter w;
			
			// Use a new extension context for every check, since strings are cached in
			// the nodes, and every extension has its own copy of the tree
			
			Thread.CurrentThread.CurrentCulture = new CultureInfo ("en-US");
			ctx = AddinManager.CreateExtensionContext ();
			node = ctx.GetExtensionNode ("/SimpleApp/Writers/SystemInfoExtension.SystemInfoWriter") as InstanceExtensionNode;
			Assert.IsNotNull (node, "t1");
			w = (IWriter) node.CreateInstance ();
			Assert.AreEqual ("System Info: File system information System information", w.Write ());
			Assert.AreEqual ("Modules", w.Title);
			
			Thread.CurrentThread.CurrentCulture = new CultureInfo ("ca-ES");
			ctx = AddinManager.CreateExtensionContext ();
			node = ctx.GetExtensionNode ("/SimpleApp/Writers/SystemInfoExtension.SystemInfoWriter") as InstanceExtensionNode;
			Assert.IsNotNull (node, "t2");
			w = (IWriter) node.CreateInstance ();
			Assert.AreEqual ("System Info: File system information Informació del sistema", w.Write ());
			Assert.AreEqual ("Mòduls", w.Title);
		}
		
/* The locale can't be changed at run-time.
 
		[Test]
		public void TestStringGetText ()
		{
			ExtensionContext ctx;
			ExtensionNode node;
			
			// Use a new extension context for every check, since strings are cached in
			// the nodes, and every extension has its own copy of the tree
			
			Thread.CurrentThread.CurrentCulture = new CultureInfo ("en-US");
			ctx = AddinManager.CreateExtensionContext ();
			node = ctx.GetExtensionNode ("/SimpleApp/Writers/SomeFile");
			Assert.IsNotNull (node, "t1.1");
			Assert.AreEqual ("Sample file", node.ToString ());
			
			Thread.CurrentThread.CurrentCulture = new CultureInfo ("ca-ES");
			ctx = AddinManager.CreateExtensionContext ();
			node = ctx.GetExtensionNode ("/SimpleApp/Writers/SomeFile");
			Assert.IsNotNull (node, "t2.1");
			Assert.AreEqual ("Arxiu d'exemple", node.ToString ());
		}
*/
	}
}
