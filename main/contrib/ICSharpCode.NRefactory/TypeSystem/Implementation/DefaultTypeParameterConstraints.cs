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
using System.Collections.ObjectModel;
using System.Linq;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Implementation of <see cref="ITypeParameterConstraints".
	/// </summary>
	public sealed class DefaultTypeParameterConstraints : ReadOnlyCollection<IType>, ITypeParameterConstraints
	{
		public static readonly ITypeParameterConstraints Empty = new DefaultTypeParameterConstraints(new IType[0], false, false, false);
		
		public bool HasDefaultConstructorConstraint { get; private set; }
		public bool HasReferenceTypeConstraint { get; private set; }
		public bool HasValueTypeConstraint { get; private set; }
		
		public DefaultTypeParameterConstraints(IEnumerable<IType> constraints, bool hasDefaultConstructorConstraint, bool hasReferenceTypeConstraint, bool hasValueTypeConstraint)
			: base(constraints.ToArray())
		{
			this.HasDefaultConstructorConstraint = hasDefaultConstructorConstraint;
			this.HasReferenceTypeConstraint = hasReferenceTypeConstraint;
			this.HasValueTypeConstraint = hasValueTypeConstraint;
		}
		
		public ITypeParameterConstraints ApplySubstitution(TypeVisitor substitution)
		{
			if (substitution == null)
				throw new ArgumentNullException("substitution");
			if (this.Count == 0)
				return this;
			else
				return new DefaultTypeParameterConstraints(
					this.Select(c => c.AcceptVisitor(substitution)),
					this.HasDefaultConstructorConstraint, this.HasReferenceTypeConstraint, this.HasValueTypeConstraint);
		}
	}
}
