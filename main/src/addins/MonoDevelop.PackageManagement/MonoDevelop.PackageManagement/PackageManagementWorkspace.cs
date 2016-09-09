//
// PackageManagementWorkspace.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement
{
	internal class PackageManagementWorkspace
	{
		List<MonoDevelopSolutionManager> solutionManagers = new List<MonoDevelopSolutionManager> ();

		public PackageManagementWorkspace ()
		{
			IdeApp.Workspace.SolutionLoaded += SolutionLoaded;
			IdeApp.Workspace.SolutionUnloaded += SolutionUnloaded;
			IdeApp.Workspace.ItemAddedToSolution += SolutionItemAddedOrRemoved;
			IdeApp.Workspace.ItemRemovedFromSolution += SolutionItemAddedOrRemoved;
		}

		void SolutionLoaded (object sender, SolutionEventArgs e)
		{
			try {
				Runtime.RunInMainThread (() => {
					AddSolution (e.Solution);
				});
			} catch (Exception ex) {
				LoggingService.LogError ("Unable to create solution manager.", ex);
			}
		}

		void SolutionUnloaded (object sender, SolutionEventArgs e)
		{
			try {
				Runtime.RunInMainThread (() => {
					RemoveSolution (e.Solution);
				});
			} catch (Exception ex) {
				LoggingService.LogError ("Unable to unload solution manager.", ex);
			}
		}

		void AddSolution (Solution solution)
		{
			var solutionManager = new MonoDevelopSolutionManager (solution);
			solutionManagers.Add (solutionManager);
		}

		void RemoveSolution (Solution solution)
		{
			solutionManagers.RemoveAll (manager => manager.Solution == solution);
		}

		public IMonoDevelopSolutionManager GetSolutionManager (Solution solution)
		{
			Runtime.AssertMainThread ();

			return GetSolutionManager (new SolutionProxy (solution));
		}

		public IMonoDevelopSolutionManager GetSolutionManager (ISolution solution)
		{
			Runtime.AssertMainThread ();

			var solutionManager = solutionManagers.FirstOrDefault (manager => manager.Solution == solution.Solution);
			if (solutionManager != null) {
				return solutionManager;
			}

			return new MonoDevelopSolutionManager (solution);
		}

		public void ReloadSettings ()
		{
			Runtime.AssertMainThread ();

			foreach (IMonoDevelopSolutionManager solutionManager in solutionManagers) {
				solutionManager.ReloadSettings ();
			}
		}

		void SolutionItemAddedOrRemoved (object sender, SolutionItemChangeEventArgs e)
		{
			var solutionManager = (MonoDevelopSolutionManager) GetSolutionManager (e.Solution);
			solutionManager.ClearProjectCache ();
		}
	}
}

