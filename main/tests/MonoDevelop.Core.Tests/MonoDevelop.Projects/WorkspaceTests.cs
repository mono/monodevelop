// WorkspaceTests.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;
using System.Linq;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	[TestFixture()]
	public class WorkspaceTests: TestBase
	{
		[Test()]
		public void AddRemoveEvents ()
		{
			int itemsAddedRoot = 0;
			int itemsRemovedRoot = 0;
			int descItemsAddedRoot = 0;
			int descItemsRemovedRoot = 0;
			
			Workspace ws = new Workspace ();
			ws.DescendantItemAdded += delegate { descItemsAddedRoot++; };
			ws.DescendantItemRemoved += delegate { descItemsRemovedRoot++; };
			ws.ItemAdded += delegate { itemsAddedRoot++; };
			ws.ItemRemoved += delegate { itemsRemovedRoot++; };

			// Direct events
			Workspace cws1 = new Workspace ();
			ws.Items.Add (cws1);
			Assert.AreEqual (1, descItemsAddedRoot);
			Assert.AreEqual (1, itemsAddedRoot);
			
			ws.Items.Remove (cws1);
			Assert.AreEqual (1, descItemsRemovedRoot);
			Assert.AreEqual (1, itemsRemovedRoot);
			
			ws.Items.Add (cws1);
			Assert.AreEqual (2, descItemsAddedRoot);
			Assert.AreEqual (2, itemsAddedRoot);
			
			// Indirect events
			
			Solution sol = new Solution ();
			cws1.Items.Add (sol);
			Assert.AreEqual (3, descItemsAddedRoot);
			Assert.AreEqual (2, itemsAddedRoot);
			
			cws1.Items.Remove (sol);
			Assert.AreEqual (2, descItemsRemovedRoot);
			Assert.AreEqual (1, itemsRemovedRoot);

			ws.Dispose ();
		}
		
		[Test()]
		public void Configurations ()
		{
			int configChangedEvs = 0;
			int configChangedEvsSub = 0;
			
			Workspace ws = new Workspace ();
			ws.ConfigurationsChanged += delegate { configChangedEvs++; };

			Workspace cws1 = new Workspace ();
			cws1.ConfigurationsChanged += delegate { configChangedEvsSub++; };
			ws.Items.Add (cws1);
			
			Solution sol = new Solution ();
			cws1.Items.Add (sol);
			
			ReadOnlyCollection<string> configs = ws.GetConfigurations ();
			Assert.AreEqual (0, configs.Count);
			configs = cws1.GetConfigurations ();
			Assert.AreEqual (0, configs.Count);
			
			// Add configurations
			
			configChangedEvs = configChangedEvsSub = 0;
			sol.AddConfiguration ("c1", false);
			Assert.AreEqual (1, configChangedEvs);
			Assert.AreEqual (1, configChangedEvsSub);
			
			configs = ws.GetConfigurations ();
			Assert.AreEqual (1, configs.Count);
			Assert.AreEqual ("c1", configs[0]);
			configs = cws1.GetConfigurations ();
			Assert.AreEqual (1, configs.Count);
			Assert.AreEqual ("c1", configs[0]);
			
			configChangedEvs = configChangedEvsSub = 0;
			sol.AddConfiguration ("c2", false);
			Assert.AreEqual (1, configChangedEvs);
			Assert.AreEqual (1, configChangedEvsSub);
			
			configs = ws.GetConfigurations ();
			Assert.AreEqual (2, configs.Count);
			Assert.AreEqual ("c1", configs[0]);
			Assert.AreEqual ("c2", configs[1]);
			configs = cws1.GetConfigurations ();
			Assert.AreEqual (2, configs.Count);
			Assert.AreEqual ("c1", configs[0]);
			Assert.AreEqual ("c2", configs[1]);

			// Add another solution
			
			Solution sol2 = new Solution ();
			sol2.AddConfiguration ("c3", false);
			sol2.AddConfiguration ("c4", false);
			
			configChangedEvs = configChangedEvsSub = 0;
			cws1.Items.Add (sol2);
			Assert.AreEqual (1, configChangedEvs);
			Assert.AreEqual (1, configChangedEvsSub);
			
			configs = ws.GetConfigurations ();
			Assert.AreEqual (4, configs.Count);
			Assert.AreEqual ("c1", configs[0]);
			Assert.AreEqual ("c2", configs[1]);
			Assert.AreEqual ("c3", configs[2]);
			Assert.AreEqual ("c4", configs[3]);
			configs = cws1.GetConfigurations ();
			Assert.AreEqual (4, configs.Count);
			Assert.AreEqual ("c1", configs[0]);
			Assert.AreEqual ("c2", configs[1]);
			Assert.AreEqual ("c3", configs[2]);
			Assert.AreEqual ("c4", configs[3]);
			
			// Remove solution
			
			configChangedEvs = configChangedEvsSub = 0;
			cws1.Items.Remove (sol2);
			Assert.AreEqual (1, configChangedEvs);
			Assert.AreEqual (1, configChangedEvsSub);
			
			configs = ws.GetConfigurations ();
			Assert.AreEqual (2, configs.Count);
			Assert.AreEqual ("c1", configs[0]);
			Assert.AreEqual ("c2", configs[1]);
			configs = cws1.GetConfigurations ();
			Assert.AreEqual (2, configs.Count);
			Assert.AreEqual ("c1", configs[0]);
			Assert.AreEqual ("c2", configs[1]);
			
			// Remove configuration
			
			configChangedEvs = configChangedEvsSub = 0;
			sol.Configurations.RemoveAt (1);
			Assert.AreEqual (1, configChangedEvs);
			Assert.AreEqual (1, configChangedEvsSub);

			configs = ws.GetConfigurations ();
			Assert.AreEqual (1, configs.Count);
			Assert.AreEqual ("c1", configs[0]);
			configs = cws1.GetConfigurations ();
			Assert.AreEqual (1, configs.Count);
			Assert.AreEqual ("c1", configs[0]);
			
			// Remove child workspace
			
			configChangedEvs = configChangedEvsSub = 0;
			ws.Items.Remove (cws1);
			Assert.AreEqual (1, configChangedEvs);
			
			configs = ws.GetConfigurations ();
			Assert.AreEqual (0, configs.Count);

			ws.Dispose ();
		}
		
		[Test()]
		public void ModelQueries ()
		{
			DotNetProject it2, it3, it4;
			DummySolutionItem it1;
			string someFile, someId;
			
			Workspace ws = new Workspace ();
			Workspace cws = new Workspace ();
			ws.Items.Add (cws);
			
			Solution sol1 = new Solution ();
			cws.Items.Add (sol1);
			sol1.RootFolder.Items.Add (it1 = new DummySolutionItem ());
			sol1.RootFolder.Items.Add (it2 = Services.ProjectService.CreateDotNetProject ("C#"));
			
			Solution sol2 = new Solution ();
			cws.Items.Add (sol2);
			SolutionFolder f = new SolutionFolder ();
			sol2.RootFolder.Items.Add (f);
			f.Items.Add (it3 = Services.ProjectService.CreateDotNetProject ("C#"));
			f.Items.Add (it4 = Services.ProjectService.CreateDotNetProject ("C#"));
			
			it3.Name = "it3";
			it4.FileName = "/test/it4";
			someFile = it4.FileName;
			someId = it3.ItemId;
			Assert.IsFalse (string.IsNullOrEmpty (someId));
			
			Assert.AreEqual (2, sol1.Items.Count);
			Assert.IsTrue (sol1.Items.Contains (it1));
			Assert.IsTrue (sol1.Items.Contains (it2));
			
			Assert.AreEqual (2, sol2.Items.Count);
			Assert.IsTrue (sol2.Items.Contains (it3));
			Assert.IsTrue (sol2.Items.Contains (it4));
			
			var its = ws.GetAllItems<SolutionFolderItem> ().ToList();
			Assert.AreEqual (7, its.Count);
			Assert.IsTrue (its.Contains (it1));
			Assert.IsTrue (its.Contains (it2));
			Assert.IsTrue (its.Contains (it3));
			Assert.IsTrue (its.Contains (it4));
			Assert.IsTrue (its.Contains (sol1.RootFolder));
			Assert.IsTrue (its.Contains (sol2.RootFolder));
			Assert.IsTrue (its.Contains (f));
			
			var its2 = ws.GetAllItems<DotNetProject> ().ToList();
			Assert.AreEqual (3, its2.Count);
			Assert.IsTrue (its2.Contains (it2));
			Assert.IsTrue (its2.Contains (it3));
			Assert.IsTrue (its2.Contains (it4));
			
			var its3 = ws.GetAllItems<Project> ().ToList();
			Assert.AreEqual (3, its3.Count);
			Assert.IsTrue (its3.Contains (it2));
			Assert.IsTrue (its3.Contains (it3));
			Assert.IsTrue (its3.Contains (it4));
			
			var its4 = ws.GetAllItems<Solution> ().ToList();
			Assert.AreEqual (2, its4.Count);
			Assert.IsTrue (its4.Contains (sol1));
			Assert.IsTrue (its4.Contains (sol2));
			
			var its5 = ws.GetAllItems<WorkspaceItem> ().ToList();
			Assert.AreEqual (4, its5.Count);
			Assert.IsTrue (its5.Contains (ws));
			Assert.IsTrue (its5.Contains (cws));
			Assert.IsTrue (its5.Contains (sol2));
			Assert.IsTrue (its5.Contains (sol2));
			
			var its6 = ws.GetAllItems<Workspace> ().ToList();
			Assert.AreEqual (2, its6.Count);
			Assert.IsTrue (its6.Contains (ws));
			Assert.IsTrue (its6.Contains (cws));
			
			SolutionFolderItem si = sol2.GetSolutionItem (someId);
			Assert.AreEqual (it3, si);
			
			SolutionItem fi = sol2.FindSolutionItem (someFile);
			Assert.AreEqual (it4, fi);
			
			fi = sol2.FindProjectByName ("it3");
			Assert.AreEqual (it3, fi);
			
			fi = sol2.FindProjectByName ("it4");
			Assert.AreEqual (it4, fi);
			
			fi = sol2.FindProjectByName ("it2");
			Assert.IsNull (fi);

			ws.Dispose ();
			cws.Dispose ();
		}
		
		[Test]
		public void Disposing ()
		{
			List<MyData> d = new List<MyData> ();
			for (int n=0; n<7; n++)
				d.Add (new MyData ());
			
			DotNetProject it2, it3, it4;
			DummySolutionItem it1;
			
			Workspace ws = new Workspace ();
			Workspace cws = new Workspace ();
			ws.Items.Add (cws);
			
			Solution sol1 = new Solution ();
			cws.Items.Add (sol1);
			sol1.RootFolder.Items.Add (it1 = new DummySolutionItem ());
			sol1.RootFolder.Items.Add (it2 = Services.ProjectService.CreateDotNetProject ("C#"));
			
			Solution sol2 = new Solution ();
			cws.Items.Add (sol2);
			SolutionFolder f = new SolutionFolder ();
			sol2.RootFolder.Items.Add (f);
			f.Items.Add (it3 = Services.ProjectService.CreateDotNetProject ("C#"));
			f.Items.Add (it4 = Services.ProjectService.CreateDotNetProject ("C#"));
			
			ws.ExtendedProperties ["data"] = d[0];
			cws.ExtendedProperties ["data"] = d[1];
			it1.ExtendedProperties ["data"] = d[2];
			it2.ExtendedProperties ["data"] = d[3];
			it3.ExtendedProperties ["data"] = d[4];
			it4.ExtendedProperties ["data"] = d[5];
			f.ExtendedProperties ["data"] = d[6];
			
			ws.Dispose ();
			
			for (int n=0; n<d.Count; n++)
				Assert.AreEqual (1, d[n].Disposed, "dispose check " + n);
		}

		[Test]
		public async Task Load ()
		{
			string wsFile = Util.GetSampleProject ("workspace", "workspace.mdw");
			var wsi = await Services.ProjectService.ReadWorkspaceItem (new ProgressMonitor (), wsFile);
			Assert.IsInstanceOf<Workspace> (wsi);
			var ws = (Workspace)wsi;

			Assert.AreEqual (1, ws.Items.Count);
			Assert.IsInstanceOf<Solution> (ws.Items[0]);
			var sol = (Solution) ws.Items [0];

			Assert.AreEqual (1, sol.Items.Count);
			Assert.IsInstanceOf<Project> (sol.Items[0]);

			ws.Dispose ();
		}
	}
	
	class MyData: IDisposable
	{
		public int Disposed;
		
		public void Dispose ()
		{
			Disposed++;
		}

	}
	
	class DummySolutionItem: SolutionItem
	{
		public DummySolutionItem ()
		{
			Initialize (this);
		}
	}
}
