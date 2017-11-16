#pragma warning disable 436

namespace MonoDevelop.SourceEditor.OptionPanels
{
	internal partial class CompletionOptionsPanel
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.Alignment alignment3;

		private global::Gtk.VBox vbox5;

		private global::Gtk.CheckButton autoCodeCompletionCheckbutton;

		private global::Gtk.HBox hbox6;

		private global::Gtk.Fixed fixed5;

		private global::Gtk.CheckButton automaticCompletionModeCheckbutton;

		private global::Gtk.HBox hbox4;

		private global::Gtk.Fixed fixed3;

		private global::Gtk.CheckButton includeKeywordsCheckbutton;

		private global::Gtk.HBox hbox5;

		private global::Gtk.Fixed fixed4;

		private global::Gtk.CheckButton includeCodeSnippetsCheckbutton;

		private global::Gtk.CheckButton showImportsCheckbutton;

		private global::Gtk.CheckButton insertParenthesesCheckbutton;

		private global::Gtk.HBox hbox2;

		private global::Gtk.Fixed fixed1;

		private global::Gtk.RadioButton openingRadiobutton;

		private global::Gtk.HBox hbox3;

		private global::Gtk.Fixed fixed2;

		private global::Gtk.RadioButton bothRadiobutton;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.SourceEditor.OptionPanels.CompletionOptionsPanel
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.SourceEditor.OptionPanels.CompletionOptionsPanel";
			// Container child MonoDevelop.SourceEditor.OptionPanels.CompletionOptionsPanel.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.alignment3 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment3.Name = "alignment3";
			this.alignment3.LeftPadding = ((uint)(12));
			// Container child alignment3.Gtk.Container+ContainerChild
			this.vbox5 = new global::Gtk.VBox();
			this.vbox5.Name = "vbox5";
			this.vbox5.Spacing = 6;
			// Container child vbox5.Gtk.Box+BoxChild
			this.autoCodeCompletionCheckbutton = new global::Gtk.CheckButton();
			this.autoCodeCompletionCheckbutton.TooltipMarkup = "Automatic Completion with Enter or Tab keys";
			this.autoCodeCompletionCheckbutton.CanFocus = true;
			this.autoCodeCompletionCheckbutton.Name = "autoCodeCompletionCheckbutton";
			this.autoCodeCompletionCheckbutton.Label = global::Mono.Unix.Catalog.GetString("_Show completion list after a character is typed");
			this.autoCodeCompletionCheckbutton.DrawIndicator = true;
			this.autoCodeCompletionCheckbutton.UseUnderline = true;
			this.vbox5.Add(this.autoCodeCompletionCheckbutton);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.autoCodeCompletionCheckbutton]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.hbox6 = new global::Gtk.HBox();
			this.hbox6.Name = "hbox6";
			this.hbox6.Spacing = 6;
			// Container child hbox6.Gtk.Box+BoxChild
			this.fixed5 = new global::Gtk.Fixed();
			this.fixed5.Name = "fixed5";
			this.fixed5.HasWindow = false;
			this.hbox6.Add(this.fixed5);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox6[this.fixed5]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Padding = ((uint)(6));
			// Container child hbox6.Gtk.Box+BoxChild
			this.automaticCompletionModeCheckbutton = new global::Gtk.CheckButton();
			this.automaticCompletionModeCheckbutton.TooltipMarkup = "Enables automatic completion with the Space key or Punctation";
			this.automaticCompletionModeCheckbutton.CanFocus = true;
			this.automaticCompletionModeCheckbutton.Name = "automaticCompletionModeCheckbutton";
			this.automaticCompletionModeCheckbutton.Label = global::Mono.Unix.Catalog.GetString("Complete with Space or Punctation");
			this.automaticCompletionModeCheckbutton.DrawIndicator = true;
			this.automaticCompletionModeCheckbutton.UseUnderline = true;
			this.hbox6.Add(this.automaticCompletionModeCheckbutton);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox6[this.automaticCompletionModeCheckbutton]));
			w3.Position = 1;
			this.vbox5.Add(this.hbox6);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.hbox6]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.hbox4 = new global::Gtk.HBox();
			this.hbox4.Name = "hbox4";
			this.hbox4.Spacing = 6;
			// Container child hbox4.Gtk.Box+BoxChild
			this.fixed3 = new global::Gtk.Fixed();
			this.fixed3.Name = "fixed3";
			this.fixed3.HasWindow = false;
			this.hbox4.Add(this.fixed3);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox4[this.fixed3]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Padding = ((uint)(6));
			// Container child hbox4.Gtk.Box+BoxChild
			this.includeKeywordsCheckbutton = new global::Gtk.CheckButton();
			this.includeKeywordsCheckbutton.CanFocus = true;
			this.includeKeywordsCheckbutton.Name = "includeKeywordsCheckbutton";
			this.includeKeywordsCheckbutton.Label = global::Mono.Unix.Catalog.GetString("Include _keywords in completion list");
			this.includeKeywordsCheckbutton.DrawIndicator = true;
			this.includeKeywordsCheckbutton.UseUnderline = true;
			this.hbox4.Add(this.includeKeywordsCheckbutton);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.hbox4[this.includeKeywordsCheckbutton]));
			w6.Position = 1;
			this.vbox5.Add(this.hbox4);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.hbox4]));
			w7.Position = 2;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.hbox5 = new global::Gtk.HBox();
			this.hbox5.Name = "hbox5";
			this.hbox5.Spacing = 6;
			// Container child hbox5.Gtk.Box+BoxChild
			this.fixed4 = new global::Gtk.Fixed();
			this.fixed4.Name = "fixed4";
			this.fixed4.HasWindow = false;
			this.hbox5.Add(this.fixed4);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox5[this.fixed4]));
			w8.Position = 0;
			w8.Expand = false;
			w8.Padding = ((uint)(6));
			// Container child hbox5.Gtk.Box+BoxChild
			this.includeCodeSnippetsCheckbutton = new global::Gtk.CheckButton();
			this.includeCodeSnippetsCheckbutton.CanFocus = true;
			this.includeCodeSnippetsCheckbutton.Name = "includeCodeSnippetsCheckbutton";
			this.includeCodeSnippetsCheckbutton.Label = global::Mono.Unix.Catalog.GetString("Include _code snippets in completion list");
			this.includeCodeSnippetsCheckbutton.DrawIndicator = true;
			this.includeCodeSnippetsCheckbutton.UseUnderline = true;
			this.hbox5.Add(this.includeCodeSnippetsCheckbutton);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.hbox5[this.includeCodeSnippetsCheckbutton]));
			w9.Position = 1;
			this.vbox5.Add(this.hbox5);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.hbox5]));
			w10.Position = 3;
			w10.Expand = false;
			w10.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.showImportsCheckbutton = new global::Gtk.CheckButton();
			this.showImportsCheckbutton.CanFocus = true;
			this.showImportsCheckbutton.Name = "showImportsCheckbutton";
			this.showImportsCheckbutton.Label = global::Mono.Unix.Catalog.GetString("_Show import items");
			this.showImportsCheckbutton.DrawIndicator = true;
			this.showImportsCheckbutton.UseUnderline = true;
			this.vbox5.Add(this.showImportsCheckbutton);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.showImportsCheckbutton]));
			w11.Position = 4;
			w11.Expand = false;
			w11.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.insertParenthesesCheckbutton = new global::Gtk.CheckButton();
			this.insertParenthesesCheckbutton.CanFocus = true;
			this.insertParenthesesCheckbutton.Name = "insertParenthesesCheckbutton";
			this.insertParenthesesCheckbutton.Label = global::Mono.Unix.Catalog.GetString("A_utomatically insert parentheses after completion:");
			this.insertParenthesesCheckbutton.DrawIndicator = true;
			this.insertParenthesesCheckbutton.UseUnderline = true;
			this.vbox5.Add(this.insertParenthesesCheckbutton);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.insertParenthesesCheckbutton]));
			w12.Position = 5;
			w12.Expand = false;
			w12.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.fixed1 = new global::Gtk.Fixed();
			this.fixed1.Name = "fixed1";
			this.fixed1.HasWindow = false;
			this.hbox2.Add(this.fixed1);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.fixed1]));
			w13.Position = 0;
			w13.Expand = false;
			w13.Padding = ((uint)(6));
			// Container child hbox2.Gtk.Box+BoxChild
			this.openingRadiobutton = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("_Opening only"));
			this.openingRadiobutton.CanFocus = true;
			this.openingRadiobutton.Name = "openingRadiobutton";
			this.openingRadiobutton.Active = true;
			this.openingRadiobutton.DrawIndicator = true;
			this.openingRadiobutton.UseUnderline = true;
			this.openingRadiobutton.Group = new global::GLib.SList(global::System.IntPtr.Zero);
			this.hbox2.Add(this.openingRadiobutton);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.openingRadiobutton]));
			w14.Position = 1;
			this.vbox5.Add(this.hbox2);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.hbox2]));
			w15.Position = 6;
			w15.Expand = false;
			w15.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.hbox3 = new global::Gtk.HBox();
			this.hbox3.Name = "hbox3";
			this.hbox3.Spacing = 6;
			// Container child hbox3.Gtk.Box+BoxChild
			this.fixed2 = new global::Gtk.Fixed();
			this.fixed2.Name = "fixed2";
			this.fixed2.HasWindow = false;
			this.hbox3.Add(this.fixed2);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.fixed2]));
			w16.Position = 0;
			w16.Expand = false;
			w16.Padding = ((uint)(6));
			// Container child hbox3.Gtk.Box+BoxChild
			this.bothRadiobutton = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("_Both opening and closing"));
			this.bothRadiobutton.CanFocus = true;
			this.bothRadiobutton.Name = "bothRadiobutton";
			this.bothRadiobutton.DrawIndicator = true;
			this.bothRadiobutton.UseUnderline = true;
			this.bothRadiobutton.Group = this.openingRadiobutton.Group;
			this.hbox3.Add(this.bothRadiobutton);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.bothRadiobutton]));
			w17.Position = 1;
			this.vbox5.Add(this.hbox3);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.hbox3]));
			w18.Position = 7;
			w18.Expand = false;
			w18.Fill = false;
			this.alignment3.Add(this.vbox5);
			this.vbox1.Add(this.alignment3);
			global::Gtk.Box.BoxChild w20 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.alignment3]));
			w20.Position = 0;
			w20.Expand = false;
			w20.Fill = false;
			this.Add(this.vbox1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
