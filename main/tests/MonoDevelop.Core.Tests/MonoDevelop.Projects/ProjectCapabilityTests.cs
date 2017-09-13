//
// ProjectCapabilityTests.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using System;
using System.Threading.Tasks;
using MonoDevelop.Projects.Extensions;
using NUnit.Framework;
using UnitTests;
using System.Linq;
using System.Collections.Generic;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class ProjectCapabilityTests : TestBase
	{
		CustomCapabilityNode capaNode;

		[TestFixtureSetUp]
		public void SetUp ()
		{
			capaNode = new CustomCapabilityNode ();
			WorkspaceObject.RegisterCustomExtension (capaNode);
		}
	
		[TestFixtureTearDown]
		public void Teardown ()
		{
			WorkspaceObject.UnregisterCustomExtension (capaNode);
		}

		List<string> defaultCapabilities;
		async Task<IEnumerable<string>> GetDefaultCapabilities ()
		{
			if (defaultCapabilities == null) {
				string f = Util.GetSampleProject ("console-project", "ConsoleProject", "ConsoleProject.csproj");
				var item = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), f);
				defaultCapabilities = item.GetProjectCapabilities ().ToList ();
			}
			return defaultCapabilities;
		}

		[Test ()]
		public async Task LoadCapability ()
		{
			string solFile = Util.GetSampleProject ("project-capability-tests", "ConsoleProject.csproj");
			var item = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), solFile);

			var defaultCaps = await GetDefaultCapabilities ();

			Assert.AreEqual (new [] { "Zero" }, item.GetProjectCapabilities ().Except (defaultCaps).ToArray ());

			item.Dispose ();
		}

		[Test ()]
		public async Task ActivateDeactivateCapability ()
		{
			string solFile = Util.GetSampleProject ("project-capability-tests", "ConsoleProject.csproj");
			var item = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), solFile);

			var defaultCaps = await GetDefaultCapabilities ();

			Assert.AreEqual (new [] { "Zero" }, item.GetProjectCapabilities ().Except (defaultCaps).ToArray ());
			var ext = item.GetFlavor<CustomCapabilityExtension> ();
			Assert.IsNull (ext);

			// Now activate "One" capability

			bool capabilitiesChanged = false;
			item.ProjectCapabilitiesChanged += (sender, e) => capabilitiesChanged = true;
		
			var import = item.MSBuildProject.AddNewImport ("extra1.targets");
			await item.ReevaluateProject (Util.GetMonitor ());

			Assert.IsTrue (capabilitiesChanged);
			Assert.AreEqual (new [] { "Zero", "One" }, item.GetProjectCapabilities ().Except (defaultCaps).ToArray ());

			ext = item.GetFlavor<CustomCapabilityExtension> ();
			Assert.IsNotNull (ext);

			capabilitiesChanged = false;
			item.MSBuildProject.RemoveImport (import);
			await item.ReevaluateProject (Util.GetMonitor ());

			Assert.IsTrue (capabilitiesChanged);
			Assert.AreEqual (new [] { "Zero" }, item.GetProjectCapabilities ().Except (defaultCaps).ToArray ());
			ext = item.GetFlavor<CustomCapabilityExtension> ();
			Assert.IsNull (ext);

			item.Dispose ();
		}

		[Test]
		public async Task AppliesTo_ActivateDeactivateCapability ()
		{
			string solFile = Util.GetSampleProject ("project-capability-tests", "ConsoleProject.csproj");
			var item = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), solFile);

			string azureFunctionsProjectExtension = "AzureFunctionsProjectExtension";
			Func<ProjectExtension, bool> isMatch = f => f.GetType ().Name == azureFunctionsProjectExtension;
			var ext = item.GetFlavors ().FirstOrDefault (isMatch);
			Assert.IsNull (ext);

			// Now activate "AzureFunctions" capability
			var import = item.MSBuildProject.AddNewImport ("azurefunctions.targets");
			await item.ReevaluateProject (Util.GetMonitor ());

			ext = item.GetFlavors ().FirstOrDefault (isMatch);
			Assert.IsNotNull (ext);

			item.MSBuildProject.RemoveImport (import);
			await item.ReevaluateProject (Util.GetMonitor ());

			ext = item.GetFlavors ().FirstOrDefault (isMatch);
			Assert.IsNull (ext);

			item.Dispose ();
		}
	}

	class CustomCapabilityNode : SolutionItemExtensionNode
	{
		public CustomCapabilityNode ()
		{
			ProjectCapability = "One | Two";
		}

		public override object CreateInstance ()
		{
			return new CustomCapabilityExtension ();
		}
	}

	class CustomCapabilityExtension : ProjectExtension
	{
	}
}
