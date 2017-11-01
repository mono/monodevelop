//
// AzureFunctionTemplateParameters.cs
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
using System.Linq;
using System.Collections.Generic;

using Xwt;

using MonoDevelop.Core;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.AzureFunctions
{
	public class AzureFunctionTemplateParameters : Table
	{
		readonly Dictionary<string, object> parameters = new Dictionary<string, object> ();

		internal class SymbolName
		{
			public readonly string Id;
			public readonly string Text;
			public readonly string Package;

			public SymbolName (string id, string text, string package)
			{
				Id = id;
				Text = text;
				Package = package;
			}
		}

		internal class SymbolInfo
		{
			public readonly string Id;
			public readonly SymbolName Name;
			public readonly bool IsVisible;

			public SymbolInfo (string id, SymbolName name, bool isVisible)
			{
				Id = id;
				Name = name;
				IsVisible = isVisible;
			}
		}

		public AzureFunctionTemplateParameters (FilePath template)
		{
			var symbols = LoadSymbols (template);
			int row = 0;

			DefaultRowSpacing = 6;
			DefaultColumnSpacing = 6;

			foreach (var item in EnumerateSymbolInfo (template)) {
				JsonValue value;

				if (!item.IsVisible)
					continue;

				if (!symbols.TryGetValue (item.Id, out value) || value.JsonType != JsonType.Object)
					continue;

				string datatype, description = null, defaultValue = null;
				var symbol = (JsonObject) value;
				Widget widget;

				if (symbol.TryGetValue ("defaultValue", out value) && value.JsonType == JsonType.String)
					defaultValue = value.ToString ();

				if (symbol.TryGetValue ("description", out value) && value.JsonType == JsonType.String)
					description = value.ToString ();

				if (symbol.TryGetValue ("datatype", out value))
					datatype = value.ToString ();
				else
					datatype = "string";

				switch (datatype) {
				case "string": case "text":
					widget = CreateText (item.Id, symbol, defaultValue);
					break;
				case "choice":
					widget = CreateChoices (item.Id, symbol, defaultValue);
					break;
				case "bool":
					widget = CreateBool (item.Id, symbol, defaultValue ?? "false");
					break;
				case "float":
					widget = CreateFloat (item.Id, symbol, defaultValue ?? "0.0");
					break;
				case "integer": case "int":
					widget = CreateInteger (item.Id, symbol, defaultValue ?? "0");
					break;
				case "hex":
					widget = CreateHex (item.Id, symbol, defaultValue);
					break;
				default:
					widget = null;
					break;
				}

				if (widget == null)
					continue;

				widget.Show ();

				if (!string.IsNullOrEmpty (description)) {
					var icon = new ImageView (StockIcons.Information) {
						TooltipText = description
					};
					icon.Show ();

					var hbox = new HBox { Spacing = 6 };
					hbox.PackStart (widget, true, true);
					hbox.PackStart (icon, false, false);
					hbox.Show ();

					widget = hbox;
				}

				var label = new Label (item.Name.Text + ":");
				label.Show ();

				InsertRow (row, row + 1);

				Add (label, 0, row);
				Add (widget, 1, row, hexpand: true);
			}
		}

		void BoolChanged (object sender, EventArgs e)
		{
			var toggle = (ToggleButton) sender;

			parameters[toggle.Name] = toggle.Active;

			toggle.Label = toggle.Active ? GettextCatalog.GetString ("True") : GettextCatalog.GetString ("False");
		}

		void ChoiceChanged (object sender, EventArgs e)
		{
			var combo = (ComboBox) sender;

			parameters[combo.Name] = combo.SelectedItem;
		}

		void FloatChanged (object sender, EventArgs e)
		{
			var spin = (SpinButton) sender;

			parameters[spin.Name] = spin.Value;
		}

		void HexChanged (object sender, EventArgs e)
		{
			var entry = (TextEntry) sender;

			parameters[entry.Name] = entry.Text;
		}

		void IntegerChanged (object sender, EventArgs e)
		{
			var spin = (SpinButton) sender;

			parameters[spin.Name] = Convert.ToInt32 (spin.Value);
		}

		void TextChanged (object sender, TextInputEventArgs e)
		{
			var entry = (TextEntry) sender;

			parameters[entry.Name] = entry.Text;
		}

		Widget CreateBool (string name, JsonObject symbol, string defaultValue)
		{
			bool.TryParse (defaultValue, out bool active);
			var toggle = new ToggleButton { Name = name, Active = active };
			toggle.Toggled += BoolChanged;
			BoolChanged (toggle, null);
			return toggle;
		}

		Widget CreateChoices (string name, JsonObject symbol, string defaultValue)
		{
			var combo = new ComboBox { Name = name };
			int defaultIndex = -1, i = 0;
			JsonValue value;

			if (!symbol.TryGetValue ("choices", out value) || value.JsonType != JsonType.Array)
				return combo;

			var choices = (JsonArray) value;

			foreach (var option in choices.OfType<JsonObject> ()) {
				if (!option.TryGetValue ("choice", out value) || value.JsonType != JsonType.String)
					continue;

				var choice = value.ToString ();

				if (!option.TryGetValue ("description", out value) || value.JsonType != JsonType.String)
					continue;

				var description = value.ToString ();

				combo.Items.Add (choice, description);

				if (defaultIndex == -1 && choice == defaultValue)
					defaultIndex = i;

				i++;
			}

			combo.SelectedIndex = defaultIndex == -1 ? 0 : defaultIndex;
			combo.SelectionChanged += ChoiceChanged;
			ChoiceChanged (combo, null);

			return combo;
		}

		Widget CreateFloat (string name, JsonObject symbol, string defaultValue)
		{
			double.TryParse (defaultValue, out double value);
			var spin = new SpinButton {
				MinimumValue = float.MinValue,
				MaximumValue = float.MaxValue,
				IncrementValue = 1,
				Value = value,
				Name = name,
				Digits = 2
			};

			spin.ValueChanged += FloatChanged;
			FloatChanged (spin, null);

			return spin;
		}

		Widget CreateHex (string name, JsonObject symbol, string defaultValue)
		{
			var entry = new TextEntry { Name = name, Text = defaultValue ?? string.Empty };
			entry.TextInput += TextChanged;
			HexChanged (entry, null);

			return entry;
		}

		Widget CreateInteger (string name, JsonObject symbol, string defaultValue)
		{
			int.TryParse (defaultValue, out int value);
			var spin = new SpinButton {
				MinimumValue = int.MinValue,
				MaximumValue = int.MaxValue,
				IncrementValue = 1,
				Value = value,
				Name = name,
				Digits = 0
			};

			spin.ValueChanged += IntegerChanged;
			IntegerChanged (spin, null);

			return spin;
		}

		Widget CreateText (string name, JsonObject symbol, string defaultValue)
		{
			var entry = new TextEntry { Name = name, Text = defaultValue ?? string.Empty };
			entry.TextInput += TextChanged;
			TextChanged (entry, null);

			return entry;
		}

		internal static JsonObject LoadSymbols (FilePath template)
		{
			var content = File.ReadAllText (template.Combine (".template.config", "template.json"));
			var json = JsonValue.Parse (content) as JsonObject;
			JsonValue value;

			if (json == null || !json.TryGetValue ("symbols", out value) || value.JsonType != JsonType.Object)
				throw new FormatException ("Invalid template.json format");

			return (JsonObject) value;
		}

		static bool TryParse (JsonObject json, out SymbolName name)
		{
			string id, text, package;
			JsonValue value;

			name = null;

			if (!json.TryGetValue ("id", out value) || value.JsonType != JsonType.String)
				return false;

			id = value.ToString ();

			if (!json.TryGetValue ("text", out value) || value.JsonType != JsonType.String)
				return false;

			text = value.ToString ();

			if (!json.TryGetValue ("package", out value) || value.JsonType != JsonType.String)
				return false;

			package = value.ToString ();

			name = new SymbolName (id, text, package);

			return true;
		}

		internal static IEnumerable<SymbolInfo> EnumerateSymbolInfo (FilePath template)
		{
			var content = File.ReadAllText (template.Combine (".template.config", "vs-2017.3.host.json"));
			var json = JsonValue.Parse (content) as JsonObject;
			JsonValue value;

			if (json == null || !json.TryGetValue ("symbolInfo", out value) || value.JsonType != JsonType.Array)
				yield break;

			var symbolInfo = (JsonArray) value;

			foreach (var item in symbolInfo.OfType<JsonObject> ()) {
				if (!item.TryGetValue ("id", out value) || value.JsonType != JsonType.String)
					continue;

				var id = value.ToString ();
				SymbolName name;

				if (!item.TryGetValue ("name", out value) || value.JsonType != JsonType.Object)
					continue;

				if (!TryParse ((JsonObject) value, out name))
					continue;

				if (!item.TryGetValue ("isVisible", out value) || value.JsonType != JsonType.Boolean)
					continue;

				var visible = bool.Parse (value.ToString ());

				yield return new SymbolInfo (id, name, visible);
			}

			yield break;
		}
	}
}
