using UnityEngine;
using UnityEngine.Rendering;

public class WeldMeshHelper
{
    //configurable parameters
    private Material mWeldMeshMaterial = null;
    
    //output
    private int mTriangleCount;
    private ComputeBuffer mVertexBuffer = null;
    private Bounds mWeldedMeshBounds;
    
    //internal
    private CommandBuffer mWeldMeshCommandBuffer = null;
    private ComputeBuffer mVertexCounter = null;

    private bool mAreResourcesReady = false;

    ~WeldMeshHelper()
    {
        Cleanup();
    }

    public bool Initialize(Shader weldMeshShader, Vector3 rootPosition, Renderer[] sourceRenderers, Mesh[] sourceMeshes, bool[][] submeshToggles, float[] expansionsDistances = null, Texture[] expansionTextures = null)
    {
        //cleanup the old buffers
        Cleanup();

        if (weldMeshShader == null || sourceRenderers.Length == 0 || sourceRenderers.Length != sourceMeshes.Length || sourceRenderers.Length != submeshToggles.Length)
        {
            return false;
        }

        //count the number of indicies and triangles in all the meshes we are rendering
        int numIndicies = 0;
        for(int meshIndex = 0; meshIndex < sourceMeshes.Length; meshIndex++)
        {
            Mesh currentMesh = sourceMeshes[meshIndex];

            if (currentMesh != null)
            {
                for (int submeshIndex = 0; submeshIndex < currentMesh.subMeshCount; submeshIndex++)
                {
                    if (submeshIndex < submeshToggles[meshIndex].Length && submeshToggles[meshIndex][submeshIndex])
                    {
                        numIndicies += (int)currentMesh.GetIndexCount(submeshIndex);
                    }
                }
            }
        }

        //don't bother if there isn't at least one triangle
        if(numIndicies < 3)
        {
            return false;
        }

        mTriangleCount = numIndicies / 3;
        mWeldedMeshBounds = new Bounds(Vector3.zero, Vector3.zero);

        //setup the material and render resources
        mWeldMeshMaterial = new Material(weldMeshShader);

        mVertexBuffer = new ComputeBuffer(numIndicies, sizeof(float) * 3);
        mVertexCounter = new ComputeBuffer(1, sizeof(uint));

        mWeldMeshCommandBuffer = new CommandBuffer();
        mWeldMeshCommandBuffer.Clear();
        mWeldMeshCommandBuffer.SetRandomWriteTarget(1, mVertexBuffer);
        mWeldMeshCommandBuffer.SetRandomWriteTarget(2, mVertexCounter);
        mWeldMeshCommandBuffer.SetGlobalVector("_BakeMeshRootPosition", rootPosition);

        //render every renderer
        for (int rendererIndex = 0; rendererIndex < sourceRenderers.Length; rendererIndex++)
        {
            if (sourceRenderers[rendererIndex] != null && sourceMeshes[rendererIndex] != null)
            {
                //expand this mesh if there was an expansion value provided
                if(expansionsDistances != null && rendererIndex < expansionsDistances.Length)
                {
                    mWeldMeshCommandBuffer.SetGlobalFloat("_MeshExpansion", expansionsDistances[rendererIndex]);
                }
                else
                {
                    mWeldMeshCommandBuffer.SetGlobalFloat("_MeshExpansion", 0.0f);
                }

                //modify the expansion by a texture heightfield if one was provided
                if (expansionTextures != null && rendererIndex < expansionTextures.Length && expansionTextures[rendererIndex] != null)
                {
                    mWeldMeshCommandBuffer.SetGlobalTexture("_MeshExpansionTexture", expansionTextures[rendererIndex]);
                }
                else
                {
                    mWeldMeshCommandBuffer.SetGlobalTexture("_MeshExpansionTexture", Texture2D.whiteTexture);
                }

                //draw every enabled submesh
                for (int submeshIndex = 0; submeshIndex < sourceMeshes[rendererIndex].subMeshCount; submeshIndex++)
                {
                    if (submeshIndex < submeshToggles[rendererIndex].Length && submeshToggles[rendererIndex][submeshIndex])
                    {
                        mWeldMeshCommandBuffer.DrawRenderer(sourceRenderers[rendererIndex], mWeldMeshMaterial, submeshIndex);
                    }
                }
            }
        }

        mWeldMeshCommandBuffer.ClearRandomWriteTargets();

        mAreResourcesReady = true;
        return true;
    }

    public bool DoWork()
    {
        if(!mAreResourcesReady)
        {
            return false;
        }

        //initilize the vertex counter to 0
        uint[] zeroArray = { 0 };
        mVertexCounter.SetData(zeroArray, 0, 0, 1);

        //do the mesh welding
        Graphics.ExecuteCommandBuffer(mWeldMeshCommandBuffer);

        //get the verticies
        Vector3[] vertices = new Vector3[mVertexBuffer.count];
        mVertexBuffer.GetData(vertices, 0, 0, mVertexBuffer.count);

        //get the min and max bound of the welded mesh
        float largeNumber = 10000000.0f;
        Vector3 minCorner = new Vector3(largeNumber, largeNumber, largeNumber);
        Vector3 maxCorner = new Vector3(-largeNumber, -largeNumber, -largeNumber);

        foreach(Vector3 position in vertices)
        {
            minCorner = new Vector3(Mathf.Min(position.x, minCorner.x), Mathf.Min(position.y, minCorner.y), Mathf.Min(position.z, minCorner.z));
            maxCorner = new Vector3(Mathf.Max(position.x, maxCorner.x), Mathf.Max(position.y, maxCorner.y), Mathf.Max(position.z, maxCorner.z));
        }
        
        mWeldedMeshBounds = new Bounds((minCorner + maxCorner) / 2.0f, maxCorner - minCorner);

        return true;
    }

    public void Cleanup()
    {
        if (mVertexBuffer != null)
        {
            mVertexBuffer.Release();
        }

        if (mVertexCounter != null)
        {
            mVertexCounter.Release();
        }

        mWeldMeshMaterial = null;

        mAreResourcesReady = false;
    }

    //Getters

    public int GetTriangleCount()
    {
        return mTriangleCount;
    }
    
    public ComputeBuffer GetVertexBuffer()
    {
        return mVertexBuffer;
    }

    public Bounds GetWeldedMeshBounds()
    {
        return mWeldedMeshBounds;
    }
}
