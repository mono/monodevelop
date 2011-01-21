// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
{
    // XDataObject functionality is only needed by the mesh-loading
    // code, thus private to the XMeshTextLoader class.
    internal partial class XMeshTextLoader
    {
        /// <summary>
        /// Helper method to retrieve an immediate child of the
        /// given <see cref="IXDataObject" /> with the given named type.
        /// </summary>
        /// <param name="dataObject">The <see cref="IXDataObject" /> instance the children of which to search</param>
        /// <param name="type">The template name of the .x file type to retrieve</param>
        /// <returns>The one child of the given type if present, null if no such child is present.</returns>
        /// <exception cref="InvalidOperationException">There are more than one child of the given type.</exception>
        private static IXDataObject GetSingleChild(IXDataObject dataObject, string type)
        {
            return dataObject.Children.SingleOrDefault(obj => obj.DataObjectType == type);
        }

        /// <summary>
        /// The base interface type for all data objects found in the .x file
        /// </summary>
        private interface IXDataObject
        {
            /// <summary>
            /// Returns true if the data object corresponds to some specific object that can
            /// be represented visually. For example, a frame or a mesh.
            /// Returns false if the data object is simply used as a data member of some other data object.
            /// For example, the vertices or materials for a mesh.
            /// </summary>
            bool IsVisualObject { get; }

            /// <summary>
            /// The template name of the data object's type.
            /// </summary>
            string DataObjectType { get; }

            /// <summary>
            /// The name of the data object itself (may be empty).
            /// </summary>
            string Name { get; }

            /// <summary>
            /// The text contained within the body of the data object, once
            /// all known data members of the data object have been parsed and
            /// removed from the body.
            /// </summary>
            string Body { get; }

            /// <summary>
            /// The immediate children of the data object.
            /// </summary>
            IEnumerable<IXDataObject> Children { get; }
        }

        /// <summary>
        /// Indicates the restriction type of an <see cref="IXTemplateObject" />. A data object
        /// with the <see cref="TemplateRestriction.Open" /> restriction can contain any data object of any type
        /// as children. A data object with the <see cref="TemplateRestriction.Restricted" /> restriction can contain
        /// only data objects of the types listed in the <see cref="IXTemplateObject.Restrictions"/>.
        /// A data object with the <see cref="TemplateRestriction.Closed" /> restriction may not contain any
        /// child data objects.
        /// </summary>
        private enum TemplateRestriction
        {
            Open,
            Restricted,
            Closed
        }

        /// <summary>
        /// A data object having the type "template".
        /// </summary>
        private interface IXTemplateObject : IXDataObject
        {
            IEnumerable<IXTemplateObject> Restrictions { get; }
            TemplateRestriction Restricted { get; }
        }

        /// <summary>
        /// A factory class used to create <see cref="IXDataObject" /> instances from
        /// text input.
        /// </summary>
        private class XDataObjectFactory
        {
            /// <summary>
            /// Creates an enumeration of data objects represented by the given text.
            /// </summary>
            /// <param name="inputText">The text to parse. After the method returns, this will reference a new string containing all the text that was not parsed.</param>
            /// <returns>An enumeration of <see cref="IXDataObject" /> instances represented by the text.</returns>
            public static IEnumerable<IXDataObject> ExtractDataObjects(ref string inputText)
            {
                return new XDataObjectFactory().ExtractDataObjectsImpl(ref inputText);
            }

            private XDataObjectFactory() { }

            #region Regex initialization

            private const RegexOptions defaultOptions = RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled;

            /// <summary>
            /// An expression describing the basic structure of an .x file data object
            /// </summary>
            private static Regex dataObjectRegex = new Regex(@"
                (?<type>[\w_]+)
                (:?\s+(?<name>[^\s{]+))?\s*
                {(?<body>
                    (?>
                        [^{}]+|
                        {(?<bracket>)|
                        }(?<-bracket>)
                    )*
                    (?(bracket)(?!))
                )}
            ", defaultOptions);

            /// <summary>
            /// An expression describing a reference to another data object, as found within the body of an .x file data object.
            /// </summary>
            private static Regex bodyReferenceRegex = new Regex(@"
                {\s*(?<name>[\w_]+)?\s*(?<uuid>\<\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\>)?\s*}
            ", defaultOptions);

            /// <summary>
            /// An expression describing a UUID declaration of a data object defined in an .x file.
            /// </summary>
            private static Regex uuidDeclarationRegex = new Regex(@"
                (?<uuid>\<\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\>)
            ", defaultOptions);

            /// <summary>
            /// An expression describing the restrictions for a template data object defined in an .x file.
            /// </summary>
            private static Regex restrictionsDeclarationRegex = new Regex(@"
                (?<=([^\w\s_]+\s*|^\s*))
                (?<restrict>\[\s*
                    (
                        (?<open>\.\.\.)|
                        (?<ref>(?<name>[\w_]+)(\s*(?<uuid>\<\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\>))?)
                        (\s*,\s*
                            (?<ref>(?<name>[\w_]+)(\s*(?<uuid>\<\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\>))?)
                        )*
                    )
                \s*\])
            ", defaultOptions | RegexOptions.ExplicitCapture);

            /// <summary>
            /// An expression describing an individual restriction declaration within a template object defined in an .x file.
            /// </summary>
            private static Regex restrictionDeclarationRegex = new Regex(@"
                (?<name>[\w_]+)(\s*(?<uuid>\<\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\>))?
            ", defaultOptions);

            #endregion // Regex initialization

            /// <summary>
            /// Implementation of <see cref="IXDataObject" />
            /// </summary>
            /// <remarks>Private implementation ensures that only the <see cref="XDataObjectFactory" /> can create this.</remarks>
            private class XDataObject : IXDataObject
            {
                public virtual bool IsVisualObject { get { return DataObjectType == "Frame" || DataObjectType == "Mesh"; } }
                public string DataObjectType { get; private set; }
                public string Name { get; private set; }
                public string Body { get; private set; }
                public IEnumerable<IXDataObject> Children { get; private set; }

                /// <summary>
                /// Constructor.
                /// </summary>
                /// <param name="type">The template name of the type of the data object</param>
                /// <param name="name">The name of the data object</param>
                /// <param name="body">The remaining unparsed body text of the data object</param>
                /// <param name="factory">The factory used to create this object</param>
                /// <exception cref="ArgumentNullException">Thrown if the <paramref name="type"/> or <paramref name="factory"/> arguments are null.</exception>
                /// <remarks>The factory passed in is used to further parse the object's body text, including
                /// resolving references to previously defined objects and templates.</remarks>
                public XDataObject(string type, string name, string body, XDataObjectFactory factory)
                {
                    if (type == null)
                    {
                        throw new ArgumentNullException("type");
                    }

                    if (factory == null)
                    {
                        throw new ArgumentNullException("factory");
                    }

                    DataObjectType = type;
                    Name = name;
                    Children = factory.ExtractDataObjectsImpl(ref body);
                    Body = body;
                }
            }

            /// <summary>
            /// Implementation of <see cref="IXTemplateObject" />
            /// </summary>
            /// <remarks>Private implementation ensures that only the <see cref="XDataObjectFactory" /> can create this.</remarks>
            private class XTemplateObject : XDataObject, IXTemplateObject
            {
                public override bool IsVisualObject { get { return false; } }
                public IEnumerable<IXTemplateObject> Restrictions { get; private set; }
                public TemplateRestriction Restricted { get; private set; }

                /// <summary>
                /// Constructor for template objects having the <see cref="TemplateRestriction.Restricted" /> restriction.
                /// </summary>
                /// <param name="name">The name of the template object</param>
                /// <param name="body">The remaining unparsed body of the template object</param>
                /// <param name="factory">The factory used to create this object</param>
                /// <param name="restrictions">A list of template objects representing the only valid types of data object children for this type</param>
                /// <seealso cref="XDataObject.XDataObject"/>
                public XTemplateObject(string name, string body, XDataObjectFactory factory, IList<IXTemplateObject> restrictions) :
                    base("template", name, body, factory)
                {
                    Restrictions = new ReadOnlyCollection<IXTemplateObject>(restrictions);
                    Restricted = TemplateRestriction.Restricted;
                }

                /// <summary>
                /// Constructor for template objects having the <see cref="TemplateRestriction.Open" /> or <see cref="TemplateRestriction.Closed" /> restriction.
                /// </summary>
                /// <param name="name">The name of the template object</param>
                /// <param name="body">The remaining unparsed body of the template object</param>
                /// <param name="factory">The factory used to create this object</param>
                /// <param name="restricted">The type of restriction of the template object</param>
                /// <exception cref="ArgumentException">Thrown if the restriction given is not either <see cref="TemplateRestriction.Open" /> or <see cref="TemplateRestriction.Closed" />.</exception>
                /// <seealso cref="XDataObject.XDataObject"/>
                public XTemplateObject(string name, string body, XDataObjectFactory factory, TemplateRestriction restricted) :
                    base("template", name, body, factory)
                {
                    if (restricted == TemplateRestriction.Restricted)
                    {
                        throw new ArgumentException("A restricted template must have actual restrictions. Without any, the restricted state may be only 'Open' or 'Closed'");
                    }

                    Restrictions = null;
                    Restricted = restricted;
                }
            }

            private Dictionary<string, XDataObject> objectDictionary = new Dictionary<string, XDataObject>();

            /// <summary>
            /// The actual implementation to extract data objects from input text complying with the .x file format.
            /// </summary>
            /// <param name="inputText">The text to parse</param>
            /// <returns>An enumeration of <see cref="IXDataObject" /> instances represented within the .x file text.</returns>
            private IEnumerable<IXDataObject> ExtractDataObjectsImpl(ref string inputText)
            {
                IEnumerable<IXDataObject> dataObjects =
                    ExtractByRegex<IXDataObject>(ref inputText, dataObjectRegex, ExtractDataObject);

                IEnumerable<IXDataObject> dataReferences =
                    ExtractByRegex<IXDataObject>(ref inputText, bodyReferenceRegex, groups =>
                    {
                        if (groups["uuid"].Success)
                        {
                            return objectDictionary[groups["uuid"].Value];
                        }

                        return objectDictionary[groups["name"].Value];
                    });

                return dataObjects.Concat(dataReferences);
            }

            /// <summary>
            /// Given a regex match for a data object, create a new <see cref="XDataObject" /> instance
            /// for the text matched.
            /// </summary>
            /// <param name="groups">The match groups for the matched regex expression</param>
            /// <returns>A new <see cref="XDataObject" /> instance based on the given regex match</returns>
            private XDataObject ExtractDataObject(GroupCollection groups)
            {
                string type = groups["type"].Value;
                string name = groups["name"].Value;
                string body = groups["body"].Value;
                string uuid;

                XDataObject dataObject;

                if (type == "template")
                {
                    dataObject = CreateTemplateObject(name, ref body, out uuid);
                }
                else
                {
                    uuid = ExtractUuid(ref body);
                    dataObject = new XDataObject(type, name, body, this);
                }

                RegisterObject(uuid, dataObject);

                return dataObject;
            }

            /// <summary>
            /// Creates an <see cref="XTemplateObject" /> for the given "template" data object
            /// type. Parses the UUID, and also the restriction list from the body, matching
            /// restriction references to known template objects when possible.
            /// </summary>
            /// <param name="name">The name of the template</param>
            /// <param name="body">The remaining unparsed body text for the template</param>
            /// <param name="uuid">Receives the declared UUID for the new template object</param>
            /// <returns>A new <see cref="XTemplateObject"/> instance</returns>
            private XTemplateObject CreateTemplateObject(string name, ref string body, out string uuid)
            {
                IEnumerable<IEnumerable<IXTemplateObject>> restrictEnums =
                    ExtractByRegex<IEnumerable<IXTemplateObject>>(
                        ref body, restrictionsDeclarationRegex, ExtractRestriction);
                List<IXTemplateObject> restrictList = new List<IXTemplateObject>();
                bool isOpen = false;

                foreach (IEnumerable<IXTemplateObject> restrictEnum in restrictEnums)
                {
                    foreach (IXTemplateObject restrictObject in restrictEnum)
                    {
                        if (restrictObject == null)
                        {
                            isOpen = true;
                        }
                        else if (!isOpen)
                        {
                            restrictList.Add(restrictObject);
                        }
                        else
                        {
                            throw new InvalidDataException(
                                string.Format(CultureInfo.InvariantCulture,
                                "Template \"{0}\" mixes open restriction with non-open.",
                                name));
                        }
                    }
                }

                uuid = ExtractUuid(ref body);

                return restrictList.Count > 0 ?
                    new XTemplateObject(name, body, this, restrictList) :
                    new XTemplateObject(name, body, this,
                        isOpen ? TemplateRestriction.Open : TemplateRestriction.Closed);
            }

            /// <summary>
            /// For a given restriction declaration, extracts the given templates
            /// referenced within the declaration, or null if the declaration is of
            /// an open restriction.
            /// </summary>
            /// <param name="groups">The match groups for the matched regex expression</param>
            /// <returns>The enumeration of <see cref="IXTemplateObjects" /> represented within the single restriction declaration.</returns>
            /// <remarks>The .x file format should not include multiple restriction declarations for
            /// a given template object. However, it is theoretically legal to have multiple declarations
            /// as long as they don't conflict (i.e. they either all are for an open restriction, or they
            /// all list templates for a restricted restriction). This parser will attempt to resolve
            /// such theoretically legal multiple declarations if present.</remarks>
            private IEnumerable<IXTemplateObject> ExtractRestriction(GroupCollection groups)
            {
                if (groups["open"].Success)
                {
                    yield return null;
                }
                else
                {
                    foreach (Capture reference in groups["ref"].Captures)
                    {
                        Match restrictMatch = restrictionDeclarationRegex.Match(reference.Value);
                        XDataObject dataObject;

                        if (!restrictMatch.Groups["uuid"].Success ||
                            (dataObject = RetrieveObject(restrictMatch.Groups["uuid"].Value)) == null)
                        {
                            dataObject = RetrieveObject(restrictMatch.Groups["name"].Value);
                        }

                        if (dataObject != null)
                        {
                            yield return (XTemplateObject)dataObject;
                        }
                    }
                }
            }

            /// <summary>
            /// Registers a given object in the factory's object cache
            /// </summary>
            /// <param name="uuid">The object's UUID, if present, null otherwise.</param>
            /// <param name="dataObject">The data object itself.</param>
            /// <remarks>The object's name will be used as the object key if no UUID is present.
            /// Note: the object dictionary will only ever contain the object
            /// most recently seen with a given name and/or UUID.  Ideally,
            /// a .x file will not use the same name for two different objects,
            /// and the specification is not clear on whether that's legal and
            /// if so, how to resolve duplicates (especially when it's possible
            /// to infer the correct object based on the expected type of object).
            /// In this implementation, however, no attempt is made to resolve
            /// duplicates intelligently; this may lead to the failure to populate
            /// some particular piece of the object tree, when a most recent
            /// object of a given name or UUID is not of the expected type.</remarks>
            private void RegisterObject(string uuid, XDataObject dataObject)
            {
                if (uuid != null)
                {
#if DEBUG
                    if (objectDictionary.ContainsKey(uuid))
                    {
                        Debug.WriteLine(string.Format("Key {0} already present", uuid));
                    }
#endif
                    objectDictionary[uuid] = dataObject;
                }

                if (!string.IsNullOrEmpty(dataObject.Name))
                {
#if DEBUG
                    if (objectDictionary.ContainsKey(dataObject.Name))
                    {
                        Debug.WriteLine(string.Format("Key {0} already present", dataObject.Name));
                    }
#endif
                    objectDictionary[dataObject.Name] = dataObject;
                }
            }

            /// <summary>
            /// Retrieves an <see cref="XDataObject" /> with the given key.
            /// </summary>
            /// <param name="key">The key of the object being requested.</param>
            /// <returns>The <see cref="XDataObject" /> with the given key in the factory's cache, null if the object is not present.</returns>
            private XDataObject RetrieveObject(string key)
            {
                if (objectDictionary.ContainsKey(key))
                {
                    return objectDictionary[key];
                }

                return null;
            }

            /// <summary>
            /// Extracts a UUID declaration from a data object body if present.
            /// </summary>
            /// <param name="body">The current unparsed .x file body of the data object</param>
            /// <returns>The UUID declaration if found, null otherwise.</returns>
            /// <remarks>For template objects, be sure to parse the template restrictions
            /// before trying to extract the UUID, so that any UUID references found in the
            /// template restrictions don't get picked up by this method.</remarks>
            private string ExtractUuid(ref string body)
            {
                string uuid;
                try
                {
                    uuid = ExtractByRegex(ref body, uuidDeclarationRegex,
                        uuidGroups => uuidGroups["uuid"].Value).SingleOrDefault();
                }
                catch (InvalidOperationException exc)
                {
                    throw new System.IO.InvalidDataException("Each data object may declare only one UUID", exc);
                }
                return uuid;
            }

            /// <summary>
            /// Processes the given text using the given regex. For every match, the text
            /// corresponding to that match is removed from the input text, and the <paramref name="processGroup"/>
            /// delegate is invoked to obtain whatever object instance corresponds to the
            /// matched text.
            /// </summary>
            /// <typeparam name="T">The type of object that will be returned for any matched text</typeparam>
            /// <param name="inputText">The text to parse. On return, this will reference to a new string containing only the text that was not parsed into new objects</param>
            /// <param name="regex">The regex expression to use to match text</param>
            /// <param name="processGroup">The delegate invoked for any matching text, and which returns a new object instance corresponding to the matched text</param>
            /// <returns>An enumeration of the objects created by parsing the text</returns>
            private static IEnumerable<T> ExtractByRegex<T>(ref string inputText,
                Regex regex, Func<GroupCollection, T> processGroup)
            {
                StringBuilder bodyBuilder = null;
                List<T> dataObjects = new List<T>();
                Match matchObject = regex.Match(inputText);
                int indexCopy = 0;

                while (matchObject.Success)
                {
                    // Deferring creation of the StringBuilder has a couple
                    // of beneficial effects: the buffer can be pre-sized to
                    // a likely reasonable size; more importantly, the code can
                    // avoid making a copy of the original input if no sub-objects
                    // were found.
                    if (bodyBuilder == null)
                    {
                        bodyBuilder = new StringBuilder(inputText.Length - matchObject.Length);
                    }

                    bodyBuilder.Append(inputText.Substring(indexCopy, matchObject.Index - indexCopy));
                    indexCopy = matchObject.Index + matchObject.Length;

                    dataObjects.Add(processGroup(matchObject.Groups));

                    matchObject = matchObject.NextMatch();
                }

                if (bodyBuilder != null)
                {
                    bodyBuilder.Append(inputText.Substring(indexCopy));

                    inputText = bodyBuilder.ToString();
                }

                return dataObjects;
            }
        }
    }
}
