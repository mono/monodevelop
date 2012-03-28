// 
// ExtractMethodDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using Gtk;

using MonoDevelop.Core;
using System.Collections.Generic;
using MonoDevelop.Ide;
using Mono.TextEditor;
using Mono.TextEditor.PopupWindow;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.TypeSystem;
using System.Linq;

namespace MonoDevelop.CSharp.Refactoring.ExtractMethod
{
	public partial class ExtractMethodDialog : Gtk.Dialog
	{
		ExtractMethodRefactoring extractMethod;
		ExtractMethodRefactoring.ExtractMethodParameters properties;
		ListStore store;
		RefactoringOptions options;
		string methodName;
		int activeModifier;
		bool generateComments;
		
		public ExtractMethodDialog (RefactoringOptions options, ExtractMethodRefactoring extractMethod, ExtractMethodRefactoring.ExtractMethodParameters properties)
		{
			this.Build ();
			this.options = options;
			this.properties = properties;
			this.extractMethod = extractMethod;
			
			store = new ListStore (typeof (string), typeof (string));
			treeviewParameters.Model = store;
			treeviewParameters.AppendColumn ("Type", new CellRendererText (), "text", 0);
			treeviewParameters.AppendColumn ("Name", new CellRendererText (), "text", 1);
			FillStore ();
			buttonPreview.Sensitive = buttonOk.Sensitive = false;
			entry.Changed += delegate { buttonPreview.Sensitive = buttonOk.Sensitive = ValidateName (); };
			ValidateName ();
			
			buttonOk.Clicked += OnOKClicked;
			buttonCancel.Clicked += OnCancelClicked;
			buttonPreview.Clicked += OnPreviewClicked;
			
			buttonUp.Clicked += delegate {
				List<int> indices = new List<int> ();
				foreach (TreePath path in treeviewParameters.Selection.GetSelectedRows ()) {
					int index = Int32.Parse (path.ToString ());
					if (index > 0) {
						VariableDescriptor tmp = properties.Parameters [index - 1];
						properties.Parameters [index - 1] = properties.Parameters [index];
						properties.Parameters [index] = tmp;
						indices.Add (index - 1);
					}
				}
				FillStore ();
				treeviewParameters.Selection.SelectPath (new TreePath (indices.ToArray ()));
			};
			buttonDown.Clicked += delegate {
				List<int> indices = new List<int> ();
				foreach (TreePath path in treeviewParameters.Selection.GetSelectedRows ()) {
					int index = Int32.Parse (path.ToString ());
					if (index + 1 < properties.Parameters.Count) {
						VariableDescriptor tmp = properties.Parameters [index + 1];
						properties.Parameters [index + 1] = properties.Parameters [index];
						properties.Parameters [index] = tmp;
						indices.Add (index + 1);
					}
				}
				FillStore ();
				treeviewParameters.Selection.SelectPath (new TreePath (indices.ToArray ()));
			};
			ListStore modifiers = new ListStore (typeof (string));
			modifiers.AppendValues ("");
			modifiers.AppendValues ("public");
			modifiers.AppendValues ("private");
			modifiers.AppendValues ("protected");
			modifiers.AppendValues ("internal");
			comboboxModifiers.Model = modifiers;
			comboboxModifiers.Active = PropertyService.Get<int> ("MonoDevelop.Refactoring.ExtractMethod.ExtractMethodDialog.DefaultModifier");
			entry.Activated += delegate {
				if (buttonOk.Sensitive)
					buttonOk.Click ();
			};
		}
		
		void FillStore ()
		{
			store.Clear ();
			foreach (VariableDescriptor var in properties.Parameters) {
				store.AppendValues (var.ReturnType != null ? var.ReturnType.ToString () : "<null>" , var.Name);
			}
		}

		bool ValidateName ()
		{
			string methodName = entry.Text;
			if (HasMember (methodName)) {
				labelWarning.Text = GettextCatalog.GetString ("A member with the name '{0}' already exists.", methodName);
				imageWarning.IconName = Gtk.Stock.DialogError;
				return false;
			}
			
			return true;
//			INameValidator nameValidator = MonoDevelop.Projects.LanguageBindingService.GetRefactorerForFile (fileName ?? "default.cs");
//			if (nameValidator == null)
//				return true;
//			ValidationResult result = nameValidator.ValidateName (new DomMethod (), entry.Text);
//			if (!result.IsValid) {
//				imageWarning.IconName = Gtk.Stock.DialogError;
//			} else if (result.HasWarning) {
//				imageWarning.IconName = Gtk.Stock.DialogWarning;
//			} else {
//				imageWarning.IconName = Gtk.Stock.Apply;
//			}
//			labelWarning.Text = result.Message;
//			return result.IsValid;
		}

		bool HasMember (string name)
		{
			foreach (var member in properties.DeclaringMember.DeclaringType.GetMembers (m => m.Name == name)) {
				var method = member as IMethod;
				if (method == null)
					continue;
				if (method.Parameters.Count != properties.Parameters.Count)
					continue;
				bool equals = true;
				for (int i = 0; i < method.Parameters.Count; i++) {
					if (!properties.Parameters[i].ReturnType.Equals (method.Parameters[i].Type)) {
						equals = false;
						break;
					}
				}
				if (equals) 
					return true;
			}
			return false;
		}
		
		void OnCancelClicked (object sender, EventArgs e)
		{
			this.Destroy ();
		}
		
		void SetProperties ()
		{
			properties.Name = methodName;
			properties.GenerateComment = generateComments;
			switch (activeModifier) {
			case 0:
				properties.Modifiers = Accessibility.Public;
				break;
			case 1:
				properties.Modifiers = Accessibility.Public;
				break;
			case 2:
				properties.Modifiers = Accessibility.Private;
				break;
			case 3:
				properties.Modifiers = Accessibility.Protected;
				break;
			case 4:
				properties.Modifiers = Accessibility.Internal;
				break;
			}
			PropertyService.Set ("MonoDevelop.Refactoring.ExtractMethod.ExtractMethodDialog.DefaultModifier", comboboxModifiers.Active);
		}
		
		void OnOKClicked (object sender, EventArgs e)
		{
			TextEditorData data = options.GetTextEditorData ();
			Mono.TextEditor.TextEditor editor = data.Parent;
// Insertion cursor mode test:
			if (editor != null) {
				var type = properties.DeclaringMember.DeclaringTypeDefinition.Parts.First ();
				
				var mode = new InsertionCursorEditMode (editor, CodeGenerationService.GetInsertionPoints (options.Document, type));
				for (int i = 0; i < mode.InsertionPoints.Count; i++) {
					var point = mode.InsertionPoints[i];
					if (point.Location < editor.Caret.Location) {
						mode.CurIndex = i;
					} else {
						break;
					}
				}
				var helpWindow = new InsertionCursorLayoutModeHelpWindow ();
				helpWindow.TransientFor = IdeApp.Workbench.RootWindow;
				helpWindow.TitleText = GettextCatalog.GetString ("Extract Method");
				mode.HelpWindow = helpWindow;
				mode.StartMode ();
				methodName = entry.Text;
				activeModifier = comboboxModifiers.Active;
				generateComments = checkbuttonGenerateComment.Active;
				
				mode.Exited += delegate(object s, InsertionCursorEventArgs args) {
					if (args.Success) {
						SetProperties ();
						properties.InsertionPoint = args.InsertionPoint;
						List<Change> changes = extractMethod.PerformChanges (options, properties);
						IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (this.Title, null);
						RefactoringService.AcceptChanges (monitor, changes);
					}
				};
			}
			
			((Widget)this).Destroy ();
		}
		
		void OnPreviewClicked (object sender, EventArgs e)
		{
			methodName = entry.Text;
			SetProperties ();
			List<Change> changes = extractMethod.PerformChanges (options, properties);
			((Widget)this).Destroy ();
			MessageService.ShowCustomDialog (new RefactoringPreviewDialog (changes));
		}
	}
}
