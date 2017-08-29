//
// SignatureChangeDialog.cs
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;
using MonoDevelop.Ide.Editor;
using Gtk;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Collections.Immutable;
using System.Text;
using MonoDevelop.Ide;

namespace MonoDevelop.Refactoring.SignatureChange
{
	partial class SignatureChangeDialog : Gtk.Dialog
	{
		TextEditor previewEditor;
		ISymbol symbol;
		ParameterConfiguration parameters;
		ListStore store = new ListStore (typeof (IParameterSymbol));

		static SymbolDisplayFormat symbolDeclarationDisplayFormat = new SymbolDisplayFormat (
			genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
			extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
			memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeModifiers);
		
		static SymbolDisplayFormat parameterDisplayFormat = new SymbolDisplayFormat (
			genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters, 
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
			parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeDefaultValue | SymbolDisplayParameterOptions.IncludeExtensionThis | SymbolDisplayParameterOptions.IncludeName);

		ImmutableArray<SymbolDisplayPart> symbolDisplayParts;

		public List<IParameterSymbol> ParameterList {
			get {
				var result = new List<IParameterSymbol> ();
				TreeIter iter;
				if (!store.GetIterFirst (out iter))
					return result;
				do {
					var param = store.GetValue (iter, 0) as IParameterSymbol;
					if (param != null)
						result.Add (param);
				} while (store.IterNext (ref iter));
				return result;
			} 
		}

		public bool CanMoveUp {
			get {
				Gtk.TreeIter iter;
				if (!treeviewParameterList.Selection.GetSelected (out iter))
					return false;
				var param = store.GetValue (iter, 0) as IParameterSymbol;
				if (param == parameters.ThisParameter || param == parameters.ParamsParameter)
					return false;
				
				var idx = store.GetPath (iter).Indices [0];
				if (parameters.ThisParameter != null)
					idx--;
				if (idx <= 0 ||
					idx == parameters.RemainingEditableParameters.Count ||
					idx >= parameters.RemainingEditableParameters.Count + parameters.ParametersWithoutDefaultValues.Count)
					return false;
				return true;
			}
		}
		public bool CanMoveDown {
			get {
				Gtk.TreeIter iter;
				if (!treeviewParameterList.Selection.GetSelected (out iter))
					return false;
				var param = store.GetValue (iter, 0) as IParameterSymbol;
				if (param == parameters.ThisParameter || param == parameters.ParamsParameter)
					return false;
				var idx = store.GetPath (iter).Indices [0];
				if (parameters.ThisParameter != null)
					idx--;
				if (idx < 0 ||
					idx == parameters.RemainingEditableParameters.Count - 1 ||
					idx >= parameters.RemainingEditableParameters.Count + parameters.ParametersWithoutDefaultValues.Count - 1)
					return false;
				return true;
			}
		}

		public bool CanRefresh {
			get {
				var l1 = ParameterList;
				var l2 = parameters.ToListOfParameters ();
				if (l1.Count != l2.Count)
					return true;
				for (int i = 0; i < l1.Count; i++) {
					if (l1 [i] != l2 [i])
						return true;
				}
				return false;
			}
		}

		public bool CanRemove {
			get {
				Gtk.TreeIter iter;
				if (!treeviewParameterList.Selection.GetSelected (out iter))
					return false;
				var param = store.GetValue (iter, 0) as IParameterSymbol;
				if (param == parameters.ThisParameter)
					return false;
				return param != null;
			}
		}

		internal SignatureChangeDialog () : base (GettextCatalog.GetString ("Change Signature"), IdeApp.Workbench.RootWindow, DialogFlags.Modal)
		{
			this.Build ();

			previewEditor = TextEditorFactory.CreateNewEditor (TextEditorFactory.CreateNewDocument ());
			previewEditor.IsReadOnly = true;
			previewEditor.MimeType = "text/x-csharp";
			var options = new CustomEditorOptions {
				ShowLineNumberMargin = false,
				ShowFoldMargin = false,
				ShowIconMargin = false,
				ShowRuler = false,
				EditorTheme = DefaultSourceEditorOptions.Instance.EditorTheme,
				FontName = DefaultSourceEditorOptions.Instance.FontName
			};
			previewEditor.Options = options;
			vbox4.PackStart (previewEditor, true, true, 0);
			vbox4.ShowAll ();

			var tr = new CellRendererText ();
			var col = this.treeviewParameterList.AppendColumn (GettextCatalog.GetString ("Modifier"), tr);
			col.SetCellDataFunc (tr, new TreeCellDataFunc ((column, cell, model, iter) => {
				var param = model.GetValue (iter, 0) as IParameterSymbol;
				if (param == parameters.ThisParameter) {
					((CellRendererText)cell).Text = "this";
					return;
				}
				if (param == parameters.ParamsParameter) {
					((CellRendererText)cell).Text = "params";
					return;
				}
				switch (param.RefKind) {
				case RefKind.Out:
					((CellRendererText)cell).Text = "out";
					break;
				case RefKind.Ref:
					((CellRendererText)cell).Text = "ref";
					break;
				default:
					((CellRendererText)cell).Text = "";
					break;
				}
			}));

			col = this.treeviewParameterList.AppendColumn (GettextCatalog.GetString ("Type"), tr);
			col.SetCellDataFunc (tr, new TreeCellDataFunc ((column, cell, model, iter) => {
				var param = model.GetValue (iter, 0) as IParameterSymbol;
				((CellRendererText)cell).Text = param.Type.ToDisplayString ();
			}));

			col = this.treeviewParameterList.AppendColumn (GettextCatalog.GetString ("Parameter"), tr);
			col.SetCellDataFunc (tr, new TreeCellDataFunc ((column, cell, model, iter) => {
				var param = model.GetValue (iter, 0) as IParameterSymbol;
				((CellRendererText)cell).Text = param.Name;
			}));

			col = this.treeviewParameterList.AppendColumn (GettextCatalog.GetString ("Standard"), tr);
			col.SetCellDataFunc (tr, new TreeCellDataFunc ((column, cell, model, iter) => {
				var param = model.GetValue (iter, 0) as IParameterSymbol;
				((CellRendererText)cell).Text = param.HasExplicitDefaultValue ? param.ExplicitDefaultValue.ToString () : "";
			}));
			this.treeviewParameterList.Model = store;
			this.treeviewParameterList.Selection.Changed += delegate {
				UpdateSensitivity ();
			};

			this.buttonUp.Clicked += delegate {
				Gtk.TreeIter iter, iter2;
				if (!treeviewParameterList.Selection.GetSelected (out iter))
					return;
				var path = store.GetPath (iter);
				if (!path.Prev ())
					return;
				if (!store.GetIter (out iter2, path))
					return;
				store.MoveBefore (iter, iter2);
				UpdatePreview ();
			};

			this.buttonDown.Clicked += delegate {
				Gtk.TreeIter iter, iter2;
				if (!treeviewParameterList.Selection.GetSelected (out iter))
					return;
				var path = store.GetPath (iter);
				path.Next ();
				if (!store.GetIter (out iter2, path))
					return;
				store.MoveAfter (iter, iter2);
				UpdatePreview ();
			};

			this.buttonRemove.Clicked += delegate {
				Gtk.TreeIter iter;
				if (!treeviewParameterList.Selection.GetSelected (out iter))
					return;
				store.Remove (ref iter); 
				UpdatePreview ();
			};

			this.buttonRefresh.Clicked += delegate {
				Refresh ();
			};
		}

		void UpdateSensitivity ()
		{
			buttonUp.Sensitive = CanMoveUp;
			buttonDown.Sensitive = CanMoveDown;
			buttonRefresh.Sensitive = CanRefresh;
			buttonRemove.Sensitive = CanRemove;
		}

		internal void Init (ISymbol symbol, ParameterConfiguration parameters)
		{
			this.symbol = symbol;
			this.parameters = parameters;
			symbolDisplayParts = symbol.ToDisplayParts (symbolDeclarationDisplayFormat);
			Refresh ();
		}

		void Refresh ()
		{
			store.Clear ();
			foreach (var p in parameters.ToListOfParameters ()) {
				store.AppendValues (p);
			}
			UpdatePreview ();
		}

		void UpdatePreview ()
		{
			var sb = new StringBuilder ();
			foreach (var part in symbolDisplayParts) {
				sb.Append (part.ToString ());
			}

			sb.Append ("(");
			bool first = true;
			foreach (var p in ParameterList) {
				if (!first) {
					sb.Append (", ");
				} else {
					first = false;
				}
				sb.Append (p.ToDisplayString (parameterDisplayFormat));
			}
			sb.Append (")");
			previewEditor.Text = sb.ToString ();
			UpdateSensitivity ();
		}
	}
}
