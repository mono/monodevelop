//--------------------------------------------------------------------------------------
// File: SimpleSample.fx
//
// The effect file for the SimpleSample sample.  
// 
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------


//--------------------------------------------------------------------------------------
// Global variables
//--------------------------------------------------------------------------------------
float3 LightDir = float3(0,0.707,-0.707);  // Light's direction in world space
float4 MaterialColor;
matrix World;
matrix View;
matrix Projection;
float Brightness = 1.5;

//-----------------------------------------------------------------------------------------
// Textures and Samplers
//-----------------------------------------------------------------------------------------
Texture2D tex2D;
SamplerState linearSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

//--------------------------------------------------------------------------------------
// shader input/output structure
//--------------------------------------------------------------------------------------
struct VS_INPUT
{
    float4 Position   : POSITION;   // vertex position 
    float4 Normal     : NORMAL;		// this normal comes in per-vertex
    float4 Color	  : COLOR;
    float2 TextureUV  : TEXCOORD;   // vertex texture coords 
};


struct GSPS_INPUT
{
    float4 Position   : SV_POSITION;
    float4 Normal     : NORMAL;
    float4 Color	  : COLOR;
    float2 TextureUV  : TEXCOORD0;   // vertex texture coords 
};


//--------------------------------------------------------------------------------------
// Vertex Shaders
//--------------------------------------------------------------------------------------
GSPS_INPUT VS_textured( VS_INPUT input )
{
	GSPS_INPUT output = (GSPS_INPUT)0;
    float3 vNormalWorldSpace;
	
	output.Position = mul( input.Position, World );
	output.Position = mul( output.Position, View );
	output.Position = mul( output.Position, Projection );

    // Calc diffuse lighting color    
    vNormalWorldSpace = normalize(mul(input.Normal, World));
    output.Color.rgb = max(0.3,dot(vNormalWorldSpace, LightDir)).rrr * Brightness;
    output.Color.a = 1.0f; 

	output.TextureUV = input.TextureUV;
    
    return output;    
}

GSPS_INPUT VS_vertexColor( VS_INPUT input )
{
	GSPS_INPUT output = (GSPS_INPUT)0;
	
	output.Position = mul( input.Position, World );
	output.Position = mul( output.Position, View );
	output.Position = mul( output.Position, Projection );

	output.Color = input.Color  * Brightness;

	output.TextureUV = input.TextureUV;
    
    return output;    
}


//--------------------------------------------------------------------------------------
// Pixel Shaders
//--------------------------------------------------------------------------------------
float4 PS_textured( GSPS_INPUT input ) : SV_Target
{
	return tex2D.Sample( linearSampler, input.TextureUV )  * input.Color;
}

float4 PS_vertexColor( GSPS_INPUT input ) : SV_Target
{
	return input.Color;
}

float4 PS_materialColor( GSPS_INPUT input ) : SV_Target
{
	return MaterialColor  * Brightness / 2.0;
}


//--------------------------------------------------------------------------------------
// Geometry Shader
//--------------------------------------------------------------------------------------
[maxvertexcount(9)]
void GS( triangle GSPS_INPUT input[3], inout TriangleStream<GSPS_INPUT> TriStream )
{
    GSPS_INPUT output;
    
    for( int t=0; t < 3; t++ )
    {
		for( int v = 0; v < 3; v++ )
		{
			output.Position = input[v].Position;
			output.Position.w = output.Position.w + (10 * t);
			output.Position.z = output.Position.z + (10 * t);
			
			output.Normal = input[v].Normal;
			output.Color = input[v].Color;
			output.TextureUV = input[v].TextureUV;
			TriStream.Append( output );
		}
	    TriStream.RestartStrip();
    }
}

//--------------------------------------------------------------------------------------
// Techniques
//--------------------------------------------------------------------------------------
technique10 RenderTextured
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_textured() ) );
        SetGeometryShader( NULL );
		SetPixelShader( CompileShader( ps_4_0, PS_textured() ) );
	}
}

technique10 RenderVertexColor
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_vertexColor() ) );
        SetGeometryShader( NULL );
		SetPixelShader( CompileShader( ps_4_0, PS_vertexColor() ) );
	}
}

technique10 RenderMaterialColor
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_textured() ) );
        SetGeometryShader( NULL );
		SetPixelShader( CompileShader( ps_4_0, PS_materialColor() ) );
	}
}
