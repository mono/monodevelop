// 
// NativeReferenceNodeBuilder.cs
//  
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2011 Michael Hutchinson
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
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.MacDev.NativeReferences
{
	class NativeReferenceNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType 
		{
			get { return typeof (NativeReference); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "NativeReference";
		}
		
		public override string ContextMenuAddinPath {
			get {
				return "/MonoDevelop/MacDev/ContextMenu/ProjectPad/NativeReference";
			}
		}
		
		public override Type CommandHandlerType
		{
			get { return typeof (NativeReferenceCommandHandler); }
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label,
			ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			var reference = (NativeReference) dataObject;
			label = reference.Path.FileNameWithoutExtension;
			//TODO: better icons
			icon = Context.GetIcon (Stock.Reference);
			closedIcon = Context.GetIcon (Stock.Reference);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return false;
		}
	}
}