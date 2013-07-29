// 
// SpecificStatus.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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
using System.Linq;

using NGit;
using NGit.Treewalk;
using NGit.Treewalk.Filter;

namespace MonoDevelop.VersionControl.Git
{
	class FilteredStatus : NGit.Api.StatusCommand
	{
		WorkingTreeIterator iter;
		IndexDiff diff;

		IEnumerable<string> Files {
			get; set;
		}
		
		public FilteredStatus (NGit.Repository repository)
			: base (repository)
		{
		}
		
		public FilteredStatus (NGit.Repository repository, IEnumerable<string> files)
			: base (repository)
		{
			Files = files;
		}
		
		public override NGit.Api.StatusCommand SetWorkingTreeIt (WorkingTreeIterator workingTreeIt)
		{
			iter = workingTreeIt;
			return this;
		}
		
		public override NGit.Api.Status Call ()
		{
			if (iter == null)
				iter = new FileTreeIterator (repo);
			
			diff = new IndexDiff (repo, Constants.HEAD, iter);
			if (Files != null) {
				var filters = Files.Where (f => f != ".").ToArray ();
				if (filters.Length > 0)
					diff.SetFilter (PathFilterGroup.CreateFromStrings (filters));
			}

			diff.Diff ();
			return new NGit.Api.Status (diff);
		}

		public virtual ICollection<string> GetIgnoredNotInIndex ()
		{
			return diff.GetIgnoredNotInIndex ();
		}
	}
}

