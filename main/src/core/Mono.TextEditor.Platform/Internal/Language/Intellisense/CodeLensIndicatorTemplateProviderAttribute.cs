//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CodeLensIndicatorTemplateProviderAttribute : ExportAttribute
    {
        private readonly string targetViewModelTypeName;

        public CodeLensIndicatorTemplateProviderAttribute(Type targetViewModelType)
            : base(typeof(ICodeLensIndicatorTemplateProvider))
        {
            if (targetViewModelType == null)
                throw new ArgumentNullException("targetViewModelType");

            this.targetViewModelTypeName = targetViewModelType.AssemblyQualifiedName;
        }

        public string TargetViewModelTypeName
        {
            get
            {
                return this.targetViewModelTypeName;
            }
        }
    }
}
