// 
// PixbufVisualizer.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

using System;
using System.IO;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Visualizer
{
	public class PixbufVisualizer: IValueVisualizer
	{
		#region IValueVisualizer implementation
		public bool CanVisualize (ObjectValue val)
		{
			return val.TypeName == "Gdk.Pixbuf";
		}
		
		public bool CanEdit (ObjectValue val)
		{
			return false;
		}
		
		public Gtk.Widget GetVisualizerWidget (ObjectValue val)
		{
			Gdk.Pixbuf pixbuf;
			string file = Path.GetTempFileName ();
			try {
				RawValue pix = (RawValue) val.GetRawValue ();
				pix.CallMethod ("Save", file, "png");
				pixbuf = new Gdk.Pixbuf (file);
			} finally {
				File.Delete (file);
			}
			Gtk.ScrolledWindow sc = new Gtk.ScrolledWindow ();
			sc.ShadowType = Gtk.ShadowType.In;
			sc.HscrollbarPolicy = Gtk.PolicyType.Automatic;
			sc.VscrollbarPolicy = Gtk.PolicyType.Automatic;
			Gtk.Image image = new Gtk.Image (pixbuf);
			sc.AddWithViewport (image);
			sc.ShowAll ();
			return sc;
		}
		
		
		public bool StoreValue (ObjectValue val)
		{
			return true;
		}
		
		
		public string Name {
			get {
				return "Pixbuf";
			}
		}
		
		#endregion
	}
}

