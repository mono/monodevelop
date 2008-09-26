// FoldingRegion.cs
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

namespace MonoDevelop.Projects.Dom
{
	public class FoldingRegion
	{
		static string defaultName = "...";
		
		public string Name { get; set; }
		
		//NOTE: thsi is only respected if the FoldType is set to "Undefined"
		public bool IsFoldedByDefault { get; set; }
		
		public DomRegion Region { get; set; }
		
		public FoldType Type { get; set; }
		
		public FoldingRegion (DomRegion region) : this (null, region)
		{
		}
		
		public FoldingRegion (string name, DomRegion region) 
		{
			this.Name = name ?? defaultName;
			this.Region = region;
		}
		
		public FoldingRegion (string name, DomRegion region, bool isFoldedByDefault) : this (name, region)
		{
			this.IsFoldedByDefault = isFoldedByDefault;
		}
		
		public FoldingRegion (string name, DomRegion region, FoldType type) : this (name, region)
		{
			this.Type = type;
		}
		
		public FoldingRegion (DomRegion region, FoldType type) : this (null, region, type)
		{
		}
	}
	
	public enum FoldType {
		Undefined = 0,
		ConditionalDefine,
		Comment,
		CommentInsideMember,
		UserRegion,
		Type,
		Member
	}
}
