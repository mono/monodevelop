//
// UnknownSolutionItem.cs
//
// Author:
//   Lluis Sanchez Gual
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

using System;
using MonoDevelop.Core;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	public class UnknownSolutionItem: SolutionItem
	{
		string loadError = string.Empty;
		bool unloaded;
		
		// Store the file name locally to avoid the file format to change it
		FilePath fileName;
		
		public UnknownSolutionItem ()
		{
			Initialize (this);
			IsUnsupportedProject = true;
		}

		protected override void OnExtensionChainInitialized ()
		{
			base.OnExtensionChainInitialized ();
			NeedsReload = false;
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

		
		public string LoadError {
			get { return loadError; }
			set { loadError = value; }
		}

		public bool UnloadedEntry {
			get { return unloaded; }
			set {
				unloaded = value;
				if (value)
					loadError = GettextCatalog.GetString ("Unavailable");
			}
		}

		protected override string OnGetName ()
		{
			if (!FileName.IsNullOrEmpty)
				return FileName.FileNameWithoutExtension;
			else
				return GettextCatalog.GetString ("Unknown entry");
		}

		protected override Task<BuildResult> OnClean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			return Task.FromResult (BuildResult.Success);
		}
		
		protected override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			var r = new BuildResult ();
			r.AddError ("Project unavailable");
			return Task.FromResult (r);
		}
		
		protected internal override Task OnSave (ProgressMonitor monitor)
		{
			return Task.FromResult (0);
		}
	}

	public class UnloadedSolutionItem: UnknownSolutionItem
	{
		public UnloadedSolutionItem ()
		{
			Initialize (this);
			UnloadedEntry = true;
		}
	}
}
