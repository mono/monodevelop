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
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Ide.TypeSystem
{
	[Serializable]
	public class ConditionBlock
	{
		public string Flag {
			get;
			set;
		}
		
		public DocumentRegion Region {
			get;
			set;
		}
		
		public DocumentLocation Start {
			get;
			set;
		}
		
		public DocumentLocation End {
			get;
			set;
		}
		
		public ConditionBlock (string flag) : this (flag, DocumentLocation.Empty)
		{
		}

		public ConditionBlock (string flag, DocumentLocation start)
		{
			this.Flag = flag;
			this.Start = start;
			this.Region = DocumentRegion.Empty;
		}
	}
	
	[Serializable]
	public class ConditionalRegion : ConditionBlock
	{
		public DocumentRegion ElseBlock {
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
