// 
// AspProjectDom.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using System;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using System.Collections.Generic;

namespace MonoDevelop.AspNet.Gui
{
	/// <summary>
	/// This wraps a project dom and adds the compilation information from the ASP.NET page to the DOM to lookup members
	/// on the page.
	/// </summary>
	class AspProjectDom : ProjectDomDecorator
	{
		ParsedDocument doc;
		IList<ProjectDom> references;
		
		//FIXME: use all the doms
		public AspProjectDom (IList<ProjectDom> references, ParsedDocument doc) : base (references[0])
		{
			this.doc = doc;
			this.references = references;
		}
		
		IType constructedType = null;
		IType CheckType (IType type)
		{
			if (type == null)
				return null;
			if (type.IsPartial && doc.CompilationUnit.Types[0].FullName == type.FullName) {
				if (constructedType == null) 
					constructedType = CompoundType.Merge (doc.CompilationUnit.Types[0], type);
				constructedType.SourceProjectDom = this;
				return constructedType;
			}
			return type;
		}
		
		public override IType ResolveType (IType type)
		{
			if (type == constructedType)
				return type;
			return CheckType (base.ResolveType (type));
		}
		
		public override IType GetType (IReturnType returnType)
		{
			return CheckType (base.GetType (returnType));
		}

		public override IType GetType (string typeName, IList<IReturnType> genericArguments, 
		                               bool deepSearchReferences, bool caseSensitive)
		{
			return CheckType (base.GetType (typeName, genericArguments, deepSearchReferences, caseSensitive));
		}

		public override IType GetType (string typeName, int genericArgumentsCount, 
		                               bool deepSearchReferences, bool caseSensitive)
		{
			return CheckType (base.GetType (typeName, genericArgumentsCount, deepSearchReferences, caseSensitive));
		}
		
		public override System.Collections.Generic.IEnumerable<IType> GetInheritanceTree (IType type)
		{
			foreach (IType t in base.GetInheritanceTree (type)) {
				yield return CheckType (t);
			}
		}
	}
}

