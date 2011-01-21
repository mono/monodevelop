' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.IO
Imports System.Linq
Imports System.Runtime.InteropServices
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports System.Diagnostics
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3DX10


Namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
	''' <summary>
	''' The format of each XMesh vertex
	''' </summary>
	<StructLayout(LayoutKind.Sequential)> _
	Public Structure XMeshVertex
		''' <summary>
		''' The vertex location
		''' </summary>
		<MarshalAs(UnmanagedType.Struct)> _
		Public Vertex As Vector4F

		''' <summary>
		''' The vertex normal
		''' </summary>
		<MarshalAs(UnmanagedType.Struct)> _
		Public Normal As Vector4F

		''' <summary>
		''' The vertex color
		''' </summary>
		<MarshalAs(UnmanagedType.Struct)> _
		Public Color As Vector4F

		''' <summary>
		''' The texture coordinates (U,V)
		''' </summary>
		<MarshalAs(UnmanagedType.Struct)> _
		Public Texture As Vector2F
	End Structure

	''' <summary>
	''' A part is a piece of a scene
	''' </summary>
	Friend Structure Part
		''' <summary>
		''' The name of the part
		''' </summary>
		Public name As String

		''' <summary>
		''' A description of the part data format
		''' </summary>
		Public dataDescription() As InputElementDescription

		''' <summary>
		''' The vertex buffer for the part
		''' </summary>
		Public vertexBuffer As D3DBuffer

		''' <summary>
        ''' The number of vertices in the vertex buffer
        ''' </summary>
		Public vertexCount As Integer

		''' <summary>
		''' The part texture/material
		''' </summary>
		Public material As Material

        ''' <summary>
        ''' The parts that are sub-parts of this part
        ''' </summary>
        Public parts As List(Of Part)

        ''' <summary>
        ''' The transformation to be applied to this part relative to the scene
        ''' </summary>
		Public partTransform As Matrix4x4F
	End Structure

    Friend Class Material
        ''' <summary>
        ''' The difuse color of the material
        ''' </summary>
        Public materialColor As Vector4F

        ''' <summary>
        ''' The exponent of the specular color
        ''' </summary>
        Public specularPower As Single

        ''' <summary>
        ''' The specualr color
        ''' </summary>
        Public specularColor As Vector3F

        ''' <summary>
        ''' The emissive color
        ''' </summary>
        Public emissiveColor As Vector3F

        ''' <summary>
        ''' The part texture
        ''' </summary>
        Public textureResource As ShaderResourceView
    End Class


	''' <summary>
	''' Specifies how a particular mesh should be shaded
	''' </summary>
	Friend Structure MaterialSpecification
		''' <summary>
		''' The difuse color of the material
		''' </summary>
		Public materialColor As Vector4F

		''' <summary>
		''' The exponent of the specular color
		''' </summary>
		Public specularPower As Single

		''' <summary>
		''' The specualr color
		''' </summary>
		Public specularColor As Vector3F

		''' <summary>
		''' The emissive color
		''' </summary>
		Public emissiveColor As Vector3F

		''' <summary>
		''' The name of the texture file
		''' </summary>
		Public textureFileName As String
	End Structure

	''' <summary>
	''' Loads a text formated .X file
	''' </summary>
    Partial Friend Class XMeshTextLoader

#Region "Input element descriptions"
        Private Shared description() As InputElementDescription = _
        { _
            New InputElementDescription With _
            { _
                .SemanticName = "POSITION", _
                .SemanticIndex = 0, _
                .Format = Format.R32G32B32A32Float, _
                .InputSlot = 0, _
                .AlignedByteOffset = 0, _
                .InputSlotClass = InputClassification.PerVertexData, _
                .InstanceDataStepRate = 0 _
            }, _
            New InputElementDescription With _
            { _
                .SemanticName = "NORMAL", _
                .SemanticIndex = 0, _
                .Format = Format.R32G32B32A32Float, _
                .InputSlot = 0, _
                .AlignedByteOffset = 16, _
                .InputSlotClass = InputClassification.PerVertexData, _
                .InstanceDataStepRate = 0 _
            }, _
            New InputElementDescription With _
            { _
                .SemanticName = "COLOR", _
                .SemanticIndex = 0, _
                .Format = Format.R32G32B32A32Float, _
                .InputSlot = 0, _
                .AlignedByteOffset = 32, _
                .InputSlotClass = InputClassification.PerVertexData, _
                .InstanceDataStepRate = 0 _
                }, _
                New InputElementDescription With _
                { _
                    .SemanticName = "TEXCOORD", _
                    .SemanticIndex = 0, _
                    .Format = Format.R32G32Float, _
                    .InputSlot = 0, _
                    .AlignedByteOffset = 48, _
                    .InputSlotClass = InputClassification.PerVertexData, _
                    .InstanceDataStepRate = 0 _
                } _
            }
#End Region

        Private device As D3DDevice
        Private meshDirectory As String = ""

        ''' <summary>
        ''' Constructor that associates a device with the resulting mesh
        ''' </summary>
        ''' <param name="device"></param>
        Public Sub New(ByVal device As D3DDevice)
            Me.device = device
        End Sub

        ''' <summary>
        ''' Loads the mesh from the file
        ''' </summary>
        ''' <param name="path"></param>
        ''' <returns></returns>
        Public Function XMeshFromFile(ByVal path As String) As IEnumerable(Of Part)
            Dim meshPath As String = Nothing

            Dim xFile As StreamReader
            If File.Exists(path) Then
                meshPath = path
            Else
                Dim sdkMediaPath As String = GetDXSDKMediaPath() & path
                If File.Exists(sdkMediaPath) Then
                    meshPath = sdkMediaPath
                End If
            End If

            If meshPath Is Nothing Then
                Throw New System.IO.FileNotFoundException("Could not find mesh file.")
            Else
                meshDirectory = System.IO.Path.GetDirectoryName(meshPath)
            End If

            xFile = File.OpenText(meshPath)

            ValidateHeader(xFile)

            Dim data As String = xFile.ReadToEnd()
            Return ExtractRootParts(data)
        End Function

        ''' <summary>
        ''' Returns the path to the DX SDK dir
        ''' </summary>
        ''' <returns></returns>
        Private Function GetDXSDKMediaPath() As String
            Return Environment.GetEnvironmentVariable("DXSDK_DIR")
        End Function

        ''' <summary>
        ''' Validates the header of the .X file. Enforces the text-only requirement of this code.
        ''' </summary>
        ''' <param name="xFile"></param>
        Private Sub ValidateHeader(ByVal xFile As StreamReader)
            Dim fileHeader As String = xFile.ReadLine()
            Dim headerParse As New Regex("xof (?<vermajor>\d\d)(?<verminor>\d\d)(?<format>\w\w\w[\w\s])(?<floatsize>\d\d\d\d)")
            Dim m As Match = headerParse.Match(fileHeader)

            If Not m.Success Then
                Throw New System.IO.InvalidDataException("Invalid .X file.")
            End If

            If m.Groups.Count <> 5 Then
                Throw New System.IO.InvalidDataException("Invalid .X file.")
            End If

            If m.Groups("vermajor").ToString() <> "03" Then ' version 3.x supported
                Throw New System.IO.InvalidDataException("Unknown .X file version.")
            End If

            If m.Groups("format").ToString() <> "txt " Then
                Throw New System.IO.InvalidDataException("Only text .X files are supported.")
            End If
        End Sub

        ''' <summary>
        ''' Parses the root scene of the .X file 
        ''' </summary>
        ''' <param name="data"></param>
        Private Function ExtractRootParts(ByVal data As String) As IEnumerable(Of Part)
            Return XDataObjectFactory.ExtractDataObjects(data) _
                .Where(Function(obj) obj.IsVisualObject) _
                .Select(Function(obj) PartFromDataObject(obj)) _
                .ToList()
        End Function

        Private Function PartFromDataObject(ByVal dataObject As IXDataObject) As Part
            Dim part As New Part()

            part.parts = New List(Of Part)()

            part.name = dataObject.Name

            Select Case dataObject.DataObjectType
                Case "Frame"
                    ' Frame data objects translate to parts with only a transform,
                    ' and no vertices, materials, etc.
                    part.partTransform = ExtractFrameTransformation(dataObject)
                    For Each childObject As IXDataObject In dataObject.Children.Where(Function(obj) obj.IsVisualObject)
                        part.parts.Add(PartFromDataObject(childObject))
                    Next childObject
                Case "Mesh"
                    ' Mesh data objects inherit transform from their parent,
                    ' but do have vertices, materials, etc.
                    part.partTransform = Matrix4x4F.Identity
                    part.dataDescription = description
                    LoadMesh(part, dataObject)
                Case Else
                    Throw New ArgumentException( _
                        String.Format(CultureInfo.InvariantCulture, _
                        "Object type ""{0}"" is incorrect. Only Frame or Mesh data objects can be converted to Part instances", _
                        dataObject.DataObjectType))
            End Select

            Return part
        End Function

        ''' <summary>
        ''' Extracts the transformation associated with the current frame
        ''' </summary>
        ''' <param name="dataFile"></param>
        ''' <param name="dataOffset"></param>
        ''' <returns></returns>
        Private Function ExtractFrameTransformation(ByVal dataObject As IXDataObject) As Matrix4x4F
            Dim matrixObject As IXDataObject = GetSingleChild(dataObject, "FrameTransformMatrix")

            If matrixObject Is Nothing Then
                Return Matrix4x4F.Identity
            End If

            Dim rawMatrixData As String = matrixObject.Body

            Dim matrixData As New Regex("([-\d\.,\s]+);;")
            Dim data As Match = matrixData.Match(rawMatrixData)
            If Not data.Success Then
                Throw New System.IO.InvalidDataException("Error parsing frame transformation.")
            End If

            Dim values() As String = data.Groups(1).ToString().Split(New Char() {","c})
            If values.Length <> 16 Then
                Throw New System.IO.InvalidDataException("Error parsing frame transformation.")
            End If
            Dim fvalues(15) As Single
            For n As Integer = 0 To 15
                fvalues(n) = Single.Parse(values(n), CultureInfo.InvariantCulture)
            Next n

            Return New Matrix4x4F(fvalues)
        End Function

        Private findArrayCount As New Regex("([\d]+);")
        Private findVector4F As New Regex("([-\d]+\.[\d]+);([-\d]+\.[\d]+);([-\d]+\.[\d]+);([-\d]+\.[\d]+);")
        Private findVector3F As New Regex("([-\d]+\.[\d]+);([-\d]+\.[\d]+);([-\d]+\.[\d]+);")
        Private findVector2F As New Regex("([-\d]+\.[\d]+);([-\d]+\.[\d]+);")
        Private findScalarF As New Regex("([-\d]+\.[\d]+);")


        ''' <summary>
        ''' Loads the first material for a mesh
        ''' </summary>
        ''' <param name="meshMAterialData"></param>
        ''' <returns></returns>
        Private Function LoadMeshMaterialList(ByVal dataObject As IXDataObject) As List(Of MaterialSpecification)
            Dim materials = From child In dataObject.Children _
                            Where child.DataObjectType = "Material" _
                            Select LoadMeshMaterial(child)

            Return New List(Of MaterialSpecification)(materials)
        End Function

        ''' <summary>
        ''' Loads a MeshMaterial subresource
        ''' </summary>
        ''' <param name="materialData"></param>
        ''' <returns></returns>
        Private Function LoadMeshMaterial(ByVal dataObject As IXDataObject) As MaterialSpecification
            Dim m As New MaterialSpecification()
            Dim dataOffset As Integer = 0
            Dim color As Match = findVector4F.Match(dataObject.Body, dataOffset)
            If Not color.Success Then
                Throw New System.IO.InvalidDataException("problem reading material color")
            End If
            m.materialColor.X = Single.Parse(color.Groups(1).ToString(), CultureInfo.InvariantCulture)
            m.materialColor.Y = Single.Parse(color.Groups(2).ToString(), CultureInfo.InvariantCulture)
            m.materialColor.Z = Single.Parse(color.Groups(3).ToString(), CultureInfo.InvariantCulture)
            m.materialColor.W = Single.Parse(color.Groups(4).ToString(), CultureInfo.InvariantCulture)
            dataOffset = color.Index + color.Length

            Dim power As Match = findScalarF.Match(dataObject.Body, dataOffset)
            If Not power.Success Then
                Throw New System.IO.InvalidDataException("problem reading material specular color exponent")
            End If
            m.specularPower = Single.Parse(power.Groups(1).ToString(), CultureInfo.InvariantCulture)
            dataOffset = power.Index + power.Length

            Dim specular As Match = findVector3F.Match(dataObject.Body, dataOffset)
            If Not specular.Success Then
                Throw New System.IO.InvalidDataException("problem reading material specular color")
            End If
            m.specularColor.X = Single.Parse(specular.Groups(1).ToString(), CultureInfo.InvariantCulture)
            m.specularColor.Y = Single.Parse(specular.Groups(2).ToString(), CultureInfo.InvariantCulture)
            m.specularColor.Z = Single.Parse(specular.Groups(3).ToString(), CultureInfo.InvariantCulture)
            dataOffset = specular.Index + specular.Length

            Dim emissive As Match = findVector3F.Match(dataObject.Body, dataOffset)
            If Not emissive.Success Then
                Throw New System.IO.InvalidDataException("problem reading material emissive color")
            End If
            m.emissiveColor.X = Single.Parse(emissive.Groups(1).ToString(), CultureInfo.InvariantCulture)
            m.emissiveColor.Y = Single.Parse(emissive.Groups(2).ToString(), CultureInfo.InvariantCulture)
            m.emissiveColor.Z = Single.Parse(emissive.Groups(3).ToString(), CultureInfo.InvariantCulture)
            dataOffset = emissive.Index + emissive.Length

            Dim filenameObject As IXDataObject = GetSingleChild(dataObject, "TextureFilename")

            If filenameObject IsNot Nothing Then
                Dim findFilename As New Regex("[\s]+""([\\\w\.]+)"";")
                Dim filename As Match = findFilename.Match(filenameObject.Body)
                If Not filename.Success Then
                    Throw New System.IO.InvalidDataException("problem reading texture filename")
                End If
                m.textureFileName = filename.Groups(1).ToString()
            End If

            Return m
        End Function

        Friend Class IndexedMeshNormals
            Public normalVectors As List(Of Vector4F)
            Public normalIndexMap As List(Of Int32)
        End Class

        ''' <summary>
        ''' Loads the indexed normal vectors for a mesh
        ''' </summary>
        ''' <param name="meshNormalData"></param>
        ''' <returns></returns>
        Private Function LoadMeshNormals(ByVal dataObject As IXDataObject) As IndexedMeshNormals
            Dim indexedMeshNormals As New IndexedMeshNormals()

            Dim normalCount As Match = findArrayCount.Match(dataObject.Body)
            If Not normalCount.Success Then
                Throw New System.IO.InvalidDataException("problem reading mesh normals count")
            End If

            indexedMeshNormals.normalVectors = New List(Of Vector4F)()
            Dim normals As Integer = Integer.Parse(normalCount.Groups(1).Value, CultureInfo.InvariantCulture)
            Dim dataOffset As Integer = normalCount.Index + normalCount.Length
            For normalIndex As Integer = 0 To normals - 1
                Dim normal As Match = findVector3F.Match(dataObject.Body, dataOffset)
                If Not normal.Success Then
                    Throw New System.IO.InvalidDataException("problem reading mesh normal vector")
                Else
                    dataOffset = normal.Index + normal.Length
                End If

                indexedMeshNormals.normalVectors.Add( _
                    New Vector4F( _
                        Single.Parse(normal.Groups(1).Value, CultureInfo.InvariantCulture), _
                        Single.Parse(normal.Groups(2).Value, CultureInfo.InvariantCulture), _
                        Single.Parse(normal.Groups(3).Value, CultureInfo.InvariantCulture), 1.0F))
            Next normalIndex

            Dim faceNormalCount As Match = findArrayCount.Match(dataObject.Body, dataOffset)
            If Not faceNormalCount.Success Then
                Throw New System.IO.InvalidDataException("problem reading mesh normals count")
            End If

            indexedMeshNormals.normalIndexMap = New List(Of Int32)()
            Dim faceCount As Integer = Integer.Parse(faceNormalCount.Groups(1).Value, CultureInfo.InvariantCulture)
            dataOffset = faceNormalCount.Index + faceNormalCount.Length
            For faceNormalIndex As Integer = 0 To faceCount - 1
                Dim normalFace As Match = findVertexIndex.Match(dataObject.Body, dataOffset)
                If Not normalFace.Success Then
                    Throw New System.IO.InvalidDataException("problem reading mesh normal face")
                Else
                    dataOffset = normalFace.Index + normalFace.Length
                End If

                Dim vertexIndexes() As String = normalFace.Groups(2).Value.Split(New Char() {","c})

                For n As Integer = 0 To vertexIndexes.Length - 3
                    indexedMeshNormals.normalIndexMap.Add(Integer.Parse(vertexIndexes(0), CultureInfo.InvariantCulture))
                    indexedMeshNormals.normalIndexMap.Add(Integer.Parse(vertexIndexes(1 + n), CultureInfo.InvariantCulture))
                    indexedMeshNormals.normalIndexMap.Add(Integer.Parse(vertexIndexes(2 + n), CultureInfo.InvariantCulture))
                Next n
            Next faceNormalIndex

            Return indexedMeshNormals
        End Function

        ''' <summary>
        ''' Loads the per vertex color for a mesh
        ''' </summary>
        ''' <param name="vertexColorData"></param>
        ''' <returns></returns>
        Private Function LoadMeshColors(ByVal dataObject As IXDataObject) As Dictionary(Of Integer, Vector4F)
            Dim findVertexColor As New Regex("([\d]+); ([\d]+\.[\d]+);([\d]+\.[\d]+);([\d]+\.[\d]+);([\d]+\.[\d]+);;")

            Dim vertexCount As Match = findArrayCount.Match(dataObject.Body)
            If Not vertexCount.Success Then
                Throw New System.IO.InvalidDataException("problem reading vertex colors count")
            End If

            Dim colorDictionary As New Dictionary(Of Integer, Vector4F)()
            Dim vertices As Integer = Integer.Parse(vertexCount.Groups(1).Value, CultureInfo.InvariantCulture)
            Dim dataOffset As Integer = vertexCount.Index + vertexCount.Length
            For vertexIndex As Integer = 0 To vertices - 1
                Dim vertexColor As Match = findVertexColor.Match(dataObject.Body, dataOffset)
                If Not vertexColor.Success Then
                    Throw New System.IO.InvalidDataException("problem reading vertex colors")
                Else
                    dataOffset = vertexColor.Index + vertexColor.Length
                End If

                colorDictionary(Integer.Parse(vertexColor.Groups(1).Value, CultureInfo.InvariantCulture)) = _
                    New Vector4F( _
                        Single.Parse(vertexColor.Groups(2).Value, CultureInfo.InvariantCulture), _
                        Single.Parse(vertexColor.Groups(3).Value, CultureInfo.InvariantCulture), _
                        Single.Parse(vertexColor.Groups(4).Value, CultureInfo.InvariantCulture), _
                        Single.Parse(vertexColor.Groups(5).Value, CultureInfo.InvariantCulture))
            Next vertexIndex

            Return colorDictionary
        End Function

        ''' <summary>
        ''' Loads the texture coordinates(U,V) for a mesh
        ''' </summary>
        ''' <param name="textureCoordinateData"></param>
        ''' <returns></returns>
        Private Function LoadMeshTextureCoordinates(ByVal dataObject As IXDataObject) As List(Of Vector2F)
            Dim coordinateCount As Match = findArrayCount.Match(dataObject.Body)
            If Not coordinateCount.Success Then
                Throw New System.IO.InvalidDataException("problem reading mesh texture coordinates count")
            End If

            Dim textureCoordinates As New List(Of Vector2F)()
            Dim coordinates As Integer = Integer.Parse(coordinateCount.Groups(1).Value, CultureInfo.InvariantCulture)
            Dim dataOffset As Integer = coordinateCount.Index + coordinateCount.Length
            For coordinateIndex As Integer = 0 To coordinates - 1
                Dim coordinate As Match = findVector2F.Match(dataObject.Body, dataOffset)
                If Not coordinate.Success Then
                    Throw New System.IO.InvalidDataException("problem reading texture coordinate count")
                Else
                    dataOffset = coordinate.Index + coordinate.Length
                End If

                textureCoordinates.Add(New Vector2F(Single.Parse(coordinate.Groups(1).Value, CultureInfo.InvariantCulture), Single.Parse(coordinate.Groups(2).Value, CultureInfo.InvariantCulture)))
            Next coordinateIndex

            Return textureCoordinates
        End Function

        Private findVertexIndex As New Regex("([\d]+);[\s]*([\d,]+)?;")

        ''' <summary>
        ''' Loads a mesh and creates the vertex/index buffers for the part
        ''' </summary>
        ''' <param name="part"></param>
        ''' <param name="meshData"></param>
        Private Sub LoadMesh(ByRef part As Part, ByVal dataObject As IXDataObject)

            ' load vertex data
            Dim dataOffset As Integer = 0
            Dim vertexCount As Match = findArrayCount.Match(dataObject.Body)
            If Not vertexCount.Success Then
                Throw New System.IO.InvalidDataException("problem reading vertex count")
            End If

            Dim vertexList As New List(Of Vector4F)()
            Dim vertices As Integer = Integer.Parse(vertexCount.Groups(1).Value, CultureInfo.InvariantCulture)
            dataOffset = vertexCount.Index + vertexCount.Length
            For vertexIndex As Integer = 0 To vertices - 1
                Dim vertex As Match = findVector3F.Match(dataObject.Body, dataOffset)
                If Not vertex.Success Then
                    Throw New System.IO.InvalidDataException("problem reading vertex")
                Else
                    dataOffset = vertex.Index + vertex.Length
                End If

                vertexList.Add(New Vector4F(Single.Parse(vertex.Groups(1).Value, CultureInfo.InvariantCulture), Single.Parse(vertex.Groups(2).Value, CultureInfo.InvariantCulture), Single.Parse(vertex.Groups(3).Value, CultureInfo.InvariantCulture), 1.0F))
            Next vertexIndex

            ' load triangle index data
            Dim triangleIndexCount As Match = findArrayCount.Match(dataObject.Body, dataOffset)
            dataOffset = triangleIndexCount.Index + triangleIndexCount.Length
            If Not triangleIndexCount.Success Then
                Throw New System.IO.InvalidDataException("problem reading index count")
            End If

            Dim triangleIndiciesList As New List(Of Int32)()
            Dim triangleIndexListCount As Integer = Integer.Parse(triangleIndexCount.Groups(1).Value, CultureInfo.InvariantCulture)
            dataOffset = triangleIndexCount.Index + triangleIndexCount.Length
            For triangleIndicyIndex As Integer = 0 To triangleIndexListCount - 1
                Dim indexEntry As Match = findVertexIndex.Match(dataObject.Body, dataOffset)
                If Not indexEntry.Success Then
                    Throw New System.IO.InvalidDataException("problem reading vertex index entry")
                Else
                    dataOffset = indexEntry.Index + indexEntry.Length
                End If

                Dim indexEntryCount As Integer = Integer.Parse(indexEntry.Groups(1).Value, CultureInfo.InvariantCulture)
                Dim vertexIndexes() As String = indexEntry.Groups(2).Value.Split(New Char() {","c})
                If indexEntryCount <> vertexIndexes.Length Then
                    Throw New System.IO.InvalidDataException("vertex index count does not equal count of indicies found")
                End If

                For entryIndex As Integer = 0 To indexEntryCount - 3
                    triangleIndiciesList.Add(Integer.Parse(vertexIndexes(0), CultureInfo.InvariantCulture))
                    triangleIndiciesList.Add(Integer.Parse(vertexIndexes(1 + entryIndex).ToString(), CultureInfo.InvariantCulture))
                    triangleIndiciesList.Add(Integer.Parse(vertexIndexes(2 + entryIndex).ToString(), CultureInfo.InvariantCulture))
                Next entryIndex
            Next triangleIndicyIndex

            ' load mesh colors
            Dim vertexColorData As IXDataObject = GetSingleChild(dataObject, "MeshVertexColors")
            Dim colorDictionary As Dictionary(Of Integer, Vector4F) = Nothing
            If vertexColorData IsNot Nothing Then
                colorDictionary = LoadMeshColors(vertexColorData)
            End If

            ' load mesh normals
            Dim meshNormalData As IXDataObject = GetSingleChild(dataObject, "MeshNormals")
            Dim meshNormals As IndexedMeshNormals = Nothing
            If meshNormalData IsNot Nothing Then
                meshNormals = LoadMeshNormals(meshNormalData)
            End If

            ' load mesh texture coordinates
            Dim meshTextureCoordsData As IXDataObject = GetSingleChild(dataObject, "MeshTextureCoords")
            Dim meshTextureCoords As List(Of Vector2F) = Nothing
            If meshTextureCoordsData IsNot Nothing Then
                meshTextureCoords = LoadMeshTextureCoordinates(meshTextureCoordsData)
            End If

            ' load mesh material
            Dim meshMaterialsData As IXDataObject = GetSingleChild(dataObject, "MeshMaterialList")
            Dim meshMaterials As List(Of MaterialSpecification) = Nothing
            If meshMaterialsData IsNot Nothing Then
                meshMaterials = LoadMeshMaterialList(meshMaterialsData)
            End If

            ' copy vertex data to HGLOBAL
            Dim byteLength As Integer = Marshal.SizeOf(GetType(XMeshVertex)) * triangleIndiciesList.Count
            Dim nativeVertex As IntPtr = Marshal.AllocHGlobal(byteLength)
            Dim byteBuffer(byteLength - 1) As Byte
            Dim varray(triangleIndiciesList.Count - 1) As XMeshVertex
            For n As Integer = 0 To triangleIndiciesList.Count - 1
                Dim vertex As New XMeshVertex() With _
                { _
                    .Vertex = vertexList(triangleIndiciesList(n)), _
                    .Normal = If((meshNormals Is Nothing), _
                        New Vector4F(0, 0, 0, 1.0F), _
                        meshNormals.normalVectors(meshNormals.normalIndexMap(n))), _
                    .Color = (If((colorDictionary Is Nothing), _
                        New Vector4F(0, 0, 0, 0), _
                        colorDictionary(triangleIndiciesList(n)))), _
                    .Texture = (If((meshTextureCoords Is Nothing), _
                        New Vector2F(0, 0), _
                        meshTextureCoords(triangleIndiciesList(n)))) _
                }
                Dim vertexData() As Byte = RawSerialize(vertex)
                Buffer.BlockCopy(vertexData, 0, byteBuffer, vertexData.Length * n, vertexData.Length)
            Next n
            Marshal.Copy(byteBuffer, 0, nativeVertex, byteLength)

            ' build vertex buffer
            Dim bdv As New BufferDescription() With {.Usage = Usage.Default, .ByteWidth = CUInt(Marshal.SizeOf(GetType(XMeshVertex)) * triangleIndiciesList.Count), .BindingOptions = BindingOptions.VertexBuffer, .CpuAccessOptions = CpuAccessOptions.None, .MiscellaneousResourceOptions = MiscellaneousResourceOptions.None}
            Dim vertexInit As New SubresourceData() With {.SystemMemory = nativeVertex}

            part.vertexBuffer = device.CreateBuffer(bdv, vertexInit)
            Debug.Assert(part.vertexBuffer IsNot Nothing)

            part.vertexCount = triangleIndiciesList.Count

            If meshMaterials IsNot Nothing Then
                ' only a single material is currently supported
                Dim m As MaterialSpecification = meshMaterials(0)

                part.material = New Material() With {.emissiveColor = m.emissiveColor, .specularColor = m.specularColor, .materialColor = m.materialColor, .specularPower = m.specularPower}

                Dim texturePath As String = ""
                If File.Exists(m.textureFileName) Then
                    texturePath = m.textureFileName
                End If
                If File.Exists(meshDirectory & "\" & m.textureFileName) Then
                    texturePath = meshDirectory & "\" & m.textureFileName
                End If
                If File.Exists(meshDirectory & "\..\" & m.textureFileName) Then
                    texturePath = meshDirectory & "\..\" & m.textureFileName
                End If

                If texturePath.Length = 0 Then
                    part.material.textureResource = Nothing
                Else
                    part.material.textureResource = D3D10XHelpers.CreateShaderResourceViewFromFile(device, texturePath)
                End If
            End If

            Marshal.FreeHGlobal(nativeVertex)
        End Sub

        ''' <summary>
        ''' Copies an arbitrary structure into a byte array
        ''' </summary>
        ''' <param name="anything"></param>
        ''' <returns></returns>
        Public Function RawSerialize(ByVal anything As Object) As Byte()
            Dim rawsize As Integer = Marshal.SizeOf(anything)
            Dim buffer As IntPtr = Marshal.AllocHGlobal(rawsize)

            Try
                Marshal.StructureToPtr(anything, buffer, False)
                Dim rawdatas(rawsize - 1) As Byte
                Marshal.Copy(buffer, rawdatas, 0, rawsize)
                Return rawdatas
            Finally
                Marshal.FreeHGlobal(buffer)
            End Try
        End Function
    End Class
End Namespace
