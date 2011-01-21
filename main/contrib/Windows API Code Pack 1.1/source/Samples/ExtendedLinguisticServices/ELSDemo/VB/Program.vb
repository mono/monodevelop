' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports Microsoft.WindowsAPICodePack.ExtendedLinguisticServices
Imports System.Threading
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.IO

Namespace ELSSamples

	Public Class Program

		Public Shared Sub Main(ByVal args() As String)
            If MappingService.IsPlatformSupported <> True Then
                Console.WriteLine("This demo requires to be run on Windows 7")
                Return
            End If
            UsageSamples()
            Console.Write("Press any key to continue . . .")
            Console.ReadKey()
        End Sub

		Public Shared Sub UsageSamples()
'            
'            Getting LAD, calling RecognizeText, exam the result with formatter, deal with exceptions, and cleanup.
'            Getting SD, calling RecongizeText, exam each data ranges with formatter, and cleanup.
'            Getting all services of transliteration, output their descriptions to a console.
'            Getting the transliteration service which supports Cyrillic to Latin, and deal with exceptions.
'            Async version for #2. The code should deal with exceptions.
'            
			LADUsageSample()
			SDUsageSample()
			TransliterationEnumSample()
			CyrlToLatinTransUsageSample1()
			CyrlToLatinTransUsageSample2()
			SDUsageSampleAsync()
		End Sub

		Public Shared Sub LADUsageSample()
			Try
				Dim languageDetection As New MappingService(MappingAvailableServices.LanguageDetection)
				Using bag As MappingPropertyBag = languageDetection.RecognizeText("This is English", Nothing)
					Dim languages() As String = bag.GetResultRanges()(0).FormatData(New StringArrayFormatter())
					For Each language As String In languages
						Console.WriteLine("Recognized language: {0}", language)
					Next language
				End Using
			Catch exc As LinguisticException
				Console.WriteLine("Error calling ELS: {0}, HResult: {1}", exc.ResultState.ErrorMessage, exc.ResultState.HResult)
			End Try
		End Sub

		Public Shared Sub SDUsageSample()
			Try
				Dim scriptDetection As New MappingService(MappingAvailableServices.ScriptDetection)
				Using bag As MappingPropertyBag = scriptDetection.RecognizeText("This is English. АБВГД.", Nothing)
					Dim ranges() As MappingDataRange = bag.GetResultRanges()
					Console.WriteLine("Recognized {0} script ranges", ranges.Length)
					Dim formatter As New NullTerminatedStringFormatter()
					For Each range As MappingDataRange In ranges
						Console.WriteLine("Range from {0} to {1}, script {2}", range.StartIndex, range.EndIndex, range.FormatData(formatter))
					Next range
				End Using
			Catch exc As LinguisticException
				Console.WriteLine("Error calling ELS: {0}, HResult: {1}", exc.ResultState.ErrorMessage, exc.ResultState.HResult)
			End Try
		End Sub

		Public Shared Sub TransliterationEnumSample()
			Try
				Dim enumOptions As New MappingEnumOptions()
				enumOptions.Category = "Transliteration"
				Dim transliterationServices() As MappingService = MappingService.GetServices(enumOptions)
				For Each service As MappingService In transliterationServices
					Console.WriteLine("Service: {0}", service.Description)
				Next service
			Catch exc As LinguisticException
				Console.WriteLine("Error calling ELS: {0}, HResult: {1}", exc.ResultState.ErrorMessage, exc.ResultState.HResult)
			End Try
		End Sub

		Public Shared Sub CyrlToLatinTransUsageSample1()
			Try
				Dim cyrlToLatin As New MappingService(MappingAvailableServices.TransliterationCyrillicToLatin)
				Using bag As MappingPropertyBag = cyrlToLatin.RecognizeText("АБВГД.", Nothing)
					Dim transliterated As String = bag.GetResultRanges()(0).FormatData(New StringFormatter())
					Console.WriteLine("Transliterated text: {0}", transliterated)
				End Using
			Catch exc As LinguisticException
				Console.WriteLine("Error calling ELS: {0}, HResult: {1}", exc.ResultState.ErrorMessage, exc.ResultState.HResult)
			End Try
		End Sub

		Public Shared Sub CyrlToLatinTransUsageSample2()
			Try
				Dim enumOptions As New MappingEnumOptions()
				enumOptions.InputScript = "Cyrl"
				enumOptions.OutputScript = "Latn"
				enumOptions.Category = "Transliteration"
				Dim cyrlToLatin() As MappingService = MappingService.GetServices(enumOptions)
				Using bag As MappingPropertyBag = cyrlToLatin(0).RecognizeText("АБВГД.", Nothing)
					Dim transliterated As String = bag.GetResultRanges()(0).FormatData(New StringFormatter())
					Console.WriteLine("Transliterated text: {0}", transliterated)
				End Using
			Catch exc As LinguisticException
				Console.WriteLine("Error calling ELS: {0}, HResult: {1}", exc.ResultState.ErrorMessage, exc.ResultState.HResult)
			End Try
		End Sub

		Public Shared Sub SDSampleCallback(ByVal iAsyncResult As IAsyncResult)
			Dim asyncResult As MappingRecognizeAsyncResult = CType(iAsyncResult, MappingRecognizeAsyncResult)
			If asyncResult.Succeeded Then
				Try
					Dim ranges() As MappingDataRange = asyncResult.PropertyBag.GetResultRanges()
					Console.WriteLine("Recognized {0} script ranges", ranges.Length)
					Dim formatter As New NullTerminatedStringFormatter()
					For Each range As MappingDataRange In ranges
						Console.WriteLine("Range from {0} to {1}, script {2}, text ""{3}""", range.StartIndex, range.EndIndex, range.FormatData(formatter), asyncResult.Text.Substring(CInt(Fix(range.StartIndex)), CInt(Fix(range.EndIndex - range.StartIndex + 1))))
					Next range
				Finally
					asyncResult.PropertyBag.Dispose()
				End Try
			Else
				Console.WriteLine("Error calling ELS: {0}, HResult: {1}", asyncResult.ResultState.ErrorMessage, asyncResult.ResultState.HResult)
			End If
		End Sub

		Public Shared Sub SDUsageSampleAsync()
			Try
				Dim scriptDetection As New MappingService(MappingAvailableServices.ScriptDetection)
				Dim asyncResult As MappingRecognizeAsyncResult = scriptDetection.BeginRecognizeText("This is English. АБВГД.", Nothing, AddressOf SDSampleCallback, Nothing)
				MappingService.EndRecognizeText(asyncResult)
			Catch exc As LinguisticException
				Console.WriteLine("Error calling ELS: {0}, HResult: {1}", exc.ResultState.ErrorMessage, exc.ResultState.HResult)
			End Try
		End Sub

	End Class
End Namespace
