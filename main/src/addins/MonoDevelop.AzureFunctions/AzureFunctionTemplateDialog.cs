//
// AzureFunctionTemplateDialog.cs
//
// Copyright (c) 2017 Microsoft Corp.
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
using System.Json;

using Xwt;
using Xwt.Drawing;

using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.AzureFunctions
{
	public class AzureFunctionTemplateDialog : Dialog
	{
		AzureFunctionTemplateParameters parameters;
		readonly DataField<ItemTemplate> templateField;
		readonly DataField<string> nameField;
		readonly DataField<Image> iconField;
		readonly ScrollView scrollView;
		readonly DotNetProject project;
		readonly ListStore model;

		public AzureFunctionTemplateDialog (DotNetProject project)
		{
			templateField = new DataField<ItemTemplate> ();
			nameField = new DataField<string> ();
			iconField = new DataField<Image> ();

			model = new ListStore (iconField, nameField, templateField);

			this.project = project;

			var listView = CreateItemTemplateList ();
			listView.Show ();

			scrollView = new ScrollView ();
			scrollView.Show ();

			var hbox = new HBox { Spacing = 6 };
			hbox.PackStart (listView, true, true);
			hbox.PackStart (scrollView, true, true);
			hbox.Show ();

			Content = hbox;

			if (model.RowCount > 0) {
				listView.SelectRow (0);
				listView.SelectionChanged += TemplateSelected;
				TemplateSelected (listView, null);
			}

			Buttons.Add (Command.Ok, Command.Cancel);
		}

		ListView CreateItemTemplateList ()
		{
			var templates = IdeApp.Services.TemplatingService.GetItemTemplates ((ItemTemplate template) => {
				return template.Id.StartsWith ("Azure.Function.CSharp.", StringComparison.Ordinal);
			});
			var listView = new ListView (model) {
				HeadersVisible = false
			};
			var iconColumn = new ListViewColumn (GettextCatalog.GetString ("Icon"), new ImageCellView (iconField));
			var nameColumn = new ListViewColumn (GettextCatalog.GetString ("Name"), new TextCellView (nameField));

			listView.Columns.Add (iconColumn);
			listView.Columns.Add (nameColumn);
			listView.Show ();

			foreach (var template in templates) {
				int row = model.AddRow ();
				string path = null;
				Image icon = null;

				using (var reader = new StreamReader (template.GetStream ("${TemplateConfigDirectory}/vs-2017.3.host.json"))) {
					var json = JsonValue.Parse (reader.ReadToEnd ()) as JsonObject;
					JsonValue value;

					if (json != null && json.TryGetValue ("icon", out value) && value.JsonType == JsonType.String)
						path = value.ToString ();
				}

				if (path != null) {
					try {
						using (var stream = template.GetStream ("${TemplateConfigDirectory}" + path))
							icon = Image.FromStream (stream);
					} catch {
						icon = null;
					}
				}

				model.SetValues (row, iconField, icon, nameField, template.Name, templateField, template);
			}

			return listView;
		}

		void TemplateSelected (object sender, EventArgs e)
		{
			var listView = (ListView) sender;

			var template = model.GetValue (listView.SelectedRow, templateField);

			parameters = new AzureFunctionTemplateParameters (template);
			parameters.Show ();

			scrollView.Content = parameters;
		}

		protected override async void OnCommandActivated (Command cmd)
		{
			if (cmd == Command.Ok)
				await IdeApp.Services.TemplatingService.ProcessTemplate (parameters.Template, project, parameters.Configuration);

			base.OnCommandActivated (cmd);
		}
	}
}
