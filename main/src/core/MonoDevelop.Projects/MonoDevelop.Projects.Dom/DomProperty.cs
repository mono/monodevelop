//
// DomProperty.cs
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
using System.Diagnostics;
using System.Text;

namespace MonoDevelop.Projects.Dom
{
	public class DomProperty : AbstractMember, IProperty
	{
		protected List<IParameter> parameters = null;

		public PropertyModifier PropertyModifier {
			get;
			set;
		}
		
		public bool IsIndexer {
			get {
				return (PropertyModifier & PropertyModifier.IsIndexer) == PropertyModifier.IsIndexer;
			}
		}
		
		public bool HasGet {
			get {
				return (PropertyModifier & PropertyModifier.HasGet) == PropertyModifier.HasGet;
			}
		}
		
		public bool HasSet {
			get {
				return (PropertyModifier & PropertyModifier.HasSet) == PropertyModifier.HasSet;
			}
		}
		static readonly ReadOnlyCollection<IParameter> emptyParameters = new ReadOnlyCollection<IParameter> (new IParameter [0]);
		public virtual ReadOnlyCollection<IParameter> Parameters {
			get {
				return parameters != null ? parameters.AsReadOnly () : emptyParameters;
			}
		}

		public DomRegion GetRegion {
			get;
			set;
		}
		
		public DomRegion SetRegion {
			get;
			set;
		}
		
		public override string HelpUrl {
			get {
				StringBuilder result = new StringBuilder ();
				result.Append ("P:");
				if (this.IsIndexer) {
					result.Append (DeclaringType.FullName);
					result.Append (".Item");
					DomMethod.AppendHelpParameterList (result, this.Parameters);
				} else {
					result.Append (this.FullName);
				}
				return result.ToString ();
			}
		}
		
		static readonly string[] iconTable = {Stock.Property, Stock.PrivateProperty, Stock.ProtectedProperty, Stock.InternalProperty};
		
		public override string StockIcon {
			get {
				return iconTable [ModifierToOffset (Modifiers)];
			}
		}
		
		public DomProperty ()
		{
		}
		
		
		public override int CompareTo (object obj)
		{
			if (obj is IMethod)
				return 1;
			if (obj is IProperty)
				return Name.CompareTo (((IProperty)obj).Name);
			return -1;
		}
		
		public DomProperty (string name)
		{
			base.Name = name;
		}
		
		public DomProperty (string name, Modifiers modifiers, DomLocation location, DomRegion bodyRegion, IReturnType returnType)
		{
			base.Name       = name;
			this.Modifiers  = modifiers;
			this.Location   = location;
			this.BodyRegion = bodyRegion;
			base.ReturnType = returnType;
		}
		
		public void Add (IParameter parameter)
		{
			if (parameters == null) 
				parameters = new List<IParameter> ();
			parameters.Add (parameter);
		}
		
		public void Add (IEnumerable<IParameter> parameters)
		{
			if (parameters == null)
				return;
			foreach (IParameter parameter in parameters) {
				Add (parameter);
			}
		}
		
		public static IProperty Resolve (IProperty source, ITypeResolver typeResolver)
		{
			DomProperty result = new DomProperty ();
			AbstractMember.Resolve (source, result, typeResolver);
			result.PropertyModifier = source.PropertyModifier;
			result.GetRegion = source.GetRegion;
			result.SetRegion = source.SetRegion;
			return result;
		}
		
		public override string ToString ()
		{
			return string.Format ("[DomProperty:Name={0}, Modifiers={1}, ReturnType={2}, Location={3}, PropertyModifier={4}, GetRegion={5}, SetRegion={6}, ExplicitInterfaces={7}]",
			                      Name,
			                      Modifiers,
			                      ReturnType,
			                      Location,
			                      PropertyModifier,
			                      GetRegion,
			                      SetRegion,
			                      this.explicitInterfaces != null ? this.explicitInterfaces.Count.ToString () : "0");
		}
		
		public override object AcceptVisitor (IDomVisitor visitor, object data)
		{
			return visitor.Visit (this, data);
		}
	}
}
