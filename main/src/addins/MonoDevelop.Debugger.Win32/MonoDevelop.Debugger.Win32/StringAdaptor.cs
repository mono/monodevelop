// 
// StringAdaptor.cs
//  
// Author: Jeffrey Stedfast <jeff@xamarin.com>
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
// 

using Microsoft.Samples.Debugging.CorDebug;
using Mono.Debugging.Evaluation;

namespace MonoDevelop.Debugger.Win32
{
	public class StringAdaptor: IStringAdaptor
	{
		readonly CorEvaluationContext ctx;
		readonly CorStringValue str;
		readonly CorValRef obj;
		
		public StringAdaptor (EvaluationContext ctx, CorValRef obj, CorStringValue str)
		{
			this.ctx = (CorEvaluationContext) ctx;
			this.str = str;
			this.obj = obj;
		}
		
		public int Length {
			get { return str.Length; }
		}
		
		public string Value {
			get { return str.String; }
		}
		
		public string Substring (int index, int length)
		{
			return str.String.Substring (index, length);
		}
	}
}
