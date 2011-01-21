//--------------------------------------------------------------------------------------
// File: Render.hlsl
//
// The shaders for rendering tessellated mesh and base mesh
// 
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------


//--------------------------------------------------------------------------------------
// Vertex Shader
//--------------------------------------------------------------------------------------
float4 VertShader( float4 Pos : POSITION ) : SV_POSITION
{
    return Pos;
}


//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------
float4 PixShader( float4 Pos : SV_POSITION ) : SV_TARGET
{
    return float4( 1.0f, 1.0f, 0.0f, 1.0f );    // Yellow, with Alpha = 1
}

//--------------------------------------------------------------------------------------
technique10 Render
{
    pass P0
    {
        SetVertexShader( CompileShader( vs_4_0, VertShader() ) );
        SetGeometryShader( NULL );
        SetPixelShader( CompileShader( ps_4_0, PixShader() ) );
    }
}