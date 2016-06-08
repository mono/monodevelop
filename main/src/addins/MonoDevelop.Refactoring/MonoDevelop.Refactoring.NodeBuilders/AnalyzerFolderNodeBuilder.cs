//
// AnalyzerFolderNodeBuilder.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Core;
using System.IO;
using MonoDevelop.CodeIssues;

namespace MonoDevelop.Refactoring.NodeBuilders
{
	class AnalyzerFolderNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(AnalyzerFolderNode); }
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "Analyzers";
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			nodeInfo.Label = GettextCatalog.GetString ("Analyzers");
			nodeInfo.Icon = Context.GetIcon ("md-reference-package");
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var folderNode = (AnalyzerFolderNode)dataObject;
			var shadowLoader = new ShadowCopyAnalyzerAssemblyLoader ();
			foreach (var item in folderNode.Analyzers) {
				if (File.Exists (item.FilePath)) {
					var assembly = shadowLoader.LoadCore (item.FilePath);
					var loader = new AnalyzersFromAssembly ();
					try {
						loader.AddAssembly (assembly, true);
						treeBuilder.AddChild (loader);
					} catch (Exception e) {
						LoggingService.LogError ("Error while getting analyzers from assembly", e);
						treeBuilder.AddChild (e);
					}
				}
			}
		}
	}
}