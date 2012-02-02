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

using System;
using Mono.Debugging.Evaluation;
using Mono.Debugger.Soft;

namespace Mono.Debugging.Soft
{
	public class StringAdaptor: IStringAdaptor
	{
		bool atleast_2_10;
		StringMirror str;
		string val;
		
		public StringAdaptor (StringMirror str)
		{
			atleast_2_10 = str.VirtualMachine.Version.AtLeast (2, 10);
			this.str = str;
		}
		
		public int Length {
			get {
				if (atleast_2_10)
					return str.Length;
				
				if (val == null)
					val = str.Value;
				
				return val.Length;
			}
		}
		
		public string Value {
			get {
				if (val == null)
					val = str.Value;
				
				return val;
			}
		}
		
		public string Substring (int index, int length)
		{
			if (val == null && atleast_2_10)
				return new string (str.GetChars (index, length));
			
			if (val == null)
				val = str.Value;
			
			return val.Substring (index, length);
		}
	}
}
