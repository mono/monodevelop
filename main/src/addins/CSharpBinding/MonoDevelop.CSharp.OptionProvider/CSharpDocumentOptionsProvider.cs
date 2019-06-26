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
using Roslyn.Utilities;
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
using System.Runtime.CompilerServices;

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
			private static readonly ConditionalWeakTable<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, string>> s_convertedDictionaryCache =
				new ConditionalWeakTable<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, string>> ();

			public DocumentOptions (PolicyBag policyBag, ICodingConventionsSnapshot codingConventionsSnapshot)
			{
				this.policyBag = policyBag;
				this.codingConventionsSnapshot = codingConventionsSnapshot;
			}

			public bool TryGetDocumentOption (OptionKey option, out object value)
			{
				if (codingConventionsSnapshot != null) {
					var editorConfigPersistence = option.Option.StorageLocations.OfType<IEditorConfigStorageLocation> ().SingleOrDefault ();
					if (editorConfigPersistence != null) {
						// Temporarly map our old Dictionary<string, object> to a Dictionary<string, string>. This can go away once we either
						// eliminate the legacy editorconfig support, or we change IEditorConfigStorageLocation.TryGetOption to take
						// some interface that lets us pass both the Dictionary<string, string> we get from the new system, and the
						// Dictionary<string, object> from the old system.
						//
						// We cache this with a conditional weak table so we're able to maintain the assumptions in EditorConfigNamingStyleParser
						// that the instance doesn't regularly change and thus can be used for further caching
						var allRawConventions = s_convertedDictionaryCache.GetValue (
							codingConventionsSnapshot.AllRawConventions,
							d => ImmutableDictionary.CreateRange (d.Select (c => KeyValuePairUtil.Create (c.Key, c.Value.ToString ()))));

						try {
							if (editorConfigPersistence.TryGetOption (allRawConventions, option.Option.Type, out value))
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
				value = result;
				return true;
			}
		}
	}
}
