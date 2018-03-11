using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Ide.Completion.Presentation
{
	public class MyCSharpCompletionData : MyRoslynCompletionData
	{
		public MyCSharpCompletionData (Microsoft.CodeAnalysis.Document document, ITextSnapshot triggerSnapshot, CompletionService completionService, CompletionItem completionItem) :
			base (document, triggerSnapshot, completionService, completionItem)
		{
		}

		protected override string MimeType => "text/csharp";

		protected override void Format (TextEditor editor, Ide.Gui.Document document, SnapshotPoint start, SnapshotPoint end)
		{
			return;
			//MonoDevelop.CSharp.Formatting.OnTheFlyFormatter.Format (editor, document, start, end);
		}

		public override IconId Icon {
			get => RoslynCompletionData.GetIcon (CompletionItem, MimeType);
		}
	}
}