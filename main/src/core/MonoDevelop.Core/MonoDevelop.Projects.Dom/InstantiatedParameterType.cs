// InstantiatedParameterType.cs
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
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom.Output;

namespace MonoDevelop.Projects.Dom
{
	internal class InstantiatedParameterType : DomType, ITypeParameterType
	{
		ITypeParameter typeparam;

		public override TypeKind Kind {
			get {
				return TypeKind.GenericParameter;
			}
		}

		public InstantiatedParameterType (ProjectDom dom, ITypeParameterMember typeParameterMember, ITypeParameter tp)
		{
			IType outerType = typeParameterMember as IType ?? typeParameterMember.DeclaringType;
			typeparam = tp;
			compilationUnit = outerType.CompilationUnit;
			ClassType = ClassType.Class;
			Modifiers = Modifiers.Public;
			Name = tp.Name;
			Namespace = outerType.DecoratedFullName;
			Location = outerType.Location;
			DeclaringType = outerType;
			if (tp.ValueTypeRequired)
				BaseType = new DomReturnType ("System.ValueType");
			
			if (tp.Constraints.Count > 0)
				ClassType = ClassType.Interface;
			foreach (IReturnType rt in tp.Constraints) {
				if (FindCyclicReference (new HashSet<ITypeParameter> () { tp }, outerType, ((DomReturnType)rt).DecoratedFullName))
					continue;
				IType bt = dom.SearchType (typeParameterMember, rt);
				IReturnType resolvedType = rt;
				if (bt != null) {
					resolvedType = new DomReturnType (bt);
					if (bt.ClassType == ClassType.Interface || BaseType != null) {
						AddInterfaceImplementation (resolvedType);
					} else {
						ClassType = bt.ClassType;
						BaseType = resolvedType;
					}
				} else {
					AddInterfaceImplementation (resolvedType);
				}
			}
			if (BaseType == null)
				BaseType = DomReturnType.Object;
		}

		bool FindCyclicReference (HashSet<ITypeParameter> visited, IType outerType, string sourceParamName)
		{
			// Normalize the param name
			if (sourceParamName.StartsWith (((DomType)outerType).DecoratedFullName + "."))
				sourceParamName = sourceParamName.Substring (sourceParamName.LastIndexOf ('.') + 1);
			else if (sourceParamName.IndexOf ('.') != -1)
				return false;
			
			ITypeParameter targetParam = null;
			foreach (ITypeParameter tp in outerType.TypeParameters) {
				if (tp.Name == sourceParamName) {
					targetParam = tp;
					break;
				}
			}
			
			if (targetParam == null)
				return false;
			
			if (!visited.Add (targetParam))
				return true;
			
			foreach (IReturnType rt in targetParam.Constraints) {
				if (FindCyclicReference (visited, outerType, ((DomReturnType)rt).DecoratedFullName))
					return true;
			}
			return false;
		}

		#region ITypeParameter Members


		public IList<IReturnType> Constraints {
			get { 
				return typeparam.Constraints; 
			}
		}

		public bool ConstructorRequired {
			get {
				return typeparam.ConstructorRequired;
			}
		}

		public bool ClassRequired {
			get { 
				return typeparam.ClassRequired; 
			}
		}

		public bool ValueTypeRequired {
			get { 
				return typeparam.ValueTypeRequired;
			}
		}

		public TypeParameterVariance Variance {
			get {
				return typeparam.Variance;
			}
		}

		#endregion
		
		public override string ToString ()
		{
			return string.Format ("[InstantiatedParameterType: typeparam={0}, #Constraints={1}]", typeparam, Constraints.Count);
		}
		
	}
}
