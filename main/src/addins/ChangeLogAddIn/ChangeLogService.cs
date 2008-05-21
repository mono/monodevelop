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

namespace MonoDevelop.ChangeLogAddIn
{
	public static class ChangeLogService
	{
		public static ChangeLogData GetChangeLogData (SolutionItem entry)
		{
			ChangeLogData changeLogData = entry.ExtendedProperties ["MonoDevelop.ChangeLogAddIn.ChangeLogInfo"] as ChangeLogData;			
			if (changeLogData == null) {
				changeLogData = entry.ExtendedProperties ["Temp.MonoDevelop.ChangeLogAddIn.ChangeLogInfo"] as ChangeLogData;			
				if (changeLogData == null) {
					changeLogData = new ChangeLogData (entry);
					entry.ExtendedProperties ["Temp.MonoDevelop.ChangeLogAddIn.ChangeLogInfo"] = changeLogData;
				}
			}
			changeLogData.Bind (entry);
			return changeLogData;
		}
		
		// Returns the path of the ChangeLog where changes of the provided file have to be logged.
		// Returns null if no ChangeLog could be found.
		// Returns an empty string if changes don't have to be logged.
		public static string GetChangeLogForFile (string baseCommitPath, string file)
		{
			if (!IdeApp.Workspace.IsOpen)
				return null;
			
			// Find the project that contains the file. If none is found
			// find a combine entry at the file location
			SolutionItem entry = null;
			string bestPath = null;
			file = FileService.GetFullPath (file);
			
			foreach (SolutionItem e in IdeApp.Workspace.GetAllSolutionItems ()) {
				if (e is Project && ((Project)e).Files.GetFile (file) != null) {
					entry = e;
					break;
				}
				string epath = FileService.GetFullPath (e.BaseDirectory) + Path.DirectorySeparatorChar;
				if (file.StartsWith (epath) && (bestPath == null || bestPath.Length < epath.Length)) {
					bestPath = epath;
					entry = e;
				}
			}
			
			if (entry == null)
				return null;
			
			if (baseCommitPath == null)
				baseCommitPath = entry.ParentSolution.BaseDirectory;
			
			baseCommitPath = FileService.GetFullPath (baseCommitPath);
			
			ChangeLogData changeLogData = GetChangeLogData (entry);
			
			SolutionItem parent = entry;
			while (changeLogData.Policy == ChangeLogPolicy.UseParentPolicy) {
				parent = parent.ParentFolder;
				changeLogData = GetChangeLogData (parent);
			}
			
			switch (changeLogData.Policy)
			{
				case ChangeLogPolicy.NoChangeLog:
					return string.Empty;
					
				case ChangeLogPolicy.UpdateNearestChangeLog: {
					string dir = FileService.GetFullPath (Path.GetDirectoryName (file));
					
					do {
						string cf = Path.Combine (dir, "ChangeLog");
						if (File.Exists (cf))
							return cf;
						dir = Path.GetDirectoryName (dir);
					} while (dir.Length >= baseCommitPath.Length);
						
					return null;
				}
					
				case ChangeLogPolicy.OneChangeLogInProjectRootDirectory:				
					return Path.Combine (entry.BaseDirectory, "ChangeLog");
					
				case ChangeLogPolicy.OneChangeLogInEachDirectory:
					string dir = Path.GetDirectoryName (file);
					return Path.Combine (dir, "ChangeLog");

				default:
					LoggingService.LogError ("Could not handle ChangeLogPolicy: " + changeLogData.Policy);
					return null;
			}                                                     	
		}	
	}
}
