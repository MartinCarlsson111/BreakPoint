using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;

public class BrickManager : MonoBehaviour
{
    static Brick[] bricks;

    public List<Material> materials;
    public List<Mesh> meshes;

    public uint2 setExtents;
    public uint2 extents = new uint2(10, 3);
    PolygonCollider2D polygonCollider;
    MeshFilter meshFilter;
    MeshFilter breakableMeshFilter;

    Mesh meshFilterMesh;
    Mesh quadMesh;
    Mesh breakableMesh;

    public bool randomSpawn = false;

    Camera mainCamera;

    private void Start()
    {
        extents.x = setExtents.x;
        extents.y = setExtents.y;

        var quadObject = Resources.Load("Quad");
        GameObject gameObject = quadObject as GameObject;


        quadMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        GenerateBricks();
        GenerateMesh();

        mainCamera = Camera.main;
    }

    void SetMeshes()
    {
        var breakable = transform.GetChild(0).GetComponent<Breakable>();
        breakable.Set(breakableMesh);
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = meshFilterMesh;
    }

    // Start is called before the first frame update

    void GenerateBricks()
    {
        bricks = new Brick[extents.x * extents.y];
        if (randomSpawn)
        {
            for (uint i = 0; i < (extents.x * extents.y) / 2U; i++)
            {
                uint pos = GetRandomPos();
                if (bricks[pos] == null)
                {
                    bricks[pos] = new Brick(UnityEngine.Random.Range(0, 2));
                }
            }
        }
        else
        {
            for (uint i = 0; i < (extents.x); i++)
            {
                for (int j = 0; j < extents.y; j++)
                {
                    bricks[i + (j * extents.x)] = new Brick(0);
                }
            }
        }
    }

    uint GetRandomPos()
    {
        return (uint)UnityEngine.Random.Range(0, (int)(extents.x * extents.y));
    }

    void GenerateMesh()
    {
        var boxColliders = GetComponents<BoxCollider2D>();
        foreach(var b in boxColliders)
        {
            Destroy(b);
        }
        List<int> nextState = new List<int>();
        List<List<int>> quads = new List<List<int>>
        {
            new List<int>(),
            new List<int>()
        };
        int currentState = 0;
        while (currentState < quads.Count)
        {
            for (int i = 0; i < bricks.Length; i++)
            {
                if(bricks[i] != null)
                {
                    if (currentState == bricks[i].state)
                    {
                        quads[currentState].Add(i);
                    }
                    else if (bricks[i].state != -1)
                    {
                        if (!nextState.Contains(bricks[i].state))
                        {
                            nextState.Add(bricks[i].state);
                        }
                    }
                }
            }
            currentState++;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        for (int i = 0; i < quads[0].Count; i++)
        {
            Vector3 pos = new Vector3(quads[0][i] % extents.x, quads[0][i] / extents.x, 0);
            int size = quadMesh.vertices.Length;
            for(int j = 0; j < size; j++)
            {
                vertices.Add(quadMesh.vertices[j] + pos);
            }
            for(int j = 0; j < quadMesh.triangles.Length; j++)
            {
                triangles.Add((i*quadMesh.vertices.Length) + quadMesh.triangles[j]);
            }
            for (int j = 0; j < quadMesh.uv.Length; j++)
            {
                uvs.Add(quadMesh.uv[j]);
            }

            var collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new float2(1.05f, 1);
            collider.offset = pos;
        }

        meshFilterMesh = new Mesh();
        meshFilterMesh.SetVertices(vertices);
        meshFilterMesh.SetTriangles(triangles, 0);
        meshFilterMesh.SetUVs(0, uvs);
        meshFilterMesh.RecalculateNormals();
        meshFilterMesh.RecalculateBounds();

        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        for (int i = 0; i < quads[1].Count; i++)
        {
            Vector3 pos = new Vector3(quads[1][i] % extents.x, quads[1][i] / extents.x, 0);
            int size = quadMesh.vertices.Length;
            for (int j = 0; j < size; j++)
            {
                vertices.Add(quadMesh.vertices[j] + pos);
            }
            for (int j = 0; j < quadMesh.triangles.Length; j++)
            {
                triangles.Add((i * quadMesh.vertices.Length) + quadMesh.triangles[j]);
            }
            for (int j = 0; j < quadMesh.uv.Length; j++)
            {
                uvs.Add(quadMesh.uv[j]);
            }

            var collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new float2(1.05f, 1);
            collider.offset = pos;
        }

        if (vertices.Count != 0)
        {
            breakableMesh = new Mesh();
            breakableMesh.SetVertices(vertices);
            breakableMesh.SetTriangles(triangles, 0);
            breakableMesh.SetUVs(0, uvs);
            breakableMesh.RecalculateNormals();
            breakableMesh.RecalculateBounds();
            
        }
        SetMeshes();
    }




    private void OnCollisionEnter2D(Collision2D collision)
    {
        mainCamera.GetComponent<CameraShake>().Shake();
        if((int)collision.otherCollider.offset.x + (int)collision.otherCollider.offset.y * extents.y < bricks.Length )
        {
            if (bricks[(int)collision.otherCollider.offset.x + (int)collision.otherCollider.offset.y * extents.y] != null)
            {
                if (bricks[(int)collision.otherCollider.offset.x + (int)collision.otherCollider.offset.y * extents.y].state != 1)
                {
                    bricks[(int)collision.otherCollider.offset.x + (int)collision.otherCollider.offset.y * extents.y].state = 1;
                    GenerateMesh();
                }
            }
        }


      

        /*
        Ball other;
        collision.otherCollider.TryGetComponent<Ball>(out other);
        if(other != null)
        {
            var contact = collision.GetContact(0);
            bricks[(int)contact.point.x + (int)contact.point.y].state = 1;
            //find contact

            //increment value on brick

            //update mesh

        }*/
    }

    Vector2 PushAway()
    {
        return Vector2.zero;
    }
}
