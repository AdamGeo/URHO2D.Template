using System;
using Urho;

namespace URHO2D.Template.Extensions
{
    public class SoftBody : Component {
		//C++ TO C# CONVERTER TODO TASK: The following statement was not recognized, possibly due to an unrecognized macro:
		//OBJECT(SoftBody);
		/// Construct.
		/// 
        const string PHYSICS_CATEGORY = "Physics";

		public SoftBody(Context context)
		{
			//this.Component = context;
			this.body_ = null;
			this.vertexBuffer_ = null;
		}
		/// Destruct. Free the soft body and geometries.
		public void Dispose()
		{
			if (body_)
			{
				body_ = null;
				body_ = null;
			}
			// We don't own the vertsbuffer
			vertexBuffer_ = null;
		}
		/// Register object factory.
		//public void RegisterObject(Context context)
		//{
		//	//context.RegisterFactory<SoftBody>(PHYSICS_CATEGORY);
  //          context.RegisterFactory();
		//}
		/// Handle logic post-update event where we update the vertex buffer.
		public void HandlePostUpdate(StringHash eventType, VariantMap eventData)
		{
			// Update vertex buffer
			if (body_ && vertexBuffer_)
			{
				//C++ TO C# CONVERTER TODO TASK: C# does not have an equivalent for pointers to value types:
				//ORIGINAL LINE: byte* pVertexData = (byte*)vertexBuffer_->Lock(0, vertexBuffer_->GetVertexCount());
				byte pVertexData = (byte)vertexBuffer_.Lock(0, vertexBuffer_.GetVertexCount());
				// Copy soft body vertices back into the model vertex buffer
				if (pVertexData != 0)
				{
					uint numVertices = vertexBuffer_.GetVertexCount();
					uint vertexSize = vertexBuffer_.GetVertexSize();

					// Copy original vertex positions
					for (uint i = 0; i < body_.m_nodes.size(); ++i)
					{
						btSoftBody.Node n = body_.m_nodes[i];
						//C++ TO C# CONVERTER TODO TASK: There is no equivalent to 'reinterpret_cast' in C#:
						Vector3 src = reinterpret_cast<Vector3*>(pVertexData + i * vertexSize);
						src = ToVector3(n.m_x);
					}
					vertexBuffer_.Unlock();
				}
			}
		}

		/// Remove the soft body.
		public void ReleaseBody()
		{
			if (body_)
			{
				RemoveBodyFromWorld();
				body_ = null;
				body_ = null;
			}
		}
		/// Create TriMesh from model's geometry.
		public void CreateTriMesh()
		{
			ResourceCache cache = GetSubsystem<ResourceCache>();
			Scene scene = GetScene();
			// Get model
			StaticModel model = node_.GetComponent<StaticModel>();
			if (model == null)
				return;
			Model originalModel = model.GetModel();
			if (originalModel == null)
				return;

			// Clone model
			SharedPtr<Model> cloneModel = originalModel.Clone();
			model.SetModel(cloneModel);

			// Get the vertex and index buffers from the first geometry's first LOD level
			VertexBuffer vertexBuffer = cloneModel.GetGeometry(0, 0).GetVertexBuffer(0);
			IndexBuffer indexBuffer = cloneModel.GetGeometry(0, 0).GetIndexBuffer();

			// Cretae soft body from TriMesh
			CreateBodyFromTriMesh(vertexBuffer, indexBuffer);
		}
		/// Create the soft body from a TriMesh.
		public bool CreateBodyFromTriMesh(VertexBuffer vertexBuffer, IndexBuffer indexBuffer, bool randomizeConstraints)
		{
			bool bConstructed = false;
			if (vertexBuffer != null && indexBuffer != null)
			{
				btAlignedObjectArray<bool> chks = new btAlignedObjectArray<bool>();
				btAlignedObjectArray<btVector3> vtx = new btAlignedObjectArray<btVector3>();

				// Save vertexbuffer ptr
				vertexBuffer_ = vertexBuffer;

				// Copy vertex buffer
				//C++ TO C# CONVERTER TODO TASK: C# does not have an equivalent for pointers to value types:
				//ORIGINAL LINE: const byte* pVertexData = (const byte*)vertexBuffer_->Lock(0, vertexBuffer_->GetVertexCount());
				byte pVertexData = (byte)vertexBuffer_.Lock(0, vertexBuffer_.GetVertexCount());

				if (pVertexData != 0)
				{
					uint numVertices = vertexBuffer_.GetVertexCount();
					uint vertexSize = vertexBuffer_.GetVertexSize();

					vtx.resize(numVertices);

					// Copy the original verts
					for (uint i = 0; i < numVertices; ++i)
					{
						//C++ TO C# CONVERTER TODO TASK: There is no equivalent to 'reinterpret_cast' in C#:
						Vector3 src = reinterpret_cast <const Vector3*> (pVertexData + i * vertexSize);
						vtx[i] = ToBtVector3(src);
					}
					vertexBuffer_.Unlock();
				}

				// Create softbody
				physicsWorld_ = GetScene().GetComponent<PhysicsWorld>();
				body_ = new btSoftBody(physicsWorld_.GetSoftBodyInfo(), vtx.size(), vtx[0], 0);

				// Copy indexbuffer
				uint[] pIndexData = (uint)indexBuffer.Lock(0, indexBuffer.GetIndexCount());
				ushort[] pUShortData = (ushort)pIndexData;
				if (pIndexData)
				{
					uint numIndices = indexBuffer.GetIndexCount();
					uint indexSize = indexBuffer.GetIndexSize();

					int ntriangles = (int)numIndices / 3;

					int maxidx = 0;
					int i; //,j,ni;

					if (indexSize == sizeof(ushort))
					{
						for (i = 0; i < (int)numIndices; ++i)
						{
							uint uidx = pUShortData[i];
							maxidx = Max(uidx, maxidx);
						}
					}
					else if (indexSize == sizeof(uint))
					{
						for (i = 0; i < (int)numIndices; ++i)
						{
							uint uidx = pIndexData[i];
							maxidx = Max(uidx, maxidx);
						}
					}
					++maxidx;
					chks.resize(maxidx * maxidx, false);

					for (i = 0; i < (int)numIndices; i += 3)
					{
						int[] idx = new int[3];
						if (indexSize == sizeof(ushort))
						{
							idx[0] = (int)pUShortData[i];
							idx[1] = (int)pUShortData[i + 1];
							idx[2] = (int)pUShortData[i + 2];
						}
						else
						{
							idx[0] = (int)pIndexData[i];
							idx[1] = (int)pIndexData[i + 1];
							idx[2] = (int)pIndexData[i + 2];
						}

						//C++ TO C# CONVERTER NOTE: The following #define macro was replaced in-line:
						//ORIGINAL LINE: #define IDX(_x_, _y_) ((_y_) * maxidx + (_x_))
#define IDX
						for (int j = 2, k = 0; k < 3; j = k++)
						{
							if (!chks[((idx[k]) * maxidx + (idx[j]))])
							{
								chks[((idx[k]) * maxidx + (idx[j]))] = true;
								chks[((idx[j]) * maxidx + (idx[k]))] = true;
								body_.appendLink(idx[j], idx[k]);
							}
						}
#undef IDX
						body_.appendFace(idx[0], idx[1], idx[2]);
					}
					indexBuffer.Unlock();
				}

				if (randomizeConstraints)
				{
					body_.randomizeConstraints();
				}

				// Straight out of Bullet's softbody demo for trimesh
				body_.m_materials[0].m_kLST = 0.1;
				body_.m_cfg.kMT = 0.05;

				btMatrix3x3 m = new btMatrix3x3();
				m.setEulerZYX(0, 0, 0);

				// Create methods for these
				body_.transform(btTransform(m, btVector3(0, 4, 0)));
				body_.scale(btVector3(2, 2, 2));
				body_.setTotalMass(50, true);
				body_.setPose(true, true);

				bConstructed = true;
			}

			AddBodyToWorld();
			SubscribeToEvent(E_POSTUPDATE, HANDLER(SoftBody, HandlePostUpdate));

			return bConstructed;
		}
		/// Return Bullet soft body.
		public btSoftBody GetBody()
		{
			return body_;
		}
		/// TODO.
		public void SetPosition(Vector3 position)
		{
			if (body_)
			{
				body_.transform(btTransform(btQuaternion.getIdentity(), ToBtVector3(position)));
				MarkNetworkUpdate();
			}
		}
		/// Handle node being assigned.
		public void OnNodeSet(Node node)
		{
			if (node != null)
			{
				node.AddListener(this);
			}
		}
		/// Handle scene being assigned.
		public void OnSceneSet(Scene scene)
		{
			if (scene != null)
			{
				if (scene == node_)
				{
					LOGWARNING(GetTypeName() + " should not be created to the root scene node");
				}
				physicsWorld_ = scene.GetOrCreateComponent<PhysicsWorld>();
				physicsWorld_.AddSoftBody(this);

				AddBodyToWorld();
			}
			else
			{
				ReleaseBody();

				if (physicsWorld_)
				{
					physicsWorld_.RemoveSoftBody(this);
				}
			}
		}
		/// Handle node transform being dirtied.
		//    virtual void OnMarkedDirty(Node* node);
		/// Create the soft body, or re-add to the physics world with changed flags. Calls UpdateMass().
		public void AddBodyToWorld()
		{
			if (!physicsWorld_)
				return;
			if (body_)
			{
				btSoftRigidDynamicsWorld world = (btSoftRigidDynamicsWorld)physicsWorld_.GetWorld();
				world.addSoftBody(body_);
			}
		}
		/// Remove the soft body from the physics world.
		public void RemoveBodyFromWorld()
		{
			if (body_)
			{
				if (physicsWorld_)
				{
					btSoftRigidDynamicsWorld pSoftRigidWorld = (btSoftRigidDynamicsWorld)physicsWorld_.GetWorld();
					pSoftRigidWorld.removeSoftBody(body_);
				}
			}
		}
		/// Physics world.
		private WeakPtr<PhysicsWorld> physicsWorld_ = new WeakPtr<PhysicsWorld>();
		/// Bullet soft body.
		private btSoftBody body_;
		/// Vertex buffer.
		private VertexBuffer vertexBuffer_;
	}
}
