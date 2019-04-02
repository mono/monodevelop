using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Xml.Editor;
using NUnit.Framework;
using MonoDevelop.Ide.CodeCompletion;
using System.Linq;

namespace MonoDevelop.Xml.Tests.CodeCompletion
{
	[TestFixture]
	public class XmlCodeCompletionTests : TextEditorExtensionTestBase
	{
		[Test]
		public async Task TestColonNotCommitChar ()
		{
			using (var testCase = await SetupTestCase ("<", 1)) {
				var doc = testCase.Document;
				var ext = doc.GetContent<BaseXmlEditorExtension> ();
				ext.CompletionWidget = doc.Editor.GetViewContent().GetContent<ICompletionWidget> ();
				await ext.TriggerCompletion (Ide.CodeCompletion.CompletionTriggerReason.CompletionCommand);
				ext.KeyPress (KeyDescriptor.FromGtk (Gdk.Key.colon, ':', Gdk.ModifierType.None));
				Assert.IsTrue (CompletionWindowManager.IsVisible);
				CompletionWindowManager.Wnd.HideWindow ();
			}
		}

		/// <summary>
		/// FeedbackTicket 739349: XAML Editor: When a closing tag already has '</' present, choosing the closing element from the completion window enters an invalid closing tag such as <//ContentView>
		/// </summary>
		[Test]
		public async Task TestVSTS739349 ()
		{
			const string input = @"
<Foo>
</F";
			using (var testCase = await SetupTestCase (input, input.Length)) {
				var doc = testCase.Document;
				var ext = doc.GetContent<BaseXmlEditorExtension> ();
				ext.CompletionWidget = doc.Editor.GetViewContent ().GetContent<ICompletionWidget> ();
				await ext.TriggerCompletion (Ide.CodeCompletion.CompletionTriggerReason.CompletionCommand);
				Assert.IsTrue (CompletionWindowManager.IsVisible);

				Assert.AreEqual ("Foo>", CompletionWindowManager.Wnd.SelectedItem.DisplayText);
				CompletionWindowManager.Wnd.HideWindow ();
			}
		}

		[Test]
		public async Task TestVSTS739349_Case2 ()
		{
			const string input = @"
<Foo>
<";
			using (var testCase = await SetupTestCase (input, input.Length)) {
				var doc = testCase.Document;
				var ext = doc.GetContent<BaseXmlEditorExtension> ();
				ext.CompletionWidget = doc.Editor.GetViewContent ().GetContent<ICompletionWidget> ();
				await ext.TriggerCompletion (Ide.CodeCompletion.CompletionTriggerReason.CompletionCommand);
				Assert.IsTrue (CompletionWindowManager.IsVisible);
				var list = CompletionWindowManager.Wnd.GetFilteredItems ();
				Assert.IsTrue (list.Any (i => i.DisplayText == "/Foo>"));
				CompletionWindowManager.Wnd.HideWindow ();
			}
		}

		protected override EditorExtensionTestData GetContentData ()
		{
			return new EditorExtensionTestData ("a.xslt", "C#", "application/xml", "test.csproj");
		}

		protected override IEnumerable<TextEditorExtension> GetEditorExtensions ()
		{
			foreach (var ext in base.GetEditorExtensions ())
				yield return ext;
			yield return new XmlTextEditorExtension ();
		}
	}
}
