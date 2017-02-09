////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Windows;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines the provider of WPF UIElements for objects of a certain type, for a specified context.
    /// </summary>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    /// <typeparam name="TContext">The type of the context.</typeparam>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(IUIElementProvider&lt;T&gt;))]
    /// [Name("")]
    /// [Order()]
    /// [ContentType("")]
    /// </remarks>
    public interface IUIElementProvider<TItem, TContext>
    {
        /// <summary>
        /// Gets a UIElement to display an item for the specified the context.
        /// </summary>
        /// <param name="itemToRender">The item for which to return a UIElement.</param>
        /// <param name="context">The context in which the item is to be rendered.</param>
        /// <param name="elementType">The type of UIElement to be returned.</param>
        /// <returns>A valid WPF UIElement, or null if none could be created.</returns>
        UIElement GetUIElement(TItem itemToRender, TContext context, UIElementType elementType);
    }
}
