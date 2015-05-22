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
using ICSharpCode.NRefactory6.CSharp;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Components.MainToolbar
{
	public enum SearchResultType
	{
		File,
		Type,
		Member,
		Command
	}

	abstract class SearchResult
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


		public abstract SearchResultType SearchResultType { get; }
		public abstract string PlainText  { get; }

		public int Rank { get; private set; }

		public virtual int Offset { get { return -1; } }
		public virtual int Length { get { return -1; } }
		
		public abstract string File { get; }
		public abstract Xwt.Drawing.Image Icon { get; }
		
		public abstract string Description { get; }
		public string MatchedString { get; private set;}

		public abstract Task<TooltipInformation> GetTooltipInformation (CancellationToken token);

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
	}
	
	class DeclaredSymbolInfoResult : SearchResult
	{
		bool useFullName;

		DeclaredSymbolInfo type;
			
		public override SearchResultType SearchResultType { get { return SearchResultType.Type; } }

		public override string File {
			get { return type.FilePath; }
		}
		
		public override Xwt.Drawing.Image Icon {
			get {
				return ImageService.GetIcon (type.GetStockIconForSymbolInfo(), IconSize.Menu);
			}
		}

		
		public override int Offset {
			get { return type.Span.Start; }
		}

		public override int Length {
			get { return type.Span.Length; }
		}
			
		public override string PlainText {
			get {
				return type.Name;
			}
		}

		public override async Task<TooltipInformation> GetTooltipInformation (CancellationToken token)
		{
			var docId = TypeSystemService.GetDocuments (type.FilePath).FirstOrDefault ();
			if (docId == null)
				return new TooltipInformation ();
			
			var symbol = await type.GetSymbolAsync (TypeSystemService.GetCodeAnalysisDocument (docId, token), token);
			return await Ambience.GetTooltip (token, symbol);
		}

		public override string Description {
			get {
				string loc;
				MonoDevelop.Projects.Project project;
//				if (type.TryGetSourceProject (out project)) {
//					loc = GettextCatalog.GetString ("project {0}", project.Name);
//				} else {
				loc = GettextCatalog.GetString ("file {0}", File);
//				}

				switch (type.Kind) {
				case DeclaredSymbolInfoKind.Interface:
					return GettextCatalog.GetString ("interface ({0})", loc);
				case DeclaredSymbolInfoKind.Struct:
					return GettextCatalog.GetString ("struct ({0})", loc);
				case DeclaredSymbolInfoKind.Delegate:
					return GettextCatalog.GetString ("delegate ({0})", loc);
				case DeclaredSymbolInfoKind.Enum:
					return GettextCatalog.GetString ("enumeration ({0})", loc);
				case DeclaredSymbolInfoKind.Class:
					return GettextCatalog.GetString ("class ({0})", loc);

				case DeclaredSymbolInfoKind.Field:
					return GettextCatalog.GetString ("field ({0})", loc);
				case DeclaredSymbolInfoKind.Property:
					return GettextCatalog.GetString ("property ({0})", loc);
				case DeclaredSymbolInfoKind.Indexer:
					return GettextCatalog.GetString ("indexer ({0})", loc);
				case DeclaredSymbolInfoKind.Event:
					return GettextCatalog.GetString ("event ({0})", loc);
				case DeclaredSymbolInfoKind.Method:
					return GettextCatalog.GetString ("method ({0})", loc);
				}
				return GettextCatalog.GetString ("symbol ({0})", loc);
			}
		}
		
		public override string GetMarkupText (Widget widget)
		{
			return HighlightMatch (widget, useFullName ? type.FullyQualifiedContainerName : type.Name, match);
		}
		
		public DeclaredSymbolInfoResult (string match, string matchedString, int rank, DeclaredSymbolInfo type, bool useFullName)  : base (match, matchedString, rank)
		{
			this.useFullName = useFullName;
			this.type = type;
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
	}
}
