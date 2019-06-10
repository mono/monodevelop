//
// RoslynEventNodeBuilder.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;
using System.Threading.Tasks;

namespace MonoDevelop.AssemblyBrowser
{
	abstract class RoslynMemberNodeBuilder : TypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((ISymbol)dataObject).ToDisplayString (Ambience.NameFormat);
		}

		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			try {
				if (thisNode == null || otherNode == null)
					return -1;
				return string.Compare (thisNode.NodeName, otherNode.NodeName, StringComparison.OrdinalIgnoreCase);
			} catch (Exception e) {
				LoggingService.LogError ("Exception in assembly browser sort function.", e);
				return -1;
			}
		}

		public Task<List<ReferenceSegment>> DecompileAsync (TextEditor data, ITreeNavigator navigator, DecompileFlags flags)
		{
			return DisassembleAsync (data, navigator);
		}

		public async Task<List<ReferenceSegment>> DisassembleAsync (TextEditor data, ITreeNavigator navigator)
		{
			var symbol = navigator.DataItem as ISymbol;
			if (symbol == null) {
				data.Text = "// DataItem is no symbol " + navigator.DataItem; // should never happen
				LoggingService.LogError ("DataItem is no symbol " + navigator.DataItem);
				return new List<ReferenceSegment> ();
			}
			var location = symbol.Locations [0];
			if (location.IsInSource) {
				var root = await location.SourceTree.GetRootAsync ();
				var node = root.FindNode (location.SourceSpan);
				if (node != null) {
					data.Text = node.ToFullString ();
				} else {
					data.Text = GettextCatalog.GetString ("// no source node found for {0}", symbol.MetadataName);
				}
			} else {
				data.Text = "// Error: Symbol " + symbol.MetadataName + " is not in source."; // should never happen
				LoggingService.LogError ("Symbol " + symbol.MetadataName + " is not in source.");
			}
			return new List<ReferenceSegment> ();
		}


	}
}
