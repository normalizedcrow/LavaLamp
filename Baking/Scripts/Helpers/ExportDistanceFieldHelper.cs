using UnityEngine;

public class ExportDistanceFieldHelper
{
    private const float cGroupWidth = 32.0f;
    private const int cSlicesPerIteration = 8;

    //configurable parameters
    private ComputeShader mCopyDistanceFieldSliceShader;

    //output
    private Texture3D mOutputTexture = null;

    //internal
    private int mUnsignedToSignedKernel = -1;
    private RenderTexture mSdfSliceTexture = null;
    private Texture2D mIntermediateCpuTexture = null;
    private Color[] mAllPixels = null;
    private Vector3Int mTextureSize;

    private int mWorkIndex = 0;
    private bool mAreResourcesReady = false;

    ~ExportDistanceFieldHelper()
    {
        Cleanup();
    }

    public bool Initialize(ComputeShader copyDistanceFieldSliceShader, RenderTexture sdfTexture)
    {
        //cleanup the old textures
        Cleanup();

        if (copyDistanceFieldSliceShader == null || sdfTexture == null || !sdfTexture.IsCreated())
        {
            return false;
        }

        mCopyDistanceFieldSliceShader = copyDistanceFieldSliceShader;
        mTextureSize = new Vector3Int(sdfTexture.width, sdfTexture.height, sdfTexture.volumeDepth);

        //create the textures
        RenderTextureDescriptor desc = new RenderTextureDescriptor(mTextureSize[0], mTextureSize[1], RenderTextureFormat.ARGBHalf, 0);
        desc.useMipMap = false;
        desc.autoGenerateMips = false;
        desc.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        desc.enableRandomWrite = true;

        mSdfSliceTexture = new RenderTexture(desc);
        mSdfSliceTexture.Create();

        mIntermediateCpuTexture = new Texture2D(mTextureSize[0], mTextureSize[1], TextureFormat.RGBAHalf, false);

        mAllPixels = new Color[mTextureSize[0] * mTextureSize[1] * mTextureSize[2]];

        //setup compute shader
        mUnsignedToSignedKernel = mCopyDistanceFieldSliceShader.FindKernel("CSMain");

        mCopyDistanceFieldSliceShader.SetVector("_TextureDimensions", (Vector3)mTextureSize);
        mCopyDistanceFieldSliceShader.SetTexture(mUnsignedToSignedKernel, "_Input", sdfTexture);
        mCopyDistanceFieldSliceShader.SetTexture(mUnsignedToSignedKernel, "_Output", mSdfSliceTexture);

        mAreResourcesReady = true;
        return true;
    }

    public bool DoWork()
    {
        if (!mAreResourcesReady)
        {
            return false;
        }

        //if we have copied all the pixels from the GPU, build them into a 3D texture
        if (mWorkIndex >= mTextureSize[2])
        {
            mOutputTexture = new Texture3D(mTextureSize[0], mTextureSize[1], mTextureSize[2], TextureFormat.RHalf, false);
            mOutputTexture.wrapMode = TextureWrapMode.Clamp;
            mOutputTexture.filterMode = FilterMode.Bilinear;
            mOutputTexture.anisoLevel = 0;
            mOutputTexture.SetPixels(mAllPixels);
            mOutputTexture.Apply();

            return true;
        }

        //copy some slices of the volume over to CPU memory
        for (int i = 0; i < cSlicesPerIteration && mWorkIndex < mTextureSize[2]; i++)
        {
            //copy a slice of the SDF texture to a 2D render texture
            mCopyDistanceFieldSliceShader.SetInt("_DepthSlice", mWorkIndex);
            mCopyDistanceFieldSliceShader.Dispatch(mUnsignedToSignedKernel, Mathf.CeilToInt(mTextureSize[0] / cGroupWidth), Mathf.CeilToInt(mTextureSize[1] / cGroupWidth), 1);

            //copy the pixels from the 2D render texture to a 3D texture
            Graphics.SetRenderTarget(mSdfSliceTexture);
            mIntermediateCpuTexture.ReadPixels(new Rect(0, 0, mTextureSize[0], mTextureSize[1]), 0, 0);
            mIntermediateCpuTexture.Apply();

            //now copy the pixels from the 2D texture into an array (yes all these steps are necessary)
            Color[] slicePixels = mIntermediateCpuTexture.GetPixels();
            slicePixels.CopyTo(mAllPixels, mTextureSize[0] * mTextureSize[1] * mWorkIndex);

            mWorkIndex++;
        }

        return false;
    }

    public void Cleanup()
    {
        if (mSdfSliceTexture != null)
        {
            mSdfSliceTexture.Release();
        }

        mAreResourcesReady = false;
        mWorkIndex = 0;
    }

    //Getters

    public Texture3D GetOutputexture()
    {
        return mOutputTexture;
    }

    public float GetPercentageDone()
    {
        if (!mAreResourcesReady)
        {
            return 0.0f;
        }

        return mWorkIndex / (float)mTextureSize[2];
    }
}
