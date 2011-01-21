' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1

Namespace D2DShapes
	Friend Class MeshShape
		Inherits DrawingShape
'INSTANT VB NOTE: The variable mesh was renamed since Visual Basic does not allow class members with the same name:
		Private mesh_Renamed As Mesh
		Private triangles As List(Of Triangle)
		Private geometry As GeometryShape

		Friend Overrides Property Bitmap() As D2DBitmap
			Get
				Return MyBase.Bitmap
			End Get
			Set(ByVal value As D2DBitmap)
				MyBase.Bitmap = value
				If geometry IsNot Nothing Then
					geometry.Bitmap = value
				End If
			End Set
		End Property

		Public Sub New(ByVal initialRenderTarget As RenderTarget, ByVal random As Random, ByVal d2DFactory As D2DFactory, ByVal bitmap As D2DBitmap)
			MyBase.New(initialRenderTarget, random, d2DFactory, bitmap)
			FillBrush = RandomBrush()

			mesh_Renamed = If(CoinFlip, MeshFromRandomGeometry(), MeshFromRandomTriangles())
		End Sub

		<TypeConverter(GetType(ExpandableObjectConverter))> _
		Public Property Mesh() As Mesh
			Get
				Return mesh_Renamed
			End Get
			Set(ByVal value As Mesh)
				mesh_Renamed = value
			End Set
		End Property

		Private Function MeshFromRandomTriangles() As Mesh
			Dim m As Mesh = RenderTarget.CreateMesh()
            Using sink As TessellationSink = m.Open()
                Dim count As Integer = Random.Next(2, 20)
                triangles = New List(Of Triangle)()
                CreateRandomTriangles(count)

                sink.AddTriangles(triangles)
                sink.Close()
            End Using
            Return m
        End Function

		Private Sub CreateRandomTriangles(ByVal count As Integer)
			Dim which = Random.NextDouble()
			If which < 0.33 Then 'random triangles
				For i As Integer = 0 To count - 1
					triangles.Add(New Triangle(RandomNearPoint(), RandomNearPoint(), RandomNearPoint()))
				Next i
			ElseIf which < 0.67 Then 'fan of triangles
				Dim p1, p2, p3 As Point2F
				p1 = RandomPoint()
				p3 = RandomNearPoint()
				For i As Integer = 0 To count - 1
					p2 = p3
					p3 = RandomNearPoint()
					triangles.Add(New Triangle(p1, p2, p3))
				Next i
			Else 'triangle strip
				Dim p1, p2, p3 As Point2F
				p2 = RandomPoint()
				p3 = RandomNearPoint()
				For i As Integer = 0 To count - 1
					p1 = p2
					p2 = p3
					p3 = RandomNearPoint()
					triangles.Add(New Triangle(p1, p2, p3))
				Next i
			End If
		End Sub

		Private Function MeshFromRandomGeometry() As Mesh
			If geometry IsNot Nothing Then
				geometry.Dispose()
			End If
			geometry = New GeometryShape(RenderTarget, Random, d2DFactory, Bitmap)
			Dim m As Mesh = RenderTarget.CreateMesh()
			Dim sink As TessellationSink = m.Open()
			If geometry.worldTransform.HasValue Then
                geometry.Geometry.Tessellate(sink, FlatteningTolerance, geometry.worldTransform.Value)
			Else
                geometry.Geometry.Tessellate(sink, FlatteningTolerance)
			End If
			sink.Close()
			sink.Dispose()
			Return m
		End Function

		Protected Friend Overrides Sub ChangeRenderTarget(ByVal newRenderTarget As RenderTarget)
			Dim sink As TessellationSink
			mesh_Renamed.Dispose()
			mesh_Renamed = newRenderTarget.CreateMesh()
			If geometry IsNot Nothing Then
				geometry.RenderTarget = newRenderTarget
				sink = mesh_Renamed.Open()
				If geometry.worldTransform.HasValue Then
                    geometry.Geometry.Tessellate(sink, FlatteningTolerance, geometry.worldTransform.Value)
				Else
                    geometry.Geometry.Tessellate(sink, FlatteningTolerance)
				End If
			Else
				sink = mesh_Renamed.Open()
				sink.AddTriangles(triangles)
			End If
			sink.Close()
			sink.Dispose()
			FillBrush = CopyBrushToRenderTarget(FillBrush, newRenderTarget)
		End Sub

		Protected Friend Overrides Sub Draw(ByVal renderTarget As RenderTarget)
			Dim stateBlock As DrawingStateBlock = d2DFactory.CreateDrawingStateBlock()
			renderTarget.SaveDrawingState(stateBlock)
			'AntialiasMode push = RenderTarget.AntialiasMode;
			renderTarget.AntialiasMode = AntialiasMode.Aliased
			renderTarget.FillMesh(mesh_Renamed, FillBrush)
			'RenderTarget.AntialiasMode = push;
			renderTarget.RestoreDrawingState(stateBlock)
			stateBlock.Dispose()
		End Sub

		Public Overrides Function HitTest(ByVal point As Point2F) As Boolean
			If geometry IsNot Nothing Then
				Return geometry.HitTest(point)
			End If
			If triangles IsNot Nothing Then
				For Each triangle In triangles
					If IsPointInTriangle(triangle, point) Then
						Return True
					End If
				Next triangle
			End If
			Return False
		End Function

		Private Shared Function IsPointInTriangle(ByVal triangle As Triangle, ByVal point As Point2F) As Boolean
			'no time to implement the proper algorithm, so let's just use a bounding rectangle...
			Dim left As Single = Math.Min(triangle.Point1.X, Math.Min(triangle.Point2.X, triangle.Point3.X))
			Dim right As Single = Math.Max(triangle.Point1.X, Math.Max(triangle.Point2.X, triangle.Point3.X))
			Dim top As Single = Math.Min(triangle.Point1.Y, Math.Min(triangle.Point2.Y, triangle.Point3.Y))
			Dim bottom As Single = Math.Max(triangle.Point1.Y, Math.Max(triangle.Point2.Y, triangle.Point3.Y))
			Return point.X >= left AndAlso point.X <= right AndAlso point.Y >= top AndAlso point.Y <= bottom
		End Function

		Public Overrides Sub Dispose()
			If geometry IsNot Nothing Then
				geometry.Dispose()
			End If
			geometry = Nothing
			If mesh_Renamed IsNot Nothing Then
				mesh_Renamed.Dispose()
			End If
			mesh_Renamed = Nothing
			MyBase.Dispose()
		End Sub
	End Class
End Namespace
