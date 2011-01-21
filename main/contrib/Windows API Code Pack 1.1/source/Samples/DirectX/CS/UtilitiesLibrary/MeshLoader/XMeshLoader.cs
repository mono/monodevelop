// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.Direct3DX10;
using Microsoft.WindowsAPICodePack.DirectX.Graphics;


namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
{
    /// <summary>
    /// The format of each XMesh vertex
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XMeshVertex
    {
        /// <summary>
        /// The vertex location
        /// </summary>
        [MarshalAs(UnmanagedType.Struct)]
        public Vector4F Vertex;

        /// <summary>
        /// The vertex normal
        /// </summary>
        [MarshalAs(UnmanagedType.Struct)]
        public Vector4F Normal;

        /// <summary>
        /// The vertex color
        /// </summary>
        [MarshalAs(UnmanagedType.Struct)]
        public Vector4F Color;

        /// <summary>
        /// The texture coordinates (U,V)
        /// </summary>
        [MarshalAs(UnmanagedType.Struct)]
        public Vector2F Texture;
    };
    
    /// <summary>
    /// A part is a piece of a scene
    /// </summary>
    internal struct Part
    {
        /// <summary>
        /// The name of the part
        /// </summary>
        public string name;

        /// <summary>
        /// A description of the part data format
        /// </summary>
        public InputElementDescription[] dataDescription;

        /// <summary>
        /// The vertex buffer for the part
        /// </summary>
        public D3DBuffer vertexBuffer;

        /// <summary>
        /// The number of vertices in the vertex buffer
        /// </summary>
        public int vertexCount;
        
        /// <summary>
        /// The part texture/material
        /// </summary>
        public Material material;

        /// <summary>
        /// The parts that are sub-parts of this part
        /// </summary>
        public List<Part> parts;

        /// <summary>
        /// The transformation to be applied to this part relative to the parent
        /// </summary>
        public Matrix4x4F partTransform;
    }

    internal class Material
    {
        /// <summary>
        /// The difuse color of the material
        /// </summary>
        public Vector4F materialColor;

        /// <summary>
        /// The exponent of the specular color
        /// </summary>
        public float specularPower;

        /// <summary>
        /// The specualr color
        /// </summary>
        public Vector3F specularColor;

        /// <summary>
        /// The emissive color
        /// </summary>
        public Vector3F emissiveColor;

        /// <summary>
        /// The part texture
        /// </summary>
        public ShaderResourceView textureResource;
    }


    /// <summary>
    /// Specifies how a particular mesh should be shaded
    /// </summary>
    internal struct MaterialSpecification
    {
        /// <summary>
        /// The difuse color of the material
        /// </summary>
        public Vector4F materialColor;

        /// <summary>
        /// The exponent of the specular color
        /// </summary>
        public float specularPower;

        /// <summary>
        /// The specualr color
        /// </summary>
        public Vector3F specularColor;

        /// <summary>
        /// The emissive color
        /// </summary>
        public Vector3F emissiveColor;

        /// <summary>
        /// The name of the texture file
        /// </summary>
        public string textureFileName;
    }

    /// <summary>
    /// Loads a text formated .X file
    /// </summary>
    internal partial class XMeshTextLoader
    {
        #region Input element descriptions

        static InputElementDescription[] description = new InputElementDescription[]
        {
            new InputElementDescription()
            {
                SemanticName = "POSITION",
                SemanticIndex = 0,
                Format = Format.R32G32B32A32Float,
                InputSlot = 0,
                AlignedByteOffset = 0,
                InputSlotClass = InputClassification.PerVertexData,
                InstanceDataStepRate = 0,
            },
            new InputElementDescription()
            {
                SemanticName = "NORMAL",
                SemanticIndex = 0,
                Format = Format.R32G32B32A32Float,
                InputSlot = 0,
                AlignedByteOffset = 16,
                InputSlotClass = InputClassification.PerVertexData,
                InstanceDataStepRate = 0,
            },
            new InputElementDescription()
            {
                SemanticName = "COLOR",
                SemanticIndex = 0,
                Format = Format.R32G32B32A32Float,
                InputSlot = 0,
                AlignedByteOffset = 32,
                InputSlotClass = InputClassification.PerVertexData,
                InstanceDataStepRate = 0
            },
            new InputElementDescription()
            {
                SemanticName = "TEXCOORD",
                SemanticIndex = 0,
                Format = Format.R32G32Float,
                InputSlot = 0,
                AlignedByteOffset = 48,
                InputSlotClass = InputClassification.PerVertexData,
                InstanceDataStepRate = 0,
            }

        };

        #endregion // Input element descriptions

        private D3DDevice device;
        private string meshDirectory = "";

        /// <summary>
        /// Constructor that associates a device with the resulting mesh
        /// </summary>
        /// <param name="device"></param>
        public XMeshTextLoader(D3DDevice device)
        {
            this.device = device;
        }

        /// <summary>
        /// Loads the mesh from the file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<Part> XMeshFromFile(string path)
        {
            string meshPath = null;

            if(File.Exists(path))
            {
                meshPath = path;
            }
            else
            {
                string sdkMediaPath = GetDXSDKMediaPath() + path;
                if (File.Exists(sdkMediaPath))
                    meshPath = sdkMediaPath;
            }

            if(meshPath == null)
                throw new System.IO.FileNotFoundException("Could not find mesh file.");
            else
                meshDirectory = Path.GetDirectoryName(meshPath);

            string data = null;
            using (StreamReader xFile = File.OpenText(meshPath))
            {
                string header = xFile.ReadLine();
                ValidateHeader(header);
                data = xFile.ReadToEnd();
            }

            return ExtractRootParts(data);
        }

        /// <summary>
        /// Returns the path to the DX SDK dir
        /// </summary>
        /// <returns></returns>
        private string GetDXSDKMediaPath()
        {
            return Environment.GetEnvironmentVariable("DXSDK_DIR");
        }

        /// <summary>
        /// Validates the header of the .X file. Enforces the text-only requirement of this code.
        /// </summary>
        /// <param name="xFile"></param>
        private void ValidateHeader(string fileHeader)
        {
            Regex headerParse = new Regex(@"xof (?<vermajor>\d\d)(?<verminor>\d\d)(?<format>\w\w\w[\w\s])(?<floatsize>\d\d\d\d)");
            Match m = headerParse.Match(fileHeader);

            if (!m.Success)
            {
                throw new System.IO.InvalidDataException("Invalid .X file.");
            }

            if (m.Groups.Count != 5)
            {
                // None of the capture groups are optional, so a successful match
                // should always have 5 capture groups
                throw new System.IO.InvalidDataException("Invalid .X file.");
            }

            if (m.Groups["vermajor"].ToString() != "03")                     // version 3.x supported
                throw new System.IO.InvalidDataException("Unknown .X file version.");

            if (m.Groups["format"].ToString() != "txt ")
                throw new System.IO.InvalidDataException("Only text .X files are supported.");
        }

        /// <summary>
        /// Parses the root scene of the .X file 
        /// </summary>
        /// <param name="data"></param>
        private IEnumerable<Part> ExtractRootParts(string data)
        {
            return XDataObjectFactory.ExtractDataObjects(ref data)
                .Where(obj => obj.IsVisualObject)
                .Select(obj => PartFromDataObject(obj))
                .ToList();
        }

        private Part PartFromDataObject(IXDataObject dataObject)
        {
            Part part = new Part();

            part.parts = new List<Part>();

            part.name = dataObject.Name;

            switch (dataObject.DataObjectType)
            {
                case "Frame":
                    // Frame data objects translate to parts with only a transform,
                    // and no vertices, materials, etc.
                    part.partTransform = ExtractFrameTransformation(dataObject);
                    foreach (IXDataObject childObject in dataObject.Children.Where(obj => obj.IsVisualObject))
                    {
                        part.parts.Add(PartFromDataObject(childObject));
                    }
                    break;
                case "Mesh":
                    // Mesh data objects inherit transform from their parent,
                    // but do have vertices, materials, etc.
                    part.partTransform = Matrix4x4F.Identity;
                    part.dataDescription = description;
                    LoadMesh(ref part, dataObject);
                    break;
                default:
                    throw new ArgumentException(
                        string.Format(CultureInfo.InvariantCulture,
                        "Object type \"{0}\" is incorrect. Only Frame or Mesh data objects can be converted to Part instances",
                        dataObject.DataObjectType));
            }

            return part;
        }

        /// <summary>
        /// Extracts the transformation associated with the current frame
        /// </summary>
        /// <param name="dataFile"></param>
        /// <param name="dataOffset"></param>
        /// <returns></returns>
        private Matrix4x4F ExtractFrameTransformation(IXDataObject dataObject)
        {
            IXDataObject matrixObject = GetSingleChild(dataObject, "FrameTransformMatrix");

            if (matrixObject == null)
            {
                return Matrix4x4F.Identity;
            }

            string rawMatrixData = matrixObject.Body;

            Regex matrixData = new Regex(@"([-\d\.,\s]+);;");
            Match data = matrixData.Match(rawMatrixData);
            if(!data.Success)
                throw new System.IO.InvalidDataException("Error parsing frame transformation.");

            string[] values = data.Groups[1].ToString().Split(new char[] { ',' });
            if(values.Length != 16)
                throw new System.IO.InvalidDataException("Error parsing frame transformation.");
            float[] fvalues = new float[16];
            for(int n = 0; n < 16; n++)
            {
                fvalues[n] = float.Parse(values[n], CultureInfo.InvariantCulture);
            }

            return new Matrix4x4F(fvalues);
        }


        Regex findArrayCount = new Regex(@"([\d]+);");
        Regex findVector4F = new Regex(@"([-\d]+\.[\d]+);([-\d]+\.[\d]+);([-\d]+\.[\d]+);([-\d]+\.[\d]+);");
        Regex findVector3F = new Regex(@"([-\d]+\.[\d]+);([-\d]+\.[\d]+);([-\d]+\.[\d]+);");
        Regex findVector2F = new Regex(@"([-\d]+\.[\d]+);([-\d]+\.[\d]+);");
        Regex findScalarF = new Regex(@"([-\d]+\.[\d]+);");


        /// <summary>
        /// Loads the first material for a mesh
        /// </summary>
        /// <param name="meshMAterialData"></param>
        /// <returns></returns>
        List<MaterialSpecification> LoadMeshMaterialList(IXDataObject dataObject)
        {
            var materials = from child in dataObject.Children
                            where child.DataObjectType == "Material"
                            select LoadMeshMaterial(child);

            return new List<MaterialSpecification>(materials);
        }

        /// <summary>
        /// Loads a MeshMaterial subresource
        /// </summary>
        /// <param name="materialData"></param>
        /// <returns></returns>
        MaterialSpecification LoadMeshMaterial(IXDataObject dataObject)
        {
            MaterialSpecification m = new MaterialSpecification();
            int dataOffset = 0;
            Match color = findVector4F.Match(dataObject.Body, dataOffset);
            if(!color.Success)
                throw new System.IO.InvalidDataException("problem reading material color");
            m.materialColor.X = float.Parse(color.Groups[1].ToString(), CultureInfo.InvariantCulture);
            m.materialColor.Y = float.Parse(color.Groups[2].ToString(), CultureInfo.InvariantCulture);
            m.materialColor.Z = float.Parse(color.Groups[3].ToString(), CultureInfo.InvariantCulture);
            m.materialColor.W = float.Parse(color.Groups[4].ToString(), CultureInfo.InvariantCulture);
            dataOffset = color.Index + color.Length;

            Match power = findScalarF.Match(dataObject.Body, dataOffset);
            if(!power.Success)
                throw new System.IO.InvalidDataException("problem reading material specular color exponent");
            m.specularPower = float.Parse(power.Groups[1].ToString(), CultureInfo.InvariantCulture);
            dataOffset = power.Index + power.Length;

            Match specular = findVector3F.Match(dataObject.Body, dataOffset);
            if(!specular.Success)
                throw new System.IO.InvalidDataException("problem reading material specular color");
            m.specularColor.X = float.Parse(specular.Groups[1].ToString(), CultureInfo.InvariantCulture);
            m.specularColor.Y = float.Parse(specular.Groups[2].ToString(), CultureInfo.InvariantCulture);
            m.specularColor.Z = float.Parse(specular.Groups[3].ToString(), CultureInfo.InvariantCulture);
            dataOffset = specular.Index + specular.Length;

            Match emissive = findVector3F.Match(dataObject.Body, dataOffset);
            if(!emissive.Success)
                throw new System.IO.InvalidDataException("problem reading material emissive color");
            m.emissiveColor.X = float.Parse(emissive.Groups[1].ToString(), CultureInfo.InvariantCulture);
            m.emissiveColor.Y = float.Parse(emissive.Groups[2].ToString(), CultureInfo.InvariantCulture);
            m.emissiveColor.Z = float.Parse(emissive.Groups[3].ToString(), CultureInfo.InvariantCulture);
            dataOffset = emissive.Index + emissive.Length;

            IXDataObject filenameObject = GetSingleChild(dataObject, "TextureFilename");

            if (filenameObject != null)
            {
                Regex findFilename = new Regex(@"[\s]+""([\\\w\.]+)"";");
                Match filename = findFilename.Match(filenameObject.Body);
                if (!filename.Success)
                    throw new System.IO.InvalidDataException("problem reading texture filename");
                m.textureFileName = filename.Groups[1].ToString();
            }

            return m;
        }

        internal class IndexedMeshNormals
        {
            public List<Vector4F> normalVectors;
            public List<Int32> normalIndexMap;
        }

        /// <summary>
        /// Loads the indexed normal vectors for a mesh
        /// </summary>
        /// <param name="meshNormalData"></param>
        /// <returns></returns>
        IndexedMeshNormals LoadMeshNormals(IXDataObject dataObject)
        {
            IndexedMeshNormals indexedMeshNormals = new IndexedMeshNormals();

            Match normalCount = findArrayCount.Match(dataObject.Body);
            if(!normalCount.Success)
                throw new System.IO.InvalidDataException("problem reading mesh normals count");

            indexedMeshNormals.normalVectors = new List<Vector4F>();
            int normals = int.Parse(normalCount.Groups[1].Value, CultureInfo.InvariantCulture);
            int dataOffset = normalCount.Index + normalCount.Length;
            for(int normalIndex = 0; normalIndex < normals; normalIndex++)
            {
                Match normal = findVector3F.Match(dataObject.Body, dataOffset);
                if(!normal.Success)
                    throw new System.IO.InvalidDataException("problem reading mesh normal vector");
                else
                    dataOffset = normal.Index + normal.Length;

                indexedMeshNormals.normalVectors.Add(
                    new Vector4F(
                        float.Parse(normal.Groups[1].Value, CultureInfo.InvariantCulture),
                        float.Parse(normal.Groups[2].Value, CultureInfo.InvariantCulture),
                        float.Parse(normal.Groups[3].Value, CultureInfo.InvariantCulture),
                        1.0f));
            }

            Match faceNormalCount = findArrayCount.Match(dataObject.Body, dataOffset);
            if(!faceNormalCount.Success)
                throw new System.IO.InvalidDataException("problem reading mesh normals count");
            
            indexedMeshNormals.normalIndexMap = new List<Int32>();
            int faceCount = int.Parse(faceNormalCount.Groups[1].Value, CultureInfo.InvariantCulture);
            dataOffset = faceNormalCount.Index + faceNormalCount.Length;
            for(int faceNormalIndex = 0; faceNormalIndex < faceCount; faceNormalIndex++)
            {
                Match normalFace = findVertexIndex.Match(dataObject.Body, dataOffset);
                if(!normalFace.Success)
                    throw new System.IO.InvalidDataException("problem reading mesh normal face");
                else
                    dataOffset = normalFace.Index + normalFace.Length;

                string[] vertexIndexes = normalFace.Groups[2].Value.Split(new char[] { ',' });

                for(int n = 0; n <= vertexIndexes.Length - 3; n ++)
                {
                    indexedMeshNormals.normalIndexMap.Add(int.Parse(vertexIndexes[0], CultureInfo.InvariantCulture));
                    indexedMeshNormals.normalIndexMap.Add(int.Parse(vertexIndexes[1 + n], CultureInfo.InvariantCulture));
                    indexedMeshNormals.normalIndexMap.Add(int.Parse(vertexIndexes[2 + n], CultureInfo.InvariantCulture));
                }
            }

            return indexedMeshNormals;
        }

        /// <summary>
        /// Loads the per vertex color for a mesh
        /// </summary>
        /// <param name="vertexColorData"></param>
        /// <returns></returns>
        Dictionary<int, Vector4F> LoadMeshColors(IXDataObject dataObject)
        {
            Regex findVertexColor = new Regex(@"([\d]+); ([\d]+\.[\d]+);([\d]+\.[\d]+);([\d]+\.[\d]+);([\d]+\.[\d]+);;");

            Match vertexCount = findArrayCount.Match(dataObject.Body);
            if(!vertexCount.Success)
                throw new System.IO.InvalidDataException("problem reading vertex colors count");

            Dictionary<int, Vector4F> colorDictionary = new Dictionary<int,Vector4F>();
            int verticies = int.Parse(vertexCount.Groups[1].Value, CultureInfo.InvariantCulture);
            int dataOffset = vertexCount.Index + vertexCount.Length;
            for(int vertexIndex = 0; vertexIndex < verticies; vertexIndex++)
            {
                Match vertexColor = findVertexColor.Match(dataObject.Body, dataOffset);
                if(!vertexColor.Success)
                    throw new System.IO.InvalidDataException("problem reading vertex colors");
                else
                    dataOffset = vertexColor.Index + vertexColor.Length;

                colorDictionary[int.Parse(vertexColor.Groups[1].Value, CultureInfo.InvariantCulture)] =
                    new Vector4F(
                        float.Parse(vertexColor.Groups[2].Value, CultureInfo.InvariantCulture),
                        float.Parse(vertexColor.Groups[3].Value, CultureInfo.InvariantCulture),
                        float.Parse(vertexColor.Groups[4].Value, CultureInfo.InvariantCulture),
                        float.Parse(vertexColor.Groups[5].Value, CultureInfo.InvariantCulture));
            }

            return colorDictionary;
        }

        /// <summary>
        /// Loads the texture coordinates(U,V) for a mesh
        /// </summary>
        /// <param name="textureCoordinateData"></param>
        /// <returns></returns>
        List<Vector2F> LoadMeshTextureCoordinates(IXDataObject dataObject)
        {
            Match coordinateCount = findArrayCount.Match(dataObject.Body);
            if(!coordinateCount.Success)
                throw new System.IO.InvalidDataException("problem reading mesh texture coordinates count");

            List<Vector2F> textureCoordinates = new List<Vector2F>();
            int coordinates = int.Parse(coordinateCount.Groups[1].Value, CultureInfo.InvariantCulture);
            int dataOffset = coordinateCount.Index + coordinateCount.Length;
            for(int coordinateIndex = 0; coordinateIndex < coordinates; coordinateIndex++)
            {
                Match coordinate = findVector2F.Match(dataObject.Body, dataOffset);
                if(!coordinate.Success)
                    throw new System.IO.InvalidDataException("problem reading texture coordinate count");
                else
                    dataOffset = coordinate.Index + coordinate.Length;

                textureCoordinates.Add(
                    new Vector2F(
                        float.Parse(coordinate.Groups[1].Value, CultureInfo.InvariantCulture),
                        float.Parse(coordinate.Groups[2].Value, CultureInfo.InvariantCulture)));
            }

            return textureCoordinates;
        }

        Regex findVertexIndex = new Regex(@"([\d]+);[\s]*([\d,]+)?;");

        /// <summary>
        /// Loads a mesh and creates the vertex/index buffers for the part
        /// </summary>
        /// <param name="part"></param>
        /// <param name="meshData"></param>
        void LoadMesh(ref Part part, IXDataObject dataObject)
        {
            // load vertex data
            int dataOffset = 0;
            Match vertexCount = findArrayCount.Match(dataObject.Body);
            if(!vertexCount.Success)
                throw new System.IO.InvalidDataException("problem reading vertex count");

            List<Vector4F> vertexList = new List<Vector4F>();
            int verticies = int.Parse(vertexCount.Groups[1].Value, CultureInfo.InvariantCulture);
            dataOffset = vertexCount.Index + vertexCount.Length;
            for(int vertexIndex = 0; vertexIndex < verticies; vertexIndex++)
            {
                Match vertex = findVector3F.Match(dataObject.Body, dataOffset);
                if(!vertex.Success)
                    throw new System.IO.InvalidDataException("problem reading vertex");
                else
                    dataOffset = vertex.Index + vertex.Length;

                vertexList.Add(
                    new Vector4F(
                        float.Parse(vertex.Groups[1].Value, CultureInfo.InvariantCulture),
                        float.Parse(vertex.Groups[2].Value, CultureInfo.InvariantCulture),
                        float.Parse(vertex.Groups[3].Value, CultureInfo.InvariantCulture),
                        1.0f));
            }

            // load triangle index data
            Match triangleIndexCount = findArrayCount.Match(dataObject.Body, dataOffset);
            dataOffset = triangleIndexCount.Index + triangleIndexCount.Length;
            if(!triangleIndexCount.Success)
                throw new System.IO.InvalidDataException("problem reading index count");

            List<Int32> triangleIndiciesList = new List<Int32>();
            int triangleIndexListCount = int.Parse(triangleIndexCount.Groups[1].Value, CultureInfo.InvariantCulture);
            dataOffset = triangleIndexCount.Index + triangleIndexCount.Length;
            for(int triangleIndicyIndex = 0; triangleIndicyIndex < triangleIndexListCount; triangleIndicyIndex++)
            {
                Match indexEntry = findVertexIndex.Match(dataObject.Body, dataOffset);
                if(!indexEntry.Success)
                    throw new System.IO.InvalidDataException("problem reading vertex index entry");
                else
                    dataOffset = indexEntry.Index + indexEntry.Length;

                int indexEntryCount = int.Parse(indexEntry.Groups[1].Value, CultureInfo.InvariantCulture);
                string[] vertexIndexes = indexEntry.Groups[2].Value.Split(new char[] { ',' });
                if(indexEntryCount != vertexIndexes.Length)
                    throw new System.IO.InvalidDataException("vertex index count does not equal count of indicies found");

                for(int entryIndex = 0; entryIndex <= indexEntryCount - 3; entryIndex++)
                {
                    triangleIndiciesList.Add(int.Parse(vertexIndexes[0], CultureInfo.InvariantCulture));
                    triangleIndiciesList.Add(int.Parse(vertexIndexes[1 + entryIndex].ToString(), CultureInfo.InvariantCulture));
                    triangleIndiciesList.Add(int.Parse(vertexIndexes[2 + entryIndex].ToString(), CultureInfo.InvariantCulture));
                }
            }

            // load mesh colors
            IXDataObject vertexColorData = GetSingleChild(dataObject, "MeshVertexColors");
            Dictionary<int, Vector4F> colorDictionary = null;
            if (vertexColorData != null)
                colorDictionary = LoadMeshColors(vertexColorData);

            // load mesh normals
            IXDataObject meshNormalData = GetSingleChild(dataObject, "MeshNormals");
            IndexedMeshNormals meshNormals = null;
            if(meshNormalData != null)
            {
                meshNormals = LoadMeshNormals(meshNormalData);
            }

            // load mesh texture coordinates
            IXDataObject meshTextureCoordsData = GetSingleChild(dataObject, "MeshTextureCoords");
            List<Vector2F> meshTextureCoords = null;
            if(meshTextureCoordsData != null)
            {
                meshTextureCoords = LoadMeshTextureCoordinates(meshTextureCoordsData);
            }

            // load mesh material
            IXDataObject meshMaterialsData = GetSingleChild(dataObject, "MeshMaterialList");
            List<MaterialSpecification> meshMaterials = null;
            if(meshMaterialsData != null)
            {
                meshMaterials = LoadMeshMaterialList(meshMaterialsData);
            }
            
            // copy vertex data to HGLOBAL
            int byteLength = Marshal.SizeOf(typeof(XMeshVertex)) * triangleIndiciesList.Count;
            IntPtr nativeVertex = Marshal.AllocHGlobal(byteLength);
            byte[] byteBuffer = new byte[byteLength];
            XMeshVertex[] varray = new XMeshVertex[triangleIndiciesList.Count];
            for(int n = 0; n < triangleIndiciesList.Count; n++)
            {
                XMeshVertex vertex = new XMeshVertex()
                {
                    Vertex = vertexList[triangleIndiciesList[n]],
                    Normal = (meshNormals == null) ? new Vector4F(0, 0, 0, 1.0f) : meshNormals.normalVectors[meshNormals.normalIndexMap[n]],
                    Color = ((colorDictionary == null) ? new Vector4F(0, 0, 0, 0) : colorDictionary[triangleIndiciesList[n]]),
                    Texture = ((meshTextureCoords == null) ? new Vector2F(0, 0) : meshTextureCoords[triangleIndiciesList[n]])
                };
                byte[] vertexData = RawSerialize(vertex);
                Buffer.BlockCopy(vertexData, 0, byteBuffer, vertexData.Length * n, vertexData.Length);
            }
            Marshal.Copy(byteBuffer, 0, nativeVertex, byteLength);

            // build vertex buffer
            BufferDescription bdv = new BufferDescription()
            {
                Usage = Usage.Default,
                ByteWidth = (uint)(Marshal.SizeOf(typeof(XMeshVertex)) * triangleIndiciesList.Count),
                BindingOptions = BindingOptions.VertexBuffer,
                CpuAccessOptions = CpuAccessOptions.None,
                MiscellaneousResourceOptions = MiscellaneousResourceOptions.None
            };
            SubresourceData vertexInit = new SubresourceData()
            {
                SystemMemory = nativeVertex
            };

            part.vertexBuffer = device.CreateBuffer(bdv, vertexInit);
            Debug.Assert(part.vertexBuffer != null);


            part.vertexCount = triangleIndiciesList.Count;

            if(meshMaterials != null)
            {
                // only a single material is currently supported
                MaterialSpecification m = meshMaterials[0];

                part.material = new Material()
                {
                    emissiveColor = m.emissiveColor,
                    specularColor = m.specularColor,
                    materialColor = m.materialColor,
                    specularPower = m.specularPower
                };
                
                string texturePath = "";
                if(File.Exists(m.textureFileName))
                    texturePath = m.textureFileName;
                if(File.Exists(meshDirectory + "\\" + m.textureFileName))
                    texturePath = meshDirectory + "\\" + m.textureFileName;
                if(File.Exists(meshDirectory + "\\..\\" + m.textureFileName))
                    texturePath = meshDirectory + "\\..\\" + m.textureFileName;

                if(texturePath.Length == 0)
                {
                    part.material.textureResource = null;
                }
                else
                {
                    part.material.textureResource =
                        D3D10XHelpers.CreateShaderResourceViewFromFile(
                            device,
                            texturePath);
                }
            }
            Marshal.FreeHGlobal(nativeVertex);
        }

        /// <summary>
        /// Copies an arbitrary structure into a byte array
        /// </summary>
        /// <param name="anything"></param>
        /// <returns></returns>
        public byte[] RawSerialize(object anything)
        {
            int rawsize = Marshal.SizeOf(anything);
            IntPtr buffer = Marshal.AllocHGlobal(rawsize);

            try
            {
                Marshal.StructureToPtr(anything, buffer, false);
                byte[] rawdatas = new byte[rawsize];
                Marshal.Copy(buffer, rawdatas, 0, rawsize);
                return rawdatas;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        } 
    }
}
