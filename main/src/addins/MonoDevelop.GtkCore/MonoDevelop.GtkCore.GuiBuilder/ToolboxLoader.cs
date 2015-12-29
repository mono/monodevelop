//
// ToolboxLoader.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using Stetic;
using MonoDevelop.Ide;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class ToolboxLoader: IToolboxLoader
	{
		public virtual string[] FileTypes {
			get { return new string [] { "dll", "exe" }; }
		}

		public virtual IList<ItemToolboxNode> Load (LoaderContext ctx, string filename)
		{
			SystemPackage sp = Runtime.SystemAssemblyService.DefaultAssemblyContext.GetPackageFromPath (filename);
			ReferenceType rt;
			string rname;
			
			if (sp != null) {
				rt = ReferenceType.Package;
				rname = Runtime.SystemAssemblyService.DefaultAssemblyContext.GetAssemblyFullName (filename, null);
			} else {
				rt = ReferenceType.Assembly;
				rname = filename;
			}
			
			List<ItemToolboxNode> list = new List<ItemToolboxNode> ();
			var types = Runtime.RunInMainThread (delegate {
				// Stetic is not thread safe, it has to be used from the gui thread
				return GuiBuilderService.SteticApp.GetComponentTypes (filename);
			}).Result;
			foreach (ComponentType ct in types) {
				if (ct.Category == "window")
					continue;
				ComponentToolboxNode cn = new ComponentToolboxNode (ct);
				cn.ReferenceType = rt;
				cn.Reference = rname;
				list.Add (cn);
			}
			return list;
		}
	}
}
