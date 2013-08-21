// 
// GitSupportFeature.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Templates;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.VersionControl.Git
{
	public class GitSupportFeature: ISolutionItemFeature
	{
		public FeatureSupportLevel GetSupportLevel (SolutionFolder parentFolder, SolutionItem entry)
		{
			if (parentFolder != null && !parentFolder.ParentSolution.FileName.IsNullOrEmpty && System.IO.File.Exists (parentFolder.ParentSolution.FileName))
				return FeatureSupportLevel.NotSupported;
			return FeatureSupportLevel.SupportedByDefault;
		}

		public Gtk.Widget CreateFeatureEditor (SolutionFolder parentCombine, SolutionItem entry)
		{
			Gtk.Label label = new Gtk.Label (GettextCatalog.GetString ("A new local Git Repository for the solution will be created"));
			label.Show ();
			return label;
		}

		public string Validate (SolutionFolder parentCombine, SolutionItem entry, Gtk.Widget editor)
		{
			return null;
		}

		public void ApplyFeature (SolutionFolder parentFolder, SolutionItem entry, Gtk.Widget editor)
		{
			// The solution may not be saved yet
			if (parentFolder.ParentSolution.FileName.IsNullOrEmpty || !System.IO.File.Exists (parentFolder.ParentSolution.FileName))
				parentFolder.ParentSolution.Saved += OnSolutionSaved;
			else
				OnSolutionSaved (parentFolder.ParentSolution, null);
		}
		
		static void OnSolutionSaved (object o, EventArgs a)
		{
			Solution sol = (Solution)o;
			sol.Saved -= OnSolutionSaved;
			GitUtil.Init (sol.BaseDirectory, null, null);
			
			GitRepository gitRepo = new GitRepository (sol.BaseDirectory, null);
			gitRepo.Add (sol.GetItemFiles (true).ToArray (), false, new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ());
		}

		public string Title {
			get {
				return GettextCatalog.GetString ("Git Support");
			}
		}

		public string Description {
			get {
				return GettextCatalog.GetString ("Git options for the new project");
			}
		}
	}
}

