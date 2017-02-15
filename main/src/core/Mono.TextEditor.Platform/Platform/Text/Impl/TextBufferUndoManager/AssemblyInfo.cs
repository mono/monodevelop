// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Permissions;

//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly: AssemblyConfiguration ("")]
[assembly: AssemblyTrademark ("")]
[assembly: AssemblyCulture ("")]
#pragma warning disable 618
[assembly: SecurityPermission (SecurityAction.RequestMinimum, Flags = SecurityPermissionFlag.Execution)]
#pragma warning restore 618
[assembly: ReliabilityContract(Consistency.MayCorruptProcess, Cer.MayFail)]

[assembly: InternalsVisibleTo("Microsoft.VisualStudio.Logic.Text.BufferUndoManager.UnitTests, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")]
