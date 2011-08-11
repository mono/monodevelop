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
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Contains well-known type references.
	/// </summary>
	public static class KnownTypeReference
	{
		/// <summary>
		/// Gets a type reference pointing to the <c>void</c> type.
		/// </summary>
		public static readonly ITypeReference Void = new GetClassTypeReference("System", "Void", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>object</c> type.
		/// </summary>
		public static readonly ITypeReference Object = new GetClassTypeReference("System", "Object", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>bool</c> type.
		/// </summary>
		public static readonly ITypeReference Boolean = new GetClassTypeReference("System", "Boolean", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>sbyte</c> type.
		/// </summary>
		public static readonly ITypeReference SByte = new GetClassTypeReference("System", "SByte", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>byte</c> type.
		/// </summary>
		public static readonly ITypeReference Byte = new GetClassTypeReference("System", "Byte", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>short</c> type.
		/// </summary>
		public static readonly ITypeReference Int16 = new GetClassTypeReference("System", "Int16", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>ushort</c> type.
		/// </summary>
		public static readonly ITypeReference UInt16 = new GetClassTypeReference("System", "UInt16", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>int</c> type.
		/// </summary>
		public static readonly ITypeReference Int32 = new GetClassTypeReference("System", "Int32", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>uint</c> type.
		/// </summary>
		public static readonly ITypeReference UInt32 = new GetClassTypeReference("System", "UInt32", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>long</c> type.
		/// </summary>
		public static readonly ITypeReference Int64 = new GetClassTypeReference("System", "Int64", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>ulong</c> type.
		/// </summary>
		public static readonly ITypeReference UInt64 = new GetClassTypeReference("System", "UInt64", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>string</c> type.
		/// </summary>
		public static readonly ITypeReference String = new GetClassTypeReference("System", "String", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>char</c> type.
		/// </summary>
		public static readonly ITypeReference Char = new GetClassTypeReference("System", "Char", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>float</c> type.
		/// </summary>
		public static readonly ITypeReference Single = new GetClassTypeReference("System", "Single", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>double</c> type.
		/// </summary>
		public static readonly ITypeReference Double = new GetClassTypeReference("System", "Double", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>decimal</c> type.
		/// </summary>
		public static readonly ITypeReference Decimal = new GetClassTypeReference("System", "Decimal", 0);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Type</c> type.
		/// </summary>
		public static readonly ITypeReference Type = new GetClassTypeReference("System", "Type", 0);
		
		/// <summary>
		/// Gets all known type references.
		/// </summary>
		public static IEnumerable<ITypeReference> AllKnownTypeReferences {
			get {
				return new[] {
					Void, Object, Boolean,
					SByte, Byte, Int16, UInt16, Int32, UInt32, Int64, UInt64,
					String, Char, Single, Double, Decimal,
					Type
				};
			}
		}
	}
}
