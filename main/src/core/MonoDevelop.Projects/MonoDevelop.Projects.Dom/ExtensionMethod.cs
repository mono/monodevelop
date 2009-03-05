// 
// ExtensionMethod.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Mike Krüger
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MonoDevelop.Projects.Dom
{
	public class ExtensionMethod : DomMethod
	{
		IMethod method;
		
		public IType ExtensionType {
			get;
			private set;
		}
		
		public IMethod OriginalMethod {
			get;
			private set;
		}

		public override DomLocation Location {
			get {
				return method.Location;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override IEnumerable<IAttribute> Attributes {
			get {
				return method.Attributes;
			}
		}
		
		public override DomRegion BodyRegion {
			get {
				return method.BodyRegion;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override string FullName {
			get {
				return method.FullName;
			}
		}
		
		public override string HelpUrl {
			get {
				return method.HelpUrl;
			}
		}
		
		public override Modifiers Modifiers {
			get {
				return method.Modifiers;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override string Name {
			get {
				return method.Name;
			}
			set {
				throw new NotSupportedException ();
			}
		}
		
		public override IReturnType ReturnType {
			get {
				return method.ReturnType;
			}
			set {
				throw new NotSupportedException ();
			}
		}
		
		
		public override string Documentation {
			get {
				return method.Documentation;
			}
			set {
				throw new NotSupportedException ();
			}
		}
		
		public override bool IsObsolete {
			get {
				return method.IsObsolete;
			}
		}
		
		public ExtensionMethod (IType extenisonType, IMethod originalMethod, IList<IReturnType> genericArguments, IList<IReturnType> methodArguments)
		{
			this.DeclaringType = extenisonType;
			List<IReturnType> args = new List<IReturnType> ();
			args.Add (new DomReturnType (extenisonType));
			args.AddRange (methodArguments);
			this.method = DomMethod.CreateInstantiatedGenericMethod (originalMethod, genericArguments, args);
			System.Console.WriteLine(method);
			// skip first parameter.
			for (int i = 1; i < method.Parameters.Count; i++) {
				Add (method.Parameters[i]);
			}
			this.ExtensionType  = extenisonType;
			this.OriginalMethod = originalMethod;
		}
	}
}
