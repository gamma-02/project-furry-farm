using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace ProjectFurryFarm;

[Tool, GlobalClass] //this should also run in the editor! we want to see our mesh, lol
public partial class MeshBuilder : MeshInstance3D
{
	[ExportToolButton("Rebuild Mesh")] public Callable RebuildMeshAction => Callable.From(RebuildMesh);
	
	private bool _isSphere = true;
	[Export]
	public bool IsSphere
	{
		get => _isSphere;
		set
		{
			_meshDirty = true;
			_isSphere = value;
		}
	}

	private bool _doCubeMarching = true;
	[Export]
	public bool DoCubeMarching
	{
		get => _doCubeMarching;
		set
		{
			_meshDirty = true;
			_doCubeMarching = value;
		}
	}

	private bool _useNoise = true;
	[Export]
	public bool UseNoise
	{
		get => _useNoise;
		set
		{
			_meshDirty = true;
			_useNoise = value;
		}
	}

	private Vector3I _chunks = new (1, 1, 1);
	[Export]
	public Vector3I Chunks
	{
		get => _chunks;
		set
		{
			_meshDirty = true;
			_chunks = value;
		}
	}

	private float _groundLevel = 0.639f;
	[Export(PropertyHint.Range, "0,1,-")]
	public float GroundLevel
	{
		get => _groundLevel;
		set
		{
			_meshDirty = true;
			_groundLevel = value;
		}
	}

	private Vector3 _noiseScale = new (1.0f, 1.0f, 1.0f);
	[Export]
	public Vector3 NoiseScale
	{
		get => _noiseScale;
		set
		{
			_meshDirty = true;
			_noiseScale = value;
		}
	}

	private Vector3 _noiseOffset;
	[Export]
	public Vector3 NoiseOffset
	{
		get => _noiseOffset;
		set
		{
			_meshDirty = true;
			_noiseOffset = value;
		}
	}

	private Noise _noise;
	[Export]
	public Noise Noise
	{
		get => _noise;
		set
		{
			_meshDirty = true;
			_noise = value;
		}
	}

	private bool _meshDirty = true;
	
	private BaseMaterial3D _thatch = ResourceLoader.Load<BaseMaterial3D>("res://Assets/Materials/ThatchRoof/testMaterial.tres");
	
	//Following two variables are for working collision mesh, to be combined with generated_collision.gd
	//marked as dirty once CollisionMesh is set.
	public bool CollisionMeshDirty = false; 
	public ConcavePolygonShape3D CollisionMesh = null;

	[Export] public bool UseFlatShading = false;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		BuildMesh();
		_meshDirty = false;

		CollisionMesh = Mesh.CreateTrimeshShape();
		CollisionMeshDirty = true;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!_meshDirty) return;
		
		BuildMesh();
		_meshDirty = false;
			
		CollisionMesh = Mesh.CreateTrimeshShape();
		CollisionMeshDirty = true;
	}

	
	public void RebuildMesh()
	{
		BuildMesh();
		_meshDirty = false;
		
		CollisionMesh = Mesh.CreateTrimeshShape();
		CollisionMeshDirty = true;
	}
	
	private Array _array;
	public override void _Notification(int what)
	{
		base._Notification(what);

#if TOOLS
		if (what == NotificationEditorPreSave && Mesh is ArrayMesh preSaveArrMesh && preSaveArrMesh.GetSurfaceCount() > 0)
		{
			_array = preSaveArrMesh.SurfaceGetArrays(0);
			preSaveArrMesh.ClearSurfaces();
		} else if (what == NotificationEditorPostSave && Mesh is ArrayMesh postSaveArrMesh)
		{
			postSaveArrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, _array);
			// postSaveArrMesh.SurfaceSetMaterial(0, _thatch);
			
			// postSaveArrMesh._Notification((int)NotificationLocalTransformChanged);

			_array = null;
		}
#endif
	}

	public void BuildMesh()
	{
		Godot.Collections.Array surface = [];
		surface.Resize((int)Mesh.ArrayType.Max);
		
		List<Vector3> verts = [];
		List<Vector2> uvs = [];
		List<Vector3> normals = [];
		List<int> indices = [];
		
		if(!_doCubeMarching)
		{
			//mesh building code (rn, just invoke the mesh info function)
			if (!_isSphere)
			{
				FillRectangleMeshInfo(verts, uvs, normals, indices);
			}
			else
			{
				FillSphericalMeshInfo(verts, uvs, normals, indices);
			}
		}
		else
		{
			// //for right now, in our starting marching cubes tests, chunk coord is 0, 0, 0
			// MarchingCubes cubes = new MarchingCubes();
			// cubes.ChunkOrigin = new Vector3(0, 0, 0); //* numPointsPerAxis - 1
			// cubes.GroundLevel = 0.639f;
			// GenerateChunk(cubes, verts, normals, indices);
			//
			// cubes = new MarchingCubes();
			// cubes.ChunkOrigin = new Vector3(8, 0, 0);
			// cubes.GroundLevel = 0.639f;
			// GenerateChunk(cubes, verts, normals, indices);

			for (int x = 0; x < _chunks.X; x++)
			{
				for (int y = 0; y < _chunks.Y; y++)
				{
					for (int z = 0; z < _chunks.Z; z++)
					{
						Vector3 origin = new Vector3(x, y, z) * 16.0f;
						MarchingCubes chunkProcessor = !_useNoise 
							? new MarchingCubes(origin, _noiseOffset, _noiseScale, _groundLevel) 
							: new MarchingCubes(origin, _noiseOffset, _noiseScale, _groundLevel, _noise);
						
						GenerateChunk(chunkProcessor, verts, normals, indices);
					}
				}
			}
			
		}
		
		
		surface[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
		// surface[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
		surface[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		surface[(int)Mesh.ArrayType.Index] = indices.ToArray();
		
		if (Mesh is ArrayMesh arrMesh)
		{
			//remove first surface (to swap surfaces)
			if(arrMesh.GetSurfaceCount() > 0)
			{
				arrMesh.ClearSurfaces();
			}
			
			// No blendshapes, lods, or compression used.
			arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surface);
			
			// arrMesh.SurfaceSetMaterial(0, _thatch);
			
			// To save mesh
			// ResourceSaver.Save(Mesh, "res://test.tres", ResourceSaver.SaverFlags.Compress);

		}
	}

	public void GenerateChunk(MarchingCubes cubes, List<Vector3> verts, List<Vector3> normals, List<int> indices)
	{
		for (int cx = 0; cx < 16; cx++)
		{
			for (int cy = 0; cy < 16; cy++)
			{
				for (int cz = 0; cz < 16; cz++)
				{
					cubes.ProcessCube(new Vector3I(cx, cy, cz));
					
				}
			}
		}

		Godot.Collections.Dictionary<Vector2I, int> vertexIndexMap = new();
		// List<Vector3> processedVertices = new();
		// List<Vector3> processedNormals = new();
		// List<int> processedTriangles = new();
		
		int triangleIndex = verts.Count;

		for (int i = 0; i < cubes.Triangles.Count * 3; i++)
		{
			MarchingCubes.Vertex v = cubes.Vertices[i];

			if (!UseFlatShading && vertexIndexMap.TryGetValue(v.Id, out int sharedVertexIndex))
			{
				indices.Add(sharedVertexIndex);
			}
			else
			{
				if (!UseFlatShading)
				{
					vertexIndexMap.Add(v.Id, triangleIndex);
				}
				verts.Add(v.Position);
				normals.Add(v.Normal);
				indices.Add(triangleIndex);
				triangleIndex++;
			}
			
			
		}
	}

	public void FillRectangleMeshInfo(List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> indices)
	{
		//vertex positions
		verts.AddRange([
			new Vector3(0.0f, 0.0f, 0.0f) + new Vector3(-0.5f, -0.5f, 0.0f), 
			new Vector3(0.0f, 1.0f, 0.0f) + new Vector3(-0.5f, -0.5f, 0.0f), 
			new Vector3(1.0f, 0.0f, 0.0f) + new Vector3(-0.5f, -0.5f, 0.0f), 
			new Vector3(1.0f, 1.0f, 0.0f) + new Vector3(-0.5f, -0.5f, 0.0f)
		]);
		
		//UV coordinates
		uvs.AddRange([
			new (0.0f, 0.0f), 
			new (0.0f, 1.0f), 
			new (1.0f, 0.0f), 
			new (1.0f, 1.0f)
		]);
		
		//Normals
		normals.AddRange([
			Vector3.Up,
			Vector3.Up,
			Vector3.Up,
			Vector3.Up,
		]);
		
		/*
		 * 0 | <0, 0, 0>
		 * 1 | <0, 1, 0>
		 * 2 | <1, 0, 0>
		 * 3 | <1, 1, 0>
		 */
		//vertex indices
		//!! wind vertices clockwise !!
		indices.AddRange([
			0, 1, 2,
			1, 3, 2
		]);
	}

	private int _rings = 50;

	[Export]
	public int Rings
	{
		get => _rings;
		set
		{
			_meshDirty = true;

			if (value <= 0)
				value = 1;
			
			_rings = value;
		}
	}
	
	private int _radialSegments = 50;
	
	[Export]
	public int RadialSegments
	{
		get => _radialSegments;
		set
		{
			_meshDirty = true;

			if (value <= 0)
				value = 1;
			
			_radialSegments = value;
		}
	}

	private float _radius = 1.0f;

	[Export]
	public float Radius
	{
		get => _radius;
		set
		{
			_meshDirty = true;
			_radius = value;
		}
	}

	public void FillSphericalMeshInfo(List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> indices)
	{
		//vertex indices
		int curRow = 0;
		int prevRow = 0;
		int point = 0;

		for (int i = 0; i < _rings + 1; i++)
		{
			float v = (float)i / _rings;
			float w = Mathf.Sin(Mathf.Pi * v);
			float y = Mathf.Cos(Mathf.Pi * v);

			for (int j = 0; j < _radialSegments + 1; j++)
			{
				float u = (float)j / _radialSegments;
				float x = Mathf.Sin(Mathf.Pi * u * 2);
				float z = Mathf.Cos(Mathf.Pi * u * 2);

				Vector3 vertPos = new Vector3(x * _radius * w, y * _radius, z * _radius * w);
				
				verts.Add(vertPos);
				normals.Add(vertPos.Normalized());
				uvs.Add(new Vector2(u, v));
				
				//update point here 
				point++;

				if (i > 0 && j > 0)
				{
					indices.AddRange([
						prevRow + j - 1,
						prevRow + j,
						curRow + j - 1,
						
						prevRow + j,
						curRow + j,
						curRow + j - 1
					]);
				}
			}

			prevRow = curRow;
			curRow = point;
		}
	}
}
