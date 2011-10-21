// 
// UnknownTypeResolveResult.cs
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Semantics
{
	/// <summary>
	/// Represents an unknown type with type parameters.
	/// </summary>
	public class UnknownTypeResolveResult : ResolveResult
	{
		readonly string identifier;
		readonly int typeParameterCount;
		
		public UnknownTypeResolveResult(string identifier, int typeParameterCount)
			: base(SharedTypes.UnknownType)
		{
			this.identifier = identifier;
			this.typeParameterCount = typeParameterCount;
		}
		
		public string Identifier {
			get { return identifier; }
		}

		public int TypeParameterCount {
			get { return typeParameterCount; }
		}
		
		public override bool IsError {
			get { return true; }
		}
		
		public override string ToString()
		{
			return string.Format("[{0} {1}~{2}]", GetType().Name, identifier, typeParameterCount);
		}
	}
}
