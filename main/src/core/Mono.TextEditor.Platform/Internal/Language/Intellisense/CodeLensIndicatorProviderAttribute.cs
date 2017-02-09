//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CodeLensIndicatorProviderAttribute : ExportAttribute
    {
        private readonly string targetViewModelTypeName;

        public CodeLensIndicatorProviderAttribute(Type targetViewModelType)
            : base(typeof(ICodeLensIndicatorProvider))
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
