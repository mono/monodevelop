//
// CSharpDocumentOptionsProvider.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects.Policies;
using Microsoft.VisualStudio.CodingConventions;
using System.IO;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Ide.Editor;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MonoDevelop.CSharp.OptionProvider
{
	class CSharpDocumentOptionsProvider : IDocumentOptionsProvider
	{
		async Task<IDocumentOptions> IDocumentOptionsProvider.GetOptionsForDocumentAsync (Document document, CancellationToken cancellationToken)
		{
			var mdws = document.Project.Solution.Workspace as MonoDevelopWorkspace;
			var project = mdws?.GetMonoProject (document.Project.Id);

			var path = GetPath (document);
			ICodingConventionContext conventions = null;
			try {
				if (path != null)
					conventions = await EditorConfigService.GetEditorConfigContext (path, cancellationToken);
			} catch (Exception e) {
				LoggingService.LogError("Error while loading coding conventions.", e);
			}
			return new DocumentOptions (project?.Policies, conventions?.CurrentConventions);
		}

		static string GetPath(Document document)
		{
			if (document.FilePath != null)
				return document.FilePath;

			// The file might not actually have a path yet, if it's a file being proposed by a code action. We'll guess a file path to use
			if (document.Name != null && document.Project.FilePath != null) {
				return Path.Combine (Path.GetDirectoryName (document.Project.FilePath), document.Name);
			}

			// Really no idea where this is going, so bail
			return null;
		}

		internal class DocumentOptions : IDocumentOptions
		{
			readonly static IEnumerable<string> types = Ide.IdeServices.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			readonly PolicyBag policyBag;

			CSharpFormattingPolicy policy;
			CSharpFormattingPolicy Policy => policy ?? (policy = policyBag?.Get<CSharpFormattingPolicy> (types) ?? PolicyService.InvariantPolicies.Get<CSharpFormattingPolicy> (types));

			TextStylePolicy textpolicy;
			TextStylePolicy TextPolicy => textpolicy ?? (textpolicy = policyBag?.Get<TextStylePolicy> (types) ?? PolicyService.InvariantPolicies?.Get<TextStylePolicy> (types));

			readonly ICodingConventionsSnapshot codingConventionsSnapshot;

			public DocumentOptions (PolicyBag policyBag, ICodingConventionsSnapshot codingConventionsSnapshot)
			{
				this.policyBag = policyBag;
				this.codingConventionsSnapshot = codingConventionsSnapshot;
			}

			public bool TryGetDocumentOption (OptionKey option, OptionSet underlyingOptions, out object value)
			{
				if (codingConventionsSnapshot != null) {
					var editorConfigPersistence = option.Option.StorageLocations.OfType<IEditorConfigStorageLocation> ().SingleOrDefault ();
					if (editorConfigPersistence != null) {

						var tempRawConventions = codingConventionsSnapshot.AllRawConventions;
						// HACK: temporarly map our old Dictionary<string, object> to a Dictionary<string, string>. This will go away in a future commit.
						// see https://github.com/dotnet/roslyn/commit/6a5be42f026f8d0432cfe8ee7770ff8f6be01bd6#diff-626aa9dd2f6e07eafa8eac7ddb0eb291R34
						var allRawConventions = ImmutableDictionary.CreateRange (tempRawConventions.Select (c => Roslyn.Utilities.KeyValuePairUtil.Create (c.Key, c.Value.ToString ())));

						try {
							var underlyingOption = Policy.OptionSet.GetOption (option);
							if (editorConfigPersistence.TryGetOption (underlyingOption, allRawConventions, option.Option.Type, out value))
								return true;
						} catch (Exception ex) {
							LoggingService.LogError ("Error while getting editor config preferences.", ex);
						}
					}
				}

				if (option.Option == Microsoft.CodeAnalysis.Formatting.FormattingOptions.IndentationSize) {
					value = TextPolicy.IndentWidth;
					return true;
				}

				if (option.Option == Microsoft.CodeAnalysis.Formatting.FormattingOptions.NewLine) {
					value = TextPolicy.GetEolMarker ();
					return true;
				}

				if (option.Option == Microsoft.CodeAnalysis.Formatting.FormattingOptions.SmartIndent) {
					value = Microsoft.CodeAnalysis.Formatting.FormattingOptions.IndentStyle.Smart;
					return true;
				}

				if (option.Option == Microsoft.CodeAnalysis.Formatting.FormattingOptions.TabSize) {
					value = TextPolicy.TabWidth;
					return true;
				}

				if (option.Option == Microsoft.CodeAnalysis.Formatting.FormattingOptions.UseTabs) {
					value = !TextPolicy.TabsToSpaces;
					return true;
				}

				var result = Policy.OptionSet.GetOption (option);
				if (result == underlyingOptions.GetOption (option)) {
					value = null;
					return false;
				}
				value = result;
				return true;
			}
		}
	}
}
