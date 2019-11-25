using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;

namespace MonoDevelop.TextEditor
{
	[Name ("Command Descriptor Service")]
	[Export (typeof (IEditorCommandDescriptorService))]

	public class EditorCommandDescriptorService :
	   IEditorCommandDescriptorService
	{
		readonly IEditorCommandHandlerServiceFactory _editorCommandHandlerServiceFactory;

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
			if (commandId == null)
				return null;

			var cmd = IdeApp.CommandService?.GetCommand (commandId);
			var bindings = KeyBindingService.CurrentKeyBindingSet.GetBindings (cmd);

			if (!bindings.Any()) return null;

			return string.Join (", ", bindings.Select (x => x.ToString ()));
		}
	}
} 
