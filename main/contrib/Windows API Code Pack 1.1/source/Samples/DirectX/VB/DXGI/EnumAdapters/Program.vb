' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports Microsoft.WindowsAPICodePack.DirectX.Graphics
Imports System
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D

Namespace EnumAdapters
	Friend Class Program
		Shared Sub Main(ByVal args() As String)
            Dim factory As Factory1 = Factory1.Create()

			Console.WriteLine("Adapter(s) Information:")
            For Each adapter As Adapter1 In factory.Adapters
                Dim description As AdapterDescription = adapter.Description
                Dim version? As AdapterDriverVersion

                Console.WriteLine("Description: {0} ", description.Description)
                Console.WriteLine(Constants.vbTab & "Dedicated System Memory: {0} ", description.DedicatedSystemMemory)
                Console.WriteLine(Constants.vbTab & "Dedicated Video Memory: {0} ", description.DedicatedVideoMemory)
                Console.WriteLine(Constants.vbTab & "Luid: {0:X}:{1:X} ", description.AdapterLuid.HighPart, description.AdapterLuid.LowPart)
                Console.WriteLine(Constants.vbTab & "Device Id: {0:X} ", description.DeviceId)
                Console.WriteLine(Constants.vbTab & "Revision: {0:X} ", description.Revision)

                Console.WriteLine()
                version = adapter.CheckDeviceSupport(DeviceType.Direct3D11)
                Console.WriteLine(Constants.vbTab & "Supports Direct3D 11.0 Device: {0}", version IsNot Nothing)
                version = adapter.CheckDeviceSupport(DeviceType.Direct3D10Point1)
                Console.WriteLine(Constants.vbTab & "Supports Direct3D 10.1 Device: {0}", version IsNot Nothing)
                version = adapter.CheckDeviceSupport(DeviceType.Direct3D10)
                Console.WriteLine(Constants.vbTab & "Supports Direct3D 10.0 Device: {0}", version IsNot Nothing)
                Console.WriteLine()

                Console.WriteLine(Constants.vbTab & "Monitor(s) Information:")
                For Each output As Output In adapter.Outputs
                    Dim outDesc As OutputDescription = output.Description

                    Console.WriteLine(Constants.vbTab & "Device Name: {0} ", outDesc.DeviceName)
                    Console.WriteLine(Constants.vbTab + Constants.vbTab & "Attached To Desktop: {0} ", outDesc.AttachedToDesktop)
                    Console.WriteLine(Constants.vbTab + Constants.vbTab & "Rotation Mode: {0} ", outDesc.Rotation)
                    Console.WriteLine(Constants.vbTab + Constants.vbTab & "Monitor Coordinates: Top: {0}, Left: {1}, Right: {2}, Bottom: {3} ", outDesc.Monitor.MonitorCoordinates.Top, outDesc.Monitor.MonitorCoordinates.Left, outDesc.Monitor.MonitorCoordinates.Right, outDesc.Monitor.MonitorCoordinates.Bottom)
                    Console.WriteLine(Constants.vbTab + Constants.vbTab & "Working Coordinates: Top: {0}, left: {1}, Right: {2}, Bottom: {3} ", outDesc.Monitor.WorkCoordinates.Top, outDesc.Monitor.WorkCoordinates.Left, outDesc.Monitor.WorkCoordinates.Right, outDesc.Monitor.WorkCoordinates.Bottom)
                Next output
            Next adapter

            Console.WriteLine("Press any key to continue...")
            Console.ReadKey()

		End Sub
	End Class
End Namespace
