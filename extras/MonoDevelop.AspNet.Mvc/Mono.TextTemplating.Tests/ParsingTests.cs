// 
// Test.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using NUnit.Framework;

namespace Mono.TextTemplating.Tests
{
	
	
	[TestFixture]
	public class ParsingTests
	{
		static string sample1 = 
@"<#@ template language=""C#v3.5"" #>
Line One
Line Two
<#
foo
#>
Line Three <#= bar #>
Line Four
<#+ 
baz \#>
#>
";
		
		
		[Test]
		public void TokenTest ()
		{
			Tokeniser tk = new Tokeniser ("test.input", sample1);
			
			//line 1
			Assert.IsTrue (tk.Advance ());
			Assert.AreEqual (new Location ("test.input", 1, 1), tk.Location);
			Assert.AreEqual (State.Content, tk.State);
			Assert.AreEqual ("", tk.Value);
			Assert.IsTrue (tk.Advance ());
			Assert.AreEqual (State.Directive, tk.State);
			Assert.IsTrue (tk.Advance ());
			Assert.AreEqual (new Location ("test.input", 1, 5), tk.Location);
			Assert.AreEqual (State.DirectiveName, tk.State);
			Assert.AreEqual ("template", tk.Value);
			Assert.IsTrue (tk.Advance ());
			Assert.AreEqual (State.Directive, tk.State);
			Assert.IsTrue (tk.Advance ());
			Assert.AreEqual (new Location ("test.input", 1, 14), tk.Location);
			Assert.AreEqual (State.DirectiveName, tk.State);
			Assert.AreEqual ("language", tk.Value);
			Assert.IsTrue (tk.Advance ());
			Assert.AreEqual (State.Directive, tk.State);
			Assert.IsTrue (tk.Advance ());
			Assert.AreEqual (State.DirectiveValue, tk.State);
			Assert.AreEqual (new Location ("test.input", 1, 23), tk.Location);
			Assert.AreEqual ("C#v3.5", tk.Value);
			Assert.IsTrue (tk.Advance ());
			Assert.AreEqual (State.Directive, tk.State);
			
			//line 2, 3
			Assert.IsTrue (tk.Advance ());
			Assert.AreEqual (new Location ("test.input", 2, 1), tk.Location);
			Assert.AreEqual (State.Content, tk.State);
			Assert.AreEqual ("Line One\nLine Two\n", tk.Value);
			
			//line 4, 5, 6
			Assert.IsTrue (tk.Advance ());
			Assert.AreEqual (new Location ("test.input", 4, 1), tk.TagStartLocation);
			Assert.AreEqual (new Location ("test.input", 4, 3), tk.Location);
			Assert.AreEqual (new Location ("test.input", 6, 3), tk.TagEndLocation);
			Assert.AreEqual (State.Block, tk.State);
			Assert.AreEqual ("\nfoo\n", tk.Value);
			
			//line 7
			Assert.IsTrue (tk.Advance ());
			Assert.AreEqual (new Location ("test.input", 7, 1), tk.Location);
			Assert.AreEqual (State.Content, tk.State);
			Assert.AreEqual ("Line Three ", tk.Value);
			Assert.IsTrue (tk.Advance ());
			Assert.AreEqual (new Location ("test.input", 7, 12), tk.TagStartLocation);
			Assert.AreEqual (new Location ("test.input", 7, 15), tk.Location);
			Assert.AreEqual (new Location ("test.input", 7, 22), tk.TagEndLocation);
			Assert.AreEqual (State.Expression, tk.State);
			Assert.AreEqual (" bar ", tk.Value);
			
			//line 8
			Assert.IsTrue (tk.Advance ());
			Assert.AreEqual (new Location ("test.input", 7, 22), tk.Location);
			Assert.AreEqual (State.Content, tk.State);
			Assert.AreEqual ("\nLine Four\n", tk.Value);
			
			//line 9, 10, 11
			Assert.IsTrue (tk.Advance ());
			Assert.AreEqual (new Location ("test.input", 9, 1), tk.TagStartLocation);
			Assert.AreEqual (new Location ("test.input", 9, 4), tk.Location);
			Assert.AreEqual (new Location ("test.input", 11, 3), tk.TagEndLocation);
			Assert.AreEqual (State.Helper, tk.State);
			Assert.AreEqual (" \nbaz \\#>\n", tk.Value);
			
			//line 12
			Assert.IsTrue (tk.Advance ());
			Assert.AreEqual (new Location ("test.input", 12, 1), tk.Location);
			Assert.AreEqual (State.Content, tk.State);
			Assert.AreEqual ("", tk.Value);
			
			//EOF
			Assert.IsFalse (tk.Advance ());
			Assert.AreEqual (new Location ("test.input", 12, 1), tk.Location);
			Assert.AreEqual (State.EOF, tk.State);
		}
	}
}
