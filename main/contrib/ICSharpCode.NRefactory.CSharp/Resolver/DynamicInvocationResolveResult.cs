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
using System.Globalization;
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Represents a single argument in a dynamic invocation.
	/// </summary>
	public class DynamicInvocationArgument {
		/// <summary>
		/// Parameter name, if the argument is named. Null otherwise.
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// Value of the argument.
		/// </summary>
		public readonly ResolveResult Value;

		public DynamicInvocationArgument(string name, ResolveResult value) {
			Name = name;
			Value = value;
		}
	}

	/// <summary>
	/// Represents the result of an invocation of a member of a dynamic object.
	/// </summary>
	public class DynamicInvocationResolveResult : ResolveResult
	{
		/// <summary>
		/// Target of the invocation (a dynamic object).
		/// </summary>
		public readonly ResolveResult Target;

		/// <summary>
		/// Arguments for the call.
		/// </summary>
		public readonly IList<DynamicInvocationArgument> Arguments; 

		public DynamicInvocationResolveResult(ResolveResult target, IList<DynamicInvocationArgument> arguments) : base(SpecialType.Dynamic) {
			this.Target    = target;
			this.Arguments = arguments ?? EmptyList<DynamicInvocationArgument>.Instance;
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[Dynamic invocation ]");
		}
	}
}
