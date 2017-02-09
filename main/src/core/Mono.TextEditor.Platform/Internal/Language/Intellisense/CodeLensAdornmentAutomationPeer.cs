using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation.Peers;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    internal class CodeLensAdornmentAutomationPeer : FrameworkElementAutomationPeer
    {
        public CodeLensAdornmentAutomationPeer(CodeLensAdornment adornment)
            : base(adornment)
        {
        }

        private CodeLensAdornment Adornment
        {
            get
            {
                return (CodeLensAdornment)this.Owner;
            }
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        protected override string GetClassNameCore()
        {
            return "CodeInformationIndicator";
        }

        protected override string GetNameCore()
        {
            ICodeLensAdornmentViewModel adornmentViewModel = this.Adornment.DataContext as ICodeLensAdornmentViewModel;
            if (adornmentViewModel == null)
            {
                return base.GetNameCore();
            }

            string fullSyntaxNode = adornmentViewModel.Indicators.Descriptor.ToString();
            int indexOfNewline = fullSyntaxNode.IndexOfAny(new[] { '\r', '\n' });
            if (indexOfNewline >= 0)
            {
                fullSyntaxNode = fullSyntaxNode.Substring(0, indexOfNewline);
            }

            return fullSyntaxNode.Trim();
        }
    }
}
