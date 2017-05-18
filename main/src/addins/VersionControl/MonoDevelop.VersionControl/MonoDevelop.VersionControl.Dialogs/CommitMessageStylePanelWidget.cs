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
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Core;

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

			textview.AcceptsTab = true;

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			entryHeader.SetCommonAccessibilityAttributes ("CommitMessageStyle.HeaderEntry",
			                                              GettextCatalog.GetString ("Message Header"),
			                                              GettextCatalog.GetString ("Enter the commit message header"));
			entryHeader.SetAccessibilityLabelRelationship (label4);

			checkUseBullets.SetCommonAccessibilityAttributes ("CommitMessageStyle.UseBullets", null,
			                                                  GettextCatalog.GetString ("Check to use bullets for each entry"));
			checkIndentEntries.SetCommonAccessibilityAttributes ("CommitMessageStyle.IndentEntries", null,
			                                                     GettextCatalog.GetString ("Check to indent each entry"));
			checkIndent.SetCommonAccessibilityAttributes ("CommitMessageStyle.CheckIndent", null,
			                                              GettextCatalog.GetString ("Check to align the message text"));
			checkLineSep.SetCommonAccessibilityAttributes ("CommitMessageStyle.CheckLineSep", null,
			                                               GettextCatalog.GetString ("Check to add a blank line between messages"));
			checkOneLinePerFile.SetCommonAccessibilityAttributes ("CommitMessageStyle.CheckOneLine", null,
			                                                      GettextCatalog.GetString ("Check to add one line per file changed"));
			checkMsgInNewLine.SetCommonAccessibilityAttributes ("CommitMessageStyle.CheckMsgNewLine", null,
			                                                    GettextCatalog.GetString ("Check to keep the file name and messages on separate lines"));
			checkIncludeDirs.SetCommonAccessibilityAttributes ("CommitMessageStyle.CheckIncludeDirs", null,
			                                                   GettextCatalog.GetString ("Check to include file directories"));
			checkWrap.SetCommonAccessibilityAttributes ("CommitMessageStyle.Wrap", null,
			                                            GettextCatalog.GetString ("Check to wrap the lines at 60 characters"));
			textview.SetCommonAccessibilityAttributes ("CommitMessagesStyle.Preview",
			                                           GettextCatalog.GetString ("Preview"),
			                                           GettextCatalog.GetString ("A preview of the settings above"));
			textview.SetAccessibilityLabelRelationship (label9);
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
			checkIncludeDirs.Active = format.Style.IncludeDirectoryPaths;
			entryHeader.Text = ToCString (format.Style.Header.TrimEnd ('\n'));
			checkWrap.Active = format.Style.Wrap;
			UpdatePreview ();
			updating = false;
		}
		
		void UpdatePreview ()
		{
			if (format == null)
				return;
			ChangeLogWriter writer = new ChangeLogWriter ("./", uinfo);
			string msg = GettextCatalog.GetString ("My changes made additional changes. This is sample documentation.");
			writer.AddFile (msg, GettextCatalog.GetString ("./somedir/myfile.ext"));
			writer.AddFile (msg, GettextCatalog.GetString ("./yourfile.ext"));
			writer.AddFile (GettextCatalog.GetString ("Some additional changes on another file of the project."), GettextCatalog.GetString ("./otherfile.ext"));
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
		
		static string FromCString (string txt)
		{
			return txt.Replace ("\\t", "\t").Replace ("\\n", "\n");
		}
		
		static string ToCString (string txt)
		{
			return txt.Replace ("\t", "\\t").Replace ("\n", "\\n");
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
			Changed?.Invoke (this, EventArgs.Empty);
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
		
		protected virtual void OnCheckIncludeDirsToggled (object sender, System.EventArgs e)
		{
			if (updating) return;
			format.Style.IncludeDirectoryPaths = checkIncludeDirs.Active;
			OnChanged ();
		}
		
		protected virtual void OnCheckWrapToggled (object sender, System.EventArgs e)
		{
			if (updating) return;
			format.Style.Wrap = checkWrap.Active;
			OnChanged ();
		}
	}
}
