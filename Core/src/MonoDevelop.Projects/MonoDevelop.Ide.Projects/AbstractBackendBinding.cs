//
// AbstractBackendBinding.cs
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
using System.Collections.Generic;

using MonoDevelop.Ide.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;

namespace MonoDevelop.Ide.Projects
{
	public abstract class AbstractBackendBinding : IBackendBinding
	{
		public virtual IParser Parser { 
			get {
				return null;
			}
		}
		
		public virtual IRefactorer Refactorer { 
			get {
				return null;
			}	
		}
		
		public abstract string CommentTag {
			get;
		}
		
		bool hasProjectSupport = false;
		public bool HasProjectSupport {
			get {
				return hasProjectSupport;
			}
		}
		
		protected AbstractBackendBinding (bool hasProjectSupport)
		{
			this.hasProjectSupport = hasProjectSupport;
		}
		
		public virtual void StartProject (IProject project, IProgressMonitor monitor, ExecutionContext context)
		{
		}
		
		public virtual void CleanProject (IProject project, IProgressMonitor monitor)
		{
		}
		
		public virtual IProject LoadProject (string fileName)
		{
			return null;
		}
		public virtual IProject CreateProject (MonoDevelop.Projects.ProjectCreateInformation info)
		{
			return null;
		}
		
		public virtual CompilerResult Compile (IProject project, IProgressMonitor monitor)
		{
			return null;
		}
	}
}
