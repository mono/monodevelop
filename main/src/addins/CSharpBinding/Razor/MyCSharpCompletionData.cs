using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class MyCSharpCompletionData : MyRoslynCompletionData
	{
		public MyCSharpCompletionData (Microsoft.CodeAnalysis.Document document, ITextSnapshot triggerSnapshot, CompletionService completionService, CompletionItem completionItem) :
			base (document, triggerSnapshot, completionService, completionItem)
		{
		}

		protected override string MimeType => "text/csharp";

		protected override void Format (TextEditor editor, Gui.Document document, SnapshotPoint start, SnapshotPoint end)
		{
			return;
			//MonoDevelop.CSharp.Formatting.OnTheFlyFormatter.Format (editor, document, start, end);
		}

		public override IconId Icon {
			get {
				IconId result = base.Icon;
				if (!result.IsNull) {
					return result;
				}

				var modifier = GetItemModifier ();
				var type = GetItemType ();
				return "md-" + modifier + type;
			}
		}

		static Dictionary<string, string> roslynCompletionTypeTable = new Dictionary<string, string> {
			{ "Field", "field" },
			{ "Alias", "field" },
			{ "ArrayType", "field" },
			{ "Assembly", "field" },
			{ "DynamicType", "field" },
			{ "ErrorType", "field" },
			{ "Label", "field" },
			{ "NetModule", "field" },
			{ "PointerType", "field" },
			{ "RangeVariable", "field" },
			{ "TypeParameter", "field" },
			{ "Preprocessing", "field" },

			{ "Constant", "literal" },

			{ "Parameter", "variable" },
			{ "Local", "variable" },

			{ "Method", "method" },

			{ "Namespace", "name-space" },

			{ "Property", "property" },

			{ "Event", "event" },

			{ "Class", "class" },

			{ "Delegate", "delegate" },

			{ "Enum", "enum" },

			{ "Interface", "interface" },

			{ "Struct", "struct" },
			{ "Structure", "struct" },

			{ "Keyword", "keyword" },

			{ "Snippet", "template"},

			{ "EnumMember", "literal" },

			{ "NewMethod", "newmethod" }
		};

		string GetItemType ()
		{
			foreach (var tag in CompletionItem.Tags) {
				if (roslynCompletionTypeTable.TryGetValue (tag, out string result))
					return result;
			}
			LoggingService.LogWarning ("RoslynCompletionData: Can't find item type '" + string.Join (",", CompletionItem.Tags) + "'");
			return "literal";
		}

		static Dictionary<string, string> modifierTypeTable = new Dictionary<string, string> {
			{ "Private", "private-" },
			{ "ProtectedAndInternal", "ProtectedOrInternal-" },
			{ "Protected", "protected-" },
			{ "Internal", "internal-" },
			{ "ProtectedOrInternal", "ProtectedOrInternal-" }
		};

		string GetItemModifier ()
		{
			foreach (var tag in CompletionItem.Tags) {
				if (modifierTypeTable.TryGetValue (tag, out string result))
					return result;
			}
			return "";
		}
	}
}