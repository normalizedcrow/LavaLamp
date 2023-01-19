using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class BindDataBaker : MonoBehaviour
{
    public enum BindDataUVSlot
    {
        UV2 = 1,
        UV3 = 2,
        UV4 = 3,
        UV5 = 4,
        UV6 = 5,
        UV7 = 6,
        UV8 = 7
    };

    public const int cMaxMaskColors = 16;

    //saved editor parameters
    [SerializeField] private GameObject mTargetObject = null;
    [SerializeField] private Renderer mTargetRenderer = null;
    [SerializeField] private Mesh mTargetMesh = null;

    [SerializeField] private Texture2D mMaskTexture = null;
    [SerializeField] private int mMaskColorCount = 1;
    [SerializeField] private Color[] mMaskColors = new Color[cMaxMaskColors];
    [SerializeField] private Color mInvalidMaskColor = Color.black;

    [SerializeField] BindDataUVSlot mBindPositionsSlot = BindDataUVSlot.UV6;
    [SerializeField] BindDataUVSlot mBindNormalsSlot = BindDataUVSlot.UV7;
    [SerializeField] BindDataUVSlot mBindTangentsSlot = BindDataUVSlot.UV8;

    //shaders
    [SerializeField] private Shader mBakeBindDataShader;
    [SerializeField] private Shader mVisualizationShader;

    //Output
    private Mesh mOutputMesh = null;
    private ComputeBuffer mBindPositionsBuffer = null;
    private ComputeBuffer mBindNormalsBuffer = null;
    private ComputeBuffer mBindTangentsBuffer = null;

    //bake rendering
    private CommandBuffer mBakeBindDataCommandBuffer = null;
    private Material mBakeBindDataMaterial = null;

    //vizualization rendering
    private CommandBuffer mVisualizationCommandBuffer = null;
    private Material mVisualizationMaterial = null;

    private bool mIsBakeReady = false;
    
    private void OnDestroy()
    {
        CleanupAndReset();
    }

    //Bake Functions

    public bool SaveSettings(GameObject newTarget, Renderer newTargetRenderer, Mesh newTargetMesh,
                             Texture2D newMasktexture, int newMaskColorCount, Color[] newMaskColors, Color newInvalidMaskColor,
                             BindDataUVSlot newBindPositionsSlot, BindDataUVSlot newBindNormalsSlot, BindDataUVSlot newBindTangentsSlot)
    {
        bool didAnythingChange = false;

        if (newTarget != mTargetObject)
        {
            mTargetObject = newTarget;
            didAnythingChange = true;
        }

        if (newTargetRenderer != mTargetRenderer)
        {
            mTargetRenderer = newTargetRenderer;
            didAnythingChange = true;
        }

        if (newTargetMesh != mTargetMesh)
        {
            mTargetMesh = newTargetMesh;
            didAnythingChange = true;
        }

        if (newMasktexture != mMaskTexture)
        {
            mMaskTexture = newMasktexture;
            didAnythingChange = true;
        }

        if (newMaskColorCount != mMaskColorCount)
        {
            mMaskColorCount = Mathf.Max(1, Mathf.Min(newMaskColorCount, cMaxMaskColors));
            didAnythingChange = true;
        }

        //only accept the new array of colors if it is the correct length
        if (newMaskColors.Length == mMaskColors.Length && !newMaskColors.SequenceEqual(mMaskColors))
        {
            newMaskColors.CopyTo(mMaskColors, 0);
            didAnythingChange = true;
        }

        //make sure the mask colors array stays the correct length
        if (mMaskColors.Length != cMaxMaskColors)
        {
            mMaskColors = new Color[cMaxMaskColors];
            didAnythingChange = true;
        }

        if (newInvalidMaskColor != mInvalidMaskColor)
        {
            mInvalidMaskColor = newInvalidMaskColor;
            didAnythingChange = true;
        }

        if (newBindPositionsSlot != mBindPositionsSlot)
        {
            mBindPositionsSlot = newBindPositionsSlot;
            didAnythingChange = true;
        }

        if (newBindNormalsSlot != mBindNormalsSlot)
        {
            mBindNormalsSlot = newBindNormalsSlot;
            didAnythingChange = true;
        }

        if (newBindTangentsSlot != mBindTangentsSlot)
        {
            mBindTangentsSlot = newBindTangentsSlot;
            didAnythingChange = true;
        }

        return didAnythingChange;
    }

    public void DoBake()
    {
        if (PrepareBake())
        {
            BakeMesh();
        }
    }

    public void CleanupAndReset()
    {
        mIsBakeReady = false;

        mBakeBindDataMaterial = null;
        mVisualizationMaterial = null;

        if (mBakeBindDataCommandBuffer != null)
        {
            mBakeBindDataCommandBuffer.Release();
            mBakeBindDataCommandBuffer = null;
        }

        if (mVisualizationCommandBuffer != null)
        {
            mVisualizationCommandBuffer.Release();
            mVisualizationCommandBuffer = null;
        }

        if (mBindPositionsBuffer != null)
        {
            mBindPositionsBuffer.Release();
            mBindPositionsBuffer = null;
        }

        if (mBindNormalsBuffer != null)
        {
            mBindNormalsBuffer.Release();
            mBindNormalsBuffer = null;
        }

        if (mBindTangentsBuffer != null)
        {
            mBindTangentsBuffer.Release();
            mBindTangentsBuffer = null;
        }

        mOutputMesh = null;
    }

    //Helpers

    private bool PrepareBake()
    {
        //cleanup the old resources
        CleanupAndReset();

        if (mTargetObject == null || mTargetRenderer == null || mTargetMesh == null || mBakeBindDataShader == null || mVisualizationShader == null)
        {
            return false;
        }

        //all output UV channels must be different
        if (mBindPositionsSlot == mBindNormalsSlot || mBindPositionsSlot == mBindTangentsSlot || mBindNormalsSlot == mBindTangentsSlot)
        {
            return false;
        }

        mBakeBindDataMaterial = new Material(mBakeBindDataShader);
        mVisualizationMaterial = new Material(mVisualizationShader);

        mBindPositionsBuffer = new ComputeBuffer(mTargetMesh.vertexCount, 4 * sizeof(float));
        mBindNormalsBuffer = new ComputeBuffer(mTargetMesh.vertexCount, 4 * sizeof(float));
        mBindTangentsBuffer = new ComputeBuffer(mTargetMesh.vertexCount, 4 * sizeof(float));

        //create command buffer for writing out the bind data
        mBakeBindDataCommandBuffer = new CommandBuffer();
        mBakeBindDataCommandBuffer.Clear();
        mBakeBindDataCommandBuffer.SetGlobalVector("_BakeMeshRootPosition", mTargetObject.transform.position);

        //create command buffer for rendering the visualization
        mVisualizationCommandBuffer = new CommandBuffer();
        mVisualizationCommandBuffer.Clear();

        //if we have a mask texture set
        if (mMaskTexture != null)
        {
            //convert the mask colors to linear color space
            Vector4[] maskColors = new Vector4[cMaxMaskColors];
            for (int i = 0; i < cMaxMaskColors; i++)
            {
                maskColors[i] = mMaskColors[i].linear;
            }

            mBakeBindDataCommandBuffer.SetGlobalTexture("_MaskTexture", mMaskTexture);
            mBakeBindDataCommandBuffer.SetGlobalInt("_MaskColorCount", mMaskColorCount);
            mBakeBindDataCommandBuffer.SetGlobalVector("_InvalidMaskColor", mInvalidMaskColor);
            mBakeBindDataCommandBuffer.SetGlobalVectorArray("_MaskColors", maskColors);

            mVisualizationCommandBuffer.SetGlobalTexture("_MaskTexture", mMaskTexture);
            mVisualizationCommandBuffer.SetGlobalInt("_MaskColorCount", mMaskColorCount);
            mVisualizationCommandBuffer.SetGlobalVector("_InvalidMaskColor", mInvalidMaskColor);
            mVisualizationCommandBuffer.SetGlobalVectorArray("_MaskColors", maskColors);
        }
        else
        {
            //set the mask so that it always chooses the first mask index
            Vector4[] maskColors = new Vector4[cMaxMaskColors];
            maskColors[0] = Vector4.one;

            mBakeBindDataCommandBuffer.SetGlobalTexture("_MaskTexture", Texture2D.whiteTexture);
            mBakeBindDataCommandBuffer.SetGlobalInt("_MaskColorCount", 1);
            mBakeBindDataCommandBuffer.SetGlobalVector("_InvalidMaskColor", Vector4.zero);
            mBakeBindDataCommandBuffer.SetGlobalVectorArray("_MaskColors", maskColors);

            mVisualizationCommandBuffer.SetGlobalTexture("_MaskTexture", Texture2D.whiteTexture);
            mVisualizationCommandBuffer.SetGlobalInt("_MaskColorCount", 1);
            mVisualizationCommandBuffer.SetGlobalVector("_InvalidMaskColor", Vector4.zero);
            mVisualizationCommandBuffer.SetGlobalVectorArray("_MaskColors", maskColors);
        }
        
        mBakeBindDataCommandBuffer.SetRandomWriteTarget(1, mBindPositionsBuffer);
        mBakeBindDataCommandBuffer.SetRandomWriteTarget(2, mBindNormalsBuffer);
        mBakeBindDataCommandBuffer.SetRandomWriteTarget(3, mBindTangentsBuffer);

        //render every submesh
        for (int subMesh = 0; subMesh < mTargetMesh.subMeshCount; subMesh++)
        {
            mBakeBindDataCommandBuffer.DrawRenderer(mTargetRenderer, mBakeBindDataMaterial, subMesh);
            mVisualizationCommandBuffer.DrawRenderer(mTargetRenderer, mVisualizationMaterial, subMesh);
        }

        mBakeBindDataCommandBuffer.ClearRandomWriteTargets();

        mIsBakeReady = true;

        return true;
    }

    private void BakeMesh()
    {
        //run the baking compute shader
        Graphics.ExecuteCommandBuffer(mBakeBindDataCommandBuffer);

        //make a deep copy of the mesh
        mOutputMesh = Instantiate(mTargetMesh);
        mOutputMesh.name = mTargetMesh.name;

        //copy the baked vertex data into the appropriate uv channels 
        Vector4[] bindData = new Vector4[mBindPositionsBuffer.count];

        mBindPositionsBuffer.GetData(bindData);
        mOutputMesh.SetUVs((int)mBindPositionsSlot, bindData);

        mBindNormalsBuffer.GetData(bindData);
        mOutputMesh.SetUVs((int)mBindNormalsSlot, bindData);

        mBindTangentsBuffer.GetData(bindData);
        mOutputMesh.SetUVs((int)mBindTangentsSlot, bindData);
    }

    //Visualization

    public void RenderVisualization()
    {
        if (mIsBakeReady && mTargetObject != null && mTargetRenderer != null && mVisualizationMaterial != null)
        {
            Shader.SetGlobalMatrix("_VisualizationMatrix", transform.localToWorldMatrix);
            Shader.SetGlobalVector("_BakeMeshRootPosition", mTargetObject.transform.position);
            Graphics.ExecuteCommandBuffer(mVisualizationCommandBuffer);
        }
    }

    //Getters

    public GameObject GetTargetObject()
    {
        return mTargetObject;
    }

    public Renderer GetTargetRenderer()
    {
        return mTargetRenderer;
    }

    public Mesh GetTargetMesh()
    {
        return mTargetMesh;
    }

    public Texture2D GetMaskTexture()
    {
        return mMaskTexture;
    }

    public int GetMaskColorCount()
    {
        return mMaskColorCount;
    }

    public Color[] GetMaskColors()
    {
        return (Color[])mMaskColors.Clone();
    }

    public Color GetInvalidMaskColor()
    {
        return mInvalidMaskColor;
    }

    public BindDataUVSlot GetBindPositionsUVSlot()
    {
        return mBindPositionsSlot;
    }

    public BindDataUVSlot GetBindNormalsUVSlot()
    {
        return mBindNormalsSlot;
    }

    public BindDataUVSlot GetBindTangentsUVSlot()
    {
        return mBindTangentsSlot;
    }

    public Mesh GetMeshWithBindData()
    {
        return mOutputMesh;
    }

    public bool IsBakeFinished()
    {
        return mIsBakeReady && mOutputMesh != null;
    }
}
