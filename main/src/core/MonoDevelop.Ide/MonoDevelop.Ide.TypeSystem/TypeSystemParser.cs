// 
// ITypeSystemParser.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using System.IO;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Core.Text;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading;
using MonoDevelop.Ide.Editor.Projection;
using MonoDevelop.Ide.Editor;
using System.Collections.Generic;

namespace MonoDevelop.Ide.TypeSystem
{
	public sealed class ParseOptions
	{
		string buildAction;

		public string BuildAction {
			get {
				return buildAction ?? "Compile";
			}
			set {
				buildAction = value;
			}
		}

		public string FileName { get; set; } 

		public ITextSource Content { get; set; }

		public MonoDevelop.Projects.Project Project {
			get {
				return (Owner as MonoDevelop.Projects.Project);
			}
			set {
				this.Owner = value;
			}
		}

		public MonoDevelop.Projects.WorkspaceObject Owner { get; set; }

		public Document RoslynDocument { get; set; }
		public ParsedDocument OldParsedDocument { get; internal set; }
		public bool IsAdhocProject { get; internal set; }
	}

	[Flags]
	public enum DisabledProjectionFeatures {
		None                     = 0,
		Completion               = 1 << 0,
		SemanticHighlighting     = 1 << 1,
		Tooltips                 = 1 << 2,

		All = Completion | SemanticHighlighting | Tooltips
	}

	public class ParsedDocumentProjection 
	{
		public ParsedDocument ParsedDocument { get; private set; }

		public IReadOnlyList<Projection> Projections { get; private set;}

		public DisabledProjectionFeatures DisabledProjectionFeatures { get; private set;}

		public ParsedDocumentProjection (ParsedDocument parsedDocument, IReadOnlyList<Projection> projections, DisabledProjectionFeatures disabledProjectionFeatures = DisabledProjectionFeatures.None)
		{
			this.ParsedDocument = parsedDocument;
			this.Projections = projections;
			this.DisabledProjectionFeatures = disabledProjectionFeatures;
		}
	}

	/// <summary>
	/// A type system parser provides a ParsedDocument (which just adds some more information to a IUnresolvedFile) for
	/// a given file. This is required for adding information to the type system service to make the file contents available
	/// for type lookup (code completion, resolving etc.).
	/// </summary>
	public abstract class TypeSystemParser 
	{
		/// <summary>
		/// Parse the specified file. The file content is provided as text reader.
		/// </summary>
		/// <param name='options'>
		/// The parse options.
		/// </param>
		/// <param name='cancellationToken'>
		/// The cancellation token to cancel the parsing task.
		/// </param>
		public abstract Task<ParsedDocument> Parse (ParseOptions options, CancellationToken cancellationToken = default(CancellationToken));

		/// <summary>
		/// If true projections are possible. A projection transforms a source to a target language and maps certain parts of the file to parts in the projected file.
		/// That's used for embedded languages for example.
		/// </summary>
		/// <returns><c>true</c> if this instance can generate projection the specified mimeType buildAction supportedLanguages;
		/// otherwise, <c>false</c>.</returns>
		/// <param name="mimeType">MIME type.</param>
		/// <param name="buildAction">Build action.</param>
		/// <param name="supportedLanguages">Supported languages.</param>
		public virtual bool CanGenerateProjection (string mimeType, string buildAction, string[] supportedLanguages)
		{
			return false;
		}

		public virtual bool CanGenerateAnalysisDocument (string mimeType, string buildAction, string [] supportedLanguages)
		{
			return false;
		}

		/// <summary>
		/// Generates the plain projection. This is used for type system services.
		/// </summary>
		/// <returns>The projection.</returns>
		/// <param name="options">Options.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		public virtual Task<IReadOnlyList<Projection>> GenerateProjections (ParseOptions options, CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotSupportedException ();
		}

		/// <summary>
		/// Generates the parsed document projection. That contains the parsed document and the projection. This is used inside the IDE for the editor.
		/// That's usually more efficient than calling Parse/GenerateProjection separately.
		/// </summary>
		/// <returns>The parsed document projection.</returns>
		/// <param name="options">Options.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		public virtual Task<ParsedDocumentProjection> GenerateParsedDocumentProjection (ParseOptions options, CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotSupportedException ();
		}

		/// <summary>
		/// Gets an up to date partial projection used by code completion.
		/// </summary>
		public virtual Task<IReadOnlyList<Projection>> GetPartialProjectionsAsync (DocumentContext ctx, TextEditor editor, ParsedDocument currentParsedDocument, CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotSupportedException ();
		}
	}
}

