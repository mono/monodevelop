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
		string flag;
		public string Flag {
			get {
				return flag;
			}
			set {
				flag = value;
			}
		}
		
		DomRegion region = DomRegion.Empty;
		public DomRegion Region {
			get {
				return region;
			}
			set {
				region = value;
			}
		}
		
		public DomLocation Start {
			get {
				return region.Start;
			}
			set {
				region.Start = value;
			}
		}
		
		public DomLocation End {
			get {
				return region.End;
			}
			set {
				region.End = value;
			}
		}
		
		public ConditionBlock (string flag)
		{
			this.flag = flag;
		}
		public ConditionBlock (string flag, DomLocation start)
		{
			this.flag = flag;
			this.Start = start;
		}
	}
	
	public class ConditionalRegion : ConditionBlock
	{
		List<ConditionBlock> conditionBlocks = new List<ConditionBlock> ();
		
		DomRegion region;
		public DomRegion ElseBlock {
			get {
				return region;
			}
			set {
				region = value;
			}
		}
		
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
