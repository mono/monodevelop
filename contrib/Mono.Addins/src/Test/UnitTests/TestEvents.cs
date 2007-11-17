
using System;
using NUnit.Framework;
using Mono.Addins;
using SimpleApp;

namespace UnitTests
{
	[TestFixture()]
	public class TestEvents: TestBase
	{
		int notifyCount;
		int addCount;
		int removeCount;
		int eventCount;

		Counter[] counters = new Counter [3];
		
		class Counter
		{
			public int notifyCount;
			public int addCount;
			public int removeCount;
			public int eventCount;
			
			public void Reset ()
			{
				notifyCount = 0;
				addCount = 0;
				removeCount = 0;
				eventCount = 0;
			}
			
			public void Check (string test, int notifyCount, int addCount, int removeCount, int eventCount)
			{
				Assert.AreEqual (notifyCount, this.notifyCount, test + " (notifyCount)");
				Assert.AreEqual (addCount, this.addCount, test + " (addCount)");
				Assert.AreEqual (removeCount, this.removeCount, test + " (removeCount)");
				Assert.AreEqual (eventCount, this.eventCount, test + " (eventCount)");
				Reset ();
			}
			
			public void Update (ExtensionNodeEventArgs args)
			{
				notifyCount++;
				if (args.Change == ExtensionChange.Add)
					addCount++;
				else
					removeCount++;
			}
			
			public void Update (ExtensionEventArgs args)
			{
				eventCount++;
			}
		}

		string errorTag;
		
		[Test()]
		public void TestLoadEvents()
		{
			errorTag = "";
			notifyCount = addCount = removeCount = eventCount = 0;
			
			Assert.AreEqual (4, AddinManager.GetExtensionNodes ("/SimpleApp/Writers").Count, "count 1");
			
			AddinManager.Registry.DisableAddin ("SimpleApp.HelloWorldExtension,0.1.0");
			AddinManager.Registry.DisableAddin ("SimpleApp.FileContentExtension,0.1.0");

			Assert.AreEqual (2, AddinManager.GetExtensionNodes ("/SimpleApp/Writers").Count, "count 2");
			
			AddinManager.ExtensionChanged += OnExtensionChangedHandler;
			AddinManager.AddExtensionNodeHandler ("/SimpleApp/Writers", OnExtensionChange);
			
			Assert.IsTrue (errorTag == "", errorTag);
			Assert.AreEqual (2, notifyCount, "notifyCount 1");
			Assert.AreEqual (2, addCount, "addCount 1");
			Assert.AreEqual (0, removeCount, "removeCount 1");
			Assert.AreEqual (0, eventCount, "eventCount 1");
			
			notifyCount = addCount = removeCount = eventCount = 0;
			AddinManager.Registry.EnableAddin ("SimpleApp.HelloWorldExtension,0.1.0");
			
			Assert.AreEqual (3, AddinManager.GetExtensionNodes ("/SimpleApp/Writers").Count, "count 3");
			
			Assert.IsTrue (errorTag == "", errorTag);
			Assert.AreEqual (1, notifyCount, "notifyCount 2");
			Assert.AreEqual (1, addCount, "addCount 1");
			Assert.AreEqual (0, removeCount, "removeCount 1");
			Assert.AreEqual (2, eventCount, "eventCount 2");
			
			// Now unregister
			
			notifyCount = addCount = removeCount = eventCount = 0;
			AddinManager.ExtensionChanged -= OnExtensionChangedHandler;
			AddinManager.RemoveExtensionNodeHandler ("/SimpleApp/Writers", OnExtensionChange);
			
			AddinManager.Registry.EnableAddin ("SimpleApp.FileContentExtension,0.1.0");
			
			Assert.IsTrue (errorTag == "", errorTag);
			Assert.AreEqual (0, notifyCount, "notifyCount 3");
			Assert.AreEqual (0, addCount, "addCount 3");
			Assert.AreEqual (0, removeCount, "removeCount 3");
			Assert.AreEqual (0, eventCount, "eventCount 3");
			
			Assert.AreEqual (4, AddinManager.GetExtensionNodes ("/SimpleApp/Writers").Count, "count 4");
		}
		
		void OnExtensionChange (object s, ExtensionNodeEventArgs args)
		{
			notifyCount++;
			
			TypeExtensionNode nod = args.ExtensionNode as TypeExtensionNode;
			if (nod == null)
				errorTag += "t1 ";

			if (args.Change == ExtensionChange.Add) {
				addCount++;
					
				IWriter w = ((TypeExtensionNode)args.ExtensionNode).CreateInstance () as IWriter;
				if (w == null)
					errorTag += "t2 ";
			}
			if (args.Change == ExtensionChange.Remove) {
				removeCount++;
			}
		}
		
		void OnExtensionChangedHandler (object s, ExtensionEventArgs args)
		{
			eventCount++;
			if (args.Path != "/SimpleApp/Writers" && args.Path != "/SimpleApp.Core/TypeExtensions/SimpleApp.ISampleExtender")
				errorTag += "t4 (" + args.Path + ")";
		}
		
		
		[Test()]
		public void TestUnloadEvents()
		{
			errorTag = "";
			notifyCount = addCount = removeCount = eventCount = 0;
			
			// All addins are enabled
			
			Assert.AreEqual (4, AddinManager.GetExtensionNodes ("/SimpleApp/Writers").Count, "count 1");
			
			AddinManager.ExtensionChanged += OnExtensionChangedHandler;
			AddinManager.AddExtensionNodeHandler ("/SimpleApp/Writers", OnExtensionChange);
			
			Assert.IsTrue (errorTag == "", errorTag);
			Assert.AreEqual (4, notifyCount, "notifyCount 1");
			Assert.AreEqual (4, addCount, "addCount 1");
			Assert.AreEqual (0, removeCount, "removeCount 1");
			Assert.AreEqual (0, eventCount, "eventCount 1");
			
			notifyCount = addCount = removeCount = eventCount = 0;
			AddinManager.Registry.DisableAddin ("SimpleApp.HelloWorldExtension,0.1.0");
			
			Assert.AreEqual (3, AddinManager.GetExtensionNodes ("/SimpleApp/Writers").Count, "count 2");
			
			Assert.IsTrue (errorTag == "", errorTag);
			Assert.AreEqual (1, notifyCount, "notifyCount 2");
			Assert.AreEqual (0, addCount, "addCount 2");
			Assert.AreEqual (1, removeCount, "removeCount 2");
			Assert.AreEqual (1, eventCount, "eventCount 2");
			
			notifyCount = addCount = removeCount = eventCount = 0;
			AddinManager.Registry.DisableAddin ("SimpleApp.FileContentExtension,0.1.0");
			
			Assert.AreEqual (2, AddinManager.GetExtensionNodes ("/SimpleApp/Writers").Count, "count 3");
			
			Assert.IsTrue (errorTag == "", errorTag);
			Assert.AreEqual (1, notifyCount, "notifyCount 3");
			Assert.AreEqual (0, addCount, "addCount 3");
			Assert.AreEqual (1, removeCount, "removeCount 3");
			Assert.AreEqual (1, eventCount, "eventCount 3");
			
			// Now unregister
			
			AddinManager.ExtensionChanged -= OnExtensionChangedHandler;
			AddinManager.RemoveExtensionNodeHandler ("/SimpleApp/Writers", OnExtensionChange);
			
			notifyCount = addCount = removeCount = eventCount = 0;
			AddinManager.Registry.DisableAddin ("SimpleApp.CommandExtension,0.1.0");
			
			Assert.AreEqual (0, AddinManager.GetExtensionNodes ("/SimpleApp/Writers").Count, "count 4");
			
			Assert.IsTrue (errorTag == "", errorTag);
			Assert.AreEqual (0, notifyCount, "notifyCount 4");
			Assert.AreEqual (0, addCount, "addCount 4");
			Assert.AreEqual (0, removeCount, "removeCount 4");
			Assert.AreEqual (0, eventCount, "eventCount 4");
		}
		
		[Test]
		public void TestExtensionContextEvents ()
		{
			AddinManager.Registry.EnableAddin ("SimpleApp.SystemInfoExtension,0.1.0");
			
			counters [0] = new Counter ();
			counters [1] = new Counter ();
			counters [2] = new Counter ();
			
			GlobalInfoCondition.Value = "";
			
			ExtensionContext c1 = AddinManager.CreateExtensionContext ();
			ExtensionContext c2 = AddinManager.CreateExtensionContext ();
			
			ParameterInfoCondition pinfo1 = new ParameterInfoCondition ();
			ParameterInfoCondition pinfo2 = new ParameterInfoCondition ();
			
			pinfo1.Value = "";
			pinfo2.Value = "";
			
			c1.RegisterCondition ("InputParameter", pinfo1);
			c2.RegisterCondition ("InputParameter", pinfo2);
			
			// Test registering
			 
			c1.GetExtensionNode ("/SimpleApp/ExtraWriters").ExtensionNodeChanged += NodeListener_1;
			c2.AddExtensionNodeHandler ("/SimpleApp/ExtraWriters", NodeListener_2);
			AddinManager.AddExtensionNodeHandler ("/SimpleApp/Writers2", NodeListener_g);
			
			counters[0].Check ("t1.0", 2, 2, 0, 0);
			counters[1].Check ("t1.1", 2, 2, 0, 0);
			counters[2].Check ("t1.2", 2, 2, 0, 0);
			
			c1.AddExtensionNodeHandler ("/SimpleApp/Writers2", NodeListener_1);
			c2.AddExtensionNodeHandler ("/SimpleApp/Writers2", NodeListener_2);

			counters[1].Check ("t2.1", 2, 2, 0, 0);
			counters[2].Check ("t2.2", 2, 2, 0, 0);

			c1.ExtensionChanged += ExtensionListener_1;
			c2.ExtensionChanged += ExtensionListener_2;
			AddinManager.ExtensionChanged += ExtensionListener_g;
			
			counters[0].Check ("t3.0", 0, 0, 0, 0);
			counters[1].Check ("t3.1", 0, 0, 0, 0);
			counters[2].Check ("t3.2", 0, 0, 0, 0);
			
			CheckWriters ("t4.1", null, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t4.2", c1, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t4.3", c2, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t4.4", c1, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2");
			CheckWriters ("t4.5", c2, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2");
			
			// Test change global var
			
			GlobalInfoCondition.Value = "yes2";
			
			counters[0].Check ("t5.0", 2, 2, 0, 1);
			counters[1].Check ("t5.1", 2, 2, 0, 1);
			counters[2].Check ("t5.2", 2, 2, 0, 1);
			
			CheckWriters ("t6.1", null, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2", "cmd:cw1", "cmd:cw2");
			CheckWriters ("t6.2", c1, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2", "cmd:cw1", "cmd:cw2");
			CheckWriters ("t6.3", c2, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2", "cmd:cw1", "cmd:cw2");
			CheckWriters ("t6.4", c1, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2");
			CheckWriters ("t6.5", c2, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2");
			
			// Test change global var
			
			GlobalInfoCondition.Value = "no";
			
			counters[0].Check ("t7.0", 2, 0, 2, 1);
			counters[1].Check ("t7.1", 5, 3, 2, 2);
			counters[2].Check ("t7.2", 5, 3, 2, 2);
			
			CheckWriters ("t7.1", null, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t7.2", c1, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t7.3", c2, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t7.4", c1, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2", "cmd:cn1", "cmd:cn2", "cmd:cn3");
			CheckWriters ("t7.5", c2, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2", "cmd:cn1", "cmd:cn2", "cmd:cn3");
			
			// Test reset global var
			
			GlobalInfoCondition.Value = "";
			
			counters[0].Check ("t8.0", 0, 0, 0, 0);
			counters[1].Check ("t8.1", 3, 0, 3, 1);
			counters[2].Check ("t8.2", 3, 0, 3, 1);
			
			CheckWriters ("t8.1", null, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t8.2", c1, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t8.3", c2, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t8.4", c1, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2");
			CheckWriters ("t8.5", c2, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2");
			
			// Test local var
			
			pinfo1.Value = "simple";
			
			counters[0].Check ("t9.0", 0, 0, 0, 0);
			counters[1].Check ("t9.1", 2, 2, 0, 1);
			counters[2].Check ("t9.2", 0, 0, 0, 0);
			
			CheckWriters ("t9.1", null, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t9.2", c1, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t9.3", c2, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t9.4", c1, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2", "cmd:sim1", "cmd:sim2");
			CheckWriters ("t9.5", c2, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2");
			
			// Test local var
			
			pinfo2.Value = "simple";
			
			counters[0].Check ("t10.0", 0, 0, 0, 0);
			counters[1].Check ("t10.1", 0, 0, 0, 0);
			counters[2].Check ("t10.2", 2, 2, 0, 1);
			
			CheckWriters ("t10.1", null, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t10.2", c1, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t10.3", c2, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t10.4", c1, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2", "cmd:sim1", "cmd:sim2");
			CheckWriters ("t10.5", c2, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2", "cmd:sim1", "cmd:sim2");
			
			// Test unset local var
			
			pinfo2.Value = "";
			
			counters[0].Check ("t10.0", 0, 0, 0, 0);
			counters[1].Check ("t10.1", 0, 0, 0, 0);
			counters[2].Check ("t10.2", 2, 0, 2, 1);
			
			CheckWriters ("t10.1", null, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t10.2", c1, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t10.3", c2, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t10.4", c1, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2", "cmd:sim1", "cmd:sim2");
			CheckWriters ("t10.5", c2, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2");
			
			// Combined global/local var change
			
			GlobalInfoCondition.Value = "yes1";
			pinfo2.Value = "x1";
			
			counters[0].Check ("t11.0", 0, 0, 0, 0);
			counters[1].Check ("t11.1", 0, 0, 0, 0);
			counters[2].Check ("t11.2", 2, 2, 0, 1);
			
			CheckWriters ("t11.1", null, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t11.2", c1, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t11.3", c2, "/SimpleApp/Writers2", "cmd:w1", "cmd:w2");
			CheckWriters ("t11.4", c1, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2", "cmd:sim1", "cmd:sim2");
			CheckWriters ("t11.5", c2, "/SimpleApp/ExtraWriters", "cmd:ca1", "cmd:ca2", "cmd:c3 x1 and yes1", "cmd:c4 x1 and yes1");

			GlobalInfoCondition.Value = "";
		}
		
		void CheckWriters (string test, ExtensionContext ctx, string path, params string[] values)
		{
			IWriter[] nodes;
			if (ctx != null)
				nodes = (IWriter[]) ctx.GetExtensionObjects (path, typeof(IWriter));
			else
				nodes = (IWriter[]) AddinManager.GetExtensionObjects (path, typeof(IWriter));
			
			Assert.AreEqual (nodes.Length, values.Length, test + " (count)");
			for (int n=0; n<values.Length; n++) {
				Assert.AreEqual (values[n], nodes[n].Write(), test + " (result #" + n + ")");
			}
		}
		
		void NodeListener_1 (object s, ExtensionNodeEventArgs args)
		{
			counters[1].Update (args);
		}
		
		void NodeListener_2 (object s, ExtensionNodeEventArgs args)
		{
			counters[2].Update (args);
		}
		
		void NodeListener_g (object s, ExtensionNodeEventArgs args)
		{
			counters[0].Update (args);
		}
		
		void ExtensionListener_1 (object s, ExtensionEventArgs args)
		{
			counters[1].Update (args);
		}
		
		void ExtensionListener_2 (object s, ExtensionEventArgs args)
		{
			counters[2].Update (args);
		}
		
		void ExtensionListener_g (object s, ExtensionEventArgs args)
		{
			counters[0].Update (args);
		}
	}
}
