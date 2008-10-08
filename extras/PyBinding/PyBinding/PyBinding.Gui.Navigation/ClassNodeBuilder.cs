// ClassNodeBuilder.cs
//
// Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
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
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

using PyBinding;
using PyBinding.Parser;
using PyBinding.Parser.Dom;

namespace PyBinding.Gui.Navigation
{
	public class ClassNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get {
				return typeof (PythonClass);
			}
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			PythonClass pyClass = dataObject as PythonClass;
			return pyClass.Name;
		}

		public override void BuildNode (ITreeBuilder treeBuilder,
		                                object dataObject,
		                                ref string label,
		                                ref Gdk.Pixbuf icon,
		                                ref Gdk.Pixbuf closedIcon)
		{
			PythonClass pyClass = dataObject as PythonClass;

			label = pyClass.Name;
			icon = Context.GetIcon (Stock.Class);
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder,
		                                      object       dataObject)
		{
			PythonClass pyClass = dataObject as PythonClass;

			if (pyClass == null)
				return;

			foreach (PythonAttribute pyAttr in pyClass.Attributes)
				treeBuilder.AddChild (pyAttr);

			foreach (PythonFunction pyFunc in pyClass.Functions)
				treeBuilder.AddChild (pyFunc);
		}

		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			PythonNode x = thisNode.DataItem as PythonNode;
			PythonNode y = otherNode.DataItem as PythonNode;

			if (x == null || y == null)
				return 1;

			return x.Region.Start.Line.CompareTo (y.Region.Start.Line);
		}
	}
}
