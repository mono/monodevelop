using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.TextEditor
{
	[Name ("Command Descriptor Service")]
	[Export (typeof (IEditorCommandDescriptorService))]

	public class EditorCommandDescriptorService :
	   IEditorCommandDescriptorService
	{
		private readonly IEditorCommandHandlerServiceFactory _editorCommandHandlerServiceFactory;
		private static Func<CommandState> Unspecified { get; } = () => CommandState.Unspecified;

		[ImportingConstructor]
		public EditorCommandDescriptorService (
			IEditorCommandHandlerServiceFactory editorCommandHandlerServiceFactory)
		{
			this._editorCommandHandlerServiceFactory = editorCommandHandlerServiceFactory;
		}

		public string GetBoundShortcut<T> (ITextView textView) where T : EditorCommandArgs
		{
			var state = _editorCommandHandlerServiceFactory
				.GetService(textView)
				.GetCommandState<T> ((a, b) => default, Unspecified);

			var commandId = CommandMappings.Instance.GetCommandId (typeof (T));
			return commandId?.ToString();
		}
	}
}
