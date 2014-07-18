//# -*- coding: utf-8 -*-

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Sce.PlayStation.Core;
using Sce.PlayStation.Core.Graphics;
using Sce.PlayStation.Core.Imaging;

using System.IO;
using System.Reflection;

namespace Game.Framework
{
	/// <summary>簡単なスプライト。</summary>
	public class SimpleSprite
	{
		public ShaderProgram shaderProgram;

		protected GraphicsContext graphics;
		Matrix4 screenMatrix;

		internal static VertexFormat[] vertexFormats = {
			VertexFormat.Float3,		// position
			VertexFormat.Float2,		// texcoord
			VertexFormat.UByte4N,		// color
		};
		internal struct Vertex {
			public Vector3 Position;	//Float3
			public Vector2 TexCoord;	//Float2
			public Rgba Color;			//UByte4N
		}

		//@e Index
		//@j インデックス。
		const int indexSize = 4;
		ushort[] indices;

		//@j 頂点情報
		internal Vertex[] vertexData;

		//@e Vertex buffer
		//@j 頂点バッファ。
		VertexBuffer vertexBuffer;

		protected Texture2D texture;

		//@e Property cannot describe Position.X, public variable is used.
		//@j プロパティではPosition.Xという記述ができないため、publicの変数にしています。

		/// <summary>スプライトの表示位置。</summary>
		public Vector3 Position;

		/// <summary>
		/// スプライトの中心座標を指定。0.0f～1.0fの範囲で指定してください。
		/// スプライトの中心を指定する場合 X=0.5f, Y=0.5fを指定します。
		/// </summary>
		public Vector2 Center;

		public Vector2 Scale = Vector2.One;

//		protected Rgba color = new Rgba( 255, 255, 255, 255 );


		float width,height;
		/// <summary>スプライトの幅。</summary>
		public float Width
		{
			get { return width * Scale.X;}
		}
		/// <summary>スプライトの高さ。</summary>
		public float Height
		{
			get { return height * Scale.Y;}
		}

		/// <summary>コンストラクタ。</summary>
		public SimpleSprite( GraphicsContext graphics, Texture2D texture, FrameBuffer frameBuffer=null, ShaderProgram shader=null )
		{
			shaderProgram = shader;
			if( shaderProgram == null ){
				shaderProgram = CreateSimpleSpriteShader();
			}
#if false
			screenMatrix = new Matrix4(
				 Width*2.0f/screenWidth,	0.0f,		0.0f, 0.0f,
				 0.0f,	 Height*(-2.0f)/screenHeight,	0.0f, 0.0f,
				 0.0f,	 0.0f, 1.0f, 0.0f,
				 -1.0f+(Position.X-Width * Center.X)*2.0f/screenWidth,  1.0f+(Position.Y-Height*Center.Y)*(-2.0f)/screenHeight, 0.0f, 1.0f
			);
#else
			if( frameBuffer == null ){
				frameBuffer = graphics.Screen;
			}
			screenMatrix = new Matrix4(
				 2.0f/frameBuffer.Rectangle.Width,	0.0f,		0.0f, 0.0f,
				 0.0f,	 -2.0f/frameBuffer.Rectangle.Height,	0.0f, 0.0f,
				 0.0f,	 0.0f, 1.0f, 0.0f,
				-1.0f, 1.0f, 0.0f, 1.0f
			);
#endif
			shaderProgram.SetUniformValue(0, ref screenMatrix);

			if( texture == null )
			{
				throw new Exception("ERROR: texture is null.");
			}

			this.graphics = graphics;
			this.texture = texture;
			this.width = texture.Width;
			this.height = texture.Height;

			vertexData = new Vertex[4];
			vertexData[0].TexCoord = new Vector2( 0.0f, 0.0f );
			vertexData[0].Color = new Rgba( 255, 255, 255, 255 );
			vertexData[1].TexCoord = new Vector2( 0.0f, 1.0f );
			vertexData[1].Color = new Rgba( 255, 255, 255, 255 );
			vertexData[2].TexCoord = new Vector2( 1.0f, 0.0f );
			vertexData[2].Color = new Rgba( 255, 255, 255, 255 );
			vertexData[3].TexCoord = new Vector2( 1.0f, 1.0f );
			vertexData[3].Color = new Rgba( 255, 255, 255, 255 );

			indices = new ushort[indexSize];
			indices[0] = 0;
			indices[1] = 1;
			indices[2] = 2;
			indices[3] = 3;

			vertexBuffer = new VertexBuffer( 4, indexSize, vertexFormats );
		}

		public void Dispose()
		{
			if( vertexBuffer != null ){
				vertexBuffer.Dispose();
				vertexBuffer = null;
			}
		}

		//@e Vertex color settings
		//@j 頂点色の設定。
		public void SetColor(Vector4 in_color)
		{
			SetColor( new Rgba(in_color) );
		}

		//@e Vertex color settings
		//@j 頂点色の設定。
		public void SetColor( Rgba in_color )
		{
//			this.color = in_color;

			for( int i = 0; i < vertexData.Length; ++i ){
				vertexData[i].Color = in_color;
			}
		}

		public void SetColor( Rgba[] in_colors )
		{
			// 0:left top
			// 1:left bottom
			// 2:right top
			// 3:right bottom
			for( int i = 0; i < vertexData.Length; ++i ){
				vertexData[i].Color = in_colors[i];
			}
		}

		/// <summary>テクスチャ座標を指定します。ピクセル単位で指定してください。</summary>
		public void SetTextureCoord(float x0, float y0, float x1, float y1)
		{
			float u0 = x0 / texture.Width;
			float v0 = y0 / texture.Height;
			float u1 = x1 / texture.Width;
			float v1 = y1 / texture.Height;
			SetTextureUV( u0, v0, u1, v1 );
		}

		/// <summary>テクスチャ座標を指定します。ピクセル単位で指定してください。</summary>
		public void SetTextureCoord(Vector2 topLeft, Vector2 bottomRight)
		{
			SetTextureCoord(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
		}

		/// <summary>UV(0.0f～1.0f)でテクスチャ座標を指定します。</summary>
		public void SetTextureUV(float u0, float v0, float u1, float v1)
		{
			vertexData[0].TexCoord.X = u0;	// left top u
			vertexData[0].TexCoord.Y = v0;	// left top v

			vertexData[1].TexCoord.X = u0;	// left bottom u
			vertexData[1].TexCoord.Y = v1;	// left bottom v

			vertexData[2].TexCoord.X = u1;	// right top u
			vertexData[2].TexCoord.Y = v0;	// right top v

			vertexData[3].TexCoord.X = u1;	// right bottom u
			vertexData[3].TexCoord.Y = v1;	// right bottom v
		}

		public void SetTextureVFlip()
		{
			// swap
			float tmp = vertexData[0].TexCoord.Y;
			vertexData[0].TexCoord.Y = vertexData[1].TexCoord.Y;
			vertexData[1].TexCoord.Y = tmp;

			tmp = vertexData[2].TexCoord.Y;
			vertexData[2].TexCoord.Y = vertexData[3].TexCoord.Y;
			vertexData[3].TexCoord.Y = tmp;
		}

		public void ChangeTexture( Texture2D in_texture, float in_width,  float in_height )
		{
			this.texture = in_texture;
			this.width = in_width;
			this.height = in_height;
		}



		public void RenderFirst()
		{
			vertexData[0].Position.X = Position.X - Width*Center.X;			// x0
			vertexData[0].Position.Y = Position.Y - Height*Center.Y;		// y0
			vertexData[0].Position.Z = Position.Z;							// z0

			vertexData[1].Position.X = Position.X - Width*Center.X;			// x1
			vertexData[1].Position.Y = Position.Y + Height*(1.0f-Center.Y);	// y1
			vertexData[1].Position.Z = Position.Z;							// z1

			vertexData[2].Position.X = Position.X + Width*(1.0f-Center.X);	// x2
			vertexData[2].Position.Y = Position.Y - Height*Center.Y;		// y2
			vertexData[2].Position.Z = Position.Z;							// z2

			vertexData[3].Position.X = Position.X + Width*(1.0f-Center.X);	// x3
			vertexData[3].Position.Y = Position.Y + Height*(1.0f-Center.Y);	// y3
			vertexData[3].Position.Z = Position.Z;							// z3

			vertexBuffer.SetVertices( vertexData, 0, 0, 4 );
			vertexBuffer.SetIndices( indices );
		}

		public void RenderDraw( Matrix4 view_matrix )
		{
			Matrix4 mat = screenMatrix * view_matrix;
			shaderProgram.SetUniformValue( 0, ref mat );

			graphics.SetShaderProgram(shaderProgram);

			graphics.SetVertexBuffer(0, vertexBuffer);
			graphics.SetTexture(0, texture);

			graphics.DrawArrays(DrawMode.TriangleStrip, 0, indexSize);
		}

		/// <summary>スプライトの描画。</summary>
		public void Render()
		{
			this.RenderFirst();
			this.RenderDraw(  Matrix4.Identity );
		}

		public void Render( Matrix4 view_matrix )
		{
			this.RenderFirst();
			this.RenderDraw( view_matrix );
		}

		//@e Initialization of shader program
		//@j シェーダープログラムの初期化。
		private static ShaderProgram CreateSimpleSpriteShader()
		{
			ShaderProgram sp = new ShaderProgram( "/Application/shaders/SimpleSprite.cgx" );
			sp.SetUniformBinding(0, "u_WorldMatrix");
			return sp;
		}

	}
}
