//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
#if CODE_ANALYSIS_BASELINE
using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage("Microsoft.Design","CA1062:Validate arguments of public methods", MessageId="0", Scope="member", Target="Microsoft.VisualStudio.Text.EditorOptions.Implementation.EditorOptionsFactoryService.#GetOptions(Microsoft.VisualStudio.Utilities.IPropertyOwner)", Justification="No need to validate in this case")]
[module: SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.VisualStudio.Text.EditorOptions.Implementation.EditorOptionsFactoryService.#set_OptionImports(System.Collections.Generic.List`1<System.Lazy`1<Microsoft.VisualStudio.Text.Editor.EditorOptionDefinition>>)", Justification="ToDo: To be looked at")]

#endif
