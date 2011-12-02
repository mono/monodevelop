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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	internal class ClassUtils
	{
		public static IField FindWidgetField (ITypeDefinition cls, string name)
		{
			foreach (IField field in cls.Fields) {
				if (name == GetWidgetFieldName (field))
					return field;
			}
			return null;
		}
		
		public static string GetWidgetFieldName (IField field)
		{
			foreach (IAttribute att in field.Attributes)	{
				var type = att.AttributeType;
				if (type.ReflectionName == "Glade.Widget" || type.ReflectionName == "Widget" || type.ReflectionName == "Glade.WidgetAttribute" || type.ReflectionName == "WidgetAttribute") {
					var pArgs = att.PositionalArguments;
					if (pArgs != null && pArgs.Count > 0) {
						var exp = pArgs[0] as ConstantResolveResult;
						if (exp != null)
							return exp.ConstantValue.ToString ();
					} else {
						return field.Name;
					}
				}
			}
			return field.Name;
		}
	}
}
