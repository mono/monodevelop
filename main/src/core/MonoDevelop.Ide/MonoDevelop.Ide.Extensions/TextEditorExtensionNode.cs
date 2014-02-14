// 
// TextEditorExtensionNode.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Mono.Addins;
using System.Collections;
using System.Linq;

namespace MonoDevelop.Ide.Extensions
{
	class TextEditorExtensionNode: TypeExtensionNode
	{
		[NodeAttribute (Description="Comma separated list of file extensions to which this editor extension applies. It applies to all if none is specified.")]
		string[] fileExtensions;
		
		[NodeAttribute (Description="Comma separated list of mime types to which this editor extension applies. It applies to all if none is specified.")]
		string[] mimeTypes;
		
		public string[] FileExtensions {
			get { return this.fileExtensions; }
			set { this.fileExtensions = value; }
		}

		public string[] MimeTypes {
			get { return this.mimeTypes; }
			set { this.mimeTypes = value; }
		}
		
		public bool Supports (string fileName, string[] mimetypeChain)
		{
			if (fileExtensions != null && fileExtensions.Length > 0) {
				string ext = System.IO.Path.GetExtension (fileName);
				return fileExtensions.Any (fe => string.Compare (fe, ext, StringComparison.OrdinalIgnoreCase) == 0);
			}

			if (mimeTypes != null && mimeTypes.Length > 0) {
				foreach (string t in mimeTypes)
					foreach (string m in mimetypeChain)
						if (m == t)
							return true;
				return false;
			}
			
			// No constraints on the file
			return true;
		}
	}
}

