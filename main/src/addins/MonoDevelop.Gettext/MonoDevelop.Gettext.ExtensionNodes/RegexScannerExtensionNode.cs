// RegexScannerExtensionNode.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using Mono.Addins;
using System.Text.RegularExpressions;

namespace MonoDevelop.Gettext.ExtensionNodes
{
	[ExtensionNode ("RegexScanner", Description="Specifies a file scanner which can extract strings to be translated using regular expressions.")]
	[ExtensionNodeChild (typeof(IncludeExtensionNode))]
	[ExtensionNodeChild (typeof(TransformExtensionNode))]
	[ExtensionNodeChild (typeof(ExcludeExtensionNode))]
	class RegexScannerExtensionNode: InstanceExtensionNode
	{
		[NodeAttribute ("extension", Description="Extensions of the files supported by this scanner (use comma to separate multiple extensions)")]
		protected string[] extensions = null;
		
		[NodeAttribute ("mimeType", Description="Mime types of the files supported by this scanner (use comma to separate multiple mime types)")]
		protected string[] mimeTypes = null;
		
		public override object CreateInstance ()
		{
			RegexFileScanner rs = new RegexFileScanner (extensions, mimeTypes);
			foreach (ExtensionNode node in ChildNodes) {
				if (node is IncludeExtensionNode) {
					IncludeExtensionNode rn = (IncludeExtensionNode) node;
					rs.AddIncludeRegex (rn.RegexValue, rn.Group, rn.PluralGroup, rn.RegexOptions, rn.EscapeMode);
				}
				else if (node is ExcludeExtensionNode) {
					ExcludeExtensionNode rn = (ExcludeExtensionNode) node;
					rs.AddExcludeRegex (rn.RegexValue, rn.RegexOptions);
				}
				else if (node is TransformExtensionNode) {
					TransformExtensionNode rn = (TransformExtensionNode) node;
					rs.AddTransformRegex (rn.RegexValue, rn.ReplaceValue, rn.RegexOptions);
				}
			}
			return rs;
		}
	}
}
