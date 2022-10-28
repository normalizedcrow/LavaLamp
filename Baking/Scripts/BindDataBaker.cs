using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class BindDataBaker : MonoBehaviour
{
    public const int cMaxMaskColors = 16;

    //saved editor parameters
    [SerializeField] private GameObject mTargetObject = null;
    [SerializeField] private Renderer mTargetRenderer = null;
    [SerializeField] private Mesh mTargetMesh = null;

    [SerializeField] private Texture2D mMaskTexture = null;
    [SerializeField] private int mMaskColorCount = 1;
    [SerializeField] private Color[] mMaskColors = new Color[cMaxMaskColors];
    [SerializeField] private Color mInvalidMaskColor = Color.black;

    //shaders
    [SerializeField] private Shader mBakeBindDataShader;
    [SerializeField] private Shader mVisualizationShader;

    //GPU output textures
    private RenderTexture mBindPositionsTexture = null;
    private RenderTexture mBindNormalsTexture = null;
    private RenderTexture mBindTangentsTexture = null;

    //CPU output textures
    private Texture2D mBindPositionsOutputTexture = null;
    private Texture2D mBindNormalsOutputTexture = null;
    private Texture2D mBindTangentsOutputTexture = null;

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

    public bool SaveSettings(GameObject newTarget, Renderer newTargetRenderer, Mesh newTargetMesh, Texture2D newMasktexture, int newMaskColorCount, Color[] newMaskColors, Color newInvalidMaskColor)
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

        if(newMaskColorCount != mMaskColorCount)
        {
            mMaskColorCount = Mathf.Max(1, Mathf.Min(newMaskColorCount, cMaxMaskColors));
            didAnythingChange = true;
        }

        //only accept the new array of colors if it is the correct length
        if(newMaskColors.Length == mMaskColors.Length && !newMaskColors.SequenceEqual(mMaskColors))
        {
            newMaskColors.CopyTo(mMaskColors, 0);
            didAnythingChange = true;
        }

        //make sure the mask colors array stays the correct length
        if(mMaskColors.Length != cMaxMaskColors)
        {
            mMaskColors = new Color[cMaxMaskColors];
            didAnythingChange = true;
        }

        if (newInvalidMaskColor != mInvalidMaskColor)
        {
            mInvalidMaskColor = newInvalidMaskColor;
            didAnythingChange = true;
        }

        return didAnythingChange;
    }

    public void DoBake()
    {
        if (PrepareBake())
        {
            BakeTextures();
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

        if(mVisualizationCommandBuffer != null)
        {
            mVisualizationCommandBuffer.Release();
            mVisualizationCommandBuffer = null;
        }

        if (mBindPositionsTexture != null)
        {
            mBindPositionsTexture.Release();
            mBindPositionsTexture = null;
        }

        if (mBindNormalsTexture != null)
        {
            mBindNormalsTexture.Release();
            mBindNormalsTexture = null;
        }

        if (mBindTangentsTexture != null)
        {
            mBindTangentsTexture.Release();
            mBindTangentsTexture = null;
        }

        mBindPositionsOutputTexture = null;
        mBindNormalsOutputTexture = null;
        mBindTangentsOutputTexture = null;
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

        //find the smallest square texture that can fit all of the verts
        int textureWidth = Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Sqrt(mTargetMesh.vertexCount)));

        if (textureWidth <= 1)
        {
            return false;
        }

        mBakeBindDataMaterial = new Material(mBakeBindDataShader);
        mVisualizationMaterial = new Material(mVisualizationShader);

        //create the output rendertextures
        RenderTextureDescriptor textureDesc = new RenderTextureDescriptor(textureWidth, textureWidth, RenderTextureFormat.ARGBHalf, 0);
        textureDesc.useMipMap = false;
        textureDesc.autoGenerateMips = false;
        textureDesc.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        textureDesc.enableRandomWrite = true;
        textureDesc.width = textureWidth;
        textureDesc.height = textureWidth;

        mBindPositionsTexture = new RenderTexture(textureDesc);
        mBindNormalsTexture = new RenderTexture(textureDesc);
        mBindTangentsTexture = new RenderTexture(textureDesc);

        //create command buffer for writing out the bind data
        mBakeBindDataCommandBuffer = new CommandBuffer();
        mBakeBindDataCommandBuffer.Clear();
        mBakeBindDataCommandBuffer.SetGlobalInt("_OutputTextureWidth", textureWidth);
        mBakeBindDataCommandBuffer.SetGlobalVector("_BakeMeshRootPosition", mTargetObject.transform.position);

        //create command buffer for rendering the visualization
        mVisualizationCommandBuffer = new CommandBuffer();
        mVisualizationCommandBuffer.Clear();

        //if we have a mask texture set
        if (mMaskTexture != null)
        {
            //convert the mask colors to linear color space
            Vector4[] maskColors = new Vector4[cMaxMaskColors];
            for(int i = 0; i < cMaxMaskColors; i++)
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
        
        mBakeBindDataCommandBuffer.SetRandomWriteTarget(1, mBindPositionsTexture);
        mBakeBindDataCommandBuffer.SetRandomWriteTarget(2, mBindNormalsTexture);
        mBakeBindDataCommandBuffer.SetRandomWriteTarget(3, mBindTangentsTexture);

        //render every submesh
        for(int subMesh = 0; subMesh < mTargetMesh.subMeshCount; subMesh++)
        {
            mBakeBindDataCommandBuffer.DrawRenderer(mTargetRenderer, mBakeBindDataMaterial, subMesh);
            mVisualizationCommandBuffer.DrawRenderer(mTargetRenderer, mVisualizationMaterial, subMesh);
        }
        
        mBakeBindDataCommandBuffer.ClearRandomWriteTargets();

        mIsBakeReady = true;

        return true;
    }

    private void BakeTextures()
    {
        Graphics.ExecuteCommandBuffer(mBakeBindDataCommandBuffer);

        mBindPositionsOutputTexture = CreateCPUTexture(mBindPositionsTexture);
        mBindNormalsOutputTexture = CreateCPUTexture(mBindNormalsTexture);
        mBindTangentsOutputTexture = CreateCPUTexture(mBindTangentsTexture);
    }

    private Texture2D CreateCPUTexture(RenderTexture texture)
    {
        //copy the contents of a RenderTexture texture into a Texture2D
        if (texture != null && texture.IsCreated())
        {
            int width = texture.width;
            int height = texture.height;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBAHalf, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Point;
            tex.anisoLevel = 0;

            // Read screen contents into the texture
            Graphics.SetRenderTarget(texture);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            return tex;
        }

        return null;
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

    public Texture2D GetBindPositions()
    {
        return mBindPositionsOutputTexture;
    }

    public Texture2D GetBindNormals()
    {
        return mBindNormalsOutputTexture;
    }

    public Texture2D GetBindTangents()
    {
        return mBindTangentsOutputTexture;
    }

    public bool IsBakeFinished()
    {
        return mIsBakeReady
               && mBindPositionsOutputTexture != null
               && mBindNormalsOutputTexture != null
               && mBindTangentsOutputTexture != null;
    }
}
