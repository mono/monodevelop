///A module for converting primitive types into nint, nfloat, and nuint
module Conversions
    open System
    #nowarn "64"

    ///Converts a primitive type that support op_Implicit into the inferred type
    let inline implicit< ^a,^b when ^a : (static member op_Implicit : ^b -> ^a)> arg =
        (^a : (static member op_Implicit : ^b -> ^a) arg)
    ///Converts a primitive type that support op_Implicit into nint
    let inline nint (x:^a) : ^b = ((^a or ^b) : (static member op_Implicit : ^a -> nint) x)
    ///Converts a primitive type that support op_Implicit into nfloat
    let inline nfloat (x:^a) : ^b = ((^a or ^b) : (static member op_Implicit : ^a -> nfloat) x)
    ///Converts a primitive type that support op_Implicit into nuint
    let inline nuint (x:^a) : ^b = ((^a or ^b) : (static member op_Implicit : ^a -> nuint) x)
