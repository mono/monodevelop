//
// ResolveConflictsCommands.cs
//
// Author:
//       Therzok <teromario@yahoo.com>
//
// Copyright (c) 2013 Therzok
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
using System.Linq;

using MonoDevelop.Ide;
using MonoDevelop.VersionControl.Views;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using System.Threading.Tasks;

namespace MonoDevelop.VersionControl
{
	public class ResolveConflictsCommand
	{
		public static async Task<bool> ResolveConflicts (VersionControlItemList list, bool test)
		{
			if (test)
				return list.All (s => (s.VersionInfo.Status & VersionStatus.Conflicted) == VersionStatus.Conflicted);

			foreach (var item in list.Where (s => (s.VersionInfo.Status & VersionStatus.Conflicted) == VersionStatus.Conflicted)) {
				Document doc = await IdeApp.Workbench.OpenDocument (item.Path, item.ContainerProject, true);
				foreach (var view in doc.Views) {
					if (view.GetContent <MergeView> () != null)
						view.Select ();
				}
			}
			return true;
		}
	}
}

