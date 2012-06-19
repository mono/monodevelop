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
using MonoDevelop.CSharpBinding.Tests;
using System.Collections.Generic;
using MonoDevelop.CSharpBinding;
using MonoDevelop.Ide.Gui;
using Mono.TextEditor;
using MonoDevelop.Ide.TypeSystem;
using System.Linq;

namespace MonoDevelop.CSharpBinding.Refactoring
{
	[TestFixture()]
	public class GenerateNewMemberTests : UnitTests.TestBase
	{
		static void TestInsertionPoints (string text)
		{
			
			TestWorkbenchWindow tww = new TestWorkbenchWindow ();
			TestViewContent sev = new TestViewContent ();
			var project = new UnknownProject ();
			project.FileName = "test.csproj";
			
			TypeSystemService.LoadProject (project);

			sev.Project = project;
			tww.ViewContent = sev;
			var doc = new MonoDevelop.Ide.Gui.Document (tww);
			var data = doc.Editor;
			List<InsertionPoint> loc = new List<InsertionPoint> ();
			for (int i = 0; i < text.Length; i++) {
				char ch = text [i];
				if (ch == '@') {
					i++;
					ch = text [i];
					NewLineInsertion insertBefore = NewLineInsertion.None;
					NewLineInsertion insertAfter = NewLineInsertion.None;
					
					switch (ch) {
					case 'n':
						break;
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
					case 'T':
						insertBefore = NewLineInsertion.None;
						insertAfter = NewLineInsertion.BlankLine;
						break;
					case 'v':
						insertBefore = NewLineInsertion.BlankLine;
						insertAfter = NewLineInsertion.Eol;
						break;
					case 'V':
						insertBefore = NewLineInsertion.None;
						insertAfter = NewLineInsertion.Eol;
						break;
					default:
						Assert.Fail ("unknown insertion point:" + ch);
						break;
					}
					loc.Add (new InsertionPoint (data.Document.OffsetToLocation (data.Document.TextLength), insertBefore, insertAfter));
				} else {
					data.Insert (data.Document.TextLength, ch.ToString ());
				}
			}
			
			var parsedFile = TypeSystemService.ParseFile (project, "program.cs", "text/x-csharp", data.Document.Text);

			var foundPoints = CodeGenerationService.GetInsertionPoints (doc.Editor, parsedFile, parsedFile.TopLevelTypeDefinitions.First ());
			Assert.AreEqual (loc.Count, foundPoints.Count, "point count doesn't match");
			for (int i = 0; i < loc.Count; i++) {
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
		public void TestBasicInsertionPoint2 ()
		{
			TestInsertionPoints (@"
class Test 
{
@D	
	void TestMe ()
	{
	}
	
@d}
");
		}
		
		
		public void TestBasicInsertionPointWithoutEmpty ()
		{
			TestInsertionPoints (@"
class Test {
	@Tvoid TestMe ()
	{
	}
@V}
");
		}
		
		[Test()]
		public void TestBasicInsertionPointOneLineCase ()
		{
			TestInsertionPoints (@"class Test {@tvoid TestMe () { }@v}");
		}
		
		
		[Test()]
		public void TestEmptyClass ()
		{
			TestInsertionPoints (@"class Test {@s}");
		}
		
		[Test()]
		public void TestEmptyClass2 ()
		{
			TestInsertionPoints (@"class Test {
@n
}");
		}
		
		[Test()]
		public void TestEmptyClass3 ()
		{
			TestInsertionPoints (@"class Test
{
@n
}");
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
@u	

	int a;
@u	

	class Test2 {
		void TestMe2 ()
		{
	
		}
	}
@s	
	public delegate void ADelegate ();
	
@d}
");
		}
		
		[Test()]
		public void TestComplexInsertionPointCase2 ()
		{
			TestInsertionPoints (@"class MainClass {
@D	static void A ()
	{
	}
@t	static void B ()
	{
	}
@s	
	public static void Main (string[] args)
	{
		System.Console.WriteLine ();
	}
@t	int g;
@t	int i;
@s	
	int j;
@t	public delegate void Del(int a);
@s}
");
		}
		
		
		[Test()]
		public void TestEmptyClassInsertion ()
		{
			TestInsertionPoints (@"
public class EmptyClass
{@s}");
			
			TestInsertionPoints (@"
public class EmptyClass : Base
{@s}");

		}

		[Ignore()]
		[Test()]
		public void TestBrokenInsertionPoint ()
		{
			TestInsertionPoints (@"
public class EmptyClass
}");

		}
		

		/// <summary>
		/// Bug 5682 - insert method inserts two trailing tabs after } and has no trailing blank line
		/// </summary>
		[Test()]
		public void Bug5682 ()
		{
			TestInsertionPoints (@"class MainClass {
@D	static void A ()
	{
	}
@s	
	public static void Main (string[] args)
	{
		System.Console.WriteLine ();
	}
@s}
");
		}

		
	}
}

