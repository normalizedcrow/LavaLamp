using System;
using UnityEngine;

public class UnsignedToSignedDistanceFieldHelper
{
    private const int cMaxPixelsPerIter = 2048 * 2048;
    private const int cGroupWidth = 4;

    //configurable parameters
    private ComputeShader mUnsignedToSignedConversionShader = null;
    private float mPixelSize;

    //output
    private RenderTexture mDistanceTexturePing = null;
    private RenderTexture mDistanceTexturePong = null;

    //internal
    private int mUnsignedToSignedKernel = -1;
    private Vector2Int mDispatchSize;
    private int mNumLayersPerDispatch;
    private int mSubDispatchesForPixels;
    private int mTotalPingPongs;

    private int mWorkIndex = 0;
    private bool mAreResourcesReady = false;

    ~UnsignedToSignedDistanceFieldHelper()
    {
        Cleanup();
    }
    
    public bool Initialize(ComputeShader unsignedToSignedConversionShader, float pixelSize, RenderTexture unsignedTexture)
    {
        //cleanup the old textures
        Cleanup();

        if (unsignedToSignedConversionShader == null || unsignedTexture == null || !unsignedTexture.IsCreated())
        {
            return false;
        }

        mUnsignedToSignedConversionShader = unsignedToSignedConversionShader;
        mPixelSize = pixelSize;

        //create the distance field texture
        RenderTextureDescriptor desc = new RenderTextureDescriptor(unsignedTexture.width, unsignedTexture.height, RenderTextureFormat.RHalf, 0);
        desc.useMipMap = false;
        desc.autoGenerateMips = false;
        desc.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        desc.volumeDepth = unsignedTexture.volumeDepth;
        desc.enableRandomWrite = true;
        
        mDistanceTexturePing = new RenderTexture(desc);
        mDistanceTexturePing.Create();

        mDistanceTexturePong = new RenderTexture(desc);
        mDistanceTexturePong.Create();
        
        //setup compute shader
        mUnsignedToSignedKernel = mUnsignedToSignedConversionShader.FindKernel("CSMain");

        mUnsignedToSignedConversionShader.SetFloat("_PixelSize", mPixelSize);
        mUnsignedToSignedConversionShader.SetVector("_TextureDimensions", new Vector3(unsignedTexture.width, unsignedTexture.height, unsignedTexture.volumeDepth));
        mUnsignedToSignedConversionShader.SetTexture(mUnsignedToSignedKernel, "_Input", unsignedTexture);

        mDispatchSize.x = (mDistanceTexturePing.width + (cGroupWidth - 1)) / cGroupWidth; //round up
        mDispatchSize.y = (mDistanceTexturePing.height + (cGroupWidth - 1)) / cGroupWidth;

        int minPixelsInDispatch = mDistanceTexturePing.width * mDistanceTexturePing.height * cGroupWidth;
        mNumLayersPerDispatch = ((cMaxPixelsPerIter + (minPixelsInDispatch - 1)) / minPixelsInDispatch) * cGroupWidth; //total texture layers we can fit in one dispatch
        mSubDispatchesForPixels = (mDistanceTexturePing.volumeDepth + (mNumLayersPerDispatch - 1)) / mNumLayersPerDispatch; //how many dispatches are needed to process every pixel
        mTotalPingPongs = Math.Max(Math.Max(mDistanceTexturePing.width, mDistanceTexturePing.height), mDistanceTexturePing.volumeDepth) * 2; //do twice as many iterations at the max texture dimension

        mAreResourcesReady = true;
        return true;
    }

    public bool DoWork()
    {
        if (!mAreResourcesReady)
        {
            return false;
        }

        //iterate over all pixels, then flip the source and destination and repeat
        int pingPongIndex = mWorkIndex / mSubDispatchesForPixels;
        int pixelWorkIndex = mWorkIndex % mSubDispatchesForPixels;

        //be sure to do an even ammount of copies so the result always ends up in the same place
        if ((pingPongIndex % 2 == 0) && (pingPongIndex > mTotalPingPongs))
        {
            return true;
        }

        mUnsignedToSignedConversionShader.SetVector("_PixelOffset", new Vector3(0, 0, pixelWorkIndex * mNumLayersPerDispatch));

        //don't set the input on the first iteration because we are copying from the unsigned texture
        if (pingPongIndex != 0)
        {
            mUnsignedToSignedConversionShader.SetTexture(mUnsignedToSignedKernel, "_Input", (pingPongIndex % 2 == 0) ? mDistanceTexturePing : mDistanceTexturePong);
        }
        mUnsignedToSignedConversionShader.SetTexture(mUnsignedToSignedKernel, "_Output", (pingPongIndex % 2 == 0) ? mDistanceTexturePong : mDistanceTexturePing);

        mUnsignedToSignedConversionShader.Dispatch(mUnsignedToSignedKernel, mDispatchSize.x, mDispatchSize.y, mNumLayersPerDispatch / cGroupWidth);

        mWorkIndex++;
        return false;
    }

    public void Cleanup()
    {
        if (mDistanceTexturePing != null)
        {
            mDistanceTexturePing.Release();
        }

        if (mDistanceTexturePong != null)
        {
            mDistanceTexturePong.Release();
        }

        mAreResourcesReady = false;
        mWorkIndex = 0;
    }

    //Getters

    public RenderTexture GetDistanceField()
    {
        return mDistanceTexturePing;
    }

    public float GetPercentageDone()
    {
        if (!mAreResourcesReady)
        {
            return 0.0f;
        }

        return mWorkIndex / (float)(mTotalPingPongs * mSubDispatchesForPixels);
    }
}
