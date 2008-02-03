//
// Ambience.cs
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
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Dom.Output
{
	public class Ambience : IDomVisitor
	{
		string name;
		protected Dictionary<Modifiers, string> modifiers = new Dictionary<Modifiers, string> ();
		protected Dictionary<ClassType, string> classTypes = new Dictionary<ClassType, string> ();
		protected Dictionary<ParameterModifiers, string> parameterModifiers = new Dictionary<ParameterModifiers, string> ();
		protected Dictionary<string, string> constructs = new Dictionary<string, string> ();
		
		public Ambience (string name)
		{
			this.name = name;
		}
		
		protected string GetString (Modifiers m)
		{
			if ((m & Modifiers.ProtectedAndInternal) == Modifiers.ProtectedAndInternal)
				return GetString (m & ~Modifiers.ProtectedAndInternal) + " " + modifiers[Modifiers.ProtectedAndInternal];
			if ((m & Modifiers.ProtectedOrInternal) == Modifiers.ProtectedOrInternal)
				return GetString (m & ~Modifiers.ProtectedOrInternal) + " " + modifiers[Modifiers.ProtectedOrInternal];
			
			StringBuilder result = new StringBuilder ();
			foreach (Modifiers singleModifier in Enum.GetValues (typeof(Modifiers))) {
				if ((m & singleModifier) == singleModifier && modifiers.ContainsKey (singleModifier)) {
					if (result.Length > 0)
						result.Append (' ');
					result.Append (modifiers[singleModifier]);
				}
			}
			return result.ToString ();
		}
		
		protected string GetString (ClassType classType)
		{
			return classTypes.ContainsKey(classType) ? classTypes[classType] : "";
		}
		
		protected string GetString (ParameterModifiers m)
		{
			StringBuilder result = new StringBuilder ();
			foreach (ParameterModifiers singleModifier in Enum.GetValues (typeof(ParameterModifiers))) {
				if ((m & singleModifier) == singleModifier && parameterModifiers.ContainsKey (singleModifier)) {
					if (result.Length > 0)
						result.Append (' ');
					result.Append (parameterModifiers[singleModifier]);
				}
			}
			return result.ToString ();
		}
		
		
		public virtual object Visit (MonoDevelop.Ide.Dom.ICompilationUnit unit, object data)
		{
			return "";
		}

		public virtual object Visit (MonoDevelop.Ide.Dom.IAttribute attr, object data)
		{
			return "";
//			StringBuilder arguments = new StringBuilder ();
//			arguments.Append ('(');
//			foreach (object o in attr.PositionalArguments) {
//				if (arguments.Length > 1)
//					arguments.Append (',');
//				arguments.Append (o.ToString ());
//			}
//			foreach (KeyValuePair<string, object> pair in attr.PositionalArguments) {
//				if (arguments.Length > 1)
//					arguments.Append (',');
//				arguments.Append (pair.Key.ToString ());
//				arguments.Append ('=');
//				arguments.Append (pair.Value.ToString ());
//			}
//				
//			arguments.Append (')');
//			return StringParserService.Parse (constructs[Attribute], new string[,] {
//				{"Type", Visit (attr.AttributeType, data).ToString ()},
//				{"ArgumentList", arguments.ToString}
//			});
		}

		public virtual object Visit (MonoDevelop.Ide.Dom.IType type, object data)
		{
			return "";
		}

		public virtual object Visit (MonoDevelop.Ide.Dom.IField field, object data)
		{
			return "";
		}

		public virtual object Visit (MonoDevelop.Ide.Dom.IMethod method, object data)
		{
			return "";
		}

		public virtual object Visit (MonoDevelop.Ide.Dom.IProperty property, object data)
		{
			return "";
		}

		public virtual object Visit (MonoDevelop.Ide.Dom.IEvent e, object data)
		{
			return "";
		}

		public virtual object Visit (MonoDevelop.Ide.Dom.IReturnType type, object data)
		{
			return "";
		}

		public virtual object Visit (MonoDevelop.Ide.Dom.IParameter parameter, object data)
		{
			return "";
		}

		public virtual object Visit (MonoDevelop.Ide.Dom.IUsing u, object data)
		{
			return "";
		}
	}
}
