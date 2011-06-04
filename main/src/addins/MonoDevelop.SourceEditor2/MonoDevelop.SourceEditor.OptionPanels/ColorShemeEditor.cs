// 
// ColorShemeEditor.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Gtk;

namespace MonoDevelop.SourceEditor.OptionPanels
{

	
	public partial class ColorShemeEditor : Gtk.Dialog
	{
		[Flags]
		enum ColorsAvailable {
			None = 0,
			Fg = 1, 
			Bg = 2,
			FontAttributes = 4,
			
			FgBg = Fg | Bg,
			Text = FgBg | FontAttributes
		
		};
		
		class ColorMetaData {
			public string Name {
				get;
				set; 
			}
			
			public string Description {
				get;
				set;
			}
			
			public ColorsAvailable ColorsAvailable {
				get;
				set;
			}
			
			public ColorMetaData (string name, string description, ColorsAvailable colorsAvailable) {
				this.Name = name;
				this.Description = description;
				this.ColorsAvailable = colorsAvailable;
			}

		}
		
		TextEditor textEditor;
		ColorSheme colorSheme;
		Gtk.TreeStore colorStore = new Gtk.TreeStore (typeof (string), typeof(ChunkStyle), typeof(ColorMetaData));
		string fileName;
		HighlightingPanel panel;
		
		static ColorMetaData[] metaData = new [] {
			new ColorMetaData ("text", GettextCatalog.GetString ("Text"), ColorsAvailable.FgBg),
			new ColorMetaData ("text.selection", GettextCatalog.GetString ("Selected text"), ColorsAvailable.FgBg),
			new ColorMetaData ("text.selection.inactive", GettextCatalog.GetString ("Non focused selected text"), ColorsAvailable.FgBg),
			
			new ColorMetaData ("text.background.searchresult", GettextCatalog.GetString ("Background of search results"), ColorsAvailable.Fg),
			new ColorMetaData ("text.background.searchresult-main", GettextCatalog.GetString ("Background of current search results"), ColorsAvailable.Fg),
			
			
			new ColorMetaData ("text.background.readonly", GettextCatalog.GetString ("Background of read only text"), ColorsAvailable.Fg),
			
			new ColorMetaData ("linenumber", GettextCatalog.GetString ("Line numbers"), ColorsAvailable.FgBg),
			new ColorMetaData ("linenumber.highlight", GettextCatalog.GetString ("Current line number"), ColorsAvailable.FgBg),
			
			
			new ColorMetaData ("iconbar", GettextCatalog.GetString ("Icon bar"), ColorsAvailable.Fg),
			new ColorMetaData ("iconbar.separator", GettextCatalog.GetString ("Icon bar separator"), ColorsAvailable.Fg),
			
			new ColorMetaData ("fold", GettextCatalog.GetString ("Folding colors"), ColorsAvailable.FgBg),
			new ColorMetaData ("fold.highlight", GettextCatalog.GetString ("Current fold marker colors"), ColorsAvailable.FgBg),
			new ColorMetaData ("fold.togglemarker", GettextCatalog.GetString ("Folding toggle marker"), ColorsAvailable.Fg),
			
			
			new ColorMetaData ("marker.line", GettextCatalog.GetString ("Current line marker"), ColorsAvailable.Fg),
			new ColorMetaData ("marker.ruler", GettextCatalog.GetString ("Ruler"), ColorsAvailable.Fg),
			
			new ColorMetaData ("marker.line.changed", GettextCatalog.GetString ("Quick diff line changed"), ColorsAvailable.Fg),
			new ColorMetaData ("marker.line.dirty", GettextCatalog.GetString ("Quick diff line dirty"), ColorsAvailable.Fg),
			
			new ColorMetaData ("marker.whitespace", GettextCatalog.GetString ("Whitespace marker"), ColorsAvailable.Fg),
			new ColorMetaData ("marker.whitespace.eol", GettextCatalog.GetString ("Eol marker"), ColorsAvailable.Fg),
			
			new ColorMetaData ("marker.invalidline", GettextCatalog.GetString ("Invalid line marker"), ColorsAvailable.Fg),
			
			new ColorMetaData ("marker.bracket", GettextCatalog.GetString ("Bracket marker"), ColorsAvailable.FgBg),
			
			
			new ColorMetaData ("marker.bookmark.color1", GettextCatalog.GetString ("Bookmark marker color 1"), ColorsAvailable.Fg),
			new ColorMetaData ("marker.bookmark.color2", GettextCatalog.GetString ("Bookmark marker color 2"), ColorsAvailable.Fg),
			
			new ColorMetaData ("marker.breakpoint.invalid", GettextCatalog.GetString ("Invalid breakpoint background"), ColorsAvailable.Fg),
			new ColorMetaData ("marker.breakpoint.invalid.color1", GettextCatalog.GetString ("Invalid breakpoint marker"), ColorsAvailable.Fg),
			new ColorMetaData ("marker.breakpoint.invalid.border", GettextCatalog.GetString ("Invalid breakpoint marker border"), ColorsAvailable.Fg),
			
			new ColorMetaData ("marker.breakpoint.color1", GettextCatalog.GetString ("Breakpoint marker color 1"), ColorsAvailable.Fg),
			new ColorMetaData ("marker.breakpoint.color2", GettextCatalog.GetString ("Breakpoint marker color 2"), ColorsAvailable.Fg),
			
			new ColorMetaData ("marker.breakpoint", GettextCatalog.GetString ("Breakpoint line"), ColorsAvailable.FgBg),
			
			new ColorMetaData ("marker.breakpoint.color1", GettextCatalog.GetString ("Breakpoint marker color 1"), ColorsAvailable.Fg),
			new ColorMetaData ("marker.breakpoint.color2", GettextCatalog.GetString ("Breakpoint marker color 2"), ColorsAvailable.Fg),
			
			new ColorMetaData ("marker.debug.currentline", GettextCatalog.GetString ("Current line (debugger)"), ColorsAvailable.FgBg),
			new ColorMetaData ("marker.debug.currentline.color1", GettextCatalog.GetString ("Current line (debugger) marker color 1"), ColorsAvailable.Fg),
			new ColorMetaData ("marker.debug.currentline.color2", GettextCatalog.GetString ("Current line (debugger) marker color 2"), ColorsAvailable.Fg),
			
			new ColorMetaData ("marker.debug.stackline", GettextCatalog.GetString ("Debugger stack line"), ColorsAvailable.FgBg),
			new ColorMetaData ("marker.debug.stackline.color1", GettextCatalog.GetString ("Debugger stack line marker color 1"), ColorsAvailable.Fg),
			new ColorMetaData ("marker.debug.stackline.color2", GettextCatalog.GetString ("Debugger stack line marker color 2"), ColorsAvailable.Fg),
		
			new ColorMetaData ("marker.underline.error", GettextCatalog.GetString ("Error underline"), ColorsAvailable.Fg),
			new ColorMetaData ("marker.underline.warning", GettextCatalog.GetString ("Warning underline"), ColorsAvailable.Fg),
			new ColorMetaData ("marker.underline.suggestion", GettextCatalog.GetString ("Suggestion underline"), ColorsAvailable.Fg),
			new ColorMetaData ("marker.underline.hint", GettextCatalog.GetString ("Hint underline"), ColorsAvailable.Fg),
			
			new ColorMetaData ("marker.template.primary_template", GettextCatalog.GetString ("Primary link (text link mode)"), ColorsAvailable.FgBg),
			new ColorMetaData ("marker.template.secondary_template", GettextCatalog.GetString ("Secondary link (text link mode)"), ColorsAvailable.FgBg),
			new ColorMetaData ("marker.template.secondary_highlighted_template", GettextCatalog.GetString ("Current link (text link mode)"), ColorsAvailable.FgBg),
			
			
			new ColorMetaData ("bubble.warning", GettextCatalog.GetString ("Message bubble warning"), ColorsAvailable.FgBg),
			new ColorMetaData ("bubble.warning.text", GettextCatalog.GetString ("Message bubble warning text"), ColorsAvailable.Fg),
			
			new ColorMetaData ("bubble.error", GettextCatalog.GetString ("Message bubble error"), ColorsAvailable.FgBg),
			new ColorMetaData ("bubble.error.text", GettextCatalog.GetString ("Message bubble error text"), ColorsAvailable.Fg),
			
			new ColorMetaData ("template", GettextCatalog.GetString ("T4 template"), ColorsAvailable.FgBg),
			new ColorMetaData ("template.tag", GettextCatalog.GetString ("T4 template tag"), ColorsAvailable.FgBg),
			new ColorMetaData ("template.directive", GettextCatalog.GetString ("T4 template direction"), ColorsAvailable.FgBg),

			new ColorMetaData ("diff.line-added", GettextCatalog.GetString ("Diff line added"), ColorsAvailable.FgBg),
			new ColorMetaData ("diff.line-removed", GettextCatalog.GetString ("Diff line removed"), ColorsAvailable.FgBg),
			new ColorMetaData ("diff.line-changed", GettextCatalog.GetString ("Diff line changed"), ColorsAvailable.FgBg),
			
			new ColorMetaData ("diff.header", GettextCatalog.GetString ("Diff line header"), ColorsAvailable.Text),
			new ColorMetaData ("diff.header-seperator", GettextCatalog.GetString ("Diff line header separator"), ColorsAvailable.Text),
			new ColorMetaData ("diff.header-oldfile", GettextCatalog.GetString ("Diff line header old file"), ColorsAvailable.Text),
			new ColorMetaData ("diff.header-newfile", GettextCatalog.GetString ("Diff line header new file"), ColorsAvailable.Text),
			new ColorMetaData ("diff.location", GettextCatalog.GetString ("Diff line header location"), ColorsAvailable.Text),
			
			// Keywords
			new ColorMetaData ("text.punctuation", GettextCatalog.GetString ("Punctuation"), ColorsAvailable.Text),
			new ColorMetaData ("text.link", GettextCatalog.GetString ("Links"), ColorsAvailable.Text),
			new ColorMetaData ("text.preprocessor", GettextCatalog.GetString ("Pre processor directive text"), ColorsAvailable.Text),
			new ColorMetaData ("text.preprocessor.keyword", GettextCatalog.GetString ("Pre processor keywords"), ColorsAvailable.Text),
			new ColorMetaData ("text.markup", GettextCatalog.GetString ("Text markup"), ColorsAvailable.Text),
			new ColorMetaData ("text.markup.tag", GettextCatalog.GetString ("Text markup tags"), ColorsAvailable.Text),
			
			new ColorMetaData ("comment", GettextCatalog.GetString ("Comments"), ColorsAvailable.Text),
			new ColorMetaData ("comment.line", GettextCatalog.GetString ("Line comments"), ColorsAvailable.Text),
			new ColorMetaData ("comment.block", GettextCatalog.GetString ("Block comments"), ColorsAvailable.Text),
			new ColorMetaData ("comment.doc", GettextCatalog.GetString ("Doc comments"), ColorsAvailable.Text),
			new ColorMetaData ("comment.tag", GettextCatalog.GetString ("Comment tags"), ColorsAvailable.Text),
			new ColorMetaData ("comment.tag.line", GettextCatalog.GetString ("Line comment tags"), ColorsAvailable.Text),
			new ColorMetaData ("comment.tag.block", GettextCatalog.GetString ("Block comment tags"), ColorsAvailable.Text),
			new ColorMetaData ("comment.tag.doc", GettextCatalog.GetString ("Doc comment tags"), ColorsAvailable.Text),
			new ColorMetaData ("comment.keyword", GettextCatalog.GetString ("Comment keywords"), ColorsAvailable.Text),
			new ColorMetaData ("comment.keyword.todo", GettextCatalog.GetString ("TODO in comments"), ColorsAvailable.Text),
			
			new ColorMetaData ("constant", GettextCatalog.GetString ("Constant literals"), ColorsAvailable.Text),
			new ColorMetaData ("constant.digit", GettextCatalog.GetString ("Digit literals"), ColorsAvailable.Text),
			new ColorMetaData ("constant.language", GettextCatalog.GetString ("Language constants"), ColorsAvailable.Text),
			new ColorMetaData ("constant.language.void", GettextCatalog.GetString ("'void' keywords"), ColorsAvailable.Text),
			
			new ColorMetaData ("string", GettextCatalog.GetString ("String literals"), ColorsAvailable.Text),
			new ColorMetaData ("string.single", GettextCatalog.GetString ("Single quote strings"), ColorsAvailable.Text),
			new ColorMetaData ("string.double", GettextCatalog.GetString ("Double quote strings"), ColorsAvailable.Text),
			new ColorMetaData ("string.other", GettextCatalog.GetString ("Other strings"), ColorsAvailable.Text),
			
			new ColorMetaData ("keyword.semantic.type", GettextCatalog.GetString ("Types (semantic)"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.semantic.field", GettextCatalog.GetString ("Field (semantic)"), ColorsAvailable.Text),
			
			new ColorMetaData ("keyword", GettextCatalog.GetString ("Keywords"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.access", GettextCatalog.GetString ("Access keywords"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.operator", GettextCatalog.GetString ("Operatork eywords"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.operator.declaration", GettextCatalog.GetString ("Operator declaration keywords"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.selection", GettextCatalog.GetString ("Selection keywords"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.iteration", GettextCatalog.GetString ("Iteration keywords"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.jump", GettextCatalog.GetString ("Jump keywords"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.context", GettextCatalog.GetString ("Context keywords"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.exceptions", GettextCatalog.GetString ("Exception keywords"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.modifier", GettextCatalog.GetString ("Modifier keywords"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.type", GettextCatalog.GetString ("Type keywords"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.namespace", GettextCatalog.GetString ("Namespace keywords"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.property", GettextCatalog.GetString ("Property keywords"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.declaration", GettextCatalog.GetString ("Declaration keywords"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.parameter", GettextCatalog.GetString ("Parameter keywords"), ColorsAvailable.Text),
			new ColorMetaData ("keyword.misc", GettextCatalog.GetString ("Misc. keywords"), ColorsAvailable.Text),
		};
		
		public ColorShemeEditor (HighlightingPanel panel)
		{
			this.panel = panel;
			this.Build ();
			textEditor = new TextEditor ();
			textEditor.Options = DefaultSourceEditorOptions.Instance;
			this.scrolledwindowTextEditor.Child = textEditor;
			textEditor.ShowAll ();
			
			this.treeviewColors.AppendColumn (GettextCatalog.GetString ("Name"), new Gtk.CellRendererText (), new CellLayoutDataFunc (SyntaxCellRenderer));
			this.treeviewColors.HeadersVisible = false;
			this.treeviewColors.Model = colorStore;
			this.treeviewColors.Selection.Changed += HandleTreeviewColorsSelectionChanged;
			this.colorbuttonFg.ColorSet += Stylechanged;
			this.colorbuttonBg.ColorSet += Stylechanged;
			colorbuttonBg.UseAlpha = true;
			this.checkbuttonBold.Toggled += Stylechanged;
			this.checkbuttonItalic.Toggled += Stylechanged;
			
			this.buttonOk.Clicked += HandleButtonOkClicked;
			HandleTreeviewColorsSelectionChanged (null, null);
			
		}

		void SyntaxCellRenderer (Gtk.CellLayout cell_layout, Gtk.CellRenderer cell, Gtk.TreeModel tree_model, Gtk.TreeIter iter)
		{
			var renderer = (Gtk.CellRendererText)cell;
			var data = (ColorMetaData)colorStore.GetValue (iter, 2);
			var style = (ChunkStyle)colorStore.GetValue (iter, 1);
			string markup = GLib.Markup.EscapeText (data.Description);
			if (style.Bold)
				markup = "<b>" + markup + "</b>";
			if (style.Italic)
				markup = "<i>" + markup + "</i>";
			renderer.Markup = markup;
			if (data.ColorsAvailable == ColorsAvailable.Text || data.ColorsAvailable == ColorsAvailable.FgBg) {
				renderer.ForegroundGdk = style.Color;
				renderer.BackgroundGdk = style.GotBackgroundColorAssigned ? style.BackgroundColor : Style.Base (StateType.Normal);
			} else {
				var b = Math.Abs (HslColor.Brightness (style.Color) - HslColor.Brightness (Style.Text (StateType.Normal)));
				Console.WriteLine (data.Description  + ":" + b);
				renderer.ForegroundGdk = b < 0.4 ? Style.Background (StateType.Normal) : Style.Text (StateType.Normal);
				renderer.BackgroundGdk = style.Color;
			}
			
		}

		void ApplyStyle (ColorSheme sheme)
		{
			sheme.Name = entryName.Text;
			sheme.Description = entryDescription.Text;
			
			Gtk.TreeIter iter;
			if (colorStore.GetIterFirst (out iter)) {
				do {
					var data = (ColorMetaData)colorStore.GetValue (iter, 2);
					var style = (ChunkStyle)colorStore.GetValue (iter, 1);
					sheme.SetChunkStyle (data.Name, style);
				} while (colorStore.IterNext (ref iter));
			}
		}

		void HandleButtonOkClicked (object sender, EventArgs e)
		{
			ApplyStyle (colorSheme);
			try {
				colorSheme.Save (fileName);
				panel.ShowStyles ();
			} catch (Exception ex) {
				MessageService.ShowException (ex);
			}
		}

		void Stylechanged (object sender, EventArgs e)
		{
			Gtk.TreeIter iter;
			if (!this.treeviewColors.Selection.GetSelected (out iter))
				return;
			ChunkProperties prop = ChunkProperties.None;
			if (checkbuttonBold.Active)
				prop |= ChunkProperties.Bold;
			if (checkbuttonItalic.Active)
				prop |= ChunkProperties.Italic;
			ChunkStyle oldStyle = (ChunkStyle)colorStore.GetValue (iter, 1);
			bool useBgColor = colorbuttonBg.Alpha > 0 && (colorbuttonBg.Color.Pixel != oldStyle.BackgroundColor.Pixel || oldStyle.GotBackgroundColorAssigned);
			colorStore.SetValue (iter, 1, useBgColor? new ChunkStyle (colorbuttonFg.Color, colorbuttonBg.Color, prop) : new ChunkStyle (colorbuttonFg.Color, prop));
			
			var newStyle = colorSheme.Clone ();
			ApplyStyle (newStyle);
			this.textEditor.TextViewMargin.PurgeLayoutCache ();
			this.textEditor.Document.MimeType = "text/x-csharp";
			this.textEditor.GetTextEditorData ().ColorStyle = newStyle;
			this.textEditor.QueueDraw ();
			
		}

		void HandleTreeviewColorsSelectionChanged (object sender, EventArgs e)
		{
			this.colorbuttonBg.Sensitive = false;
			this.colorbuttonFg.Sensitive = false;
			this.checkbuttonBold.Sensitive = false;
			this.checkbuttonItalic.Sensitive = false;
			
			Gtk.TreeIter iter;
			if (!this.treeviewColors.Selection.GetSelected (out iter))
				return;
			var chunkStyle = (ChunkStyle)colorStore.GetValue (iter, 1);
			var data = (ColorMetaData)colorStore.GetValue (iter, 2);
			colorbuttonFg.Color = chunkStyle.Color;
			colorbuttonBg.Color = chunkStyle.BackgroundColor;
			checkbuttonBold.Active = chunkStyle.Bold;
			checkbuttonItalic.Active = chunkStyle.Italic;
			
			this.label4.Visible = this.colorbuttonFg.Visible = (data.ColorsAvailable & ColorsAvailable.Fg) != 0;
			this.colorbuttonFg.Sensitive = true;
			
			this.label5.Visible = this.colorbuttonBg.Visible = (data.ColorsAvailable & ColorsAvailable.Bg) != 0;
			this.colorbuttonBg.Sensitive = true;
			this.colorbuttonBg.Alpha = chunkStyle.GotBackgroundColorAssigned ? ushort.MaxValue : (ushort)0;
			
			this.checkbuttonBold.Visible = (data.ColorsAvailable & ColorsAvailable.FontAttributes) != 0;
			this.checkbuttonBold.Sensitive = true;
			
			this.checkbuttonItalic.Visible = (data.ColorsAvailable & ColorsAvailable.FontAttributes) != 0;
			this.checkbuttonItalic.Sensitive = true;
		}
		
		public void SetSheme (ColorSheme style)
		{
			if (style == null)
				throw new ArgumentNullException ("style");
			this.fileName = Mono.TextEditor.Highlighting.SyntaxModeService.GetFileNameForStyle (style);
			this.colorSheme = style;
			this.entryName.Text = style.Name;
			this.entryDescription.Text = style.Description;
			this.textEditor.Document.MimeType = "text/x-csharp";
			this.textEditor.GetTextEditorData ().ColorStyle = style;
			this.textEditor.Text = @"using System;

// This is an example
class Example
{
	public static void Main (string[] args)
	{
		Console.WriteLine (""Hello World"");
	}
}";
			foreach (var data in metaData) {
				colorStore.AppendValues (data.Description, style.GetChunkStyle (data.Name), data);
			}
			Stylechanged (null, null);
		}
	}
}

