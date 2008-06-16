//
// DefaultParserContext.cs
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
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Projects.Dom
{
	public class DefaultParserContext : IParserContext
	{
		ProjectDom dom;
		
		public DefaultParserContext (ProjectDom dom)
		{
			this.dom = dom;
		}
		
		public IExpressionFinder GetExpressionFinderFor (string fileName)
		{
			return null;
		}
		
		public ICompilationUnit GetCompilationUnitFor (string fileName)
		{
			return null;
		}
		
		public System.Collections.Generic.List<IMember> GetNamespaceContents (string nsName)
		{
			return null;
		}

		public SearchTypeResult SearchType (SearchTypeRequest request)
		{
			IType result = ProjectDomService.GetType (request.Name, request.GenericParameterCount);
			if (result != null)
				return new SearchTypeResult (result);
			foreach (IUsing u in request.CurrentCompilationUnit.Usings) {
				result = ProjectDomService.GetType (u.Namespaces + "." + request.Name, request.GenericParameterCount);
				if (result != null)
					return new SearchTypeResult (result);
			}
			return null;
		}

		public IType LookupType (IReturnType returnType)
		{
			if (dom != null) 
				return dom.GetType (returnType.FullName, returnType.GenericArguments.Count);
			
			return ProjectDomService.GetType (returnType.FullName, returnType.GenericArguments.Count);
		}
	}
}
