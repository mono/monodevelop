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
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	#if WITH_CONTRACTS
	[ContractClass(typeof(IConstantValueContract))]
	#endif
	public interface IConstantValue : IFreezable
	{
		/// <summary>
		/// Gets the type of the constant value.
		/// </summary>
		IType GetValueType(ITypeResolveContext context);
		
		/// <summary>
		/// Gets the .NET value of the constant value.
		/// Possible return values are:
		/// - null
		/// - primitive integers
		/// - float/double
		/// - bool
		/// - string
		/// - IType (for typeof-expressions)
		/// and arrays of these values. Enum values are returned using the underlying primitive integer.
		/// 
		/// TODO: how do we represent errors (value not available?)
		/// </summary>
		object GetValue(ITypeResolveContext context);
	}
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(IConstantValue))]
	abstract class IConstantValueContract : IFreezableContract, IConstantValue
	{
		IType IConstantValue.GetValueType(ITypeResolveContext context)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IType>() != null);
			return null;
		}
		
		object IConstantValue.GetValue(ITypeResolveContext context)
		{
			Contract.Requires(context != null);
			return null;
		}
	}
	#endif
}
