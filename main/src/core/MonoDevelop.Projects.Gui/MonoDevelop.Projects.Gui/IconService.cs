//  IconService.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Resources;
using MonoDevelop.Projects.Parser;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Core.Gui;
using Stock = MonoDevelop.Core.Gui.Stock;

namespace MonoDevelop.Projects.Gui
{
	public class IconService : AbstractService {
		Hashtable extensionHashtable   = new Hashtable ();
		Hashtable projectFileHashtable = new Hashtable ();
		
		public override void InitializeService()
		{
			base.InitializeService();
			InitializeIcons ("/MonoDevelop/ProjectModel/Gui/Icons");
		}
		
		public string GetImageForProjectType (string projectType)
		{
			if (projectFileHashtable [projectType] != null)
				return (string) projectFileHashtable [projectType];
			
			return (string) extensionHashtable [".PRJX"];
		}
		
		public string GetImageForFile (string fileName)
		{
			string extension = Path.GetExtension (fileName).ToUpper ();
			
			if (extensionHashtable.Contains (extension))
				return (string) extensionHashtable [extension];
			
			return Stock.MiscFiles;
		}


		void InitializeIcons (string path)
		{			
			extensionHashtable[".PRJX"] = Stock.Project;
			extensionHashtable[".CMBX"] = Stock.Solution;
			extensionHashtable[".MDS"] = Stock.Solution;
			extensionHashtable[".MDP"] = Stock.Project;
		
			foreach (IconCodon iconCodon in AddinManager.GetExtensionNodes (path, typeof(IconCodon))) {
				string image;
				if (iconCodon.Resource != null)
					image = iconCodon.Resource;
				else
					image = iconCodon.Id;
				
				image = ResourceService.GetStockId (iconCodon.Addin, image);
				
				if (iconCodon.Extensions != null) {
					foreach (string ext in iconCodon.Extensions)
						extensionHashtable [ext.ToUpper()] = image;
				}
				if (iconCodon.Language != null)
					projectFileHashtable [iconCodon.Language] = image;
			}
		}
		
		string GetWithModifiers (ModifierEnum modifier, string mod_public, string mod_protected, string mod_internal, string mod_private)
		{
			if ((modifier & ModifierEnum.Public) == ModifierEnum.Public)
				return mod_public;
			
			if ((modifier & ModifierEnum.Protected) == ModifierEnum.Protected)
				return mod_protected;

			if ((modifier & ModifierEnum.Internal) == ModifierEnum.Internal)
				return mod_internal;
			
			return mod_private;
		}
		
		public string GetIcon (IMember member)
		{
			if (member is IMethod)
				return GetIcon ((IMethod) member);
			if (member is IProperty)
				return GetIcon ((IProperty) member);
			if (member is IField)
				return GetIcon ((IField) member);
			if (member is IEvent)
				return GetIcon ((IEvent) member);
			if (member is IIndexer)
				return GetIcon ((IIndexer) member);
			return null;
		}
		
		public string GetIcon (IMethod method)
		{
			return GetWithModifiers (method.Modifiers, Stock.Method, Stock.ProtectedMethod, Stock.InternalMethod, Stock.PrivateMethod);
		}
		
		public string GetIcon (IProperty method)
		{
			return GetWithModifiers (method.Modifiers, Stock.Property, Stock.ProtectedProperty, Stock.InternalProperty, Stock.PrivateProperty);
		}
		
		public string GetIcon (IField field)
		{
			if (field.IsLiteral)
				return Stock.Literal;
			
			return GetWithModifiers (field.Modifiers, Stock.Field, Stock.ProtectedField, Stock.InternalField, Stock.PrivateField);
		}
		
		public string GetIcon (IEvent evt)
		{
			return GetWithModifiers (evt.Modifiers, Stock.Event, Stock.ProtectedEvent, Stock.InternalEvent, Stock.PrivateEvent);
		}
		
		public string GetIcon (IClass c)
		{
			switch (c.ClassType) {
			case ClassType.Delegate:
				return GetWithModifiers (c.Modifiers, Stock.Delegate, Stock.ProtectedDelegate, Stock.InternalDelegate, Stock.PrivateDelegate);
			case ClassType.Enum:
				return GetWithModifiers (c.Modifiers, Stock.Enum, Stock.ProtectedEnum, Stock.InternalEnum, Stock.PrivateEnum);
			case ClassType.Struct:
				return GetWithModifiers (c.Modifiers, Stock.Struct, Stock.ProtectedStruct, Stock.InternalStruct, Stock.PrivateStruct);
			case ClassType.Interface:
				return GetWithModifiers (c.Modifiers, Stock.Interface, Stock.ProtectedInterface, Stock.InternalInterface, Stock.PrivateInterface);
			default:
				return GetWithModifiers (c.Modifiers, Stock.Class, Stock.ProtectedClass, Stock.InternalClass, Stock.PrivateClass);
			}
		}
		
		string GetWithModifiers (MethodBase mb, string mod_public, string mod_protected, string mod_internal, string mod_private)
		{
			if (mb.IsAssembly)
				return mod_internal;
			
			if (mb.IsPrivate)
				return mod_private;
			
			if (!(mb.IsPrivate || mb.IsPublic))
				return mod_protected;
			
			return mod_public;
		}
		
		public string GetIcon (MethodBase m)
		{
			return GetWithModifiers (m, Stock.Method, Stock.ProtectedMethod, Stock.InternalMethod, Stock.PrivateMethod);
		}
		
		public string GetIcon (PropertyInfo propertyinfo)
		{
			MethodBase m;
			if ((propertyinfo.CanRead  && (m = propertyinfo.GetGetMethod (true)) != null) || 
			    (propertyinfo.CanWrite && (m = propertyinfo.GetSetMethod (true)) != null))
				return GetWithModifiers (m, Stock.Property, Stock.ProtectedProperty, Stock.InternalProperty, Stock.PrivateProperty);
			
			return Stock.Property;
		}
		
		public string GetIcon (FieldInfo fieldinfo)
		{
			if (fieldinfo.IsLiteral)
				return Stock.Literal;
			
			if (fieldinfo.IsAssembly)
				return Stock.InternalField;
			
			if (fieldinfo.IsPrivate)
				return Stock.PrivateField;
			
			if (!(fieldinfo.IsPrivate || fieldinfo.IsPublic))
				return Stock.ProtectedField;
			
			return Stock.Field;
		}
				
		public string GetIcon(EventInfo eventinfo)
		{
			if (eventinfo.GetAddMethod (true) != null)
				return GetWithModifiers (eventinfo.GetAddMethod (true), Stock.Event, Stock.ProtectedEvent, Stock.InternalEvent, Stock.PrivateEvent);
			
			return Stock.Event;
		}
		
		public string GetIcon(System.Type type)
		{
			ModifierEnum mod;
			
			if (type.IsNestedPrivate)
				mod = ModifierEnum.Private;
			else if (type.IsNotPublic || type.IsNestedAssembly)
				mod = ModifierEnum.Internal;
			else if (type.IsNestedFamily)
				mod = ModifierEnum.Protected;
			else
				mod = ModifierEnum.Public;
			
			if (type.IsValueType)
				return GetWithModifiers (mod, Stock.Struct, Stock.ProtectedStruct, Stock.InternalStruct, Stock.PrivateStruct);
			
			if (type.IsEnum)
				return GetWithModifiers (mod, Stock.Enum, Stock.ProtectedEnum, Stock.InternalEnum, Stock.PrivateEnum);
			
			if (type.IsInterface)
				return GetWithModifiers (mod, Stock.Interface, Stock.ProtectedInterface, Stock.InternalInterface, Stock.PrivateInterface);
			
			if (type.IsSubclassOf (typeof (System.Delegate)))
				return GetWithModifiers (mod, Stock.Delegate, Stock.ProtectedDelegate, Stock.InternalDelegate, Stock.PrivateDelegate);
			
			return GetWithModifiers (mod, Stock.Class, Stock.ProtectedClass, Stock.InternalClass, Stock.PrivateClass);
		}
		
		public string GetIcon (IIndexer indexer)
		{
			return GetWithModifiers (indexer.Modifiers, Stock.Property, Stock.ProtectedProperty, Stock.InternalProperty, Stock.PrivateProperty);
		}
		
		public Gdk.Pixbuf MakeTransparent (Gdk.Pixbuf icon, double opacity)
		{
			// If somebody knows a better way of doing this, please redo.
			Gdk.Pixbuf gicon = icon.Copy ();
			gicon.Fill (0);
			gicon = gicon.AddAlpha (true,0,0,0);
			icon.Composite (gicon, 0, 0, icon.Width, icon.Height, 0, 0, 1, 1, Gdk.InterpType.Bilinear, (int)(256 * opacity));
			return gicon;
		}
	}
}
