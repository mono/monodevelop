// SolutionItemEventArgs.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2004 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects;
using MonoDevelop.Core;
using System.Collections.Generic;

namespace MonoDevelop.Projects
{
	public delegate void SolutionItemEventHandler (object sender, SolutionItemEventArgs e);
	
	public class SolutionItemEventArgs : EventArgs
	{
		SolutionFolderItem entry;
		Solution solution;
		
		public SolutionFolderItem SolutionItem {
			get {
				return entry;
			}
		}
		
		public Solution Solution {
			get {
				return solution ?? entry.ParentSolution;
			}
		}
		
		public SolutionItemEventArgs (SolutionFolderItem entry)
		{
			this.entry = entry;
		}
		
		public SolutionItemEventArgs (SolutionFolderItem entry, Solution solution)
		{
			this.solution = solution;
			this.entry = entry;
		}
	}

	public delegate void SolutionItemSavedEventHandler (object sender, SolutionItemSavedEventArgs e);

	public class SolutionItemSavedEventArgs : SolutionItemEventArgs
	{
		bool savingSolution;

		public SolutionItemSavedEventArgs (SolutionFolderItem item, Solution parentSolution, bool savingSolution) : base (item, parentSolution)
		{
			this.savingSolution = savingSolution;
		}

		public bool SavingSolution {
			get { return savingSolution; }
		}

		/// <summary>
		/// When Reloading is true, it returns the original solution item that is being reloaded
		/// </summary>
		/// <value>The replaced item.</value>
		public SolutionFolderItem ReplacedItem { get; internal set; }
	}
	
	public delegate void SolutionItemChangeEventHandler (object sender, SolutionItemChangeEventArgs e);

	public class SolutionItemChangeEventArgs : SolutionItemEventArgs
	{
		bool reloading;
		
		public SolutionItemChangeEventArgs (SolutionFolderItem item, Solution parentSolution, bool reloading): base (item, parentSolution)
		{
			this.reloading = reloading;
		}
		
		public bool Reloading {
			get { return reloading; }
		}

		/// <summary>
		/// When Reloading is true, it returns the original solution item that is being reloaded
		/// </summary>
		/// <value>The replaced item.</value>
		public SolutionFolderItem ReplacedItem { get; internal set; }
	}
	
	public delegate void SolutionItemModifiedEventHandler (object sender, SolutionItemModifiedEventArgs e);
	
	public class SolutionItemModifiedEventArgs: EventArgsChain<SolutionItemModifiedEventInfo>
	{
		public SolutionItemModifiedEventArgs (SolutionFolderItem item, string hint)
		{
			Add (new SolutionItemModifiedEventInfo (item, hint));
		}
	}
	
	public class SolutionItemModifiedEventInfo: SolutionItemEventArgs
	{
		string hint;
		
		public SolutionItemModifiedEventInfo (SolutionFolderItem item, string hint): base (item)
		{
			this.hint = hint;
		}
		
		public string Hint {
			get { return hint; }
		}
	}
}
