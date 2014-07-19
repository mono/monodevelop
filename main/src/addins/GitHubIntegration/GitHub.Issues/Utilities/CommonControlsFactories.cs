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
	}
}

