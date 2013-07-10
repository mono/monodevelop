 /*
 * DefaultPropertyTab.cs - The default PropertyTab
 * 
 * Part of PropertyGrid - A Gtk# widget that displays and allows 
 * editing of all of an object's public properties 
 * 
 * Authors: 
 * 	Michael Hutchinson <m.j.hutchinson@gmail.com>
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

using Gtk;
using System;
using System.ComponentModel;
using MonoDevelop.Core;
 
namespace MonoDevelop.Components.PropertyGrid
{
	public class DefaultPropertyTab : PropertyTab
	{
		public DefaultPropertyTab ()
			: base ()
		{
		}
		
		public override string TabName {
			get {return GettextCatalog.GetString ("Properties"); }
		}
		
		public override bool CanExtend (object extendee)
		{
			return true;
		}
		
		public override PropertyDescriptor GetDefaultProperty (object component)
		{
			if (component == null)
				return null;
			return TypeDescriptor.GetDefaultProperty (component);			
		}
		
		public override PropertyDescriptorCollection GetProperties (object component, Attribute[] attributes)
		{
			if (component == null)
				return new PropertyDescriptorCollection (new PropertyDescriptor[] {});
			return TypeDescriptor.GetProperties (component);
		}
	}
	
	public abstract class PropertyTab
	{
		public abstract string TabName { get; }
		public abstract bool CanExtend (object extendee);
		public abstract PropertyDescriptor GetDefaultProperty (object component);
		public abstract PropertyDescriptorCollection GetProperties (object component, Attribute[] attributes);
		
		public PropertyDescriptorCollection GetProperties (object component)
		{
			return GetProperties (component, null);
		}
		
		public Xwt.Drawing.Image GetIcon ()
		{
			using (var stream = GetType ().Assembly.GetManifestResourceStream (GetType ().FullName + ".bmp")) {
				if (stream != null) {
					try {
						return new Gdk.Pixbuf (stream).ToXwtImage ();
					} catch (Exception e) {
						LoggingService.LogError ("Can't create pixbuf from resource:" + GetType ().FullName + ".bmp", e);
					}
				}
			}
			return null;
		}
	}
}
