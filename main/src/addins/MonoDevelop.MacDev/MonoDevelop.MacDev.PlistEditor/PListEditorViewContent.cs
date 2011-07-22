// 
// PListEditorViewContent.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoMac.Foundation;	

namespace MonoDevelop.MacDev.PlistEditor
{
	public class PListEditorViewContent : AbstractViewContent
	{
		PDictionary dict;
		Gtk.Widget widget;
		
		public override Gtk.Widget Control { get { return widget; } }
		
		public PListEditorViewContent (IPlistEditingHandler handler, Project proj)
		{
			dict = new PDictionary ();
			dict.Changed += (sender, e) => IsDirty = true;
			if (handler != null) {
				widget =  new PListEditorWidget (handler, proj, dict);
			} else {
				widget = new CustomPropertiesWidget () { NSDictionary = dict };
			}
		}
		
		public override void Load (string fileName)
		{
			ContentName = fileName;
			dict.Reload (fileName);
			this.IsDirty = false;
		}
		
		public override void Save (string fileName)
		{
			this.IsDirty = false;
			ContentName = fileName;
			try {
				dict.Save (fileName);
			} catch (Exception e) {
				MessageService.ShowException (e, GettextCatalog.GetString ("Error while writing plist"));
			}
		}
	}
}