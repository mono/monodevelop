//
// NestingRulesFile.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2019, Microsoft Inc. (http://microsoft.com)
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
using System.Linq;
using Newtonsoft.Json.Linq;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.FileNesting
{
	public abstract class NestingRulesProvider
	{
		const string RuleNameAddedExtension = "addedExtension";
		const string RuleNameAllExtensions = "allExtensions";
		const string RuleNameExtensionToExtension = "extensionToExtension";
		const string RuleNameFileToFile = "fileToFile";
		const string RuleNameFileSuffixToExtension = "fileSuffixToExtension";
		const string RuleNamePathSegment = "pathSegment";

		const string TokenNameAdd = "add";

		List<NestingRule> nestingRules;

		public NestingRulesProvider ()
		{
		}

		public NestingRulesProvider (string fromFile)
		{
			if (string.IsNullOrEmpty (fromFile))
				throw new ArgumentNullException (nameof (fromFile));
			SourceFile = fromFile;
		}

		public string SourceFile { get; protected set; }

		void AddRule (NestingRuleKind kind, string appliesTo, IEnumerable<string> patterns)
		{
			if (nestingRules == null) {
				nestingRules = new List<NestingRule> ();
			}

			nestingRules.Add (new NestingRule (kind, appliesTo, patterns));
			LoggingService.LogInfo ($"Added nesting rule of type {kind} for {appliesTo} on files [{String.Join (", ", patterns)}]");
		}

		static void ParseRulesProvider (NestingRulesProvider provider, NestingRuleKind kind, JObject jobj)
		{
			if (jobj == null) {
				// Fallback to create an empty rule for this NestingRuleKind
				provider.AddRule (kind, NestingRule.AllFilesWildcard, Array.Empty<string> ());
				return;
			}

			foreach (var prop in jobj.Properties ()) {
				if (prop.Value.Type == JTokenType.Array) {
					provider.AddRule (kind, prop.Name, (prop.Value as JArray).Select (x => x.Value<string> ()));
				}
			}
		}

		static bool LoadFromFile (NestingRulesProvider provider)
		{
			try {
				using (var reader = new StreamReader (provider.SourceFile)) {
					var json = JObject.Parse (reader.ReadToEnd ());
					if (json != null) {
						var parentNode = json ["dependentFileProviders"] [TokenNameAdd] as JObject;
						foreach (var jsonProp in parentNode.Properties ()) {
							JObject rpobj = null;
							try {
								rpobj = parentNode [jsonProp.Name] [TokenNameAdd].Value<JObject> ();
							} catch {
								LoggingService.LogWarning ($"No patterns specified for {jsonProp.Name} nesting rule");
							}

							if (jsonProp.Name == RuleNameAddedExtension) {
								ParseRulesProvider (provider, NestingRuleKind.AddedExtension, rpobj);
							} else if (jsonProp.Name == RuleNameAllExtensions) {
								ParseRulesProvider (provider, NestingRuleKind.AllExtensions, rpobj);
							} else if (jsonProp.Name == RuleNameExtensionToExtension) {
								ParseRulesProvider (provider, NestingRuleKind.ExtensionToExtension, rpobj);
							} else if (jsonProp.Name == RuleNameFileSuffixToExtension) {
								ParseRulesProvider (provider, NestingRuleKind.FileSuffixToExtension, rpobj);
							} else if (jsonProp.Name == RuleNameFileToFile) {
								ParseRulesProvider (provider, NestingRuleKind.FileToFile, rpobj);
							} else if (jsonProp.Name == RuleNamePathSegment) {
								ParseRulesProvider (provider, NestingRuleKind.PathSegment, rpobj);
							}
						}

						return true;
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ($"Unable to parse {provider.SourceFile}: {ex}");
			}

			return false;
		}

		public string GetParentFile (string inputFile)
		{
			if (nestingRules == null) {
				if (!File.Exists (SourceFile)) {
					return null;
				}

				if (!LoadFromFile (this)) {
					return null;
				}
			}

			foreach (var rule in nestingRules) {
				string parentFile = rule.GetParentFile (inputFile);
				if (!String.IsNullOrEmpty (parentFile)) {
					// Stop at the 1st rule found
					return parentFile;
				}
			}

			return null;
		}

	}
}
