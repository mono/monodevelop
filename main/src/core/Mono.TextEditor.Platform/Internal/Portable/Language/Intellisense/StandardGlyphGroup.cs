////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Describes the different types of glyphs that can be displayed in the default completion tool implementation.
    /// </summary>
    public enum StandardGlyphGroup
    {
        /// <summary>
        /// Describes symbols for classes.
        /// </summary>
        GlyphGroupClass = 0,

        /// <summary>
        /// Describes symbols for constants.
        /// </summary>
        GlyphGroupConstant = 6,

        /// <summary>
        /// Describes symbols for delegates.
        /// </summary>
        GlyphGroupDelegate = 12,

        /// <summary>
        /// Describes symbols for enumerations.
        /// </summary>
        GlyphGroupEnum = 18,

        /// <summary>
        /// Describes symbols for enumeration members.
        /// </summary>
        GlyphGroupEnumMember = 24,

        /// <summary>
        /// Describes symbols for events.
        /// </summary>
        GlyphGroupEvent = 30,

        /// <summary>
        /// Describes symbols for exceptions.
        /// </summary>
        GlyphGroupException = 36,

        /// <summary>
        /// Describes symbols for fields.
        /// </summary>
        GlyphGroupField = 42,

        /// <summary>
        /// Describes symbols for interfaces.
        /// </summary>
        GlyphGroupInterface = 48,

        /// <summary>
        /// Describes symbols for macros.
        /// </summary>
        GlyphGroupMacro = 54,

        /// <summary>
        /// Describes symbols for maps.
        /// </summary>
        GlyphGroupMap = 60,

        /// <summary>
        /// Describes symbols for map items.
        /// </summary>
        GlyphGroupMapItem = 66,

        /// <summary>
        /// Describes symbols for methods.
        /// </summary>
        GlyphGroupMethod = 72,

        /// <summary>
        /// Describes symbols for overloads.
        /// </summary>
        GlyphGroupOverload = 78,

        /// <summary>
        /// Describes symbols for modules.
        /// </summary>
        GlyphGroupModule = 84,

        /// <summary>
        /// Describes symbols for namespaces.
        /// </summary>
        GlyphGroupNamespace = 90,

        /// <summary>
        /// Describes symbols for operators.
        /// </summary>
        GlyphGroupOperator = 96,

        /// <summary>
        /// Describes symbols for properties.
        /// </summary>
        GlyphGroupProperty = 102,

        /// <summary>
        /// Describes symbols for structs.
        /// </summary>
        GlyphGroupStruct = 108,

        /// <summary>
        /// Describes symbols for templates.
        /// </summary>
        GlyphGroupTemplate = 114,

        /// <summary>
        /// Describes symbols for typedefs.
        /// </summary>
        GlyphGroupTypedef = 120,

        /// <summary>
        /// Describes symbols for types.
        /// </summary>
        GlyphGroupType = 126,

        /// <summary>
        /// Describes symbols for unions.
        /// </summary>
        GlyphGroupUnion = 132,

        /// <summary>
        /// Describes symbols for variables.
        /// </summary>
        GlyphGroupVariable = 138,

        /// <summary>
        /// Describes symbols for value types.
        /// </summary>
        GlyphGroupValueType = 144,

        /// <summary>
        /// Describes intrinsic symbols.
        /// </summary>
        GlyphGroupIntrinsic = 150,

        /// <summary>
        /// Describes symbols for J# methods.
        /// </summary>
        GlyphGroupJSharpMethod = 156,

        /// <summary>
        /// Describes symbols for J# fields.
        /// </summary>
        GlyphGroupJSharpField = 162,

        /// <summary>
        /// Describes symbols for J# classes.
        /// </summary>
        GlyphGroupJSharpClass = 168,

        /// <summary>
        /// Describes symbols for J# namespaces.
        /// </summary>
        GlyphGroupJSharpNamespace = 174,

        /// <summary>
        /// Describes symbols for J# interfaces.
        /// </summary>
        GlyphGroupJSharpInterface = 180,

        /// <summary>
        /// Describes symbols for errors.
        /// </summary>
        GlyphGroupError = 186,

        /// <summary>
        /// Describes symbols for BSC files.
        /// </summary>
        GlyphBscFile = 191,

        /// <summary>
        /// Describes symbols for assemblies.
        /// </summary>
        GlyphAssembly = 192,

        /// <summary>
        /// Describes symbols for libraries.
        /// </summary>
        GlyphLibrary = 193,

        /// <summary>
        /// Describes symbols for VB projects.
        /// </summary>
        GlyphVBProject = 194,

        /// <summary>
        /// Describes symbols for C# projects.
        /// </summary>
        GlyphCoolProject = 196,

        /// <summary>
        /// Describes symbols for C++ projects.
        /// </summary>
        GlyphCppProject = 199,

        /// <summary>
        /// Describes symbols for dialog identifiers.
        /// </summary>
        GlyphDialogId = 200,

        /// <summary>
        /// Describes symbols for open folders.
        /// </summary>
        GlyphOpenFolder = 201,

        /// <summary>
        /// Describes symbols for closed folders.
        /// </summary>
        GlyphClosedFolder = 202,

        /// <summary>
        /// Describes arrow symbols.
        /// </summary>
        GlyphArrow = 203,

        /// <summary>
        /// Describes symbols for C# files.
        /// </summary>
        GlyphCSharpFile = 204,

        /// <summary>
        /// Describes symbols for C# expansions.
        /// </summary>
        GlyphCSharpExpansion = 205,

        /// <summary>
        /// Describes symbols for keywords.
        /// </summary>
        GlyphKeyword = 206,

        /// <summary>
        /// Describes symbols for information.
        /// </summary>
        GlyphInformation = 207,

        /// <summary>
        /// Describes symbols for references.
        /// </summary>
        GlyphReference = 208,

        /// <summary>
        /// Describes symbols for recursion.
        /// </summary>
        GlyphRecursion = 209,

        /// <summary>
        /// Describes symbols for XML items.
        /// </summary>
        GlyphXmlItem = 210,

        /// <summary>
        /// Describes symbols for J# projects.
        /// </summary>
        GlyphJSharpProject = 211,

        /// <summary>
        /// Describes symbols for J# documents.
        /// </summary>
        GlyphJSharpDocument = 212,

        /// <summary>
        /// Describes symbols for forwarded types.
        /// </summary>
        GlyphForwardType = 213,

        /// <summary>
        /// Describes symbols for callers graphs.
        /// </summary>
        GlyphCallersGraph = 214,

        /// <summary>
        /// Describes symbols for call graphs.
        /// </summary>
        GlyphCallGraph = 215,

        /// <summary>
        /// Describes symbols for build warnings.
        /// </summary>
        GlyphWarning = 216,

        /// <summary>
        /// Describes symbols for something that may be a reference.
        /// </summary>
        GlyphMaybeReference = 217,

        /// <summary>
        /// Describes symbols for something that may be a caller.
        /// </summary>
        GlyphMaybeCaller = 218,

        /// <summary>
        /// Describes symbols for something that may be a call.
        /// </summary>
        GlyphMaybeCall = 219,

        /// <summary>
        /// Describes symbols for extension methods.
        /// </summary>
        GlyphExtensionMethod = 220,

        /// <summary>
        /// Describes symbols for internal extension methods.
        /// </summary>
        GlyphExtensionMethodInternal = 221,

        /// <summary>
        /// Describes symbols for friend extension methods.
        /// </summary>
        GlyphExtensionMethodFriend = 222,

        /// <summary>
        /// Describes symbols for protected extension methods.
        /// </summary>
        GlyphExtensionMethodProtected = 223,

        /// <summary>
        /// Describes symbols for private extension methods.
        /// </summary>
        GlyphExtensionMethodPrivate = 224,

        /// <summary>
        /// Describes symbols for extension method shortcuts.
        /// </summary>
        GlyphExtensionMethodShortcut = 225,

        /// <summary>
        /// Describes symbols for XML attributes.
        /// </summary>
        GlyphXmlAttribute = 226,

        /// <summary>
        /// Describes symbols for child XML elements.
        /// </summary>
        GlyphXmlChild = 227,

        /// <summary>
        /// Describes symbols for descendant XML elements.
        /// </summary>
        GlyphXmlDescendant = 228,

        /// <summary>
        /// Describes symbols for XML namespaces.
        /// </summary>
        GlyphXmlNamespace = 229,

        /// <summary>
        /// Describes symbols with a question mark for XML attributes. 
        /// </summary>
        GlyphXmlAttributeQuestion = 230,

        /// <summary>
        /// Describes symbols with a check mark for XML attributes. 
        /// </summary>
        GlyphXmlAttributeCheck = 231,

        /// <summary>
        /// Describes symbols with a question mark for XML child elements.
        /// </summary>
        GlyphXmlChildQuestion = 232,

        /// <summary>
        /// Describes symbols with a check mark for XML child elements.
        /// </summary>
        GlyphXmlChildCheck = 233,

        /// <summary>
        /// Describes symbols with a question mark for XML descendant elements.
        /// </summary>
        GlyphXmlDescendantQuestion = 234,

        /// <summary>
        /// Describes symbols with a check mark for XML descendant elements.
        /// </summary>
        GlyphXmlDescendantCheck = 235,

        /// <summary>
        /// Describes symbols for completion warnings.
        /// </summary>
        GlyphCompletionWarning = 236,

        /// <summary>
        /// Describes symbols for unknown types.
        /// </summary>
        GlyphGroupUnknown = 237
    }
}