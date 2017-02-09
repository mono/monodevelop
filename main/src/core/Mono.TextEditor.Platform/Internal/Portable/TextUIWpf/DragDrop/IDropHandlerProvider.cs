// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor.DragDrop
{

    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Creates an <see cref="IDropHandler"/> for a <see cref="IWpfTextView"/>.
    /// </summary>
    /// <remarks>
    /// <para>This is a MEF component part, and must be exported with the [Export(typeof(IDropHandlerProvider))] attribute. 
    /// It must also have one or more [DropFormat("FormatKind")] attributes. For example,
    /// if the provided <see cref="IDropHandler"/> handles both text and RTF formats, two <see cref="DropFormatAttribute"/> annotations
    /// are necessary:
    /// </para>
    /// <para>[Export(typeof(IDropHandlerProvider))]</para>
    /// <para>[DropFormat("Rich Text Format")]</para>
    /// <para>[DropFormat("Text")]</para>
    /// <para><see cref="IDropHandler"/> objects are used to handle drag and drop operations for various data formats
    /// and act as extension points for customizing drop operations.</para>
    /// <para>If you provide a <see cref="IDropHandler"/>, you must
    /// export a factory service in order to instantiate the <see cref="IDropHandler"/> with the required context.
    /// At runtime the editor looks for these exports, and calls the GetAssociatedDropHandler method to activate the 
    /// <see cref="IDropHandler"/> associated with the factory service. The <see cref="IDropHandler"/> will then be notified
    /// when a drag and drop operation of the corresponding data format has been requested. All other tasks, 
    /// such as capturing mouse events, scrolling the view, etc., are handled by the editor.
    /// </para>
    /// <para>
    /// <see cref="DropFormatAttribute"/> objects specify
    /// the data formats that the associated <see cref="IDropHandler"/> can handle. These formats are specified by string
    /// keys and correspond to the standard data formats defined by the <see cref="System.Windows.IDataObject"/> interface. For
    /// example, to handle RTF content you must specify [DropFormat("Rich Text Format")], as defined in the 
    /// <see cref="System.Windows.IDataObject"/> interface.
    /// </para>
    /// <para>
    /// A single <see cref="System.Windows.IDataObject"/> can contain multiple data formats, so that multiple drop handlers
    /// might be available to handle the formats. In this case, the data is delegated to the drop handlers according to a predefined set of priorities.
    /// The format priorities are as follows, from the highest to the lowest priority:
    /// </para>
    /// <para>
    /// Any custom format
    /// </para>
    /// <para>
    /// FileDrop
    /// </para>
    /// <para>
    /// EnhancedMetafile
    /// </para>
    /// <para>
    /// WaveAudio
    /// </para>
    /// <para>
    /// Riff
    /// </para>
    /// <para>
    /// Dif
    /// </para>
    /// <para>
    /// Locale
    /// </para>
    /// <para>
    /// Palette
    /// </para>
    /// <para>
    /// PenData
    /// </para>
    /// <para>
    /// Serializable
    /// </para>
    /// <para>
    /// SymbolicLink
    /// </para>
    /// <para>
    /// Xaml
    /// </para>
    /// <para>
    /// XamlPackage
    /// </para>
    /// <para>
    /// Tiff
    /// </para>
    /// <para>
    /// Bitmap
    /// </para>
    /// <para>
    /// Dib
    /// </para>
    /// <para>
    /// MetafilePicture
    /// </para>
    /// <para>
    /// CommaSeparatedValue
    /// </para>
    /// <para>
    /// StringFormat
    /// </para>
    /// <para>
    /// Html
    /// </para>
    /// <para>
    /// Rtf
    /// </para>
    /// <para>
    /// UnicodeText
    /// </para>
    /// <para>
    /// OemText
    /// </para>
    /// <para>
    /// Text
    /// </para>
    /// </remarks>
    public interface IDropHandlerProvider
    {
	    /// <summary>
	    /// Gets an <see cref="IDropHandler"/> for a specified <see cref="IWpfTextView"/>. 
	    /// </summary>
        /// <param name="wpfTextView">The text view for which to get the drop handler.</param>
        /// <returns>The <see cref="IDropHandler"/>.</returns>
        /// <remarks>This method is expected to return non-null values.</remarks>
        IDropHandler GetAssociatedDropHandler(IWpfTextView wpfTextView);
    }
}
