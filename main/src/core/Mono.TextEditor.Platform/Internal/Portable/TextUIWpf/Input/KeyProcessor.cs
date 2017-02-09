// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System.Windows.Input;

    /// <summary>
    /// Processes the keyboard input of the editor.
    /// </summary>
    /// <remarks>
    /// Export this functionality by using the <see cref="IKeyProcessorProvider"/>.
    /// </remarks>
    public abstract class KeyProcessor
    {
        /// <summary>
        /// Determines whether this processor should be called for events that have been handled by earlier <see cref="KeyProcessor"/> objects.
        /// </summary>
        public virtual bool IsInterestedInHandledEvents { get { return false; } }

        /// <summary>
        /// Handles the PreviewKeyDown event.
        /// </summary>
        /// <param name="args">
        /// A <see cref="KeyEventArgs"/> describing the key event.
        /// </param>
        public virtual void PreviewKeyDown(KeyEventArgs args) { }

        /// <summary>
        /// Handles the KeyDown event.
        /// </summary>
        /// <param name="args">
        /// A <see cref="KeyEventArgs"/> describing the key event.
        /// </param>
        public virtual void KeyDown(KeyEventArgs args) { }

        /// <summary>
        /// Handles the PreviewKeyUp event.
        /// </summary>
        /// <param name="args">
        /// A <see cref="KeyEventArgs"/> describing the key event.
        /// </param>
        public virtual void PreviewKeyUp(KeyEventArgs args) { }

        /// <summary>
        /// Handles the KeyUp event.
        /// </summary>
        /// <param name="args">
        /// A <see cref="KeyEventArgs"/> describing the key event.
        /// </param>
        public virtual void KeyUp(KeyEventArgs args) { }

        /// <summary>
        /// Handles the PreviewTextInputStart event.
        /// </summary>
        /// <param name="args">
        /// A <see cref="KeyEventArgs"/> describing the key event.
        /// </param>
        public virtual void PreviewTextInputStart(TextCompositionEventArgs args) { }

        /// <summary>
        /// Handles the TextInputStart event.
        /// </summary>
        /// <param name="args">
        /// A <see cref="KeyEventArgs"/> describing the key event.
        /// </param>
        public virtual void TextInputStart(TextCompositionEventArgs args) { }

        /// <summary>
        /// Handles the PreviewTextInput event.
        /// </summary>
        /// <param name="args">
        /// A <see cref="KeyEventArgs"/> describing the key event.
        /// </param>
        public virtual void PreviewTextInput(TextCompositionEventArgs args) { }

        /// <summary>
        /// Handles the TextInput event.
        /// </summary>
        /// <param name="args">
        /// A <see cref="KeyEventArgs"/> describing the key event.
        /// </param>
        public virtual void TextInput(TextCompositionEventArgs args) { }

        /// <summary>
        /// Handles the PreviewTextInputUpdate event.
        /// </summary>
        /// <param name="args">
        /// A <see cref="KeyEventArgs"/> describing the key event.
        /// </param>
        public virtual void PreviewTextInputUpdate(TextCompositionEventArgs args) { }

        /// <summary>
        /// Handles the TextInputUpdate event.
        /// </summary>
        /// <param name="args">
        /// A <see cref="KeyEventArgs"/> describing the key event.
        /// </param>
        public virtual void TextInputUpdate(TextCompositionEventArgs args) { }
    }
}
