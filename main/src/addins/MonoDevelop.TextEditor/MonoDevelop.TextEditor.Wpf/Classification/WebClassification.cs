//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Ide.Text
{
	class WebClassification
	{
		[Export (typeof (EditorFormatDefinition))]
		[ClassificationType (ClassificationTypeNames = "HTML Element Name")]
		[Name ("HTML Element Name")]
		[UserVisible (true)]
		internal class ExportedClassificationFormatHTMLElementName : ClassificationFormatDefinition
		{
			internal ExportedClassificationFormatHTMLElementName ()
			{
				ForegroundColor = Colors.Maroon;
			}
		}

		[Export (typeof (EditorFormatDefinition))]
		[ClassificationType (ClassificationTypeNames = "HTML Attribute Name")]
		[Name ("HTML Attribute Name")]
		[UserVisible (true)]
		internal class ExportedClassificationFormatHTMLAttributeName : ClassificationFormatDefinition
		{
			internal ExportedClassificationFormatHTMLAttributeName ()
			{
				ForegroundColor = Colors.Red;
			}
		}

		[Export (typeof (EditorFormatDefinition))]
		[ClassificationType (ClassificationTypeNames = "HTML Attribute Value")]
		[Name ("HTML Attribute Value")]
		[UserVisible (true)]
		internal class ExportedClassificationFormatHTMLAttributeValue : ClassificationFormatDefinition
		{
			internal ExportedClassificationFormatHTMLAttributeValue ()
			{
				ForegroundColor = Colors.Blue;
			}
		}

		[Export (typeof (EditorFormatDefinition))]
		[ClassificationType (ClassificationTypeNames = "HTML Operator")]
		[Name ("HTML Operator")]
		[UserVisible (true)]
		internal class ExportedClassificationFormatHTMLOperator : ClassificationFormatDefinition
		{
			internal ExportedClassificationFormatHTMLOperator ()
			{
				ForegroundColor = Colors.Blue;
			}
		}

		[Export (typeof (EditorFormatDefinition))]
		[ClassificationType (ClassificationTypeNames = "HTML Tag Delimiter")]
		[Name ("HTML Tag Delimiter")]
		[UserVisible (true)]
		internal class ExportedClassificationFormatHTMLTagDelimiter : ClassificationFormatDefinition
		{
			internal ExportedClassificationFormatHTMLTagDelimiter ()
			{
				ForegroundColor = Colors.Blue;
			}
		}

		private const string RazorCode = nameof (RazorCode);

		[Export (typeof (EditorFormatDefinition))]
		[UserVisible (true)]
		[ClassificationType (ClassificationTypeNames = RazorCode)]
		[Name (RazorCode)]
		[Order]
		internal sealed class RazorCodeClassificationFormat : ClassificationFormatDefinition
		{
			public RazorCodeClassificationFormat ()
			{
				BackgroundColor = Color.FromRgb (0xE5, 0xE5, 0xE5);
			}
		}

		[Export (typeof (ClassificationTypeDefinition))]
		[Name (RazorCode), Export]
		internal ClassificationTypeDefinition RazorCodeClassificationType { get; set; }

		[Export (typeof (EditorFormatDefinition))]
		[ClassificationType (ClassificationTypeNames = "HTML Server-Side Script")]
		[Name ("HTML Server-Side Script")]
		[UserVisible (true)]
		internal class ExportedClassificationFormatHTMLServerSideScript : ClassificationFormatDefinition
		{
			internal ExportedClassificationFormatHTMLServerSideScript ()
			{
				ForegroundColor = Colors.Black;
				BackgroundColor = Colors.Yellow;
			}
		}

		[Export (typeof (EditorFormatDefinition))]
		[UserVisible (true)]
		[ClassificationType (ClassificationTypeNames = FormatName)]
		[Name (FormatName)]
		[Order (Before = LanguagePriority.FormalLanguage)]
		internal sealed class JsonPropertyNameClassificationFormat : ClassificationFormatDefinition
		{
			public const string FormatName = "JSON Property Name";

			public JsonPropertyNameClassificationFormat ()
			{
				ForegroundColor = Color.FromRgb (0x2E, 0x75, 0xB6);
				this.DisplayName = FormatName;
			}
		}

		[Export (typeof (EditorFormatDefinition))]
		[UserVisible (true)]
		[ClassificationType (ClassificationTypeNames = "CSS Comment")]
		[Name ("CSS Comment")]
		[Order (After = "HTML Priority Workaround", Before = LanguagePriority.FormalLanguage)]
		internal sealed class CommentClassificationFormat : ClassificationFormatDefinition
		{
			internal CommentClassificationFormat ()
			{
				ForegroundColor = Colors.DarkGreen;
				this.DisplayName = "CSS Comment";
			}
		}

		[Export (typeof (EditorFormatDefinition))]
		[UserVisible (true)]
		[ClassificationType (ClassificationTypeNames = "CSS Keyword")]
		[Name ("CSS Keyword")]
		[Order (After = "HTML Priority Workaround", Before = LanguagePriority.FormalLanguage)]
		internal sealed class KeywordClassificationFormat : ClassificationFormatDefinition
		{
			public KeywordClassificationFormat ()
			{
				ForegroundColor = Colors.Purple;
				this.DisplayName = "CSS Keyword";
			}
		}

		[Export (typeof (EditorFormatDefinition))]
		[UserVisible (true)]
		[ClassificationType (ClassificationTypeNames = "CSS Selector")]
		[Name ("CSS Selector")]
		[Order (After = "HTML Priority Workaround", Before = LanguagePriority.FormalLanguage)]
		internal sealed class SelectorClassificationFormat : ClassificationFormatDefinition
		{
			public SelectorClassificationFormat ()
			{
				ForegroundColor = Colors.Maroon;
				this.DisplayName = "CSS Selector";
			}
		}

		[Export (typeof (EditorFormatDefinition))]
		[UserVisible (true)]
		[ClassificationType (ClassificationTypeNames = "CSS Property Name")]
		[Name ("CSS Property Name")]
		[Order (After = "HTML Priority Workaround", Before = LanguagePriority.FormalLanguage)]
		internal sealed class CssPropertyNameClassificationFormat : ClassificationFormatDefinition
		{
			public CssPropertyNameClassificationFormat ()
			{
				ForegroundColor = Colors.Red;
				this.DisplayName = "CSS Property Name";
			}
		}

		[Export (typeof (EditorFormatDefinition))]
		[UserVisible (true)]
		[ClassificationType (ClassificationTypeNames = "CSS Property Value")]
		[Name ("CSS Property Value")]
		[Order (After = "HTML Priority Workaround", Before = LanguagePriority.FormalLanguage)]
		internal sealed class PropertyValueClassificationFormat : ClassificationFormatDefinition
		{
			public PropertyValueClassificationFormat ()
			{
				ForegroundColor = Colors.Blue;
				this.DisplayName = "CSS Property Value";
			}
		}

		[Export (typeof (EditorFormatDefinition))]
		[UserVisible (true)]
		[ClassificationType (ClassificationTypeNames = "CSS String Value")]
		[Name ("CSS String Value")]
		[Order (After = "HTML Priority Workaround", Before = LanguagePriority.FormalLanguage)]
		internal sealed class StringValueClassificationFormat : ClassificationFormatDefinition
		{
			internal StringValueClassificationFormat ()
			{
				ForegroundColor = Colors.Blue;
				this.DisplayName = "CSS String Value";
			}
		}
	}
}
