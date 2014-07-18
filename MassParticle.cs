//# -*- coding: utf-8 -*-

using System;
using System.Runtime.InteropServices;

using Sce.PlayStation.Core;
using Sce.PlayStation.Core.Graphics;
using Game.Framework;

namespace PSMMassParticle
{
	// particle
	public class MassParticle : IDisposable
	{
		public const int TexWidth = 512;
		public const int TexHeight = 512;
		public const int DataSize = 4;
		public const int ParticleNum = TexWidth*TexHeight/DataSize-1;	//65536以上無理っぽい
		public const uint EmitParicleNum = 1024;
		public const uint EmitParticleMax = EmitParicleNum*DataSize-1;
		public const uint EmitIndexMax = TexWidth*TexHeight-1;

		GraphicsContext m_graphics = null;
		Matrix4 screenMatrix;

		PositionFrameBuffer[] m_positionBuff = new PositionFrameBuffer[2];
		int m_bufIn = 0;
		int m_bufOut = 1;
#if DEBUG
		SimpleSprite m_dbgSpr = null;
#endif

		// position
		ShaderProgram m_shaderPos = null;

		internal static VertexFormat[] vertexFormatsPos = {
			VertexFormat.Float,		// Index
			VertexFormat.Float2,	// Value
		};
		[StructLayout(LayoutKind.Explicit,Size=12)]
		internal struct VertexPos {
			[FieldOffset(0)]
			public float Index;
			[FieldOffset(4)]
			public Vector2 Value;
		}

		internal VertexPos[] vertexDataPos;
		VertexBuffer m_vertexBufferPos;
		uint m_emitOffset;
		uint m_emitIndex;

		// move
		MoveParticle m_moveRender = null;

		// output
		ShaderProgram m_shaderParticle = null;

		internal static VertexFormat[] vertexFormats = {
			VertexFormat.Float3,	// Position Direction
			VertexFormat.Float4,	// Size     Center
			VertexFormat.Float4,	// UVOffset UVSize
			VertexFormat.UByte4N	// Color
		};
		[StructLayout(LayoutKind.Explicit,Size=48)]
		internal struct Vertex {
			[FieldOffset(0)]
			public Vector2 Position ;
			[FieldOffset(8)]
			public float Direction ;
			[FieldOffset(12)]
			public Vector2 Size ;
			[FieldOffset(20)]
			public Vector2 Center ;
			[FieldOffset(28)]
			public Vector2 UVOffset ;
			[FieldOffset(36)]
			public Vector2 UVSize ;
			[FieldOffset(44)]
			public Rgba Color ;
		}

		internal static VertexFormat[] vertexFormats2 = {
			VertexFormat.Float,		// index
		};

		internal Vertex[] vertexData;
		VertexBuffer m_vertexBuffer0;
		VertexBuffer m_vertexBuffer1;
		VertexBuffer m_vertexBuffer2;

		public MassParticle( GraphicsContext graphics, FrameBuffer frameBuffer )
		{
			m_graphics = graphics;

			m_shaderPos = new ShaderProgram( "/Application/shaders/MassParticlePos.cgx" );
			m_shaderPos.SetUniformBinding(0, "WorldViewProj");
			m_shaderPos.SetUniformBinding(1, "Frag");
			m_shaderPos.SetUniformBinding(2, "DataSize");
			m_shaderPos.SetUniformValue( 1, 1.0f/TexWidth );
			m_shaderPos.SetUniformValue( 2, (float)DataSize );

			m_shaderParticle = new ShaderProgram( "/Application/shaders/MassParticle.cgx" );
			m_shaderParticle.SetUniformBinding(0, "WorldViewProj");
			m_shaderParticle.SetUniformBinding(1, "Frag");
			m_shaderParticle.SetUniformBinding(2, "DataSize");
			m_shaderParticle.SetUniformValue( 1, 1.0f/TexWidth );
			m_shaderParticle.SetUniformValue( 2, (float)DataSize );

			screenMatrix = new Matrix4(
				 2.0f/frameBuffer.Rectangle.Width,	0.0f,	0.0f, 0.0f,
				 0.0f, -2.0f/frameBuffer.Rectangle.Height,	0.0f, 0.0f,
				 0.0f,	0.0f, 1.0f, 0.0f,
				-1.0f, 1.0f, 0.0f, 1.0f
			);

			// position texture
			for( int i = 0; i < m_positionBuff.Length; ++i ){
				m_positionBuff[i] = new PositionFrameBuffer( m_graphics, TexWidth, TexHeight );
				m_positionBuff[i].Texture.SetFilter( TextureFilterMode.Disabled );
			}
			m_bufIn = 0;
			m_bufOut = 1;
#if DEBUG
			//for debug
//			m_dbgSpr = new SimpleSprite( m_graphics, m_positionBuff[1].Texture );
#endif
			int emitBufferSize = (int)EmitParicleNum * DataSize;
			m_vertexBufferPos = new VertexBuffer( emitBufferSize, vertexFormatsPos ) ;
			vertexDataPos = new VertexPos[ m_vertexBufferPos.VertexCount ];
			for( int i = 0; i < vertexDataPos.Length; ++i ){
				vertexDataPos[ i ].Index = (float)0;
				vertexDataPos[ i ].Value = new Vector2(0f,0f);
			}
			m_emitOffset = 0;

			// move
			m_moveRender = new MoveParticle( graphics, m_positionBuff[0].FrameBuf, TexWidth, TexHeight );

			// vertex0
			int m_maxNumOfSprite = 4;
			vertexData = new Vertex[ m_maxNumOfSprite ];
			for( int i = 0; i < m_maxNumOfSprite; ++i ){
				//vertexData[i].Position = new Vector2(480,272);
				vertexData[i].Size = new Vector2(2,2);
				vertexData[i].Center = new Vector2(0.5f,0.5f);
				vertexData[i].UVSize = new Vector2(1,1);
				vertexData[i].Color = new Rgba( 255, 255, 255, 255 );
			}
			m_vertexBuffer0 = new VertexBuffer( m_maxNumOfSprite, 0, 0, vertexFormats ) ;
			m_vertexBuffer0.SetVertices( vertexData, 0, 0, m_maxNumOfSprite ) ;

			// vertex1
			float[] points = { 0, 0, 0, 1, 1, 0, 1, 1 } ;
			m_vertexBuffer1 = new VertexBuffer( 4, VertexFormat.Float2 ) ;
			m_vertexBuffer1.SetVertices( 0, points ) ;

			// vertex2 - index
			float[] indexs = new float[ParticleNum];
			for( int i = 0; i < ParticleNum; ++i ){
				indexs[i] = i;
			}
			m_vertexBuffer2 = new VertexBuffer( ParticleNum, 0, 1, vertexFormats2 );
			m_vertexBuffer2.SetVertices( 0, indexs );

			this.ClearParticle();
		}

		public void ClearParticle()
		{
			for( int i = 0; i < m_positionBuff.Length; ++i ){
				m_positionBuff[i].RenderTexture();
			}
			m_emitOffset = 0;
			m_emitIndex = 0;
		}

		public void Dispose()
		{
			if( m_vertexBuffer2 != null ){
				m_vertexBuffer2.Dispose();
				m_vertexBuffer2 = null;
			}
			if( m_vertexBuffer1 != null ){
				m_vertexBuffer1.Dispose();
				m_vertexBuffer1 = null;
			}
			if( m_vertexBuffer0 != null ){
				m_vertexBuffer0.Dispose();
				m_vertexBuffer0 = null;
			}
			vertexData = null;
			m_shaderParticle = null;

			if( m_moveRender != null ){
				m_moveRender.Dispose();
				m_moveRender = null;
			}

			if( m_vertexBufferPos != null ){
				m_vertexBufferPos.Dispose();
				m_vertexBufferPos = null;
			}
			vertexDataPos = null;
			m_shaderPos = null;

#if DEBUG
			if( m_dbgSpr != null ){
				m_dbgSpr.Dispose();
				m_dbgSpr = null;
			}
#endif
			foreach( var fb in m_positionBuff ){
				fb.Dispose();
			}
			m_positionBuff = null;

			m_graphics = null;
		}

		public void RenderPosition()
		{
			FrameBuffer cfb = m_positionBuff[m_bufIn].FrameBuf;

			//this.EmitSplash( new Vector2(480.0f, 272.0f), 512 );

			if( m_emitOffset > 0 )
			{
				m_vertexBufferPos.SetVertices( vertexDataPos, 0, 0, (int)m_emitOffset );

				Matrix4 posMatrix = new Matrix4(
					 2.0f/cfb.Rectangle.Width,	0.0f,	0.0f, 0.0f,
					 0.0f, -2.0f/cfb.Rectangle.Height,	0.0f, 0.0f,
					 0.0f,	0.0f, 1.0f, 0.0f,
					-1.0f, 1.0f, 0.0f, 1.0f
				);
				Matrix4 mat = posMatrix;

				FrameBuffer prev_fb = m_graphics.GetFrameBuffer( );
				var prev_vp = m_graphics.GetViewport();
				m_graphics.SetFrameBuffer( cfb );
				m_graphics.SetViewport( 0, 0, cfb.Width, cfb.Height );
				m_graphics.SetBlendFunc( BlendFuncMode.Add, BlendFuncFactor.One, BlendFuncFactor.Zero );
				//FIXME:戻してない	

				m_graphics.SetShaderProgram(m_shaderPos);
				m_graphics.SetVertexBuffer( 0, m_vertexBufferPos );
				m_shaderPos.SetUniformValue( 0, ref mat );
				m_graphics.DrawArrays( DrawMode.Points, 0, m_vertexBufferPos.VertexCount );

				// frame buffer戻す
				m_graphics.SetFrameBuffer( prev_fb );
				m_graphics.SetViewport( prev_vp );
			}
#if DEBUG
			if( m_dbgSpr != null ){
				m_dbgSpr.Position.X = 950;
				m_dbgSpr.Position.Y = 10;
				m_dbgSpr.Center = new Vector2(1.0f, 0.0f);
				m_dbgSpr.Scale = new Vector2(0.5f, 0.5f);
				m_dbgSpr.Render();
			}
#endif
		}

		public void RenderMove()
		{
			m_moveRender.RenderTexture( m_positionBuff[m_bufIn].Texture, m_positionBuff[m_bufOut].FrameBuf );
#if DEBUG
			if( m_dbgSpr != null ){
				Texture2D tex = m_positionBuff[m_bufOut].Texture;
				m_dbgSpr.ChangeTexture( tex, tex.Width, tex.Height );
			}
#endif
		}

		public void RenderParticle( Matrix4 view_matrix )
		{
			m_graphics.SetShaderProgram(m_shaderParticle);
			m_graphics.SetVertexBuffer(0, m_vertexBuffer0);
			m_graphics.SetVertexBuffer(1, m_vertexBuffer1);
			m_graphics.SetVertexBuffer(2, m_vertexBuffer2);
			m_graphics.SetTexture(1, m_positionBuff[m_bufOut].Texture);

			Matrix4 mat = screenMatrix * view_matrix;
			m_shaderParticle.SetUniformValue( 0, ref mat );

			m_graphics.DrawArraysInstanced( DrawMode.TriangleStrip, 0, 4, 0, ParticleNum);

			this.swapBuffer();
		}

		void swapBuffer()
		{
			m_bufIn++;
			m_bufOut++;
			m_bufIn &= 1;
			m_bufOut &= 1;

			m_emitOffset = 0;
		}


		void set_emit_index()
		{
			m_emitOffset = (m_emitOffset + DataSize) & EmitParticleMax;
			m_emitIndex = (m_emitIndex + DataSize) & EmitIndexMax;
		}

		public void EmitSplash( Vector2 in_pos, int emit_num=256 )
		{
			const float vr = 300.0f;
			const float ar = 0.0f;//-340.0f;
			for( int i = 0; i < emit_num; ++i ){
				// position
				vertexDataPos[ m_emitOffset+0 ].Index = (float)m_emitIndex;
				vertexDataPos[ m_emitOffset+0 ].Value.X = in_pos.X;
				vertexDataPos[ m_emitOffset+1 ].Index = (float)m_emitIndex+1;
				vertexDataPos[ m_emitOffset+1 ].Value.X = in_pos.Y;
				// velocity
				float rad = AppMain.Random_Int(360) * FMath.DegToRad;
				Vector2 tmp = new Vector2( FMath.Cos( rad ), FMath.Sin( rad ) ) * (float)AppMain.Math_random();
				vertexDataPos[ m_emitOffset+2 ].Index = (float)m_emitIndex+2;
				vertexDataPos[ m_emitOffset+2 ].Value = tmp * vr;

				// acceleration
				vertexDataPos[ m_emitOffset+3 ].Index = (float)m_emitIndex+3;
				vertexDataPos[ m_emitOffset+3 ].Value = tmp * ar;

				this.set_emit_index();
			}
		}
	}


	// SimpleSpriteのFrameBuffer
	public class SimpleFrameBuffer : IDisposable
	{
		protected GraphicsContext graphics;	// 参照

		protected FrameBuffer m_frameBuffer;
		protected Texture2D m_Texture;
		protected SimpleSprite m_Spr;

		public Texture2D Texture {
			get { return m_Texture; }
		}

		public SimpleFrameBuffer( GraphicsContext gc, int width, int height )
		{
			graphics = gc;
			m_frameBuffer = new FrameBuffer();
			m_Texture = new Texture2D( width, height, false, PixelFormat.Rgba, PixelBufferOption.Renderable );
			m_frameBuffer.SetColorTarget( m_Texture, 0 );
			m_Spr = new SimpleSprite( gc, AppMain.pixel_texture, m_frameBuffer );
			m_Spr.Scale = new Vector2( width, height );
		}

		public virtual void Dispose()
		{
			if( m_Spr != null ){
				m_Spr.Dispose();
				m_Spr = null;
			}
			if( m_Texture != null ){
				m_Texture.Dispose();
				m_Texture = null;
			}
			if( m_frameBuffer != null ){
				m_frameBuffer.Dispose();
				m_frameBuffer = null;
			}
		}

		public void RenderTexture()
		{
			FrameBuffer prev = graphics.GetFrameBuffer( );
			var prev_vp = graphics.GetViewport();
			graphics.SetFrameBuffer( m_frameBuffer );
			graphics.SetViewport( 0, 0, m_frameBuffer.Width, m_frameBuffer.Height );
			graphics.SetClearColor( 0.0f, 0.0f, 0.0f, 1.0f );
			graphics.Clear();

			m_Spr.Render();

			// frame buffer戻す
			graphics.SetFrameBuffer( prev );
			graphics.SetViewport( prev_vp );
		}
	}

	// position用のframebuffer
	class PositionFrameBuffer : SimpleFrameBuffer
	{
		public FrameBuffer FrameBuf;

		public PositionFrameBuffer( GraphicsContext gc, int width, int height )
			: base( gc, width, height )
		{
			FrameBuf = new FrameBuffer();
			FrameBuf.SetColorTarget( m_Texture, 0 );
			m_Spr.SetColor( new Rgba(0,0,0,0) );
		}

		public override void Dispose()
		{
			if( FrameBuf != null ){
				FrameBuf.Dispose();
				FrameBuf = null;
			}

			base.Dispose();
		}
	}

	// particle移動用
	class MoveParticle
	{
		protected GraphicsContext graphics;		// 参照
		protected FrameBuffer a_frameBuffer;	// 参照

		protected SimpleSprite m_Spr;

		ShaderProgram m_shader;

		public MoveParticle( GraphicsContext gc, FrameBuffer fb, int width, int height )
		{
			graphics = gc;

			a_frameBuffer = fb;

			var filename = "/Application/shaders/MassParticleMove.cgx" ;
			m_shader = new ShaderProgram( filename );
			//m_shader = ShaderManager.GetShader( "Kishimen" );
			//m_shader.SetUniformBinding( 1, "Delta" );

			m_Spr = new SimpleSprite( gc, AppMain.pixel_texture, a_frameBuffer, m_shader );
			m_Spr.SetTextureVFlip();

			m_shader.SetUniformBinding( 1, "TexSize" );
			m_shader.SetUniformBinding( 2, "DataSize" );
			m_shader.SetUniformBinding( 3, "Delta" );
			m_shader.SetUniformValue( 1, (float)MassParticle.TexWidth );
			m_shader.SetUniformValue( 2, (float)MassParticle.DataSize );
		}

		public void Dispose()
		{
			if( m_Spr != null ){
				m_Spr.Dispose();
				m_Spr = null;
			}

			m_shader = null;
		}

		public void RenderTexture( Texture2D in_tex, FrameBuffer out_buf )
		{
			m_Spr.ChangeTexture( in_tex, in_tex.Width, in_tex.Height );

			FrameBuffer prev = graphics.GetFrameBuffer( );
			var prev_vp = graphics.GetViewport();
			{
				graphics.SetFrameBuffer( out_buf );
				graphics.SetViewport( 0, 0, out_buf.Width, out_buf.Height );
				//graphics.SetClearColor( 1f, 1f, 1f, 1f );
				//graphics.Clear();

				m_Spr.Render();
			}
			// frame buffer戻す
			graphics.SetFrameBuffer( prev );
			graphics.SetViewport( prev_vp );
		}

	}

}
