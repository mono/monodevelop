using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger
{
	class ClassificationTypes
	{
		public const string BreakpointForegroundTypeName = "BreakpointForeground";
		public const string BreakpointDisabledForegroundTypeName = "BreakpointDisabledForeground";
		public const string BreakpointInvalidForegroundTypeName = "BreakpointInvalidForeground";
		public const string CurrentStatementForegroundTypeName = "CurrentStatementForeground";
		public const string ReturnStatementForegroundTypeName = "ReturnStatementForeground";

		[Export]
		[Name (BreakpointForegroundTypeName)]
		[BaseDefinition (PredefinedClassificationTypeNames.FormalLanguage)]
		internal readonly ClassificationTypeDefinition BreakpointForegroundTypeDefinition = null;

		[Export (typeof (EditorFormatDefinition))]
		[ClassificationType (ClassificationTypeNames = BreakpointForegroundTypeName)]
		[Name (BreakpointForegroundTypeName + "Format")]
		[Order (After = LanguagePriority.FormalLanguage, Before = Priority.High)]
		[UserVisible (true)]
		private sealed class BreakpointForegroundClassificationFormat
			: ClassificationFormatDefinition
		{
			private BreakpointForegroundClassificationFormat ()
			{
				this.DisplayName = BreakpointForegroundTypeName;
				this.ForegroundColor = Colors.White;
			}
		}


		[Export]
		[Name (BreakpointDisabledForegroundTypeName)]
		[BaseDefinition (PredefinedClassificationTypeNames.FormalLanguage)]
		internal readonly ClassificationTypeDefinition BreakpointDisabledForegroundTypeDefinition = null;

		[Export (typeof (EditorFormatDefinition))]
		[ClassificationType (ClassificationTypeNames = BreakpointDisabledForegroundTypeName)]
		[Name (BreakpointDisabledForegroundTypeName + "Format")]
		[Order (After = LanguagePriority.FormalLanguage, Before = Priority.High)]
		[UserVisible (true)]
		private sealed class BreakpointDisabledForegroundClassificationFormat
			: ClassificationFormatDefinition
		{
			private BreakpointDisabledForegroundClassificationFormat ()
			{
				this.DisplayName = BreakpointDisabledForegroundTypeName;
				this.ForegroundColor = Colors.White;
			}
		}

		[Export]
		[Name (BreakpointInvalidForegroundTypeName)]
		[BaseDefinition (PredefinedClassificationTypeNames.FormalLanguage)]
		internal readonly ClassificationTypeDefinition BreakpointInvalidForegroundTypeDefinition = null;

		[Export (typeof (EditorFormatDefinition))]
		[ClassificationType (ClassificationTypeNames = BreakpointInvalidForegroundTypeName)]
		[Name (BreakpointInvalidForegroundTypeName + "Format")]
		[Order (After = LanguagePriority.FormalLanguage, Before = Priority.High)]
		[UserVisible (true)]
		private sealed class BreakpointInvalidForegroundClassificationFormat
			: ClassificationFormatDefinition
		{
			private BreakpointInvalidForegroundClassificationFormat ()
			{
				this.DisplayName = BreakpointInvalidForegroundTypeName;
				this.ForegroundColor = Colors.White;
			}
		}

		[Export]
		[Name (CurrentStatementForegroundTypeName)]
		[BaseDefinition (PredefinedClassificationTypeNames.FormalLanguage)]
		internal readonly ClassificationTypeDefinition CurrentStatementForegroundTypeDefinition = null;

		[Export (typeof (EditorFormatDefinition))]
		[ClassificationType (ClassificationTypeNames = CurrentStatementForegroundTypeName)]
		[Name (CurrentStatementForegroundTypeName + "Format")]
		[Order (After = LanguagePriority.FormalLanguage, Before = Priority.High)]
		[UserVisible (true)]
		private sealed class CurrentStatementForegroundClassificationFormat
			: ClassificationFormatDefinition
		{
			private CurrentStatementForegroundClassificationFormat ()
			{
				this.DisplayName = CurrentStatementForegroundTypeName;
				this.ForegroundColor = Colors.Black;
			}
		}

		[Export]
		[Name (ReturnStatementForegroundTypeName)]
		[BaseDefinition (PredefinedClassificationTypeNames.FormalLanguage)]
		internal readonly ClassificationTypeDefinition ReturnStatementForegroundTypeDefinition = null;

		[Export (typeof (EditorFormatDefinition))]
		[ClassificationType (ClassificationTypeNames = ReturnStatementForegroundTypeName)]
		[Name (ReturnStatementForegroundTypeName + "Format")]
		[Order (After = LanguagePriority.FormalLanguage, Before = Priority.High)]
		[UserVisible (true)]
		private sealed class ReturnStatementForegroundClassificationFormat
			: ClassificationFormatDefinition
		{
			private ReturnStatementForegroundClassificationFormat ()
			{
				this.DisplayName = ReturnStatementForegroundTypeName;
				this.ForegroundColor = Colors.Black;
			}
		}
	}
}
