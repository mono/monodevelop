/* 
 * SurrogateUITypeEditorAttribute.cs - Marks a GTK# Visual Editor as a substitute
 *	for a particular System.Drawing.Design.UITypeEditor-derived SWF editor. 
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

namespace MonoDevelop.Components.PropertyGrid
{

	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SurrogateUITypeEditorAttribute : Attribute
	{
		public Type Type;
		public SurrogateUITypeEditorAttribute (Type myType)
		{
			this.Type = myType; 
		}
	}

	/* TODO: Surrogates for...
	 * 
	 * System.Drawing.Design.FontEditor
	 * System.Drawing.Design.ImageEditor
	 * System.Web.UI.Design.DataBindingCollectionEditor
	 * System.Web.UI.Design.UrlEditor
	 * System.Web.UI.Design.WebControls.DataGridColumnCollectionEditor
	 * System.Web.UI.Design.WebControls.RegexTypeEditor
	 * System.Web.UI.Design.XmlFileEditor
	 * System.Web.UI.Design.TreeNodeCollectionEditor *STUPID: isn't based on CollectionEditor*
	 */
}
