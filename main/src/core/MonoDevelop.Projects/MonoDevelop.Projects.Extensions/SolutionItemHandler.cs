// MD1SolutionItemHandler.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.CodeDom.Compiler;
using MonoDevelop.Core;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects.Extensions
{
	public abstract class SolutionItemHandler: ISolutionItemHandler
	{
		SolutionItem item;
		
		public SolutionItemHandler (SolutionItem item)
		{
			this.item = item;
		}
		
		public virtual bool SyncFileName {
			get { return true; }
		}

		public SolutionItem Item {
			get { return item; }
		}
		
		public virtual ICompilerResult RunTarget (IProgressMonitor monitor, string target, string configuration)
		{
			switch (target)
			{
			case "Build":
				return OnBuild (monitor, configuration);
			case "Clean":
				return OnClean (monitor, configuration);
			}
			return new DefaultCompilerResult (new CompilerResults (null), "");
		}
		
		protected virtual ICompilerResult OnBuild (IProgressMonitor monitor, string configuration)
		{
			return null;
		}
		
		protected virtual ICompilerResult OnClean (IProgressMonitor monitor, string configuration)
		{
			return null;
		}
		
		public virtual void Dispose ()
		{
		}
		
		public abstract string ItemId { get; }
		
		public abstract void Save (IProgressMonitor monitor);
	}
}
