//
// MSBuildSolution.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

using System;
using System.Collections.Generic;
using System.IO;

namespace MonoDevelop.Prj2Make
{
	class MSBuildSolution : Combine
	{
		Dictionary<string, DotNetProject> projectsByGuidTable;

		public MSBuildSolution () : base ()
		{
			FileFormat = new SlnFileFormat ();
		}

		static MSBuildSolution ()
		{
			IdeApp.ProjectOperations.AddingEntryToCombine += new AddEntryEventHandler (HandleAddEntry);
		}	

		public SlnData Data {
			get {
				if (!ExtendedProperties.Contains (typeof (SlnFileFormat)))
					return null;
				return (SlnData) ExtendedProperties [typeof (SlnFileFormat)];
			}
			set {
				ExtendedProperties [typeof (SlnFileFormat)] = value;
			}
		}

		public Dictionary<string, DotNetProject> ProjectsByGuid {
			get {
				if (projectsByGuidTable == null)
					projectsByGuidTable = new Dictionary<string, DotNetProject> ();
				return projectsByGuidTable;
			}
		}

		public static void HandleAddEntry (object s, AddEntryEventArgs args)
		{
			if (args.Combine.GetType () != typeof (MSBuildSolution))
				return;

			string extn = Path.GetExtension (args.FileName);

			//FIXME: Use IFileFormat.CanReadFile 
			if (String.Compare (extn, ".mdp", true) == 0 || String.Compare (extn, ".mds", true) == 0) {
				if (!IdeApp.Services.MessageService.AskQuestion (GettextCatalog.GetString (
					"The project file {0} must be converted to msbuild format to be added " + 
					"to a msbuild solution. Convert?", args.FileName), "Conversion required")) {
					args.Cancel = true;
					return;
				}
			}

			IProgressMonitor monitor = new NullProgressMonitor ();
			CombineEntry ce = Services.ProjectService.ReadCombineEntry (args.FileName, monitor);
			ce = SlnFileFormat.ConvertToMSBuild (ce, false);
			args.FileName = ce.FileName;

			if (String.Compare (extn, ".mds", true) == 0)
				SlnFileFormat.UpdateProjectReferences (((Combine) ce), true);

			ce.FileFormat.WriteFile (ce.FileName, ce, monitor);
		}

	}
}
