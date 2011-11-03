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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;

namespace MonoDevelop.Ide.NavigateToDialog
{
	abstract class SearchResult
	{
		protected string match;
		
		public virtual string GetMarkupText (Widget widget)
		{
			return HighlightMatch (widget, PlainText, match);
		}
		
		public abstract string PlainText  { get; }
		
		public int Rank { get; private set; }

		public virtual int Row { get { return -1; } }
		public virtual int Column { get { return -1; } }
		
		public abstract string File { get; }
		public abstract Gdk.Pixbuf Icon { get; }
		
		public abstract string Description { get; }
		public string MatchedString { get; private set;}
		
		public SearchResult (string match, string matchedString, int rank)
		{
			this.match = match;
			this.MatchedString = matchedString;
			Rank = rank;
		}
		
		protected static string HighlightMatch (Widget widget, string text, string toMatch)
		{
			var lane = StringMatcher.GetMatcher (toMatch, false).GetMatch (text);
			if (lane != null) {
				StringBuilder result = new StringBuilder ();
				int lastPos = 0;
				for (int n=0; n < lane.Length; n++) {
					int pos = lane[n];
					if (pos - lastPos > 0)
						result.Append (GLib.Markup.EscapeText (text.Substring (lastPos, pos - lastPos)));
					result.Append ("<span foreground=\"");
					var color = Mono.TextEditor.HslColor.GenerateHighlightColors (widget.Style.Base (StateType.Normal), 
						widget.Style.Text (StateType.Normal), 3)[2];
					result.Append (color.ToPangoString ());
					result.Append ("\">");
					result.Append (GLib.Markup.EscapeText (text[pos].ToString ()));
					result.Append ("</span>");
					lastPos = pos + 1;
				}
				if (lastPos < text.Length)
					result.Append (GLib.Markup.EscapeText (text.Substring (lastPos, text.Length - lastPos)));
				return result.ToString ();
			}
			
			return GLib.Markup.EscapeText (text);
		}
	}
	
	class TypeSearchResult : MemberSearchResult
	{
		public override string File {
			get {
				var cu = ((IType)member).CompilationUnit;
				return cu != null ? cu.FileName : null;
			}
		}
		
		public override string Description {
			get {
				IType type = (IType)member;
				if (useFullName) 
					return type.SourceProject != null
						? GettextCatalog.GetString ("from Project \"{0}\"", type.SourceProject.Name ?? "")
						: GettextCatalog.GetString ("from \"{0}\"", (string)type.CompilationUnit.FileName ?? "");
				if (type.SourceProject != null)
					return GettextCatalog.GetString ("from Project \"{0} in {1}\"", type.SourceProject.Name ?? "", type.Namespace ?? "");
				return GettextCatalog.GetString ("from \"{0} in {1}\"", (string)type.CompilationUnit.FileName ?? "", type.Namespace ?? "");
			}
		}
		
		
		public TypeSearchResult (string match, string matchedString, int rank, IType type, bool useFullName)
			: base (match, matchedString, rank, type, useFullName)
		{
		}
	}
	
	class FileSearchResult: SearchResult
	{
		ProjectFile file;
		bool useFileName;
		
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

		public override string Description {
			get {
				if (useFileName)
					return file.Project != null
						? GettextCatalog.GetString ("from \"{0}\" in Project \"{1}\"", GetRelProjectPath (file), file.Project.Name)
						: GettextCatalog.GetString ("from \"{0}\"", GetRelProjectPath (file));
				return file.Project != null ? GettextCatalog.GetString ("from Project \"{0}\"", file.Project.Name) : "";
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
		protected IMember member;
		
		protected virtual OutputFlags Flags {
			get {
				return OutputFlags.IncludeParameters | OutputFlags.IncludeGenerics
					| (useFullName  ? OutputFlags.UseFullName : OutputFlags.None);
			}
		}
		
		public override string GetMarkupText (Widget widget)
		{
			if (useFullName)
				return HighlightMatch (widget, Ambience.GetString (member, Flags), match);
			OutputSettings settings = new OutputSettings (Flags | OutputFlags.IncludeMarkup);
			settings.EmitNameCallback = delegate (INode domVisitable, ref string outString) {
				if (domVisitable == member)
					outString = HighlightMatch (widget, outString, match);
			};
			return Ambience.GetString (member, settings);
		}
		
			/*	
		public override string MarkupText {
			get {
				return useFullName ? HighlightMatch (Ambience.GetString (member, Flags | OutputFlags.IncludeMarkup), match) : base.MarkupText;
			}
		}*/

		
		public override string PlainText {
			get {
				return Ambience.GetString (member, Flags);
			}
		}
		
		public override string File {
			get { return member.DeclaringType.CompilationUnit.FileName; }
		}

		public override Gdk.Pixbuf Icon {
			get {
				return ImageService.GetPixbuf (member.StockIcon, IconSize.Menu);
			}
		}
		
		public override int Row {
			get { return member.Location.Line; }
		}
		
		public override int Column {
			get { return member.Location.Column; }
		}
		
		public override string Description {
			get {
				return GettextCatalog.GetString ("from Type \"{0}\"", member.DeclaringType.Name);
			}
		}
		
		public MemberSearchResult (string match, string matchedString, int rank, IMember member, bool useFullName)
								: base (match, matchedString, rank)
		{
			this.member = member;
			this.useFullName = useFullName;
		}
		
		protected Ambience Ambience { 
			get {
				IType type = member is IType ? (IType)member : member.DeclaringType;
				if (type.SourceProject is DotNetProject)
					return ((DotNetProject)type.SourceProject).Ambience;
				return AmbienceService.DefaultAmbience;
			}
		}
	}
}
