// 
// DomOutputVisitor.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.Linq;

namespace MonoDevelop.Projects.Dom
{
	/// <summary>
	/// DOM output visitor. Used for debug purposes to get a string out of a monodevelop dom object.
	/// </summary>
	public class DomOutputVisitor : AbstractDomVisitor<object, object>
	{
		StringBuilder output = new StringBuilder ();
		
		public string Output {
			get {
				return this.output.ToString ();
			}
		}
		
		int indentLevel = 0;
		
		string CurIndent {
			get {
				return new string ('\t', indentLevel);
			}
		}
		
		public static string GetOutput (IMember member)
		{
			var visitor = new DomOutputVisitor ();
			member.AcceptVisitor (visitor, null);
			return visitor.Output;
		}

		
		public void OututReturnType (IReturnType returnType)
		{
			if (returnType == null) {
				output.Append ("<null>");
				return;
			}
			output.Append (returnType.ToInvariantString ());
		}		
		
		public override object Visit (IAttribute attribute, object data)
		{
			output.Append (CurIndent);
			output.Append ("[");
			output.Append (attribute.Name);
			output.AppendLine ("]");
			return null;
		}

		
		public void VisitAttributes (IMember member)
		{
			foreach (var attr in member.Attributes)
				attr.AcceptVisitor (this, null);
		}
		
		public override object Visit (IEvent evt, object data)
		{
			VisitAttributes (evt);
			output.Append (CurIndent);
			output.Append (evt.Modifiers.ToString ());
			output.Append (" event ");
			OututReturnType (evt.ReturnType);
			output.Append (" ");
			output.Append (evt.Name);
			output.AppendLine (";");
			return null;
		}
		
		public override object Visit (IField field, object data)
		{
			VisitAttributes (field);
			output.Append (CurIndent);
			output.Append (field.Modifiers.ToString ());
			output.Append (" ");
			OututReturnType (field.ReturnType);
			output.Append (" ");
			output.Append (field.Name);
			output.AppendLine (";");
			return null;
		}
		
		public override object Visit (IMethod method, object data)
		{
			VisitAttributes (method);
			output.Append (CurIndent);
			output.Append (method.Modifiers.ToString ());
			output.Append (" ");
			OututReturnType (method.ReturnType);
			output.Append (" ");
			output.Append (method.Name);
			output.AppendLine (" { }");
			return null;
		}
		
		public override object Visit (IProperty property, object data)
		{
			VisitAttributes (property);
			output.Append (CurIndent);
			output.Append (property.Modifiers.ToString ());
			output.Append (" ");
			OututReturnType (property.ReturnType);
			output.Append (" ");
			output.Append (property.Name);
			output.Append (" {");
			if (property.HasGet)
				output.Append (" get;");
			if (property.HasSet)
				output.Append (" set;");
			output.AppendLine ("}");
			return null;
		}
		
		public override object Visit (IType type, object data)
		{
			VisitAttributes (type);
			output.Append (CurIndent);
			output.Append (type.Modifiers.ToString ());
			output.Append (" ");
			switch (type.ClassType) {
			case ClassType.Class:
				output.Append ("class");
				break;
			case ClassType.Struct:
				output.Append ("struct");
				break;
			case ClassType.Delegate:
				output.Append ("delegate");
				break;
			case ClassType.Enum:
				output.Append ("enum");
				break;
			case ClassType.Interface:
				output.Append ("interface");
				break;
			}
			output.Append (" ");
			output.Append (type.Name);
			if (type.BaseTypes.Any ()) {
				output.Append (" : ");
				foreach (var baseType in type.BaseTypes) {
					output.Append (baseType.ToInvariantString ());
					output.Append (",");
				}
			}
			output.AppendLine (" {");
			indentLevel++;
			foreach (var member in type.Members)
				member.AcceptVisitor (this, null);
			indentLevel--;
			output.Append (CurIndent);
			output.AppendLine ("}");
			return null;
		}
	}
}

