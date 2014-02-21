//
// FileInformation.cs
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
using Mono.TextEditor.Utils;
using Jurassic;
using Jurassic.Library;
using System.IO;

namespace TypeScriptBinding.Hosting
{
	class FileInformation : ObjectInstance
	{
		public FileInformation(ScriptEngine engine, string contents, ByteOrderMark byteOrderMark) : base (engine)
		{
			this.contents = contents;
			this.byteOrderMark = (int)byteOrderMark;
			this.PopulateFunctions ();
		}
		
		[JSProperty]
		public string contents { get; set; }

		[JSProperty]
		public int byteOrderMark { get; set; }

		public static FileInformation Read (ScriptEngine engine, string path, int codepage)
		{
			bool hadBom;
			System.Text.Encoding encoding;
			string contents = null;

			ByteOrderMark bom = ByteOrderMark.None;
			if (File.Exists (path)) {
				contents = TextFileUtility.ReadAllText (path, out hadBom, out encoding); 
				if (hadBom) {
					if (encoding.CodePage == System.Text.Encoding.UTF8.CodePage)
						bom = ByteOrderMark.Utf8;
					if (encoding.CodePage == System.Text.Encoding.BigEndianUnicode.CodePage)
						bom = ByteOrderMark.Utf16BigEndian;
					if (encoding.CodePage == System.Text.Encoding.Unicode.CodePage)
						bom = ByteOrderMark.Utf16LittleEndian;
				}
			} else {
				var stream = typeof(IOImpl).Assembly.GetManifestResourceStream (Path.GetFileName (path));
				if (stream != null) {
					contents = new StreamReader (stream).ReadToEnd ();
				}
			}
			return new FileInformation (engine, contents, bom);
		}
	}
}
