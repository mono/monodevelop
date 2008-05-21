// ChangeLogData.cs
//
// Author:
//   Jacob Ilsø Christensen
//   Lluis Sanchez Gual
//
// Copyright (C) 2007  Jacob Ilsø Christensen
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
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.ChangeLogAddIn
{	
	public enum ChangeLogPolicy
	{
		NoChangeLog,
		UseParentPolicy,
		UpdateNearestChangeLog,
		OneChangeLogInProjectRootDirectory,
		OneChangeLogInEachDirectory		
	}
	
	[DataItem ("ChangeLogInfo")]
	public class ChangeLogData
	{
		[ItemProperty]
		ChangeLogPolicy policy = ChangeLogPolicy.UseParentPolicy;
		
		SolutionItem entry;
		
		internal ChangeLogData ()
		{
		}
		
		internal ChangeLogData (SolutionItem entry)
		{
			this.entry = entry;
		}
		
		internal void Bind (SolutionItem entry)
		{
			this.entry = entry;
		}
		
		void UpdateData ()
		{
			if (entry != null) {
				entry.ExtendedProperties ["MonoDevelop.ChangeLogAddIn.ChangeLogInfo"] = this;
				entry.ExtendedProperties.Remove ("Temp.MonoDevelop.ChangeLogAddIn.ChangeLogInfo");
			}
		}
		
		public ChangeLogPolicy Policy
		{
			get {
				if (policy == ChangeLogPolicy.UseParentPolicy && (entry is SolutionFolder) && ((SolutionFolder)entry).IsRoot)
					return ChangeLogPolicy.UpdateNearestChangeLog;
				else
					return policy; 
			}
			set {
				if (value != policy) {
					policy = value; 
					UpdateData ();
				}
			}
		}
	}
}
