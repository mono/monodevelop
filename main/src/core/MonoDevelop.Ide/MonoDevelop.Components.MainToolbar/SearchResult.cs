// 
// NavigateToDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Text;
using Gdk;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Components.MainToolbar
{
	public enum SearchResultType
	{
		Unknown,
		File,
		Type,
		Member,
		Command
	}

	public abstract class SearchResult
	{
		protected string match;
		
		public virtual string GetMarkupText (bool selected)
		{
			return HighlightMatch (PlainText, match, selected);
		}

		public virtual string GetDescriptionMarkupText ()
		{
			return Ambience.EscapeText (Description);
		}


		public virtual SearchResultType SearchResultType { get { return SearchResultType.Unknown; } }
		public virtual string PlainText  { get { return null; } }

		public int Rank { get; private set; }

		public double Weight {
			get;
			set;
		}

		public virtual int Offset { get { return -1; } }
		public virtual int Length { get { return -1; } }
		
		public virtual string File { get { return null;} }
		public virtual Xwt.Drawing.Image Icon { get { return null; } }
		
		public virtual string Description { get { return null;} }
		public string MatchedString { get; private set;}

		public ISegment Segment { get { return new TextSegment (Offset, Length); } }

		public virtual Task<TooltipInformation> GetTooltipInformation (CancellationToken token)
		{
			return TaskUtil.Default<TooltipInformation> ();
		}

		public SearchResult (string match, string matchedString, int rank)
		{
			this.match = match;
			this.MatchedString = matchedString;
			Rank = rank;
		}

		static string selectedResultMatchTextColor = Styles.ColorGetHex (Styles.GlobalSearch.SelectedResultMatchTextColor);
		static string resultMatchTextColor = Styles.ColorGetHex (Styles.GlobalSearch.ResultMatchTextColor);
		protected static string HighlightMatch (string text, string toMatch, bool selected)
		{
			var lane = StringMatcher.GetMatcher (toMatch, true).GetMatch (text);
			var matchHexColor = selected ? selectedResultMatchTextColor : resultMatchTextColor;
			var result = StringBuilderCache.Allocate ();
			if (lane != null) {
				int lastPos = 0;
				for (int n=0; n < lane.Length; n++) {
					int pos = lane[n];
					if (pos - lastPos > 0)
						MarkupUtilities.AppendEscapedString (result, text, lastPos, pos - lastPos);
					result.Append ("<span foreground=\"");
					result.Append (matchHexColor);
					result.Append ("\" font_weight=\"bold\">");
					MarkupUtilities.AppendEscapedString (result, text, pos, 1);
					result.Append ("</span>");
					lastPos = pos + 1;
				}
				if (lastPos < text.Length)
					MarkupUtilities.AppendEscapedString (result, text, lastPos, text.Length - lastPos);
			} else {
				MarkupUtilities.AppendEscapedString (result, text, 0, text.Length);
			}
			return StringBuilderCache.ReturnAndFree  (result);
		}

		public virtual bool CanActivate {
			get { return false; }
		}

		public virtual void Activate ()
		{
		}

		public virtual bool IsValid {
			get { return true; }
		}
	}

	class FileSearchResult : SearchResult
	{
		ProjectFile file;

		public override SearchResultType SearchResultType { get { return SearchResultType.File; } }

		public override string PlainText {
			get {
				return System.IO.Path.GetFileName (file.FilePath);
			}
		}

		public override string File {
			get {
				return file.FilePath;
			}
		}

		public override Xwt.Drawing.Image Icon {
			get {
				return DesktopService.GetIconForFile (file.FilePath, IconSize.Menu);
			}
		}

		public override string Description {
			get {
				return file.Project != null
					? GettextCatalog.GetString ("file \"{0}\" in project \"{1}\"", GetRelProjectPath (file), file.Project.Name)
					: GettextCatalog.GetString ("file \"{0}\"", GetRelProjectPath (file));
			}
		}

		public FileSearchResult (string match, string matchedString, int rank, ProjectFile file)
							: base (match, matchedString, rank)
		{
			this.file = file;
		}

		internal static string GetRelProjectPath (ProjectFile file)
		{
			if (file.Project != null)
				return file.ProjectVirtualPath;
			return file.FilePath;
		}
	}

	class CommandResult: SearchResult
	{
		Command command;
		CommandInfo ci;
		CommandTargetRoute route;

		public CommandResult (Command cmd, CommandInfo ci, CommandTargetRoute route, string match, string matchedString, int rank): base (match, matchedString, rank)
		{
			this.ci = ci;
			command = cmd;
			this.route = route;
		}

		public override SearchResultType SearchResultType {
			get {
				return SearchResultType.Command;
			}
		}

		public override string PlainText {
			get {
				return MatchedString;
			}
		}

		public override string File {
			get {
				return null;
			}
		}

		public override Xwt.Drawing.Image Icon {
			get {
				return ImageService.GetIcon ("md-command", IconSize.Menu);
			}
		}

		public override string Description {
			get {
				string desc = "";
				if (!string.IsNullOrEmpty (ci.AccelKey))
					desc = KeyBindingManager.BindingToDisplayLabel (ci.AccelKey, false);
				if (!string.IsNullOrEmpty (ci.Description)) {
					if (desc.Length > 0)
						desc += " - ";
					desc += ci.Description;
				}
				else if (desc.Length == 0) {
					desc = "Command";
				}
				if (!string.IsNullOrEmpty (command.Category))
					desc += " (" + command.Category + ")";
				return desc;
			}
		}
		
		public override string GetMarkupText (bool selected)
		{
			return HighlightMatch (MatchedString, match, selected);
		}

		public override bool CanActivate {
			get {
				return true;
			}
		}

		public override void Activate ()
		{
			IdeApp.CommandService.DispatchCommand (command.Id, null, route.InitialTarget, CommandSource.MainToolbar);
		}

		public override bool IsValid {
			get {
				if (ci == null) {
					//GetCommandInfo throws exception if GetActionCommand returns null
					if (CommandManager.ToCommandId (IdeApp.CommandService.GetActionCommand (command.Id)) == null) {
						return false;
					}
					Runtime.RunInMainThread (delegate {
						ci = IdeApp.CommandService.GetCommandInfo (command.Id, new CommandTargetRoute (MainToolbar.LastCommandTarget));
					}).Wait ();
				}
				return ci.Enabled && ci.Visible;
			}
		}
	}
}
