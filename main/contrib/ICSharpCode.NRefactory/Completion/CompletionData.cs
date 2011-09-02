// 
// CompletionData.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;

namespace Completion
{
	[Flags]
	public enum DisplayFlags
	{
		None = 0,
		Obsolete = 1
	}
	
	public class EntityCompletionData : CompletionData
	{
		public IEntity Entity {
			get;
			set;
		}
	}
	
	public class CompletionData
	{
		protected CompletionData () {}
		
		public virtual string DisplayText { get; set; }
		public virtual string Description { get; set; }
		
		public virtual DisplayFlags DisplayFlags { get; set; }
		
		public virtual bool IsOverloaded { 
			get {
				return false;
			}
		}
		
		public virtual IEnumerable<CompletionData> OverloadedData {
			get {
				throw new System.InvalidOperationException ();
			}
		}
	}
	
}

