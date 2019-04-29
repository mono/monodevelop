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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Extensions
{
	[ExtensionNodeChild (typeof(MimeTypeFileNode), "File")]
	class MimeTypeNode: ExtensionNode
	{
		[NodeAttribute ("_description", Localizable=true)]
		public string Description { get; private set; }

		//these fields are assigned by reflection, suppress "never assigned" warning
		#pragma warning disable 649

		[NodeAttribute]
		string icon;
		
		[NodeAttribute (Required=false)]
		string baseType;
		
		[NodeAttribute (Required=false)]
		protected bool isText;

		#pragma warning restore 649

		/// <summary>
		/// The name used by Roslyn to identify this language.
		/// </summary>
		[NodeAttribute ("roslynName", "The name used by Roslyn to identify this language", Required=false)]
		public string RoslynName { get; private set; }

		[NodeAttribute ("contentType", "The content type name used by the Visual Studio editor to identify this language", Required = false)]
		public string ContentType { get; private set; }

		FileNameEvalutor regex;
		
		public IconId Icon {
			get => icon;
			set => icon = value;
		}

		public string BaseType {
			get {
				if (string.IsNullOrEmpty (baseType))
					return isText ? "text/plain" : null;
				else
					return baseType;
			}
		}

		string overrideId;
		public new string Id => overrideId ?? (overrideId = base.Id);

		//deserialization
		public MimeTypeNode () { }

		public MimeTypeNode (string id, string baseType, string description, string icon, bool isText, string contentType)
		{
			overrideId = id;
			this.baseType = baseType;
			Description = description;
			this.icon = icon;
			this.isText = isText;
			ContentType = contentType;
		}

		public bool SupportsFile (string fileName)
		{
			if (regex == null)
				regex = FileNameEvalutor.CreateFileNameEvaluator (GetPatterns ());
			return regex.SupportsFile (fileName);
		}

		IEnumerable<string> GetPatterns ()
		{
			foreach (MimeTypeFileNode node in ChildNodes)
				yield return node.Pattern;
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
		//these fields are assigned by reflection, suppress "never assigned" warning
		#pragma warning disable 649

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
