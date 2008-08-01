//
// IReturnType.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MonoDevelop.Projects.Dom
{
	[Flags]
	public enum ReturnTypeModifiers {
		None     = 0,
		ByRef    = 1,
		Nullable = 2
	}
	
	public interface IReturnTypePart
	{
		string Name {
			get;
			set;
		}
		
		ReadOnlyCollection<IReturnType> GenericArguments {
			get;
		}
		
		void AddTypeParameter (IReturnType type);
	}
	
	/// <summary>
	/// General return type format:
	/// Namespace.Part1,...,PartN
	/// Where Part is a typename: Typename&lt;arg1, ... ,argn&gt;
	/// </summary>
	public interface IReturnType : IReturnTypePart, IDomVisitable
	{
		string FullName {
			get;
		}
		
		string Namespace {
			get;
		}
		
		List<IReturnTypePart> Parts {
			get;
		}
		
		int PointerNestingLevel {
			get;
		}
		
		int ArrayDimensions {
			get;
		}
		
		ReturnTypeModifiers Modifiers {
			get;
		}
		
		bool IsByRef {
			get;
		}
		
		bool IsNullable {
			get;
		}
		
		string ToInvariantString ();
		int GetDimension (int arrayDimension);
		
		
	}
	
	public interface ITypeResolver
	{
		IReturnType Resolve (IReturnType type);
	}
}
