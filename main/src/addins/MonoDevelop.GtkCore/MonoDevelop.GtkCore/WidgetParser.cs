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
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis;
using System.Linq;
using ICSharpCode.NRefactory6.CSharp;

namespace MonoDevelop.GtkCore
{

	public class WidgetParser
	{
		Compilation ctx;

		public Compilation Ctx {
			get {
				return ctx;
			}
		}
		
		public WidgetParser (Compilation ctx)
		{
			this.ctx = ctx;
		}
		
		static bool IsWidget(INamedTypeSymbol type)
		{
			if (type.SpecialType == SpecialType.System_Object)
				return false;
			if (type.GetFullName () == "Gtk.Widget")
				return true;
			
			return IsWidget (type.BaseType);
		}
		
		public Dictionary<string, INamedTypeSymbol> GetToolboxItems ()
		{
			var tb_items = new Dictionary<string, INamedTypeSymbol> ();

			foreach (var t in ctx.GetAllTypesInMainAssembly ()) {
				if (t.IsToolboxItem() && IsWidget(t))
					tb_items [t.GetFullName ()] = t;
			}
			
			return tb_items;
		}

		public void CollectMembers (ITypeSymbol cls, bool inherited, string topType, ListDictionary properties, ListDictionary events)
		{
			if (cls.GetFullName () == topType)
				return;

			foreach (var prop in cls.GetMembers ().OfType<IPropertySymbol> ())
				if (IsBrowsable (prop))
					properties [prop.Name] = prop;

			foreach (var ev in cls.GetMembers ().OfType<IEventSymbol> ())
				if (IsBrowsable (ev))
					events [ev.Name] = ev;
					
			if (inherited) {
				CollectMembers (cls.BaseType, true, topType, properties, events);
			}
		}
		
		public string GetBaseType (ITypeSymbol cls, Hashtable knownTypes)
		{
			if (cls.SpecialType == SpecialType.System_Object)
				return null;
			if (knownTypes.Contains (cls.BaseType.GetFullName ()))
				return cls.BaseType.GetFullName ();
			return GetBaseType (cls.BaseType, knownTypes);
		}
		
		
		public INamedTypeSymbol GetClass (string classname)
		{
			return ctx.GetTypeByMetadataName (classname);
		}

		public bool IsBrowsable (ISymbol member)
		{
			if (member.DeclaredAccessibility != Accessibility.Public)
				return false;

			var prop = member as IPropertySymbol;
			if (prop != null) {
				if (prop.GetMethod == null || prop.SetMethod == null)
					return false;
				if (Array.IndexOf (supported_types, prop.Type.GetFullName ()) == -1)
					return false;
			}

			return member.IsDesignerBrowsable ();
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
