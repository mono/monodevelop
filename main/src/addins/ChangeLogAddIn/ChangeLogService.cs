// ChangeLogService.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.VersionControl;

namespace MonoDevelop.ChangeLogAddIn
{
	public static class ChangeLogService
	{
		// Returns the path of the ChangeLog where changes of the provided file have to be logged.
		// Returns null if no ChangeLog could be found.
		// Returns an empty string if changes don't have to be logged.
		public static string GetChangeLogForFile (string baseCommitPath, string file, out SolutionItem parentEntry, out ChangeLogPolicy policy)
		{
			parentEntry = null;
			policy = null;
			if (!IdeApp.Workspace.IsOpen)
				return null;
			
			// Find the project that contains the file. If none is found
			// find a combine entry at the file location
			string bestPath = null;
			file = FileService.GetFullPath (file);
			
			foreach (SolutionItem e in IdeApp.Workspace.GetAllSolutionItems ()) {
				if (e is Project && ((Project)e).Files.GetFile (file) != null) {
					parentEntry = e;
					break;
				}
				string epath = FileService.GetFullPath (e.BaseDirectory) + Path.DirectorySeparatorChar;
				if (file.StartsWith (epath) && (bestPath == null || bestPath.Length < epath.Length)) {
					bestPath = epath;
					parentEntry = e;
				}
			}
			
			if (parentEntry == null)
				return null;
			
			policy = parentEntry.Policies.Get<ChangeLogPolicy> ();
			
			if (baseCommitPath == null)
				baseCommitPath = parentEntry.ParentSolution.BaseDirectory;
			
			baseCommitPath = FileService.GetFullPath (baseCommitPath);
			
			if (policy.VcsIntegration == VcsIntegration.None)
				return "";
			
			switch (policy.UpdateMode)
			{
				case ChangeLogUpdateMode.None:
					return string.Empty;
					
				case ChangeLogUpdateMode.Nearest: {
					string dir = FileService.GetFullPath (Path.GetDirectoryName (file));
					
					do {
						string cf = Path.Combine (dir, "ChangeLog");
						if (File.Exists (cf))
							return cf;
						dir = Path.GetDirectoryName (dir);
					} while (dir.Length >= baseCommitPath.Length);
						
					return null;
				}
					
				case ChangeLogUpdateMode.ProjectRoot:				
					return Path.Combine (parentEntry.BaseDirectory, "ChangeLog");
					
				case ChangeLogUpdateMode.Directory:
					string dir = Path.GetDirectoryName (file);
					return Path.Combine (dir, "ChangeLog");

				default:
					LoggingService.LogError ("Could not handle ChangeLogUpdateMode: " + policy.UpdateMode);
					return null;
			}                                                     	
		}	
		
		public static string GetChangeLogForFile (string baseCommitPath, string file)
		{
			SolutionItem parentEntry;
			ChangeLogPolicy policy;
			return GetChangeLogForFile (baseCommitPath, file, out parentEntry, out policy);
		}
		
		public static CommitMessageStyle GetMessageStyle (SolutionItem item)
		{
			ChangeLogPolicy policy = item.Policies.Get<ChangeLogPolicy> ();
			return policy.MessageStyle;
		}
	}
}
