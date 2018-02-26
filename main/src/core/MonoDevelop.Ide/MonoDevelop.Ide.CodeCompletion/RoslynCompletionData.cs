//
// RoslynCompletionData.cs
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using Microsoft.VisualStudio.Platform;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Ide.CodeTemplates;
using System.Linq;
using System.Text;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Ide.CodeCompletion
{
	public abstract class RoslynCompletionData : CompletionData
	{
		protected readonly Microsoft.CodeAnalysis.Document doc;
		protected readonly ITextSnapshot triggerSnapshot;
		protected readonly CompletionService completionService;

		public CompletionItem CompletionItem { get; private set; }

		public override CompletionItemRules Rules { get { return CompletionItem.Rules; } }

		public override string DisplayText {
			get {
				return CompletionItem.DisplayText;
			}
		}

		public override string Description {
			get {
				var description = completionService.GetDescriptionAsync (doc, CompletionItem).Result;
				return description.Text;
			}
		}

		public override string CompletionText {
			get {
				return CompletionItem.DisplayText;
			}
		}

		public abstract CompletionProvider Provider { get; }

		protected abstract string MimeType { get; }

		public override IconId Icon {
			get {
				if (CompletionItem.Tags.Contains ("Snippet")) {
					var template = CodeTemplateService.GetCodeTemplates (MimeType).FirstOrDefault (t => t.Shortcut == CompletionItem.DisplayText);
					if (template != null)
						return template.Icon;
				}
				var modifier = GetItemModifier ();
				var type = GetItemType ();
				var hash = modifier | type << 16;
				if (!IconIdCache.ContainsKey (hash))
					IconIdCache [hash] = "md-" + modifierType [modifier] + completionType [type];
				return IconIdCache [hash];
			}
		}

		static Dictionary<int, string> IconIdCache = new Dictionary<int, string> ();

		public RoslynCompletionData (Microsoft.CodeAnalysis.Document document, ITextSnapshot triggerSnapshot, CompletionService completionService, CompletionItem completionItem)
		{
			this.doc = document;
			this.triggerSnapshot = triggerSnapshot;
			this.completionService = completionService;
			CompletionItem = completionItem;
		}

		public override string GetDisplayDescription (bool isSelected)
		{
			if (CompletionItem.Properties.TryGetValue ("DescriptionMarkup", out string result))
				return result;
			return base.GetDisplayDescription (isSelected);
		}

		public override string GetRightSideDescription (bool isSelected)
		{
			if (CompletionItem.Properties.TryGetValue ("RightSideMarkup", out string result))
				return result;
			return null;
		}

		string [] completionType = {
			"field",
			"literal",
			"variable",
			"method",
			"name-space",
			"property",
			"event",
			"class",
			"delegate",
			"enum",
			"interface",
			"struct",
			"keyword",
			"template",
			"newmethod",
			"extensionmethod"
		};

		static Dictionary<string, int> roslynCompletionTypeTable = new Dictionary<string, int> {
			{ "Field", 0 },
			{ "Alias", 0 },
			{ "ArrayType", 0 },
			{ "Assembly", 0 },
			{ "DynamicType", 0 },
			{ "ErrorType", 0 },
			{ "Label", 0 },
			{ "NetModule", 0 },
			{ "PointerType", 0 },
			{ "RangeVariable", 0 },
			{ "TypeParameter", 0 },
			{ "Preprocessing", 0 },

			{ "Constant", 1 },
			{ "EnumMember", 1 },

			{ "Parameter", 2 },
			{ "Local", 2 },

			{ "Method", 3 },

			{ "Namespace", 4 },

			{ "Property", 5 },

			{ "Event", 6 },

			{ "Class", 7 },

			{ "Delegate", 8 },

			{ "Enum", 9 },

			{ "Interface", 10 },

			{ "Struct", 11 },
			{ "Structure", 11 },

			{ "Keyword", 12 },

			{ "Snippet", 13},

			{ "NewMethod", 14 },

			{ "ExtensionMethod", 15 }
		};

		int GetItemType ()
		{
			foreach (var tag in CompletionItem.Tags) {
				if (roslynCompletionTypeTable.TryGetValue (tag, out int result))
					return result;
			}
			LoggingService.LogWarning ("RoslynCompletionData: Can't find item type '" + string.Join (",", CompletionItem.Tags) + "'");
			return 1;
		}

		string [] modifierType = {
			"",
			"private-",
			"ProtectedOrInternal-",
			"protected-",
			"internal-",
			"ProtectedOrInternal-"
		};


		static Dictionary<string, int> modifierTypeTable = new Dictionary<string, int> {
			{ "Private", 1 },
			{ "ProtectedAndInternal", 2 },
			{ "Protected", 3 },
			{ "Internal", 4 },
			{ "ProtectedOrInternal", 5 }
		};

		int GetItemModifier ()
		{
			foreach (var tag in CompletionItem.Tags) {
				if (modifierTypeTable.TryGetValue (tag, out int result))
					return result;
			}
			return 0;
		}

		public override DisplayFlags DisplayFlags {
			get {
				DisplayFlags result = DisplayFlags.None;

				if (CompletionItem.Tags.Contains ("bold")) {
					result = DisplayFlags.MarkedBold;
				}

				return result;
			}
		}

		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
		{
			var document = IdeApp.Workbench.ActiveDocument;
			var editor = document?.Editor;
			if (editor == null || Provider == null) {
				base.InsertCompletionText (window, ref ka, descriptor);
				return;
			}
			var completionChange = Provider.GetChangeAsync (doc, CompletionItem, null, default (CancellationToken)).WaitAndGetResult (default (CancellationToken));

			var currentBuffer = editor.GetPlatformTextBuffer ();
			var textChange = completionChange.TextChange;
			var triggerSnapshotSpan = new SnapshotSpan (triggerSnapshot, new Span (textChange.Span.Start, textChange.Span.Length));
			var mappedSpan = triggerSnapshotSpan.TranslateTo (currentBuffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);
			using (var undo = editor.OpenUndoGroup ()) {
				// Work around for https://github.com/dotnet/roslyn/issues/22885
				if (editor.GetCharAt (mappedSpan.Start) == '@') {
					editor.ReplaceText (mappedSpan.Start + 1, mappedSpan.Length - 1, completionChange.TextChange.NewText);
				} else
					editor.ReplaceText (mappedSpan.Start, mappedSpan.Length, completionChange.TextChange.NewText);


				if (completionChange.NewPosition.HasValue)
					editor.CaretOffset = completionChange.NewPosition.Value;

				if (CompletionItem.Rules.FormatOnCommit) {
					var endOffset = mappedSpan.Start.Position + completionChange.TextChange.NewText.Length;
					Format (editor, document, mappedSpan.Start, endOffset);
				}
			}
		}

		protected abstract void Format (TextEditor editor, Gui.Document document, int start, int end);

		public override async Task<TooltipInformation> CreateTooltipInformation (bool smartWrap, CancellationToken cancelToken)
		{
			CompletionDescription description;
			if (CommonCompletionItem.HasDescription (CompletionItem)) {
				description = CommonCompletionItem.GetDescription (CompletionItem);
			} else {
				description = await Task.Run (() => completionService.GetDescriptionAsync (doc, CompletionItem)).ConfigureAwait (false);
			}
			var markup = StringBuilderCache.Allocate ();
			var theme = SyntaxHighlightingService.GetIdeFittingTheme (DefaultSourceEditorOptions.Instance.GetEditorTheme ());
			var taggedParts = description.TaggedParts;
			int i = 0;
			while (i < taggedParts.Length) {
				if (taggedParts [i].Tag == "LineBreak")
					break;
				i++;
			}
			if (i + 1 >= taggedParts.Length) {
				markup.AppendTaggedText (theme, taggedParts);
			} else {
				markup.AppendTaggedText (theme, taggedParts.Take (i));
				markup.Append ("<span font='" + FontService.SansFontName + "' size='small'>");
				markup.AppendLine ();
				markup.AppendLine ();
				markup.AppendTaggedText (theme, taggedParts.Skip (i + 1));
				markup.Append ("</span>");
			}
			return new TooltipInformation {
				SignatureMarkup = StringBuilderCache.ReturnAndFree (markup)
			};
		}

		public override bool IsCommitCharacter (char keyChar, string partialWord)
		{
			foreach (var rule in CompletionItem.Rules.CommitCharacterRules) {
				switch (rule.Kind) {
				case CharacterSetModificationKind.Add:
					if (rule.Characters.Contains (keyChar))
						return true;
					continue;
				case CharacterSetModificationKind.Remove:
					if (rule.Characters.Contains (keyChar))
						return false;
					continue;
				case CharacterSetModificationKind.Replace:
					return rule.Characters.Contains (keyChar);
				}
			}
			return base.IsCommitCharacter (keyChar, partialWord);
		}
	}
}