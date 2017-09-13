//
// SyntaxHighlightingTest_TextMate.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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

using NUnit.Framework;
using UnitTests;
using MonoDevelop.Ide.Editor.Highlighting;
using System.IO;
using System.Text;

namespace MonoDevelop.Ide.Editor
{

	[TestFixture]
	class SyntaxHighlightingTest_TextMate : IdeTestBase
	{
		[Test]
		public void TestMatch ()
		{
			string highlighting = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
	<key>fileTypes</key>
	<array>
		<string>t</string>
	</array>
	<key>name</key>
	<string>Test</string>
	<key>patterns</key>
	<array>
		<dict>
			<key>match</key>
			<string>\b(foo|bar)\b</string>
			<key>name</key>
			<string>keyword</string>
		</dict>
	</array>
	<key>scopeName</key>
	<string>source.cs</string>
	<key>uuid</key>
	<string>1BA75B32-707C-11D9-A928-000D93589AF6</string>
</dict>
</plist>
";
			string test = @"
test foo this bar
^ source
     ^ keyword
         ^ source
              ^ keyword
";
			var h = TextMateFormat.ReadHighlighting (new MemoryStream (Encoding.UTF8.GetBytes (highlighting)));
			SyntaxHighlightingTest.RunHighlightingTest (h, test);
		}

		[Test]
		public void TestPushPop ()
		{
			string highlighting = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
	<key>fileTypes</key>
	<array>
		<string>t</string>
	</array>
	<key>name</key>
	<string>Test</string>
	<key>patterns</key>
	<array>
		<dict>
			<key>begin</key>
			<string>""</string>
			<key>beginCaptures</key>
			<dict>
				<key>0</key>
				<dict>
					<key>name</key>
					<string>punctuation.definition.string.begin</string>
				</dict>
			</dict>
			<key>end</key>
			<string>""</string>
			<key>endCaptures</key>
			<dict>
				<key>0</key>
				<dict>
					<key>name</key>
					<string>punctuation.definition.string.end</string>
				</dict>
			</dict>
			<key>name</key>
			<string>string.quoted.double</string>
			<key>patterns</key>
			<array>
				<dict>
					<key>match</key>
					<string>\\.</string>
					<key>name</key>
					<string>constant.character.escape</string>
				</dict>
			</array>
		</dict>
	</array>
	<key>scopeName</key>
	<string>source</string>
	<key>uuid</key>
	<string>1BA75B32-707C-11D9-A928-000D93589AF6</string>
</dict>
</plist>
";
			string test = @"
test ""f\t"" this bar
^ source
     ^ punctuation.definition.string.begin
      ^ string.quoted.double
       ^ constant.character.escape
         ^ punctuation.definition.string.end
";
			var h = TextMateFormat.ReadHighlighting (new MemoryStream (Encoding.UTF8.GetBytes (highlighting)));
			SyntaxHighlightingTest.RunHighlightingTest (h, test);
		}
	}
}
