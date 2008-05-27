//
// DomParameter.cs
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
	public class DomParameter : IParameter
	{
		protected string name;
		protected ParameterModifiers parameterModifiers;
		protected IReturnType returnType;
		protected List<IAttribute> attributes = new List<IAttribute> ();
		protected DomRegion region;
		
		public string Name {
			get {
				return name;
			}
		}

		public ParameterModifiers ParameterModifiers {
			get {
				return parameterModifiers;
			}
		}

		public IReturnType ReturnType {
			get {
				return returnType;
			}
		}

		public System.Collections.Generic.IEnumerable<IAttribute> Attributes {
			get {
				return attributes;
			}
		}

		public DomRegion Region {
			get {
				return region;
			}
		}
		
		protected DomParameter ()
		{
		}
		
		public DomParameter (string name, IReturnType returnType)
		{
			this.name       = name;
			this.returnType = returnType;
		}
		
		public override string ToString ()
		{
			return string.Format ("[DomParameter:Name={0}, ParameterModifiers={1}, ReturnType={2}, Region={3}]",
			                      Name,
			                      ParameterModifiers,
			                      ReturnType,
			                      Region);
		}

		public object AcceptVisitior (IDomVisitor visitor, object data)
		{
			return visitor.Visit (this, data);
		}
	}
}
