//
// DomEvent.cs
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

namespace MonoDevelop.Projects.Dom
{
	public class DomEvent : AbstractMember, IEvent
	{
		IMethod addMethod = null;
		IMethod removeMethod = null;
		IMethod raiseMethod = null;
		
		public virtual IMethod AddMethod {
			get {
				if (addMethod != null)
					return addMethod;
				return LookupSpecialMethod ("add_");
			}
			set {
				addMethod = value;
			}
		}
		
		public virtual IMethod RemoveMethod {
			get {
				if (removeMethod != null)
					return removeMethod;
				return LookupSpecialMethod ("remove_");
			}
			set {
				removeMethod = value;
			}
		}
		
		public virtual IMethod RaiseMethod {
			get {
				if (raiseMethod != null)
					return raiseMethod;
				return LookupSpecialMethod ("raise_");
			}
			set {
				raiseMethod = value;
			}
		}
		
		public DomEvent ()
		{
		}
		
		public DomEvent (string name, Modifiers modifiers, DomLocation location, IReturnType returnType)
		{
			this.Name = name;
			this.Modifiers = modifiers;
			this.Location = location;
			this.ReturnType = returnType;
		}
		
		public static IEvent Resolve (IEvent source, ITypeResolver typeResolver)
		{
			DomEvent result = new DomEvent ();
			AbstractMember.Resolve (source, result, typeResolver);
			if (source.AddMethod != null)
				result.addMethod = DomMethod.Resolve (source.AddMethod, typeResolver);
			if (source.RemoveMethod != null)
				result.removeMethod = DomMethod.Resolve (source.RemoveMethod, typeResolver);
			if (source.RaiseMethod != null)
				result.raiseMethod = DomMethod.Resolve (source.RaiseMethod, typeResolver);
			return result;
		}
		
		public override string HelpUrl {
			get {
				return "F:" + this.FullName;
			}
		}
		
		static readonly string[] iconTable = {Stock.Event, Stock.PrivateEvent, Stock.ProtectedEvent, Stock.InternalEvent};
		public override string StockIcon {
			get {
				return iconTable [ModifierToOffset (Modifiers)];
			}
		}
		
		public override string ToString ()
		{
			return string.Format ("[DomEvent:Name={0}, Modifiers={1}, ReturnType={2}, Location={3}]",
			                      Name,
			                      Modifiers,
			                      ReturnType,
			                      Location);
		}
		
		public override int CompareTo (object obj)
		{
			if (obj is IEvent)
				return Name.CompareTo (((IEvent)obj).Name);
			return 1;
		}
		
		public override object AcceptVisitor (IDomVisitor visitor, object data)
		{
			return visitor.Visit (this, data);
		}
	}
}
