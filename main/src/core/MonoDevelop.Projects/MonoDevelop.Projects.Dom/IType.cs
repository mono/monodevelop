//
// IType.cs
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
	public interface IType : IMember
	{
		string Namespace {
			get;
		}
		
		object SourceProject {
			get;
			set;
		}
		
		ICompilationUnit CompilationUnit {
			get;
		}
		
		ClassType ClassType {
			get;
		}
		
		IReturnType BaseType {
			get;
			set;
		}
		
		ReadOnlyCollection<IReturnType> ImplementedInterfaces {
			get;
		}
		
		ReadOnlyCollection<TypeParameter> TypeParameters {
			get;
		}
		
		IEnumerable<IMember> Members {
			get;
		}
		
		IEnumerable<IType> InnerTypes {
			get;
		}

		IEnumerable<IField> Fields {
			get;
		}

		IEnumerable<IProperty> Properties {
			get;
		}

		IEnumerable<IMethod> Methods {
			get;
		}

		IEnumerable<IEvent> Events {
			get;
		}
		
		List<IMember> SearchMember (string name, bool caseSensitive);
		
		/// <value>
		/// Types that are defined across several compilation unit (e.g. files) this returns
		/// the other types that are part of the same super type.
		/// </value>
		IEnumerable<IType> Parts { 
			get; 
		}
		bool HasParts {
			get;
		}
		
		int FieldCount {
			get;
		}
		int MethodCount {
			get;
		}
		int ConstructorCount {
			get;
		}
		int IndexerCount {
			get;
		}
		int PropertyCount {
			get;
		}
		int EventCount {
			get;
		}
		int InnerTypeCount {
			get;
		}
		
		bool HasOverriden (IMember member);
	}
}
