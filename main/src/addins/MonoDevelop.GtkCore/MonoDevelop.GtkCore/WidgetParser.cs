//
// ObjectsDocument.cs
//
// Authors:
//   Lluis Sanchez Gual
//   Mike Kestner
//
// Copyright (C) 2006-2008 Novell, Inc (http://www.novell.com)
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
using System.Xml;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using MonoDevelop.Projects.Parser;

namespace MonoDevelop.GtkCore
{

	public class WidgetParser
	{

		IParserContext ctx;

		public WidgetParser (IParserContext ctx)
		{
			this.ctx = ctx;
		}
		
		public Dictionary<string, IClass> ToolboxItems {
			get {
				Dictionary<string, IClass> tb_items = new Dictionary<string, IClass> ();
				foreach (IClass cls in ctx.GetProjectContents ())
					if (IsToolboxWidget (cls))
						tb_items [cls.FullyQualifiedName] = cls;
				return tb_items;
			}
		}

		public void CollectMembers (IClass cls, bool inherited, string topType, ListDictionary properties, ListDictionary events)
		{
			if (cls.FullyQualifiedName == topType)
				return;

			foreach (IProperty prop in cls.Properties)
				if (IsBrowsable (prop))
					properties [prop.Name] = prop;

			foreach (IEvent ev in cls.Events)
				if (IsBrowsable (ev))
					events [ev.Name] = ev;
					
			if (inherited) {
				foreach (IReturnType bt in cls.BaseTypes) {
					IClass bcls = ctx.GetClass (bt.FullyQualifiedName, true, true);
					if (bcls != null && bcls.ClassType != ClassType.Interface)
						CollectMembers (bcls, true, topType, properties, events);
				}
			}
		}
		
		public string GetBaseType (IClass cls, Hashtable knownTypes)
		{
			foreach (IReturnType bt in cls.BaseTypes) {
				if (knownTypes.Contains (bt.FullyQualifiedName))
					return bt.FullyQualifiedName;
			}

			foreach (IReturnType bt in cls.BaseTypes) {
				IClass bcls = ctx.GetClass (bt.FullyQualifiedName, true, true);
				if (bcls != null) {
					string ret = GetBaseType (bcls, knownTypes);
					if (ret != null)
						return ret;
				}
			}
			return null;
		}
		
		public string GetCategory (IDecoration decoration)
		{
			foreach (IAttributeSection section in decoration.Attributes) {
				foreach (IAttribute at in section.Attributes) {
					switch (at.Name) {
					case "Category":
					case "CategoryAttribute":
					case "System.ComponentModel.Category":
					case "System.ComponentModel.CategoryAttribute":
						break;
					default:
						continue;
					}
					if (at.PositionalArguments != null && at.PositionalArguments.Length > 0) {
						CodePrimitiveExpression exp = at.PositionalArguments [0] as CodePrimitiveExpression;
						if (exp != null && exp.Value != null)
							return exp.Value.ToString ();
					}
				}
			}
			return "";
		}
		
		public IClass GetClass (string classname)
		{
			return ctx.GetClass (classname);
		}

		public bool IsBrowsable (IMember member)
		{
			if (!member.IsPublic)
				return false;

			IProperty prop = member as IProperty;
			if (prop != null) {
				if (!prop.CanGet || !prop.CanSet)
					return false;
				if (Array.IndexOf (supported_types, prop.ReturnType.FullyQualifiedName) == -1)
					return false;
			}

			foreach (IAttributeSection section in member.Attributes) {
				foreach (IAttribute at in section.Attributes) {
					switch (at.Name) {
					case "Browsable":
					case "BrowsableAttribute":
					case "System.ComponentModel.Browsable":
					case "System.ComponentModel.BrowsableAttribute":
						break;
					default:
						continue;
					}
					if (at.PositionalArguments != null && at.PositionalArguments.Length > 0) {
						CodePrimitiveExpression exp = at.PositionalArguments [0] as CodePrimitiveExpression;
						if (exp != null && exp.Value != null && exp.Value is bool) {
							return (bool) exp.Value;
						}
					}
				}
			}
			return true;
		}
		
		public bool IsToolboxWidget (IClass cls)
		{
			if (!cls.IsPublic || !IsWidget (ctx, cls))
				return false;

			foreach (IAttributeSection section in cls.Attributes) {
				foreach (IAttribute at in section.Attributes) {
					switch (at.Name) {
					case "ToolboxItem":
					case "ToolboxItemAttribute":
					case "System.ComponentModel.ToolboxItem":
					case "System.ComponentModel.ToolboxItemAttribute":
						break;
					default:
						continue;
					}
					if (at.PositionalArguments != null && at.PositionalArguments.Length > 0) {
						CodePrimitiveExpression exp = at.PositionalArguments [0] as CodePrimitiveExpression;
						if (exp == null || exp.Value == null)
							return false;
						else if (exp.Value is bool)
							return (bool) exp.Value;
						else 
							return exp.Value != null;
					}
				}
			}

			foreach (IReturnType bt in cls.BaseTypes) {
				IClass bcls = ctx.GetClass (bt.FullyQualifiedName, true, true);
				if (bcls != null && bcls.ClassType != ClassType.Interface)
					return IsToolboxWidget (bcls);
			}

			return false;
		}
		
		bool IsWidget (IParserContext ctx, IClass cls)
		{
			foreach (IReturnType bt in cls.BaseTypes) {
				if (bt.FullyQualifiedName == "Gtk.Widget")
					return true;
				IClass bcls = ctx.GetClass (bt.FullyQualifiedName, true, true);
				if (bcls != null && bcls.ClassType != ClassType.Interface)
					return IsWidget (ctx, bcls);
			}
			return false;
		}

		static string[] supported_types = new string[] {
			"System.Boolean",
			"System.Char",
			"System.SByte",
			"System.Byte",
			"System.Int16",
			"System.UInt16",
			"System.Int32",
			"System.UInt32",
			"System.Int64",
			"System.UInt64",
			"System.Decimal",
			"System.Single",
			"System.Double",
			"System.DateTime",
			"System.String",
			"System.TimeSpan",
			"Gtk.Adjustment",
		};
	}	
}
