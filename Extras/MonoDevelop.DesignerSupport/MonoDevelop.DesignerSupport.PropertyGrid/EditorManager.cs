/* 
 * EditorManager.cs - Used to register, lookup and select visual editors.
 * 
 * Part of PropertyGrid - A Gtk# widget that displays and allows 
 * editing of all of an object's public properties 
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
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
using AspNetEdit.UI.PropertyEditors;
using System.Drawing.Design;

namespace AspNetEdit.UI
{
	internal class EditorManager
	{
		private Hashtable editors = new Hashtable ();
		private Hashtable inheritingEditors = new Hashtable ();
		private Hashtable surrogates = new Hashtable ();

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
						if (t.IsSubclassOf (typeof (BaseEditor)))
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

		public BaseEditor GetEditor(PropertyDescriptor pd, GridRow parentRow)
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
				return instantiateEditor ((Type) editors[editType], parentRow);
			
			//editors that edit derived types
			foreach (DictionaryEntry de in inheritingEditors)
				if (editType.IsSubclassOf((Type) de.Key))
					return instantiateEditor ((Type) de.Value, parentRow);
				
			//special cases
			if (editType.IsEnum)
				return new EnumEditor (parentRow);

			//collections with items of single type that aren't just objects
			if(editType.GetInterface ("IList") != null) {
				PropertyInfo member = editType.GetProperty ("Item");
				if (member != null)
					if (member.PropertyType != typeof (object))
						return new CollectionEditor (parentRow, member.PropertyType);
			}
			//TODO: support simple SWF collection editor derivatives that just override Types available
			// and reflect protected Type[] NewItemTypes {get;} to get types
			//if (UITypeEd is System.ComponentModel.Design.CollectionEditor)
			//	((System.ComponentModel.Design.CollectionEditor)UITypeEd).

			//can we use a type converter with a built-in editor?
			TypeConverter tc = pd.Converter;
			
			//TODO: build this functionality into the grid 
			if (tc.GetType () == typeof (ExpandableObjectConverter)) {
				return new ExpandableObjectEditor (parentRow);
			}

			//This is a temporary workaround *and* and optimisation
			//First, most unknown types will be most likely to convert to/from strings
			//Second, System.Web.UI.WebControls/UnitConverter.cs dies on non-strings
			if (tc.CanConvertFrom (typeof (string)) && tc.CanConvertTo (typeof(string)))
				return new StringEditor (parentRow);
			
			foreach (DictionaryEntry editor in editors)
				if (tc.CanConvertFrom((Type) editor.Key) && tc.CanConvertTo((Type) editor.Key))
					return instantiateEditor((Type) editor.Value, parentRow);
					
			foreach (DictionaryEntry de in inheritingEditors)
				if (tc.CanConvertFrom((Type) de.Key) && tc.CanConvertTo((Type) de.Key))
					return instantiateEditor((Type) de.Value, parentRow);

			//nothing found - just display type
			return new DefaultEditor (parentRow);
		}

		private BaseEditor instantiateEditor(Type type, GridRow parentRow)
		{
			ConstructorInfo ctor = type.GetConstructor( new Type[] { typeof (GridRow) });
			return (BaseEditor) ctor.Invoke(new object[] { parentRow });
		}
	}
}
