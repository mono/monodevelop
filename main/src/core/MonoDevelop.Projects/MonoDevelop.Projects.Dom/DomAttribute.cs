//
// DomAttribute.cs
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
	public class DomAttribute : IAttribute
	{
		protected DomRegion       region;
		protected AttributeTarget attributeTarget;
		protected IReturnType     attributeType;
		
		protected List<object> positionalArguments = new List<object> ();
		protected Dictionary<string, object> namedArguments = new Dictionary<string, object> ();
		
		public DomRegion Region {
			get {
				return region;
			}
		}

		public AttributeTarget AttributeTarget {
			get {
				return attributeTarget;
			}
		}

		public IReturnType AttributeType {
			get {
				return attributeType;
			}
		}

		public System.Collections.Generic.IEnumerable<object> PositionalArguments {
			get {
				return positionalArguments;
			}
		}

		public System.Collections.Generic.IDictionary<string, object> NamedArguments {
			get {
				return namedArguments;
			}
		}
		
		public static IAttribute Resolve (IAttribute source, ITypeResolver typeResolver)
		{
			DomAttribute result = new DomAttribute ();
			result.region          = source.Region;
			result.attributeTarget = source.AttributeTarget;
			result.attributeType   = source.AttributeType;
			return result;
		}
		
		public static List<IAttribute> Resolve (IEnumerable<IAttribute> source, ITypeResolver typeResolver)
		{
			List<IAttribute> result = new List<IAttribute> ();
			foreach (IAttribute attr in source) {
				result.Add (Resolve (attr, typeResolver));
			}
			return result;
		}
		
		public object AcceptVisitior (IDomVisitor visitor, object data)
		{
			return visitor.Visit (this, data);
		}
	}
}
