// CommitMessageStylePanelWidget.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.VersionControl
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class CommitMessageStylePanelWidget : Gtk.Bin
	{
		CommitMessageFormat format;
		AuthorInformation uinfo;
		bool updating;
		
		public event EventHandler Changed;
		
		public CommitMessageStylePanelWidget()
		{
			this.Build();
			
			Pango.FontDescription font = Pango.FontDescription.FromString (
			   MonoDevelop.Core.Gui.DesktopService.DefaultMonospaceFont);
			font.Size = Pango.Units.FromPixels (8);
			textview.ModifyFont (font);
			textview.AcceptsTab = true;
			font.Dispose ();
		}
		
		public void Load (CommitMessageFormat format, AuthorInformation uinfo)
		{
			updating = true;
			this.format = format;
			this.uinfo = uinfo;
			checkIndent.Active = format.Style.LineAlign != 0;
			checkLineSep.Active = format.Style.InterMessageLines != 0;
			checkMsgInNewLine.Active = format.Style.LastFilePostfix == ":\n";
			checkOneLinePerFile.Active = format.Style.FileSeparator == ":\n* ";
			checkUseBullets.Active = format.Style.FirstFilePrefix.Trim ().Length > 0;
			checkIndentEntries.Active = format.Style.Indent.Length > 0;
			entryHeader.Text = ToCString (format.Style.Header.TrimEnd ('\n'));
			UpdatePreview ();
			updating = false;
		}
		
		void UpdatePreview ()
		{
			if (format == null)
				return;
			ChangeLogWriter writer = new ChangeLogWriter ("./", uinfo);
			string msg = "My changes made additional changes. This is sample documentation.";
			writer.AddFile (msg, "./myfile.ext");
			writer.AddFile (msg, "./yourfile.ext");
			writer.AddFile ("Some additional changes on another file of the project.", "./otherfile.ext");
			format.MaxColumns = 60;
			writer.MessageFormat = format;
			
			textview.Buffer.Text = writer.ToString ();
		}

		protected virtual void OnCheckIndentToggled (object sender, System.EventArgs e)
		{
			if (updating) return;
			UpdateBullets ();
			OnChanged ();
		}

		protected virtual void OnCheckLineSepToggled (object sender, System.EventArgs e)
		{
			if (updating) return;
			format.Style.InterMessageLines = checkLineSep.Active ? 1 : 0;
			OnChanged ();
		}

		protected virtual void OnCheckOneLinePerFileToggled (object sender, System.EventArgs e)
		{
			if (updating) return;
			UpdateBullets ();
			OnChanged ();
		}

		protected virtual void OnCheckMsgInNewLineToggled (object sender, System.EventArgs e)
		{
			if (updating) return;
			UpdateBullets ();
			OnChanged ();
		}

		protected virtual void OnEntryHeaderChanged (object sender, System.EventArgs e)
		{
			if (updating) return;
			format.Style.Header = !string.IsNullOrEmpty (entryHeader.Text) ? FromCString (entryHeader.Text) + "\n\n" : "";
			OnChanged ();
		}
		
		string FromCString (string txt)
		{
			return txt.Replace ("\\t","\t").Replace ("\\n","\n");
		}
		
		string ToCString (string txt)
		{
			return txt.Replace ("\t","\\t").Replace ("\n","\\n");
		}
		
		void UpdateBullets ()
		{
			format.Style.FileSeparator = checkOneLinePerFile.Active ? ":\n" + format.Style.FirstFilePrefix : ", ";
			format.Style.LineAlign = checkIndent.Active ? format.Style.FirstFilePrefix.Length : 0;
			format.Style.LastFilePostfix = checkMsgInNewLine.Active ? ":\n" + new string (' ',format.Style.LineAlign): ": ";
		}
		
		void OnChanged ()
		{
			UpdatePreview ();
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		protected virtual void OnCheckUseBulletsToggled (object sender, System.EventArgs e)
		{
			if (updating) return;
			if (checkUseBullets.Active)
				format.Style.FirstFilePrefix = "* ";
			else
				format.Style.FirstFilePrefix = "";
			UpdateBullets ();
			OnChanged ();
		}

		protected virtual void OnCheckIndentEntriesToggled (object sender, System.EventArgs e)
		{
			if (updating) return;
			format.Style.Indent = checkIndentEntries.Active ? "\t" : "";
			OnChanged ();
		}
	}
}
