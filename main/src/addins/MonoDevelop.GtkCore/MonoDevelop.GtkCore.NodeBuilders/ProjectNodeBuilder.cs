//
// ProjectNodeBuilder.cs
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
using System.Collections;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;

using MonoDevelop.GtkCore.GuiBuilder;

namespace MonoDevelop.GtkCore.NodeBuilders
{
	public class ProjectNodeBuilder: NodeBuilderExtension
	{
		static ProjectNodeBuilder instance;

		public override bool CanBuildNode (Type dataType)
		{
			return typeof(DotNetProject).IsAssignableFrom (dataType);
		}
		
		protected override void Initialize ()
		{
			base.Initialize ();

			lock (typeof (ProjectNodeBuilder))
				instance = this;
		}
		
		public override void Dispose ()
		{
			lock (typeof (ProjectNodeBuilder))
				instance = null;

			base.Dispose ();
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			if (GtkDesignInfo.HasDesignedObjects ((Project)dataObject))
				builder.AddChild (new WindowsFolder ((Project)dataObject));
		}
		
		public static void OnSupportChanged (Project p)
		{
			if (instance == null)
				return;

			ITreeBuilder tb = instance.Context.GetTreeBuilder (p);
			if (tb != null)
				tb.UpdateAll ();
		}
	}
}
