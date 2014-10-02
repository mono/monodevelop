//
// CodeIssueDescriptor.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis.Diagnostics;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.CodeIssues
{
	class CodeDiagnosticDescriptor
	{
		readonly Type codeIssueType;
		readonly NRefactoryCodeDiagnosticAnalyzerAttribute nrefactoryAttr;

		DiagnosticAnalyzer instance;

		/// <summary>
		/// Gets the identifier string.
		/// </summary>
		internal string IdString {
			get {
				return codeIssueType.FullName;
			}
		}

		public Type CodeIssueType {
			get {
				return codeIssueType;
			}
		}

		/// <summary>
		/// Gets the display name for this issue.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the description of the issue provider (used in the option panel).
		/// </summary>

		public string AnalysisDisableKeyword { get { return nrefactoryAttr != null ? nrefactoryAttr.AnalysisDisableKeyword : null; } }
		public string SuppressMessageCategory { get { return nrefactoryAttr != null ? nrefactoryAttr.SuppressMessageCategory : null; } }
		public string SuppressMessageCheckId { get { return nrefactoryAttr != null ? nrefactoryAttr.SuppressMessageCheckId : null; } }
		public int PragmaWarning { get { return nrefactoryAttr != null ? nrefactoryAttr.PragmaWarning : 0; }}

		/// <summary>
		/// Gets the languages for this issue.
		/// </summary>
		public string[] Languages { get; private set; }

		public DiagnosticSeverity? DiagnosticSeverity {
			get {
				DiagnosticSeverity? result = null;

				foreach (var diagnostic in GetProvider ().SupportedDiagnostics) {
					if (!result.HasValue)
						result = GetSeverity(diagnostic);
					if (result != GetSeverity(diagnostic))
						return null;
				}
				return result;
			}

			set {
				if (!value.HasValue)
					return;
				foreach (var diagnostic in GetProvider ().SupportedDiagnostics) {
					SetSeverity (diagnostic, value.Value);
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this code action is enabled by the user.
		/// </summary>
		/// <value><c>true</c> if this code action is enabled; otherwise, <c>false</c>.</value>
		public bool IsEnabled {
			get {
				foreach (var diagnostic in GetProvider ().SupportedDiagnostics) {
					if (!GetIsEnabled (diagnostic))
						return false;
				}
				return true;
			}
			set {
				foreach (var diagnostic in GetProvider ().SupportedDiagnostics) {
					SetIsEnabled (diagnostic, value);
				}
			}
		}

		internal DiagnosticSeverity GetSeverity (DiagnosticDescriptor diagnostic)
		{
			return PropertyService.Get ("CodeIssues." + Languages + "." + IdString + "." + diagnostic.Id + ".severity", diagnostic.DefaultSeverity);
		}

		internal void SetSeverity (DiagnosticDescriptor diagnostic, DiagnosticSeverity severity)
		{
			PropertyService.Set ("CodeIssues." + Languages + "." + IdString + "." + diagnostic.Id + ".severity", severity);
		}

		internal bool GetIsEnabled (DiagnosticDescriptor diagnostic)
		{
			return PropertyService.Get ("CodeIssues." + Languages + "." + IdString + "." + diagnostic.Id + ".enabled", true);
		}

		internal void SetIsEnabled (DiagnosticDescriptor diagnostic, bool value)
		{
			PropertyService.Set ("CodeIssues." + Languages + "." + IdString + "." + diagnostic.Id + ".enabled", value);
		}

		internal CodeDiagnosticDescriptor (string name, string[] languages, Type codeIssueType, NRefactoryCodeDiagnosticAnalyzerAttribute nrefactoryAttr)
		{
			if (string.IsNullOrEmpty (name))
				throw new ArgumentNullException ("name");
			if (languages == null)
				throw new ArgumentNullException ("languages");
			if (codeIssueType == null)
				throw new ArgumentNullException ("codeIssueType");
			Name = name;
			Languages = languages;
			this.codeIssueType = codeIssueType;
			this.nrefactoryAttr = nrefactoryAttr;
		}

		/// <summary>
		/// Gets the roslyn code action provider.
		/// </summary>
		public DiagnosticAnalyzer GetProvider ()
		{
			if (instance == null)
				instance = (DiagnosticAnalyzer)Activator.CreateInstance(codeIssueType);

			return instance;
		}

		public override string ToString ()
		{
			return string.Format ("[CodeIssueDescriptor: IdString={0}, Name={1}, Language={2}]", IdString, Name, Languages);
		}

		public bool CanDisableOnce { get { return !string.IsNullOrEmpty (AnalysisDisableKeyword); } }

		public bool CanDisableAndRestore { get { return !string.IsNullOrEmpty (AnalysisDisableKeyword); } }

		public bool CanDisableWithPragma { get { return PragmaWarning > 0; } }

		public bool CanSuppressWithAttribute { get { return !string.IsNullOrEmpty (SuppressMessageCheckId); } }

		const string analysisDisableTag = "Analysis ";

		public void DisableOnce (TextEditor editor, DocumentContext context, TextSpan span)
		{
			var start =editor.OffsetToLocation (span.Start);
			editor.InsertText (
				editor.LocationToOffset (start.Line, 1), 
				editor.GetVirtualIndentationString (start.Line) + "// " + analysisDisableTag + "disable once " + AnalysisDisableKeyword + editor.EolMarker
			); 
		}

		public void DisableAndRestore (TextEditor editor, DocumentContext context, TextSpan span)
		{
			using (editor.OpenUndoGroup ()) {
				var start = editor.OffsetToLocation (span.Start);
				var end = editor.OffsetToLocation (span.End);
				editor.InsertText (
					editor.LocationToOffset (end.Line + 1, 1),
					editor.GetVirtualIndentationString (end.Line) + "// " + analysisDisableTag + "restore " + AnalysisDisableKeyword + editor.EolMarker
				); 
				editor.InsertText (
					editor.LocationToOffset (start.Line, 1),
					editor.GetVirtualIndentationString (start.Line) + "// " + analysisDisableTag + "disable " + AnalysisDisableKeyword + editor.EolMarker
				); 
			}
		}

		public void DisableWithPragma (TextEditor editor, DocumentContext context, TextSpan span)
		{
			using (editor.OpenUndoGroup ()) {
				var start = editor.OffsetToLocation (span.Start);
				var end = editor.OffsetToLocation (span.End);
				editor.InsertText (
					editor.LocationToOffset (end.Line + 1, 1),
					editor.GetVirtualIndentationString (end.Line) + "#pragma warning restore " + PragmaWarning + editor.EolMarker
				); 
				editor.InsertText (
					editor.LocationToOffset (start.Line, 1),
					editor.GetVirtualIndentationString (start.Line) + "#pragma warning disable " + PragmaWarning + editor.EolMarker
				); 
			}
		}

		public void SuppressWithAttribute (TextEditor editor, DocumentContext context, TextSpan span)
		{
			var end = editor.OffsetToLocation (span.End);
			var member = context.ParsedDocument.GetMember (new ICSharpCode.NRefactory.TextLocation (end.Line, end.Column));
			editor.InsertText (
				editor.LocationToOffset (member.Region.BeginLine, 1),
				editor.GetVirtualIndentationString (member.Region.BeginLine) + string.Format ("[SuppressMessage(\"{0}\", \"{1}\")]" + editor.EolMarker, SuppressMessageCategory, SuppressMessageCheckId)
			); 
		}
	}
}

