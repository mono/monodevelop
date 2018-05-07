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
			await TestCompletion (@"$", list => Assert.IsNotNull (list));
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
", list => Assert.IsFalse (list.AutoSelect), new CompletionTriggerInfo (CompletionTriggerReason.CharTyped, '_'));

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
", list => Assert.IsTrue (list.Any (d => d.CompletionText == "Any")));

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
", list => {
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
", list => Assert.IsFalse (list.Any (d => d.CompletionText == "Console")));

		}

		Task TestCompletion (string text, Action<ICompletionDataList> action)
		{
			return TestCompletion (text, action, CompletionTriggerInfo.CodeCompletionCommand);
		}

		async Task TestCompletion (string text, Action<ICompletionDataList> action, CompletionTriggerInfo triggerInfo)
		{
			DesktopService.Initialize ();

			int endPos = text.IndexOf ('$');
			if (endPos >= 0)
				text = text.Substring (0, endPos) + text.Substring (endPos + 1);

			var doc = await SetupDocument (text, cursorPosition: Math.Max (0, endPos));

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
				action (list);
			} finally {
				IdeApp.Preferences.EnableAutoCodeCompletion.Set (tmp);
			}
		}

		/// <summary>
		/// Bug 568065: Multiple identical entries for Tuple in completion list
		/// </summary>
		[Test]
		public async Task TestVSTSBug568065 ()
		{
			IdeApp.Preferences.AddImportedItemsToCompletionList.Value = true;
			await TestCompletion (@"$", list => Assert.AreEqual (1, list.Where (d => d.CompletionText == "Tuple").Count ()));
		}


		/// <summary>
		/// Bug 567937: Completion after using keyword should be in suggestion mode
		/// </summary>
		[Test]
		public async Task TestVSTSBug567937 ()
		{
			IdeApp.Preferences.AddImportedItemsToCompletionList.Value = true;
			await TestCompletion (@"using S$", list => Assert.AreEqual (0, list.OfType<ImportSymbolCompletionData> ().Count ()));
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


", list => Assert.AreEqual (1, list.Where (d => d.CompletionText == "FooBar").Count ()));
		}
	}
}
