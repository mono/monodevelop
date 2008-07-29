//
// GenericParameter.cs: Represents a type parameter for generic types. It stores
//                      constraint information.
//
// Author:
//   Matej Urbas (matej.urbas@gmail.com)
//
// (C) 2006 Matej Urbas
//
//
// This source code is licenced under The MIT License:
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

using SDReturnType = CSharpBinding.Parser.SharpDevelopTree.ReturnType;
using MDGenericParameter = MonoDevelop.Projects.Parser.GenericParameter;
using System;
using ICSharpCode.NRefactory.Ast;
using MonoDevelop.Projects.Parser;

namespace CSharpBinding.Parser.SharpDevelopTree
{
	public class GenericParameter : MDGenericParameter
	{
		public GenericParameter(TemplateDefinition td)
		{
			// Get the name of the generic parameter
			this.name = td.Name;
			
			// Get the constraints (base types) of this parameter 
			if (td.Bases != null && td.Bases.Count > 0) {
				this.baseTypes = new ReturnTypeList ();
				foreach (TypeReference tr in td.Bases) {
					// BUG: There seems to be a bug in the current version of
					// NRefactory. new(), struct and class special constraints
					// are all recognised as the struct constraint
					if (tr.Type == "struct") {
						this.specialConstraints = System.Reflection.GenericParameterAttributes.DefaultConstructorConstraint;
					}
					else
						this.baseTypes.Add (new SDReturnType (tr));
				}
			}
			
			// TODO: What about other special constraints?
		}
	}
}
