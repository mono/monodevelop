//
// RenamePopup.cs
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
using MonoDevelop.Components;
using Microsoft.CodeAnalysis.LanguageServices;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor.Highlighting;
using RefactoringEssentials;
using MonoDevelop.Core;

namespace MonoDevelop.Refactoring.Rename
{
	class RenamePopup : XwtThemedPopup
	{
		Xwt.TextEntry entry = new Xwt.TextEntry();
		bool didShowError = true;

		public string ItemName {
			get {
				return entry.Text;
			}
			set {
				entry.Text = value;
				entry.SelectionStart = 0;
				entry.SelectionLength = value.Length;
			}
		}

		bool RenameFile {
			get {
				foreach (var loc in symbol.Locations) {
					if (loc.IsInSource) {
						if (loc.SourceTree == null ||
							!System.IO.File.Exists(loc.SourceTree.FilePath) ||
							GeneratedCodeRecognition.IsFileNameForGeneratedCode(loc.SourceTree.FilePath))
							continue;
						var oldName = System.IO.Path.GetFileNameWithoutExtension(loc.SourceTree.FilePath);
						if (RenameRefactoring.IsCompatibleForRenaming(oldName, symbol.Name))
							return true;
					}
				}
				return false;
			}
		}

		RenameRefactoring.RenameProperties Properties {
			get {
				return new RenameRefactoring.RenameProperties() {
					NewName = ItemName,
					RenameFile = this.RenameFile,
					IncludeOverloads = true
				};
			}
		}

		readonly ISymbol symbol;

		public RenamePopup(RenameRefactoring refactoring, ISymbol symbol)
		{
			this.symbol = symbol;
			Content = entry;

			var scheme = SyntaxHighlightingService.GetEditorTheme(IdeApp.Preferences.ColorScheme);
			Theme.SetSchemeColors(scheme);

			entry.Changed += delegate {
				var syntaxFactsService = TypeSystemService.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<ISyntaxFactsService>();
				var isValid = syntaxFactsService.IsValidIdentifier(ItemName);

				if (isValid) {
					if (scheme.TryGetColor(EditorThemeColors.TooltipBackground, out HslColor bgColor))
						Theme.BackgroundColor = bgColor;
					IdeApp.Workbench.StatusBar.ShowReady();
					didShowError = false;
				} else {
					if (scheme.TryGetColor(EditorThemeColors.UnderlineError, out HslColor errorColor)) {
						Theme.BackgroundColor = errorColor;
					}
					if (!didShowError) {
						didShowError = true;
						IdeApp.Workbench.StatusBar.ShowError(GettextCatalog.GetString("Invalid name"));
					}
				}
			};

			entry.KeyPressed += async (o, args) => {
				switch (args.Key) {
					case Xwt.Key.Escape:
						Destroy();
						break;
					case Xwt.Key.Return:
						if (didShowError)
							break;
						var changes = await refactoring.PerformChangesAsync(symbol, this.Properties);
						ProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor(Title, null);
						RefactoringService.AcceptChanges(monitor, changes);
						Destroy();
						break;
				}
			};

			var window = BackendHost.ToolkitEngine.GetNativeWindow(this) as Gtk.Window;
			if (window != null) {
				window.Modal = true;
				window.Focus = (Gtk.Widget)BackendHost.ToolkitEngine.GetNativeWidget(entry);
			}
			entry.SetFocus();
			Content.BoundsChanged += Content_BoundsChanged;
			Show();
			Content_BoundsChanged(null, EventArgs.Empty);
		}

		void Content_BoundsChanged(object sender, EventArgs e)
		{
			var provider = IdeApp.Workbench.ActiveDocument.GetContent<Mono.TextEditor.ITextEditorDataProvider>();

			var geometry = Visible ? Screen.VisibleBounds : Xwt.MessageDialog.RootWindow.Screen.VisibleBounds;
			var lastW = (int)Width;
			var lastH = (int)Height;

			var data = provider.GetTextEditorData();
			var editor = data.Parent;
			int offset = data.Caret.Offset;
			while (offset > 0) {
				var ch = data.GetCharAt(offset - 1);
				if (char.IsLetterOrDigit(ch) || ch == '_') {
					offset--;
					continue;
				}
				break;
			}
			var p = editor.LocationToPoint(editor.OffsetToLocation(offset));

			var x = (int)(p.X - editor.HAdjustment.Value);
			var y = (int)(p.Y - editor.VAdjustment.Value);
			editor.GdkWindow.GetOrigin(out int ox, out int oy);

			x += ox;
			y += oy + (int)editor.LineHeight;

			if (x + lastW > geometry.Right)
				x = (int)geometry.Right - (int)lastW;
			if (y + lastH > geometry.Bottom)
				y -= (int)(editor.LineHeight - lastH - 4);

			Location = new Xwt.Point(x, y);
		}
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (didShowError)
				IdeApp.Workbench.StatusBar.ShowReady();
		}
	}
}
