// 
// RefactoringOperation.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace MonoDevelop.Refactoring
{
	public abstract class RefactoringOperation
	{
		public string Name {
			get;
			set;
		}
		
		public bool IsBreakingAPI {
			get;
			set;
		}
		
		public virtual string AccelKey {
			get {
				return "";
			}
		}
		public virtual string GetMenuDescription (RefactoringOptions options)
		{
			return Name;
		}
		
		public virtual bool IsValid (RefactoringOptions options)
		{
			return true;
		}
		
		public virtual List<Change> PerformChanges (RefactoringOptions options, object properties)
		{
			throw new System.NotImplementedException ();
		}

		public virtual void Run (RefactoringOptions options)
		{
			var changes = PerformChanges (options, null);
			var monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (Name, null);
			RefactoringService.AcceptChanges (monitor, changes);
		}
	}
}
