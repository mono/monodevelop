//
// PackageBuilderEditor.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Deployment;
using MonoDevelop.Projects;

namespace MonoDevelop.Deployment.Gui
{
	public class PackageBuilderEditor: HBox
	{
		public PackageBuilderEditor (PackageBuilder target, CombineEntry entry)
		{
			try {
				IPackageBuilderEditor editor = GetEditor (target, entry);
				if (editor != null)
					PackStart (editor.CreateEditor (target, entry), true, true, 0);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				PackStart (new Gtk.Label ("Could not create editor for deploy target of type " + target));
			}
			ShowAll ();
		}
		
		public static bool HasEditor (PackageBuilder target, CombineEntry entry)
		{
			return GetEditor (target, entry) != null;
		}
		
		static IPackageBuilderEditor GetEditor (PackageBuilder builder, CombineEntry entry)
		{
			foreach (IPackageBuilderEditor editor in Runtime.AddInService.GetTreeItems ("/MonoDevelop/DeployService/PackageBuilderEditors")) {
				if (editor.CanEdit (builder, entry))
					return editor;
			}
			return null;
		}
	}
}
