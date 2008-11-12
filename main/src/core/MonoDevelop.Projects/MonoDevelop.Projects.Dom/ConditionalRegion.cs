// ConditionalRegion.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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

using System;
using System.Collections.Generic;

namespace MonoDevelop.Projects.Dom
{
	public class ConditionBlock
	{
		public string Flag {
			get;
			set;
		}
		
		public DomRegion Region {
			get;
			set;
		}
		
		public DomLocation Start {
			get;
			set;
		}
		
		public DomLocation End {
			get;
			set;
		}
		
		public ConditionBlock (string flag) : this (flag, DomLocation.Empty)
		{
		}
		public ConditionBlock (string flag, DomLocation start)
		{
			this.Flag   = flag;
			this.Start  = start;
			this.Region = DomRegion.Empty;
		}
	}
	
	public class ConditionalRegion : ConditionBlock
	{
		public DomRegion ElseBlock {
			get;
			set;
		}
		
		List<ConditionBlock> conditionBlocks = new List<ConditionBlock> ();
		public List<ConditionBlock> ConditionBlocks {
			get {
				return conditionBlocks;
			}
		}
		
		public ConditionalRegion (string flag) : base (flag)
		{
		}
	}
}
