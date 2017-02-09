// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
#if TARGET_VS
using Microsoft.VisualStudio.Imaging.Interop;
#endif
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// A possible action suggested to be performed. Examples of such suggested
    /// actions include quick fixes for syntax errors, suggestions aimed on improving
    /// code quality or refactorings.
    /// Suggested actions are provided by <see cref="ISuggestedActionsSource"/> instances
    /// and represented by a LightBulb presenter as menu items in a LightBulb dropdown menu.
    /// </summary>
    [CLSCompliant(false)]
    public interface ISuggestedAction : IDisposable, ITelemetryIdProvider<Guid>
    {
        /// <summary>
        /// Gets whether this action has nested suggested action sets.
        /// </summary>
        /// <remarks>This property is expected to be fast so if calculating the return value is not trivial it's recommended to
        /// just return <c>true</c> and delay evaluating real list of nested actions until 
        /// <see cref="GetActionSetsAsync(CancellationToken)"/> method is called. In other words, the scenario of <see cref="HasActionSets"/> 
        /// returning <c>true</c> and <see cref="GetActionSetsAsync(CancellationToken)"/> returning null or empty list is supported.
        /// On the other hand, if this property returns <c>false</c>, <see cref="GetActionSetsAsync(CancellationToken)"/> method 
        /// will not be called.</remarks>
        bool HasActionSets { get; }

        /// <summary>
        /// Gets a list of nested sets of suggested actions.
        /// </summary>
        /// <returns>A task whose result is either a list of <see cref="SuggestedActionSet"/>s representing nested suggested actions, 
        /// or null if this <see cref="ISuggestedAction"/> instance has no nested suggested actions.</returns>
        Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the localized text representing the suggested action.
        /// </summary>
        /// <remarks>This property should never be null.</remarks>
        string DisplayText { get; }

#if TARGET_VS
        /// <summary>
        /// Gets an optional icon representing the suggested action or null if this suggested
        /// action doesn't have an icon.
        /// </summary>
        ImageMoniker IconMoniker { get; }
#endif

        /// <summary>
        /// Gets the text to be used as the automation name for the icon when it's displayed.
        /// </summary>
        /// <remarks>For accessibility reasons this property should not be null if the icon is specified.</remarks>
        string IconAutomationText { get; }

        /// <summary>
        /// Gets the text describing an input gesture that will apply the suggested action.
        /// </summary>
        string InputGestureText { get; }

        /// <summary>
        /// Gets whether this suggested action can provide a preview via <see cref="GetPreviewAsync(CancellationToken)"/> method call.
        /// </summary>
        /// <remarks>This property is expected to be fast so if calculating the return value is not trivial it's recommended to
        /// just return <c>true</c> and delay evaluating whether a preview can be provided until
        /// <see cref="GetPreviewAsync(CancellationToken)"/> method is called. In other words, the scenario of <see cref="HasPreview"/> 
        /// returning <c>true</c> and <see cref="GetPreviewAsync(CancellationToken)"/> returning null is supported.
        /// On the other hand, if this property returns <c>false</c>, <see cref="GetPreviewAsync(CancellationToken)"/> method 
        /// will not be called.</remarks>
        bool HasPreview { get; }

        /// <summary>
        /// Gets an object visually representing a preview of the suggested action.  
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that allows to cancel preview creation.</param>
        /// <remarks>
        /// <para>The only currently supported type of preview object is <see cref="UIElement" />.</para>
        /// <para>By default preview panel gets highlighted when focused by setting background color to 
        /// <see cref="LightBulbPresenterStyle.PreviewFocusBackgroundBrush"/>. When providing a preview object make sure
        /// it doesn't set different background for the whole preview content, otherwise it's recommended that preview
        /// object indicates focused state using <see cref="LightBulbPresenterStyle.PreviewFocusBackgroundBrush"/> color.</para>
        /// </remarks>
        /// <returns>A task whose result is an object visually representing a preview of the suggested action, or null if no preview can be provided.</returns>
        Task<object> GetPreviewAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Invokes the suggested action.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that allows cancel of action invocation.</param>
        void Invoke(CancellationToken cancellationToken);
    }
}