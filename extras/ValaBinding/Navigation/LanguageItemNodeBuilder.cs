//
// LanguageItemNodeBuilder.cs
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2009 Levi Bard
//
// This source code is licenced under The MIT License:
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
using System.Threading;

using Mono.Addins;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
 
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;

using MonoDevelop.ValaBinding.Parser;
using MonoDevelop.ValaBinding.Parser.Afrodite;

namespace MonoDevelop.ValaBinding.Navigation
{
	/// <summary>
	/// Class pad node builder for all Vala language items
	/// </summary>
	public class LanguageItemNodeBuilder: TypeNodeBuilder
	{
		//// <value>
		/// Sort order for nodes
		/// </value>
		private static string[] types = { "namespace", "class", "struct", "interface", "property", "method", "signal", "field", "constant", "enum", "other" };
		
		public override Type NodeDataType {
			get { return typeof(Symbol); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(LanguageItemCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((Symbol)dataObject).Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder,
		                                object dataObject,
		                                ref string label,
		                                ref Gdk.Pixbuf icon,
		                                ref Gdk.Pixbuf closedIcon)
		{
			Symbol c = (Symbol)dataObject;
			label = c.DisplayText;
			icon = Context.GetIcon (c.Icon);
		}
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			// bool publicOnly = treeBuilder.Options["PublicApiOnly"];
			Symbol thisSymbol = (Symbol)dataObject;

			foreach (Symbol child in thisSymbol.Children) {
				treeBuilder.AddChild (child);
			}
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			Symbol symbol = (Symbol)dataObject;
			return (null != symbol.Children && 0 < symbol.Children.Count);
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (null != thisNode && null != otherNode) {
				Symbol thisCN = thisNode.DataItem as Symbol,
				         otherCN = otherNode.DataItem as Symbol;
	
				if (null != thisCN && null != otherCN) {
					return Array.IndexOf<string>(types, thisCN.MemberType) - 
					       Array.IndexOf<string>(types, otherCN.MemberType);
				}
			}

			return -1;
		}
	}
}
