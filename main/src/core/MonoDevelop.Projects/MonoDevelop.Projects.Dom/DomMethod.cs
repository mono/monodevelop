//
// DomMethod.cs
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
using System.Collections.ObjectModel;

namespace MonoDevelop.Projects.Dom
{
	public class DomMethod : AbstractMember, IMethod
	{
		protected bool isConstructor;
		protected List<IParameter> parameters = null;
		protected List<IReturnType> genericParameters = null;
		
		public bool IsConstructor {
			get {
				return isConstructor;
			}
			set {
				isConstructor = value;
			}
		}
		
		public ReadOnlyCollection<IParameter> Parameters {
			get {
				return parameters != null ? parameters.AsReadOnly () : null;
			}
		}
		
		public ReadOnlyCollection<IReturnType> GenericParameters {
			get {
				return genericParameters != null ? genericParameters.AsReadOnly () : null;
			}
		}
		
		
		
		static readonly string[] iconTable = {Stock.Method, Stock.PrivateMethod, Stock.ProtectedMethod, Stock.InternalMethod};
		public override string StockIcon {
			get {
				return iconTable [ModifierToOffset (Modifiers)];
			}
		}
		
		public DomMethod ()
		{
		}
		
		public DomMethod (string name, Modifiers modifiers, bool isConstructor, DomLocation location, DomRegion bodyRegion)
		{
			this.Name       = name;
			this.modifiers      = modifiers;
			this.location   = location;
			this.bodyRegion = bodyRegion;
			this.isConstructor = isConstructor;
		}
		
		public DomMethod (string name, Modifiers modifiers, bool isConstructor, DomLocation location, DomRegion bodyRegion, IReturnType returnType, List<IParameter> parameters)
		{
			this.Name       = name;
			this.modifiers      = modifiers;
			this.location   = location;
			this.bodyRegion = bodyRegion;
			this.returnType = returnType;
			this.parameters = parameters;
			this.isConstructor = isConstructor;
		}
		
		public void Add (IParameter parameter)
		{
			if (parameters == null) 
				parameters = new List<IParameter> ();
			parameters.Add (parameter);
		}
		
		public void AddGenericParameter (IReturnType genPara)
		{
			if (genericParameters == null) 
				genericParameters = new List<IReturnType> ();
			genericParameters.Add (genPara);
		}

		
		public override int CompareTo (object obj)
		{
			if (obj is IMethod)
				return Name.CompareTo (((IMethod)obj).Name);
			return -1;
		}
		
		public static IMethod Resolve (IMethod source, ITypeResolver typeResolver)
		{
			DomMethod result = new DomMethod ();
			result.Name          = source.Name;
			result.Documentation = source.Documentation;
			result.Modifiers     = source.Modifiers;
			result.ReturnType    = DomReturnType.Resolve (source.ReturnType, typeResolver);
			result.Location      = source.Location;
			result.bodyRegion    = source.BodyRegion;
			result.AddRange (DomAttribute.Resolve (source.Attributes, typeResolver));
			
			if (source.Parameters != null) {
				foreach (IParameter parameter in source.Parameters)
					result.Add (DomParameter.Resolve (parameter, typeResolver));
			}
			
			if (source.GenericParameters != null && source.GenericParameters.Count > 0) {
				foreach (IReturnType returnType in source.GenericParameters) {
					result.AddGenericParameter (DomReturnType.Resolve (returnType, typeResolver));
				}
			}
			
			return result;
		}
		
		
		public override string ToString ()
		{
			return string.Format ("[DomMethod:Name={0}, Modifiers={1}, ReturnType={2}, Location={3}]",
			                      Name,
			                      Modifiers,
			                      ReturnType,
			                      Location);
		}
		
		public override object AcceptVisitior (IDomVisitor visitor, object data)
		{
			return visitor.Visit (this, data);
		}
	}
}
