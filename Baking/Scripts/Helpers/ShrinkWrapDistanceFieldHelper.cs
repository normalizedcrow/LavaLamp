using UnityEngine;

public class ShrinkWrapDistanceFieldHelper
{
    private const int cMaxPixelsPerIter = 2048 * 2048;
    private const int cMaxCellsPerIter = 1024;
    private const int cGroupWidth = 4;

    //configurable parameters
    private ComputeShader mShrinkWrapDistanceFieldShader = null;
    private float mPixelSize;
    private float mSmoothingRadus;
    private int mNumCellsInExpandedSurface;

    //output
    private RenderTexture mShrinkWrappedDistanceTexture;

    //internal
    private int mGetSurfaceKernel = -1;
    private int mRecalculateDistaceKernel = -1;
    private ComputeBuffer mExpandedSurfaceCells = null;
    private ComputeBuffer mExpandedSurfaceCellCount = null;

    private Vector2Int mDispatchSize;
    private int mNumLayersPerDispatch;
    private int mSubDispatchesForPixels;

    private int mWorkIndex = 0;
    private bool mAreResourcesReady = false;

    ~ShrinkWrapDistanceFieldHelper()
    {
        Cleanup();
    }
    
    public bool Initialize(ComputeShader shrinkWrapDistanceFieldShader, float pixelSize, float smoothingRadus, RenderTexture sdfTexture)
    {
        //cleanup the old textures and buffers
        Cleanup();

        if (shrinkWrapDistanceFieldShader == null || sdfTexture == null || !sdfTexture.IsCreated())
        {
            return false;
        }

        mShrinkWrapDistanceFieldShader = shrinkWrapDistanceFieldShader;
        mPixelSize = pixelSize;
        mSmoothingRadus = smoothingRadus;
        mNumCellsInExpandedSurface = -1;

        //create the textures
        RenderTextureDescriptor desc = new RenderTextureDescriptor(sdfTexture.width, sdfTexture.height, RenderTextureFormat.RHalf, 0);
        desc.useMipMap = false;
        desc.autoGenerateMips = false;
        desc.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        desc.volumeDepth = sdfTexture.volumeDepth;
        desc.enableRandomWrite = true;

        mShrinkWrappedDistanceTexture = new RenderTexture(desc);
        mShrinkWrappedDistanceTexture.Create();

        //create the buffers
        mExpandedSurfaceCells = new ComputeBuffer(sdfTexture.width * sdfTexture.height * sdfTexture.volumeDepth, sizeof(float) * 11);
        mExpandedSurfaceCellCount = new ComputeBuffer(1, sizeof(int));

        //initilize the surrface cell counter to 0
        uint[] zeroArray = { 0 };
        mExpandedSurfaceCellCount.SetData(zeroArray, 0, 0, 1);

        //setup compute shaders
        mGetSurfaceKernel = mShrinkWrapDistanceFieldShader.FindKernel("GetExpandedSurface");
        mRecalculateDistaceKernel = mShrinkWrapDistanceFieldShader.FindKernel("RecalculateDistance");

        mShrinkWrapDistanceFieldShader.SetFloat("_PixelSize", mPixelSize);
        mShrinkWrapDistanceFieldShader.SetVector("_TextureDimensions", new Vector3(sdfTexture.width, sdfTexture.height, sdfTexture.volumeDepth));
        mShrinkWrapDistanceFieldShader.SetFloat("_ExpansionSize", mSmoothingRadus);

        mShrinkWrapDistanceFieldShader.SetTexture(mGetSurfaceKernel, "_Input", sdfTexture);
        mShrinkWrapDistanceFieldShader.SetBuffer(mGetSurfaceKernel, "_ExpandedSurfaceCells", mExpandedSurfaceCells);
        mShrinkWrapDistanceFieldShader.SetBuffer(mGetSurfaceKernel, "_ExpandedSurfaceCellCount", mExpandedSurfaceCellCount);

        mShrinkWrapDistanceFieldShader.SetTexture(mRecalculateDistaceKernel, "_Input", sdfTexture);
        mShrinkWrapDistanceFieldShader.SetTexture(mRecalculateDistaceKernel, "_Output", mShrinkWrappedDistanceTexture);
        mShrinkWrapDistanceFieldShader.SetBuffer(mRecalculateDistaceKernel, "_ExpandedSurfaceCells", mExpandedSurfaceCells);

        mDispatchSize.x = (mShrinkWrappedDistanceTexture.width + (cGroupWidth - 1)) / cGroupWidth; //round up
        mDispatchSize.y = (mShrinkWrappedDistanceTexture.height + (cGroupWidth - 1)) / cGroupWidth;

        int minPixelsInDispatch = mShrinkWrappedDistanceTexture.width * mShrinkWrappedDistanceTexture.height * cGroupWidth;
        mNumLayersPerDispatch = ((cMaxPixelsPerIter + (minPixelsInDispatch - 1)) / minPixelsInDispatch) * cGroupWidth; //total texture layers we can fit in one dispatch
        mSubDispatchesForPixels = (mShrinkWrappedDistanceTexture.volumeDepth + (mNumLayersPerDispatch - 1)) / mNumLayersPerDispatch; //how many dispatches are needed to process every pixel

        mAreResourcesReady = true;
        return true;
    }

    public bool DoWork()
    {
        if (!mAreResourcesReady)
        {
            return false;
        }

        //after we have processed every pixel in the expansion stage we can move on to the shrinkwrapping stage
        bool isFirstStageDone = mWorkIndex >= mSubDispatchesForPixels; 
        
        if (!isFirstStageDone)
        {
            mShrinkWrapDistanceFieldShader.SetVector("_PixelOffset", new Vector3(0, 0, mWorkIndex * mNumLayersPerDispatch));
            mShrinkWrapDistanceFieldShader.Dispatch(mGetSurfaceKernel, mDispatchSize.x, mDispatchSize.y, mNumLayersPerDispatch / cGroupWidth);
        }
        else
        {
            //only get the cell count once
            if(mNumCellsInExpandedSurface < 0)
            {
                int[] cellCount = new int[1];
                mExpandedSurfaceCellCount.GetData(cellCount, 0, 0, 1);
                mNumCellsInExpandedSurface = cellCount[0];

                //just stop if there is no expanded surface
                if (mNumCellsInExpandedSurface <= 0)
                {
                    return true;
                }
            }

            //can't calculate this until expansion is done
            int subDispatchesForCells = (mNumCellsInExpandedSurface + (cMaxCellsPerIter - 1)) / cMaxCellsPerIter;

            //iterate over expanded cells, then pixels
            int thisStageWorkIndex = (mWorkIndex - mSubDispatchesForPixels);
            int cellWorkIndex = thisStageWorkIndex % subDispatchesForCells;
            int pixelWorkIndex = thisStageWorkIndex / subDispatchesForCells;

            //stop if we have evaluated every cell for every pixel
            if(pixelWorkIndex >= mSubDispatchesForPixels)
            {
                return true;
            }

            mShrinkWrapDistanceFieldShader.SetInt("_TotalCellCount", mNumCellsInExpandedSurface);
            mShrinkWrapDistanceFieldShader.SetInt("_CellOffset", cellWorkIndex * cMaxCellsPerIter);
            mShrinkWrapDistanceFieldShader.SetVector("_PixelOffset", new Vector3(0, 0, pixelWorkIndex * mNumLayersPerDispatch));

            mShrinkWrapDistanceFieldShader.Dispatch(mRecalculateDistaceKernel, mDispatchSize.x, mDispatchSize.y, mNumLayersPerDispatch / cGroupWidth);
        }
        
        mWorkIndex++;

        return false;
    }

    public void Cleanup()
    {
        if (mShrinkWrappedDistanceTexture != null)
        {
            mShrinkWrappedDistanceTexture.Release();
        }

        if (mExpandedSurfaceCells != null)
        {
            mExpandedSurfaceCells.Release();
        }

        if (mExpandedSurfaceCellCount != null)
        {
            mExpandedSurfaceCellCount.Release();
        }

        mAreResourcesReady = false;
        mWorkIndex = 0;
    }

    //Getters

    public RenderTexture GetDistanceField()
    {
        return mShrinkWrappedDistanceTexture;
    }

    public float GetPercentageDone()
    {
        if (!mAreResourcesReady || mNumCellsInExpandedSurface < 0)
        {
            return 0.0f;
        }

        int subDispatchesForCells = (mNumCellsInExpandedSurface + (cMaxCellsPerIter - 1)) / cMaxCellsPerIter;
        return mWorkIndex  / (float)((subDispatchesForCells + 1) * mSubDispatchesForPixels);
    }
}
