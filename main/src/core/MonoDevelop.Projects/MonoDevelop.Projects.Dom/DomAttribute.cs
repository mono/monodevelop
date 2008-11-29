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
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonoDevelop.Core.Collections;

namespace MonoDevelop.Projects.Dom
{
	public class DomAttribute : IAttribute
	{
		public string Name {
			get;
			set;
		}
		
		public DomRegion Region {
			get;
			set;
		}

		public AttributeTarget AttributeTarget {
			get;
			set;
		}

		public IReturnType AttributeType {
			get;
			set;
		}

		protected List<CodeExpression> positionalArguments;
		static readonly ReadOnlyCollection<CodeExpression> emptyPositionalArguments = new ReadOnlyCollection<CodeExpression> (new CodeExpression [0]);
		public ReadOnlyCollection<CodeExpression> PositionalArguments {
			get {
				return positionalArguments != null ? positionalArguments.AsReadOnly() : emptyPositionalArguments;
			}
		}

		protected Dictionary<string, CodeExpression> namedArguments;
		static readonly ReadOnlyDictionary<string, CodeExpression> emptyNamedArguments = new ReadOnlyDictionary<string, CodeExpression> (new Dictionary<string, CodeExpression> ());
		public ReadOnlyDictionary<string, CodeExpression> NamedArguments {
			get {
				return namedArguments != null ? new ReadOnlyDictionary<string, CodeExpression> (namedArguments) : emptyNamedArguments;
			}
		}

		public void AddPositionalArgument (CodeExpression exp)
		{
			if (positionalArguments == null)
				positionalArguments = new List<CodeExpression> ();
			positionalArguments.Add (exp);
		}

		public void AddNamedArgument (string name, CodeExpression exp)
		{
			if (namedArguments == null)
				namedArguments = new Dictionary<string, CodeExpression> ();
			namedArguments [name] = exp;
		}
		
		public static IAttribute Resolve (IAttribute source, ITypeResolver typeResolver)
		{
			DomAttribute result = new DomAttribute ();
			result.Name            = source.Name;
			result.Region          = source.Region;
			result.AttributeTarget = source.AttributeTarget;
			result.AttributeType   = source.AttributeType;

			if (source.PositionalArguments.Count > 0)
				result.positionalArguments = new List<CodeExpression> (source.PositionalArguments);
			if (source.NamedArguments.Count > 0)
				result.namedArguments = new Dictionary<string, CodeExpression> (source.NamedArguments);
			return result;
		}
		
		public static List<IAttribute> Resolve (IEnumerable<IAttribute> source, ITypeResolver typeResolver)
		{
			if (source == null)
				return null;
			List<IAttribute> result = new List<IAttribute> ();
			foreach (IAttribute attr in source) {
				result.Add (Resolve (attr, typeResolver));
			}
			return result;
		}
		
		public S AcceptVisitor<T, S> (IDomVisitor<T, S> visitor, T data)
		{
			return visitor.Visit (this, data);
		}
		
		public override string ToString ()
		{
			return string.Format ("[DomAttribute:Name={0}, #PositionalArguments={1}, #NamedArguments={2}, AttributeType={3}, Region={4}]",
			                      Name,
			                      PositionalArguments.Count,
			                      NamedArguments.Count,
			                      AttributeType,
			                      Region);
		}
	}
}
