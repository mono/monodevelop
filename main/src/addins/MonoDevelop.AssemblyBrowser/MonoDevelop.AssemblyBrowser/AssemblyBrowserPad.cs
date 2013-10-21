//
// AssemblyBrowserPad.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml;
using Gtk;

using Mono.Cecil;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;
using System.Linq;
using Mono.TextEditor;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.Documentation;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Projects;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using Mono.TextEditor.Theatrics;
using MonoDevelop.SourceEditor;
using XmlDocIdLib;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.AssemblyBrowser
{
	class AssemblyBrowserPad : AbstractPadContent
	{
		readonly AssemblyBrowserWidget widget = new AssemblyBrowserWidget ();
		public AssemblyBrowserWidget AssemblyBrowserWidget { 
			get {
				return widget;
			}
		}
		#region implemented abstract members of AbstractPadContent
		public override Gtk.Widget Control {
			get {
				return widget;
				// return (Gtk.Widget)Toolkit.CurrentEngine.GetNativeWidget (widget);
			}
		}
		#endregion
	}
}

