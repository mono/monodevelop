using System;

namespace Microsoft.WindowsAPICodePack.ShellExtensions
{
    /// <summary>    
    /// This class attribute is applied to a Thumbnail Provider to specify registration parameters
    /// and aesthetic attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ThumbnailProviderAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the attribute.
        /// </summary>
        /// <param name="name">Name of the provider</param>
        /// <param name="extensions">Semi-colon-separated list of extensions supported by this provider.</param>
        public ThumbnailProviderAttribute(string name, string extensions)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (extensions == null) throw new ArgumentNullException("extensions");

            Name = name;
            Extensions = extensions;

            DisableProcessIsolation = false;
            ThumbnailCutoff = ThumbnailCutoffSize.Square20;
            TypeOverlay = null;
            ThumbnailAdornment = ThumbnailAdornment.Default;
        }

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the semi-colon-separated list of extensions supported by the provider.
        /// </summary>
        public string Extensions { get; private set; }

        // optional parameters below.

        /// <summary>
        /// Opts-out of running within the surrogate process DllHost.exe.
        /// This will reduce robustness and security.
        /// This value should be true if the provider does not implement <typeparamref name="IThumbnailFromStream"/>.
        /// </summary>
        // Note: The msdn documentation and property name are contradicting.
        // http://msdn.microsoft.com/en-us/library/cc144118(VS.85).aspx
        public bool DisableProcessIsolation { get; set; } // If true: Makes it run IN PROCESS.


        /// <summary>
        /// Below this size thumbnail images will not be generated - file icons will be used instead.
        /// </summary>
        public ThumbnailCutoffSize ThumbnailCutoff { get; set; }

        /// <summary>
        /// A resource reference string pointing to the icon to be used as an overlay on the bottom right of the thumbnail.
        /// ex. ISVComponent.dll@,-155
        /// ex. C:\Windows\System32\SampleIcon.ico
        /// If an empty string is provided, no overlay will be used.
        /// If the property is set to null, the default icon for the associated icon will be used as an overlay.
        /// </summary>
        public string TypeOverlay { get; set; }

        /// <summary>
        /// Specifies the <typeparamref name="ThumbnailAdornment"/> for the thumbnail.
        /// <remarks>
        /// Only 32bpp bitmaps support adornments. 
        /// While 24bpp bitmaps will be displayed, their adornments will not.
        /// If an adornment is specified by the file-type's associated application, 
        /// the applications adornment will override the value specified in this registration.</remarks>
        /// </summary>
        public ThumbnailAdornment ThumbnailAdornment { get; set; }
    }

    /// <summary>
    /// Defines the minimum thumbnail size for which thumbnails will be generated.
    /// </summary>
    public enum ThumbnailCutoffSize
    {
        /// <summary>
        /// Default size of 20x20
        /// </summary>
        Square20 = -1, //For 20x20, you do not add any key in the registry

        /// <summary>
        /// Size of 32x32
        /// </summary>
        Square32 = 0,

        /// <summary>
        /// Size of 16x16
        /// </summary>
        Square16 = 1,

        /// <summary>
        /// Size of 48x48
        /// </summary>
        Square48 = 2,

        /// <summary>
        /// Size of 16x16. An alternative to Square16.
        /// </summary>
        Square16B = 3
    }

    /// <summary>
    /// Adornment applied to thumbnails.
    /// </summary>
    public enum ThumbnailAdornment
    {
        /// <summary>
        /// This will use the associated application's default icon as the adornment.
        /// </summary>
        Default = -1, // Default behaviour for no value added in registry

        /// <summary>
        /// No adornment
        /// </summary>
        None = 0,

        /// <summary>
        /// Drop shadow adornment
        /// </summary>
        DropShadow = 1,

        /// <summary>
        /// Photo border adornment
        /// </summary>
        PhotoBorder = 2,

        /// <summary>
        /// Video sprocket adornment
        /// </summary>
        VideoSprockets = 3
    }


}
