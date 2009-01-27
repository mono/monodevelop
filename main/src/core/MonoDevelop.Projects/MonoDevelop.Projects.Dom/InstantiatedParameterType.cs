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

namespace MonoDevelop.Projects.Dom
{
	internal class InstantiatedParameterType: DomType
	{
		public InstantiatedParameterType (IType outerType, TypeParameter tp)
		{
			compilationUnit = outerType.CompilationUnit;
			ClassType = ClassType.Class;
			Modifiers = Modifiers.Public;
			Name = tp.Name;
			Namespace = outerType.Namespace;
			Location = outerType.Location;
			DeclaringType = outerType;
			
			Console.WriteLine ("pp InstantiatedParameterType: " + tp.Name);
			
			foreach (IReturnType rt in tp.Constraints) {
				Console.WriteLine ("pprt: " + rt);
				if (rt.FullName == "constraint: struct")
					BaseType = new DomReturnType ("System.ValueType");
				else if (rt.FullName == "constraint: class")
					BaseType = DomReturnType.Object;
				else if (rt.FullName == "constraint: new")
					continue;
				else {
					if (BaseType == null)
						BaseType = rt;
					else
						AddInterfaceImplementation (rt);
				}
			}
			if (BaseType == null)
				BaseType = DomReturnType.Object;
		}
	}
}
