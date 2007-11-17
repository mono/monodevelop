
using System;
using System.Collections;
using NUnit.Framework;
using Mono.Addins;
using SimpleApp;

namespace UnitTests
{
	[TestFixture()]
	public class TestConditions: TestBase
	{
		ParameterInfoCondition pinfo;
		ExtensionContext ctx;
		Hashtable added;
		Hashtable removed;
		Hashtable oldwriters;
		
		public override void Setup ()
		{
			base.Setup ();
			
			pinfo = new ParameterInfoCondition ();
			pinfo.Value = "res";
			GlobalInfoCondition.Value = "res";
			
			ctx = AddinManager.CreateExtensionContext ();
			ctx.RegisterCondition ("InputParameter", pinfo);
		}
		
		void StartListenerCheck (string tid)
		{
			// Get the current writers
			
			added = new Hashtable ();
			removed = new Hashtable ();
			
			ctx.AddExtensionNodeHandler ("/SimpleApp/ExtraWriters", OnExtensionAddRemove);
			Assert.AreEqual (0, removed.Count, tid + ": RegisterExtensionListener should not remove");
			
			oldwriters = added;
			added = new Hashtable ();
		}
		
		void EndListenerCheck (string tid)
		{
			IWriter[] writers = (IWriter[]) ctx.GetExtensionObjects ("/SimpleApp/ExtraWriters", typeof(IWriter));
			
			Hashtable newwriters = new Hashtable ();
			for (int n=0; n<writers.Length; n++) {
				string nwrit = writers[n].Write ();
				
				// Check added events
				if (oldwriters.Contains (nwrit)) {
					Assert.IsFalse (added.Contains (nwrit), tid + ": incorrect Add event for node: " + nwrit);
					Assert.IsFalse (removed.Contains (nwrit), tid + ": incorrect Remove event for node: " + nwrit);
				} else {
					Assert.IsTrue (added.Contains (nwrit), tid + ": Add event not sent for node: " + nwrit);
				}
				newwriters [nwrit] = nwrit;
			}
			
			// Check remove events
			foreach (string old in oldwriters.Keys) {
				if (!newwriters.Contains (old))
					Assert.IsTrue (removed.Contains (old), tid + ": Remove event not sent for node: " + old);
			}
			
			ctx.RemoveExtensionNodeHandler ("/SimpleApp/ExtraWriters", OnExtensionAddRemove);
		}
		
		void CheckWriters (string tid, string gval, string pval, params string[] result)
		{
			// Do the change
			
			StartListenerCheck (tid + " set InputParameter");
			pinfo.Value = pval;
			EndListenerCheck (tid + " set InputParameter");
			
			StartListenerCheck (tid + " set GlobalInfo");
			GlobalInfoCondition.Value = gval;
			EndListenerCheck (tid + " set GlobalInfo");
			
			// Get the new writers
			
			IWriter[] writers = (IWriter[]) ctx.GetExtensionObjects ("/SimpleApp/ExtraWriters", typeof(IWriter));
			Assert.AreEqual (result.Length, writers.Length, tid + ": result count");
			
			for (int n=0; n<result.Length; n++) {
				string nwrit = writers[n].Write ();
				Assert.AreEqual (result[n], nwrit, tid + ": result #" + n);
			}
		}
		
		void OnExtensionAddRemove (object s, ExtensionNodeEventArgs args)
		{
			IWriter w = (IWriter) ((TypeExtensionNode)args.ExtensionNode).CreateInstance ();
			if (args.Change == ExtensionChange.Add)
				added [w.Write ()] = w;
			else
				removed [w.Write ()] = w;
		}
		
		[Test()]
		public void TestAllFalse ()
		{
			// All conditions evaluate to false
			CheckWriters (
				"t1",
				"", "",
				"cmd:ca1",
				"cmd:ca2"
			);
		}
			
		[Test()]
		public void TestSimpleCondition ()
		{
			// Simple condition is true
			
			CheckWriters (
				"t1",
				"no", "",
				"cmd:ca1",
				"cmd:ca2",
				"cmd:cn1",
				"cmd:cn2",
				"cmd:cn3"
			);
		}
			
		[Test()]
		public void TestOr ()
		{
			string[] istrue = new string [] { "cmd:ca1", "cmd:ca2", "cmd:c1 x or y or yes", "cmd:c2 x or y or yes" };
			
			CheckWriters ("t1", "", "x", istrue);
			
			CheckWriters ("t2", "", "y", istrue);
			
			CheckWriters ("t3", "yes", "", istrue);
			
			CheckWriters ("t3", "yes", "x", istrue);
			
			CheckWriters ("t3", "yes", "y", istrue);
		}

		[Test()]
		public void TestAnd ()
		{
			string[] isfalse = new string [] { "cmd:ca1", "cmd:ca2" };
			string[] istrue = new string [] { "cmd:ca1", "cmd:ca2", "cmd:c3 x1 and yes1", "cmd:c4 x1 and yes1" };
			
			CheckWriters ("t1", "", "x1", isfalse);
			
			CheckWriters ("t2", "yes1", "", isfalse);
			
			CheckWriters ("t3", "yes1", "x1", istrue);
		}

		[Test()]
		public void NestedTestOrAnd ()
		{
			string[] isfalse = new string [] { "cmd:ca1", "cmd:ca2" };
			string[] istrue = new string [] { "cmd:ca1", "cmd:ca2", "cmd:cc5", "cmd:cc6" };
			
			// First or
			
			CheckWriters ("t1", "", "nx", isfalse);
			
			CheckWriters ("t2", "nx1", "nx", istrue);
			
			CheckWriters ("t3", "nx2", "nx", istrue);
			
			CheckWriters ("t4", "nx1", "", isfalse);
			
			CheckWriters ("t5", "nx2", "", isfalse);
			
			// Second or
			
			CheckWriters ("t6", "", "ny", isfalse);
			
			CheckWriters ("t7", "ny1", "ny", istrue);
			
			CheckWriters ("t8", "ny2", "ny", istrue);
			
			CheckWriters ("t9", "ny1", "", isfalse);
			
			CheckWriters ("t10", "ny2", "", isfalse);
		}
		
		[Test()]
		public void InnerCondition ()
		{
			CheckWriters ("t1", "", "ines1", "cmd:ca1", "cmd:ca2");
			
			CheckWriters ("t2", "cnes", "", "cmd:ca1", "cmd:ca2", "cmd:cnes1", "cmd:cnes2");
			
			CheckWriters ("t3", "cnes", "ines1", "cmd:ca1", "cmd:ca2", "cmd:cnes1", "cmd:cnes2", "cmd:ines1", "cmd:ines2");
		}
		
		[Test()]
		public void InnerOrCondition ()
		{
			CheckWriters ("t1", "", "inesOr", "cmd:ca1", "cmd:ca2");
			
			CheckWriters ("t2", "cnesOr", "", "cmd:ca1", "cmd:ca2", "cmd:cnesOr1", "cmd:cnesOr2");
			
			CheckWriters ("t3", "cnesOr", "inesOr", "cmd:ca1", "cmd:ca2", "cmd:cnesOr1", "cmd:cnesOr2", "cmd:inesOr1", "cmd:inesOr2");
		}
		
		[Test()]
		public void InnerAndCondition ()
		{
			CheckWriters ("t1", "", "inesAnd", "cmd:ca1", "cmd:ca2");
			
			CheckWriters ("t2", "cnesAnd", "", "cmd:ca1", "cmd:ca2", "cmd:cnesAnd1", "cmd:cnesAnd2");
			
			CheckWriters ("t3", "cnesAnd", "inesAnd", "cmd:ca1", "cmd:ca2", "cmd:cnesAnd1", "cmd:cnesAnd2", "cmd:inesAnd1", "cmd:inesAnd2");
		}
		
		[Test()]
		public void ChildNodeConditions ()
		{
			CheckWriters ("t1", "testChildren", "tc1", "cmd:ca1", "cmd:ca2", "file:someFile1[child1][child2]");
			
			pinfo.Value = "tc2";
			GlobalInfoCondition.Value = "testChildren";
			
			string[] result = new string [] {"cmd:ca1", "cmd:ca2", "file:someFile1[child1][child3]"};
			
			IWriter[] writers = (IWriter[]) ctx.GetExtensionObjects ("/SimpleApp/ExtraWriters", typeof(IWriter));
			Assert.AreEqual (result.Length, writers.Length, "t2: result count");
			for (int n=0; n<result.Length; n++) {
				string nwrit = writers[n].Write ();
				Assert.AreEqual (result[n], nwrit, "t2: result #" + n);
			}
		}
	}
}
