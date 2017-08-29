//
// ExtractInterfaceDialog.cs
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.LanguageServices;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.Refactoring.ExtractInterface
{
	partial class ExtractInterfaceDialog : Gtk.Dialog
	{
		ISyntaxFactsService syntaxFactsService;
		INotificationService notificationService;
		string defaultInterfaceName;
		List<string> conflictingTypeNames;
		string defaultNamespace;
		string generatedNameTypeParameterSuffix;
		string languageName;
		readonly TreeStore treeStore = new TreeStore (typeof (bool), typeof (ISymbol));

		public string InterfaceName {
			get {
				return entryName.Text;
			}
			set {
				entryName.Text = value;
			}
		}

		public string FileName {
			get {
				return entryFileName.Text;
			}
			set {
				entryFileName.Text = value;
			}
		}

		public IEnumerable<ISymbol> IncludedMembers {
			get {
				if (treeStore.GetIterFirst (out TreeIter iter)) {
					do {
						var entity = treeStore.GetValue (iter, 1) as ISymbol;
						if (entity != null)
							yield return entity;
					} while (treeStore.IterNext (ref iter));
				}
			}
		}

		public ExtractInterfaceDialog () : base (GettextCatalog.GetString ("Extract Interface"), IdeApp.Workbench.RootWindow, DialogFlags.Modal)
		{
			this.Build ();
			this.buttonSelectAll.Clicked += delegate {
				if (treeStore.GetIterFirst (out TreeIter iter)) {
					do {
						treeStore.SetValue (iter, 0, true);
					} while (treeStore.IterNext (ref iter));
				}
				UpdateOkButton ();
			};

			this.buttonDeselectAll.Clicked += delegate {
				if (treeStore.GetIterFirst (out TreeIter iter)) {
					do {
						treeStore.SetValue (iter, 0, false);
					} while (treeStore.IterNext (ref iter));
				}
				UpdateOkButton ();
			};

			treeviewPublicMembers.HeadersVisible = false;
			this.treeviewPublicMembers.Model = treeStore;
			var toggle = new CellRendererToggle ();
			toggle.Toggled += delegate (object o, ToggledArgs args) {
				if (treeStore.GetIterFromString (out TreeIter iter, args.Path)) {
					treeStore.SetValue (iter, 0, !(bool)treeStore.GetValue (iter, 0));
				}
				UpdateOkButton ();
			};
			this.treeviewPublicMembers.AppendColumn ("", toggle, "active", 0);

			var crImage = new CellRendererImage ();
			var col = this.treeviewPublicMembers.AppendColumn ("", crImage);
			col.SetCellDataFunc (crImage, RenderPixbuf);

			var crText = new CellRendererText ();
			col = this.treeviewPublicMembers.AppendColumn ("", crText);
			col.SetCellDataFunc (crText, RenderText);

			this.entryName.Changed += delegate { UpdateOkButton (); };
			this.entryFileName.Changed += delegate { UpdateOkButton (); };
		}

		static SymbolDisplayFormat memberDisplayFormat = new SymbolDisplayFormat (
			genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
			memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
			parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeOptionalBrackets,
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
		private string fileExtension;

		void RenderText (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			var crText = (Gtk.CellRendererText)cell;
			var entity = tree_model.GetValue (iter, 1) as ISymbol;
			if (entity != null)
				crText.Text = entity.ToDisplayString (memberDisplayFormat);
		}

		void RenderPixbuf (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			var crImage = (CellRendererImage)cell;
			var entity = tree_model.GetValue (iter, 1) as ISymbol;
			if (entity != null)
				crImage.Image = ImageService.GetIcon (MonoDevelop.Ide.TypeSystem.Stock.GetStockIcon (entity));
		}


		internal void Init (ISyntaxFactsService syntaxFactsService, INotificationService notificationService, List<ISymbol> extractableMembers, string defaultInterfaceName, List<string> conflictingTypeNames, string defaultNamespace, string generatedNameTypeParameterSuffix, string languageName)
		{
			this.syntaxFactsService = syntaxFactsService;
			this.notificationService = notificationService;
			this.InterfaceName = this.defaultInterfaceName = defaultInterfaceName;
			this.conflictingTypeNames = conflictingTypeNames;
			this.defaultNamespace = defaultNamespace;
			this.generatedNameTypeParameterSuffix = generatedNameTypeParameterSuffix;
			this.languageName = languageName;

			// TODO: Add proper language name file extension support (atm there is only c# and vb supported by roslyn)
			this.fileExtension = (languageName == LanguageNames.CSharp ? ".cs" : ".vb");
			this.FileName = defaultInterfaceName + fileExtension;
			treeStore.Clear ();
			foreach (var member in extractableMembers) {
				treeStore.AppendValues (true, member);
			}
		}

		void UpdateOkButton ()
		{
			buttonOk.Sensitive = TrySubmit ();
		}

		bool TrySubmit ()
		{
			var trimmedInterfaceName = InterfaceName.Trim ();
			var trimmedFileName = FileName.Trim ();

			if (!IncludedMembers.Any ()) {
				return false;
			}

			if (conflictingTypeNames.Contains (trimmedInterfaceName)) {
				return false;
			}

			if (!syntaxFactsService.IsValidIdentifier (trimmedInterfaceName)) {
				return false;
			}

			if (trimmedFileName.IndexOfAny (System.IO.Path.GetInvalidFileNameChars ()) >= 0) {
				return false;
			}

			if (!System.IO.Path.GetExtension (trimmedFileName).Equals (fileExtension, StringComparison.OrdinalIgnoreCase)) {
				return false;
			}
			return true;
		}
	}
}
