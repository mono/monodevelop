//
// JavaScriptLanguageBinding.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.IO;

using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace JavaScriptBinding
{
	class JavaScriptLanguageBinding : ILanguageBinding
	{
		public string Language {
			get {
				return "JavaScript";
			}
		}

		public string ProjectStockIcon {
			get { 
				return "md-project";
			}
		}

		public bool IsSourceCodeFile (FilePath fileName)
		{
			Console.WriteLine ("is source:"+ fileName.Extension == ".ts");
			Console.WriteLine (fileName);
			Console.WriteLine (">"+fileName.Extension+"<");
			Console.WriteLine (fileName.Extension == ".ts");
			return fileName.Extension == ".ts";
		}

		public string SingleLineCommentTag { get { return "//"; } }
		public string BlockCommentStartTag { get { return "/*"; } }
		public string BlockCommentEndTag { get { return "*/"; } }

		public System.CodeDom.Compiler.CodeDomProvider GetCodeDomProvider ()
		{
			return null;
		}

		public FilePath GetFileName (FilePath baseName)
		{
			return baseName + ".ts";
		}

		public ClrVersion[] GetSupportedClrVersions ()
		{
			return new [] { 
				ClrVersion.Net_1_1, 
				ClrVersion.Net_2_0,
				ClrVersion.Clr_2_1,
				ClrVersion.Net_4_0,
				ClrVersion.Net_4_5,
			};
		}
	}
}
