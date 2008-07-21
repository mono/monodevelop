//
// Translation.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using System;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Gettext
{
	public class Translation
	{
		TranslationProject parentProject;
		
		[ItemProperty]
		string isoCode;
		
		internal Translation ()
		{
		}
			
		internal Translation (TranslationProject prj, string isoCode)
		{
			this.parentProject = prj;
			this.isoCode = isoCode;
		}
		
		public string IsoCode {
			get { return isoCode; }
			set { isoCode = value; }
		}
		
		public string FileName {
			get {
				return isoCode + ".po";
			}
		}

		public TranslationProject ParentProject {
			get {
				return parentProject;
			}
			internal set {
				parentProject = value;
			}
		}
		
		public string PoFile {
			get {
				return Path.Combine (parentProject.BaseDirectory, isoCode + ".po");
			}
		}
		
		public string GetOutFile (string configuration)
		{
			string moDirectory = Path.Combine (Path.Combine (parentProject.GetOutputDirectory (configuration), isoCode), "LC_MESSAGES");
			return Path.Combine (moDirectory, parentProject.PackageName + ".mo");
		}
		
		public BuildResult Build (IProgressMonitor monitor, string configuration)
		{
			BuildResult results = new BuildResult ("", 0, 0);
			
			string moFileName  = GetOutFile (configuration);
			string moDirectory = Path.GetDirectoryName (moFileName);
			if (!Directory.Exists (moDirectory))
				Directory.CreateDirectory (moDirectory);
			
			ProcessWrapper process = Runtime.ProcessService.StartProcess ("msgfmt",
		                                     PoFile + " -o " + moFileName,
		                                     parentProject.BaseDirectory,
		                                     monitor.Log,
		                                     monitor.Log,
		                                     null);
			process.WaitForOutput ();

			if (process.ExitCode == 0) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Translation {0}: Compilation succeeded.", IsoCode));
			} else {
				string error   = process.StandardError.ReadToEnd ();
				string message = String.Format (GettextCatalog.GetString ("Translation {0}: Compilation failed. Reason: {1}"), IsoCode, error);
				monitor.Log.WriteLine (message);
				results.AddError (PoFile, 1, 1, "", message);
				results.FailedBuildCount = 1;
			}
			return results;
		}
		
		public bool NeedsBuilding (string configuration)
		{
			if (!File.Exists (PoFile))
				return false;
			string moFileName = GetOutFile (configuration);
			if (!File.Exists (moFileName))
				return true;
			return File.GetLastWriteTime (PoFile) > File.GetLastWriteTime (moFileName);
		}
	}
	
}
