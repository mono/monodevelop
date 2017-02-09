// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// A base implementation for legacy LightBulb providers not supporting ImageMonikers.
    /// </summary>
    [CLSCompliant(false)]
    public abstract class LegacySuggestedActionBase : ISuggestedAction
    {
        private static ConditionalWeakTable<BitmapSource, IImageHandle> _imageTable;

        private static ConditionalWeakTable<BitmapSource, IImageHandle> ImageTable
        {
            get
            {
                return _imageTable ?? (_imageTable = new ConditionalWeakTable<BitmapSource, IImageHandle>());
            }
        }

        /// <summary>
        /// Gets whether this action has nested suggested action sets. See <see cref="ISuggestedAction.HasActionSets"/>.
        /// </summary>
        public virtual bool HasActionSets { get { return false; } }

        /// <summary>
        /// Gets a list of nested sets of suggested actions.
        /// </summary>
        /// <returns>A task whose result is either a list of <see cref="SuggestedActionSet"/>s representing nested suggested actions, 
        /// or null if this <see cref="ISuggestedAction"/> instance has no nested suggested actions.</returns>
        public virtual Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        /// <summary>
        /// Gets the localized text representing the suggested action.
        /// </summary>
        /// <remarks>This property should never be null.</remarks>
        public abstract string DisplayText { get; }

        /// <summary>
        /// Gets an optional icon representing the suggested action or null if this suggested
        /// action doesn't have an icon.
        /// </summary>
        public ImageMoniker IconMoniker
        {
            get
            {
                Debug.Assert(this.IconSource == null || this.IconSource is BitmapImage);
                // Until Dev14 #1126060 is fixed, we only support BitmapImage
                BitmapImage iconSource = this.IconSource as BitmapImage;
                if (iconSource != null)
                {
                    IImageHandle handle = ImageTable.GetValue(iconSource, s => CrispImage.DefaultImageLibrary.AddCustomImage(s, canTheme: true));
                    return handle.Moniker;
                }

                return default(ImageMoniker);
            }
        }

        /// <summary>
        /// Gets an optional icon representing the suggested action as <see cref="ImageSource"/> or null if this suggested
        /// action doesn't have an icon.
        /// </summary>
        public virtual ImageSource IconSource { get { return null; } }

        /// <summary>
        /// Gets the text to be used as the automation name for the icon when it's displayed.
        /// </summary>
        /// <remarks>For accesibility reasons this property should not be null if the icon is specified.</remarks>
        public virtual string IconAutomationText { get { return null; } }

        /// <summary>
        /// Gets the text describing an input gesture that will apply the suggested action.
        /// </summary>
        public virtual string InputGestureText { get { return null; } }

        /// <summary>
        /// Gets whether this suggested action can provide a preview via <see cref="GetPreviewAsync(CancellationToken)"/> method call.
        /// </summary>
        public virtual bool HasPreview { get { return false; } }

        /// <summary>
        /// Gets an object visually representing a preview of the suggested action.  
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that allows to cancel preview creation.</param>
        public virtual Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Invokes the suggested action.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that allows cancel of action invocation.</param>
        public abstract void Invoke(CancellationToken cancellationToken);

        /// <summary>
        /// Disposes this action.
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Provides telemetry ID for this suggested action.
        /// </summary>
        /// <param name="telemetryId"></param>
        /// <returns></returns>
        public virtual bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}
