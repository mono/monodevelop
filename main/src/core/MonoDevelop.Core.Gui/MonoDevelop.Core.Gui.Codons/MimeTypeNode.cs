// MimeTypeNode.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Mono.Addins;

namespace MonoDevelop.Core.Gui.Codons
{
	[ExtensionNodeChild (typeof(MimeTypeFileNode), "File")]
	class MimeTypeNode: ExtensionNode
	{
		[NodeAttribute]
		string icon;
		
		[NodeAttribute ("_description", Localizable=true)]
		string description;
		
		Regex regex;
		
		public string Icon {
			get {
				return icon;
			}
			set {
				icon = value;
			}
		}

		public string Description {
			get {
				return description;
			}
			set {
				description = value;
			}
		}
		
		Regex CreateRegex ()
		{
			StringBuilder globalPattern = new StringBuilder ();
			
			foreach (MimeTypeFileNode file in ChildNodes) {
				string pattern = Regex.Escape (file.Pattern);
				pattern = pattern.Replace ("\\*",".*");
				pattern = pattern.Replace ("\\?",".");
				pattern = pattern.Replace ("\\|","$|^");
				pattern = "^" + pattern + "$";
				if (globalPattern.Length > 0)
					globalPattern.Append ('|');
				globalPattern.Append (pattern);
			}
			return new Regex (globalPattern.ToString ());
		}

		
		public bool SupportsFile (string fileName)
		{
			if (regex == null)
				regex = CreateRegex ();
			return regex.IsMatch (fileName);
		}
		
		protected override void OnChildNodeAdded (ExtensionNode node)
		{
			base.OnChildNodeAdded (node);
			regex = null;
		}
		
		protected override void OnChildNodeRemoved (ExtensionNode node)
		{
			base.OnChildNodeRemoved (node);
			regex = null;
		}
	}
	
	class MimeTypeFileNode: ExtensionNode
	{
		[NodeAttribute]
		string pattern;
		
		public string Pattern {
			get {
				return pattern;
			}
			set {
				pattern = value;
			}
		}
	}
}
