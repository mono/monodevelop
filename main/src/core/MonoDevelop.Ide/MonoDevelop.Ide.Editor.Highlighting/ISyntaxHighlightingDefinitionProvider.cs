﻿//
// ISyntaxHighlightingDefinitionProvider.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;
using System.Linq;
using MonoDevelop.Ide.Editor.Highlighting.RegexEngine;
using System.Collections.Immutable;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	interface ISyntaxHighlightingDefinitionProvider
	{
		IReadOnlyList<string> FileTypes { get; }

		SyntaxHighlightingDefinition GetSyntaxHighlightingDefinition ();
	}

	enum SyntaxHighlightingDefinitionFormat
	{
		TextMate,
		TextMateJson,
		Sublime3
	}

	abstract class AbstractSyntaxHighlightingDefinitionProvider : ISyntaxHighlightingDefinitionProvider
	{
		readonly Func<IStreamProvider> getStreamProvider;
		public IReadOnlyList<string> FileTypes { get; }

		public AbstractSyntaxHighlightingDefinitionProvider (IReadOnlyList<string> fileTypes, Func<IStreamProvider> getStreamProvider)
		{
			this.FileTypes = fileTypes;
			this.getStreamProvider = getStreamProvider;
		}

		SyntaxHighlightingDefinition syntaxHighlightingDefinition;

		public SyntaxHighlightingDefinition GetSyntaxHighlightingDefinition ()
		{
			if (syntaxHighlightingDefinition != null)
				return syntaxHighlightingDefinition;

			syntaxHighlightingDefinition = LoadSyntaxHighlightingDefinition (getStreamProvider ().Open ());
			syntaxHighlightingDefinition.PrepareMatches ();
			return syntaxHighlightingDefinition;
		}

		protected abstract SyntaxHighlightingDefinition LoadSyntaxHighlightingDefinition (Stream stream);

		public static ISyntaxHighlightingDefinitionProvider CreateProvider (SyntaxHighlightingDefinitionFormat format, IReadOnlyList<string> fileTypes, Func<IStreamProvider> getStreamProvider)
		{
			switch (format) {
			case SyntaxHighlightingDefinitionFormat.TextMate:
				return new TextMateSyntaxProvider (fileTypes, getStreamProvider);
			case SyntaxHighlightingDefinitionFormat.TextMateJson:
				return new TextMateJSonSyntaxProvider (fileTypes, getStreamProvider);
			case SyntaxHighlightingDefinitionFormat.Sublime3:
				return new Sublime3SyntaxProvider (fileTypes, getStreamProvider);
			default:
				throw new InvalidOperationException ("Unknown syntax highlighting definition format " + format);
			}
		}

		class TextMateSyntaxProvider : AbstractSyntaxHighlightingDefinitionProvider
		{
			public TextMateSyntaxProvider (IReadOnlyList<string> fileTypes, Func<IStreamProvider> getStreamProvider) : base (fileTypes, getStreamProvider)
			{
			}

			protected override SyntaxHighlightingDefinition LoadSyntaxHighlightingDefinition (Stream stream) => TextMateFormat.ReadHighlighting (stream);
		}

		class TextMateJSonSyntaxProvider : AbstractSyntaxHighlightingDefinitionProvider
		{
			public TextMateJSonSyntaxProvider (IReadOnlyList<string> fileTypes, Func<IStreamProvider> getStreamProvider) : base (fileTypes, getStreamProvider)
			{
			}

			protected override SyntaxHighlightingDefinition LoadSyntaxHighlightingDefinition (Stream stream) => TextMateFormat.ReadHighlightingFromJson (stream);
		}

		class Sublime3SyntaxProvider : AbstractSyntaxHighlightingDefinitionProvider
		{
			public Sublime3SyntaxProvider (IReadOnlyList<string> fileTypes, Func<IStreamProvider> getStreamProvider) : base (fileTypes, getStreamProvider)
			{
			}

			protected override SyntaxHighlightingDefinition LoadSyntaxHighlightingDefinition (Stream stream)
			{
				using (var sr = new StreamReader (stream))
					return Sublime3Format.ReadHighlighting (sr);
			}
		}
	}
}