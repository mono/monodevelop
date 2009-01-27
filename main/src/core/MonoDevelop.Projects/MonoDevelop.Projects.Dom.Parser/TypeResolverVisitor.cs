// TypeResolverVisitor.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.Projects.Dom.Parser
{
	public class TypeResolverVisitor: CopyDomVisitor<IType>
	{
		ProjectDom db;
		ICompilationUnit unit;
		int unresolvedCount;
		IMethod currentMethod;
		
		public TypeResolverVisitor (ProjectDom db, ICompilationUnit unit)
		{
			this.db = db;
			this.unit = unit;
		}
		
		public override IDomVisitable Visit (IType type, IType data)
		{
			return base.Visit (type, type);
		}
		
		public override IDomVisitable Visit (IMethod source, IType data)
		{
			currentMethod = source;
			IDomVisitable res = base.Visit (source, data);
			currentMethod = null;
			return res;
		}
		
		public override IDomVisitable Visit (IReturnType type, IType contextType)
		{
			if (currentMethod != null) {
				foreach (IReturnType t in currentMethod.GenericParameters) {
					if (t.FullName == type.FullName)
						return type;
				}
			}
			
			IReturnTypePart firstPart = type.Parts [0];
			string name = type.Namespace.Length > 0 ? type.Namespace + "." + firstPart.Name : firstPart.Name;
			if (firstPart.GenericArguments.Count > 0)
				name += "`" + firstPart.GenericArguments.Count;
			IType c = db.SearchType (new SearchTypeRequest (unit, contextType, name));
			if (c == null) {
				unresolvedCount++;
				return db.GetSharedReturnType (type);
			}

			List<IReturnTypePart> parts = new List<IReturnTypePart> (type.Parts.Count);
			IType curType = c.DeclaringType;
			while (curType != null) {
				ReturnTypePart newPart = new ReturnTypePart {Name = curType.Name};
				for (int n=curType.TypeParameters.Count - 1; n >= 0; n--)
					newPart.AddTypeParameter (new DomReturnType ("?"));
				parts.Insert (0, newPart);
				curType = curType.DeclaringType;
			}
			
			foreach (ReturnTypePart part in type.Parts) {
				ReturnTypePart newPart = new ReturnTypePart ();
				newPart.Name = part.Name;
				foreach (IReturnType ga in part.GenericArguments)
					newPart.AddTypeParameter ((IReturnType)ga.AcceptVisitor (this, contextType));
				parts.Add (newPart);
			}
			
			DomReturnType rt = new DomReturnType (c.Namespace, parts);
			
			// Make sure the whole type is resolved
			if (parts.Count > 1 && db.SearchType (new SearchTypeRequest (unit, rt, contextType)) == null) {
				unresolvedCount++;
				return db.GetSharedReturnType (type);
			}
			
			rt.PointerNestingLevel = type.PointerNestingLevel;
			rt.IsNullable = type.IsNullable;
			rt.ArrayDimensions = type.ArrayDimensions;
			for (int n=0; n<type.ArrayDimensions; n++)
				rt.SetDimension (n, type.GetDimension (n));
			
			return db.GetSharedReturnType (rt);
		}
		
		public int UnresolvedCount
		{
			get { return unresolvedCount; }
			set { unresolvedCount = value; }
		}
	}
}
