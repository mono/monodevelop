using System;

namespace Microsoft.WindowsAPICodePack.ShellExtensions
{
    /// <summary>    
    /// This class attribute is applied to a Preview Handler to specify registration parameters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PreviewHandlerAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the attribute.
        /// </summary>
        /// <param name="name">Name of the Handler</param>
        /// <param name="extensions">Semi-colon-separated list of file extensions supported by the handler.</param>
        /// <param name="appId">A unique guid used for process isolation.</param>
        public PreviewHandlerAttribute(string name, string extensions, string appId)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (extensions == null) throw new ArgumentNullException("extensions");
            if (appId == null) throw new ArgumentNullException("appId");

            Name = name;
            Extensions = extensions;
            AppId = appId;
            DisableLowILProcessIsolation = false;
        }

        /// <summary>
        /// Gets the name of the handler.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the semi-colon-separated list of extensions supported by the preview handler.
        /// </summary>
        public string Extensions { get; private set; }

        /// <summary>
        /// Gets the AppId associated with the handler for use with the surrogate host process.
        /// </summary>
        public string AppId { get; private set; }

        /// <summary>
        /// Disables low integrity-level process isolation.        
        /// <remarks>This should be avoided as it could be a security risk.</remarks>
        /// </summary>
        public bool DisableLowILProcessIsolation { get; set; }
    }
}
