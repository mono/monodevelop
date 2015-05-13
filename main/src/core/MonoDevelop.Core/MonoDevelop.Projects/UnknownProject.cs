//
// UnknownProject.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.Projects.Formats.MSBuild;

namespace MonoDevelop.Projects
{
	public class UnknownProject: Project
	{
		// Store the file name locally to avoid the file format to change it
		FilePath fileName;

		public UnknownProject ()
		{
			Initialize (this);
			IsUnsupportedProject = true;
		}

		protected override void OnExtensionChainInitialized ()
		{
			base.OnExtensionChainInitialized ();
			NeedsReload = false;
		}

		public UnknownProject (FilePath file, string loadError): this ()
		{
			NeedsReload = false;
			FileName = file;
			UnsupportedProjectMessage = loadError;
		}

		public override FilePath FileName {
			get { return fileName; }
			set {
				// Don't allow changing the file name once it is set
				// File formats may try to change it
				if (fileName == FilePath.Null) {
					fileName = value; 
					NeedsReload = false;
				}
			}
		}

		protected override string OnGetName ()
		{
			if (!FileName.IsNullOrEmpty)
				return FileName.FileNameWithoutExtension;
			else
				return GettextCatalog.GetString ("Unknown entry");
		}

		protected override bool OnGetSupportsTarget (string target)
		{
			// We can't do anything with unsupported projects, other than display them in the solution pad
			return false;
		}

		protected override Task<BuildResult> OnClean (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return Task.FromResult (BuildResult.Success);
		}

		protected override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			var r = new BuildResult ();
			r.AddError (UnsupportedProjectMessage);
			return Task.FromResult (r);
		}

		protected override Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return new Task (delegate {
			});
		}

		protected override SolutionItemConfiguration OnCreateConfiguration (string name, ConfigurationKind kind)
		{
			return new ProjectConfiguration (name);
		}
	}
}

