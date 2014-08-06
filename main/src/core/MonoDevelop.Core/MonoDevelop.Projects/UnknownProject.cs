﻿//
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

namespace MonoDevelop.Projects
{
	public class UnknownProject: Project
	{
		string loadError = string.Empty;
		bool unloaded;

		// Store the file name locally to avoid the file format to change it
		FilePath fileName;

		public UnknownProject ()
		{
			NeedsReload = false;
			loadError = GettextCatalog.GetString ("Unknown project type");
		}

		public UnknownProject (FilePath file, string loadError): this ()
		{
			NeedsReload = false;
			FileName = file;
			this.loadError = loadError;
		}

		public override bool SupportsConfigurations ()
		{
			return true;
		}

		public override IEnumerable<string> GetProjectTypes ()
		{
			yield return "";
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
			get { return unloaded ? GettextCatalog.GetString ("Unavailable") : loadError; }
			set { loadError = value; }
		}

		public bool UnloadedEntry {
			get { return unloaded; }
			set { unloaded = value; }
		}

		public override string Name {
			get {
				if (!FileName.IsNullOrEmpty)
					return FileName.FileNameWithoutExtension;
				else
					return GettextCatalog.GetString ("Unknown entry");
			}
			set { }
		}

		internal protected override bool OnGetSupportsTarget (string target)
		{
			// We can't do anything with unsupported projects, other than display them in the solution pad
			return false;
		}

		protected override void OnClean (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
		}

		protected override BuildResult OnBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			var r = new BuildResult ();
			r.AddError (loadError);
			return r;
		}

		protected internal override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
		}

		protected internal override bool OnGetNeedsBuilding (ConfigurationSelector configuration)
		{
			return false;
		}

		protected internal override void OnSetNeedsBuilding (bool value, ConfigurationSelector configuration)
		{
		}

		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			return new ProjectConfiguration (name);
		}
	}
}

