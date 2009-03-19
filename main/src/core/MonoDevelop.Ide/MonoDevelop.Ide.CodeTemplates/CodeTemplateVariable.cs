// 
// CodeTemplateVariable.cs
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
using System.Xml;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.CodeTemplates
{
	public class CodeTemplateVariable
	{
		public string Name {
			get;
			set;
		}
		
		public string Default {
			get;
			set;
		}
		
		public string ToolTip {
			get;
			set;
		}
		
		public string Function {
			get;
			set;
		}
		
		public bool IsEditable {
			get;
			set;
		}
		
		public CodeTemplateVariable ()
		{
			IsEditable = true;
		}
		
		public override string ToString ()
		{
			return string.Format("[CodeTemplateVariable: Name={0}, Default={1}, ToolTip={2}, Function={3}]", Name, Default, ToolTip, Function);
		}
		
		public const string Node        = "Variable";
		const string nameAttribute       = "name";
		const string editableAttribute   = "isEditable";
		const string DefaultNode         = "Default";
		const string TooltipNode         = "_ToolTip";
		const string FunctionNode        = "Function";

		public void Write (XmlWriter writer)
		{
			writer.WriteStartElement (Node);
			writer.WriteAttributeString (nameAttribute, Name);
			if (!IsEditable) 
				writer.WriteAttributeString (editableAttribute, "false");
			

			if (!string.IsNullOrEmpty (Default)) {
				writer.WriteStartElement (DefaultNode);
				writer.WriteString (Default);
				writer.WriteEndElement (); 
			}
			
			if (!string.IsNullOrEmpty (ToolTip)) {
				writer.WriteStartElement (TooltipNode);
				writer.WriteString (ToolTip);
				writer.WriteEndElement (); 
			}
			
			if (!string.IsNullOrEmpty (Function)) {
				writer.WriteStartElement (FunctionNode);
				writer.WriteString (Function);
				writer.WriteEndElement (); 
			}
			
			
			writer.WriteEndElement (); // Node
		}
		
		public static CodeTemplateVariable Read (XmlReader reader)
		{
			CodeTemplateVariable result = new CodeTemplateVariable ();
			result.Name = reader.GetAttribute (nameAttribute);
			string isEditable = reader.GetAttribute (editableAttribute);
			if (!string.IsNullOrEmpty (isEditable))
				result.IsEditable = Boolean.Parse (isEditable);
			XmlReadHelper.ReadList (reader, Node, delegate () {
				//Console.WriteLine ("ctv:" +reader.LocalName);
				switch (reader.LocalName) {
				case DefaultNode:
					result.Default = reader.ReadElementContentAsString ();
					return true;
				case TooltipNode:
					result.ToolTip = reader.ReadElementContentAsString ();
					return true;
				case FunctionNode:
					result.Function = reader.ReadElementContentAsString ();
					return true;
				}
				return false;
			});
			//Console.WriteLine ("return:" + result);
			return result;
		}
	}
}
