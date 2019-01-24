//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using System.Collections.Immutable;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Mono.Addins;

namespace MonoDevelop.TextEditor
{
	class CommandMappings
	{
		public static CommandMappings Instance { get; } = new CommandMappings ();

		ImmutableDictionary<string, CommandMappingExtensionNode> mappings = ImmutableDictionary<string, CommandMappingExtensionNode>.Empty;

		CommandMappings ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/TextEditor/CommandMapping", ExtensionChanged);
		}

		void ExtensionChanged (object sender, ExtensionNodeEventArgs args)
		{
			switch (args.Change) {
			case ExtensionChange.Add:
				mappings = mappings.SetItem (args.ExtensionNode.Id, (CommandMappingExtensionNode) args.ExtensionNode);
				break;
			case ExtensionChange.Remove:
				mappings = mappings.Remove (args.ExtensionNode.Id);
				break;
			}
		}

		public MappedEditorCommand GetMapping (object commandId)
		{
			if (commandId is string s && mappings.TryGetValue (s, out var node)) {
				return node.GetMappedCommand ();
			}
			return null;
		}

		internal bool HasMapping (object commandId)
		{
			return commandId is string s && mappings.ContainsKey (s);
		}
	}

	abstract class MappedEditorCommand
	{
		public abstract void Execute (IEditorCommandHandlerService service, Action nextCommandHandler);
		public abstract CommandState GetCommandState (IEditorCommandHandlerService service, Func<CommandState> nextCommandHandler);
	}
}