// 
// ExtractMethodTests.cs
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
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Refactoring;
using MonoDevelop.CSharpBinding.Tests;
using MonoDevelop.CSharp.Refactoring.ExtractMethod;
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
	public class ExtractMethodTests : UnitTests.TestBase
	{
		static int pcount = 0;
		
		internal static RefactoringOptions CreateRefactoringOptions (string text)
		{
			int cursorPosition = -1;
			int endPos = text.IndexOf ('$');
			if (endPos >= 0) {
				cursorPosition = endPos;
				text = text.Substring (0, cursorPosition) + text.Substring (cursorPosition + 1);
			}
			
			int selectionStart = -1;
			int selectionEnd   = -1;
			int idx = text.IndexOf ("<-");
			if (idx >= 0) {
				selectionStart = idx;
				text = text.Substring (0, idx) + text.Substring (idx + 2);
				selectionEnd = idx = text.IndexOf ("->");
				
				text = text.Substring (0, idx) + text.Substring (idx + 2);
				if (cursorPosition < 0)
					cursorPosition = selectionEnd - 1;
			}
			
			TestWorkbenchWindow tww = new TestWorkbenchWindow ();
			TestViewContent sev = new TestViewContent ();
	//		return new RefactoringOptions ();
			
			DotNetProject project = new DotNetAssemblyProject ("C#");
			Solution solution = new Solution ();
			solution.RootFolder.Items.Add (project);
			project.FileName = GetTempFile (".csproj");
			string file = GetTempFile (".cs");
			project.AddFile (file);
			string parsedText = text;
			string editorText = text;
			ProjectDomService.Load (project);
			ProjectDom dom = ProjectDomService.GetProjectDom (project);
			dom.ForceUpdate (true);
			ProjectDomService.Parse (project, file, delegate { return parsedText; });
			ProjectDomService.Parse (project, file, delegate { return parsedText; });
			
			sev.Project = project;
			sev.ContentName = file;
			sev.Text = editorText;
			sev.CursorPosition = cursorPosition;
			
			tww.ViewContent = sev;
			var doc = new MonoDevelop.Ide.Gui.Document (tww);
			doc.Editor.Document.MimeType = "text/x-csharp";
			doc.Editor.Document.FileName = file;
			doc.ParsedDocument = new McsParser ().Parse (null, sev.ContentName, parsedText);
			foreach (var e in doc.ParsedDocument.Errors)
				Console.WriteLine (e);
			if (cursorPosition >= 0)
				doc.Editor.Caret.Offset = cursorPosition;
			if (selectionStart >= 0) 
				doc.Editor.SetSelection (selectionStart, selectionEnd);
			
			NRefactoryResolver resolver = new NRefactoryResolver (dom, 
			                                                      doc.ParsedDocument.CompilationUnit, 
			                                                      sev.Data, 
			                                                      file);
			
			ExpressionResult expressionResult;
			if (selectionStart >= 0) {
				expressionResult = new ExpressionResult (editorText.Substring (selectionStart, selectionEnd - selectionStart).Trim ());
				endPos = selectionEnd;
			} else {
				expressionResult = new NewCSharpExpressionFinder (dom).FindFullExpression (doc.Editor, cursorPosition + 1);
			}
			ResolveResult resolveResult = endPos >= 0 ? resolver.Resolve (expressionResult, new DomLocation (doc.Editor.Caret.Line, doc.Editor.Caret.Column)) : null;
			
			RefactoringOptions result = new RefactoringOptions {
				Document = doc,
				Dom = dom,
				ResolveResult = resolveResult,
				SelectedItem = null
			};
			if (resolveResult is MemberResolveResult)
				result.SelectedItem = ((MemberResolveResult)resolveResult).ResolvedMember;
			if (resolveResult is LocalVariableResolveResult)
				result.SelectedItem = ((LocalVariableResolveResult)resolveResult).LocalVariable;
			if (resolveResult is ParameterResolveResult)
				result.SelectedItem = ((ParameterResolveResult)resolveResult).Parameter;
			result.TestFileProvider = new FileProvider (result);
			return result;
		}
		
		class FileProvider : MonoDevelop.Projects.Text.ITextFileProvider
		{
			RefactoringOptions options;
			
			public FileProvider (RefactoringOptions options)
			{
				this.options = options;
			}
			
			public MonoDevelop.Projects.Text.IEditableTextFile GetEditableTextFile (FilePath filePath)
			{
				return options.Document.GetContent<MonoDevelop.Projects.Text.IEditableTextFile> ();;
			}
		}
			
		internal static string GetOutput (RefactoringOptions options, List<Change> changes)
		{
			RefactoringService.AcceptChanges (null, options.Dom, changes, new FileProvider (options));
			return options.Document.Editor.Text;
		}
			
		
		internal static bool CompareSource (string code1, string code2)
		{
			return Strip (code1) == Strip (code2);
		}

		static string Strip (string code1)
		{
			StringBuilder result = new StringBuilder ();
			for (int i = 0; i < code1.Length; i++) {
				char ch = code1[i];
				if (Char.IsWhiteSpace (ch))
					continue;
				result.Append (ch);
			}
			return result.ToString ();
		}

		void TestExtractMethod (string inputString, string outputString)
		{
			ExtractMethodRefactoring refactoring = new ExtractMethodRefactoring ();
			RefactoringOptions options = CreateRefactoringOptions (inputString);
			ExtractMethodRefactoring.ExtractMethodParameters parameters = refactoring.CreateParameters (options);
			Assert.IsNotNull (parameters);
			parameters.Name = "NewMethod";
			parameters.InsertionPoint = new Mono.TextEditor.InsertionPoint (new DocumentLocation (options.ResolveResult.CallingMember.BodyRegion.End.Line + 1, 1), NewLineInsertion.BlankLine, NewLineInsertion.None);
			List<Change> changes = refactoring.PerformChanges (options, parameters);
			string output = GetOutput (options, changes);
			Assert.IsTrue (CompareSource (output, outputString), "Expected:" + Environment.NewLine + outputString + Environment.NewLine + "was:" + Environment.NewLine + output);
		}
		
		[Test()]
		public void ExtractMethodResultStatementTest ()
		{
			TestExtractMethod (@"class TestClass
{
	int member = 5;
	void TestMethod ()
	{
		int i = 5;
		<- i = member + 1; ->
		Console.WriteLine (i);
	}
}
", @"class TestClass
{
	int member = 5;
	void TestMethod ()
	{
		int i = 5;
		NewMethod (ref i);
		Console.WriteLine (i);
	}

	void NewMethod (ref int i)
	{
		i = member + 1;
	}
}
");
		}
		
		[Test()]
		public void ExtractMethodResultExpressionTest ()
		{
			TestExtractMethod (@"class TestClass
{
	int member =5;
	void TestMethod ()
	{
		int i = <- member + 1 ->;
		Console.WriteLine (i);
	}
}
", @"class TestClass
{
	int member =5;
	void TestMethod ()
	{
		int i = NewMethod ();
		Console.WriteLine (i);
	}

	int NewMethod ()
	{
		return member + 1;
	}
}
");
		}
		
		[Test()]
		public void ExtractMethodStaticResultStatementTest ()
		{
			TestExtractMethod (@"class TestClass
{
	void TestMethod ()
	{
		int i = 5;
		<- i = i + 1; ->
		Console.WriteLine (i);
	}
}
", @"class TestClass
{
	void TestMethod ()
	{
		int i = 5;
		NewMethod (ref i);
		Console.WriteLine (i);
	}

	static void NewMethod (ref int i)
	{
		i = i + 1;
	}
}
");
		}
		
		[Test()]
		public void ExtractMethodStaticResultExpressionTest ()
		{
			TestExtractMethod (@"class TestClass
{
	void TestMethod ()
	{
		int i = <- 5 + 1 ->;
		Console.WriteLine (i);
	}
}
", @"class TestClass
{
	void TestMethod ()
	{
		int i = NewMethod ();
		Console.WriteLine (i);
	}

	static int NewMethod ()
	{
		return 5 + 1;
	}
}
");
		}
		
		[Test()]
		public void ExtractMethodMultiVariableTest ()
		{
			TestExtractMethod (@"class TestClass
{
	int member;
	void TestMethod ()
	{
		int i = 5, j = 10, k;
		<-
		j = i + j;
		k = j + member;
		->
		Console.WriteLine (k + j);
	}
}
", @"class TestClass
{
	int member;
	void TestMethod ()
	{
		int i = 5, j = 10, k;
		NewMethod (i, ref j, out k);
		Console.WriteLine (k + j);
	}
	
	void NewMethod (int i, ref int j, out int k)
	{
		j = i + j;
		k = j + member;
	}
}
");
		}
		
		/// <summary>
		/// Bug 607990 - "Extract Method" refactoring sometimes tries to pass in unnecessary parameter depending on selection
		/// </summary>
		[Test()]
		public void TestBug607990 ()
		{
			TestExtractMethod (@"class TestClass
{
	void TestMethod ()
	{
		<-
		Object obj1 = new Object();
		obj1.ToString();
		->
	}
}
", @"class TestClass
{
	void TestMethod ()
	{
		NewMethod ();
	}
	
	static void NewMethod ()
	{
		Object obj1 = new Object();
		obj1.ToString();
	}
}
");
		}
		
		
		/// <summary>
		/// Bug 616193 - Extract method passes param with does not exists any more in main method
		/// </summary>
		[Test()]
		public void TestBug616193 ()
		{
			TestExtractMethod (@"class TestClass
{
	void TestMethod ()
	{
		string ret;
		string x;
		IEnumerable<string> y;
		<-string z = ret + y;
		ret = x + z;->
	}
}
", @"class TestClass
{
	void TestMethod ()
	{
		string ret;
		string x;
		IEnumerable<string> y;
		NewMethod (out ret, x, y);
	}
	
	static void NewMethod (out string ret, string x, IEnumerable<string> y)
	{
		string z = ret + y;
		ret = x + z;
	}
}
");
		}
		
		/// <summary>
		/// Bug 616199 - Extract method forgets to return a local var which is used in main method
		/// </summary>
		[Test()]
		public void TestBug616199 ()
		{
			TestExtractMethod (@"class TestClass
{
	void TestMethod ()
	{
		<-string z = ""test"" + ""x"";->
		string ret = ""test1"" + z;
	}
}
", @"class TestClass
{
	void TestMethod ()
	{
		string z = NewMethod ();
		string ret = ""test1"" + z;
	}
	
	static string NewMethod ()
	{
		string z = ""test"" + ""x"";
		return z;
	}
}
");
		}
		
		/// <summary>
		/// Bug 666271 - "Extract Method" on single line adds two semi-colons in method, none in replaced text
		/// </summary>
		[Test()]
		public void TestBug666271 ()
		{
			TestExtractMethod (@"class TestClass
{
	void TestMethod ()
	{
		<-TestMethod ();->
	}
}
", @"class TestClass
{
	void TestMethod ()
	{
		NewMethod ();
	}
	
	void NewMethod ()
	{
		TestMethod ();
	}
}
");
		}
		
		
		/// <summary>
		/// Bug 693944 - Extracted method returns void instead of the correct type
		/// </summary>
		[Test()]
		public void TestBug693944 ()
		{
			TestExtractMethod (@"class TestClass
{
	void TestMethod ()
	{
		TestMethod (<-""Hello""->);
	}
}
", @"class TestClass
{
	void TestMethod ()
	{
		TestMethod (NewMethod ());
	}
	
	string NewMethod ()
	{
		return ""Hello"";
	}
}
");
		}
		
		
		/* Currently not possible to implement, would cause serve bugs:
		[Test()]
		public void ExtractMethodMultiVariableWithLocalReturnVariableTest ()
		{
			TestExtractMethod (@"class TestClass
{
	int member;
	void TestMethod ()
	{
		int i = 5, j = 10, k;
		<-
		int test;
		j = i + j;
		k = j + member;
		test = i + j + k;
		->
		Console.WriteLine (test);
	}
}
", @"class TestClass
{
	int member;
	void TestMethod ()
	{
		int i = 5, j = 10, k;
		int test;
		NewMethod (i, ref j, out k, out test);
		Console.WriteLine (test);
	}
	
	void NewMethod (int i, ref int j, out int k, out int test)
	{
		j = i + j;
		k = j + member;
		test = i + j + k;
	}
}
");
		}
		*/
		
		
		
	}
}
 