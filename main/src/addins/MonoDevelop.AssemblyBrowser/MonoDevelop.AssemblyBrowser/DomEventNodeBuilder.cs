//
// DomEventNodeBuilder.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Text;

using Mono.Cecil;

using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.AssemblyBrowser
{
	public class DomEventNodeBuilder : TypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(IEvent); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			
			IEvent evt = (IEvent)dataObject;
			return evt.FullName;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			IEvent evt = (IEvent)dataObject;
			label = AmbienceService.GetAmbience ("text/x-csharp").GetString (evt, OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup);
			if (evt.IsPrivate || evt.IsInternal)
				label = DomMethodNodeBuilder.FormatPrivate (label);
			icon = MonoDevelop.Ide.Gui.IdeApp.Services.Resources.GetIcon (evt.StockIcon, Gtk.IconSize.Menu);
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is IEvent)
				return ((IEvent)thisNode.DataItem).Name.CompareTo (((IEvent)otherNode.DataItem).Name);
			return 1;
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			IEvent evt = (IEvent)dataObject;
			if (evt.AddMethod != null)
				ctx.AddChild (evt.AddMethod);
			if (evt.RemoveMethod != null)
				ctx.AddChild (evt.RemoveMethod);
			if (evt.RaiseMethod != null)
				ctx.AddChild (evt.RaiseMethod);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		#region IAssemblyBrowserNodeBuilder
		string IAssemblyBrowserNodeBuilder.GetDescription (ITreeNavigator navigator)
		{
			IEvent evt = (IEvent)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			result.Append ("<span font_family=\"monospace\">");
			result.Append (DomTypeNodeBuilder.ambience.GetString (evt, OutputFlags.AssemblyBrowserDescription));
			result.Append ("</span>");
			result.AppendLine ();
			DomMethodNodeBuilder.PrintDeclaringType (result, navigator);
			DomTypeNodeBuilder.PrintAssembly (result, navigator);
			return result.ToString ();
		}
		
		string IAssemblyBrowserNodeBuilder.GetDisassembly (ITreeNavigator navigator)
		{
			IEvent evt = (IEvent)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			result.Append (DomTypeNodeBuilder.ambience.GetString (evt, DomTypeNodeBuilder.settings));
			return result.ToString ();
		}
		string IAssemblyBrowserNodeBuilder.GetDecompiledCode (ITreeNavigator navigator)
		{
			IEvent evt = (IEvent)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			result.Append (DomTypeNodeBuilder.ambience.GetString (evt, DomTypeNodeBuilder.settings));
			return result.ToString ();
		}
		#endregion
		
	}
}