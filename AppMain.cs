//# -*- coding: utf-8 -*-

using System;
using System.Collections.Generic;

using Sce.PlayStation.Core;
using Sce.PlayStation.Core.Environment;
using Sce.PlayStation.Core.Graphics;
using Sce.PlayStation.Core.Input;

using Sample;

namespace PSMMassParticle
{
	public class AppMain
	{
		static GraphicsContext graphics;
		static Random random ;
		public static Texture2D pixel_texture;

		public static double Math_random( ){
			return random.NextDouble();
		}

		public static int Random_Int( int max ){
			return random.Next( max+1 );
		}

		static MassParticle m_massParticle;

		public static void Main (string[] args)
		{
			Initialize ();

			while (true) {
				SystemEvents.CheckEvents ();
				Update ();
				Render ();
			}
		}

		public static void Initialize ()
		{
			// Set up the graphics system
			graphics = new GraphicsContext ();
			random = new Random() ;
			SampleDraw.Init( graphics ) ;
			SampleTimer.Init() ;

			pixel_texture = new Texture2D( 1, 1, false, PixelFormat.Rgba ) ;
			Rgba[] pixels = { new Rgba( 255, 255, 255, 255 ) } ;
			pixel_texture.SetPixels( 0, pixels ) ;

			m_massParticle = new MassParticle( graphics, graphics.Screen );
		}

		public static void Update ()
		{
			// Query gamepad for current state
			var gamePadData = GamePad.GetData (0);
		}

		public static void Render ()
		{
			SampleTimer.StartFrame() ;

			// Clear the screen
			graphics.SetViewport( 0, 0, graphics.Screen.Width, graphics.Screen.Height ) ;
			graphics.SetClearColor (0.0f, 0.0f, 0.0f, 0.0f);
			graphics.Clear ();

			{
				{
					m_massParticle.EmitSplash( new Vector2(480.0f, 272.0f), 512 );

					m_massParticle.RenderPosition();
				}

				{
					m_massParticle.RenderMove();
				}

				{
					graphics.SetTexture( 0, pixel_texture );
					m_massParticle.RenderParticle(  Matrix4.Identity );
				}
			}

			SampleDraw.DrawText( "Sprite Sample 2", 0xffffffff, 0, 0 ) ;
			var msg = string.Format( "FrameRate {0:F2} fps / FrameTime {1:F2} ms",
									 SampleTimer.AverageFrameRate, SampleTimer.AverageFrameTime * 1000 ) ;
			SampleDraw.DrawText( msg, 0xffffffff, 0, graphics.Screen.Height - 24 ) ;

			SampleTimer.EndFrame() ;
			// Present the screen
			graphics.SwapBuffers ();
		}
	}
}
