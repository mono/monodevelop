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
using Xwt;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.Linq;
using MonoDevelop.Core;
using Xwt.Drawing;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Refactoring.ExtractInterface
{
	class ExtractInterfaceDialog : Xwt.Dialog
	{
		ISyntaxFactsService syntaxFactsService;
		INotificationService notificationService;
		string defaultInterfaceName;
		List<string> conflictingTypeNames;
		string defaultNamespace;
		string generatedNameTypeParameterSuffix;
		string languageName;

		DataField<bool> symbolIncludedField = new DataField<bool> ();
		DataField<string> symbolTextField = new DataField<string> ();
		DataField<Image> symbolIconField = new DataField<Image> ();

		DataField<ISymbol> symbolField = new DataField<ISymbol> ();

		ListStore treeStore;
		 
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
				for (int i = 0; i < treeStore.RowCount; i++) {
					if (treeStore.GetValue (i, symbolIncludedField))
						yield return treeStore.GetValue (i, symbolField);
				}
			}
		}

		TextEntry entryFileName = new TextEntry ();
		TextEntry entryName = new TextEntry ();
		ListView listViewPublicMembers = new ListView ();

		public ExtractInterfaceDialog ()
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

			listViewPublicMembers.HeadersVisible = false;
			listViewPublicMembers.DataSource = treeStore;
			var checkBoxCellView = new CheckBoxCellView (symbolIncludedField);
			checkBoxCellView.Editable = true;
			checkBoxCellView.Toggled += delegate { 
				GLib.Timeout.Add (20, delegate {
					UpdateOkButton (); 
					return false;
				});
			};
			listViewPublicMembers.Columns.Add ("", checkBoxCellView);
			listViewPublicMembers.Columns.Add ("", new ImageCellView (symbolIconField), new TextCellView (symbolTextField));


			this.entryName.Changed += delegate { UpdateOkButton (); };
			this.entryFileName.Changed += delegate { UpdateOkButton (); };
		}

		void Build ()
		{
			this.TransientFor = MessageDialog.RootWindow;
			this.Title = GettextCatalog.GetString ("Extract Interface");

			treeStore = new ListStore (symbolIncludedField, symbolField, symbolTextField, symbolIconField); 
			var box = new VBox {
				Margin = 6,
				Spacing = 6
			};

			box.PackStart (new Label {
				Markup = GettextCatalog.GetString ("Name of the new interface:")
			});
			box.PackStart (entryName);
			entryName.Name = "entryName.Name";
			entryName.SetCommonAccessibilityAttributes (entryName.Name, GettextCatalog.GetString ("Name of the new interface"),
			                                            GettextCatalog.GetString ("The name of the new interface"));

			entryName.Changed += delegate {
				UpdateOkButton ();
			};
			box.PackStart (new Label {
				Markup = GettextCatalog.GetString ("File name:")
			});

			box.PackStart (entryFileName);
			entryFileName.Name = "entryFileName.Name";
			entryFileName.SetCommonAccessibilityAttributes (entryFileName.Name, GettextCatalog.GetString ("Name of the new file"),
			                                                GettextCatalog.GetString ("The name of the file for the new interface"));

			entryFileName.Changed += delegate {
				UpdateOkButton ();
			};
			box.PackStart (new Label {
				Markup = "<b>" + GettextCatalog.GetString ("Select public members for the interface:") + "</b>"
			});

			var hbox = new HBox {
				Spacing = 6
			};
			hbox.PackStart (listViewPublicMembers, true);
			listViewPublicMembers.Accessible.Description = GettextCatalog.GetString ("Select the public members which are added to the interface");

			var vbox = new VBox {
				Spacing = 6
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
			Buttons.Add (okButton = new DialogButton (Command.Ok));
			Buttons.Add (new DialogButton (Command.Cancel));

			this.Width = 400;
			this.Height = 421;
			this.Resizable = false;
		}

		static SymbolDisplayFormat memberDisplayFormat = new SymbolDisplayFormat (
			genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
			memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
			parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeOptionalBrackets,
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
		private string fileExtension;
		private Button buttonSelectAll;
		private Button buttonDeselectAll;
		private DialogButton okButton;

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
				var row = treeStore.AddRow ();
				treeStore.SetValue (row, symbolIncludedField, true);
				treeStore.SetValue (row, symbolField, member);
				treeStore.SetValue (row, symbolTextField, member.ToDisplayString (memberDisplayFormat));
				treeStore.SetValue (row, symbolIconField, ImageService.GetIcon (MonoDevelop.Ide.TypeSystem.Stock.GetStockIcon (member)));
			}
			UpdateOkButton ();
		}

		void UpdateOkButton ()
		{
			okButton.Sensitive = TrySubmit ();
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
