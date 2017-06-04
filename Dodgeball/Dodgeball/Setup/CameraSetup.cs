	// This is a generated file created by Glue. To change this file, edit the camera settings in Glue.
	// To access the camera settings, push the camera icon.
	using Camera = FlatRedBall.Camera;
	namespace Dodgeball
	{
		internal static class CameraSetup
		{
			const float Scale = 0.5f;
			internal static void ResetCamera (Camera cameraToReset)
			{
				FlatRedBall.Camera.Main.Orthogonal = true;
				FlatRedBall.Camera.Main.OrthogonalHeight = 1080;
				FlatRedBall.Camera.Main.OrthogonalWidth = 1920;
				FlatRedBall.Camera.Main.FixAspectRatioYConstant();
				SetAspectRatioTo(16 / 9m);
			}
			internal static void SetupCamera (Camera cameraToSetUp, Microsoft.Xna.Framework.GraphicsDeviceManager graphicsDeviceManager, int width = 1920, int height = 1080)
			{
				#if WINDOWS || DESKTOP_GL
				FlatRedBall.FlatRedBallServices.Game.Window.AllowUserResizing = true;
				FlatRedBall.FlatRedBallServices.GraphicsOptions.SetResolution((int)(width * Scale), (int)(height * Scale));
				#elif IOS || ANDROID
				FlatRedBall.FlatRedBallServices.GraphicsOptions.SetFullScreen(FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth, FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight);
				#elif UWP
				#endif
				ResetCamera(cameraToSetUp);
				FlatRedBall.FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += HandleResolutionChange;
			}
			private static void HandleResolutionChange (object sender, System.EventArgs args)
			{
				SetAspectRatioTo(16 / 9m);
				FlatRedBall.Camera.Main.OrthogonalHeight = FlatRedBall.Camera.Main.DestinationRectangle.Height / Scale;
				FlatRedBall.Camera.Main.FixAspectRatioYConstant();
			}
			private static void SetAspectRatioTo (decimal aspectRatio)
			{
				var resolutionAspectRatio = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth / (decimal)FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;
				int destinationRectangleWidth;
				int destinationRectangleHeight;
				int x = 0;
				int y = 0;
				if (aspectRatio > resolutionAspectRatio)
				{
					destinationRectangleWidth = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth;
					destinationRectangleHeight = FlatRedBall.Math.MathFunctions.RoundToInt(destinationRectangleWidth / (float)aspectRatio);
					y = (FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight - destinationRectangleHeight) / 2;
				}
				else
				{
					destinationRectangleHeight = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;
					destinationRectangleWidth = FlatRedBall.Math.MathFunctions.RoundToInt(destinationRectangleHeight * (float)aspectRatio);
					x = (FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth - destinationRectangleWidth) / 2;
				}
				FlatRedBall.Camera.Main.DestinationRectangle = new Microsoft.Xna.Framework.Rectangle(x, y, destinationRectangleWidth, destinationRectangleHeight);
				FlatRedBall.Camera.Main.FixAspectRatioYConstant();
			}
		}
	}
