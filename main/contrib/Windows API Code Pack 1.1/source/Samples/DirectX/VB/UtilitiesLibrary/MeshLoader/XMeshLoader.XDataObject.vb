' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports System
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions


Namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
    ' XDataObject functionality is only needed by the mesh-loading
    ' code, thus private to the XMeshTextLoader class.
    Partial Friend Class XMeshTextLoader
        ''' <summary>
        ''' Helper method to retrieve an immediate child of the
        ''' given <see cref="IXDataObject" /> with the given named type.
        ''' </summary>
        ''' <param name="dataObject">The <see cref="IXDataObject" /> instance the children of which to search</param>
        ''' <param name="type">The template name of the .x file type to retrieve</param>
        ''' <returns>The one child of the given type if present, null if no such child is present.</returns>
        ''' <exception cref="InvalidOperationException">There are more than one child of the given type.</exception>
        Private Shared Function GetSingleChild(ByVal dataObject As IXDataObject, ByVal type As String) As IXDataObject
            Return dataObject.Children.SingleOrDefault(Function(obj) obj.DataObjectType = type)
        End Function

        ''' <summary>
        ''' The base interface type for all data objects found in the .x file
        ''' </summary>
        Private Interface IXDataObject
            ''' <summary>
            ''' Returns true if the data object corresponds to some specific object that can
            ''' be represented visually. For example, a frame or a mesh.
            ''' Returns false if the data object is simply used as a data member of some other data object.
            ''' For example, the vertices or materials for a mesh.
            ''' </summary>
            ReadOnly Property IsVisualObject() As Boolean

            ''' <summary>
            ''' The template name of the data object's type.
            ''' </summary>
            ReadOnly Property DataObjectType() As String

            ''' <summary>
            ''' The name of the data object itself (may be empty).
            ''' </summary>
            ReadOnly Property Name() As String

            ''' <summary>
            ''' The text contained within the body of the data object, once
            ''' all known data members of the data object have been parsed and
            ''' removed from the body.
            ''' </summary>
            ReadOnly Property Body() As String

            ''' <summary>
            ''' The immediate children of the data object.
            ''' </summary>
            ReadOnly Property Children() As IEnumerable(Of IXDataObject)
        End Interface

        ''' <summary>
        ''' Indicates the restriction type of an <see cref="IXTemplateObject" />. A data object
        ''' with the <see cref="TemplateRestriction.Open" /> restriction can contain any data object of any type
        ''' as children. A data object with the <see cref="TemplateRestriction.Restricted" /> restriction can contain
        ''' only data objects of the types listed in the <see cref="IXTemplateObject.Restrictions"/>.
        ''' A data object with the <see cref="TemplateRestriction.Closed" /> restriction may not contain any
        ''' child data objects.
        ''' </summary>
        Private Enum TemplateRestriction
            Open
            Restricted
            Closed
        End Enum

        ''' <summary>
        ''' A data object having the type "template".
        ''' </summary>
        Private Interface IXTemplateObject
            Inherits IXDataObject

            ReadOnly Property Restrictions() As IEnumerable(Of IXTemplateObject)
            ReadOnly Property Restricted() As TemplateRestriction
        End Interface

        ''' <summary>
        ''' A factory class used to create <see cref="IXDataObject" /> instances from
        ''' text input.
        ''' </summary>
        Private Class XDataObjectFactory
            ''' <summary>
            ''' Creates an enumeration of data objects represented by the given text.
            ''' </summary>
            ''' <param name="inputText">The text to parse. After the method returns, this will reference a new string containing all the text that was not parsed.</param>
            ''' <returns>An enumeration of <see cref="IXDataObject" /> instances represented by the text.</returns>
            Public Shared Function ExtractDataObjects(ByRef inputText As String) As IEnumerable(Of IXDataObject)
                Return New XDataObjectFactory().ExtractDataObjectsImpl(inputText)
            End Function

            Private Sub New()

            End Sub


#Region "Regex initialization"

            Private Const defaultOptions As RegexOptions = RegexOptions.IgnorePatternWhitespace Or RegexOptions.Compiled

            ''' <summary>
            ''' An expression describing the basic structure of an .x file data object
            ''' </summary>
            Private Shared dataObjectRegex As New Regex( _
            "   (?<type>[\w_]+)" & _
                "(:?\s+(?<name>[^\s{]+))?\s*" & _
                "{(?<body>" & _
                    "(?>" & _
                        "[^{}]+|" & _
                        "{(?<bracket>)|" & _
                        "}(?<-bracket>)" & _
                    ")*" & _
                    "(?(bracket)(?!))" & _
                ")}", defaultOptions)

            ''' <summary>
            ''' An expression describing a reference to another data object, as found within the body of an .x file data object.
            ''' </summary>
            Private Shared bodyReferenceRegex As New Regex( _
            "  {\s*(?<name>[\w_]+)?\s*(?<uuid>\<\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\>)?\s*}", defaultOptions)

            ''' <summary>
            ''' An expression describing a UUID declaration of a data object defined in an .x file.
            ''' </summary>
            Private Shared uuidDeclarationRegex As New Regex( _
            "    (?<uuid>\<\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\>)", defaultOptions)

            ''' <summary>
            ''' An expression describing the restrictions for a template data object defined in an .x file.
            ''' </summary>
            Private Shared restrictionsDeclarationRegex As New Regex( _
            "    (?<=([^\w\s_]+\s*|^\s*))" & _
                "(?<restrict>\[\s*" & _
                    "(" & _
                        "(?<open>\.\.\.)|" & _
                        "(?<ref>(?<name>[\w_]+)(\s*(?<uuid>\<\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\>))?)" & _
                        "(\s*,\s*" & _
                            "(?<ref>(?<name>[\w_]+)(\s*(?<uuid>\<\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\>))?)" & _
                        ")*" & _
                    ")" & _
                "\s*\])", defaultOptions Or RegexOptions.ExplicitCapture)

            ''' <summary>
            ''' An expression describing an individual restriction declaration within a template object defined in an .x file.
            ''' </summary>
            Private Shared restrictionDeclarationRegex As New Regex( _
            "    (?<name>[\w_]+)(\s*(?<uuid>\<\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\>))?", defaultOptions)

#End Region ' Regex initialization

            ''' <summary>
            ''' Implementation of <see cref="IXDataObject" />
            ''' </summary>
            ''' <remarks>Private implementation ensures that only the <see cref="XDataObjectFactory" /> can create this.</remarks>
            Private Class XDataObject
                Implements IXDataObject

                Public Overridable ReadOnly Property IsVisualObject() As Boolean Implements IXDataObject.IsVisualObject
                    Get
                        Return DataObjectType = "Frame" Or DataObjectType = "Mesh"
                    End Get
                End Property

                Private _DataObjectType As String
                Public ReadOnly Property DataObjectType() As String Implements IXDataObject.DataObjectType
                    Get
                        Return _DataObjectType
                    End Get
                End Property

                Private _Name As String
                Public ReadOnly Property Name() As String Implements IXDataObject.Name
                    Get
                        Return _Name
                    End Get
                End Property

                Private _Body As String
                Public ReadOnly Property Body() As String Implements IXDataObject.Body
                    Get
                        Return _Body
                    End Get
                End Property

                Private _Children As IEnumerable(Of IXDataObject)
                Public ReadOnly Property Children() As IEnumerable(Of IXDataObject) Implements IXDataObject.Children
                    Get
                        Return _Children
                    End Get
                End Property

                ''' <summary>
                ''' Constructor.
                ''' </summary>
                ''' <param name="type">The template name of the type of the data object</param>
                ''' <param name="name">The name of the data object</param>
                ''' <param name="body">The remaining unparsed body text of the data object</param>
                ''' <param name="factory">The factory used to create this object</param>
                ''' <exception cref="ArgumentNullException">Thrown if the <paramref name="type"/> or <paramref name="factory"/> arguments are null.</exception>
                ''' <remarks>The factory passed in is used to further parse the object's body text, including
                ''' resolving references to previously defined objects and templates.</remarks>
                Public Sub New(ByVal type As String, ByVal name As String, ByVal body As String, ByVal factory As XDataObjectFactory)
                    If type Is Nothing Then
                        Throw New ArgumentNullException("type")
                    End If

                    If factory Is Nothing Then
                        Throw New ArgumentNullException("factory")
                    End If

                    _DataObjectType = type
                    _Name = name
                    _Children = factory.ExtractDataObjectsImpl(body)
                    _Body = body
                End Sub
            End Class

            ''' <summary>
            ''' Implementation of <see cref="IXTemplateObject" />
            ''' </summary>
            ''' <remarks>Private implementation ensures that only the <see cref="XDataObjectFactory" /> can create this.</remarks>
            Private Class XTemplateObject
                Inherits XDataObject
                Implements IXTemplateObject

                Public Overrides ReadOnly Property IsVisualObject() As Boolean
                    Get
                        Return False
                    End Get
                End Property

                Private _Restrictions As IEnumerable(Of IXTemplateObject)
                Public ReadOnly Property Restrictions() As IEnumerable(Of IXTemplateObject) Implements IXTemplateObject.Restrictions
                    Get
                        Return _Restrictions
                    End Get
                End Property

                Private _Restricted As TemplateRestriction
                Public ReadOnly Property Restricted() As TemplateRestriction Implements IXTemplateObject.Restricted
                    Get
                        Return _Restricted
                    End Get
                End Property

                ''' <summary>
                ''' Constructor for template objects having the <see cref="TemplateRestriction.Restricted" /> restriction.
                ''' </summary>
                ''' <param name="name">The name of the template object</param>
                ''' <param name="body">The remaining unparsed body of the template object</param>
                ''' <param name="factory">The factory used to create this object</param>
                ''' <param name="restrictions">A list of template objects representing the only valid types of data object children for this type</param>
                ''' <seealso cref="XDataObject.XDataObject"/>
                Public Sub New(ByVal name As String, ByVal body As String, ByVal factory As XDataObjectFactory, ByVal restrictions As IList(Of IXTemplateObject))
                    MyBase.New("template", name, body, factory)

                    _Restrictions = New ReadOnlyCollection(Of IXTemplateObject)(restrictions)
                    _Restricted = TemplateRestriction.Restricted
                End Sub

                ''' <summary>
                ''' Constructor for template objects having the <see cref="TemplateRestriction.Open" /> or <see cref="TemplateRestriction.Closed" /> restriction.
                ''' </summary>
                ''' <param name="name">The name of the template object</param>
                ''' <param name="body">The remaining unparsed body of the template object</param>
                ''' <param name="factory">The factory used to create this object</param>
                ''' <param name="restricted">The type of restriction of the template object</param>
                ''' <exception cref="ArgumentException">Thrown if the restriction given is not either <see cref="TemplateRestriction.Open" /> or <see cref="TemplateRestriction.Closed" />.</exception>
                ''' <seealso cref="XDataObject.XDataObject"/>
                Public Sub New(ByVal name As String, ByVal body As String, ByVal factory As XDataObjectFactory, ByVal restricted As TemplateRestriction)
                    MyBase.New("template", name, body, factory)

                    If restricted = TemplateRestriction.Restricted Then
                        Throw New ArgumentException("A restricted template must have actual restrictions. Without any, the restricted state may be only 'Open' or 'Closed'")
                    End If

                    _Restrictions = Nothing
                    _Restricted = restricted
                End Sub
            End Class

            Private objectDictionary As Dictionary(Of String, XDataObject) = New Dictionary(Of String, XDataObject)()

            ''' <summary>
            ''' The actual implementation to extract data objects from input text complying with the .x file format.
            ''' </summary>
            ''' <param name="inputText">The text to parse</param>
            ''' <returns>An enumeration of <see cref="IXDataObject" /> instances represented within the .x file text.</returns>
            Private Function ExtractDataObjectsImpl(ByRef inputText As String) As IEnumerable(Of IXDataObject)
                Dim dataObjects As IEnumerable(Of IXDataObject) = _
                    ExtractByRegex(Of IXDataObject)(inputText, dataObjectRegex, AddressOf ExtractDataObject)

                Dim dataReferences As IEnumerable(Of IXDataObject) = _
                    ExtractByRegex(Of IXDataObject)(inputText, bodyReferenceRegex, AddressOf ExtractReference)

                Return dataObjects.Concat(dataReferences)
            End Function

            ''' <summary>
            ''' Given a regex match for a data object, create a new <see cref="XDataObject" /> instance
            ''' for the text matched.
            ''' </summary>
            ''' <param name="groups">The match groups for the matched regex expression</param>
            ''' <returns>A new <see cref="XDataObject" /> instance based on the given regex match</returns>
            Private Function ExtractDataObject(ByVal groups As GroupCollection) As XDataObject
                Dim typeName As String = groups("type").Value
                Dim name As String = groups("name").Value
                Dim body As String = groups("body").Value
                Dim uuid As String = Nothing

                Dim dataObject As XDataObject

                If typeName = "template" Then
                    dataObject = CreateTemplateObject(name, body, uuid)
                Else
                    uuid = ExtractUuid(body)
                    dataObject = New XDataObject(typeName, name, body, Me)
                End If

                RegisterObject(uuid, dataObject)

                Return dataObject
            End Function

            Private Function ExtractReference(ByVal groups As GroupCollection) As XDataObject
                If groups("uuid").Success Then
                    Return objectDictionary(groups("uuid").Value)
                End If

                Return objectDictionary(groups("name").Value)
            End Function

            ''' <summary>
            ''' Creates an <see cref="XTemplateObject" /> for the given "template" data object
            ''' type. Parses the UUID, and also the restriction list from the body, matching
            ''' restriction references to known template objects when possible.
            ''' </summary>
            ''' <param name="name">The name of the template</param>
            ''' <param name="body">The remaining unparsed body text for the template</param>
            ''' <param name="uuid">Receives the declared UUID for the new template object</param>
            ''' <returns>A new <see cref="XTemplateObject"/> instance</returns>
            Private Function CreateTemplateObject(ByVal name As String, ByRef body As String, ByRef uuid As String) As XTemplateObject
                Dim restrictEnums As IEnumerable(Of IEnumerable(Of IXTemplateObject)) = _
                    ExtractByRegex(Of IEnumerable(Of IXTemplateObject))( _
                        body, restrictionsDeclarationRegex, AddressOf ExtractRestriction)
                Dim restrictList As List(Of IXTemplateObject) = New List(Of IXTemplateObject)()
                Dim isOpen As Boolean = False

                For Each restrictEnum As IEnumerable(Of IXTemplateObject) In restrictEnums
                    For Each restrictObject As IXTemplateObject In restrictEnum
                        If restrictObject Is Nothing Then
                            isOpen = True
                        ElseIf Not isOpen Then
                            restrictList.Add(restrictObject)
                        Else
                            Throw New InvalidDataException( _
                                String.Format(CultureInfo.InvariantCulture, _
                                "Template ""{0}"" mixes open restriction with non-open.", _
                                name))
                        End If
                    Next
                Next

                uuid = ExtractUuid(body)

                Return If(restrictList.Count > 0, _
                    New XTemplateObject(name, body, Me, restrictList), _
                    New XTemplateObject(name, body, Me, _
                        If(isOpen, TemplateRestriction.Open, TemplateRestriction.Closed)))
            End Function

            ''' <summary>
            ''' For a given restriction declaration, extracts the given templates
            ''' referenced within the declaration, or null if the declaration is of
            ''' an open restriction.
            ''' </summary>
            ''' <param name="groups">The match groups for the matched regex expression</param>
            ''' <returns>The enumeration of <see cref="IXTemplateObjects" /> represented within the single restriction declaration.</returns>
            ''' <remarks>The .x file format should not include multiple restriction declarations for
            ''' a given template object. However, it is theoretically legal to have multiple declarations
            ''' as long as they don't conflict (i.e. they either all are for an open restriction, or they
            ''' all list templates for a restricted restriction). This parser will attempt to resolve
            ''' such theoretically legal multiple declarations if present.</remarks>
            Private Function ExtractRestriction(ByVal groups As GroupCollection) As IEnumerable(Of IXTemplateObject)
                Dim returnEnumeration As List(Of IXTemplateObject) = New List(Of IXTemplateObject)()

                If groups("open").Success Then
                    returnEnumeration.Add(Nothing)
                Else
                    For Each reference As Capture In groups("ref").Captures
                        Dim restrictMatch As Match = restrictionDeclarationRegex.Match(reference.Value)
                        Dim dataObject As XDataObject

                        If restrictMatch.Groups("uuid").Success Then
                            dataObject = RetrieveObject(restrictMatch.Groups("uuid").Value)
                            If dataObject Is Nothing Then
                                dataObject = RetrieveObject(restrictMatch.Groups("name").Value)
                            End If
                        Else
                            dataObject = RetrieveObject(restrictMatch.Groups("name").Value)
                        End If

                        If dataObject IsNot Nothing Then
                            returnEnumeration.Add(CType(dataObject, IXTemplateObject))
                        End If
                    Next
                End If

                Return returnEnumeration
            End Function

            ''' <summary>
            ''' Registers a given object in the factory's object cache
            ''' </summary>
            ''' <param name="uuid">The object's UUID, if present, null otherwise.</param>
            ''' <param name="dataObject">The data object itself.</param>
            ''' <remarks>The object's name will be used as the object key if no UUID is present.
            ''' Note: the object dictionary will only ever contain the object
            ''' most recently seen with a given name and/or UUID.  Ideally,
            ''' a .x file will not use the same name for two different objects,
            ''' and the specification is not clear on whether that's legal and
            ''' if so, how to resolve duplicates (especially when it's possible
            ''' to infer the correct object based on the expected type of object).
            ''' In this implementation, however, no attempt is made to resolve
            ''' duplicates intelligently this may lead to the failure to populate
            ''' some particular piece of the object tree, when a most recent
            ''' object of a given name or UUID is not of the expected type.</remarks>
            Private Sub RegisterObject(ByVal uuid As String, ByVal dataObject As XDataObject)
                If uuid IsNot Nothing Then
#If DEBUG Then
                    If objectDictionary.ContainsKey(uuid) Then
                        Debug.WriteLine(String.Format("Key {0} already present", uuid))
                    End If
#End If
                    objectDictionary(uuid) = dataObject
                End If

                If Not String.IsNullOrEmpty(dataObject.Name) Then
#If DEBUG Then
                    If objectDictionary.ContainsKey(dataObject.Name) Then
                        Debug.WriteLine(String.Format("Key {0} already present", dataObject.Name))
                    End If
#End If
                    objectDictionary(dataObject.Name) = dataObject
                End If
            End Sub

            ''' <summary>
            ''' Retrieves an <see cref="XDataObject" /> with the given key.
            ''' </summary>
            ''' <param name="key">The key of the object being requested.</param>
            ''' <returns>The <see cref="XDataObject" /> with the given key in the factory's cache, null if the object is not present.</returns>
            Private Function RetrieveObject(ByVal key As String) As XDataObject
                If objectDictionary.ContainsKey(key) Then
                    Return objectDictionary(key)
                End If

                Return Nothing
            End Function

            ''' <summary>
            ''' Extracts a UUID declaration from a data object body if present.
            ''' </summary>
            ''' <param name="body">The current unparsed .x file body of the data object</param>
            ''' <returns>The UUID declaration if found, null otherwise.</returns>
            ''' <remarks>For template objects, be sure to parse the template restrictions
            ''' before trying to extract the UUID, so that any UUID references found in the
            ''' template restrictions don't get picked up by this method.</remarks>
            Private Function ExtractUuid(ByRef body As String) As String
                Dim uuid As String

                Try
                    uuid = ExtractByRegex(body, uuidDeclarationRegex, _
                        Function(uuidGroups) uuidGroups("uuid").Value).SingleOrDefault()
                Catch exc As InvalidOperationException
                    Throw New System.IO.InvalidDataException("Each data object may declare only one UUID", exc)
                End Try

                Return uuid
            End Function

            ''' <summary>
            ''' Processes the given text using the given regex. For every match, the text
            ''' corresponding to that match is removed from the input text, and the <paramref name="processGroup"/>
            ''' delegate is invoked to obtain whatever object instance corresponds to the
            ''' matched text.
            ''' </summary>
            ''' <typeparam name="T">The type of object that will be returned for any matched text</typeparam>
            ''' <param name="inputText">The text to parse. On return, this will reference to a new String containing only the text that was not parsed into new objects</param>
            ''' <param name="regex">The regex expression to use to match text</param>
            ''' <param name="processGroup">The delegate invoked for any matching text, and which returns a new object instance corresponding to the matched text</param>
            ''' <returns>An enumeration of the objects created by parsing the text</returns>
            Private Shared Function ExtractByRegex(Of T)(ByRef inputText As String, _
                ByVal regex As Regex, ByVal processGroup As Func(Of GroupCollection, T)) As IEnumerable(Of T)

                Dim bodyBuilder As StringBuilder = Nothing
                Dim dataObjects As List(Of T) = New List(Of T)()
                Dim matchObject As Match = regex.Match(inputText)
                Dim indexCopy As Integer = 0

                While matchObject.Success

                    ' Deferring creation of the StringBuilder has a couple
                    ' of beneficial effects: the buffer can be pre-sized to
                    ' a likely reasonable size more importantly, the code can
                    ' avoid making a copy of the original input if no sub-objects
                    ' were found.
                    If bodyBuilder Is Nothing Then
                        bodyBuilder = New StringBuilder(inputText.Length - matchObject.Length)
                    End If

                    bodyBuilder.Append(inputText.Substring(indexCopy, matchObject.Index - indexCopy))
                    indexCopy = matchObject.Index + matchObject.Length

                    dataObjects.Add(processGroup(matchObject.Groups))

                    matchObject = matchObject.NextMatch()
                End While

                If bodyBuilder IsNot Nothing Then
                    bodyBuilder.Append(inputText.Substring(indexCopy))

                    inputText = bodyBuilder.ToString()
                End If

                Return dataObjects
            End Function
        End Class
    End Class
End Namespace