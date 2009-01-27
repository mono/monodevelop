// CopyDomVisitor.cs
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
using System.CodeDom;
using System.Collections.Generic;

namespace MonoDevelop.Projects.Dom
{
	public class CopyDomVisitor<T>: IDomVisitor<T,IDomVisitable>
	{
		#region  IDomVisitor<object, IDomVisitable> implementation 
		
		public virtual IDomVisitable Visit (ICompilationUnit unit, T data)
		{
			CompilationUnit newUnit = CreateInstance (unit, data);
			foreach (IUsing u in unit.Usings)
				newUnit.Add ((IUsing) u.AcceptVisitor (this, data));
			foreach (IAttribute a in unit.Attributes)
				newUnit.Add ((IAttribute) a.AcceptVisitor (this, data));
			foreach (IType t in unit.Attributes)
				newUnit.Add ((IType) t.AcceptVisitor (this, data));
			return newUnit;
		}
		
		public virtual IDomVisitable Visit (IAttribute attribute, T data)
		{
			DomAttribute newAttr = CreateInstance (attribute, data);
			newAttr.Name = attribute.Name;
			newAttr.Region = attribute.Region;
			newAttr.AttributeTarget = attribute.AttributeTarget;
			newAttr.AttributeType = attribute.AttributeType;
			
			foreach (CodeExpression exp in attribute.PositionalArguments)
				newAttr.AddPositionalArgument (exp);
			foreach (KeyValuePair<string,CodeExpression> val in attribute.NamedArguments)
				newAttr.AddNamedArgument (val.Key, val.Value);
			return newAttr;
		}
		
		void Visit (IMember source, AbstractMember target, T data)
		{
			target.Name           = source.Name;
			target.Documentation  = source.Documentation;
			target.Modifiers      = source.Modifiers;
			target.Location       = source.Location;
			target.BodyRegion     = source.BodyRegion;
			if (source.ReturnType != null)
				target.ReturnType = (IReturnType) source.ReturnType.AcceptVisitor (this, data);
			foreach (IReturnType rt in source.ExplicitInterfaces)
				target.AddExplicitInterface ((IReturnType) rt.AcceptVisitor (this, data));
			foreach (IAttribute attr in source.Attributes)
				target.Add ((IAttribute) attr.AcceptVisitor (this, data));
		}
		
		public virtual IDomVisitable Visit (IType type, T data)
		{
			DomType result = CreateInstance (type, data);
			Visit (type, result, data);
			result.CompilationUnit = type.CompilationUnit;
			result.Namespace     = type.Namespace;
			result.ClassType     = type.ClassType;
			
			foreach (ITypeParameter param in type.TypeParameters)
				result.AddTypeParameter ((TypeParameter) Visit (param, data));
			
			if (type.BaseType != null)
				result.BaseType = (IReturnType) type.BaseType.AcceptVisitor (this, data);
			
			foreach (IReturnType iface in type.ImplementedInterfaces)
				result.AddInterfaceImplementation ((IReturnType) iface.AcceptVisitor (this, data));
			
			foreach (IMember member in type.Members)
				result.Add ((IMember) member.AcceptVisitor (this, data));
			
			return result;
		}
		
		public virtual IDomVisitable Visit (IField field, T data)
		{
			DomField result = CreateInstance (field, data);
			Visit (field, result, data);
			return result;
		}
		
		public virtual IDomVisitable Visit (IMethod source, T data)
		{
			DomMethod result = CreateInstance (source, data);
			Visit (source, result, data);
			
			foreach (IReturnType returnType in source.GenericParameters)
				result.AddGenericParameter ((IReturnType) returnType.AcceptVisitor (this, data));
			
			result.MethodModifier = source.MethodModifier;
			if (source.Parameters != null) {
				foreach (IParameter parameter in source.Parameters)
					result.Add ((IParameter) parameter.AcceptVisitor (this, data));
			}
			
			return result;
		}
		
		public virtual IDomVisitable Visit (IProperty source, T data)
		{
			DomProperty result = CreateInstance (source, data);
			Visit (source, result, data);
			result.PropertyModifier = source.PropertyModifier;
			result.GetRegion = source.GetRegion;
			result.SetRegion = source.SetRegion;
			return result;
		}
		
		public virtual IDomVisitable Visit (IEvent source, T data)
		{
			DomEvent result = CreateInstance (source, data);
			Visit (source, result, data);
			if (source.AddMethod != null)
				result.AddMethod = (IMethod) source.AddMethod.AcceptVisitor (this, data);
			if (source.RemoveMethod != null)
				result.RemoveMethod = (IMethod) source.RemoveMethod.AcceptVisitor (this, data);
			if (source.RaiseMethod != null)
				result.RaiseMethod = (IMethod) source.RaiseMethod.AcceptVisitor (this, data);
			return result;
		}
		
		protected virtual IReturnTypePart Visit (IReturnTypePart returnTypePart, T data)
		{
			ReturnTypePart newPart = new ReturnTypePart ();
			newPart.Name = returnTypePart.Name;
			foreach (IReturnType ga in returnTypePart.GenericArguments)
				newPart.AddTypeParameter ((IReturnType)ga.AcceptVisitor (this, data));
			return newPart;
		}
		
		public virtual IDomVisitable Visit (IReturnType type, T data)
		{
			List<IReturnTypePart> parts = new List<IReturnTypePart> (type.Parts.Count);
			
			foreach (IReturnTypePart part in type.Parts)
				parts.Add ((IReturnTypePart) Visit (part, data));
			
			DomReturnType rt = new DomReturnType (type.Namespace, parts);
			rt.PointerNestingLevel = type.PointerNestingLevel;
			rt.IsNullable = type.IsNullable;
			rt.ArrayDimensions = type.ArrayDimensions;
			for (int n=0; n<type.ArrayDimensions; n++)
				rt.SetDimension (n, type.GetDimension (n));
			return rt;
		}
		
		public virtual IDomVisitable Visit (IParameter source, T data)
		{
			DomParameter result = new DomParameter ();
			result.Name               = source.Name;
			result.ParameterModifiers = source.ParameterModifiers;
			result.ReturnType         = (IReturnType) source.ReturnType.AcceptVisitor (this, data);
			foreach (IAttribute attr in source.Attributes)
				result.Add ((IAttribute) attr.AcceptVisitor (this, data));
			return result;
		}
		
		public virtual IDomVisitable Visit (IUsing u, T data)
		{
			DomUsing result = new DomUsing ();
			result.Region = u.Region;
			result.IsFromNamespace = u.IsFromNamespace;
			foreach (string s in u.Namespaces)
				result.Add (s);
			foreach (KeyValuePair<string, IReturnType> val in u.Aliases)
				result.Add (val.Key, val.Value);
			return result;
		}
		
		public virtual IDomVisitable Visit (Namespace namesp, T data)
		{
			Namespace ns = new Namespace (namesp.Name);
			Visit (namesp, ns, data);
			return ns;
		}
		
		public virtual IDomVisitable Visit (LocalVariable var, T data)
		{
			return new LocalVariable (var.DeclaringMember, var.Name, (IReturnType)var.ReturnType.AcceptVisitor (this, data), var.Region);
		}
		
		protected virtual ITypeParameter Visit (ITypeParameter type, T data)
		{
			TypeParameter tp = new TypeParameter (type.Name);
			tp.ClassRequired = type.ClassRequired;
			tp.ValueTypeRequired = type.ValueTypeRequired;
			tp.ConstructorRequired = type.ConstructorRequired;
			foreach (IAttribute attr in type.Attributes)
				tp.AddAttribute ((IAttribute) attr.AcceptVisitor (this, data));
			foreach (IReturnType rt in type.Constraints)
				tp.AddConstraint ((IReturnType) rt.AcceptVisitor (this, data));
			return tp;
		}
		
		#endregion 
		
		protected virtual DomType CreateInstance (IType type, T data)
		{
			return new DomType ();
		}
		
		protected virtual CompilationUnit CreateInstance (ICompilationUnit source, T data)
		{
			return new CompilationUnit (source.FileName);
		}
		
		protected virtual DomAttribute CreateInstance (IAttribute source, T data)
		{
			return new DomAttribute ();
		}
		
		protected virtual DomField CreateInstance (IField source, T data)
		{
			return new DomField ();
		}
		
		protected virtual DomMethod CreateInstance (IMethod source, T data)
		{
			return new DomMethod ();
		}
		
		protected virtual DomProperty CreateInstance (IProperty source, T data)
		{
			return new DomProperty ();
		}
		
		protected virtual DomEvent CreateInstance (IEvent source, T data)
		{
			return new DomEvent ();
		}
	}
}
