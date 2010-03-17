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
		
		public void SetCurrentMember (IMember currentMember)
		{
			currentMethod = currentMember as IMethod;
		}
		
		public TypeResolverVisitor (ProjectDom db, ICompilationUnit unit)
		{
			this.db = db;
			this.unit = unit;
		}
		
		public override INode Visit (IType type, IType data)
		{
			return base.Visit (type, type);
		}
		
		public override INode Visit (IMethod source, IType data)
		{
			currentMethod = source;
			INode res = base.Visit (source, data);
			currentMethod = null;
			return res;
		}
		
		bool visitAttribute = false;
		public override INode Visit (IAttribute attribute, IType data)
		{
			visitAttribute = true;
			var result = base.Visit (attribute, data);
			visitAttribute = false;
			return result;
		}

		
		public override INode Visit (IReturnType type, IType contextType)
		{
			if (type.GenericArguments.Count == 0 && unit != null) { 
				foreach (IUsing u in unit.Usings) {
					if (u.IsFromNamespace || u.Aliases.Count == 0 && contextType != null && u.Region.Contains (contextType.Location))
						continue;
					foreach (KeyValuePair<string, IReturnType> alias in u.Aliases) {
						if (alias.Key == type.FullName) 
							return Visit (alias.Value, contextType);
					}
				}
			}
			
			if (currentMethod != null) {
				IMethod method = null;
				if (currentMethod.IsOverride) {
					foreach (IType curType2 in db.GetInheritanceTree (contextType)) {
						foreach (IMethod curMethod in curType2.SearchMember (currentMethod.Name, true)) {
							if (!curMethod.IsOverride && curMethod.Parameters.Count == currentMethod.Parameters.Count && curMethod.TypeParameters.Count == currentMethod.TypeParameters.Count) {
								method = curMethod;
								break;
							}
						}
						if (method != null)
							break;
					}
				}
				if (method == null)
					method = currentMethod;
				int idx = currentMethod.GetTypeParameterIndex (type.Name);
				if (idx >= 0) {
					ITypeParameter t = method.TypeParameters[idx];
					DomReturnType typeParameterReturnType = new DomReturnType (type.FullName);
					DomType constructedType = new InstantiatedParameterType (db, method, t);
					
					if (constructedType.BaseType == null) 
						constructedType.BaseType = DomReturnType.Object;
					
					constructedType.SourceProjectDom = db;
					
					typeParameterReturnType.Type = constructedType;
					typeParameterReturnType.ArrayDimensions = type.ArrayDimensions;
					typeParameterReturnType.PointerNestingLevel = type.PointerNestingLevel;
					for (int i = 0; i < type.ArrayDimensions; i++)
						typeParameterReturnType.SetDimension (i, type.GetDimension (i));
					return typeParameterReturnType;
				}
			}
			
//			IReturnTypePart firstPart = type.Parts[0];
			string name = type.DecoratedFullName; //!string.IsNullOrEmpty (type.Namespace) ? type.Namespace + "." + firstPart.Name : firstPart.Name;
//			if (firstPart.GenericArguments.Count > 0)
//				name += "`" + firstPart.GenericArguments.Count;
			IType lookupType = db.SearchType (unit, contextType, null, name);
			if (visitAttribute && lookupType == null) {
				name += "Attribute";
				lookupType = db.SearchType (unit, contextType, null, name);
			}
			if (lookupType == null) {
				unresolvedCount++;
				return db.GetSharedReturnType (type);
			}
			
			List<IReturnTypePart> parts = new List<IReturnTypePart> (type.Parts.Count);
			IType curType = lookupType.DeclaringType;
			while (curType != null) {
				ReturnTypePart newPart = new ReturnTypePart {Name = curType.Name};
				newPart.IsGenerated = true;
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
			
			DomReturnType rt = new DomReturnType (lookupType.Namespace, parts);
			
			// Make sure the whole type is resolved
			if (parts.Count > 1 && db.SearchType (unit, contextType, null, rt) == null) {
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