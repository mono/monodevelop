using System;
using Gtk;
using MonoDevelop.Core.AddIns;

public class AddInDetailsFrame : Frame
{
	Label author;
	Label description;
	Label copyright;
	Label url;
	// RuntimeLibraries
	// Extensions

	public AddInDetailsFrame ()
	{
		VBox vbox = new VBox ();
		author = new Label ();
		vbox.PackStart (author, false, true, 0);
		copyright = new Label ();
		vbox.PackStart (copyright, false, true, 0);
		url = new Label ();
		vbox.PackStart (url, false, true, 0);
		description = new Label ();
		vbox.PackStart (description, false, true, 0);
		this.Add (vbox);
	}

	public void Clear ()
	{
		author.Text = "Author: ";
		description.Text = "Description: ";
		copyright.Text = "Copyright: ";
		url.Text = "Url: ";
	}

	public void SetAddin (AddIn addin)
	{
		author.Text = "Author: " + addin.Author;
		description.Text = "Description: " + addin.Description;
		copyright.Text = "Copyright: " + addin.Copyright;
		url.Text = "Url: " + addin.Url;
	}
}

