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
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Resolve result representing a built-in unary operator.
	/// (user-defined operators use InvocationResolveResult)
	/// </summary>
	public class UnaryOperatorResolveResult : ResolveResult
	{
		public readonly UnaryOperatorType Operator;
		public readonly ResolveResult Input;
		
		public UnaryOperatorResolveResult(IType resultType, UnaryOperatorType op, ResolveResult input)
			: base(resultType)
		{
			if (input == null)
				throw new ArgumentNullException("input");
			this.Operator = op;
			this.Input = input;
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return new [] { Input };
		}
	}
	
	/// <summary>
	/// Resolve result representing a built-in binary operator.
	/// (user-defined operators use InvocationResolveResult)
	/// </summary>
	public class BinaryOperatorResolveResult : ResolveResult
	{
		public readonly BinaryOperatorType Operator;
		public readonly ResolveResult Left;
		public readonly ResolveResult Right;
		
		public BinaryOperatorResolveResult(IType resultType, ResolveResult lhs, BinaryOperatorType op, ResolveResult rhs)
			: base(resultType)
		{
			if (lhs == null)
				throw new ArgumentNullException("lhs");
			if (rhs == null)
				throw new ArgumentNullException("rhs");
			this.Left = lhs;
			this.Operator = op;
			this.Right = rhs;
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return new [] { Left, Right };
		}
	}
	
	/// <summary>
	/// Resolve result representing the conditional operator.
	/// </summary>
	public class ConditionalOperatorResolveResult : ResolveResult
	{
		public readonly ResolveResult Condition;
		public readonly ResolveResult True;
		public readonly ResolveResult False;
		
		public ConditionalOperatorResolveResult(IType targetType, ResolveResult condition, ResolveResult @true, ResolveResult @false)
			: base(targetType)
		{
			if (condition == null)
				throw new ArgumentNullException("condition");
			if (@true == null)
				throw new ArgumentNullException("true");
			if (@false == null)
				throw new ArgumentNullException("false");
			this.Condition = condition;
			this.True = @true;
			this.False = @false;
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return new [] { Condition, True, False };
		}
	}
	
	/// <summary>
	/// Resolve result representing an array access.
	/// </summary>
	public class ArrayAccessResolveResult : ResolveResult
	{
		public readonly ResolveResult Array;
		public readonly ResolveResult[] Indices;
		
		public ArrayAccessResolveResult(IType elementType, ResolveResult array, ResolveResult[] indices) : base(elementType)
		{
			if (array == null)
				throw new ArgumentNullException("array");
			if (indices == null)
				throw new ArgumentNullException("indices");
			this.Array = array;
			this.Indices = indices;
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return new [] { Array }.Concat(Indices);
		}
	}
	
	/// <summary>
	/// Resolve result representing an array creation.
	/// </summary>
	public class ArrayCreateResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets the size arguments.
		/// </summary>
		public readonly ResolveResult[] SizeArguments;
		
		/// <summary>
		/// Gets the initializer elements.
		/// This field may be null if no initializer was specified.
		/// </summary>
		public readonly ResolveResult[] InitializerElements;
		
		readonly object[] constantArray;
		
		public ArrayCreateResolveResult(IType arrayType, ResolveResult[] sizeArguments, ResolveResult[] initializerElements,
		                                bool allowArrayConstants)
			: base(arrayType)
		{
			this.SizeArguments = sizeArguments;
			this.InitializerElements = initializerElements;
			if (allowArrayConstants) {
				this.constantArray = MakeConstantArray(sizeArguments, initializerElements);
			}
		}
		
		static object[] MakeConstantArray(ResolveResult[] sizeArguments, ResolveResult[] initializerElements)
		{
			if (initializerElements == null)
				return null;
			
			for (int i = 0; i < initializerElements.Length; i++) {
				if (!initializerElements[i].IsCompileTimeConstant)
					return null;
			}
			
			if (sizeArguments != null && sizeArguments.Length > 0) {
				if (sizeArguments.Length > 1) {
					// 2D-arrays can't be constant
					return null;
				}
				if (!sizeArguments[0].IsCompileTimeConstant)
					return null;
				
				int expectedSize;
				try {
					expectedSize = (int)CSharpPrimitiveCast.Cast(TypeCode.Int32, sizeArguments[0].ConstantValue, true);
				} catch (InvalidCastException) {
					return null;
				} catch (OverflowException) {
					return null;
				}
				if (expectedSize != initializerElements.Length)
					return null;
			}
			
			object[] constants = new object[initializerElements.Length];
			for (int i = 0; i < initializerElements.Length; i++) {
				constants[i] = initializerElements[i].ConstantValue;
			}
			return constants;
		}
		
		public override object ConstantValue {
			get { return constantArray; }
		}
		
		public override bool IsCompileTimeConstant {
			get { return constantArray != null; }
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			if (SizeArguments != null && InitializerElements != null)
				return SizeArguments.Concat(InitializerElements);
			else
				return SizeArguments ?? InitializerElements ?? EmptyList<ResolveResult>.Instance;
		}
	}
}
