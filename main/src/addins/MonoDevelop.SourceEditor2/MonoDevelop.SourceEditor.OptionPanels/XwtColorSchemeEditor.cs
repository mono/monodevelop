//
// XwtColorSchemeEditor.cs
//
// Author:
//       Aleksandr Shevchenko <alexandre.shevchenko@gmail.com>
//
// Copyright (c) 2014 Aleksandr Shevchenko
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
using Xwt;
using Xwt.Drawing;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using System.Linq;
using System.Collections.Generic;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	public partial class XwtColorSchemeEditor:Dialog
	{
		public XwtColorSchemeEditor (HighlightingPanel panel)
		{
			this.panel = panel;
			Build ();
			this.textEditor.ShowAll ();
			TreeviewColorsSelectionChanged (null, null);
		}

		void SearchTextChanged (object sender, EventArgs e)
		{
			var searchText = searchEntry.Text;
			var searchStore = CreateStoreByQuery (searchText);
			this.treeviewColors.DataSource = searchStore;
			this.treeviewColors.ExpandAll ();
		}

		ITreeDataSource CreateStoreByQuery (string searchText)
		{
			colorStore.Clear ();
			var treeStore = colorStore;
			var styleCollection = ColorScheme.TextColors
				.Concat (ColorScheme.AmbientColors)
				.Where (desc => desc.Attribute.Name.ToLower ()
					.Contains (searchText.ToLower ()));

			FillTreeStore (treeStore, styleCollection, this.colorScheme);
			return treeStore;
		}

		void CanUndoRedoChanged (object sender, EventArgs e)
		{
			undoButton.Sensitive = history.CanUndo;
			redoButton.Sensitive = history.CanRedo;
		}

		void Undo (object sender, EventArgs e)
		{
			if (!history.CanUndo)
				return;

			history.Undo ();
			var pos = treeviewColors.SelectedRow;
			if (pos == null) {
				ApplyNewScheme (GroupNames.Other);
				return;
			}

			var navigator = colorStore.GetNavigatorAt (pos);
			var groupName = GetGroupNameFromNode (navigator);
			ApplyNewScheme (groupName);
		}

		void Redo (object sender, EventArgs e)
		{
			if (!history.CanRedo)
				return;

			history.Redo ();
			var pos = treeviewColors.SelectedRow;
			if (pos == null) {
				ApplyNewScheme (GroupNames.Other);
				return;
			}

			var navigator = colorStore.GetNavigatorAt (pos);
			var groupName = GetGroupNameFromNode (navigator);
			ApplyNewScheme (groupName);
		}

		void TreeviewColorsSelectionChanged (object sender, EventArgs e)
		{
			this.colorbuttonPrimary.Sensitive = false;
			this.colorbuttonSecondary.Sensitive = false;
			this.colorbuttonBorder.Sensitive = false;
			this.togglebuttonBold.Sensitive = false;
			this.togglebuttonItalic.Sensitive = false;

			TreePosition pos = treeviewColors.SelectedRow;
			if (pos == null)
				return;

			var navigator = colorStore.GetNavigatorAt (pos);
			var o = navigator.GetValue (styleField);

			if (o is ChunkStyle)
				ChunkStyleSelected ((ChunkStyle)o);

			if (o is AmbientColor)
				AmbientColorSelected ((AmbientColor)o);

			var groupName = GetGroupNameFromNode (navigator);
			ApplyNewScheme (groupName);
		}

		void ChunkStyleSelected (ChunkStyle chunkStyle)
		{
			handleUIEvents = false;

			SetColorToButton (colorbuttonPrimary, chunkStyle.Foreground);
			SetColorToButton (colorbuttonSecondary, chunkStyle.Background);

			this.togglebuttonBold.Active = chunkStyle.FontWeight == Xwt.Drawing.FontWeight.Bold;
			this.togglebuttonItalic.Active = chunkStyle.FontStyle == Xwt.Drawing.FontStyle.Italic;

			this.colorbuttonPrimary.LabelText = "Foreground";
			this.colorbuttonSecondary.LabelText = "Background";

			this.colorbuttonPrimary.Sensitive = true;
			this.colorbuttonSecondary.Sensitive = true;
			this.togglebuttonBold.Sensitive = true;
			this.togglebuttonItalic.Sensitive = true;

			this.togglebuttonBold.Visible = true;
			this.togglebuttonItalic.Visible = true;
			this.colorbuttonBorder.Visible = false;

			handleUIEvents = true;
		}

		void AmbientColorSelected (AmbientColor ambientColor)
		{
			handleUIEvents = false;

			SetColorToButton (colorbuttonPrimary, ambientColor.Color);
			SetColorToButton (colorbuttonSecondary, ambientColor.SecondColor);
			SetColorToButton (colorbuttonBorder, ambientColor.BorderColor);

			this.colorbuttonPrimary.Sensitive = true;
			this.colorbuttonSecondary.Sensitive = ambientColor.HasSecondColor;
			this.colorbuttonBorder.Sensitive = ambientColor.HasBorderColor;

			this.colorbuttonBorder.Visible = true;
			this.togglebuttonBold.Visible = false;
			this.togglebuttonItalic.Visible = false;

			this.colorbuttonPrimary.LabelText = "Primary";
			this.colorbuttonSecondary.LabelText = "Secondary";

			handleUIEvents = true;
		}

		void SetColorToButton (LabeledColorButton button, Cairo.Color color)
		{
			button.Color = new Color (color.R, color.G, color.B, color.A);
		}

		void StyleChanged (object sender, EventArgs e)
		{
			if (!handleUIEvents)
				return;

			TreePosition pos = treeviewColors.SelectedRow;
			if (pos == null)
				return;

			var navigator = colorStore.GetNavigatorAt (pos);
			var o = navigator.GetValue (styleField);

			if (o is ChunkStyle)
				ChangeChunkStyle (navigator, (ChunkStyle)o);
			else if (o is AmbientColor)
				ChangeAmbientColor (navigator, (AmbientColor)o);
		}

		void ChangeChunkStyle (TreeNavigator navigator, ChunkStyle oldStyle)
		{
			var newStyle = new ChunkStyle (oldStyle);
			newStyle.Foreground = GetColorFromButton (colorbuttonPrimary);
			newStyle.Background = GetColorFromButton (colorbuttonSecondary);

			newStyle.FontWeight = togglebuttonBold.Active
				? FontWeight.Bold
				: FontWeight.Normal;

			newStyle.FontStyle = togglebuttonItalic.Active 
				? FontStyle.Italic 
				: FontStyle.Normal;

			if (handleUIEvents)
				history.AddCommand (new ChangeChunkStyleCommand (oldStyle, newStyle, navigator));

			var groupName = GetGroupNameFromNode (navigator);
			ApplyNewScheme (groupName);
		}

		string GetGroupNameFromNode (TreeNavigator navigator)
		{
			var property = navigator.GetValue (propertyField);
			if (property != null)
				return property.Attribute.GroupName;

			var name = navigator.GetValue (nameField);
			if (name != null)
				return name;

			return GroupNames.Other;
		}

		void ApplyNewScheme (string groupName)
		{
			var newscheme = colorScheme.Clone ();
			WriteDataToScheme (newscheme);
			this.colorScheme = newscheme;

			this.textEditor.TextViewMargin.PurgeLayoutCache ();
			SetCodeExample (groupName);
			this.textEditor.GetTextEditorData ().ColorStyle = newscheme;
			this.textEditor.QueueDraw ();
		}

		void SetCodeExample (string groupName)
		{
			GetSampleFromEditor ();
			switch (groupName) {
			case GroupNames.XML:
				SetEditorText (CodeSamples.XML, "application/xml");
				break;
			case GroupNames.HTML:
				SetEditorText (CodeSamples.Web, "text/html");
				break;
			case GroupNames.CSS:
				SetEditorText (CodeSamples.CSS, "text/css");
				break;
			case GroupNames.Script:
				SetEditorText (CodeSamples.Javascript, "text/javascript");
				break;
			case GroupNames.CSharp:
				SetEditorText (CodeSamples.CSharp, "text/x-csharp");
				break;
			default:
				SetEditorText (CodeSamples.Text, "text");
				break;
			}
		}

		void GetSampleFromEditor ()
		{
			switch (this.textEditor.MimeType) {
			case "application/xml":
				CodeSamples.XML = textEditor.Text;
				break;
			case "text/html":
				CodeSamples.Web = textEditor.Text;
				break;
			case "text/css":
				CodeSamples.CSS = textEditor.Text;
				break;
			case "text/javascript":
				CodeSamples.Javascript = textEditor.Text;
				break;
			case "text/x-csharp":
				CodeSamples.CSharp = textEditor.Text;
				break;
			}
		}

		void SetEditorText (string newText, string newMimeType)
		{
			if (this.textEditor.MimeType == newMimeType)
				return;
			this.textEditor.Text = newText;
			this.textEditor.Document.MimeType = newMimeType;
		}

		void ChangeAmbientColor (TreeNavigator navigator, AmbientColor oldStyle)
		{
			var newStyle = new AmbientColor ();
			newStyle.Color = GetColorFromButton (colorbuttonPrimary);
			newStyle.SecondColor = GetColorFromButton (colorbuttonSecondary);

			if (handleUIEvents)
				history.AddCommand (new ChangeAmbientColorCommand (oldStyle, newStyle, navigator));

			var groupName = GetGroupNameFromNode (navigator);
			ApplyNewScheme (groupName);
		}

		Cairo.Color GetColorFromButton (LabeledColorButton button)
		{
			return new Cairo.Color (button.Color.Red, button.Color.Green, button.Color.Blue, button.Color.Alpha);
		}

		void WriteDataToScheme (ColorScheme scheme)
		{
			scheme.Name = entryName.Text;
			scheme.Description = entryDescription.Text;

			TreePosition pos = treeviewColors.SelectedRow;
			if (pos == null)
				return;

			var navigator = colorStore.GetFirstNode ();

			do {
				navigator.MoveToChild ();

				do {
					var data = (ColorScheme.PropertyDecsription)navigator.GetValue (propertyField);
					var style = navigator.GetValue (styleField);
					data.Info.SetValue (scheme, style, null);
				} while (navigator.MoveNext ());

				navigator.MoveToParent ();
			} while (navigator.MoveNext ());
		}

		protected override void OnCommandActivated (Command cmd)
		{
			if (cmd.Equals (Command.Ok)) {
				WriteDataToScheme (colorScheme);
				try {
					if (fileName.EndsWith (".vssettings", StringComparison.Ordinal)) {
						System.IO.File.Delete (fileName);
						fileName += "Style.json";
					}
					colorScheme.Save (fileName);
					panel.ShowStyles ();
				} catch (Exception ex) {
					MessageService.ShowException (ex);
				}
				RefreshAllColors ();
			}
			base.OnCommandActivated (cmd);
		}

		public static void RefreshAllColors ()
		{
			foreach (var doc in Ide.IdeApp.Workbench.Documents) {
				var editor = doc.Editor;
				if (editor == null)
					continue;
				doc.UpdateParseDocument ();
				editor.Parent.TextViewMargin.PurgeLayoutCache ();
				editor.Document.CommitUpdateAll ();
			}
		}

		public void SetScheme (ColorScheme scheme)
		{
			if (scheme == null)
				throw new ArgumentNullException ("scheme");

			this.fileName = scheme.FileName;
			this.colorScheme = scheme;
			this.entryName.Text = scheme.Name;
			this.entryDescription.Text = scheme.Description;
			SetCodeExample (GroupNames.CSharp);
			var data = this.textEditor.GetTextEditorData ();
			data.ColorStyle = scheme;
			data.Caret.PositionChanged += CaretPositionChanged;

			var styleCollection = ColorScheme.TextColors.Concat (ColorScheme.AmbientColors);
			FillTreeStore (colorStore, styleCollection, scheme);

			treeviewColors.ExpandAll ();
			StyleChanged (null, null);
		}

		void FillTreeStore (TreeStore treeStore, IEnumerable<ColorScheme.PropertyDecsription> dataCollection, ColorScheme scheme)
		{
			foreach (var data in dataCollection) {
				var parent = treeStore.GetGroupParentNode (data.Attribute.GroupName, nameField);
				var navigator = parent.AddChild ();
				SetValueToNode (navigator, scheme, data);
			}
		}

		void SetValueToNode (TreeNavigator navigator, ColorScheme scheme, ColorScheme.PropertyDecsription data)
		{
			navigator.SetValue (nameField, data.Attribute.Name);
			navigator.SetValue (propertyField, data);
			navigator.SetValue (styleField, data.Info.GetValue (scheme, null));
		}

		void CaretPositionChanged (object sender, DocumentLocationEventArgs e)
		{
			var caret = this.textEditor.Caret;
			var lineNumber = caret.Line;
			var columnNumber = caret.Column;

			var document = this.textEditor.Document;
			var syntaxMode = document.SyntaxMode;
			var line = document.GetLine (lineNumber);

			var lineChunks = syntaxMode.GetChunks (this.colorScheme, line, line.Offset, line.Length);
			var offset = line.Offset + columnNumber - 1;
			var chunk = lineChunks.FirstOrDefault (ch => ch.Offset <= offset && ch.EndOffset >= offset);
			if (chunk == null) {
				treeviewColors.UnselectAll ();
				return;
			}

			var styleName = chunk.Style;
			var navigator = GetNodeFromStyleName (styleName);
			if (navigator == null) {
				treeviewColors.UnselectAll ();
				return;
			}

			if (formatByPatternMode) {
				var oldNavigator = colorStore.GetNavigatorAt (treeviewColors.SelectedRow);
				var oldStyle = oldNavigator.GetValue (styleField);
				var newStyle = navigator.GetValue (styleField);
				if (oldStyle.GetType () == newStyle.GetType ()) {
					this.treeviewColors.SelectRow (navigator.CurrentPosition);
					CopyStyle (oldStyle, newStyle, oldNavigator);
					this.treeviewColors.SelectRow (oldNavigator.CurrentPosition);
				}
				this.buttonFormat.Active = false;
			} else {
				this.treeviewColors.SelectRow (navigator.CurrentPosition);
				treeviewColors.ScrollToRow (navigator.CurrentPosition);
			}
		}

		void CopyStyle (object oldStyle, object newStyle, TreeNavigator navigator)
		{
			var oldChunkStyle = oldStyle as ChunkStyle;
			if (oldChunkStyle != null) {
				var newChunkStyle = newStyle as ChunkStyle;
				oldChunkStyle.Background = newChunkStyle.Background;
				oldChunkStyle.FontStyle = newChunkStyle.FontStyle;
				oldChunkStyle.FontWeight = newChunkStyle.FontWeight;
				oldChunkStyle.Foreground = newChunkStyle.Foreground;
				oldChunkStyle.Underline = newChunkStyle.Underline;
				ChangeChunkStyle (navigator, oldChunkStyle);
				return;
			}

			var oldAmbientColor = oldStyle as AmbientColor;
			if (oldAmbientColor != null) {
				var newAmbientColor = newStyle as AmbientColor;
				oldAmbientColor.Color = newAmbientColor.Color;
				if (newAmbientColor.HasBorderColor && oldAmbientColor.HasBorderColor)
					oldAmbientColor.BorderColor = newAmbientColor.BorderColor;
				if (newAmbientColor.HasSecondColor && oldAmbientColor.HasSecondColor)
					oldAmbientColor.SecondColor = newAmbientColor.SecondColor;
				ChangeAmbientColor (navigator, oldAmbientColor);
				return;
			}
		}

		TreeNavigator GetNodeFromStyleName (string styleName)
		{
			var navigator = colorStore.GetFirstNode ();

			do {
				navigator.MoveToChild ();

				do {
					var data = (ColorScheme.PropertyDecsription)navigator.GetValue (propertyField);
					if (data != null && data.Attribute != null && data.Attribute.Name == styleName)
						return navigator;
				} while (navigator.MoveNext ());

				navigator.MoveToParent ();
			} while (navigator.MoveNext ());

			return null;
		}

		void FormatByPatternToggled (object sender, EventArgs e)
		{
			formatByPatternMode = !formatByPatternMode;
		}
	}
}
