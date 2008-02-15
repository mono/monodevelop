//
// DomType.cs
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

namespace MonoDevelop.Ide.Dom
{
	public class DomType : AbstractMember, IType
	{
		protected object sourceProject;
		protected List<ICompilationUnit> compilationUnits = new List<ICompilationUnit> ();
		protected IReturnType baseType;
		protected List<TypeParameter> typeParameters = new List<TypeParameter> ();
		protected List<IDomItem> members = new List<IDomItem> ();
		protected List<IReturnType> implementedInterfaces = new List<IReturnType> ();
		protected ClassType classType = ClassType.Unknown;
		protected string namesp;
		
		public override string FullName {
			get {
				return !String.IsNullOrEmpty (namesp) ? namesp + "." + name : name;
			}
		}
		
		public string Namespace {
			get {
				return namesp;
			}
		}
		
		public object SourceProject {
			get {
				return sourceProject;
			}
		}

		public IEnumerable<ICompilationUnit> CompilationUnits {
			get {
				return compilationUnits;
			}
		}
		
		public ClassType ClassType {
			get {
				return classType;
			}
		}
		
		public IReturnType BaseType {
			get {
				return baseType;
			}
		}
		
		public IEnumerable<IReturnType> ImplementedInterfaces {
			get {
				return implementedInterfaces;
			}
		}
		
		public IEnumerable<TypeParameter> TypeParameters {
			get {
				return typeParameters;
			}
		}
		
		public virtual IEnumerable<IDomItem> Members {
			get {
				return members;
			}
		}
		
		public IEnumerable<IType> InnerTypes {
			get {
				foreach (IDomItem item in Members)
					if (item is IType)
						yield return (IType)item;
			}
		}

		public IEnumerable<IField> Fields {
			get {
				foreach (IDomItem item in Members)
					if (item is IField)
						yield return (IField)item;
			}
		}

		public IEnumerable<IProperty> Properties {
			get {
				foreach (IDomItem item in Members)
					if (item is IProperty)
						yield return (IProperty)item;
			}
		}

		public IEnumerable<IMethod> Methods {
			get {
				foreach (IDomItem item in Members)
					if (item is IMethod)
						yield return (IMethod)item;
			}
		}

		public IEnumerable<IEvent> Events {
			get {
				foreach (IDomItem item in Members)
					if (item is IEvent)
						yield return (IEvent)item;
			}
		}
		
		public override object AcceptVisitior (IDomVisitor visitor, object data)
		{
			return visitor.Visit (this, data);
		}
	}
}
