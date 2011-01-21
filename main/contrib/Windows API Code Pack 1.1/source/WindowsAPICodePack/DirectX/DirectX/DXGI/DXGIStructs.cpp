// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DXGIStructs.h"

ColorRgba::ColorRgba(ColorF color)
{
    Red = color.Red;
	Green = color.Green;
	Blue = color.Blue; 
	Alpha = color.Alpha;
}

ColorRgba::ColorRgba(ColorF color, Single alpha)
{
    Red = color.Red;
	Green = color.Green;
	Blue = color.Blue; 
    Alpha = alpha;
}