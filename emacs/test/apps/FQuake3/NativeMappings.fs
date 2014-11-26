(*
Copyright (C) 2013 William F. Smith

This program is free software; you can redistribute it
and/or modify it under the terms of the GNU General Public License as
published by the Free Software Foundation; either version 2 of the License,
or (at your option) any later version.

This program is distributed in the hope that it will be
useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

Derivative of Quake III Arena source:
Copyright (C) 1999-2005 Id Software, Inc.
*)

// Disable native interop warnings
#nowarn "9"
#nowarn "51"

namespace Engine.Native

open System
open System.IO
open System.Runtime.InteropServices
open Microsoft.FSharp.NativeInterop
open FSharp.Game.Math
open Engine.Core
open Engine.Net
open Engine.FileSystem
open Engine.NativeInterop
open FQuake3.Math
open FQuake3.Md3

/// Used to prevent massive copying of large immutable data.
module private Cache =
    let mutable md3Map = Map.empty<nativeint, Md3>

module Boolean =
    let inline ofNativePtr (ptr: nativeptr<qboolean>) =
        let mutable native = NativePtr.read ptr

        match native with
        | qboolean.qtrue -> true
        | _ -> false

    let inline toNativeByPtr (ptr: nativeptr<qboolean>) (value: bool) =
        let mutable native = NativePtr.read ptr

        native <- if value then qboolean.qtrue else qboolean.qfalse

        NativePtr.write ptr native

    let inline toNative (value: bool) =
        if value then qboolean.qtrue else qboolean.qfalse

module Vec2 =
    let inline ofNativePtr (ptr: nativeptr<vec2_t>) =
        let mutable native = NativePtr.read ptr

        vec2 (native.value, native.value1)

    let inline toNativeByPtr (ptr: nativeptr<vec2_t>) (v: vec2) =
        let mutable native = NativePtr.read ptr

        native.value <- v.X
        native.value1 <- v.Y

        NativePtr.write ptr native  

module Vec3 =
    let inline ofNativePtr (ptr: nativeptr<vec3_t>) =
        let mutable native = NativePtr.read ptr

        vec3 (native.value, native.value1, native.value2)

    let inline toNativeByPtr (ptr: nativeptr<vec3_t>) (v: vec3) =
        let mutable native = NativePtr.read ptr

        native.value <- v.X
        native.value1 <- v.Y
        native.value2 <- v.Z

        NativePtr.write ptr native   
        
module Vec4 =
    let inline ofNativePtr (ptr: nativeptr<vec4_t>) =
        let mutable native = NativePtr.read ptr

        vec4 (native.value, native.value1, native.value2, native.value3)

    let inline toNativeByPtr (ptr: nativeptr<vec4_t>) (v: vec4) =
        let mutable native = NativePtr.read ptr

        native.value <- v.X
        native.value1 <- v.Y
        native.value2 <- v.Z
        native.value3 <- v.W

        NativePtr.write ptr native  

module Mat4 =
    let inline ofNativePtr (ptr: nativeptr<single>) =
        mat4 (
            (NativePtr.get ptr 0),
            (NativePtr.get ptr 1),
            (NativePtr.get ptr 2),
            (NativePtr.get ptr 3),
            (NativePtr.get ptr 4),
            (NativePtr.get ptr 5),
            (NativePtr.get ptr 6),
            (NativePtr.get ptr 7),
            (NativePtr.get ptr 8),
            (NativePtr.get ptr 9),
            (NativePtr.get ptr 10),
            (NativePtr.get ptr 11),
            (NativePtr.get ptr 12),
            (NativePtr.get ptr 13),
            (NativePtr.get ptr 14),
            (NativePtr.get ptr 15)
        )

    let inline toNativeByPtr (ptr: nativeptr<single>) (m: mat4) =
        NativePtr.set ptr 0 m.[0, 0]
        NativePtr.set ptr 1 m.[0, 1]
        NativePtr.set ptr 2 m.[0, 2]
        NativePtr.set ptr 3 m.[0, 3]
        NativePtr.set ptr 4 m.[1, 0]
        NativePtr.set ptr 5 m.[1, 1]
        NativePtr.set ptr 6 m.[1, 2]
        NativePtr.set ptr 7 m.[1, 3]
        NativePtr.set ptr 8 m.[2, 0]
        NativePtr.set ptr 9 m.[2, 1]
        NativePtr.set ptr 10 m.[2, 2]
        NativePtr.set ptr 11 m.[2, 3]
        NativePtr.set ptr 12 m.[3, 0]
        NativePtr.set ptr 13 m.[3, 1]
        NativePtr.set ptr 14 m.[3, 2]
        NativePtr.set ptr 15 m.[3, 3]

module Cvar =
    let inline ofNativePtr (ptr: nativeptr<cvar_t>) =
        let mutable native = NativePtr.read ptr

        {
            Name = NativePtr.toStringAnsi native.name;
            String = NativePtr.toStringAnsi native.string;
            ResetString = NativePtr.toStringAnsi native.resetString;
            LatchedString = NativePtr.toStringAnsi native.latchedString;
            Flags = native.flags;
            IsModified = Boolean.ofNativePtr &&native.modified;
            ModificationCount = native.modificationCount;
            Value = native.value;
            Integer = native.integer;
        }

module Bounds =
    let inline ofNativePtr (ptr: nativeptr<vec3_t>) =
        Bounds (
            Vec3.ofNativePtr <| NativePtr.add ptr 0,
            Vec3.ofNativePtr <| NativePtr.add ptr 1)

    let inline toNativeByPtr (ptr: nativeptr<vec3_t>) (bounds: Bounds) =
        let mutable nativeX = NativePtr.get ptr 0
        let mutable nativeY = NativePtr.get ptr 1

        Vec3.toNativeByPtr &&nativeX bounds.Min
        Vec3.toNativeByPtr &&nativeY bounds.Max

        NativePtr.set ptr 0 nativeX
        NativePtr.set ptr 1 nativeY

module Message =
    let inline ofNativePtr (ptr: nativeptr<msg_t>) =
        let mutable native = NativePtr.read ptr

        {
            IsAllowedOverflow = Boolean.ofNativePtr &&native.allowoverflow;
            IsOverflowed = Boolean.ofNativePtr &&native.overflowed;
            IsOutOfBand = Boolean.ofNativePtr &&native.oob;
            Data = Seq.ofNativePtrArray native.cursize native.data;
            MaxSize = native.maxsize;
            ReadCount = native.readcount;
            Bit = native.bit;
        }

module IPAddress =
    let inline ofNativePtr (ptr: nativeptr<byte>) =
        {
            Octet1 = NativePtr.get ptr 0;
            Octet2 = NativePtr.get ptr 1;
            Octet3 = NativePtr.get ptr 2;
            Octet4 = NativePtr.get ptr 3;
        }

module Address =
    let inline ofNativePtr (ptr: nativeptr<netadr_t>) =
        let mutable native = NativePtr.read ptr

        {
            Type = enum<AddressType> (int native.type');
            IP = IPAddress.ofNativePtr &&native.ip
            Port = native.port;
        }

module Md3Frame =
    let inline ofNativePtr (ptr: nativeptr<md3Frame_t>) =
        let mutable native = NativePtr.read ptr

        {
            Bounds = Bounds.ofNativePtr &&native.bounds;
            LocalOrigin = Vec3.ofNativePtr &&native.localOrigin;
            Radius = native.radius;
            Name = NativePtr.toStringAnsi &&native.name;
        }

module Md3 =
    let ofNativePtr (ptr: nativeptr<md3Header_t>) =
        let mutable native = NativePtr.read ptr

        let hash = NativePtr.toNativeInt ptr

        match Map.tryFind hash Cache.md3Map with
        | Some x -> x
        | None ->

        let bytes = Array.zeroCreate<byte> native.ofsEnd
        Marshal.Copy (NativePtr.toNativeInt ptr, bytes, 0, native.ofsEnd)
        let md3 = FQuake3.Utils.Md3.parse bytes
        Cache.md3Map <- Map.add hash md3 Cache.md3Map
        md3

module DirectoryInfo =
    let ofNativePtr (ptr: nativeptr<directory_t>) =
        let mutable native = NativePtr.read ptr

        let path = NativePtr.toStringAnsi &&native.path
        let name = NativePtr.toStringAnsi &&native.gamedir

        DirectoryInfo (Path.Combine (path, name))

module Pak =
    let ofNativePtr (ptr: nativeptr<pack_t>) =
        let mutable native = NativePtr.read ptr

        {
            FileInfo = FileInfo (NativePtr.toStringAnsi &&native.pakFilename);
            Checksum = native.checksum;
            PureChecksum = native.checksum;
            FileCount = native.numfiles;
        }

module ServerPakChecksum =
    let createFrom_fs_serverPaks (size: int) (ptr: nativeptr<int>) =
        match NativePtr.isValid ptr with
        | false -> []
        | _ -> NativePtr.toList size ptr

module SearchPath =
    let ofNativePtr (ptr: nativeptr<searchpath_t>) =
        let mutable native = NativePtr.read ptr

        {
            DirectoryInfo = Option.ofNativePtr DirectoryInfo.ofNativePtr native.directory
        }

    let convertFrom_fs_searchpaths (ptr: nativeptr<searchpath_t>) =
        let rec f (searchPaths: SearchPath list) (ptr: nativeptr<searchpath_t>) =
            match NativePtr.isValid ptr with
            | false -> searchPaths
            | _ ->
            let mutable native = NativePtr.read ptr
            f (ofNativePtr ptr :: searchPaths) (native.next)

        f [] ptr
