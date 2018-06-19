//
// AbstractOptionPreviewViewModel.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CodeStyle;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace MonoDevelop.Refactoring.Options
{
	internal abstract class AbstractOptionPreviewViewModel : AbstractNotifyPropertyChanged, IDisposable
	{
		private TextEditor _textViewHost;
		private PreviewWorkspace curWorkspace;
		private Microsoft.CodeAnalysis.Project project;

		public List<object> Items { get; set; }
		public ObservableCollection<AbstractCodeStyleOptionViewModel> CodeStyleItems { get; set; }

		public OptionSet Options { get; set; }
		private readonly OptionSet _originalOptions;

		protected AbstractOptionPreviewViewModel (OptionSet options, IServiceProvider serviceProvider, string language)
		{
			this.Options = options;
			_originalOptions = options;
			this.Items = new List<object> ();
			this.CodeStyleItems = new ObservableCollection<AbstractCodeStyleOptionViewModel> ();
			this.Language = language;
			_textViewHost = TextEditorFactory.CreateNewEditor ();
			_textViewHost.Options = DefaultSourceEditorOptions.PlainEditor;
		}

		internal OptionSet ApplyChangedOptions (OptionSet optionSet)
		{
			foreach (var optionKey in this.Options.GetChangedOptions (_originalOptions)) {
				optionSet = optionSet.WithChangedOption (optionKey, this.Options.GetOption (optionKey));
			}

			return optionSet;
		}

		public void SetOptionAndUpdatePreview<T> (T value, IOption option, string preview)
		{
			if (option is Option<CodeStyleOption<T>>) {
				var opt = Options.GetOption ((Option<CodeStyleOption<T>>)option);
				opt.Value = value;
				Options = Options.WithChangedOption ((Option<CodeStyleOption<T>>)option, opt);
			} else if (option is PerLanguageOption<CodeStyleOption<T>>) {
				var opt = Options.GetOption ((PerLanguageOption<CodeStyleOption<T>>)option, Language);
				opt.Value = value;
				Options = Options.WithChangedOption ((PerLanguageOption<CodeStyleOption<T>>)option, Language, opt);
			} else if (option is Option<T>) {
				Options = Options.WithChangedOption ((Option<T>)option, value);
			} else if (option is PerLanguageOption<T>) {
				Options = Options.WithChangedOption ((PerLanguageOption<T>)option, Language, value);
			} else {
				throw new InvalidOperationException ("Unexpected option type" + option);
			}

			UpdateDocument (preview);
		}

		public TextEditor TextViewHost {
			get {
				return _textViewHost;
			}

			private set {
				// make sure we close previous view.
				if (_textViewHost != null) {
					_textViewHost.Dispose ();
				}

				SetProperty (ref _textViewHost, value);
			}
		}

		public string Language { get; }

		public async void UpdatePreview (string text)
		{
			var workspace = new PreviewWorkspace (MonoDevelop.Ide.Composition.CompositionManager.Instance.HostServices);
			var fileName = string.Format ("project.{0}", Language == "C#" ? "csproj" : "vbproj");
			project = workspace.CurrentSolution.AddProject (fileName, "assembly.dll", Language);

			// use the mscorlib, system, and system.core that are loaded in the current process.
			string [] references =
				{
				"mscorlib",
				"System",
				"System.Core"
			};

			var metadataService = workspace.Services.GetService<IMetadataService> ();

			var referenceAssemblies = Thread.GetDomain ().GetAssemblies ()
				.Where (x => references.Contains (x.GetName (true).Name, StringComparer.OrdinalIgnoreCase))
				.Select (a => metadataService.GetReference (a.Location, MetadataReferenceProperties.Assembly));

			project = project.WithMetadataReferences (referenceAssemblies);

			var document = project.AddDocument ("document.cs", SourceText.From (text, Encoding.UTF8));
			var formatted = Formatter.FormatAsync (document, this.Options).WaitAndGetResult (CancellationToken.None);
			workspace.TryApplyChanges (project.Solution);

			TextViewHost.MimeType = "text/x-csharp";
			TextViewHost.Text = (await document.GetTextAsync ()).ToString ();
			TextViewHost.DocumentContext = new MyDocumentContext (workspace, document);

			TextViewHost.IsReadOnly = false;
			for (int i = 1; i <= TextViewHost.LineCount; i++) {
				var txt = TextViewHost.GetLineText (i);
				if (txt == "//[" || txt == "//]") {
					var line = TextViewHost.GetLine (i);
					TextViewHost.RemoveText (line.Offset, line.LengthIncludingDelimiter);
					i--;
				}
			}
			TextViewHost.IsReadOnly = true;
			if (curWorkspace != null) {
				curWorkspace.Dispose ();
			}

			this.curWorkspace = workspace;
		}

		class MyDocumentContext : DocumentContext
		{
			private PreviewWorkspace workspace;
			private Document document;

			public MyDocumentContext (PreviewWorkspace workspace, Document document)
			{
				this.workspace = workspace;
				this.document = document;
			}

			public override string Name => document.Name;

			public override MonoDevelop.Projects.Project Project => null;

			public override Document AnalysisDocument => document;

			public override ParsedDocument ParsedDocument => null;

			public override void AttachToProject (MonoDevelop.Projects.Project project)
			{
			}

			public override OptionSet GetOptionSet ()
			{
				return null;
			}

			public override void ReparseDocument ()
			{
			}

			public override Task<ParsedDocument> UpdateParseDocument ()
			{
				return new Task<ParsedDocument> (null);
			}
		}

		//private static List<LineSpan> GetExposedLineSpans (ITextSnapshot textSnapshot)
		//{
		//	const string start = "//[";
		//	const string end = "//]";

		//	var bufferText = textSnapshot.GetText ().ToString ();

		//	var lineSpans = new List<LineSpan> ();
		//	var lastEndIndex = 0;

		//	while (true) {
		//		var startIndex = bufferText.IndexOf (start, lastEndIndex, StringComparison.Ordinal);
		//		if (startIndex == -1) {
		//			break;
		//		}

		//		var endIndex = bufferText.IndexOf (end, lastEndIndex, StringComparison.Ordinal);

		//		var startLine = textSnapshot.GetLineNumberFromPosition (startIndex) + 1;
		//		var endLine = textSnapshot.GetLineNumberFromPosition (endIndex);

		//		lineSpans.Add (LineSpan.FromBounds (startLine, endLine));
		//		lastEndIndex = endIndex + end.Length;
		//	}

		//	return lineSpans;
		//}

		public void Dispose ()
		{
			if (_textViewHost != null) {
				_textViewHost.Dispose ();
				_textViewHost = null;
			}
			if (curWorkspace != null) {
				curWorkspace.Dispose ();
				curWorkspace = null;
			}
		}

		private void UpdateDocument (string text)
		{
			UpdatePreview (text);
		}

		//protected void AddParenthesesOption (
		//	string language, OptionSet optionSet,
		//	PerLanguageOption<CodeStyleOption<ParenthesesPreference>> languageOption,
		//	string title, string [] examples, bool isIgnoreOption)
		//{
		//	var preferences = new List<ParenthesesPreference> ();
		//	var codeStylePreferences = new List<CodeStylePreference> ();

		//	if (isIgnoreOption) {
		//		preferences.Add (ParenthesesPreference.Ignore);
		//		codeStylePreferences.Add (new CodeStylePreference (ServicesVSResources.Ignore, isChecked: false));
		//	} else {
		//		preferences.Add (ParenthesesPreference.AlwaysForClarity);
		//		codeStylePreferences.Add (new CodeStylePreference (ServicesVSResources.Always_for_clarity, isChecked: false));
		//	}

		//	preferences.Add (ParenthesesPreference.NeverIfUnnecessary);
		//	codeStylePreferences.Add (new CodeStylePreference (
		//		ServicesVSResources.Never_if_unnecessary,
		//		isChecked: false));

		//	CodeStyleItems.Add (new EnumCodeStyleOptionViewModel<ParenthesesPreference> (
		//		languageOption, language, title, preferences.ToArray (),
		//		examples, this, optionSet, ServicesVSResources.Parentheses_preferences_colon,
		//		codeStylePreferences));
		//}
	}
}
