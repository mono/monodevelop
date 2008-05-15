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
using System.Collections;
using System.Reflection;
using System.ComponentModel;
using MonoDevelop.DesignerSupport.PropertyGrid.PropertyEditors;
using System.Drawing.Design;

namespace MonoDevelop.DesignerSupport.PropertyGrid
{
	internal class EditorManager
	{
		private Hashtable editors = new Hashtable ();
		private Hashtable inheritingEditors = new Hashtable ();
		private Hashtable surrogates = new Hashtable ();
		static PropertyEditorCell Default = new PropertyEditorCell ();
		static Hashtable cellCache = new Hashtable ();

		internal EditorManager ()
		{
			LoadEditor (Assembly.GetAssembly (typeof (EditorManager)));
		}

		public void LoadEditor (Assembly editorAssembly)
		{
			foreach (Type t in editorAssembly.GetTypes ()) {
				foreach (Attribute currentAttribute in Attribute.GetCustomAttributes (t)) {
					if (currentAttribute.GetType() == typeof (PropertyEditorTypeAttribute)) {
						PropertyEditorTypeAttribute peta = (PropertyEditorTypeAttribute)currentAttribute;
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

		public PropertyEditorCell GetEditor (PropertyDescriptor pd)
		{
			PropertyEditorCell cell = pd.GetEditor (typeof(PropertyEditorCell)) as PropertyEditorCell;
			if (cell != null)
				return cell;
			
			Type editorType = GetEditorType (pd);
			if (editorType == null)
				return Default;
			
			if (typeof(IPropertyEditor).IsAssignableFrom (editorType)) {
				if (!typeof(Gtk.Widget).IsAssignableFrom (editorType))
					throw new Exception ("The property editor '" + editorType + "' must be a Gtk Widget");
				return Default;
			}

			cell = cellCache [editorType] as PropertyEditorCell;
			if (cell != null)
				return cell;

			if (!typeof(PropertyEditorCell).IsAssignableFrom (editorType))
				throw new Exception ("The property editor '" + editorType + "' must be a subclass of Stetic.PropertyEditorCell or implement Stetic.IPropertyEditor");

			cell = (PropertyEditorCell) Activator.CreateInstance (editorType);
			cellCache [editorType] = cell;
			return cell;
		}
		
		public Type GetEditorType (PropertyDescriptor pd)
		{
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
			if (editors.Contains (editType))
				return (Type) editors [editType];
			
			//editors that edit derived types
			foreach (DictionaryEntry de in inheritingEditors)
				if (editType.IsSubclassOf((Type) de.Key))
					return (Type) de.Value;
			
			if (pd.PropertyType.IsEnum) {
				if (pd.PropertyType.IsDefined (typeof (FlagsAttribute), true))
					return typeof (PropertyEditors.FlagsEditorCell);
				else
					return typeof (PropertyEditors.EnumerationEditorCell);
			}
			
			//collections with items of single type that aren't just objects
			if (typeof(IList).IsAssignableFrom (editType)) {
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
			
			if (typeof (ExpandableObjectConverter).IsAssignableFrom (tc.GetType ()))
				return typeof(ExpandableObjectEditor);

			//This is a temporary workaround *and* and optimisation
			//First, most unknown types will be most likely to convert to/from strings
			//Second, System.Web.UI.WebControls/UnitConverter.cs dies on non-strings
			if (tc.CanConvertFrom (typeof (string)) && tc.CanConvertTo (typeof(string)))
				return typeof(TextEditor);
			
			foreach (DictionaryEntry editor in editors)
				if (tc.CanConvertFrom((Type) editor.Key) && tc.CanConvertTo((Type) editor.Key))
					return (Type) editor.Value;
					
			foreach (DictionaryEntry de in inheritingEditors)
				if (tc.CanConvertFrom((Type) de.Key) && tc.CanConvertTo((Type) de.Key))
					return (Type) de.Value;

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
