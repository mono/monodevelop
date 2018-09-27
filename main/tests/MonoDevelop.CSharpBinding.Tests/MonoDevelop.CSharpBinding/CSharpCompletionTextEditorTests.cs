//
// CSharpCompletionTextEditorTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.CSharpBinding.Tests;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using NUnit.Framework;
using MonoDevelop.Debugger;
using Mono.Debugging.Client;
using System.Threading;
using MonoDevelop.Ide.Editor;
using MonoDevelop.SourceEditor;
using Gtk;

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	public class CSharpCompletionTextEditorTests : TextEditorExtensionTestBase
	{
		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharpWithReferences;
		protected override IEnumerable<TextEditorExtension> GetEditorExtensions ()
		{
			foreach (var ext in base.GetEditorExtensions ())
				yield return ext;
			yield return new CSharpCompletionTextEditorExtension ();
		}

		[Test]
		public async Task TestBug58473 ()
		{
			await TestCompletion (@"$", (doc, list) => Assert.IsNotNull (list));
		}

		[Test]
		public async Task TestBug57170 ()
		{
			await TestCompletion (@"using System;

namespace console61
{
	class MainClass
	{
		public static void Main ()
		{
			_ = Foo (out _$); // No completion for discards
			return;
		}

		static (int Test, string Me) Foo (out int gg)
		{
			gg = 3;
			return (1, ""3"");
		}
	}
}
", (doc, list) => Assert.IsFalse (list.AutoSelect), new CompletionTriggerInfo (CompletionTriggerReason.CharTyped, '_'));

		}

		[Test]
		public async Task TestImportCompletionExtensionMethods ()
		{
			IdeApp.Preferences.AddImportedItemsToCompletionList.Value = true;
			await TestCompletion (@"using System;

namespace console61
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			args.$
		}

	}
}
", (doc, list) => Assert.IsTrue (list.Any (d => d.CompletionText == "Any")));

		}

		[Test]
		public async Task TestImportCompletionTypes ()
		{
			IdeApp.Preferences.AddImportedItemsToCompletionList.Value = true;
			await TestCompletion (@"
namespace console61
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			C$
		}

	}
}
", (doc, list) => {
				Assert.IsTrue (list.Any (d => d.CompletionText == "Console"));

				// The display text should not include the namespace
				var item = list.FirstOrDefault (d => d.CompletionText == "DateTime");
				Assert.AreEqual ("DateTime", item.DisplayText);
			});
		}

		[Test]
		public async Task TestImportCompletionTurnedOff ()
		{
			IdeApp.Preferences.AddImportedItemsToCompletionList.Value = false;
			await TestCompletion (@"
namespace console61
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			C$
		}

	}
}
", (doc, list) => Assert.IsFalse (list.Any (d => d.CompletionText == "Console")));

		}

		Task TestCompletion (string text, Action<Document, ICompletionDataList> action, Action<Document> preCompletionAction = null)
		{
			return TestCompletion (text, action, CompletionTriggerInfo.CodeCompletionCommand, preCompletionAction);
		}

		async Task TestCompletion (string text, Action<Document, ICompletionDataList> action, CompletionTriggerInfo triggerInfo, Action<Document> preCompletionAction = null)
		{
			int endPos = text.IndexOf ('$');
			if (endPos >= 0)
				text = text.Substring (0, endPos) + text.Substring (endPos + 1);

			using (var testCase = await SetupTestCase (text, cursorPosition: Math.Max (0, endPos))) {
				var doc = testCase.Document;
				if (preCompletionAction != null)
					preCompletionAction (doc);

				var compExt = doc.GetContent<CSharpCompletionTextEditorExtension> ();
				compExt.CurrentCompletionContext = new CodeCompletionContext {
					TriggerOffset = doc.Editor.CaretOffset,
					TriggerWordLength = 1,
				};

				await doc.UpdateParseDocument ();

				var tmp = IdeApp.Preferences.EnableAutoCodeCompletion;
				IdeApp.Preferences.EnableAutoCodeCompletion.Set (false);
				var list = await compExt.HandleCodeCompletionAsync (compExt.CurrentCompletionContext, triggerInfo);
				try {
					action (doc, list);
				} finally {
					IdeApp.Preferences.EnableAutoCodeCompletion.Set (tmp);
				}
			}
		}

		/// <summary>
		/// Bug 568065: Multiple identical entries for Tuple in completion list
		/// </summary>
		[Test]
		public async Task TestVSTSBug568065 ()
		{
			IdeApp.Preferences.AddImportedItemsToCompletionList.Value = true;
			await TestCompletion (@"$", (doc, list) => Assert.AreEqual (1, list.Where (d => d.CompletionText == "Tuple").Count ()));
		}


		/// <summary>
		/// Bug 567937: Completion after using keyword should be in suggestion mode
		/// </summary>
		[Test]
		public async Task TestVSTSBug567937 ()
		{
			IdeApp.Preferences.AddImportedItemsToCompletionList.Value = true;
			await TestCompletion (@"using S$", (doc, list) => Assert.AreEqual (0, list.OfType<ImportSymbolCompletionData> ().Count ()));
		}

		/// <summary>
		/// Bug 564610: code completion is broken
		/// </summary>
		[Test]
		public async Task TestVSTSBug564610 ()
		{
			IdeApp.Preferences.AddImportedItemsToCompletionList.Value = true;

			await TestCompletion (@"
using System;
using Foundation;

namespace Foundation
{
	public class ExportAttribute : Attribute
	{
		public ExportAttribute(string id) { }
	}

	public class ProtocolAttribute : Attribute
	{
		public string Name { get; set; }
		public ProtocolAttribute() { }
	}
}

class MyProtocol
{
	[Export("":FooBar"")]
	public virtual void FooBar() { }
}


[Protocol(Name = ""MyProtocol"")]
class ProtocolClass
{
}

class FooBar : ProtocolClass
{
	override $$
}


", (doc, list) => Assert.AreEqual (1, list.Where (d => d.CompletionText == "FooBar").Count ()));
		} 

		/// <summary>
		/// Bug 611923: Unable to add declarations to an empty C# file which is added to existing .net console project.
		/// </summary>
		[Test]
		public async Task TestVSTS611923 ()
		{
			await TestCompletion (@"using $", (doc, list) => {
				var item = (RoslynCompletionData)list.FirstOrDefault (d => d.CompletionText == "System");
				KeyActions actions = KeyActions.Complete;
				item.InsertCompletionText (doc.Editor, doc, ref actions, KeyDescriptor.Return);
				Assert.AreEqual ("using System", doc.Editor.Text);
			});
		} 

		[Test]
		public async Task TestDebuggerCompletionProvider ()
		{
			var text = @"
namespace console61
	{
		class MainClass
		{
			public static void Main (string [] args)
			{
				$Console.WriteLine(2);$
			}

			static void Method2 (int a)
			{
			}
		}
	}
";

			int startOfStatement = text.IndexOf ('$');
			if (startOfStatement >= 0)
				text = text.Substring (0, startOfStatement) + text.Substring (startOfStatement + 1);
			int endOfStatement = text.IndexOf ('$');
			if (endOfStatement >= 0)
				text = text.Substring (0, endOfStatement) + text.Substring (endOfStatement + 1);

			using (var testCase = await SetupTestCase (text, cursorPosition: Math.Max (0, startOfStatement))) {
				var doc = testCase.Document;
				var compExt = doc.GetContent<IDebuggerCompletionProvider> ();

				await doc.UpdateParseDocument ();
				var startLine = doc.Editor.GetLineByOffset (startOfStatement);
				var startColumn = startOfStatement - startLine.Offset;
				var endLine = doc.Editor.GetLineByOffset (endOfStatement);
				var endColumn = endOfStatement - endLine.Offset;

				var completionResult = await compExt.GetExpressionCompletionData ("a", new StackFrame (0, new SourceLocation ("", "", startLine.LineNumber, startColumn, endLine.LineNumber, endColumn), "C#"), default (CancellationToken));
				Assert.IsNotNull (completionResult);
				Assert.Less (10, completionResult.Items.Count);//Just randomly high number
				Assert.IsTrue (completionResult.Items.Any (i => i.Name == "args"));
				Assert.IsTrue (completionResult.Items.Any (i => i.Name == "System"));
				Assert.IsTrue (completionResult.Items.Any (i => i.Name == "Method2"));
				Assert.AreEqual (1, completionResult.ExpressionLength);
			}
		}


		/// <summary>
		/// Text loses indentation when typing #5025
		/// </summary>
		[Test]
		public async Task TestIssue5025 ()
		{
			IdeApp.Preferences.AddImportedItemsToCompletionList.Value = true;
			await TestCompletion (@"
namespace console61
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            t$
        }
    }
}
",
								  (doc, list) => {
									  var extEditor = doc.Editor.GetContent<SourceEditorView> ().TextEditor;
									  var compExt = doc.GetContent<CSharpCompletionTextEditorExtension> ();
									  CompletionWindowManager.StartPrepareShowWindowSession ();
									  extEditor.EditorExtension = compExt;
									  extEditor.OnIMProcessedKeyPressEvent (Gdk.Key.BackSpace, '\0', Gdk.ModifierType.None);
									  var listWindow = new CompletionListWindow ();
									  var widget = new NamedArgumentCompletionTests.TestCompletionWidget (doc.Editor, doc);
									  listWindow.CompletionWidget = widget;
									  listWindow.CodeCompletionContext = widget.CurrentCodeCompletionContext;
									  var item = (RoslynCompletionData)list.FirstOrDefault (d => d.CompletionText == "MainClass");
									  KeyActions ka = KeyActions.Process;
									  Gdk.Key key = Gdk.Key.Tab;
									  item.InsertCompletionText (doc.Editor, doc, ref ka, KeyDescriptor.FromGtk (key, (char)key, Gdk.ModifierType.None));
									  Assert.AreEqual (@"
namespace console61
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            MainClass
        }
    }
}
", doc.Editor.Text);
								  });
		}


		[Test]
		public async Task TestIssue5816 ()
		{
			await TestCompletion (@"
using System;
using System.Collections.Generic;

namespace MyLibrary
{
    public class MyClass
    {
        public MyClass()
        {
            Console.WriteLine ();
            var str = new List<string> {$$
        }
    }
}

", (doc, list) => Assert.AreEqual (0, list.Count), new CompletionTriggerInfo (CompletionTriggerReason.CharTyped, '{'));
		}

	}
}
