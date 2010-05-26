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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Gdk;
using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.Ide.NavigateToDialog
{
	abstract class SearchResult
	{
		protected string match;
		
		public virtual string MarkupText {
			get {
				return HighlightMatch (PlainText, match);
			}
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
		
		protected static string HighlightMatch (string text, string toMatch)
		{
			var lane = !string.IsNullOrEmpty (toMatch) ? NavigateToDialog.MatchString (text, toMatch) : null;
			if (lane != null) {
				StringBuilder result = new StringBuilder ();
				int lastPos = 0;
				for (int n=0; n <= lane.Index; n++) {
					int pos = lane.Positions [n];
					int len = lane.Lengths [n];
					if (pos - lastPos > 0)
						result.Append (GLib.Markup.EscapeText (text.Substring (lastPos, pos - lastPos)));
					result.Append ("<span foreground=\"blue\">");
					result.Append (GLib.Markup.EscapeText (text.Substring (pos, len)));
					result.Append ("</span>");
					lastPos = pos + len;
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
			get { return ((IType)member).CompilationUnit.FileName; }
		}
		
		public override string Description {
			get {
				IType type = (IType)member;
				if (useFullName) 
					return type.SourceProject != null ? String.Format (GettextCatalog.GetString ("from Project \"{0}\""), type.SourceProject.Name) : String.Format (GettextCatalog.GetString ("from \"{0}\""), type.CompilationUnit.FileName);
				if (type.SourceProject != null)
					return String.Format (GettextCatalog.GetString ("from Project \"{0} in {1}\""), type.SourceProject.Name, type.Namespace);
				return String.Format (GettextCatalog.GetString ("from \"{0} in {1}\""), type.CompilationUnit.FileName, type.Namespace);
			}
		}
		
		
		public TypeSearchResult (string match, string matchedString, int rank, IType type, bool useFullName) : base (match, matchedString, rank, type, useFullName)
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
					return file.Project != null ? String.Format (GettextCatalog.GetString ("from \"{0}\" in Project \"{1}\""), GetRelProjectPath (file), file.Project.Name) : String.Format (GettextCatalog.GetString ("from \"{0}\""), GetRelProjectPath (file));
				return file.Project != null ? String.Format (GettextCatalog.GetString ("from Project \"{0}\""), file.Project.Name) : "";
			}
		}
		
		public FileSearchResult (string match, string matchedString, int rank, ProjectFile file, bool useFileName) : base (match, matchedString, rank)
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
				return OutputFlags.IncludeParameters | OutputFlags.IncludeGenerics | (useFullName  ? OutputFlags.UseFullName : OutputFlags.None);
			}
		}
		
		public override string MarkupText {
			get {
				if (useFullName)
					return HighlightMatch (Ambience.GetString (member, Flags | OutputFlags.IncludeMarkup), match);
				OutputSettings settings = new OutputSettings (Flags | OutputFlags.IncludeMarkup);
				settings.EmitNameCallback = delegate (INode domVisitable, ref string outString) {
					if (domVisitable == member)
						outString = HighlightMatch (outString, match);
				};
				return Ambience.GetString (member, settings);
			}
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
				return String.Format (GettextCatalog.GetString ("from Type \"{0}\""), member.DeclaringType.Name);
			}
		}
		
		public MemberSearchResult (string match, string matchedString, int rank, IMember member, bool useFullName) : base (match, matchedString, rank)
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
