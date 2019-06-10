using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Expansion;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeTemplates;

namespace MonoDevelop.TextEditor.Cocoa
{
	[Export (typeof (IExpansionManager))]
	class ExpansionManager : IExpansionManager
	{
		static string UserSnippetsPath { get; } = UserProfile.Current.UserDataRoot.Combine ("Snippets");

		readonly Dictionary<IContentType, List<ExpansionTemplate>> expansionTemplates
			= new Dictionary<IContentType, List<ExpansionTemplate>> ();

		public IEnumerable<ExpansionTemplate> EnumerateExpansions (IContentType contentType, bool shortcutOnly, string[] snippetTypes, bool includeNullType, bool includeDuplicates)
		{
			List<ExpansionTemplate> templates = null;
			lock (expansionTemplates) {
				if (expansionTemplates.TryGetValue (contentType, out templates))
					return templates;
			}

			templates = new List<ExpansionTemplate> ();

			foreach (var codeTemplate in CodeTemplateService.GetCodeTemplates (
				IdeServices.DesktopService.GetMimeTypeForContentType (contentType))) {
				try {
					templates.Add (Convert (codeTemplate));
				} catch (Exception e) {
					LoggingService.LogError ("Exception converting Snippet to VS Expansion Template", e);
				}
			}

			if (Directory.Exists (UserSnippetsPath)) {
				try {
					foreach (var snippetFile in Directory.EnumerateFiles (UserSnippetsPath, "*.snippet")) {
						try {
							templates.Add (new ExpansionTemplate (snippetFile));
						} catch (Exception parseException) {
							LoggingService.LogError (
								$"Unable to parse VS Expansion template: {snippetFile}",
								parseException);
						}
					}
				} catch (Exception enumerateException) {
					LoggingService.LogError (
						$"Exception enumerating snippets directory: {UserSnippetsPath}",
						enumerateException);
				}
			}

			lock (expansionTemplates)
				expansionTemplates[contentType] = templates;

			return templates;
		}

		private ExpansionTemplate Convert (CodeTemplate codeTemplate)
		{
			var codeSnippet = new CodeSnippet ();
			codeSnippet.Code = codeTemplate.Code;
			if (!codeSnippet.Code.Contains ("$end$"))
				codeSnippet.Code += "$end$";
			codeSnippet.Description = codeTemplate.Description;
			codeSnippet.Fields = new List<ExpansionField> ();
			foreach (var variable in codeTemplate.Variables) {
				var field = new ExpansionField ();
				field.Editable = variable.IsEditable;
				field.Function = ConvertFunctionName (variable.Function);
				field.ToolTip = variable.ToolTip;
				field.Default = variable.Default ?? "";
				if ("GetConstructorModifier()" == variable.Function && string.IsNullOrEmpty (field.Default))
					field.Default = "public ";
				field.ID = variable.Name;
				codeSnippet.Fields.Add (field);
			}
			codeSnippet.Language = codeTemplate.MimeType;
			codeSnippet.Shortcut = codeTemplate.Shortcut;
			codeSnippet.Title = codeTemplate.Shortcut;
			return new ExpansionTemplate (codeSnippet);
		}

		private string ConvertFunctionName (string function)
		{
			if (function == null)
				return function;
			if (function == "GetCurrentClassName()")
				return "ClassName()";
			if (function.StartsWith ("GetSimpleTypeName", StringComparison.Ordinal))
				return function.Replace ("GetSimpleTypeName", "SimpleTypeName").Replace ("#", ".").Replace ("\"", "");
			return function;
		}

		public ExpansionTemplate GetTemplateByName (IExpansionClient expansionClient, IContentType contentType, string name, string filePath, ITextView textView, SnapshotSpan span, bool showDisambiguationUI)
		{
			foreach (var template in EnumerateExpansions (contentType, false, null, true, true))
				if (template.Snippet.Title == name)
					return template;
			return null;
		}

		public ExpansionTemplate GetTemplateByShortcut (IExpansionClient expansionClient, string shortcut, IContentType contentType, ITextView textView, SnapshotSpan span, bool showDisambiguationUI)
		{
			foreach (var template in EnumerateExpansions (contentType, false, null, true, true))
				if (template.Snippet.Shortcut == shortcut)
					return template;
			return null;
		}
	}
}
