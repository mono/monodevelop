// 
// BaseRefactorerTests.cs
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
using NUnit.Framework;
using MonoDevelop.Projects.CodeGeneration;

namespace MonoDevelop.Refactoring
{

	[TestFixture()]
	public class BaseRefactorerTests
	{
		[Test()]
		public void TestRemoveIndentWithUnixLineEndings ()
		{
			string unindented = BaseRefactorer.RemoveIndent ("\n\t\n\tprotected virtual void OnButton4Clicked (object sender, System.EventArgs e)\n\t{\n\t}\n");
			Assert.AreEqual ("\n\nprotected virtual void OnButton4Clicked (object sender, System.EventArgs e)\n{\n}\n", unindented);
		}
		
		[Test()]
		public void TestRemoveIndentWithMacLineEndings ()
		{
			string unindented = BaseRefactorer.RemoveIndent ("\r\t\r\tprotected virtual void OnButton4Clicked (object sender, System.EventArgs e)\r\t{\r\t}\r");
			Assert.AreEqual ("\r\rprotected virtual void OnButton4Clicked (object sender, System.EventArgs e)\r{\r}\r", unindented);
		}
		
		[Test()]
		public void TestRemoveIndentWithWindowsLineEndings ()
		{
			string unindented = BaseRefactorer.RemoveIndent ("\r\n\t\r\n\tprotected virtual void OnButton4Clicked (object sender, System.EventArgs e)\r\n\t{\r\n\t}\r\n");
			Assert.AreEqual ("\r\n\r\nprotected virtual void OnButton4Clicked (object sender, System.EventArgs e)\r\n{\r\n}\r\n", unindented);
		}
		
		[Test()]
		public void TestIndentIndentWithUnixLineEndings ()
		{
			string indented = BaseRefactorer.Indent ("\n\nprotected virtual void OnButton4Clicked (object sender, System.EventArgs e)\n{\n}\n", "\t", false);
			Assert.AreEqual ("\n\t\n\tprotected virtual void OnButton4Clicked (object sender, System.EventArgs e)\n\t{\n\t}\n\t", indented);
		}
		
		[Test()]
		public void TestIndentIndentWithMacLineEndings ()
		{
			string indented = BaseRefactorer.Indent ("\r\rprotected virtual void OnButton4Clicked (object sender, System.EventArgs e)\r{\r}\r", "\t", false);
			Assert.AreEqual ("\r\t\r\tprotected virtual void OnButton4Clicked (object sender, System.EventArgs e)\r\t{\r\t}\r\t", indented);
		}
		
		[Test()]
		public void TestIndentIndentWithWindowsLineEndings ()
		{
			string indented = BaseRefactorer.Indent ("\r\n\r\nprotected virtual void OnButton4Clicked (object sender, System.EventArgs e)\r\n{\r\n}\r\n", "\t", false);
			Assert.AreEqual ("\r\n\t\r\n\tprotected virtual void OnButton4Clicked (object sender, System.EventArgs e)\r\n\t{\r\n\t}\r\n\t", indented);
		}
	}
}
