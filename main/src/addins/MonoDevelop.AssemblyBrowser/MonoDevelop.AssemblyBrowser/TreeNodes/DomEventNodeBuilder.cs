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

using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using ICSharpCode.Decompiler;
using System.Threading;
using System.Collections.Generic;
using Mono.Cecil;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.AssemblyBrowser
{
	class DomEventNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(IUnresolvedEvent); }
		}
		
		public DomEventNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var evt = (IUnresolvedEvent)dataObject;
			return evt.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var evt = (IUnresolvedEvent)dataObject;
			try {
				var resolved = Resolve (treeBuilder, evt);
				nodeInfo.Label = MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (Ambience.ConvertSymbol (resolved));
			} catch (Exception) {
				nodeInfo.Label = evt.Name;
			}
			if (evt.IsPrivate || evt.IsInternal)
				nodeInfo.Label = DomMethodNodeBuilder.FormatPrivate (nodeInfo.Label);
			nodeInfo.Icon = Context.GetIcon (evt.GetStockIcon ());
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			var evt = (IUnresolvedEvent)dataObject;
			if (evt.CanAdd)
				ctx.AddChild (evt.AddAccessor);
			if (evt.CanRemove)
				ctx.AddChild (evt.RemoveAccessor);
			if (evt.CanInvoke)
				ctx.AddChild (evt.InvokeAccessor);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return false;
		}
		
		#region IAssemblyBrowserNodeBuilder
		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Disassemble (TextEditor data, ITreeNavigator navigator)
		{
			if (DomMethodNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var evt = GetCecilLoader (navigator).GetCecilObject <EventDefinition> ((IUnresolvedEvent)navigator.DataItem);
			return DomMethodNodeBuilder.Disassemble (data, rd => rd.DisassembleEvent (evt));
		}
		
		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Decompile (TextEditor data, ITreeNavigator navigator, DecompileFlags flags)
		{
			if (DomMethodNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var evt = GetCecilLoader (navigator).GetCecilObject <EventDefinition>((IUnresolvedEvent)navigator.DataItem);
			if (evt == null)
				return null;
			return DomMethodNodeBuilder.Decompile (data, DomMethodNodeBuilder.GetAssemblyLoader (navigator), b => b.Decompile (evt), flags: flags);
		}

		string IAssemblyBrowserNodeBuilder.GetDocumentationMarkup (ITreeNavigator navigator)
		{
			var evt = (IUnresolvedEvent)navigator.DataItem;
			var resolved = Resolve (navigator, evt);
			StringBuilder result = new StringBuilder ();
			result.Append ("<big>");
			result.Append (MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (Ambience.ConvertSymbol (resolved)));
			result.Append ("</big>");
			result.AppendLine ();
			result.AppendLine ();

			//result.Append (AmbienceService.GetDocumentationMarkup (resolved, AmbienceService.GetDocumentation (resolved), options));
			
			return result.ToString ();
		}
		#endregion
		
	}
}
