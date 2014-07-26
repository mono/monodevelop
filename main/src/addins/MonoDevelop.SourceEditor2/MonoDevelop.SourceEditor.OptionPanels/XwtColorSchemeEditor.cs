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

			this.colorbuttonPrimary.LabelText = "Primary color";
			this.colorbuttonSecondary.LabelText = "Secondary color";

			handleUIEvents = true;
		}

		void SetColorToButton (LabeledColorButton button, Cairo.Color color)
		{
			button.Color = new Color (color.R, color.G, color.B, color.A);
		}

		void StyleChanged (object sender, EventArgs e)
		{
			TreePosition pos = treeviewColors.SelectedRow;
			if (pos == null)
				return;

			var navigator = colorStore.GetNavigatorAt (pos);
			var o = navigator.GetValue (styleField);

			if (o is ChunkStyle) {
				ChangeChunkStyle (navigator, (ChunkStyle)o);
			} else if (o is AmbientColor) {
				ChangeAmbientColor (navigator, (AmbientColor)o);
			}
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
			switch (groupName) {
			case GroupNames.XML:
				this.textEditor.Text = CodeSamples.XML;
				this.textEditor.Document.MimeType = "application/xml";
				break;
			case GroupNames.HTML:
				this.textEditor.Text = CodeSamples.Web;
				this.textEditor.Document.MimeType = "text/html";
				break;
			case GroupNames.CSS:
				this.textEditor.Text = CodeSamples.CSS;
				this.textEditor.Document.MimeType = "text/css";
				break;
			case GroupNames.Script:
				this.textEditor.Text = CodeSamples.Javascript;
				this.textEditor.Document.MimeType = "text/javascript";
				break;
			default:
				this.textEditor.Text = CodeSamples.CSharp;
				this.textEditor.Document.MimeType = "text/x-csharp";
				break;
			}
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
			var lineNumber = caret.Line;//e.Location.Line;
			var columnNumber = caret.Column;//e.Location.Column;

			var document = this.textEditor.Document;
			var syntaxMode = document.SyntaxMode;
			var line = document.GetLine (lineNumber);

			var lineChunks = syntaxMode.GetChunks (this.colorScheme, line, line.Offset, line.Length);
			var offset = line.Offset + columnNumber-1;
			var chunk = lineChunks.FirstOrDefault (ch => ch.Offset <= offset && ch.EndOffset >= offset);
			if (chunk == null)
				return;

			var style = chunk.Style;
		}
	}
}
