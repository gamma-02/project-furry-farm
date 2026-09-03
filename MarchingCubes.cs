using System;
using System.Collections.Generic;
using Godot;
using static ProjectFurryFarm.Assets.Scripts.MarchTables;

namespace ProjectFurryFarm;

public class MarchingCubes
{

    public record Vertex(Vector3 Position, Vector3 Normal, Vector2I Id);

    public record Triangle(Vertex C, Vertex B, Vertex A);
    
    public int NumPointsPerAxis; //for now

    public Vector3 ChunkOrigin;
    public float GroundLevel; // also known as iso level

    public List<Triangle> Triangles = new List<Triangle>(); //for now - will change later
    public List<Vertex> Vertices = new List<Vertex>();

    public MarchingCubes(Vector3 chunkOrigin, float groundLevel, int numPointsPerAxis = 10)
    {
        ChunkOrigin = chunkOrigin;
        GroundLevel = groundLevel;
        NumPointsPerAxis = numPointsPerAxis;
    }


    //I don't know why this is here. We do not have a planet size. We do not have a texture size. What.
    Vector3 CoordToWorld(Vector3I coord)
    {
        return new Vector3(coord.X, coord.Y, coord.Z) /*- new Vector3(0.5f, 0.5f, 0.5f)*/; // is this transform needed?
    }

    int IndexFromCoord(Vector3I coord)
    {
        coord = coord - new Vector3I((int)ChunkOrigin.X, (int)ChunkOrigin.Y, (int)ChunkOrigin.Z);
        return coord.Z * NumPointsPerAxis * NumPointsPerAxis + coord.Y * NumPointsPerAxis + coord.X;
    }
    
    //this is where density sampling happens!
    //right now, it's not going to be a 3D texture. though we'll see. 
    //issue w/ 3d texture is that it has to be definedf or
    //oh wait i can scale the mesh down after it's generated
    //nvm we're good. ti could be a 3d texture, lol
    float SampleDensity(Vector3I pos)
    {
        return Mathf.Cos(3 * pos.X) - 2 * Mathf.Sin(pos.Y) + Mathf.Cos(Mathf.Sin(pos.Y) * pos.Z);
    }

    Vector3 CalculateNormal(Vector3I coord)
    {
        Vector3I offsetX = new(1, 0, 0);
        Vector3I offsetY = new(0, 1, 0);
        Vector3I offsetZ = new(0, 0, 1);

        float dx = SampleDensity(coord + offsetX) - SampleDensity(coord - offsetX);
        float dy = SampleDensity(coord + offsetY) - SampleDensity(coord - offsetY);
        float dz = SampleDensity(coord + offsetZ) - SampleDensity(coord - offsetZ);

        return -(new Vector3(dx, dy, dz).Normalized());
    }

    Vertex CreateVertex(Vector3I coordA, Vector3I coordB)
    {
        Vector3 posA = CoordToWorld(coordA);
        Vector3 posB = CoordToWorld(coordB);
        float densityA = SampleDensity(coordA);
        float densityB = SampleDensity(coordB);

        float t = (GroundLevel - densityA) / (densityB - densityA);
        Vector3 pos = posA + t * (posB - posA);

        Vector3 normalA = CalculateNormal(coordA);
        Vector3 normalB = CalculateNormal(coordB);
        Vector3 normal = (normalA + t * (normalB - normalA)).Normalized();

        int indexA = IndexFromCoord(coordA);
        int indexB = IndexFromCoord(coordB);

        Vertex vertex = new(
            pos, 
            normal, 
            new Vector2I(Math.Min(indexA, indexB), Math.Max(indexA, indexB))
        );

        return vertex;

    }
    
    public void ProcessCube(Vector3I id) // cube? id (from/for shaders
    {
        int numCubesPerAxis = NumPointsPerAxis - 1;
        if (id.X >= numCubesPerAxis || id.Y >= numCubesPerAxis || id.Z >= numCubesPerAxis)
        {
            return;
        }

        Vector3I coord = id + new Vector3I((int)ChunkOrigin.X, (int)ChunkOrigin.Y, (int)ChunkOrigin.Z);

        Vector3I[] cornerCoords = new Vector3I[8];
        cornerCoords[0] = coord + new Vector3I(0, 0, 0);
        cornerCoords[1] = coord + new Vector3I(1, 0, 0);
        cornerCoords[2] = coord + new Vector3I(1, 0, 1);
        cornerCoords[3] = coord + new Vector3I(0, 0, 1);
        cornerCoords[4] = coord + new Vector3I(0, 1, 0);
        cornerCoords[5] = coord + new Vector3I(1, 1, 0);
        cornerCoords[6] = coord + new Vector3I(1, 1, 1);
        cornerCoords[7] = coord + new Vector3I(0, 1, 1);

        int cubeConfiguration = 0;
        for (int i = 0; i < 8; i++)
        {
            // Think of the configuration as an 8-bit binary number (each bit represents the state of a corner point).
            // The state of each corner point is either 0: above the surface, or 1: below the surface.
            // The code below sets the corresponding bit to 1, if the point is below the surface.
            
            if (SampleDensity(cornerCoords[i]) >= GroundLevel)
                continue;
            
            cubeConfiguration |= (1 << i);
            
        }
        
        //get edge array for the cube that the surface passes through
        int[] edgeIndices = Triangulation[cubeConfiguration];

        for (int i = 0; i < 16; i += 3)
        {
            //if edge index is -1, then no more vertices to process
            if (edgeIndices[i] == -1)
            {
                break;
            }

            int edgeIndexA = edgeIndices[i];
            int a0 = CornerIndexAFromEdge[edgeIndexA];
            int a1 = CornerIndexBFromEdge[edgeIndexA];

            int edgeIndexB = edgeIndices[i + 1];
            int b0 = CornerIndexAFromEdge[edgeIndexB];
            int b1 = CornerIndexBFromEdge[edgeIndexB];

            int edgeIndexC = edgeIndices[i + 2];
            int c0 = CornerIndexAFromEdge[edgeIndexC];
            int c1 = CornerIndexBFromEdge[edgeIndexC];

            var vertexA = CreateVertex(cornerCoords[a0], cornerCoords[a1]);
            var vertexB = CreateVertex(cornerCoords[b0], cornerCoords[b1]);
            var vertexC = CreateVertex(cornerCoords[c0], cornerCoords[c1]);
            
            Triangles.Add(new Triangle(vertexC, vertexB, vertexA));
            Vertices.AddRange([vertexA, vertexB, vertexC]);

        }

    }
    
}