using System;
using UnityEngine;

public class MeshToDistanceFieldHelper
{
    private const int cMaxTrianglesPerIter = 1024;
    private const int cMaxPixelsPerIter = 2048 * 2048;
    private const int cGroupWidth = 4;

    //configurable parameters
    private ComputeShader mMeshToDistanceFieldShader = null;
    private Vector3 mMinCorner;
    private int mTriangleCount;
    private float mPixelSize;

    //output
    private RenderTexture mDistanceTexture = null;

    //internal
    private int mComputeKernel = -1;
    private Vector2Int mDispatchSize;
    private int mNumLayersPerDispatch;
    private int mSubDispatchesForPixels;
    private int mTotalDispatches;

    private int mWorkIndex = 0;
    private bool mAreResourcesReady = false;

    ~MeshToDistanceFieldHelper()
    {
        Cleanup();
    }
    
    public bool Initialize(ComputeShader distanceFieldGenerationShader, Vector3Int textureDimensions, Vector3 minCorner, float pixelSize, int numTriangles, ComputeBuffer vertexBuffer)
    {
        //cleanup the old texture
        Cleanup();

        if (distanceFieldGenerationShader == null || vertexBuffer == null || !vertexBuffer.IsValid())
        {
            return false;
        }

        mMeshToDistanceFieldShader = distanceFieldGenerationShader;
        mMinCorner = minCorner;
        mTriangleCount = numTriangles;
        mPixelSize = pixelSize;

        //create the distance field texture
        RenderTextureDescriptor desc = new RenderTextureDescriptor(textureDimensions[0], textureDimensions[1], RenderTextureFormat.RHalf, 0);
        desc.useMipMap = false;
        desc.autoGenerateMips = false;
        desc.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        desc.volumeDepth = textureDimensions[2];
        desc.enableRandomWrite = true;

        mDistanceTexture = new RenderTexture(desc);
        mDistanceTexture.Create();

        //setup compute shader
        mComputeKernel = mMeshToDistanceFieldShader.FindKernel("CSMain");

        mMeshToDistanceFieldShader.SetVector("_MinCorner", mMinCorner);
        mMeshToDistanceFieldShader.SetFloat("_PixelSize", mPixelSize);
        mMeshToDistanceFieldShader.SetVector("_TextureDimensions", (Vector3)textureDimensions);

        mMeshToDistanceFieldShader.SetBuffer(mComputeKernel, "_MeshVerts", vertexBuffer);
        mMeshToDistanceFieldShader.SetTexture(mComputeKernel, "_Output", mDistanceTexture);

        mDispatchSize.x = (mDistanceTexture.width + (cGroupWidth - 1)) / cGroupWidth; //round up
        mDispatchSize.y = (mDistanceTexture.height + (cGroupWidth - 1)) / cGroupWidth;

        int minPixelsInDispatchSlice = mDistanceTexture.width * mDistanceTexture.height * cGroupWidth;
        mNumLayersPerDispatch = ((cMaxPixelsPerIter + (minPixelsInDispatchSlice - 1)) / minPixelsInDispatchSlice) * cGroupWidth; //total texture layers we can fit in one dispatch
        mSubDispatchesForPixels = (mDistanceTexture.volumeDepth + (mNumLayersPerDispatch - 1)) / mNumLayersPerDispatch; //how many dispatches are needed to process every pixel

        int subDispatchesForTriangles = (mTriangleCount + (cMaxTrianglesPerIter - 1)) / cMaxTrianglesPerIter; //how many dispatches are needed to process every triangle for a pixel
        mTotalDispatches = mSubDispatchesForPixels * subDispatchesForTriangles;

        mAreResourcesReady = true;
        return true;
    }
    public bool DoWork()
    {
        if (!mAreResourcesReady)
        {
            return false;
        }

        if (mWorkIndex < mTotalDispatches)
        {
            //iterate over pixels, then triangles  
            int triangleWorkIndex = mWorkIndex / mSubDispatchesForPixels;
            int pixelWorkIndex = mWorkIndex % mSubDispatchesForPixels;

            int triangleOffset = triangleWorkIndex * cMaxTrianglesPerIter;
            mMeshToDistanceFieldShader.SetInt("_NumTriangles", Math.Min(mTriangleCount - triangleOffset, cMaxTrianglesPerIter));
            mMeshToDistanceFieldShader.SetInt("_TriangleOffset", triangleOffset);
            mMeshToDistanceFieldShader.SetVector("_PixelOffset", new Vector3(0, 0, pixelWorkIndex * mNumLayersPerDispatch));

            mMeshToDistanceFieldShader.Dispatch(mComputeKernel, mDispatchSize.x, mDispatchSize.y, mNumLayersPerDispatch / cGroupWidth);

            mWorkIndex++;
            return false;
        }

        return true;
    }

    public void Cleanup()
    {
        if (mDistanceTexture != null)
        {
            mDistanceTexture.Release();
        }

        mAreResourcesReady = false;
        mWorkIndex = 0;
    }

    //Getters

    public RenderTexture GetDistanceField()
    {
        return mDistanceTexture;
    }

    public float GetPercentageDone()
    {
        if(!mAreResourcesReady)
        {
            return 0.0f;
        }

        return mWorkIndex / (float)mTotalDispatches;
    }
}
