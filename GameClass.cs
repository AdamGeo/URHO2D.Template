﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Urho.Urho2D;
using Urho;

namespace URHO2D.Template
{
    public class GameClass : DefLayout
    {
		Scene scene;
        bool drawDebug;
		const uint NumObjects = 10;
        Vector2 touchStart;
        Vector2 touchMove;
        bool wasTap;
        Node pickedNode;
        RigidBody2D dummyBody;
		double currentScale = 1;
		double startScale = 1;
        double startPinchScale = 0;
        bool didZoom = false;
		readonly List<Vector3> originalVertices = new List<Vector3>();
        Vector2 viewportSize;
        Camera camera;
		public GameClass(ApplicationOptions options = null) : base(options) { }
        static GB2ShapeCache gbcache;
        const int k_allCategory = 0xFFFF;
        const int CB_1 = 0x0001; //  using ^ CB_1 causes no contact with other things aswell ?!? (in cocos2d)
		const int CB_2 = 0x0002;
		const int CB_3 = 0x0004;
		const int CB_4 = 0x0008;
		const int CB_5 = 0x0010;
		const int CB_6 = 0x0020;
		const int CB_7 = 0x0040;
		const int CB_TERRAIN = 0x0080;
		const int CB_9 = 0x0100;
		const int CB_EXIT = 0x0200;
		const int CB_11 = 0x0400;
		const int CB_12 = 0x0800;
		const int CB_13 = 0x1000;
		const int CB_14 = 0x2000;
		const int CB_15 = 0x4000;
		const int CB_16 = 0x8000;
		const int k_allMaskBits           = k_allCategory;
		const int k_allButTerrainMaskBits = k_allCategory ^ CB_TERRAIN;

        protected override void Start()
		{
			base.Start();

            this.ResourceCache.UnknownResourceType += ResourceCache_UnknownResourceType;
            this.ResourceCache.LoadFailed += ResourceCache_LoadFailed;
            this.ResourceCache.ResourceNotFound += ResourceCache_ResourceNotFound;
            gbcache = new GB2ShapeCache().sharedGB2ShapeCache();
            gbcache.addShapesWithFile(Assets.SpriteShapes.UrhoIcon);
            Task create = CreateScene();
            var renderer = Renderer;

            viewportSize = new Vector2(Graphics.Width, Graphics.Height);

            //renderer.BeginViewRender
            //Urho.Renderer.GetActualView(this.Graphics.get)
			//SimpleCreateInstructionsWithWasd(", use PageUp PageDown keys to zoom.");
			SetupViewport();
			Engine.PostRenderUpdate += args =>
			{
				if (drawDebug)
				{
					// If draw debug mode is enabled, draw viewport debug geometry, which will show eg. drawable bounding boxes and skeleton
					// bones. Note that debug geometry has to be separately requested each frame. Disable depth test so that we can see the
					// bones properly
					if (drawDebug)
						scene.GetComponent<PhysicsWorld2D>().DrawDebugGeometry();
				}

			};

			//var pinchGesture = new PinchGestureRecognizer();
			//pinchGesture.PinchUpdated += OnPinchUpdated;
			//GestureRecognizers.Add(pinchGesture);

		}
		void HandleTouchBegin(TouchBeginEventArgs args)
		{

            if (Input.NumTouches > 1)
            {
				// Store both touches.
				TouchState touchZero = Input.GetTouch(0);
				TouchState touchOne = Input.GetTouch(1);

                //// Find the position in the previous frame of each touch.
                //IntVector2 touchZeroPrevPos = IntVector2.Subtract(touchZero.Position, touchZero.LastPosition);
                //IntVector2 touchOnePrevPos = IntVector2.Subtract(touchOne.Position, touchOne.LastPosition);

                //// Find the magnitude of the vector (the distance) between the touches in each frame.
                //float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).Length;
                //float touchDeltaMag = (touchZero.Position - touchOne.Position).Length;

                // Find the difference in the distances between each frame.
                //startPinchScale = (touchDeltaMag - prevTouchDeltaMag); //*PixelSize;

                didZoom = true;
                startScale = camera.Zoom;
                startPinchScale = IntVector2.Distance(touchZero.Position, touchOne.Position);
            }
            else {
				//Debug.WriteLine($"TouchBegin: {args.X} {args.Y}");
				touchStart = new Vector2(args.X, args.Y);
				wasTap = true;

				PhysicsWorld2D physicsWorld = scene.GetComponent<PhysicsWorld2D>();

				var graphics = Graphics;
				Vector3 p1 = new Vector3(camera.ScreenToWorldPoint(new Vector3((float)args.X / graphics.Width, (float)args.Y / graphics.Height, 0.0f)));
				RigidBody2D rigidBody = physicsWorld.GetRigidBody(p1.Xy);

                if (rigidBody != null && rigidBody.BodyType == BodyType2D.Dynamic)
				{
					pickedNode = rigidBody.Node;
					//StaticSprite2D staticSprite = pickedNode.GetComponent<StaticSprite2D>();
					//staticSprite.Color = (new Color(1.0f, 0.0f, 0.0f, 1.0f)); // Temporary modify color of the picked sprite
					rigidBody = pickedNode.GetComponent<RigidBody2D>();

					// Create a ConstraintMouse2D - Temporary apply this constraint to the pickedNode to allow grasping and moving with touch
					ConstraintMouse2D constraintMouse = pickedNode.CreateComponent<ConstraintMouse2D>();
					Vector3 pos = camera.ScreenToWorldPoint(new Vector3((float)args.X / graphics.Width, (float)args.Y / graphics.Height, 0.0f));
					constraintMouse.Target = pos.Xy;
					constraintMouse.MaxForce = 1000 * rigidBody.Mass;
					constraintMouse.CollideConnected = true;
					constraintMouse.OtherBody = dummyBody;  // Use dummy body instead of rigidBody. It's better to create a dummy body automatically in ConstraintMouse2D
					constraintMouse.DampingRatio = 0;
				}
            }
		}
        void AddBallAt(Vector2 pos) {
			Node node = scene.CreateChild("RigidBody");
			node.Position = (new Vector3(pos.X, pos.Y, 0.0f));

            //Debug.WriteLine($"node.Position: {node.Position.X} {node.Position.Y}");

			var cache = ResourceCache;
            Sprite2D ballSprite = cache.GetSprite2D(Assets.Sprites.Ball);

			// Create rigid body
			RigidBody2D body = node.CreateComponent<RigidBody2D>();
			body.BodyType = BodyType2D.Dynamic;
			StaticSprite2D staticSprite = node.CreateComponent<StaticSprite2D>();


			staticSprite.Sprite = ballSprite;
			// Create circle
			CollisionCircle2D circle = node.CreateComponent<CollisionCircle2D>();
			// Set radius
			circle.Radius = 0.16f;
			// Set density
			circle.Density = 1.0f;
			// Set friction.
			circle.Friction = 0.5f;
			// Set restitution
			circle.Restitution = 0.6f;
        }
		void HandleTouchMove(TouchMoveEventArgs args)
		{
            if (Input.NumTouches > 1)
            {
				TouchState touchZero = Input.GetTouch(0);
				TouchState touchOne = Input.GetTouch(1);
                double curPinchScale = IntVector2.Distance(touchZero.Position, touchOne.Position);
                double scl = (((curPinchScale/startPinchScale) - 1) * startScale); // * PixelSize;
				currentScale += scl;
				currentScale = currentScale.Clamp(0.25, 3.0); // Math.Max(1, currentScale);
                camera.Zoom = (float)currentScale;
                //Debug.WriteLine($"currentScale: {currentScale} | scl: {scl} | startPinchScale: {startPinchScale}");
                startPinchScale = curPinchScale;
            }
            else
            {
                if (pickedNode != null)
                {
                    //Debug.WriteLine("pickedNode != null");
                    var graphics = Graphics;
                    ConstraintMouse2D constraintMouse = pickedNode.GetComponent<ConstraintMouse2D>();
                    Vector3 pos = camera.ScreenToWorldPoint(new Vector3((float)args.X / graphics.Width, (float)args.Y / graphics.Height, 0.0f));
                    constraintMouse.Target = pos.Xy;
                }
                else
                {
                    if(!didZoom){
						//Debug.WriteLine("touchMove");
						//Debug.WriteLine($"TouchMove: [{args.X}] {args.Y}");
						touchMove = new Vector2(args.X, args.Y);
						wasTap = false;
                    }
                }
            }
		}

		void HandleTouchEnd(TouchEndEventArgs args)
		{
            if (Input.NumTouches > 1)
            {


            }
            else
            {
                Vector2 wargs = TouchToWorldPosition(args);
                Debug.WriteLine($"TouchEnd: {wargs.X} {wargs.Y}");

                if (wasTap && NearlyEquals(args.X, touchStart.X) && NearlyEquals(args.Y, touchStart.Y))
                {
                    //Debug.WriteLine("wasTap && NearlyEquals");
                    wasTap = false;

                    if((args.X > viewportSize.X * 0.9) && (args.Y < viewportSize.Y *0.15)) {
						scene.GetComponent<PhysicsWorld2D>().DrawJoint = !drawDebug;
						drawDebug = !drawDebug;
					}
                    else {

                        //CreateCar(wargs);

						//int x = RandomNumber(1, 4);
						//switch (x)
						//{
						//	case 1:
								gbcache.CreateSpriteWithNameAt("Ball", Assets.Sprites.Ball, true, wargs, scene, this);
						//		break;
						//	case 2:
						//		gbcache.CreateSpriteWithNameAt("UrhoIcon", Assets.Sprites.UrhoIcon, true, wargs, scene, this);
						//		break;
						//	case 3:
						//		gbcache.CreateSpriteWithNameAt("Cog", Assets.Sprites.Cog, true, wargs, scene, this);
						//		break;

						//}
                    }
                }
                if (pickedNode != null)
                {
                    //Debug.WriteLine("pickedNode != null");
                    //StaticSprite2D staticSprite = pickedNode.GetComponent<StaticSprite2D>();
                    //staticSprite.Color = (new Color(1.0f, 1.0f, 1.0f, 1.0f)); // Restore picked sprite color
                    pickedNode.RemoveComponent<ConstraintMouse2D>(); // Remove temporary constraint
                    pickedNode = null;
                }
                didZoom = false;
            }
		}
		private static readonly Random random = new Random();
		private static readonly object syncLock = new object();
		public static int RandomNumber(int min, int max)
		{
			lock (syncLock)
			{ // synchronize
				return random.Next(min, max);
			}
		}
		bool NearlyEquals(double? value1, double? value2, double unimportantDifference = 0.0001)
		{
			if (value1 != value2)
			{
				if (value1 == null || value2 == null)
					return false;
				return Math.Abs(value1.Value - value2.Value) < unimportantDifference;
			}
			return true;
		}
        void CreateCar(Vector2 pos)
        {

			Node car = scene.CreateChild("Box");
            car.Scale = new Vector3(4.0f, 1.0f, 0.0f);
			car.Position = (new Vector3(pos.X, pos.Y, 0.0f));
			StaticSprite2D boxSprite = car.CreateComponent<StaticSprite2D>();
            boxSprite.Sprite = ResourceCache.GetSprite2D("Sprites/Box.png");
			RigidBody2D boxBody = car.CreateComponent<RigidBody2D>();
            boxBody.BodyType = BodyType2D.Dynamic;
			boxBody.LinearDamping = 0.0f;
			boxBody.AngularDamping = 0.0f;
			CollisionBox2D shape = car.CreateComponent<CollisionBox2D>(); // Create box shape
			shape.Size = new Vector2(0.32f, 0.32f); // Set size
			shape.Density = 1.0f; // Set shape density (kilograms per meter squared)
			shape.Friction = 0.5f; // Set friction
			shape.Restitution = 0.1f; // Set restitution (slight bounce)

            Node ball1WheelNode = scene.CreateChild("Ball");
			ball1WheelNode.Position = (new Vector3(pos.X + 0.5f, pos.Y - 0.25f, 0.0f));
			StaticSprite2D ballSprite = ball1WheelNode.CreateComponent<StaticSprite2D>();
			ballSprite.Sprite = ResourceCache.GetSprite2D("Sprites/Ball.png");
			RigidBody2D ballBody = ball1WheelNode.CreateComponent<RigidBody2D>();
			ballBody.BodyType = BodyType2D.Dynamic;
			ballBody.LinearDamping = 0.0f;
			ballBody.AngularDamping = 0.0f;
			CollisionCircle2D ballShape = ball1WheelNode.CreateComponent<CollisionCircle2D>(); // Create circle shape
			ballShape.Radius = 0.16f; // Set radius
			ballShape.Density = 1.0f; // Set shape density (kilograms per meter squared)
			ballShape.Friction = 0.5f; // Set friction
			ballShape.Restitution = 0.6f; // Set restitution: make it bounce

			Node ball2WheelNode = scene.CreateChild("Ball");
			ball2WheelNode.Position = (new Vector3(pos.X - 0.5f, pos.Y - 0.25f, 0.0f));
			StaticSprite2D ballSprite2 = ball2WheelNode.CreateComponent<StaticSprite2D>();
			ballSprite2.Sprite = ResourceCache.GetSprite2D("Sprites/Ball.png");
			RigidBody2D ballBody2 = ball2WheelNode.CreateComponent<RigidBody2D>();
			ballBody2.BodyType = BodyType2D.Dynamic;
			ballBody2.LinearDamping = 0.0f;
			ballBody2.AngularDamping = 0.0f;
			CollisionCircle2D ballShape2 = ball2WheelNode.CreateComponent<CollisionCircle2D>(); // Create circle shape
			ballShape2.Radius = 0.16f; // Set radius
			ballShape2.Density = 1.0f; // Set shape density (kilograms per meter squared)
			ballShape2.Friction = 0.5f; // Set friction
			ballShape2.Restitution = 0.6f; // Set restitution: make it bounce

			ConstraintWheel2D wheel1 = car.CreateComponent<ConstraintWheel2D>();
			wheel1.OtherBody = ball1WheelNode.GetComponent<RigidBody2D>();
			wheel1.Anchor = ball1WheelNode.Position2D;
			wheel1.Axis = new Vector2(0.0f, 1.0f);
			wheel1.MaxMotorTorque = 20.0f;
			wheel1.FrequencyHz = 4.0f;
			wheel1.DampingRatio = 0.4f;
			wheel1.MotorSpeed = 5.0f;
			wheel1.EnableMotor = true;

			ConstraintWheel2D wheel2 = car.CreateComponent<ConstraintWheel2D>();
			wheel2.OtherBody = ball2WheelNode.GetComponent<RigidBody2D>();
			wheel2.Anchor = ball2WheelNode.Position2D;
			wheel2.Axis = new Vector2(0.0f, 1.0f);
			wheel2.MaxMotorTorque = 10.0f;
			wheel2.FrequencyHz = 4.0f;
			wheel2.DampingRatio = 0.4f;
			wheel2.MotorSpeed = 5.0f;
			wheel2.EnableMotor = true;
        }

		protected override void OnUpdate(float timeStep)
		{
            if (Input.NumTouches > 1) {
				if (pickedNode != null)
				{
					pickedNode.RemoveComponent<ConstraintMouse2D>(); // Remove temporary constraint
					pickedNode = null;
				}
                wasTap = false;

            }
            else {
				if (touchMove != Vector2.Zero && pickedNode == null)
				{
					MoveCameraByTouches2D(Vector2.Subtract(new Vector2(touchMove.X, touchMove.Y), touchStart), timeStep);
					touchStart = touchMove;
					touchMove = Vector2.Zero;
				}
            }

		}
		void SetupViewport()
		{
			var renderer = Renderer;
			renderer.SetViewport(0, new Viewport(Context, scene, CameraNode.GetComponent<Camera>(), null));

			Zone zone = renderer.DefaultZone;
			zone.FogColor = (new Color(0.1f, 0.1f, 0.1f));
		}
        async Task<int> CreateScene()
		{
			scene = new Scene();
			scene.CreateComponent<Octree>();
			scene.CreateComponent<DebugRenderer>();
			// Create camera node
			CameraNode = scene.CreateChild("Camera");
			// Set camera's position
			CameraNode.Position = (new Vector3(0.0f, 0.0f, -10.0f));

			camera = CameraNode.CreateComponent<Camera>();
			camera.Orthographic = true;

			var graphics = Graphics;
			camera.OrthoSize = (float)graphics.Height * PixelSize;
            camera.Zoom = 1.0f; // 1.2f * Math.Min((float)graphics.Width / 1280.0f, (float)graphics.Height / 800.0f); // Set zoom according to user's resolution to ensure full visibility (initial zoom (1.2) is set for full visibility at 1280x800 resolution)
            currentScale = 1.0f;
            startScale = 1.0f;
			var startMenu = scene.CreateComponent<StartMenu>();
            await startMenu.ShowStartMenu(false);
			startMenu.Remove();
			await StartGame();
            return 0;
		}

        Task StartGame()
        {
			// Create 2D physics world component
			PhysicsWorld2D physicsWorld = scene.CreateComponent<PhysicsWorld2D>();
			physicsWorld.DrawJoint = true;
			drawDebug = true;


            SVGDataDict svgt = new SVGDataDict(Assets.Terrain.Level06);
			List<SVGPathPoint> svgTokens = svgt["svgTokens"];
			CreateSVGTerrain(svgTokens, new Vector2(-2048.0f * Application.PixelSize, -2048.0f * Application.PixelSize));


			//svgt = new SVGDataDict(Assets.Terrain.Rotator);
			//svgTokens = svgt["svgTokens"];
			//Node rotr = CreateSvgNode(svgTokens, new Vector2(-16.5f, -16.5f));




			var cache = ResourceCache;
			Sprite2D boxSprite = cache.GetSprite2D(Assets.Sprites.Box);
			Sprite2D ballSprite = cache.GetSprite2D(Assets.Sprites.Ball);

			for (uint i = 0; i < NumObjects; ++i)
			{
				Node node = scene.CreateChild("RigidBody");
				node.Position = (new Vector3(NextRandom(3.6f, 3.8f), 12.0f + i * 0.4f, 0.0f));
				// 3.713754 12.11882

				// Create rigid body
				RigidBody2D body = node.CreateComponent<RigidBody2D>();
				body.BodyType = BodyType2D.Dynamic;
				StaticSprite2D staticSprite = node.CreateComponent<StaticSprite2D>();

				if (i % 2 == 0)
				{
					//boxSprite.SubscribeToEvent();
					staticSprite.Sprite = boxSprite;
					// Create box
					CollisionBox2D box = node.CreateComponent<CollisionBox2D>();
					// Set size
					box.Size = new Vector2(0.32f, 0.32f);
					// Set density
					box.Density = 1.0f;
					// Set friction
					box.Friction = 0.5f;
					// Set restitution
					box.Restitution = 0.1f;
				}
				else
				{
					staticSprite.Sprite = ballSprite;
					// Create circle
					CollisionCircle2D circle = node.CreateComponent<CollisionCircle2D>();
					// Set radius
					circle.Radius = 0.16f;
					// Set density
					circle.Density = 1.0f;
					// Set friction.
					circle.Friction = 0.5f;
					// Set restitution
					circle.Restitution = 0.4f;
                    //circle.Restitution = 1.0f;
				}
			}

			Input.TouchBegin += HandleTouchBegin;
			Input.TouchMove += HandleTouchMove;
			Input.TouchEnd += HandleTouchEnd;



            dummyBody = scene.CreateComponent<RigidBody2D>();

            return Task.FromResult(0);
		}

        void CreateSVGTerrain(List<SVGPathPoint> svgTokens, Vector2 atPosition, string spriteName = "") {
			Node mainNode = scene.CreateChild("RigidBody");
            mainNode.SetWorldPosition2D(atPosition);
			Node snode = mainNode.CreateChild("RigidBody");
			RigidBody2D centerbody = snode.CreateComponent<RigidBody2D>();
			centerbody.BodyType = BodyType2D.Static;
			centerbody.FixedRotation = true;
			snode.Position = new Vector3(0, 0, 0);
            if(spriteName != string.Empty) {
				Node spriteNode = mainNode.CreateChild("StaticSprite2D");
				StaticSprite2D sprite = spriteNode.CreateComponent<StaticSprite2D>();
				Sprite2D sr = ResourceCache.GetSprite2D(spriteName);
                sr.HotSpot = new Vector2(0, 0);
				spriteNode.SetPosition2D(0, 0);
                sprite.Sprite = sr;
            }
            Vector2 currentPos = svgTokens[0].vPoint;
            for (int i = 0; i < svgTokens.Count; i++)
			{
                SVGPathPoint token = svgTokens[i];

				//Debug.WriteLine($"token.CommandType: {token.CommandType}");

				switch (token.CommandType) {
					case "Z": // Z = closepath
					case "z":
						break;
					case "M": // M = moveto
					case "m":
                        currentPos = token.vPoint;
						break;
					case "L": // L = lineto
                    case "l": {
							CollisionEdge2D fd = snode.CreateComponent<CollisionEdge2D>();
							fd.Friction = 0.4f;
							fd.Restitution = 0.2f;
							fd.Density = 1.0f;
							fd.MaskBits = (ushort)65535;
							fd.GroupIndex = (short)0;
                            fd.CategoryBits = CB_TERRAIN;
							Vector2 a = currentPos;
							Vector2 b = token.vPoint;
							fd.SetVertices(a, b);
							currentPos = b;
                        }
						break;
					case "H": // H = horizontal lineto
					case "h":
						{
							CollisionEdge2D fd = snode.CreateComponent<CollisionEdge2D>();
							fd.Friction = 0.4f;
							fd.Restitution = 0.2f;
							fd.Density = 1.0f;
							fd.MaskBits = (ushort)65535;
							fd.GroupIndex = (short)0;
                            fd.CategoryBits = CB_TERRAIN;
							Vector2 a = currentPos;
							Vector2 b = token.vPoint;
							fd.SetVertices(a, b);
							currentPos = b;
						}
						break;
					case "V": // V = vertical lineto
					case "v":
						{
							CollisionEdge2D fd = snode.CreateComponent<CollisionEdge2D>();
							fd.Friction = 0.4f;
							fd.Restitution = 0.2f;
							fd.Density = 1.0f;
							fd.MaskBits = (ushort)65535;
							//fd.CategoryBits = (ushort)1;
                            fd.CategoryBits = CB_TERRAIN;
							fd.GroupIndex = (short)0;
							Vector2 a = currentPos;
							Vector2 b = token.vPoint;
							fd.SetVertices(a, b);
							currentPos = b;
						}
						break;
					case "C": // C = curveto
					case "c":
                        {
                            for (int si = 0; si < token.vPoints.Count; si ++) {
                                Vector2 stoken = token.vPoints[si];
								CollisionEdge2D fd = snode.CreateComponent<CollisionEdge2D>();
								fd.Friction = 0.4f;
								fd.Restitution = 0.2f;
								fd.Density = 1.0f;
								fd.MaskBits = (ushort)65535;
                                fd.CategoryBits = CB_TERRAIN;
								fd.GroupIndex = (short)0;
								Vector2 a = currentPos;
								Vector2 b = stoken;
								fd.SetVertices(a, b);
                                currentPos = b;
                            }
                        }
						break;
					case "S": // S = smooth curveto
					case "s":
						{
							for (int si = 0; si < token.vPoints.Count; si++)
							{
								Vector2 stoken = token.vPoints[si];
								CollisionEdge2D fd = snode.CreateComponent<CollisionEdge2D>();
								fd.Friction = 0.4f;
								fd.Restitution = 0.2f;
								fd.Density = 1.0f;
								fd.MaskBits = (ushort)65535;
                                fd.CategoryBits = CB_TERRAIN;
								fd.GroupIndex = (short)0;
								Vector2 a = currentPos;
								Vector2 b = stoken;
								fd.SetVertices(a, b);
								currentPos = b;
							}
						}
						break;
					case "Q": // Q = quadratic Bézier curve
					case "q":
					case "T": // T = smooth quadratic Bézier curveto
					case "t":
					case "A": // A = elliptical Arc
					case "a":
						Debug.WriteLine("TODO: Unimplemented path command: " + token.CommandType);
						break;
                    case "ellipse":
                        {
                            Dictionary<string, float> ShapeDetails = token.ShapeDetails;
							CollisionCircle2D centercircle = snode.CreateComponent<CollisionCircle2D>();
                            centercircle.SetCenter(ShapeDetails["X"], ShapeDetails["Y"]);
							centercircle.Radius = ShapeDetails["width"];
							centercircle.Friction = 0.4f;
							centercircle.Restitution = 0.2f;
							centercircle.Density = 1.0f;
							centercircle.MaskBits = (ushort)65535;
                            centercircle.CategoryBits = CB_TERRAIN;
							centercircle.GroupIndex = (short)0;
                        }
                        break;
					case "circle":
						{
							Dictionary<string, float> ShapeDetails = token.ShapeDetails;
							CollisionCircle2D centercircle = snode.CreateComponent<CollisionCircle2D>();
							centercircle.SetCenter(ShapeDetails["X"], ShapeDetails["Y"]);
							centercircle.Radius = ShapeDetails["radius"];
							centercircle.Friction = 0.4f;
							centercircle.Restitution = 0.2f;
							centercircle.Density = 1.0f;
							centercircle.MaskBits = (ushort)65535;
                            centercircle.CategoryBits = CB_TERRAIN;
							centercircle.GroupIndex = (short)0;
						}
						break;
					case "rect":
						{
							Dictionary<string, float> ShapeDetails = token.ShapeDetails;
							CollisionBox2D box = snode.CreateComponent<CollisionBox2D>();
							box.Size = new Vector2(ShapeDetails["width"], ShapeDetails["height"]);
							box.Density = 1.0f;
							box.Friction = 0.5f;
							box.Restitution = 0.1f;
                            box.SetCenter(ShapeDetails["X"], ShapeDetails["Y"]);
                            box.CategoryBits = CB_TERRAIN;
						}
						break;
					case "Rotator":
                        Vector2 pos = Vector2.Add(token.vPoint, atPosition);
                        CreateSvgNode(token.rotatorPoints, pos);
						break;
                    default:
                        Debug.WriteLine("default: Unimplemented path command: " + token.CommandType);
                        break;
				}
			}
        }

		private Node CreateSvgNode(List<SVGPathPoint> svgTokens, Vector2 atPosition)
		{

			Node BaseNode = scene.CreateChild("Box");
			BaseNode.Position = (new Vector3(atPosition.X, atPosition.Y, 0.0f));
			//StaticSprite2D boxSprite = BaseNode.CreateComponent<StaticSprite2D>();
			//boxSprite.Sprite = ResourceCache.GetSprite2D("Sprites/Box.png");
			RigidBody2D boxBody = BaseNode.CreateComponent<RigidBody2D>();
            boxBody.BodyType = BodyType2D.Static;
			boxBody.LinearDamping = 0.0f;
			boxBody.AngularDamping = 0.0f;
			CollisionCircle2D baseShape = BaseNode.CreateComponent<CollisionCircle2D>(); // Create circle shape
			baseShape.Radius = 0.16f; // Set radius
			baseShape.Density = 1.0f; // Set shape density (kilograms per meter squared)
			baseShape.Friction = 0.5f; // Set friction
			baseShape.Restitution = 0.6f; // Set restitution: make it bounce

			Node RotatorNode = scene.CreateChild("Ball");
			RotatorNode.Position = (new Vector3(atPosition.X, atPosition.Y, 0.0f));
			StaticSprite2D ballSprite = RotatorNode.CreateComponent<StaticSprite2D>();
			ballSprite.Sprite = ResourceCache.GetSprite2D("Sprites/Ball.png");
			RigidBody2D ballBody = RotatorNode.CreateComponent<RigidBody2D>();
			ballBody.BodyType = BodyType2D.Dynamic;
			ballBody.LinearDamping = 0.0f;
			ballBody.AngularDamping = 0.0f;

			CollisionCircle2D ballShape = RotatorNode.CreateComponent<CollisionCircle2D>(); // Create circle shape
			ballShape.Radius = 0.16f; // Set radius
			ballShape.Density = 1.0f; // Set shape density (kilograms per meter squared)
			ballShape.Friction = 0.5f; // Set friction
			ballShape.Restitution = 0.6f; // Set restitution: make it bounce


            List<Vector2> vertices = new List<Vector2>();


			float minX = float.MaxValue;
            float maxX = 0f;
            float minY = float.MaxValue;
            float maxY = 0f;
            for (int i = 0; i < svgTokens.Count; i++)
            {
                minX = Math.Min(svgTokens[i].vPoint.X, minX);
				maxX = Math.Max(svgTokens[i].vPoint.X, maxX);
				minY = Math.Min(svgTokens[i].vPoint.Y, minY);
				maxY = Math.Max(svgTokens[i].vPoint.Y, maxY);
            }
			Vector2 center = new Vector2(((maxX * 0.5f) + (minX * 0.5f)), ((maxY * 0.5f) + (minY * 0.5f)));
            for (int i = 0; i < svgTokens.Count; i++)
            {
                if(svgTokens[i].CommandType == "L" || svgTokens[i].CommandType == "l") {
                    vertices.Add(Vector2.Subtract(center, svgTokens[i].vPoint));
                }
            }

			Polygon pgon = new Polygon(vertices.ToArray());
			List<Triangle> triangles = pgon.Triangulate();

            foreach(Triangle tri in triangles) {
				CollisionPolygon2D polygonShape = RotatorNode.CreateComponent<CollisionPolygon2D>();
				polygonShape.VertexCount = 3;
                polygonShape.SetVertex(0, tri.Points[0]);
                polygonShape.SetVertex(1, tri.Points[1]);
                polygonShape.SetVertex(2, tri.Points[2]);
				polygonShape.Density = 3.0f;
				polygonShape.Friction = 0.2f;
				polygonShape.Restitution = 0.0f;
				polygonShape.MaskBits = k_allButTerrainMaskBits;
            }

			ConstraintRevolute2D constraintRevolute = BaseNode.CreateComponent<ConstraintRevolute2D>();
			constraintRevolute.OtherBody = RotatorNode.GetComponent<RigidBody2D>(); 
			constraintRevolute.Anchor = RotatorNode.Position2D; 
			constraintRevolute.EnableLimit = false;
			constraintRevolute.MaxMotorTorque = float.MaxValue;
			constraintRevolute.MotorSpeed = -0.5f;
			constraintRevolute.EnableMotor = true;
			return BaseNode;
		}
		void OnPinchUpdated(GestureInputEventArgs e)
        {
            var x = e;
            if (e.NumFingers > 1)
            {
                TouchState touch1 = Input.GetTouch((uint)0);
                TouchState touch2 = Input.GetTouch((uint)1);
            }
        }
        Vector2 TouchToWorldPosition(TouchEndEventArgs args) {
			PhysicsWorld2D physicsWorld = scene.GetComponent<PhysicsWorld2D>();
			var graphics = Graphics;
            return new Vector3(camera.ScreenToWorldPoint(new Vector3((float)args.X / graphics.Width, (float)args.Y / graphics.Height, 0.0f))).Xy;
        }
        void ResourceCache_ResourceNotFound(Urho.Resources.ResourceNotFoundEventArgs obj)
        {
            var ff = obj;
        }
		void ResourceCache_LoadFailed(Urho.Resources.LoadFailedEventArgs obj)
		{
            var ff = obj;
		}
		void ResourceCache_UnknownResourceType(Urho.Resources.UnknownResourceTypeEventArgs obj)
		{
            var ff = obj;
		}
    }
}
