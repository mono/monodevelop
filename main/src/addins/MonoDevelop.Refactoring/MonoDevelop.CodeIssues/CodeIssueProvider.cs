// 
// IInspector.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.Refactoring;
using Mono.TextEditor;
using System;

namespace MonoDevelop.CodeIssues
{
	public abstract class BaseCodeIssueProvider
	{
		public virtual BaseCodeIssueProvider Parent {
			get {
				return null;
			}
		}
		/// <summary>
		/// Gets or sets the type of the MIME the provider is attached to.
		/// </summary>
		public abstract string MimeType {
			get;
		}

		/// <summary>
		/// Gets or sets the title of the issue provider (used in the option panel).
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// Gets or sets the description of the issue provider (used in the option panel).
		/// </summary>
		public string Description { get; set; }

		
		/// <summary>
		/// Gets the identifier string used as property ID tag.
		/// </summary>
		public virtual string IdString {
			get {
				return "refactoring.codeissues." + MimeType + "." + GetType ().FullName;
			}
		}

		/// <summary>
		/// Gets or sets the default severity. Note that GetSeverity () should be called to get the valid value inside the IDE.
		/// </summary>
		public Severity DefaultSeverity { get; set; }

		
		/// <summary>
		/// Gets or sets a value indicating how this issue should be marked inside the text editor.
		/// Note: There is only one code issue provider generated therfore providers need to be state less.
		/// </summary>
		public virtual IssueMarker IssueMarker { get; set; }

		/// <summary>
		/// Gets the current (user defined) severity.
		/// </summary>
		protected Severity severity;
		public Severity GetSeverity ()
		{
			return severity;
		}

		/// <summary>
		/// Sets the user defined severity.
		/// </summary>
		public void SetSeverity (Severity severity)
		{
			if (this.severity == severity)
				return;
			this.severity = severity;
			PropertyService.Set (IdString, severity);
		}

		protected void UpdateSeverity ()
		{
			severity = PropertyService.Get<Severity> (IdString, DefaultSeverity);
		}

		/// <summary>
		/// Gets all the code issues inside a document.
		/// </summary>
		public abstract IEnumerable<CodeIssue> GetIssues (object refactoringContext, CancellationToken cancellationToken);

		public virtual bool CanDisableOnce { get { return false; } }

		public virtual bool CanDisableAndRestore { get { return false; } }

		public virtual bool CanDisableWithPragma { get { return false; } }

		public virtual bool CanSuppressWithAttribute { get { return false; } }

		public virtual void DisableOnce (MonoDevelop.Ide.Gui.Document document, DocumentRegion loc)
		{
			throw new NotSupportedException ();
		}

		public virtual void DisableAndRestore (MonoDevelop.Ide.Gui.Document document, DocumentRegion loc)
		{
			throw new NotSupportedException ();
		}

		public virtual void DisableWithPragma (MonoDevelop.Ide.Gui.Document document, DocumentRegion loc)
		{
			throw new NotSupportedException ();
		}

		public virtual void SuppressWithAttribute (MonoDevelop.Ide.Gui.Document document, DocumentRegion loc)
		{
			throw new NotSupportedException ();
		}
	}


	/// <summary>
	/// A code issue provider is a factory that creates code issues of a given document.
	/// </summary>
	public abstract class CodeIssueProvider : BaseCodeIssueProvider
	{
		string mimeType;
		public override string MimeType {
			get {
				return mimeType;
			}
		}

		public void SetMimeType (string mimeType)
		{
			this.mimeType = mimeType;
			UpdateSeverity ();
		}

		/// <summary>
		/// Gets or sets the category of the issue provider (used in the option panel).
		/// </summary>
		public string Category { get; set; }

		/// <summary>
		/// If true this issue has sub issues.
		/// </summary>
		public virtual bool HasSubIssues { get { return false; } }

		/// <summary>
		/// Gets the sub issues of this issue. If HasSubIssus == false an InvalidOperationException is thrown.
		/// </summary>
		public virtual IEnumerable<BaseCodeIssueProvider> SubIssues { get { throw new InvalidOperationException (); } }
	}
}

