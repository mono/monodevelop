// 
// ILanguageBinding.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using System.CodeDom.Compiler;
using Mono.Addins;
using MonoDevelop.Projects.Extensions;
using System.Linq;

namespace MonoDevelop.Projects
{
	public class LanguageBinding
	{
		string[] fileExtensions;
		string codeDomTypeName;
		RuntimeAddin addin;
		CodeDomProvider codeDomProvider;
		bool? supportsPartialTypes;

		public string Language { get; protected set; }
			
		public string SingleLineCommentTag { get; protected set; }

		public string BlockCommentStartTag { get; protected set; }

		public string BlockCommentEndTag { get; protected set; }

		public bool SupportsPartialTypes {
			get {
				if (!supportsPartialTypes.HasValue && codeDomTypeName != null)
					supportsPartialTypes = GetCodeDomProvider ().Supports (GeneratorSupport.PartialTypes);
				return supportsPartialTypes ?? false;
			}
			set {
				supportsPartialTypes = value;
			}
		}

		public virtual bool IsSourceCodeFile (FilePath fileName)
		{
			if (fileExtensions != null && fileExtensions.Length > 0)
				return fileExtensions.Any (ex => StringComparer.OrdinalIgnoreCase.Equals (fileName.Extension, ex));
			throw new NotImplementedException ();
		}

		public virtual FilePath GetFileName (FilePath fileNameWithoutExtension)
		{
			if (fileExtensions != null && fileExtensions.Length > 0)
				return fileNameWithoutExtension + fileExtensions [0];
			throw new NotImplementedException ();
		}

		public virtual CodeDomProvider GetCodeDomProvider ()
		{
			if (codeDomTypeName != null) {
				if (codeDomProvider == null)
					codeDomProvider = (CodeDomProvider) addin.CreateInstance (codeDomTypeName, true);
				return codeDomProvider;
			}
			return null;
		}

		internal void InitFromNode (LanguageBindingExtensionNode node)
		{
			Language = node.Id;

			if (!string.IsNullOrEmpty (node.SingleLineCommentTag))
				SingleLineCommentTag = node.SingleLineCommentTag;
			
			if (!string.IsNullOrEmpty (node.BlockCommentStartTag))
				BlockCommentStartTag = node.BlockCommentStartTag;
			
			if (!string.IsNullOrEmpty (node.BlockCommentEndTag))
				BlockCommentEndTag = node.BlockCommentEndTag;
			
			if (node.SupportedExtensions != null && node.SupportedExtensions.Length > 0)
				fileExtensions = node.SupportedExtensions;
			
			if (!string.IsNullOrEmpty (node.CodeDomType)) {
				codeDomTypeName = node.CodeDomType;
				addin = node.Addin;
			}
		}
	}
}
