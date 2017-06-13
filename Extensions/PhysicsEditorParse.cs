using System;
using System.Diagnostics;
using System.Collections.Generic;
using Urho;
using Urho.Physics;
using Urho.Audio;
using Urho.Actions;
using Urho.Shapes;
using Urho.Urho2D;
using System.Linq;
using System.Reflection;
namespace URHO2D.Template
{
	/*
	  Internal class to hold the fixtures
	 */
	public class FixtureDef
	{
		public FixtureDef()
		{
			next = null;
		}
		public void Dispose()
		{
			if (next != null)
				next.Dispose();
			fixture = null;
		}
        public FixtureDef next;
        public CollisionShape fixture = new CollisionShape();  // = new RigidBody();
		public int callbackData;
	}

	public class BodyDef
	{
		public BodyDef()
		{
			fixtures = null;
		}
		public void Dispose() {
			if (fixtures != null)
			{
				if (fixtures != null)
					fixtures.Dispose();
			}
		}
		public FixtureDef fixtures;
        public Vector2 anchorPoint = new Vector2(); // CCPoint
	}

    public class UrhoFixtureDef
    {
		public UrhoFixtureDef()
		{
            vertices = new List<Vector2>();
		}
        public int categoryBits;
        public int maskBits;
        public int groupIndex;
        public float friction;
        public float density;
        public float restitution;
        public bool isSensor;
        public string fixtureType;
        public uint vertexCount;
        public List<Vector2> vertices;
        public Vector2 center;
        public float radius;
        public Type shapeType;
    }



	public class GB2ShapeCache
	{
		//public float GetPtmRatio()
		//{
		//	return ptmRatio;
		//}
  //      public void SetPtmRatio(float ratio)
		//{
		//	ptmRatio = ratio;
		//}
		public void Dispose()
		{
            Debug.WriteLine("Dispose");
		}
		static SortedDictionary<string, BodyDef[]> BodyObjects = new SortedDictionary<string, BodyDef[]>();
        static SortedDictionary<string, List<UrhoFixtureDef>> ShapeObjects = new SortedDictionary<string, List<UrhoFixtureDef>>();

		public GB2ShapeCache()
		{
		}
        //private float ptmRatio;

        static GB2ShapeCache _sharedGB2ShapeCache = null;

        public GB2ShapeCache sharedGB2ShapeCache()
		{
			if (_sharedGB2ShapeCache == null)
			{
				_sharedGB2ShapeCache = new GB2ShapeCache();
				_sharedGB2ShapeCache.init();
			}
			return _sharedGB2ShapeCache;
		}

		public bool init()
		{
			return true;
		}

		public void reset()
		{
            List<string> keys = new List<string>(ShapeObjects.Keys);
            foreach (string key in keys) {
                ShapeObjects[key] = null;
            }
            ShapeObjects.Clear();
		}

		static Vector2 CCPointFromString(String str)
		{
			String theString = str;
            theString = theString.Replace("{ ", String.Empty); // [theString stringByReplacingOccurrencesOfString: @"{ " withString: @""];
            theString = theString.Replace(" }", String.Empty); // [theString stringByReplacingOccurrencesOfString: @" }" withString: @""];
            var array = theString.Split(',');        // [theString componentsSeparatedByString: @","];
            return new Vector2(float.Parse(array[0]), float.Parse(array[1]));
        }

		//public void addFixturesToBody(Node body, string shape)
		//{
		//          //Node snode = mainNode.CreateChild("RigidBody");

		//	CollisionPolygon2D triangle = body.CreateComponent<CollisionPolygon2D>();
		//	triangle.VertexCount = 3; // Set number of vertices (mandatory when using SetVertex())
		//	triangle.SetVertex(0, new Vector2(-0.064f, -0.0f));
		//	triangle.SetVertex(1, new Vector2(0.0f, 0.128f));
		//	triangle.SetVertex(2, new Vector2(0.64f, 0.0f));
		//	//triangle.SetVertex(3, new Vector2(0.4f, 0.25f));
		//	//triangle.SetVertex(4, new Vector2(0.25f, 0.45f));
		//	//triangle.SetVertex(5, new Vector2(-0.25f, 0.35f));
		//	triangle.Density = 1.0f; // Set shape density (kilograms per meter squared)
		//	triangle.Friction = 0.3f; // Set friction
		//	triangle.Restitution = 0.0f; // Set restitution (no bounce)


		//          var keys = shapeObjects[shape];
		//          foreach (BodyDef pos in keys)
		//	{
		//		//BodyDef so = pos.second;

		//		FixtureDef fix = pos.fixtures;
		//		while (fix != null)
		//		{
		//			body.CreateFixture(fix.fixture);
		//                  body.cre
		//			fix = fix.next;
		//		}
		//	}


		//	//SortedDictionary<string, BodyDef[]>.Enumerator pos = shapeObjects[shape];
		//	//C++ TO C# CONVERTER TODO TASK: Iterators are only converted within the context of 'while' and 'for' loops:
		//	//Debug.Assert(pos != shapeObjects.end());

		//	//C++ TO C# CONVERTER TODO TASK: Iterators are only converted within the context of 'while' and 'for' loops:

		//}

		//      public Vector2 anchorPointForShape(string shape)
		//{
		//	SortedDictionary<string, BodyDef>.Enumerator pos = shapeObjects.find(shape);
		//	//C++ TO C# CONVERTER TODO TASK: Iterators are only converted within the context of 'while' and 'for' loops:
		//	Debug.Assert(pos != shapeObjects.end());

		//	//C++ TO C# CONVERTER TODO TASK: Iterators are only converted within the context of 'while' and 'for' loops:
		//	BodyDef bd = pos.second;
		//	return bd.anchorPoint;
		//}

		public void addShapesWithFile(string plist)
		{
			//C++ TO C# CONVERTER TODO TASK: C# does not have an equivalent for pointers to value types:
			//ORIGINAL LINE: const sbyte fullName = CCFileUtils::fullPathFromRelativePath(plist.c_str());
			//sbyte fullName = CCFileUtils.fullPathFromRelativePath(plist);
			//Dictionary<string, Object> dict = CCFileUtils.dictionaryWithContentsOfFile(fullName);
			//CCAssert(dict != null, "Shape-file not found"); // not triggered - cocos2dx delivers empty dict if non was found
			//CCAssert(dict.count() != 0, "plist file empty or not existing");

			PList dict = new PList(plist);

			Dictionary<string, Object> metadataDict = (Dictionary<string, Object>)dict["metadata"];
			int format = (int)metadataDict["format"];
			//ptmRatio = (float)metadataDict["ptm_ratio"]; need URHO ratio not 32 from physicseditor
			Debug.Assert(format == 1, "Format not supported");

			Dictionary<string, Object> bodyDict = (Dictionary<string, Object>)dict["bodies"];

			 // 8 = b2_maxPolygonVertices

			//Dictionary<string, Object>.ObjectMapIter iter = new Dictionary<string, Object>.ObjectMapIter();

			//bodyDict.begin();
			//string bodyName;


			for (int i = 0; i < bodyDict.Count; i++)
			{
				string keyName = bodyDict.Keys.ElementAt(i);
				Dictionary<string, Object> bodyData = (Dictionary<string, Object>)bodyDict[keyName];

				BodyDef bodyDef = new BodyDef();
				bodyDef.anchorPoint = CCPointFromString((string)bodyData["anchorpoint"]);

				List<Object> fixtureList = (List<Object>)bodyData["fixtures"];

                List<UrhoFixtureDef> shapeArray = new List<UrhoFixtureDef>();

				foreach (PList fixtureData in fixtureList)
				{
					UrhoFixtureDef basicData = new UrhoFixtureDef();
					basicData.categoryBits = fixtureData["filter_categoryBits"]; 
					basicData.maskBits = fixtureData["filter_maskBits"]; 
					basicData.groupIndex = fixtureData["filter_groupIndex"]; 
					basicData.friction = fixtureData["friction"]; 
					basicData.density = fixtureData["density"]; 
					basicData.restitution = fixtureData["restitution"]; 
					basicData.isSensor = fixtureData["isSensor"]; 
					basicData.fixtureType = fixtureData["fixture_type"];
					if (basicData.fixtureType == "POLYGON")
					{
						List<Object> polygonsArrays = (List<Object>)fixtureData["polygons"];
						
						foreach (List<Object> polygonArray in polygonsArrays)
						{
                            UrhoFixtureDef polyshape = new UrhoFixtureDef();
                            polyshape.shapeType = typeof(CollisionPolygon2D);
                            polyshape.friction = basicData.friction;
							polyshape.restitution = basicData.restitution;
							polyshape.density = basicData.density;
							polyshape.categoryBits = basicData.categoryBits;
							polyshape.maskBits = basicData.maskBits;
							polyshape.groupIndex = basicData.groupIndex;
                            polyshape.vertexCount = (uint)polygonArray.Count;
                            int vindex = 0;
							foreach (string piter in polygonArray) {
								Vector2 offset = CCPointFromString(piter);
                                polyshape.vertices.Add(new Vector2((offset.X * Application.PixelSize), (offset.Y * Application.PixelSize)));
								vindex++;
							}
							shapeArray.Add(polyshape);
						}
						
					}
					else if (basicData.fixtureType == "CIRCLE")
					{
						//List<UrhoFixtureDef> shapeArray = new List<UrhoFixtureDef>();
						Dictionary<string, Object> circleData = (Dictionary<string, Object>)fixtureData["circle"];
                        UrhoFixtureDef circleshape = new UrhoFixtureDef();
                        circleshape.shapeType = typeof(CollisionCircle2D);
						circleshape.friction = basicData.friction;
						circleshape.restitution = basicData.restitution;
						circleshape.density = basicData.density;
						circleshape.categoryBits = basicData.categoryBits;
						circleshape.maskBits = basicData.maskBits;
						circleshape.groupIndex = basicData.groupIndex;
						circleshape.radius = (float)circleData["radius"] * Application.PixelSize;
						Vector2 p = CCPointFromString((String)circleData["position"]);
						circleshape.center = new Vector2(p.X * Application.PixelSize, p.Y * Application.PixelSize);
						shapeArray.Add(circleshape);
						//ShapeObjects.Add(keyName, shapeArray);
					}
				}
				// add the body element to the hash
				//shapeObjects[bodyName] = bodyDef;
                ShapeObjects.Add(keyName, shapeArray);
			}
		}

        public Node CreateSpriteWithNameAt(string name, string spriteFile, bool isDynamic, Vector2 pos, Node inParent, Application app) {
			Node node = inParent.CreateChild("RigidBody");
			node.Position = (new Vector3(pos.X, pos.Y, 0.0f));
			RigidBody2D body = node.CreateComponent<RigidBody2D>();
            body.BodyType = isDynamic ? BodyType2D.Dynamic : BodyType2D.Static;
			StaticSprite2D staticSprite = node.CreateComponent<StaticSprite2D>();
            staticSprite.Sprite = app.ResourceCache.GetSprite2D(spriteFile);
            List<UrhoFixtureDef> shapes = ShapeObjects[name];
			foreach (UrhoFixtureDef shapeData in shapes)
			{
				if (shapeData.shapeType == typeof(CollisionPolygon2D))
				{
					CollisionPolygon2D polygonShape = node.CreateComponent<CollisionPolygon2D>();
                    polygonShape.VertexCount = shapeData.vertexCount; // Set number of vertices (mandatory when using SetVertex())
                    for (uint i = 0; i < shapeData.vertices.Count; i++)
                    {
                        var v = shapeData.vertices.ElementAt((int)i);
                        polygonShape.SetVertex(i, v);
                    }
                    polygonShape.Density =shapeData.density;
                    polygonShape.Friction = shapeData.friction; 
                    polygonShape.Restitution = shapeData.restitution;
                    polygonShape.CategoryBits = shapeData.categoryBits;
					polygonShape.MaskBits = shapeData.maskBits;
					polygonShape.GroupIndex = shapeData.groupIndex;
				}
				else if (shapeData.shapeType == typeof(CollisionCircle2D))
				{
					CollisionCircle2D circle = node.CreateComponent<CollisionCircle2D>();
                    circle.Radius = shapeData.radius;
                    circle.Density = 0.1f; //shapeData.density;
                    circle.Friction = 0.0f; //shapeData.friction;
					circle.Restitution = shapeData.restitution;
					circle.CategoryBits = shapeData.categoryBits;
					circle.MaskBits = shapeData.maskBits;
					circle.GroupIndex = shapeData.groupIndex;
				}
				else
				{
					Debug.WriteLine("NONE TYPE!!!");
				}
			}
            return node;
		}

		public Node CreateStaticSpriteWithNameAt(string name, string spriteFile, Vector2 pos, Node inParent, Application app)
		{
			Node node = inParent.CreateChild("RigidBody");
			node.Position = (new Vector3(pos.X, pos.Y, 0.0f));
			var cache = app.ResourceCache;
			Sprite2D Sprite = cache.GetSprite2D(spriteFile, true);
			RigidBody2D body = node.CreateComponent<RigidBody2D>();
			body.BodyType = BodyType2D.Static;
			List<UrhoFixtureDef> shapes = ShapeObjects[name];
			StaticSprite2D staticSprite = node.CreateComponent<StaticSprite2D>();
			staticSprite.Sprite = Sprite;
			foreach (UrhoFixtureDef shapeData in shapes)
			{
				if (shapeData.shapeType == typeof(CollisionPolygon2D))
				{
					Node polygon = node.CreateChild("Polygon");
					RigidBody2D polygonBody = polygon.CreateComponent<RigidBody2D>();
					polygonBody.BodyType = BodyType2D.Static;

					for (int i = 0; i < shapeData.vertices.Count; i++)
					{

						Vector2 a = shapeData.vertices.ElementAt(i);
						Vector2 b;
						if (i == shapeData.vertices.Count - 1)
						{
							b = shapeData.vertices.ElementAt(0);
						}
						else
						{
							b = shapeData.vertices.ElementAt(i + 1);
						}

						CollisionEdge2D fd = polygon.CreateComponent<CollisionEdge2D>();
						fd.Friction = shapeData.friction;
						fd.Restitution = shapeData.restitution;
						fd.Density = shapeData.density;
                        fd.CategoryBits = shapeData.categoryBits;
                        fd.MaskBits = shapeData.maskBits;
                        fd.GroupIndex = shapeData.groupIndex;
						fd.SetVertices(a, b);
					}
				}
				else if (shapeData.shapeType == typeof(CollisionCircle2D))
				{
					CollisionCircle2D circle = node.CreateComponent<CollisionCircle2D>();
					circle.Radius = shapeData.radius;
					circle.Density = shapeData.density;
					circle.Friction = shapeData.friction;
					circle.Restitution = shapeData.restitution;
					circle.CategoryBits = shapeData.categoryBits;
					circle.MaskBits = shapeData.maskBits;
					circle.GroupIndex = shapeData.groupIndex;
				}
				else
				{
					Debug.WriteLine("FUCK! NONE TYPE!!!");
				}
			}
            return node;
		}
	}
}
