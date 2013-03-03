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
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide;

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
			return AmbienceService.EscapeText (Description);
		}


		public abstract SearchResultType SearchResultType { get; }
		public abstract string PlainText  { get; }

		public int Rank { get; private set; }

		public virtual int Row { get { return -1; } }
		public virtual int Column { get { return -1; } }
		
		public abstract string File { get; }
		public abstract Gdk.Pixbuf Icon { get; }
		
		public abstract string Description { get; }
		public string MatchedString { get; private set;}

		public abstract TooltipInformation TooltipInformation { get; }

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
	
	class TypeSearchResult : MemberSearchResult
	{
		ITypeDefinition type;
			
		public override SearchResultType SearchResultType { get { return SearchResultType.Type; } }

		public override string File {
			get { return type.Region.FileName; }
		}
		
		public override Gdk.Pixbuf Icon {
			get {
				return ImageService.GetPixbuf (type.GetStockIcon (false), IconSize.Menu);
			}
		}
		
		public override int Row {
			get { return type.Region.BeginLine; }
		}
		
		public override int Column {
			get { return type.Region.BeginColumn; }
		}

		public static string GetPlainText (ITypeDefinition type, bool useFullName)
		{
			if (type.TypeParameterCount == 0)
				return useFullName ? type.FullName : type.Name;
			StringBuilder sb = new StringBuilder (useFullName ? type.FullName : type.Name);
			sb.Append ("<");
			for (int i = 0; i < type.TypeParameterCount; i++) {
				if (i > 0)
					sb.Append (", ");
				sb.Append (type.TypeParameters [i].Name);
			}
			sb.Append (">");
			return sb.ToString ();
		}
		
		public override string PlainText {
			get {
				return GetPlainText (type, false);
			}
		}

		public override MonoDevelop.Ide.CodeCompletion.TooltipInformation TooltipInformation {
			get {
				return Ambience.GetTooltip (type);
			}
		}

		public override string Description {
			get {
				string loc;
				if (type.GetSourceProject () != null) {
					loc = GettextCatalog.GetString ("project {0}", type.GetSourceProject ().Name);
				} else {
					loc = GettextCatalog.GetString ("file {0}", type.Region.FileName);
				}

				switch (type.Kind) {
				case TypeKind.Interface:
					return GettextCatalog.GetString ("interface ({0})", loc);
				case TypeKind.Struct:
					return GettextCatalog.GetString ("struct ({0})", loc);
				case TypeKind.Delegate:
					return GettextCatalog.GetString ("delegate ({0})", loc);
				case TypeKind.Enum:
					return GettextCatalog.GetString ("enumeration ({0})", loc);
				default:
					return GettextCatalog.GetString ("class ({0})", loc);
				}
			}
		}
		
		public override string GetMarkupText (Widget widget)
		{
			return HighlightMatch (widget, GetPlainText (type, useFullName), match);
		}
		
		public TypeSearchResult (string match, string matchedString, int rank, ITypeDefinition type, bool useFullName) : base (match, matchedString, rank, null, null, useFullName)
		{
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
		
		public override Gdk.Pixbuf Icon {
			get {
				return DesktopService.GetPixbufForFile (file.FilePath, IconSize.Menu);
			}
		}

		public override MonoDevelop.Ide.CodeCompletion.TooltipInformation TooltipInformation {
			get {
				return null;
			}
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
	
	class MemberSearchResult : SearchResult
	{
		protected bool useFullName;
		protected IUnresolvedMember member;
		protected ITypeDefinition declaringType;

		public override SearchResultType SearchResultType { get { return SearchResultType.Member; } }

		protected virtual OutputFlags Flags {
			get {
				return OutputFlags.IncludeParameters | OutputFlags.IncludeGenerics
					| (useFullName  ? OutputFlags.UseFullName : OutputFlags.None);
			}
		}
		
		public override string PlainText {
			get {
				return member.Name;
			}
		}

		public override MonoDevelop.Ide.CodeCompletion.TooltipInformation TooltipInformation {
			get {
				var ctx = member.DeclaringTypeDefinition.CreateResolveContext (new SimpleTypeResolveContext (declaringType));
				return Ambience.GetTooltip (member.Resolve (ctx));
			}
		}

		public override string File {
			get { return member.DeclaringTypeDefinition.Region.FileName; }
		}
		
		public override Gdk.Pixbuf Icon {
			get {
				return ImageService.GetPixbuf (member.GetStockIcon (false), IconSize.Menu);
			}
		}
		
		public override int Row {
			get { return member.Region.BeginLine; }
		}
		
		public override int Column {
			get { return member.Region.BeginColumn; }
		}
		
		public override string Description {
			get {
				string loc = GettextCatalog.GetString ("type \"{0}\"", member.DeclaringTypeDefinition.Name);

				switch (member.EntityType) {
				case EntityType.Field:
					return GettextCatalog.GetString ("field ({0})", loc);
				case EntityType.Property:
					return GettextCatalog.GetString ("property ({0})", loc);
				case EntityType.Indexer:
					return GettextCatalog.GetString ("indexer ({0})", loc);
				case EntityType.Event:
					return GettextCatalog.GetString ("event ({0})", loc);
				case EntityType.Method:
					return GettextCatalog.GetString ("method ({0})", loc);
				case EntityType.Operator:
					return GettextCatalog.GetString ("operator ({0})", loc);
				case EntityType.Constructor:
					return GettextCatalog.GetString ("constructor ({0})", loc);
				case EntityType.Destructor:
					return GettextCatalog.GetString ("destrutcor ({0})", loc);
				default:
					throw new NotSupportedException (member.EntityType + " is not supported.");
				}
			}
		}
		
		public MemberSearchResult (string match, string matchedString, int rank, ITypeDefinition declaringType, IUnresolvedMember member, bool useFullName) : base (match, matchedString, rank)
		{
			this.declaringType = declaringType;
			this.member = member;
			this.useFullName = useFullName;
		}
		
		public override string GetMarkupText (Widget widget)
		{
			if (useFullName)
				return HighlightMatch (widget, member.EntityType == EntityType.Constructor ? member.DeclaringTypeDefinition.FullName :  member.FullName, match);
			return HighlightMatch (widget, member.EntityType == EntityType.Constructor ? member.DeclaringTypeDefinition.Name : member.Name, match);
		}
		
		internal Ambience Ambience { 
			get;
			set;
		}
	}

	class CommandResult: SearchResult
	{
		Command command;

		public CommandResult (Command cmd, string match, string matchedString, int rank): base (match, matchedString, rank)
		{
			command = cmd;
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

		public override Pixbuf Icon {
			get {
				return ImageService.GetPixbuf ("md-command", IconSize.Menu);
			}
		}

		public override MonoDevelop.Ide.CodeCompletion.TooltipInformation TooltipInformation {
			get {
				return null;
			}
		}

		public override string Description {
			get {
				string desc = "";
				if (!string.IsNullOrEmpty (command.AccelKey))
					desc = KeyBindingManager.BindingToDisplayLabel (command.AccelKey, false);
				if (!string.IsNullOrEmpty (command.Description)) {
					if (desc.Length > 0)
						desc += " - ";
					desc += command.Description;
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
			IdeApp.CommandService.DispatchCommand (command.Id);
		}
	}
}
