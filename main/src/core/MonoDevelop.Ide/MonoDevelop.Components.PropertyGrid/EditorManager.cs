/* 
 * EditorManager.cs - Used to register, lookup and select visual editors.
 * 
 * Part of PropertyGrid - A Gtk# widget that displays and allows 
 * editing of all of an object's public properties 
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  Lluis Sanchez Gual
 *  
 * Copyright (C) 2005 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using MonoDevelop.Components.PropertyGrid.PropertyEditors;

namespace MonoDevelop.Components.PropertyGrid
{
	class EditorManager
	{
		readonly Dictionary<Type,Type> editors = new Dictionary<Type,Type> ();
		readonly Dictionary<Type,Type> inheritingEditors = new Dictionary<Type, Type>();
		readonly Dictionary<Type,Type> surrogates = new Dictionary<Type,Type> ();
		static readonly PropertyEditorCell Default = new PropertyEditorCell ();
		static readonly Dictionary<Type,PropertyEditorCell> cellCache = new Dictionary<Type,PropertyEditorCell> ();

		public EditorManager ()
		{
			LoadEditor (Assembly.GetAssembly (typeof (EditorManager)));
		}

		public void LoadEditor (Assembly editorAssembly)
		{
			foreach (Type t in editorAssembly.GetTypes ()) {
				foreach (Attribute currentAttribute in Attribute.GetCustomAttributes (t)) {
					if (currentAttribute.GetType() == typeof (PropertyEditorTypeAttribute)) {
						var peta = (PropertyEditorTypeAttribute)currentAttribute;
						Type editsType = peta.Type;
						if (t.IsSubclassOf (typeof (PropertyEditorCell)))
							if (peta.Inherits)
								inheritingEditors.Add (editsType, t);
							else
								editors.Add (editsType, t);
					}
					else if (currentAttribute.GetType () == typeof (SurrogateUITypeEditorAttribute)) {
						Type editsType = (currentAttribute as SurrogateUITypeEditorAttribute).Type;
						surrogates.Add (editsType, t);
					}
				}
			}
		}

		public PropertyEditorCell GetEditor (ITypeDescriptorContext context)
		{
			var cell = context.PropertyDescriptor.GetEditor (typeof(PropertyEditorCell)) as PropertyEditorCell;
			if (cell != null)
				return cell;
			
			Type editorType = GetEditorType (context);
			if (editorType == null)
				return Default;
			
			if (typeof(IPropertyEditor).IsAssignableFrom (editorType)) {
				if (!typeof(Gtk.Widget).IsAssignableFrom (editorType))
					throw new Exception ("The property editor '" + editorType + "' must be a Gtk Widget");
				return Default;
			}

			if (cellCache.TryGetValue (editorType, out cell)) {
				return cell;
			}

			if (!typeof(PropertyEditorCell).IsAssignableFrom (editorType))
				throw new Exception ("The property editor '" + editorType + "' must be a subclass of Stetic.PropertyEditorCell or implement Stetic.IPropertyEditor");

			cell = (PropertyEditorCell) Activator.CreateInstance (editorType);
			cellCache [editorType] = cell;
			return cell;
		}

		public Type GetEditorType (ITypeDescriptorContext context)
		{
			var pd = context.PropertyDescriptor;

			//try to find a custom editor
			//TODO: Find a way to provide a IWindowsFormsEditorService so this can work directly
			//for now, substitute GTK#-based editors
			/*
			UITypeEditor UITypeEd = (UITypeEditor) pd.GetEditor(typeof (System.Drawing.Design.UITypeEditor));//first, does it have custom editors?
			if (UITypeEd != null)
				if (surrogates.Contains(UITypeEd.GetType ()))
					return instantiateEditor((Type) surrogates[UITypeEd.GetType()], parentRow);
			*/

			//does a registered GTK# editor support this natively?
			Type editType = pd.PropertyType;
			if (editors.ContainsKey (editType))
				return editors [editType];
			
			//editors that edit derived types
			//TODO: find most derived type?
			foreach (var kvp in inheritingEditors)
				if (editType.IsSubclassOf (kvp.Key))
					return kvp.Value;

			if (pd.PropertyType.IsEnum) {
				if (pd.PropertyType.IsDefined (typeof(FlagsAttribute), true))
					return typeof(FlagsEditorCell);
				return typeof(EnumerationEditorCell);
			}
			
			//collections with items of single type that aren't just objects
			if (typeof(System.Collections.IList).IsAssignableFrom (editType)) {
				// Iterate through all properties since there may be more than one indexer.
				if (GetCollectionItemType (editType) != null)
					return typeof (CollectionEditor);
			}
			
			//TODO: support simple SWF collection editor derivatives that just override Types available
			// and reflect protected Type[] NewItemTypes {get;} to get types
			//if (UITypeEd is System.ComponentModel.Design.CollectionEditor)
			//	((System.ComponentModel.Design.CollectionEditor)UITypeEd).

			//can we use a type converter with a built-in editor?
			TypeConverter tc = pd.Converter;

			//TODO: find best match, not first
			foreach (var kvp in editors)
				if (tc.CanConvertFrom (kvp.Key) && tc.CanConvertTo (kvp.Key))
					return kvp.Value;
					
			foreach (var kvp in inheritingEditors)
				if (tc.CanConvertFrom (kvp.Key) && tc.CanConvertTo (kvp.Key))
					return kvp.Value;

			if (tc.CanConvertTo (typeof(string)) || tc.GetStandardValuesSupported (context)) {
				return typeof(TextEditor);
			}

			//nothing found - just display type
			return null;
		}
		
		public static Type GetCollectionItemType (Type colType)
		{
			foreach (PropertyInfo member in colType.GetProperties ()) {
				if (member.Name == "Item") {
					if (member.PropertyType != typeof (object))
						return member.PropertyType;
				}
			}
			return null;
		}
	}
}
