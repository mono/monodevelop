// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.TypeSystem
{
	public sealed class ParameterListComparer : IEqualityComparer<IParameterizedMember>
	{
		ITypeResolveContext context;
		
		/// <summary>
		/// Creates a new ParameterListComparer that compares type <b>references</b>.
		/// </summary>
		public ParameterListComparer()
		{
		}
		
		/// <summary>
		/// Creates a new ParameterListComparer that uses the specified context to resolve types.
		/// </summary>
		public ParameterListComparer(ITypeResolveContext context)
		{
			this.context = context;
		}
		
		public bool Equals(IParameterizedMember x, IParameterizedMember y)
		{
			var px = x.Parameters;
			var py = y.Parameters;
			if (px.Count != py.Count)
				return false;
			for (int i = 0; i < px.Count; i++) {
				var a = px[i];
				var b = py[i];
				if (a == null && b == null)
					continue;
				if (a == null || b == null)
					return false;
				if (context != null) {
					if (!a.Type.Resolve(context).Equals(b.Type.Resolve(context)))
						return false;
				} else {
					if (!a.Type.Equals(b.Type))
						return false;
				}
			}
			return true;
		}
		
		public int GetHashCode(IParameterizedMember obj)
		{
			int hashCode = obj.Parameters.Count;
			unchecked {
				foreach (IParameter p in obj.Parameters) {
					hashCode *= 27;
					if (context != null)
						hashCode += p.Type.Resolve(context).GetHashCode();
					else
						hashCode += p.Type.GetHashCode();
				}
			}
			return hashCode;
		}
	}
}
