//
// Copyright (c) 2008-2015 the Urho3D project.
// Copyright (c) 2015 Xamarin Inc
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Diagnostics;
using System.Globalization;
using Urho.Resources;
using Urho.Gui;
using Urho;

namespace URHO2D.Template
{
    public class DefLayout : Application
	{
		//UrhoConsole console;
		DebugHud debugHud;
		ResourceCache cache;
		Sprite logoSprite;
		UI ui;

		protected const float TouchSensitivity = 2;
		protected float Yaw { get; set; }
		protected float Pitch { get; set; }
		protected bool TouchEnabled { get; set; }
		protected Node CameraNode { get; set; }
		protected MonoDebugHud MonoDebugHud { get; set; }

		[Preserve]
        public DefLayout() : base(new ApplicationOptions(assetsFolder: "Data") { Height = 1024, Width = 576, Orientation = ApplicationOptions.OrientationType.Landscape,  }) { }

		[Preserve]
		public DefLayout(ApplicationOptions opts) : base(opts) { }

		static DefLayout()
		{
			//Urho.Application.UnhandledException += Application_UnhandledException1;
		}

		static void Application_UnhandledException1(object sender, UnhandledExceptionEventArgs e)
		{
			if (Debugger.IsAttached)
				Debugger.Break();
			e.Handled = true;

		}

		protected bool IsLogoVisible
		{
			get { return logoSprite.Visible; }
			set { logoSprite.Visible = value; }
		}

		static readonly Random random = new Random();
		/// Return a random float between 0.0 (inclusive) and 1.0 (exclusive.)
		public static float NextRandom() { return (float)random.NextDouble(); }
		/// Return a random float between 0.0 and range, inclusive from both ends.
		public static float NextRandom(float range) { return (float)random.NextDouble() * range; }
		/// Return a random float between min and max, inclusive from both ends.
		public static float NextRandom(float min, float max) { return (float)((random.NextDouble() * (max - min)) + min); }
		/// Return a random integer between min and max - 1.
		public static int NextRandom(int min, int max) { return random.Next(min, max); }

		/// <summary>
		/// Joystick XML layout for mobile platforms
		/// </summary>
		//protected virtual string JoystickLayoutPatch => string.Empty;

		protected override void Start()
		{
			Log.LogMessage += e => Debug.WriteLine($"[{e.Level}] {e.Message}");
			base.Start();
            cache = this.ResourceCache;
			if (Platform == Platforms.Android ||
				Platform == Platforms.iOS ||
				Options.TouchEmulation)
			{
				InitTouchInput();
			}
			Input.Enabled = true;
			MonoDebugHud = new MonoDebugHud(this);
			MonoDebugHud.Show();

			//CreateLogo();
			CreateConsoleAndDebugHud();
            //Input.SubscribeToKeyDown(HandleKeyDown);

		}

		protected override void OnUpdate(float timeStep)
		{
			base.OnUpdate(timeStep);
		}

		/// <summary>
		/// Move camera for 2D samples
		/// </summary>
		protected void SimpleMoveCamera2D(float timeStep)
		{
			// Do not move if the UI has a focused element (the console)
			if (UI.FocusElement != null)
				return;

			// Movement speed as world units per second
			const float moveSpeed = 4.0f;

			// Read WASD keys and move the camera scene node to the corresponding direction if they are pressed
			if (Input.GetKeyDown(Key.W)) CameraNode.Translate(Vector3.UnitY * moveSpeed * timeStep);
			if (Input.GetKeyDown(Key.S)) CameraNode.Translate(-Vector3.UnitY * moveSpeed * timeStep);
			if (Input.GetKeyDown(Key.A)) CameraNode.Translate(-Vector3.UnitX * moveSpeed * timeStep);
			if (Input.GetKeyDown(Key.D)) CameraNode.Translate(Vector3.UnitX * moveSpeed * timeStep);

			if (Input.GetKeyDown(Key.PageUp))
			{
				Camera camera = CameraNode.GetComponent<Camera>();
				camera.Zoom = camera.Zoom * 1.01f;
			}

			if (Input.GetKeyDown(Key.PageDown))
			{
				Camera camera = CameraNode.GetComponent<Camera>();
				camera.Zoom = camera.Zoom * 0.99f;
			}
		}

		protected void MoveCameraByTouches2D(Vector2 offset, float timeStep)
		{
			if (!TouchEnabled || CameraNode == null)
				return;
            float z = CameraNode.GetComponent<Camera>().Zoom;
            CameraNode.Translate2D(new Vector2((((offset.X*-1)/2)* timeStep)/z, ((offset.Y/2 )* timeStep)/ z));
            //Debug.WriteLine($"Translate2D : {new Vector2(((offset.X * -1) / 2) * timeStep, (offset.Y / 2) * timeStep)} @zoom: {CameraNode.GetComponent<Camera>().Zoom}");

		}

		void CreateLogo()
		{
			var logoTexture = cache.GetTexture2D("Textures/LogoLarge.png");

			if (logoTexture == null)
				return;

			ui = UI;
			logoSprite = ui.Root.CreateSprite();
			logoSprite.Texture = logoTexture;
			int w = logoTexture.Width;
			int h = logoTexture.Height;
			logoSprite.SetScale(256.0f / w);
			logoSprite.SetSize(w, h);
			logoSprite.SetHotSpot(0, h);
			logoSprite.SetAlignment(HorizontalAlignment.Left, VerticalAlignment.Bottom);
			logoSprite.Opacity = 0.75f;
			logoSprite.Priority = -100;
		}

		void CreateConsoleAndDebugHud()
		{
            //var f = ResourceCache.GetXmlFile("UI/DefaultStyle.xml");
			var xml = cache.GetXmlFile("UI/DefaultStyle.xml");
			//console = Engine.CreateConsole();
			//console.DefaultStyle = xml;
			//console.Background.Opacity = 0.8f;

			debugHud = Engine.CreateDebugHud();
			debugHud.DefaultStyle = xml;
		}
		//void HandleKeyDown(KeyDownEventArgs e)
		//{
		//	switch (e.Key)
		//	{
		//		case Key.Esc:
		//			Exit();
		//			return;
		//		case Key.F1:
		//			console.Toggle();
		//			return;
		//		case Key.F2:
		//			debugHud.ToggleAll();
		//			return;
		//	}

		//	var renderer = Renderer;
		//	switch (e.Key)
		//	{
		//		case Key.N1:
		//			var quality = renderer.TextureQuality;
		//			++quality;
		//			if (quality > 2)
		//				quality = 0;
		//			renderer.TextureQuality = quality;
		//			break;

		//		case Key.N2:
		//			var mquality = renderer.MaterialQuality;
		//			++mquality;
		//			if (mquality > 2)
		//				mquality = 0;
		//			renderer.MaterialQuality = mquality;
		//			break;

		//		case Key.N3:
		//			renderer.SpecularLighting = !renderer.SpecularLighting;
		//			break;

		//		case Key.N4:
		//			renderer.DrawShadows = !renderer.DrawShadows;
		//			break;

		//		case Key.N5:
		//			var shadowMapSize = renderer.ShadowMapSize;
		//			shadowMapSize *= 2;
		//			if (shadowMapSize > 2048)
		//				shadowMapSize = 512;
		//			renderer.ShadowMapSize = shadowMapSize;
		//			break;

		//		// shadow depth and filtering quality
		//		case Key.N6:
		//			var q = (int)renderer.ShadowQuality++;
		//			if (q > 3)
		//				q = 0;
		//			renderer.ShadowQuality = (ShadowQuality)q;
		//			break;

		//		// occlusion culling
		//		case Key.N7:
		//			var o = !(renderer.MaxOccluderTriangles > 0);
		//			renderer.MaxOccluderTriangles = o ? 5000 : 0;
		//			break;

		//		// instancing
		//		case Key.N8:
		//			renderer.DynamicInstancing = !renderer.DynamicInstancing;
		//			break;

		//		case Key.N9:
		//			Image screenshot = new Image();
		//			Graphics.TakeScreenShot(screenshot);
		//			screenshot.SavePNG(FileSystem.ProgramDir + $"Data/Screenshot_{GetType().Name}_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture)}.png");
		//			break;
		//	}
		//}

		void InitTouchInput()
		{
			TouchEnabled = true;
			//var layout = ResourceCache.GetXmlFile("UI/ScreenJoystick_DefLayouts.xml");
			//if (!string.IsNullOrEmpty(JoystickLayoutPatch))
			//{
			//	XmlFile patchXmlFile = new XmlFile();
			//	patchXmlFile.FromString(JoystickLayoutPatch);
			//	layout.Patch(patchXmlFile);
			//}
			//var screenJoystickIndex = Input.AddScreenJoystick(layout, ResourceCache.GetXmlFile("UI/DefaultStyle.xml"));
			//Input.SetScreenJoystickVisible(screenJoystickIndex, true);
		}
	}
}

