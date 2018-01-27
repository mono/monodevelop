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
using MonoDevelop.Ide.TypeSystem;
using System.Linq;
using MonoDevelop.Refactoring;
using MonoDevelop.Core.ProgressMonitoring;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core;
using System.Threading.Tasks;

namespace MonoDevelop.CSharpBinding.Refactoring
{
	[TestFixture()]
	[Ignore]
	public class GenerateNewMemberTests : UnitTests.TestBase
	{
		static async Task TestInsertionPoints (string text)
		{
			var tww = new TestWorkbenchWindow ();
			var content = new TestViewContent ();
			tww.ViewContent = content;
			content.ContentName = "/a.cs";
			content.Data.MimeType = "text/x-csharp";
			MonoDevelop.AnalysisCore.AnalysisOptions.EnableUnitTestEditorIntegration.Set (true);
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
					var vv = data.OffsetToLocation (data.Length);
					loc.Add (new InsertionPoint (new DocumentLocation (vv.Line, vv.Column), insertBefore, insertAfter));
				} else {
					data.InsertText (data.Length, ch.ToString ());
				}
			}


			var project = Services.ProjectService.CreateProject ("C#");
			project.Name = "test";
			project.FileName = "test.csproj";
			project.Files.Add (new ProjectFile ("/a.cs", BuildAction.Compile)); 

			var solution = new MonoDevelop.Projects.Solution ();
			solution.AddConfiguration ("", true); 
			solution.DefaultSolutionFolder.AddItem (project);
			using (var monitor = new ProgressMonitor ())
				await TypeSystemService.Load (solution, monitor);
			content.Project = project;
			doc.SetOwner (project);
			var parsedFile = await doc.UpdateParseDocument ();
			var model = parsedFile.GetAst<SemanticModel> ();
			var sym = model?.GetEnclosingSymbol (data.Text.IndexOf ('{'));
			var type = sym as INamedTypeSymbol ?? sym?.ContainingType;
			if (type != null) {
				var foundPoints = InsertionPointService.GetInsertionPoints (doc.Editor, parsedFile, type, type.Locations.First ());
				Assert.AreEqual (loc.Count, foundPoints.Count, "point count doesn't match");
				for (int i = 0; i < loc.Count; i++) {
					Assert.AreEqual (loc [i].Location, foundPoints [i].Location, "point " + i + " doesn't match");
					Assert.AreEqual (loc [i].LineAfter, foundPoints [i].LineAfter, "point " + i + " ShouldInsertNewLineAfter doesn't match");
					Assert.AreEqual (loc [i].LineBefore, foundPoints [i].LineBefore, "point " + i + " ShouldInsertNewLineBefore doesn't match");
				}
			}

			TypeSystemService.Unload (solution);

		}
		
		[Test()]
		public async Task TestBasicInsertionPoint ()
		{
			await TestInsertionPoints (@"
class Test {
@D	
	void TestMe ()
	{
	}
	
@d}
");
		}
		
		[Test()]
		public async Task TestBasicInsertionPoint2 ()
		{
			await TestInsertionPoints (@"
class Test 
{
@D	
	void TestMe ()
	{
	}
	
@d}
");
		}
		
		
		public async Task TestBasicInsertionPointWithoutEmpty ()
		{
			await TestInsertionPoints (@"
class Test {
	@Tvoid TestMe ()
	{
	}
@V}
");
		}
		
		[Test()]
		public async Task TestBasicInsertionPointOneLineCase ()
		{
			await TestInsertionPoints (@"class Test {@tvoid TestMe () { }@v}");
		}
		
		
		[Test()]
		public async Task TestEmptyClass ()
		{
			await TestInsertionPoints (@"class Test {@s}");
		}
		
		[Test()]
		public async Task TestEmptyClass2 ()
		{
			await TestInsertionPoints (@"class Test {
@n
}");
		}
		
		[Test()]
		public async Task TestEmptyClass3 ()
		{
			await TestInsertionPoints (@"class Test
{
@n
}");
		}
		
		[Test()]
		public async Task TestComplexInsertionPoint ()
		{
			await TestInsertionPoints (@"
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
		public async Task TestComplexInsertionPointCase2 ()
		{
			await TestInsertionPoints (@"class MainClass {
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
		public async Task TestEmptyClassInsertion ()
		{
			await TestInsertionPoints (@"
public class EmptyClass
{@s}");
			
			await TestInsertionPoints (@"
public class EmptyClass : Base
{@s}");

		}

		[Ignore()]
		[Test()]
		public async Task TestBrokenInsertionPoint ()
		{
			await TestInsertionPoints (@"
public class EmptyClass
}");

		}
		

		/// <summary>
		/// Bug 5682 - insert method inserts two trailing tabs after } and has no trailing blank line
		/// </summary>
		[Test()]
		public async Task Bug5682 ()
		{
			await TestInsertionPoints (@"class MainClass {
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

		[Test]
		public async Task TestComplexInsertionPOintCase3 ()
		{
			await TestInsertionPoints (@"using System;
class vaevle
{
@D    int fooBar = 0;
@u	

    public event EventHandler FooBar;
@u	

    public vaevle ()
    {
        FooBar += HandleEventHandler;
    }
@u	

    public static void Main (string [] args)
    {
        try {
            System.Console.WriteLine (nameof (args));
        } catch (Exception e) when (true) {

        }
    }
@s}

");
		}
	}
}

