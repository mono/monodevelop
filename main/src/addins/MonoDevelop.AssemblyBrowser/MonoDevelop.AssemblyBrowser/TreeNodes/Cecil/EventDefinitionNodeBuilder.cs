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
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.ILSpy;
using MonoDevelop.Core;

namespace MonoDevelop.AssemblyBrowser
{
	class EventDefinitionNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(EventDefinition); }
		}
		
		public EventDefinitionNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var evt = (EventDefinition)dataObject;
			return evt.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var evt = (EventDefinition)dataObject;
			nodeInfo.Label = evt.Name + " : " + CSharpLanguage.Instance.TypeToString (evt.EventType, false, evt);
			var accessor = evt.AddMethod ?? evt.RemoveMethod;

			if (!accessor.IsPublic)
				nodeInfo.Label = MethodDefinitionNodeBuilder.FormatPrivate (nodeInfo.Label);

			nodeInfo.Icon = Context.GetIcon (GetStockIcon (evt));
		}

		public static IconId GetStockIcon (EventDefinition evt)
		{
			var accessor = evt.AddMethod ?? evt.RemoveMethod;
			return MethodDefinitionNodeBuilder.GetStockIcon (accessor);
		}

		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			var evt = (EventDefinition)dataObject;
			if (evt.AddMethod != null)
				ctx.AddChild (evt.AddMethod);
			if (evt.RemoveMethod != null)
				ctx.AddChild (evt.RemoveMethod);
			if (evt.InvokeMethod != null)
				ctx.AddChild (evt.InvokeMethod);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return false;
		}
		
		#region IAssemblyBrowserNodeBuilder
		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Disassemble (TextEditor data, ITreeNavigator navigator)
		{
			if (MethodDefinitionNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var evt = (EventDefinition)navigator.DataItem;
			return MethodDefinitionNodeBuilder.Disassemble (data, rd => rd.DisassembleEvent (evt));
		}
		
		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Decompile (TextEditor data, ITreeNavigator navigator, DecompileFlags flags)
		{
			if (MethodDefinitionNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var evt = navigator.DataItem as EventDefinition;
			if (evt == null)
				return null;
			return MethodDefinitionNodeBuilder.Decompile (data, MethodDefinitionNodeBuilder.GetAssemblyLoader (navigator), b => b.Decompile (evt), flags: flags);
		}

		#endregion
		
	}
}
