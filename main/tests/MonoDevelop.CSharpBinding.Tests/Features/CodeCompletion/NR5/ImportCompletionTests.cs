//
// ImportCompletionTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory6.CSharp.Completion;
using NUnit.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using System.Collections.Immutable;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.CSharp.Completion;

namespace ICSharpCode.NRefactory6.CSharp.CodeCompletion
{
	[TestFixture]
	class ImportCompletionTests
	{
		public static CSharpCompletionTextEditorExtension.CSharpCompletionDataList CreateProvider (string text)
		{
			int cursorPosition;
			SemanticModel semanticModel;
			Document document;

			var engine = CodeCompletionBugTests.CreateEngine (text, out cursorPosition, out semanticModel, out document, null);
			var ext = new CSharpCompletionTextEditorExtension ();
			var list = new CSharpCompletionTextEditorExtension.CSharpCompletionDataList ();
			var result = CodeCompletionBugTests.CreateProvider (text);
			list.AddRange (result);
			ext.AddImportCompletionData (result, list, new RoslynCodeCompletionFactory (ext, semanticModel), semanticModel, cursorPosition);

			return list;
		}

		/*
		[Test]
		public void TestSimpleCase ()
		{
			var provider = CreateProvider(@"
class Test
{
	public static void Main (string[] args)
	{
		$
	}
}
");
			
			var data = provider.Find ("AppDomain", true) as CodeCompletionBugTests.TestFactory.ImportCompletionData;
			Assert.NotNull(data);
			Assert.AreEqual("System", data.Type.Namespace);
			Assert.False(data.UseFullName);

			data = provider.Find ("File", true) as CodeCompletionBugTests.TestFactory.ImportCompletionData;
			Assert.NotNull(data);
			Assert.AreEqual("System.IO", data.Type.Namespace);
			Assert.False(data.UseFullName);
		}

		[Test]
		public void TestHiding ()
		{
			var provider = CreateProvider(@"using System;
class Test
{
	public static void Main (string[] args)
	{
		$
	}
}
");
			
			var data = provider.Find ("AppDomain", true) as CodeCompletionBugTests.TestFactory.ImportCompletionData;
			Assert.IsNull(data);
			
			data = provider.Find ("File", true) as CodeCompletionBugTests.TestFactory.ImportCompletionData;
			Assert.NotNull(data);
			Assert.AreEqual("System.IO", data.Type.Namespace);
			Assert.False(data.UseFullName);
		}

		[Test]
		public void TestFullName ()
		{
			var provider = CreateProvider(@"using Foo;

namespace Foo {
	class AppDomain {}
}

class Test
{
	public static void Main (string[] args)
	{
		$
	}
}
");
			
			var data = provider.Find ("AppDomain", true) as CodeCompletionBugTests.TestFactory.ImportCompletionData;
			Assert.NotNull(data);
			Assert.True(data.UseFullName);
		}

*/
		[Test]
		public void TestAutomaticImport ()
		{
			var provider = CreateProvider (@"class Test
{
	public static void Main (string[] args)
	{
		$c$
	}
}");
			var data = provider.OfType<ImportSymbolCompletionData> ().FirstOrDefault (item => item.DisplayText == "Console");
			Assert.NotNull(data);
		}
		/*
		[Test]
		public void TestAutomaticImportClash1 ()
		{
			var provider = CodeCompletionBugTests.CreateProvider(@"class Console {}

class Test
{
	public static void Main (string[] args)
	{
		$c$
	}
}");
			var data = provider.OfType<CodeCompletionBugTests.TestFactory.ImportCompletionData>().Single(d => d.DisplayText == "Console");
			Assert.NotNull(data);
			Assert.True(data.UseFullName);

		}

		[Ignore("Too slow atm :(")]
		[Test]
		public void TestAutomaticImportLocalClash ()
		{
			var provider = CodeCompletionBugTests.CreateProvider(@"
class Test
{
	public static void Main (string[] args)
	{
		int Console = 12;
		$c$
	}
}");
			var data = provider.OfType<CodeCompletionBugTests.TestFactory.ImportCompletionData>().Single(d => d.DisplayText == "Console");
			Assert.NotNull(data);
			Assert.True(data.UseFullName);

		}

		[Test]
		public void TestAutomaticHiding ()
		{
			var provider = CodeCompletionBugTests.CreateProvider(@"using System.Collections.Generic;

class Test
{
	public static void Main (string[] args)
	{
		$D$
	}
}");
			var data = provider.OfType<CodeCompletionBugTests.TestFactory.ImportCompletionData>().FirstOrDefault(d => d.DisplayText == "Dictionary");
			Assert.IsNull(data);

		}*/

		/// <summary>
		/// Bug 40888 - Extension methods shown on types
		/// </summary>
		[Test]
		public void TestBug40888 ()
		{
			var provider = CreateProvider (@"
using System.Collections.Generic;

class X
{
    y ()
    {
        $List<int>.$
    }
}");
			var data = provider.OfType<ImportSymbolCompletionData> ();
			Assert.IsTrue (!data.Any ());
		}
	}
}
