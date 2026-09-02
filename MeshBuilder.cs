using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

namespace ProjectFurryFarm;

[Tool, GlobalClass] //this should also run in the editor! we want to see our mesh, lol
public partial class MeshBuilder : MeshInstance3D
{
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
	private bool _meshDirty = true;

	//Following two variables are for working collision mesh, to be combined with generated_collision.gd
	//marked as dirty once CollisionMesh is set.
	[Export] public bool CollisionMeshDirty = false; 
	[Export] public ConcavePolygonShape3D CollisionMesh = null;
	
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
		if (_meshDirty)
		{
			BuildMesh();
			_meshDirty = false;
			
			CollisionMesh = Mesh.CreateTrimeshShape();
			CollisionMeshDirty = true;
		}
	}

	public void BuildMesh()
	{
		Godot.Collections.Array surface = [];
		surface.Resize((int)Mesh.ArrayType.Max);
		
		List<Vector3> verts = [];
		List<Vector2> uvs = [];
		List<Vector3> normals = [];
		List<int> indices = [];
		
		//mesh building code (rn, just invoke the mesh info function)
		if(!_isSphere)
		{
			FillRectangleMeshInfo(verts, uvs, normals, indices);
		}
		else
		{
			FillSphericalMeshInfo(verts, uvs, normals, indices);
		}
		
		surface[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
		surface[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
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

			// To save mesh
			// ResourceSaver.Save(Mesh, "res://test.tres", ResourceSaver.SaverFlags.Compress);
			
		}
	}

	public void FillRectangleMeshInfo(List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> indices)
	{
		//vertex positions
		verts.AddRange([
			new (0.0f, 0.0f, 0.0f), 
			new (0.0f, 1.0f, 0.0f), 
			new (1.0f, 0.0f, 0.0f), 
			new (1.0f, 1.0f, 0.0f)
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