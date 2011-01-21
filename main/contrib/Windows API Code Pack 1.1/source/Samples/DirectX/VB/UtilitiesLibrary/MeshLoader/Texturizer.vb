' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports System.IO

Imports Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
Imports Microsoft.WindowsAPICodePack.DirectX
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports System.Windows.Media.Media3D


Namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
	''' <summary>
    ''' A Mesh that allows for changing textures within the scene
	''' </summary>
	Public Class Texturizer
		Inherits XMesh
		''' <summary>
        ''' If true shows one texture at a time
		''' </summary>
		Public Property ShowOneTexture() As Boolean
			Get
				Return showOneTexture_Renamed
			End Get
			Set(ByVal value As Boolean)
				showOneTexture_Renamed = value
			End Set
		End Property
        Private showOneTexture_Renamed As Boolean = True

		''' <summary>
		''' This method sets which part to texture during rendering.
		''' </summary>
		''' <param name="partName"></param>
        Public Sub PartToTexture(ByVal partName As String)
            If String.IsNullOrEmpty(partName) Then
                Throw New ArgumentException("Must be a non-empty string", "partName")
            End If

            partEmphasis.Clear()

            For Each part In rootParts
                If BuildEmphasisDictionary(part, partName, False) Then
                    Exit For
                End If
            Next part
        End Sub

        Private partEmphasis As New HashSet(Of Part)

        ''' <summary>
        ''' Builds a dictionary of parts to be emphasized if displaying wireframe.
        ''' </summary>
        ''' <remarks>
        ''' During rendering, as the mesh tree is traversed each part is checked to
        ''' see whether it should be displayed as wireframe or not. A part in the dictionary
        ''' built by this method will be rendered as a solid part, otherwise the part
        ''' will be rendered as wireframe.
        ''' This method traverses the mesh tree looking for the named part. Once that
        ''' named part is found, that part and all its children are added to the dictionary.
        ''' The traversal is terminated once the named part has been found and all of its
        ''' children have also been traversed (and added to the dictionary).
        ''' </remarks>
        ''' <param name="part">The current part to inspect</param>
        ''' <param name="partName">The name of the root part to emphasize during rendering</param>
        ''' <param name="fEmphasizeParent">True if the parent of this part will be emphasized, false otherwise</param>
        ''' <returns>True if this part has been emphasized, false otherwise</returns>
        Private Function BuildEmphasisDictionary(ByVal part As Part, ByVal partName As String, ByVal fEmphasizeParent As Boolean) As Boolean
            If fEmphasizeParent OrElse (Not String.IsNullOrEmpty(part.name) AndAlso part.name = partName) Then
                partEmphasis.Add(part)
                fEmphasizeParent = True
            End If

            For Each childPart In part.parts
                If BuildEmphasisDictionary(childPart, partName, fEmphasizeParent) And Not fEmphasizeParent Then
                    Exit For
                End If
            Next childPart

            Return fEmphasizeParent
        End Function

        ''' <summary>
        ''' Clears the alternate texture list (restoring the model's textures)
        ''' </summary>
        Public Sub RevertTextures()
            alternateTextures.Clear()
        End Sub

        ''' <summary>
        ''' Gets a list of the names of the parts in the mesh
        ''' </summary>
        ''' <returns></returns>
        Public Function GetParts() As List(Of String)
            Dim partNames As New List(Of String)()

            If rootParts IsNot Nothing Then
                For Each part In rootParts
                    GetParts(part, partNames)
                Next
            End If

            Return partNames
        End Function

        Private Sub GetParts(ByVal part As Part, ByVal names As List(Of String))
            If Not String.IsNullOrEmpty(part.name) Then
                names.Add(part.name)
            End If

            For Each childPart In part.parts
                GetParts(childPart, names)
            Next childPart
        End Sub


        ''' <summary>
        ''' Creates an alternate texture for a part
        ''' </summary>
        ''' <param name="partName">The name of the part to create the texture for.</param>
        ''' <param name="imagePath">The path to the image to be used for the texture.</param>
        Public Sub SwapTexture(ByVal partName As String, ByVal imagePath As String)
            If partName IsNot Nothing Then
                If File.Exists(imagePath) Then
                    Dim stream As FileStream = File.OpenRead(imagePath)

                    Try
                        Dim srv As ShaderResourceView = TextureLoader.LoadTexture(Me.manager.device, stream)
                        If srv IsNot Nothing Then
                            alternateTextures(partName) = srv
                        End If
                    Catch e1 As COMException
                        System.Windows.MessageBox.Show("Not a valid image.")
                    End Try

                Else
                    alternateTextures(partName) = Nothing
                End If
            End If
        End Sub
        Private alternateTextures As New Dictionary(Of String, ShaderResourceView)()

        Private solidRasterizerState As RasterizerState
        Private wireframeRasterizerState As RasterizerState
        Private currentRasterizerState As RasterizerState

        Friend Overrides Function UpdateRasterizerStateForPart(ByVal part As Part) As ShaderResourceView
            Dim state As RasterizerState = _
                If(ShowOneTexture And Not partEmphasis.Contains(part), wireframeRasterizerState, solidRasterizerState)

            If state IsNot currentRasterizerState Then
                currentRasterizerState = state
                Me.manager.device.RS.State = state
            End If

            Dim textureOverride As ShaderResourceView

            textureOverride = Nothing
            If Not alternateTextures.TryGetValue(part.name, textureOverride) Then
                textureOverride = Nothing
            End If

            Return textureOverride
        End Function

        ''' <summary>
        ''' Renders the mesh with the specified transformation. This alternate render method
        ''' supplements the base class rendering to provide part-by-part texturing support.
        ''' </summary>
        ''' <param name="modelTransform"></param>
        Public Overloads Sub Render(ByVal modelTransform As Matrix3D)
            ' setup rasterization
            Dim rasterizerDesc As New RasterizerDescription With _
            { _
                .FillMode = FillMode.Solid, _
                .CullMode = CullMode.Back, _
                .FrontCounterclockwise = False, _
                .DepthBias = 0, _
                .DepthBiasClamp = 0, _
                .SlopeScaledDepthBias = 0, _
                .DepthClipEnable = True, _
                .ScissorEnable = False, _
                .MultisampleEnable = True, _
                .AntiAliasedLineEnable = True _
            }

            Try
                solidRasterizerState = Me.manager.device.CreateRasterizerState(rasterizerDesc)

                rasterizerDesc.FillMode = FillMode.Wireframe
                wireframeRasterizerState = Me.manager.device.CreateRasterizerState(rasterizerDesc)

                MyBase.Render(modelTransform.ToMatrix4x4F())
            Finally
                If solidRasterizerState IsNot Nothing Then
                    solidRasterizerState.Dispose()
                    solidRasterizerState = Nothing
                End If

                If wireframeRasterizerState IsNot Nothing Then
                    wireframeRasterizerState.Dispose()
                    wireframeRasterizerState = Nothing
                End If

                currentRasterizerState = Nothing
            End Try
        End Sub
    End Class
End Namespace
