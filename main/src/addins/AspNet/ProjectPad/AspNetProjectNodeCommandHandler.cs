//
// AspNetProjectNodeCommandHandler.cs
//
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using System.Collections.Generic;

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using MonoDevelop.AspNet.Commands;
using MonoDevelop.AspNet.Projects;

namespace MonoDevelop.AspNet.ProjectPad
{
	class AspNetProjectNodeCommandHandler: NodeCommandHandler
	{
		
		[CommandHandler (AspNetCommands.AddAspNetDirectory)]
		public void OnAddSpecialDirectory (object ob)
		{
			var proj = CurrentNode.DataItem as DotNetProject;
			if (proj == null)
				return;
			proj.AddDirectory ((string) ob);
			IdeApp.ProjectOperations.SaveAsync (proj);
		}
		
		[CommandUpdateHandler (AspNetCommands.AddAspNetDirectory)]
		public void OnAddSpecialDirectoryUpdate (CommandArrayInfo info)
		{
			var proj = CurrentNode.DataItem as DotNetProject;
			if (proj == null)
				return;

			var asp = proj.GetFlavor<AspNetAppProjectFlavor> ();
			if (asp == null)
				return;

			List<string> dirs = new List<string> (asp.GetSpecialDirectories ());
			dirs.Sort ();
			List<FilePath> fullPaths = new List<FilePath> (dirs.Count);
			foreach (string s in dirs)
				fullPaths.Add (proj.BaseDirectory.Combine (s));
			RemoveDirsNotInProject (fullPaths, proj);

			if (fullPaths.Count == 0)
				return;

			foreach (string dir in dirs) {
				if (!fullPaths.Contains (proj.BaseDirectory.Combine (dir)))
					continue;
				info.Add (dir.Replace("_", "__"), dir);
			}
		}

		static void RemoveDirsNotInProject (List<FilePath> dirs, Project proj)
		{
			//don't query project for existence of each dir, because proj.Files is much bigger than dirs
			//instead we switch the loops
			foreach (ProjectFile pf in proj.Files) {
				for (int i = 0; i < dirs.Count; i++) {
					// Equality comparison in case we don't have files in dirs[i].
					if (pf.FilePath.IsChildPathOf (dirs[i]) || pf.FilePath == dirs[i]) {
						dirs.RemoveAt (i);
						if (dirs.Count == 0)
							return;
						break;
					}
				}
			}
		}
	}
	
}
