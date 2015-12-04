//
// DomPropertyNodeBuilder.cs
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
using MonoDevelop.Projects.Text;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler;
using System.Threading;
using Mono.TextEditor;
using System.Collections.Generic;
using Mono.Cecil;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace MonoDevelop.AssemblyBrowser
{
	class DomPropertyNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(IUnresolvedProperty); }
		}
		
		public DomPropertyNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
			
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var property = (IUnresolvedProperty)dataObject;
			return property.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var property = (IUnresolvedProperty)dataObject;
			try {
				var resolved = Resolve (treeBuilder, property);
				nodeInfo.Label = Ambience.GetString (resolved, OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup | OutputFlags.CompletionListFomat);
			} catch (Exception) {
				nodeInfo.Label = property.Name;
			}
			if (property.IsPrivate || property.IsInternal)
				nodeInfo.Label = DomMethodNodeBuilder.FormatPrivate (nodeInfo.Label);
			nodeInfo.Icon = Context.GetIcon (property.GetStockIcon ());
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return false;
		}
		
		
		#region IAssemblyBrowserNodeBuilder

		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Disassemble (TextEditorData data, ITreeNavigator navigator)
		{
			if (DomMethodNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var property = CecilLoader.GetCecilObject ((IUnresolvedProperty)navigator.DataItem);
			return DomMethodNodeBuilder.Disassemble (data, rd => rd.DisassembleProperty (property));
		}
		
		static string GetBody (string text)
		{
			int idx = text.IndexOf ('{') + 1;
			int idx2 = text.LastIndexOf ('}');
			if (idx2 - idx <= 0)
				return text;
			string result = text.Substring (idx, idx2 - idx);
			if (result.StartsWith ("\n"))
				result = result.Substring (1);
			return result;
		}

		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Decompile (TextEditorData data, ITreeNavigator navigator, bool publicOnly)
		{
			if (DomMethodNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var property = CecilLoader.GetCecilObject ((IUnresolvedProperty)navigator.DataItem);
			if (property == null)
				return null;
			return DomMethodNodeBuilder.Decompile (data, DomMethodNodeBuilder.GetModule (navigator), property.DeclaringType, b => b.AddProperty (property));
		}
		
		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.GetSummary (TextEditorData data, ITreeNavigator navigator, bool publicOnly)
		{
			if (DomMethodNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var property = CecilLoader.GetCecilObject ((IUnresolvedProperty)navigator.DataItem);
			if (property == null)
				return null;
			return DomMethodNodeBuilder.GetSummary (data, DomMethodNodeBuilder.GetModule (navigator), property.DeclaringType, b => b.AddProperty (property));
		}

		string IAssemblyBrowserNodeBuilder.GetDocumentationMarkup (ITreeNavigator navigator)
		{
			var property = (IUnresolvedProperty)navigator.DataItem;
			var resolved = Resolve (navigator, property);
			StringBuilder result = new StringBuilder ();
			result.Append ("<big>");
			result.Append (Ambience.GetString (resolved, OutputFlags.AssemblyBrowserDescription));
			result.Append ("</big>");
			result.AppendLine ();
			
			AmbienceService.DocumentationFormatOptions options = new AmbienceService.DocumentationFormatOptions ();
			options.MaxLineLength = -1;
			options.BigHeadings = true;
			options.Ambience = Ambience;
			result.AppendLine ();
			
			result.Append (AmbienceService.GetDocumentationMarkup (resolved, AmbienceService.GetDocumentation (resolved), options));
			
			return result.ToString ();
		}
		#endregion

	}
}
