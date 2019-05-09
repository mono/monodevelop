using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor.Expansion;
using MonoDevelop.Core;
using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.TextEditor
{
	[Serializable]
	public class SnippetToolboxNode : ItemToolboxNode, ITextToolboxNode
	{
		static IExpansionServiceProvider expansionServiceProvider = CompositionManager.Instance.GetExportedValue<IExpansionServiceProvider> ();
		static IEditorCommandHandlerServiceFactory commandDispatchFactory = CompositionManager.Instance.GetExportedValue<IEditorCommandHandlerServiceFactory> ();

		public override string Name {
			get {
				return Template.Snippet.Shortcut;
			}
			set {
				Template.Snippet.Shortcut = value;
			}
		}

		public override string Description {
			get {
				return Template.Snippet.Description;
			}
			set {
				Template.Snippet.Description = value;
			}
		}

		public string Code {
			get { return Template.Snippet.Code; }
			set { Template.Snippet.Code = value; }
		}

		public ExpansionTemplate Template { get; set; }

		public string GetDragPreview (Document document)
		{
			return Template.Snippet.Shortcut;
		}

		public bool IsCompatibleWith (Document document)
		{
			return true;
		}

		public void InsertAtCaret (Document document)
		{
			var textView = document.GetContent<ITextView> ();
			textView.Properties.AddProperty (typeof (ExpansionTemplate), Template);
			try {
				commandDispatchFactory.GetService (textView).Execute ((t, b) => new InsertSnippetCommandArgs (t, b), null);
			} finally {
				textView.Properties.RemoveProperty (typeof (ExpansionTemplate));
			}
			((ICocoaTextView) textView).VisualElement.Window?.MakeKeyWindow ();
		}

		public SnippetToolboxNode (ExpansionTemplate template)
		{
			this.Template = template;
			ItemFilters.Add (new ToolboxItemFilterAttribute ("text/plain", ToolboxItemFilterType.Allow));
		}
	}
}
