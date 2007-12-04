//
// ClassUtils.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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

using Gtk;
using System;
using System.Collections;
using System.CodeDom;
using MonoDevelop.Projects.Parser;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	internal class ClassUtils
	{
		public static IField FindWidgetField (IClass cls, string name)
		{
			foreach (IField field in cls.Fields) {
				if (name == GetWidgetFieldName (field))
					return field;
			}
			return null;
		}
		
		public static string GetWidgetFieldName (IField field)
		{
			foreach (IAttributeSection asec in field.Attributes) {
				foreach (IAttribute att in asec.Attributes)	{
					if (att.Name == "Glade.Widget" || att.Name == "Widget" || att.Name == "Glade.WidgetAttribute" || att.Name == "WidgetAttribute") {
						if (att.PositionalArguments != null && att.PositionalArguments.Length > 0) {
							CodePrimitiveExpression exp = att.PositionalArguments [0] as CodePrimitiveExpression;
							if (exp != null)
								return exp.Value.ToString ();
						} else {
							return field.Name;
						}
					}
				}
			}
			return field.Name;
		}
	}
}
