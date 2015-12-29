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
		
		public virtual string GetMarkupText (Widget widget)
		{
			return HighlightMatch (widget, PlainText, match);
		}

		public virtual string GetDescriptionMarkupText (Widget widget)
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
		
		protected static string HighlightMatch (Widget widget, string text, string toMatch)
		{
			var lane = StringMatcher.GetMatcher (toMatch, true).GetMatch (text);
			StringBuilder result = new StringBuilder ();
			if (lane != null) {
				int lastPos = 0;
				for (int n=0; n < lane.Length; n++) {
					int pos = lane[n];
					if (pos - lastPos > 0)
						MarkupUtilities.AppendEscapedString (result, text.Substring (lastPos, pos - lastPos));
					result.Append ("<span foreground=\"#4d4d4d\" font_weight=\"bold\">");
					MarkupUtilities.AppendEscapedString (result, text[pos].ToString ());
					result.Append ("</span>");
					lastPos = pos + 1;
				}
				if (lastPos < text.Length)
					MarkupUtilities.AppendEscapedString (result, text.Substring (lastPos, text.Length - lastPos));
			} else {
				MarkupUtilities.AppendEscapedString (result, text);
			}
			return result.ToString ();
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

	class FileSearchResult: SearchResult
	{
		ProjectFile file;
		bool useFileName;

		public override SearchResultType SearchResultType { get { return SearchResultType.File; } }

		public override string PlainText {
			get {
				if (useFileName)
					return System.IO.Path.GetFileName (file.FilePath);
				return GetRelProjectPath (file);
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

		public override Task<TooltipInformation> GetTooltipInformation (CancellationToken token)
		{
			return Task.FromResult<TooltipInformation> (null);
		}

		public override string Description {
			get {
				if (useFileName)
					return file.Project != null
						? GettextCatalog.GetString ("file \"{0}\" in project \"{1}\"", GetRelProjectPath (file), file.Project.Name)
						: GettextCatalog.GetString ("file \"{0}\"", GetRelProjectPath (file));
				return file.Project != null ? GettextCatalog.GetString ("file in project \"{0}\"", file.Project.Name) : "";
			}
		}
		
		public FileSearchResult (string match, string matchedString, int rank, ProjectFile file, bool useFileName)
							: base (match, matchedString, rank)
		{
			this.file = file;
			this.useFileName = useFileName;
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

		public override Task<TooltipInformation> GetTooltipInformation (CancellationToken token)
		{
			return Task.FromResult<TooltipInformation> (null);
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
		
		public override string GetMarkupText (Widget widget)
		{
			return HighlightMatch (widget, MatchedString, match);
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
