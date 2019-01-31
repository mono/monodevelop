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
using System.ComponentModel.Composition;

using AppKit;
using ObjCRuntime;

using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Projects;

namespace MonoDevelop.TextEditor
{
	[Export (typeof (EditorFormatDefinition))]
	[ClassificationType (ClassificationTypeNames = ClassificationTypeNames.Text)]
	[Name (ClassificationTypeNames.Text)]
	[Order (After = Priority.Default, Before = Priority.High)]
	[UserVisible (true)]
	class ClassificationFormatDefinitionFromPreferences : ClassificationFormatDefinition
	{
		public ClassificationFormatDefinitionFromPreferences ()
		{
			nfloat fontSize = -1;
			var fontName = Ide.Editor.DefaultSourceEditorOptions.Instance.FontName;

			if (!string.IsNullOrEmpty (fontName)) {
				var sizeStartOffset = fontName.LastIndexOf (' ');
				if (sizeStartOffset >= 0) {
					nfloat.TryParse (fontName.Substring (sizeStartOffset + 1), out fontSize);
					fontName = fontName.Substring (0, sizeStartOffset);
				}
			}

			if (string.IsNullOrEmpty (fontName))
				fontName = "Menlo";

			if (fontSize <= 1)
				fontSize = 12;

			FontTypeface = NSFontWorkarounds.FromFontName (fontName, fontSize);
		}
	}

	class CocoaTextViewContent : TextViewContent<ICocoaTextView, CocoaTextViewImports>
	{
		ICocoaTextViewHost textViewHost;
		NSView textViewHostControl;

		sealed class EmbeddedNSViewControl : Control
		{
			readonly NSView nsView;

			public EmbeddedNSViewControl (NSView nsView)
				=> this.nsView = nsView;

			protected override object CreateNativeWidget<T> ()
				=> nsView;
		}

		public CocoaTextViewContent (CocoaTextViewImports imports, FilePath fileName, string mimeType, Project ownerProject)
			: base (imports, fileName, mimeType, ownerProject)
		{
		}

		protected override ICocoaTextView CreateTextView (ITextViewModel viewModel, ITextViewRoleSet roles)
			=> Imports.TextEditorFactoryService.CreateTextView (viewModel, roles, Imports.EditorOptionsFactoryService.GlobalOptions);

		protected override ITextViewRoleSet GetAllPredefinedRoles ()
			=> Imports.TextEditorFactoryService.AllPredefinedRoles;

		protected override Control CreateControl ()
		{
			textViewHost = Imports.TextEditorFactoryService.CreateTextViewHost (TextView, setFocus: true);
			textViewHostControl = textViewHost.HostControl;

			var control = new EmbeddedNSViewControl (textViewHostControl);
			control.GetNativeWidget<Gtk.Widget> ().CanFocus = true;
			TextView.GotAggregateFocus += (sender, e) => {
				control.GetNativeWidget<Gtk.Widget> ().GrabFocus ();
			};
			TextView.Properties.AddProperty (typeof (Gtk.Widget), control.GetNativeWidget<Gtk.Widget> ());

			return control;
		}

		public override void Dispose ()
		{
			base.Dispose ();

			if (textViewHost != null) {
				textViewHost.Close ();
				textViewHost = null;
			}
		}

		protected override void InstallAdditionalEditorOperationsCommands ()
		{
			base.InstallAdditionalEditorOperationsCommands ();

			EditorOperationCommands.Add (SearchCommands.Find, new EditorOperationCommand (
				_ => HandleTextFinderAction (
					TextFinderAction.ShowFindInterface,
					perform: true),
				(_, info) => info.Enabled = HandleTextFinderAction (
					TextFinderAction.ShowFindInterface,
					perform: false)));

			EditorOperationCommands.Add (SearchCommands.Replace, new EditorOperationCommand (
				_ => HandleTextFinderAction (
					TextFinderAction.ShowReplaceInterface,
					perform: true),
				(_, info) => info.Enabled = HandleTextFinderAction (
					TextFinderAction.ShowReplaceInterface,
					perform: false)));

			bool HandleTextFinderAction (TextFinderAction action, bool perform)
			{
				var responder = textViewHostControl?.Window?.FirstResponder;

				if (responder != null && responder.RespondsToSelector (action.Action)) {
					if (perform)
						responder.PerformTextFinderAction (action);
					return true;
				}

				return false;
			}
		}

		sealed class TextFinderAction : Foundation.NSObject, INSValidatedUserInterfaceItem
		{
			public static readonly TextFinderAction ShowFindInterface
				= new TextFinderAction (NSTextFinderAction.ShowFindInterface);

			public static readonly TextFinderAction ShowReplaceInterface
				= new TextFinderAction (NSTextFinderAction.ShowReplaceInterface);

			public Selector Action { get; } = new Selector ("performTextFinderAction:");
			public nint Tag { get; }

			TextFinderAction (IntPtr handle) : base (handle)
			{
			}

			TextFinderAction (NSTextFinderAction action)
				=> Tag = (int)action;
		}
	}
}