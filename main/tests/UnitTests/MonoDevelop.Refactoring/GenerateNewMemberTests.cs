// 
// GenerateNewMemberTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using NUnit.Framework;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Refactoring;
using MonoDevelop.CSharpBinding.Tests;
using MonoDevelop.Refactoring.ExtractMethod;
using System.Collections.Generic;
using MonoDevelop.CSharpBinding;
using System.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.CSharp.Parser;
using Mono.TextEditor;

namespace MonoDevelop.Refactoring.Tests
{
	[TestFixture()]
	public class GenerateNewMemberTests : UnitTests.TestBase
	{
		static void TestInsertionPoints (string text)
		{
			TextEditorData data = new TextEditorData ();
			List<InsertionPoint> loc = new List<InsertionPoint> ();
			for (int i = 0; i < text.Length; i++) {
				char ch = text[i];
				if (ch == '@') {
					i++;
					ch = text[i];
					NewLineInsertion insertBefore = NewLineInsertion.None;
					NewLineInsertion insertAfter  = NewLineInsertion.None;
					
					switch (ch) {
					case 'd':
						insertAfter = NewLineInsertion.Eol;
						break;
					case 'D':
						insertAfter = NewLineInsertion.BlankLine;
						break;
					case 'u':
						insertBefore = NewLineInsertion.Eol;
						break;
					case 'U':
						insertBefore = NewLineInsertion.BlankLine;
						break;
					case 's':
						insertBefore = insertAfter = NewLineInsertion.Eol;
						break;
					case 'S':
						insertBefore = insertAfter = NewLineInsertion.BlankLine;
						break;
						
					case 't':
						insertBefore = NewLineInsertion.Eol;
						insertAfter = NewLineInsertion.BlankLine;
						break;
					case 'v':
						insertBefore = NewLineInsertion.BlankLine;
						insertAfter = NewLineInsertion.Eol;
						break;
					}
					loc.Add (new InsertionPoint (data.Document.OffsetToLocation (data.Document.Length), insertBefore, insertAfter));
				} else {
					data.Insert (data.Document.Length, ch.ToString ());
				}
			}
			
			var parseResult = new NRefactoryParser ().Parse (null, "a.cs", data.Document.Text);
			
			var foundPoints = HelperMethods.GetInsertionPoints (data.Document, parseResult.CompilationUnit.Types[0]);
			Assert.AreEqual (loc.Count, foundPoints.Count, "point count doesn't match");
			for (int i = 0; i < loc.Count; i++) {
				Console.WriteLine (loc[i] + "/" + foundPoints[i]);
				Assert.AreEqual (loc[i].Location, foundPoints[i].Location, "point " + i + " doesn't match");
				Assert.AreEqual (loc[i].LineAfter, foundPoints[i].LineAfter, "point " + i + " ShouldInsertNewLineAfter doesn't match");
				Assert.AreEqual (loc[i].LineBefore, foundPoints[i].LineBefore, "point " + i + " ShouldInsertNewLineBefore doesn't match");
			}
		}
		
		[Test()]
		public void TestBasicInsertionPoint ()
		{
			TestInsertionPoints (@"
class Test {
	@D
	void TestMe ()
	{
	}
	
@d}
");
		}
		
		
		[Test()]
		public void TestBasicInsertionPointWithoutEmpty ()
		{
			TestInsertionPoints (@"
class Test {
	@Dvoid TestMe ()
	{
	}
@v}
");
		}

		
		
		
		[Test()]
		public void TestBasicInsertionPointOneLineCase ()
		{
			TestInsertionPoints (@"class Test {@Svoid TestMe () { }@v}");
		}
		
		
		
		[Test()]
		public void TestComplexInsertionPoint ()
		{
			TestInsertionPoints (@"
class Test {
	@D
	void TestMe ()
	{
	}
	
	@Dint a;
	
	@Dclass Test2 {
		void TestMe2 ()
		{
	
		}
	}
	
	@Dpublic delegate void ADelegate ();
	
@d}
");
		}
		
		
		[Test()]
		public void TestComplexInsertionPointCase2 ()
		{
			TestInsertionPoints (@"class MainClass {
	@Dstatic void A ()
	{
	}
	@Sstatic void B ()
	{
	}
	
	@Dpublic static void Main (string[] args)
	{
		System.Console.WriteLine ();
	}
	@Sint g;
	@Sint i;
	
	@Dint j;
	@Spublic delegate void Del(int a);
@v}
");
		}

	}
}

