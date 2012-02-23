// 
// ReorderParametersDialog.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang
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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.ProgressMonitoring;
using MonoDevelop.Refactoring;
using MonoDevelop.CSharp.Formatting;
using Mono.TextEditor;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.CSharp.Refactoring.ReorderParameters
{
	public partial class ReorderParametersDialog : Gtk.Dialog
	{
		class ParameterInfo
		{
			public string Text { 
				get; 
				set; 
			}
			
			public int Pos { 
				get; 
				set; 
			}
			
			public ParameterInfo (string text, int pos)
			{
				Text = text;
				Pos = pos;
			}
		}
		
		RefactoringOptions options;
		ReorderParametersRefactoring reorder;
		IMethod method;
		ParameterInfo[] parameters;
		int selectedIndex;
		TreeSelection selection;
		ListStore parameterStore;
		string signatureFormat;
		TextEditor signaturePreviewText = new TextEditor ();
		
		public ReorderParametersDialog (RefactoringOptions options, ReorderParametersRefactoring reorder)
		{
			this.options = options;
			this.reorder = reorder;
			this.method = (IMethod)options.SelectedItem;
			
			this.Build ();
			
			buttonOk.Sensitive = buttonPreview.Sensitive = false;
			buttonOk.Clicked += OnOkClicked;
			buttonPreview.Clicked += OnPreviewClicked;
			
			SetupSignaturePreview ();
			
			parameterTreeView.AppendColumn (GettextCatalog.GetString ("Parameter"), new CellRendererText (), "text", 0);
			selection = parameterTreeView.Selection;
			selection.Changed += OnSelectionChanged;
			
			parameterStore = new ListStore (typeof(string));
			LoadMethodInfo ();
			foreach (var param in parameters)
				parameterStore.AppendValues (param.Text);
			parameterTreeView.Model = parameterStore;
			
			buttonUp.Sensitive = buttonDown.Sensitive = false;
			buttonUp.Clicked += OnButtonUpClicked;
			buttonDown.Clicked += OnButtonDownClicked;
		}
		
		void SwapParameters (int index1, int index2)
		{
			var param = parameters [index1];
			parameters [index1] = parameters [index2];
			parameters [index2] = param;
			
			buttonOk.Sensitive = buttonPreview.Sensitive = CheckParameterOrderChanged ();
			
			UpdateSignaturePreview ();
		}
		
		void SetSelectedIndex (int index)
		{
			selectedIndex = index;
			buttonUp.Sensitive = selectedIndex > 0;
			buttonDown.Sensitive = selectedIndex < parameters.Length - 1;
		}
		
		/// <summary>
		/// Moves the selected parameter to the position given by relative offset.
		/// Offset can only be -1 (Move up) or +1 (Move down)
		/// </summary>
		void MoveSelectedParameter (int offset)
		{
			if (offset != 1 && offset != -1) {
				throw new ArgumentOutOfRangeException ("offset");
			}
			
			var selectedRow = selection.GetSelectedRows () [0];
			var selectedIndex = selectedRow.Indices [0];
			
			var target = selectedRow.Copy ();
			if (offset == 1)
				target.Next ();
			else
				target.Prev ();
			
			TreeIter selectedIter;
			TreeIter targetIter;
			if (!parameterStore.GetIter (out selectedIter, selectedRow))
				return;
			if (!parameterStore.GetIter (out targetIter, target))
				return;
			
			parameterStore.Swap (selectedIter, targetIter);
			SwapParameters (selectedIndex, selectedIndex + offset);
			SetSelectedIndex (selectedIndex + offset);
		}
		
		void OnButtonUpClicked (object sender, EventArgs e)
		{
			MoveSelectedParameter (-1);
		}

		void OnButtonDownClicked (object sender, EventArgs e)
		{
			MoveSelectedParameter (1);
		}

		void OnSelectionChanged (object sender, EventArgs e)
		{
			var selectedRows = selection.GetSelectedRows ();
			if (selectedRows.Length == 0)
				return;
			SetSelectedIndex (selectedRows [0].Indices [0]);
		}
		
		bool CheckParameterOrderChanged ()
		{
			for (int i = 0; i < parameters.Length; i++) {
				if (parameters [i].Pos != i)
					return true;
			}
			return false;
		}
		
		void LoadMethodInfo ()
		{
			var filename = method.Region.FileName;
			var doc = IdeApp.Workbench.GetDocument (filename);
			if (doc != null) {
				LoadMethodInfo (doc.Editor);
				return;
			}
			using (var editor = new TextEditorData ()) {
				editor.Text = File.ReadAllText (filename);
				LoadMethodInfo (editor);
			}
		}
		
		void LoadMethodInfo (TextEditorData editor)
		{
			var signatureFormatBuilder = new StringBuilder ();
			TextLocation signatureTextBegin = method.Region.Begin;
			
			parameters = new ParameterInfo[method.Parameters.Count];
			for (int i = 0; i < method.Parameters.Count; i++) {
				var param = method.Parameters [i];
				var paramText = editor.GetTextBetween (param.Region.Begin, param.Region.End);
				var paramInfo = new ParameterInfo (paramText, i);
				parameters [i] = paramInfo;
				
				signatureFormatBuilder.Append (editor.GetTextBetween (signatureTextBegin, param.Region.Begin));
				signatureFormatBuilder.AppendFormat ("{{{0}}}", i);
				signatureTextBegin = param.Region.End;
			}
			TextLocation signatureTextEnd = 
				method.BodyRegion.BeginLine == 0 ? method.Region.End : method.BodyRegion.Begin;
			signatureFormatBuilder.Append (editor.GetTextBetween (signatureTextBegin, signatureTextEnd));
			
			signaturePreviewText.Document.Text = signatureFormatBuilder.ToString ().TrimEnd ();
			//remove unnecessary leading spaces to get correct identation
			int columnOffset = method.Region.Begin.Column;
			for (int i = 2; i <= signaturePreviewText.LineCount; i++) {
				var line = signaturePreviewText.GetLine (i);
				int count = columnOffset - 1;
				while (!char.IsWhiteSpace (signaturePreviewText.GetCharAt (line.Offset + count - 1)))
					count--;
				signaturePreviewText.Remove (line.Offset, count);
			}
			signatureFormat = signaturePreviewText.Document.Text;
			UpdateSignaturePreview ();
		}
		
		void SetupSignaturePreview ()
		{
			signatureWindow.Child = signaturePreviewText;
			signaturePreviewText.Document.MimeType = CSharpFormatter.MimeType;
			signaturePreviewText.Show ();
			
			var options = new TextEditorOptions ();
			options.ShowLineNumberMargin = false;
			options.ShowFoldMargin = false;
			options.ShowIconMargin = false;
			options.ShowInvalidLines = false;
			options.ShowSpaces = options.ShowTabs = options.ShowEolMarkers = false;
			options.ColorScheme = PropertyService.Get ("ColorScheme", "Default");
			signatureWindow.Sensitive = false;
			signaturePreviewText.Options = options;
		}
		
		void UpdateSignaturePreview ()
		{
			var paramText = parameters.Select (param => param.Text).ToArray<System.Object> ();
			signaturePreviewText.Document.Text = string.Format (signatureFormat, paramText);
		}
		
		ReorderParametersRefactoring.ReorderParametersProperties createProperties ()
		{
			return new ReorderParametersRefactoring.ReorderParametersProperties (
				from param in parameters select param.Pos);
		}

		void OnOkClicked (object sender, EventArgs e)
		{
			var properties = createProperties ();
			((Widget)this).Destroy ();
			var changes = reorder.PerformChanges (options, properties);
			var monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (Name, null);
			RefactoringService.AcceptChanges (monitor, changes);
		}

		void OnPreviewClicked (object sender, EventArgs e)
		{
			var properties = createProperties ();
			((Widget)this).Destroy ();
			var changes = reorder.PerformChanges (options, properties);
			MessageService.ShowCustomDialog (new RefactoringPreviewDialog (changes));
		}
	}
}

