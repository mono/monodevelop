'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Linq
Imports Microsoft.WindowsAPICodePack.Shell
Imports System.Collections.Generic
Imports System.IO
Imports Microsoft.WindowsAPICodePack.Shell.PropertySystem

Namespace Microsoft.WindowsAPICodePack.Shell.Samples
    Public Class PropertyEdit
        Shared Sub Main(ByVal args() As String)
            Dim app As New PropertyEdit()
            Try
                app.DoAction(args)
            Catch ex As Exception
                Console.WriteLine("Exception: " & ex.Message)
                If (Not ex.InnerException Is Nothing) Then
                    Console.WriteLine("Inner exception: " & ex.InnerException.Message)
                End If
            End Try

            If Diagnostics.Debugger.IsAttached Then
                Console.WriteLine()
                Console.Write("Press any key to exit...")
                Console.ReadKey(True)
            End If
        End Sub

        Private Sub DoAction(ByVal args() As String)
            If args.Length = 0 OrElse args(0).Contains("?") Then
                Usage()
                Return
            End If

            If args(0).Equals("-get", StringComparison.InvariantCultureIgnoreCase) Then
                If args.Length <> 3 Then
                    Usage()
                    Return
                End If
                Dim propertyName As String = args(1)
                Dim fileName As String = Path.GetFullPath(args(2))

                Dim prop As IShellProperty = ShellObject.FromParsingName(fileName).Properties.GetProperty(propertyName)

                DisplayPropertyValue(prop)

            ElseIf args(0).Equals("-set", StringComparison.InvariantCultureIgnoreCase) Then
                If args.Length <> 4 Then
                    Usage()
                    Return
                End If
                Dim propertyName As String = args(1)
                Dim value As String = args(2)
                Dim fileName As String = Path.GetFullPath(args(3))


                Dim prop As IShellProperty = ShellObject.FromParsingName(fileName).Properties.GetProperty(propertyName)
                SetPropertyValue(value, prop)

            ElseIf args(0).Equals("-info", StringComparison.InvariantCultureIgnoreCase) Then
                If args.Length <> 2 Then
                    Usage()
                    Return
                End If
                Dim propertyName As String = args(1)

                Dim propDesc As ShellPropertyDescription = SystemProperties.GetPropertyDescription(propertyName)
                ShowPropertyInfo(propertyName, propDesc)

            ElseIf args(0).Equals("-enum", StringComparison.InvariantCultureIgnoreCase) Then
                Dim fileName As String = Nothing
                Dim filter As String = Nothing
                If args.Length < 2 Then
                    Usage()
                    Return
                End If
                If args.Length > 2 Then
                    filter = args(1)
                    fileName = Path.GetFullPath(args(2))
                Else
                    fileName = Path.GetFullPath(args(1))
                End If

                EnumProperties(fileName, filter)

            Else
                Usage()
                Return
            End If
        End Sub

        Private Shared Sub DisplayPropertyValue(ByVal prop As IShellProperty)
            Dim value As String = String.Empty
            value = If(prop.ValueAsObject Is Nothing, "", prop.FormatForDisplay(PropertyDescriptionFormatOptions.None))

            Console.WriteLine("{0} = {1}", prop.CanonicalName, value)
        End Sub

        Private Shared Sub EnumProperties(ByVal fileName As String, ByVal filter As String)
            Dim collection As New ShellPropertyCollection(fileName)
            Dim properties = collection.Where(Function(prop) prop.CanonicalName IsNot Nothing AndAlso (If(filter Is Nothing, True, prop.CanonicalName.StartsWith(filter, StringComparison.CurrentCultureIgnoreCase)))).ToArray()
            Array.ForEach(properties, Function(p) AnonymousMethod1(p))
        End Sub

        Private Shared Function AnonymousMethod1(ByVal p As IShellProperty) As IShellProperty
            DisplayPropertyValue(p)
            Return Nothing
        End Function

        Private Shared Sub ShowPropertyInfo(ByVal propertyName As String, ByVal propDesc As ShellPropertyDescription)
            Console.WriteLine(Constants.vbLf & "Property {0}", propertyName)
            Console.WriteLine(Constants.vbTab & "PropertyKey: {0}, {1}", propDesc.PropertyKey.FormatId.ToString("B"), propDesc.PropertyKey.PropertyId)
            Console.WriteLine(Constants.vbTab & "Label:  {0}", propDesc.DisplayName)
            Console.WriteLine(Constants.vbTab & "Edit Invitation:  {0}", propDesc.EditInvitation)
            Console.WriteLine(Constants.vbTab & "Display Type:  {0}", propDesc.DisplayType)
            Console.WriteLine(Constants.vbTab & "Var Enum Type:  {0}", propDesc.VarEnumType)
            Console.WriteLine(Constants.vbTab & "Value Type:  {0}", propDesc.ValueType)
            Console.WriteLine(Constants.vbTab & "Default Column Width:  {0}", propDesc.DefaultColumWidth)
            Console.WriteLine(Constants.vbTab & "Aggregation Type:  {0}", propDesc.AggregationTypes)
            Console.WriteLine(Constants.vbTab & "Has Multiple Values:  {0}", (propDesc.TypeFlags And PropertyTypeOptions.MultipleValues) = PropertyTypeOptions.MultipleValues)
            Console.WriteLine(Constants.vbTab & "Is Group:  {0}", (propDesc.TypeFlags And PropertyTypeOptions.IsGroup) = PropertyTypeOptions.IsGroup)
            Console.WriteLine(Constants.vbTab & "Is Innate:  {0}", (propDesc.TypeFlags And PropertyTypeOptions.IsInnate) = PropertyTypeOptions.IsInnate)
            Console.WriteLine(Constants.vbTab & "Is Queryable:  {0}", (propDesc.TypeFlags And PropertyTypeOptions.IsQueryable) = PropertyTypeOptions.IsQueryable)
            Console.WriteLine(Constants.vbTab & "Is Viewable:  {0}", (propDesc.TypeFlags And PropertyTypeOptions.IsViewable) = PropertyTypeOptions.IsViewable)
            Console.WriteLine(Constants.vbTab & "Is SystemProperty:  {0}", (propDesc.TypeFlags And PropertyTypeOptions.IsSystemProperty) = PropertyTypeOptions.IsSystemProperty)
        End Sub

        Private Shared Sub SetPropertyValue(ByVal value As String, ByVal prop As IShellProperty)
            If prop.ValueType Is GetType(String()) Then
                Dim values() As String = value.Split(New Char() {";"c}, StringSplitOptions.RemoveEmptyEntries)
                TryCast(prop, ShellProperty(Of String())).Value = values
            End If
            If prop.ValueType Is GetType(String) Then
                TryCast(prop, ShellProperty(Of String)).Value = value
            ElseIf prop.ValueType Is GetType(UShort?) Then
                TryCast(prop, ShellProperty(Of UShort?)).Value = UShort.Parse(value)
            ElseIf prop.ValueType Is GetType(Short?) Then
                TryCast(prop, ShellProperty(Of Short?)).Value = Short.Parse(value)
            ElseIf prop.ValueType Is GetType(UInteger?) Then
                TryCast(prop, ShellProperty(Of UInteger?)).Value = UInteger.Parse(value)
            ElseIf prop.ValueType Is GetType(Integer?) Then
                TryCast(prop, ShellProperty(Of Integer?)).Value = Integer.Parse(value)
            ElseIf prop.ValueType Is GetType(ULong?) Then
                TryCast(prop, ShellProperty(Of ULong?)).Value = ULong.Parse(value)
            ElseIf prop.ValueType Is GetType(Long?) Then
                TryCast(prop, ShellProperty(Of Long?)).Value = Long.Parse(value)
            ElseIf prop.ValueType Is GetType(DateTime?) Then
                TryCast(prop, ShellProperty(Of DateTime?)).Value = DateTime.Parse(value)
            ElseIf prop.ValueType Is GetType(Double?) Then
                TryCast(prop, ShellProperty(Of Double?)).Value = Double.Parse(value)
            End If
        End Sub

        Private Sub Usage()
            Console.WriteLine("Usage: PropertyEdit.exe <OPTIONS> Filename ")
            Console.WriteLine("")
            Console.WriteLine("OPTIONS:")
            Console.WriteLine(" -get <PropertyName>   Get the value for the property defined")
            Console.WriteLine("                       by its Canonical Name in <propertyName>")
            Console.WriteLine(" -set <PropertyName>   Set the value for the property defined")
            Console.WriteLine("      <PropertyValue>	 by <PropertyName> with value <PropertyValue>")
            Console.WriteLine(" -enum  [Filter]  Enumerate all the properties for this file.")
            Console.WriteLine("                  filtering (starting with) Filter value.")
            Console.WriteLine(" -info <PropertyName>  Get schema information on property for this file.")
            Console.WriteLine("")
            Console.WriteLine("Examples:")
            Console.WriteLine("PropertyEdit -get System.Author foo.jpg")
            Console.WriteLine("PropertyEdit -set System.Author ""Jane Smith;John Smith"" foo.docx")
            Console.WriteLine("PropertyEdit -set System.Photo.MeteringMode 2 foo.jpg")
            Console.WriteLine("PropertyEdit -set System.Photo.DateTaken ""3/11/2009 12:03:02"" foo.jpg")
            Console.WriteLine("PropertyEdit -enum foo.jpg")
            Console.WriteLine("PropertyEdit -enum System.Photo foo.jpg")
            Console.WriteLine("PropertyEdit -info System.Author foo.docx")
        End Sub


    End Class
End Namespace
