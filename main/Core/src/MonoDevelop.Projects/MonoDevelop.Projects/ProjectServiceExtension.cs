// ProjectServiceExtension.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
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
using System.Collections.Specialized;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public class ProjectServiceExtension
	{
		internal ProjectServiceExtension Next;

		
		public virtual void Save (IProgressMonitor monitor, CombineEntry entry)
		{
			Next.Save (monitor, entry);
		}
		
		public virtual StringCollection GetExportFiles (CombineEntry entry)
		{
			return Next.GetExportFiles (entry);
		}
		
		public virtual bool IsCombineEntryFile (string fileName)
		{
			return Next.IsCombineEntryFile (fileName);
		}
		
		public virtual CombineEntry Load (IProgressMonitor monitor, string fileName)
		{
			return Next.Load (monitor, fileName);
		}
		
		public virtual void Clean (IProgressMonitor monitor, CombineEntry entry)
		{
			Next.Clean (monitor, entry);
		}
		
		public virtual ICompilerResult Build (IProgressMonitor monitor, CombineEntry entry)
		{
			return Next.Build (monitor, entry);
		}
		
		public virtual void Execute (IProgressMonitor monitor, CombineEntry entry, ExecutionContext context)
		{
			Next.Execute (monitor, entry, context);
		}
		
		public virtual bool GetNeedsBuilding (CombineEntry entry)
		{
			return Next.GetNeedsBuilding (entry);
		}
		
		public virtual void SetNeedsBuilding (CombineEntry entry, bool val)
		{
			Next.SetNeedsBuilding (entry, val);
		}

		public virtual string GetDefaultResourceId (ProjectFile pf)
		{
			return Next.GetDefaultResourceId (pf);
		}
	}
}
