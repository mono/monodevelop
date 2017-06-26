﻿//
// MicrosoftTemplateEngineProcessedTemplateResult.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
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
using System.Collections.Generic;
using Microsoft.TemplateEngine.Edge.Template;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Templates
{
	class MicrosoftTemplateEngineProcessedTemplateResult : ProcessedTemplateResult
	{
		readonly IWorkspaceFileObject[] workspaceItems;

		public MicrosoftTemplateEngineProcessedTemplateResult (IWorkspaceFileObject[] workspaceItems, string solutionFileName, string projectBasePath)
		{
			this.workspaceItems = workspaceItems;
			this.SolutionFileName = solutionFileName;
			this.ProjectBasePath = projectBasePath;
		}
		IEnumerable<string> filesToOpen = new string [0];
		public void SetFilesToOpen(IEnumerable<string> filesToOpen)
		{
			this.filesToOpen = filesToOpen;
		}

		public override IEnumerable<string> Actions {
			get {
				return filesToOpen;
			}
		}

		public override IList<PackageReferencesForCreatedProject> PackageReferences {
			get {
				throw new NotImplementedException ();
			}
		}

		public override IEnumerable<IWorkspaceFileObject> WorkspaceItems {
			get {
				return workspaceItems;
			}
		}

		public override bool HasPackages ()
		{
			return false;
		}
	}
}
