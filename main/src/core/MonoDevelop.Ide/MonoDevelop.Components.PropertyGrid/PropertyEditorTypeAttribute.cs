/* 
 * PropertyTypeAttribute.cs - Shows which types a visual type editor can edit
 * 
 * Part of PropertyGrid - A Gtk# widget that displays and allows 
 * editing of all of an object's public properties 
 * 
 * Authors: 
 * 	Eric Butler <eric@extremeboredom.net>
 *  
 * Copyright (C) 2005 Eric Butler
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
	public class PropertyEditorTypeAttribute : Attribute
	{
		private Type type;
		private bool inherits = false;
		
		public PropertyEditorTypeAttribute (Type type)
		{
			this.type = type;
		}
		
		public PropertyEditorTypeAttribute (Type myType, bool inherits)
		{
			this.type = myType;
			this.inherits = inherits;
		}
		
		public bool Inherits {
			get { return inherits; }
		}
		
		public Type Type {
			get {return type; }
		}
	}
}
