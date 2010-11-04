// 
// TastBlankLineFormatting.cs
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.CSharp.Completion;
using Mono.TextEditor;
using MonoDevelop.CSharp.Formatting;

namespace MonoDevelop.CSharpBinding.FormattingTests
{
	[TestFixture()]
	public class TestBlankLineFormatting : UnitTests.TestBase
	{
		[Test()]
		public void TestBlankLinesAfterUsings ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"using System;
using System.Text;
namespace Test
{
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BlankLinesAfterUsings = 2;
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"using System;
using System.Text;


namespace Test
{
}", data.Document.Text);
			
		policy.BlankLinesAfterUsings = 0;
		compilationUnit = new CSharpParser ().Parse (data);
		compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
		Assert.AreEqual (@"using System;
using System.Text;
namespace Test
{
}", data.Document.Text);
		}
		
		[Test()]
		public void TestBlankLinesBeforeUsings ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"using System;
using System.Text;
namespace Test
{
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BlankLinesAfterUsings = 0;
			policy.BlankLinesBeforeUsings = 2;
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"

using System;
using System.Text;
namespace Test
{
}", data.Document.Text);
			
		policy.BlankLinesBeforeUsings = 0;
		compilationUnit = new CSharpParser ().Parse (data);
		compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
		Assert.AreEqual (@"using System;
using System.Text;
namespace Test
{
}", data.Document.Text);
		}
		
		[Test()]
		public void TestBlankLinesBeforeFirstDeclaration ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"namespace Test
{
	class Test
	{
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BlankLinesBeforeFirstDeclaration = 2;
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"namespace Test
{


	class Test
	{
	}
}", data.Document.Text);
			
		policy.BlankLinesBeforeFirstDeclaration = 0;
		compilationUnit = new CSharpParser ().Parse (data);
		compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
		Assert.AreEqual (@"namespace Test
{
	class Test
	{
	}
}", data.Document.Text);
		}
		
		
		[Test()]
		public void TestBlankLinesBetweenTypes ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"namespace Test
{
	class Test1
	{
	}
	class Test2
	{
	}
	class Test3
	{
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BlankLinesBetweenTypes = 1;
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"namespace Test
{
	class Test1
	{
	}

	class Test2
	{
	}

	class Test3
	{
	}
}", data.Document.Text);
			
		policy.BlankLinesBetweenTypes = 0;
		compilationUnit = new CSharpParser ().Parse (data);
		compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
		Assert.AreEqual (@"namespace Test
{
	class Test1
	{
	}
	class Test2
	{
	}
	class Test3
	{
	}
}", data.Document.Text);
		}
		
		[Test()]
		public void TestBlankLinesBetweenFields ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	int a;
	int b;
	int c;
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BlankLinesBetweenFields = 1;
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	int a;

	int b;

	int c;
}", data.Document.Text);
			
		policy.BlankLinesBetweenFields = 0;
		compilationUnit = new CSharpParser ().Parse (data);
		compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
		Assert.AreEqual (@"class Test
{
	int a;
	int b;
	int c;
}", data.Document.Text);
		}
		
		[Test()]
		public void TestBlankLinesBetweenEventFields ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	public event EventHandler a;
	public event EventHandler b;
	public event EventHandler c;
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BlankLinesBetweenEventFields = 1;
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Assert.AreEqual (@"class Test
{
	public event EventHandler a;

	public event EventHandler b;

	public event EventHandler c;
}", data.Document.Text);
			
		policy.BlankLinesBetweenEventFields = 0;
		compilationUnit = new CSharpParser ().Parse (data);
		compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
		Assert.AreEqual (@"class Test
{
	public event EventHandler a;
	public event EventHandler b;
	public event EventHandler c;
}", data.Document.Text);
		}
		
		
		[Test()]
		public void TestBlankLinesBetweenMembers ()
		{
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = "a.cs";
			data.Document.Text = @"class Test
{
	void AMethod ()
	{
	}
	void BMethod ()
	{
	}
	void CMethod ()
	{
	}
}";
			
			CSharpFormattingPolicy policy = new CSharpFormattingPolicy ();
			policy.BlankLinesBetweenMembers = 1;
			CSharp.Dom.CompilationUnit compilationUnit = new CSharpParser ().Parse (data);
			compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
			Console.WriteLine (data.Text);
			Assert.AreEqual (@"class Test
{
	void AMethod ()
	{
	}

	void BMethod ()
	{
	}

	void CMethod ()
	{
	}
}", data.Document.Text);
			
		policy.BlankLinesBetweenMembers = 0;
		compilationUnit = new CSharpParser ().Parse (data);
		compilationUnit.AcceptVisitor (new DomIndentationVisitor (policy, data), null);
		Assert.AreEqual (@"class Test
{
	void AMethod ()
	{
	}
	void BMethod ()
	{
	}
	void CMethod ()
	{
	}
}", data.Document.Text);
		}
		
		
		
	}
}

