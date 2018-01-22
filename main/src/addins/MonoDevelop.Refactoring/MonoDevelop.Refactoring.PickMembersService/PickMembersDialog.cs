//
// PickMembersDialog.cs
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.LanguageServices;
using System.Collections.Generic;
using Xwt;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.Linq;
using MonoDevelop.Core;
using Xwt.Drawing;
using MonoDevelop.Components.AtkCocoaHelper;
using Microsoft.CodeAnalysis.PickMembers;
using System.Collections.Immutable;

namespace MonoDevelop.Refactoring.PickMembersService
{
	class PickMembersDialog : Xwt.Dialog
	{
		DataField<bool> symbolIncludedField = new DataField<bool> ();
		DataField<string> symbolTextField = new DataField<string> ();
		DataField<Image> symbolIconField = new DataField<Image> ();

		DataField<ISymbol> symbolField = new DataField<ISymbol> ();

		ListStore treeStore;

		public IEnumerable<ISymbol> IncludedMembers {
			get {
				for (int i = 0; i < treeStore.RowCount; i++) {
					if (treeStore.GetValue (i, symbolIncludedField))
						yield return treeStore.GetValue (i, symbolField);
				}
			}
		}

		public ImmutableArray<PickMembersOption> Options { get; set; }

		ListBox listBoxPublicMembers = new ListBox ();

		public PickMembersDialog ()
		{
			this.Build ();
			this.buttonSelectAll.Clicked += delegate {
				for (int i = 0; i < treeStore.RowCount; i++) {
					treeStore.SetValue (i, symbolIncludedField, true);
				}
				UpdateOkButton ();
			};

			this.buttonDeselectAll.Clicked += delegate {
				for (int i = 0; i < treeStore.RowCount; i++) {
					treeStore.SetValue (i, symbolIncludedField, false);
				}
				UpdateOkButton ();
			};

			listBoxPublicMembers.DataSource = treeStore;
			var checkBoxCellView = new CheckBoxCellView (symbolIncludedField);
			checkBoxCellView.Editable = true;
			checkBoxCellView.Toggled += delegate { UpdateOkButton (); };
			listBoxPublicMembers.Views.Add (checkBoxCellView);
			listBoxPublicMembers.Views.Add (new ImageCellView (symbolIconField));
			listBoxPublicMembers.Views.Add (new TextCellView (symbolTextField));
		}

		void Build ()
		{
			this.TransientFor = MessageDialog.RootWindow;
			this.Title = GettextCatalog.GetString ("Pick members");

			treeStore = new ListStore (symbolIncludedField, symbolField, symbolTextField, symbolIconField);
			var box = new VBox {
			//	Margin = 6,
			//	Spacing = 6
			};

/*			box.PackStart (new Label {
				Markup = "<b>" + GettextCatalog.GetString ("Select public members for the interface:") + "</b>"
			});*/

			var hbox = new HBox {
			//	Spacing = 6
			};
			hbox.PackStart (listBoxPublicMembers, true);
			listBoxPublicMembers.Accessible.Description = GettextCatalog.GetString ("Pick members");

			var vbox = new VBox {
			//	Spacing = 6
			};
			buttonSelectAll = new Button (GettextCatalog.GetString ("Select All"));
			buttonSelectAll.Clicked += delegate {
				UpdateOkButton ();
			};
			vbox.PackStart (buttonSelectAll);

			buttonDeselectAll = new Button (GettextCatalog.GetString ("Clear"));
			buttonDeselectAll.Clicked += delegate {
				UpdateOkButton ();
			};
			vbox.PackStart (buttonDeselectAll);

			hbox.PackStart (vbox);

			box.PackStart (hbox, true);

			Content = box;
			Buttons.Add (new DialogButton (Command.Cancel));
			Buttons.Add (okButton = new DialogButton (Command.Ok));
			this.DefaultCommand = okButton.Command;

			this.Width = 400;
			this.Height = 321;
			this.Resizable = false;
		}

		static SymbolDisplayFormat memberDisplayFormat = new SymbolDisplayFormat (
			genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
			memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
			parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeOptionalBrackets,
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

		Button buttonSelectAll;
		Button buttonDeselectAll;
		DialogButton okButton;


		internal void Init (string title, ImmutableArray<ISymbol> members, ImmutableArray<PickMembersOption> options)
		{
			this.Title = title;
			listBoxPublicMembers.Accessible.Description = title;

			this.Options = options;
			treeStore.Clear ();
			foreach (var member in members) {
				var row = treeStore.AddRow ();
				treeStore.SetValue (row, symbolIncludedField, false);
				treeStore.SetValue (row, symbolField, member);
				treeStore.SetValue (row, symbolTextField, member.ToDisplayString (memberDisplayFormat));
				treeStore.SetValue (row, symbolIconField, ImageService.GetIcon (MonoDevelop.Ide.TypeSystem.Stock.GetStockIcon (member)));
			}
		}

		void UpdateOkButton ()
		{
			okButton.Sensitive = true;
		}
	}
}
