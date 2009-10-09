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
using MonoDevelop.Refactoring.ExtractMethod;
using System.Collections.Generic;
using MonoDevelop.CSharpBinding;
using System.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.CSharp.Parser;

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
					cursorPosition = selectionEnd;
			}
			
			TestWorkbenchWindow tww = new TestWorkbenchWindow ();
			TestViewContent sev = new TestViewContent ();
	//		return new RefactoringOptions ();
			
			DotNetProject project = new DotNetAssemblyProject ("C#");
			Solution solution = new Solution ();
			solution.RootFolder.Items.Add (project);
			project.FileName = "/tmp/a" + pcount + ".csproj";
			string file = "/tmp/test-file-" + (pcount++) + ".cs";
			project.AddFile (file);
			string parsedText = text;
			string editorText = text;
			ProjectDomService.Load (project);
			ProjectDom dom = ProjectDomService.GetProjectDom (project);
			dom.ForceUpdate (true);
			ProjectDomService.Parse (project, file, null, delegate { return parsedText; });
			ProjectDomService.Parse (project, file, null, delegate { return parsedText; });
			
			sev.Project = project;
			sev.ContentName = file;
			sev.Text = editorText;
			sev.CursorPosition = cursorPosition;
			
			tww.ViewContent = sev;
			Document doc = new Document (tww);
			
			doc.ParsedDocument = new NRefactoryParser ().Parse (null, sev.ContentName, parsedText);
			foreach (var e in doc.ParsedDocument.Errors)
				Console.WriteLine (e);
			if (cursorPosition >= 0)
				doc.TextEditor.CursorPosition = cursorPosition;
			if (selectionStart >= 0) 
				doc.TextEditor.Select (selectionStart, selectionEnd);
			
			NRefactoryResolver resolver = new NRefactoryResolver (dom, 
			                                                      doc.ParsedDocument.CompilationUnit, 
			                                                      MonoDevelop.Ide.Gui.TextEditor.GetTextEditor (sev), 
			                                                      "a.cs");
			
			ResolveResult resolveResult = endPos >= 0 ? resolver.Resolve (new NewCSharpExpressionFinder (dom).FindFullExpression (editorText, cursorPosition + 1), new DomLocation (doc.TextEditor.CursorLine, doc .TextEditor.CursorColumn)) : null;
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
			return options.Document.TextEditor.Text;
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
		i = NewMethod ();
		Console.WriteLine (i);
	}

	int NewMethod ()
	{
		int i;
		i = member + 1;
		return i;
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
		i = NewMethod (i);
		Console.WriteLine (i);
	}

	static int NewMethod (int i)
	{
		i = i + 1;
		return i;
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
		Console.WriteLine (k);
	}
}
", @"class TestClass
{
	int member;
	void TestMethod ()
	{
		int i = 5, j = 10, k;
		NewMethod (i, ref j, out k);
		Console.WriteLine (k);
	}
	
	void NewMethod (int i, ref int j, out int k)
	{
		j = i + j;
		k = j + member;
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
 