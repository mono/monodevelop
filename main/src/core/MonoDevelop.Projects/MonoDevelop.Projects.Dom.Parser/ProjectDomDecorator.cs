// 
// ProjectDomDecorator.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Dom.Parser
{
	public class ProjectDomDecorator : ProjectDom
	{
		protected ProjectDom decorated;
		
		public ProjectDomDecorator (ProjectDom decorated)
		{
			this.decorated = decorated;
		}
		
		internal override System.Collections.Generic.IEnumerable<string> OnGetReferences ()
		{
			throw new NotImplementedException ();
		}
		
		public override ReadOnlyCollection<ProjectDom> References {
			get {
				return decorated.References;
			}
		}
		
		public override IEnumerable<IType> Types {
			get {
				return decorated.Types;
			}
		}
		
		
		public override IEnumerable<string> ResolvePossibleNamespaces (IReturnType returnType)
		{
			return decorated.ResolvePossibleNamespaces (returnType);
		}

		public override IList<Tag> GetSpecialComments (FilePath fileName)
		{
			return decorated.GetSpecialComments (fileName);
		}
		
		public override void UpdateTagComments (FilePath fileName, IList<Tag> tags)
		{
			decorated.UpdateTagComments (fileName, tags);
		}

		public override void ForceUpdate ()
		{
			decorated.ForceUpdate ();
		}
		public override void ForceUpdate (bool updateReferences)
		{
			decorated.ForceUpdate (updateReferences);
		}
		
		public override IEnumerable<IType> GetTypes (FilePath fileName)
		{
			return decorated.GetTypes (fileName);
		}
		
		public override IEnumerable<IType> GetInheritanceTree (IType type)
		{
			return decorated.GetInheritanceTree (type);
		}
		
		public override IType ResolveType (IType type)
		{
			return decorated.ResolveType (type);
		}
		
		public override IType SearchType (INode searchIn, string decoratedFullName)
		{
			return decorated.SearchType (searchIn, decoratedFullName);
		}
		
		public override IType SearchType (INode searchIn, IReturnType returnType)
		{
			return decorated.SearchType (searchIn, returnType);
		}
		
		public override List<IMember> GetNamespaceContents (string subNamespace, bool includeReferences, bool caseSensitive)
		{
			return decorated.GetNamespaceContents (subNamespace, includeReferences, caseSensitive);
		}
		
		public override List<IMember> GetNamespaceContents (IEnumerable<string> subNamespaces, bool includeReferences, bool caseSensitive)
		{
			return decorated.GetNamespaceContents (subNamespaces, includeReferences, caseSensitive);
		}
		
		public override bool NamespaceExists (string namespaceName, bool searchDeep, bool caseSensitive)
		{
			return decorated.NamespaceExists (namespaceName, searchDeep, caseSensitive);
		}
		
		public override bool NeedCompilation (FilePath fileName)
		{
			return decorated.NeedCompilation (fileName);
		}
		
		protected override IEnumerable<IType> InternalGetSubclasses (IType type, bool searchDeep, IList<string> namespaces)
		{
			throw new NotImplementedException ();
		}
		
		public override IEnumerable<IType> GetSubclasses (IType type, bool searchDeep, IList<string> namespaces)
		{
			return decorated.GetSubclasses (type, searchDeep, namespaces);
		}
		
		public override IType GetType (IReturnType returnType)
		{
			return decorated.GetType (returnType);
		}
		
		public override IType GetType (string typeName, IList<IReturnType> genericArguments, bool deepSearchReferences, bool caseSensitive)
		{
			return decorated.GetType (typeName, genericArguments, deepSearchReferences, caseSensitive);
		}
		
		public override IType GetType (string typeName, int genericArgumentsCount, bool deepSearchReferences, bool caseSensitive)
		{
			return decorated.GetType (typeName, genericArgumentsCount, deepSearchReferences, caseSensitive);
		}
		
		public override TypeUpdateInformation UpdateFromParseInfo (ICompilationUnit unit)
		{
			return decorated.UpdateFromParseInfo (unit);
		}
		
		public override IType CreateInstantiatedGenericType (IType type, IList<IReturnType> genericArguments)
		{
			return decorated.CreateInstantiatedGenericType (type, genericArguments);
		}
		
		public override IType CreateInstantiatedParameterType (IType outerType, TypeParameter tp)
		{
			return decorated.CreateInstantiatedParameterType (outerType, tp);
		}
	}
}
