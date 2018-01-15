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

namespace MonoDevelop.CSharp.OptionProvider
{
	class CSharpDocumentOptionsProvider : IDocumentOptionsProvider
	{
		readonly Workspace workspace;

		public CSharpDocumentOptionsProvider (Workspace workspace)
		{
			this.workspace = workspace;
		}

		async Task<IDocumentOptions> IDocumentOptionsProvider.GetOptionsForDocumentAsync (Document document, CancellationToken cancellationToken)
		{
			var mdws = (MonoDevelopWorkspace)workspace;
			var project = mdws?.GetMonoProject (document.Project.Id);
			CSharpFormattingPolicy policy;
			TextStylePolicy textpolicy;
			if (project == null) {
				textpolicy = PolicyService.InvariantPolicies.Get<TextStylePolicy> (CSharpFormatter.MimeType);
				policy = PolicyService.InvariantPolicies.Get<CSharpFormattingPolicy> (CSharpFormatter.MimeType);
			} else {
				var policyParent = project.Policies;
				policy = policyParent.Get<CSharpFormattingPolicy> (CSharpFormatter.MimeType);
				textpolicy = policyParent.Get<TextStylePolicy> (CSharpFormatter.MimeType);
			}
			var path = GetPath (document);
			ICodingConventionContext conventions = null;
			try {
				if (path != null)
					conventions = await EditorConfigService.GetEditorConfigContext (path, cancellationToken);
			} catch (Exception e) {
				LoggingService.LogError("Error while loading coding conventions.", e);
			}
			return new DocumentOptions (policy.CreateOptions (textpolicy), conventions?.CurrentConventions);
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

		class DocumentOptions : IDocumentOptions
		{
			readonly OptionSet optionSet;
			readonly ICodingConventionsSnapshot codingConventionsSnapshot;

			public DocumentOptions (OptionSet optionSet, ICodingConventionsSnapshot codingConventionsSnapshot)
			{
				this.optionSet = optionSet;
				this.codingConventionsSnapshot = codingConventionsSnapshot;
			}

			public bool TryGetDocumentOption (Document document, OptionKey option, OptionSet underlyingOptions, out object value)
			{
				if (codingConventionsSnapshot != null) {
					var editorConfigPersistence = option.Option.StorageLocations.OfType<IEditorConfigStorageLocation> ().SingleOrDefault ();
					if (editorConfigPersistence != null) {
						var allRawConventions = codingConventionsSnapshot.AllRawConventions;
						try {
							var underlyingOption = underlyingOptions.GetOption (option);
							if (editorConfigPersistence.TryGetOption (underlyingOption, allRawConventions, option.Option.Type, out value))
								return true;
						} catch (Exception ex) {
							LoggingService.LogError ("Error while getting editor config preferences.", ex);
						}
					}
				}
				var result = optionSet.GetOption (option);

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
