using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Xml.Editor;
using NUnit.Framework;
using MonoDevelop.Ide.CodeCompletion;

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
