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
		protected List<IParameter> parameters = new List<IParameter> ();
		
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
				return parameters.AsReadOnly ();
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
		
		public void Add (IParameter parameter)
		{
			this.parameters.Add (parameter);
		}
		
		public DomMethod (string name, Modifiers modifiers, bool isConstructor, DomLocation location, DomRegion bodyRegion)
		{
			this.name       = name;
			this.modifiers      = modifiers;
			this.location   = location;
			this.bodyRegion = bodyRegion;
		}
		
		public DomMethod (string name, Modifiers modifiers, bool isConstructor, DomLocation location, DomRegion bodyRegion, IReturnType returnType, List<IParameter> parameters)
		{
			this.name       = name;
			this.modifiers      = modifiers;
			this.location   = location;
			this.bodyRegion = bodyRegion;
			this.returnType = returnType;
			this.parameters = parameters;
		}
		
		
		public override int CompareTo (object obj)
		{
			if (obj is IMethod)
				return Name.CompareTo (((IMethod)obj).Name);
			return -1;
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
