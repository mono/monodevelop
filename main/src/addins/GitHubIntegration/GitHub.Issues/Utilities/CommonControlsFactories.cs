using System;

namespace GitHub.Issues
{
	/// <summary>
	/// Common controls factories.
	/// Everything is created in here and uses the common methods so the common controls can be easily styled by modifing each method
	/// </summary>
	public class CommonControlsFactories
	{
		/// <summary>
		/// Default line height for the controls with default font
		/// </summary>
		private uint defaultLineHeight = 18;
		/// <summary>
		/// The default text padding (text boxes and labels)
		/// </summary>
		private uint defaultTextPadding = 3;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHub.Issues.CommonControlsFactories"/> class.
		/// </summary>
		public CommonControlsFactories ()
		{
		}

		/// <summary>
		/// Creates a label with a given text
		/// </summary>
		/// <returns>The label.</returns>
		/// <param name="text">Text.</param>
		public Gtk.Label CreateLabel(String text)
		{
			Gtk.Label label = new Gtk.Label (text);

			return label;
		}

		/// <summary>
		/// Creates a text box with a given text
		/// </summary>
		/// <returns>The text box.</returns>
		/// <param name="text">Text.</param>
		/// <param name="lineNumbers">Number of lines - sets height</param>
		/// <param name="wrapMode">Wrap mode</param>
		public Gtk.TextView CreateTextBox(String text, int lineNumbers, Gtk.WrapMode wrapMode)
		{
			Gtk.TextView textBox = new Gtk.TextView ();
			textBox.WrapMode = wrapMode;
			textBox.HeightRequest = Convert.ToInt32(this.defaultLineHeight * lineNumbers);
			textBox.Buffer.Text = text;

			return textBox;
		}

		/// <summary>
		/// Creates a text box with a given text
		/// </summary>
		/// <returns>The text box.</returns>
		/// <param name="text">Text.</param>
		/// <param name="lineNumbers">Number of lines - sets height</param>
		public Gtk.TextView CreateTextBox(String text, int lineNumbers)
		{
			Gtk.TextView textBox = new Gtk.TextView ();
			textBox.HeightRequest = Convert.ToInt32(this.defaultLineHeight * lineNumbers);
			textBox.Buffer.Text = text;

			return textBox;
		}

		/// <summary>
		/// Creates a button
		/// </summary>
		/// <returns>The button.</returns>
		/// <param name="text">Text to display in the button.</param>
		/// <param name="clickHandler">Handler to be used for the button click.</param>
		public Gtk.Button CreateButton(String text, EventHandler clickHandler)
		{
			Gtk.Button button = new Gtk.Button ();

			button.Label = text;
			button.Clicked += clickHandler;

			return button;
		}

		/// <summary>
		/// Creates the combo box with the specified options and changed handler
		/// </summary>
		/// <returns>The combo box.</returns>
		/// <param name="items">Items.</param>
		/// <param name="changedHandler">Changed handler.</param>
		/// <param name="columnToShow">Column to show.</param>
		/// <param name="propertyToShow">Property to show.</param>
		public Gtk.ComboBox CreateComboBox(String[] items, EventHandler changedHandler, int columnToShow, String propertyToShow)
		{
			Gtk.ComboBox comboBox = new Gtk.ComboBox ();

			foreach (String item in items) {
				comboBox.AppendText (item);
			}

			Gtk.CellRenderer cell = new Gtk.CellRendererText ();

			comboBox.PackStart (cell, true);
			comboBox.AddAttribute (cell, propertyToShow, columnToShow);

			comboBox.Changed += changedHandler;

			return comboBox;
		}

		/// <summary>
		/// Creates the combo box with the specified store and changed handler
		/// </summary>
		/// <returns>The combo box.</returns>
		/// <param name="store">Store.</param>
		/// <param name="changedHandler">Changed handler.</param>
		/// <param name="columnToShow">Column to show.</param>
		/// <param name="propertyToShow">Property to show.</param>
		public Gtk.ComboBox CreateComboBox(Gtk.ListStore store, EventHandler changedHandler, int columnToShow, String propertyToShow)
		{
			Gtk.ComboBox comboBox = new Gtk.ComboBox ();

			comboBox.Model = store;

			Gtk.CellRenderer cell = new Gtk.CellRendererText ();

			comboBox.PackStart (cell, true);
			comboBox.AddAttribute (cell, propertyToShow, columnToShow);

			comboBox.Changed += changedHandler;

			return comboBox;
		}

		/// <summary>
		/// Creates the combo box with the specified store and changed handler
		/// </summary>
		/// <returns>The combo box.</returns>
		/// <param name="store">Store.</param>
		/// <param name="changedHandler">Changed handler.</param>
		/// <param name="columnToShow">Column to show.</param>
		/// <param name="propertyToShow">Property to show.</param>
		/// <param name="columnToShow2">Column to show2.</param>
		/// <param name="propertyToShow2">Property to show2.</param>
		public Gtk.ComboBox CreateComboBox(Gtk.ListStore store, EventHandler changedHandler, int columnToShow, String propertyToShow, int columnToShow2, String propertyToShow2)
		{
			Gtk.ComboBox comboBox = new Gtk.ComboBox ();

			comboBox.Model = store;

			Gtk.CellRenderer cell = new Gtk.CellRendererText ();

			comboBox.PackStart (cell, true);
			comboBox.AddAttribute (cell, propertyToShow, columnToShow);

			Gtk.CellRenderer cell2 = new Gtk.CellRendererText ();

			comboBox.PackStart (cell2, true);
			comboBox.AddAttribute (cell2, propertyToShow2, columnToShow2);

			comboBox.Changed += changedHandler;

			return comboBox;
		}
	}
}

