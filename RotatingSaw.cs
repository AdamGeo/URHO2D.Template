using System;
using System.Linq;
using System.Threading.Tasks;
using Urho;
using Urho.Physics;
using Urho.Audio;
using Urho.Actions;
using Urho.Shapes;
using Urho.Urho2D;
using System.Diagnostics;
using System.Collections.Generic;

namespace URHO2D.Template
{
	public class RotatingSawDef
	{
		public RotatingSawDef()
		{
            center = new Vector2(0.0f, 0.0f);
			radius = 0.5f;
			numParts = 6;
			softness = 0.2f;
			density = 1.0f;
			friction = 0.3f;
			jointFrequencyHz = 4.0f;
			jointDampingRatio = 0.5f;
		}
		public Vector2 center = new Vector2(); 
		public float radius; 
		public int numParts; 
		public float softness; 
		public float density;
		public float friction;
		public float jointFrequencyHz;
		public float jointDampingRatio;
	}

    public class RotatingSaw : Component
    {
        Node mainNode;
		public int m_numParts;
		public List<RigidBody2D> m_parts;
		public Node m_center;



		protected RotatingSaw()
		{
			ReceiveSceneUpdates = true;
		}
		public RotatingSaw(Scene pscene, RotatingSawDef def)
		{
            mainNode = pscene.CreateChild("RigidBody");
            m_parts = new List<RigidBody2D>();
			float angleStep = (3.14159265358979323846f * 2.0f) / def.numParts;
            float sinHalfAngle = (float)Math.Sin(angleStep * 0.5f); //  sinf(angleStep * 0.5f);
			float subCircleRadius = sinHalfAngle * def.radius / (1.0f + sinHalfAngle);
			float angle = 0F;
			for (int i = 0; i < def.numParts; i++)
			{
				Node snode = mainNode.CreateChild("RigidBody");

    //            CollisionCircle2D circle = snode.CreateComponent<CollisionCircle2D>();
				//circle.Radius = subCircleRadius;
				//circle.Density = 1.0f;
				//circle.Friction = 0.5f;
				//circle.Restitution = 0.6f;

                CollisionPolygon2D triangle = snode.CreateComponent<CollisionPolygon2D>();
				triangle.VertexCount = 3; // Set number of vertices (mandatory when using SetVertex())
				triangle.SetVertex(0, new Vector2(-0.064f, -0.0f));
				triangle.SetVertex(1, new Vector2(0.0f, 0.128f));
				triangle.SetVertex(2, new Vector2(0.64f, 0.0f));
				//triangle.SetVertex(3, new Vector2(0.4f, 0.25f));
				//triangle.SetVertex(4, new Vector2(0.25f, 0.45f));
				//triangle.SetVertex(5, new Vector2(-0.25f, 0.35f));
				triangle.Density = 1.0f; // Set shape density (kilograms per meter squared)
				triangle.Friction = 0.3f; // Set friction
				triangle.Restitution = 0.0f; // Set restitution (no bounce)

				

                Vector2 offset = new Vector2((float)System.Math.Sin(angle), (float)System.Math.Cos(angle));
				offset *= def.radius - subCircleRadius;
                RigidBody2D sbd = snode.CreateComponent<RigidBody2D>();
                sbd.BodyType = BodyType2D.Static;
                Vector2 pos = def.center + offset;
                snode.Position = new Vector3(pos.X, pos.Y, 0);
                m_parts.Add(sbd);
				angle += angleStep;
			}
            m_center = mainNode.CreateChild("RigidBody");
			RigidBody2D centerbody = m_center.CreateComponent<RigidBody2D>();
            centerbody.BodyType = BodyType2D.Static;
            centerbody.FixedRotation = true;
            m_center.Position = new Vector3(def.center.X, def.center.Y, 0);



			CollisionCircle2D centercircle = m_center.CreateComponent<CollisionCircle2D>();
			centercircle.Radius = (def.radius - subCircleRadius * 2.0f) * (1.0f - def.softness);
			centercircle.Density = 1.0f;
			centercircle.Friction = 0.5f;
			centercircle.Restitution = 0.6f;


            //centerbody.GravityScale = 0.25f;



            //ConstraintWeld2D constraintPrismatic = centerbody.Node.CreateComponent<ConstraintWeld2D>();
            //constraintPrismatic.OtherBody = m_parts[0];
            //constraintPrismatic.Anchor = centerbody.Node.Position2D;
            //constraintPrismatic.FrequencyHz = 4.0f;
            //constraintPrismatic.DampingRatio = 0.5f;
            var cache = Application.ResourceCache;
			for (int i = 0; i < def.numParts; i++)
			{
				//int neighborIndex = (i + 1) % def.numParts;
    //            ConstraintDistance2D constraintDistance = m_parts[i].Node.CreateComponent<ConstraintDistance2D>(); 
				//constraintDistance.OtherBody = m_parts[neighborIndex]; 
				//constraintDistance.OwnerBodyAnchor = m_parts[i].Node.Position2D;
    //            constraintDistance.OtherBodyAnchor = m_parts[neighborIndex].Node.Position2D;
				//constraintDistance.FrequencyHz = def.jointFrequencyHz;
				//constraintDistance.DampingRatio = def.jointDampingRatio;
                //constraintDistance.CollideConnected = true;
                ConstraintDistance2D toCenterDistance = centerbody.Node.CreateComponent<ConstraintDistance2D>();
				toCenterDistance.OtherBody = m_parts[i];
                toCenterDistance.OwnerBodyAnchor = centerbody.Node.Position2D;
				toCenterDistance.OtherBodyAnchor = m_parts[i].Node.Position2D;
				toCenterDistance.FrequencyHz = def.jointFrequencyHz;
				toCenterDistance.DampingRatio = def.jointDampingRatio;
                toCenterDistance.CollideConnected = true;
			}
            Sprite2D boxSprite = cache.GetSprite2D(Assets.Sprites.Ball);
            StaticSprite2D staticSprite = m_center.CreateComponent<StaticSprite2D>();
            staticSprite.Sprite = boxSprite;



		}
    }
}
