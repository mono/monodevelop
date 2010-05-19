// 
// RawValue.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Mono.Debugging.Backend;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class RawValue
	{
		IRawValue source;
		internal EvaluationOptions options;
		
		public RawValue (IRawValue source)
		{
			this.source = source;
		}
		
		internal IRawValue Source {
			get { return this.source; }
		}
		
		
		public string TypeName { get; set; }
		
		public object CallMethod (string methodName, params object[] parameters)
		{
			object res = source.CallMethod (methodName, parameters, options);
			RawValue val = res as RawValue;
			if (val != null)
				val.options = options;
			return res;
		}
		
		public object GetMemberValue (string name)
		{
			object res = source.GetMemberValue (name, options);
			RawValue val = res as RawValue;
			if (val != null)
				val.options = options;
			return res;
		}
		
		public void SetMemberValue (string name, object value)
		{
			source.SetMemberValue (name, value, options);
		}
	}
	
	[Serializable]
	public class RawValueArray
	{
		IRawValueArray source;
		int[] dimensions;
		
		public RawValueArray (IRawValueArray source)
		{
			this.source = source;
		}
		
		internal IRawValueArray Source {
			get { return this.source; }
		}
		
		public string ElementTypeName { get; set; }
		
		public object this [int index] {
			get {
				return source.GetValue (new int[] { index });
			}
			set {
				source.SetValue (new int[] { index }, value);
			}
		}
		
		public Array ToArray ()
		{
			return source.ToArray ();
		}
		
		public int Length {
			get {
				if (dimensions == null)
					dimensions = source.Dimensions;
				return dimensions[0];
			}
		}
	}
}

