// GladeWidgetExtract.cs
//
// Author:
//   Ben Maurer  <bmaurer@users.sourceforge.net>
//
// Copyright (c) 2004 Ben Maurer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//


//
// Makes a widget from a glade file. Does reparenting.
// This is nice because you can use it for things like
// option dialogs.
//


using System;
using Gtk;
using Glade;

using Assembly = System.Reflection.Assembly;

namespace MonoDevelop.Components {
	public abstract class GladeWidgetExtract : HBox {
		
		Glade.XML glade;
		string dialog_name;
		
		private GladeWidgetExtract (string dialog_name) : base (false, 0)
		{
			this.dialog_name = dialog_name;
		}
		
		protected GladeWidgetExtract (string resource_name, string dialog_name) : this (dialog_name)
		{
			// we must do it from *here* otherwise, we get this assembly, not the caller
			glade = new XML (Assembly.GetCallingAssembly (), resource_name, dialog_name, null);
			Init ();
		}
		
		
		protected GladeWidgetExtract (Assembly assembly, string resource_name, string dialog_name) : this (dialog_name)
		{
			// we must do it from *here* otherwise, we get this assembly, not the caller
			glade = new XML (assembly, resource_name, dialog_name, null);
			Init ();
		}
		
		protected GladeWidgetExtract (string resource_name, string dialog_name, string domain) : this (dialog_name)
		{
			// we must do it from *here* otherwise, we get this assembly, not the caller
			glade = new XML (resource_name, dialog_name, domain);
			Init ();
		}
		
		void Init ()
		{
			glade.Autoconnect (this);
			
			Window win = (Window) glade [dialog_name];
			Widget child = win.Child;
			
			child.Reparent (this);
			win.Destroy ();
		}
	}
}
