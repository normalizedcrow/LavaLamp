using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SDFBaker : MonoBehaviour
{
    [System.Serializable]
    public struct TargetRendererInfo //container for all data about a specific renderer we want to bake
    {
        public Renderer mRenderer;
        public Mesh mMesh;
        public bool mEnabled;
        public bool[] mSubmeshToggles;

        public TargetRendererInfo(Renderer renderer, Mesh mesh)
        {
            mRenderer = renderer;
            mMesh = mesh;
            mEnabled = true;
            mSubmeshToggles = new bool[mMesh != null ? mMesh.subMeshCount : 0];

            for(int i = 0; i < mSubmeshToggles.Length; i++)
            {
                mSubmeshToggles[i] = true;
            }
        }

        public TargetRendererInfo(Renderer renderer, Mesh mesh, bool enabled, bool[] submeshToggles)
        {
            mRenderer = renderer;
            mMesh = mesh;
            mEnabled = enabled;
            mSubmeshToggles = (bool[])submeshToggles.Clone();
        }

        public bool IsEqualTo(TargetRendererInfo other)
        {
            return (mRenderer == other.mRenderer)
                && (mMesh == other.mMesh)
                && (mEnabled == other.mEnabled)
                && mSubmeshToggles.SequenceEqual(other.mSubmeshToggles);
        }
    };

    public enum DebugVisualization
    {
        Current,
        Geometry,
        Unsigned,
        Signed,
        ShrinkWrapped,
        None
    }

    private enum WorkStage
    {
        Waiting,
        GetTriangles,
        UnsignedDistance,
        SignedDistance,
        Expansion,
        Exporting,
        Done
    };

    //saved editor parameters
    [SerializeField] private float mPixelSize = 0.01f;
    [SerializeField] private float mPadding = 0.04f;
    [SerializeField] private float mShrinkWrapRadius = 0.02f;
    [SerializeField] private GameObject mTargetObject = null;
    [SerializeField] private List<TargetRendererInfo> mTargetRendererInfos = new List<TargetRendererInfo>(0);

    //shaders
    [SerializeField] private Shader mWeldMeshShader;
    [SerializeField] private ComputeShader mDistanceFieldGenerationShader;
    [SerializeField] private ComputeShader mUnsignedToSignedConversionShader;
    [SerializeField] private ComputeShader mExpandDistanceFieldShader;
    [SerializeField] private ComputeShader mCopyDistanceFieldSliceShader;
    [SerializeField] private Shader mMeshVisualizationShader;
    [SerializeField] private Shader _SdfVisualizationShader;

    //bake helpers
    private WeldMeshHelper weldMeshHelper;
    private MeshToDistanceFieldHelper distanceFieldHelper;
    private UnsignedToSignedDistanceFieldHelper signedHelper;
    private ShrinkWrapDistanceFieldHelper shrinkWrapHelper;
    private ExportDistanceFieldHelper exportHelper;

    //output SDF parameters
    private Vector3Int mTextureDimensions;
    private Vector3 mMinCorner;
    private Vector3 mBoxSize;
    private Mesh mRenderBox;
    private Matrix4x4 mObjToSDF;

    //vizualization materials
    private Material mSdfVisualizationMaterial;
    private Material mMeshVisualizationMaterial;

    WorkStage mCurrentWorkStage = WorkStage.Waiting;

    private void OnDestroy()
    {
        CleanupAndReset();
    }

    //Baking Functions

    public void Initilize()
    {
        CleanupAndReset(); //make sure we are starting from a clean slate

        weldMeshHelper = new WeldMeshHelper();
        distanceFieldHelper = new MeshToDistanceFieldHelper();
        signedHelper = new UnsignedToSignedDistanceFieldHelper();
        shrinkWrapHelper = new ShrinkWrapDistanceFieldHelper();
        exportHelper = new ExportDistanceFieldHelper();

        mCurrentWorkStage = WorkStage.Waiting;

        if (mMeshVisualizationShader != null)
        {
            mMeshVisualizationMaterial = new Material(mMeshVisualizationShader);
        }

        if (_SdfVisualizationShader != null)
        {
            mSdfVisualizationMaterial = new Material(_SdfVisualizationShader);
        }
    }

    public void CleanupAndReset()
    {
        if (weldMeshHelper != null)
        {
            weldMeshHelper.Cleanup();
            weldMeshHelper = null;
        }

        if (distanceFieldHelper != null)
        {
            distanceFieldHelper.Cleanup();
            distanceFieldHelper = null;
        }

        if (signedHelper != null)
        {
            signedHelper.Cleanup();
            signedHelper = null;
        }

        if (shrinkWrapHelper != null)
        {
            shrinkWrapHelper.Cleanup();
            shrinkWrapHelper = null;
        }

        if (exportHelper != null)
        {
            exportHelper.Cleanup();
            exportHelper = null;
        }

        mTextureDimensions = Vector3Int.zero;
        mMinCorner = Vector3.zero;
        mBoxSize = Vector3.zero;
        mRenderBox = null;
        mObjToSDF = Matrix4x4.identity;

        mMeshVisualizationMaterial = null;
        mSdfVisualizationMaterial = null;

        mCurrentWorkStage = WorkStage.Waiting;
    }

    public bool AttemptSaveSettings(float newPixelSize, float newPadding, float newShrinkWrapRadius, GameObject newTargetObject, List<TargetRendererInfo> newTargetRendererInfos)
    {
        //don't change the settings if a bake is active
        if (mCurrentWorkStage != WorkStage.Waiting)
        {
            return false;
        }

        bool didAnythingChange = mTargetRendererInfos.Count != newTargetRendererInfos.Count;

        //remove target renderers if we have more than the new setting
        if (newTargetRendererInfos.Count < mTargetRendererInfos.Count)
        {
            mTargetRendererInfos.RemoveRange(newTargetRendererInfos.Count, mTargetRendererInfos.Count - newTargetRendererInfos.Count);
        }

        //compare each target renderer to the new ones to detect changes
        for (int index = 0; index < newTargetRendererInfos.Count; index++)
        {
            TargetRendererInfo newContainer = newTargetRendererInfos[index];

            if (index >= mTargetRendererInfos.Count)
            {
                //add any new targets
                mTargetRendererInfos.Add(newContainer);

                didAnythingChange = true;
            }
            else if (!mTargetRendererInfos[index].IsEqualTo(newContainer))
            {
                mTargetRendererInfos[index] = newContainer;

                didAnythingChange = true;
            }
        }

        if (newPixelSize != mPixelSize)
        {
            mPixelSize = newPixelSize;
            didAnythingChange = true;
        }

        if (newPadding != mPadding)
        {
            mPadding = newPadding;
            didAnythingChange = true;
        }

        if (newShrinkWrapRadius != mShrinkWrapRadius)
        {
            mShrinkWrapRadius = newShrinkWrapRadius;
            didAnythingChange = true;
        }

        if (newTargetObject != mTargetObject)
        {
            mTargetObject = newTargetObject;
            didAnythingChange = true;
        }

        return didAnythingChange;
    }

    public bool BeginBake(float pixelSize, float padding, float shrinkWrapRadius, GameObject targetObject, List<TargetRendererInfo> newTargetRendererInfos)
    {
        //make sure all the settings are set up correctly
        Initilize();
        AttemptSaveSettings(pixelSize, padding, shrinkWrapRadius, targetObject, newTargetRendererInfos);

        if (mPixelSize <= 0 || mPadding <= 0 || mShrinkWrapRadius < 0)
        {
            return false;
        }

        //cull the list of renderers
        List<Renderer> culledRenderers = new List<Renderer>(0);
        List<Mesh> culledMeshes = new List<Mesh>(0);
        List<bool[]> culledSubmeshToggles = new List<bool[]>(0);

        for (int index = 0; index < mTargetRendererInfos.Count; index++)
        {
            if (mTargetRendererInfos[index].mEnabled && mTargetRendererInfos[index].mRenderer != null && mTargetRendererInfos[index].mMesh != null)
            {
                culledRenderers.Add(mTargetRendererInfos[index].mRenderer);
                culledMeshes.Add(mTargetRendererInfos[index].mMesh);
                culledSubmeshToggles.Add(mTargetRendererInfos[index].mSubmeshToggles);
            }
        }

        if (culledRenderers.Count <= 0)
        {
            return false;
        }

        //start actually start the bake
        weldMeshHelper.Initialize(mWeldMeshShader, targetObject.transform.position, culledRenderers.ToArray(), culledMeshes.ToArray(), culledSubmeshToggles.ToArray());
        mCurrentWorkStage = WorkStage.GetTriangles;

        return true;
    }

    public void DoWork()
    {
        if(mCurrentWorkStage == WorkStage.Waiting || mCurrentWorkStage == WorkStage.Done)
        {
            return;
        }

        if (WorkOnCurrentStage()) //true when current stage is done
        {
            MoveToNextStage();
        }
    }

    //Bake Helpers

    private bool WorkOnCurrentStage()
    {
        switch (mCurrentWorkStage)
        {
            case WorkStage.GetTriangles:
                return weldMeshHelper.DoWork();

            case WorkStage.UnsignedDistance:
                return distanceFieldHelper.DoWork();

            case WorkStage.SignedDistance:
                return signedHelper.DoWork();

            case WorkStage.Expansion:
                return shrinkWrapHelper.DoWork();

            case WorkStage.Exporting:
                return exportHelper.DoWork();

            default:
                return false;
        }
    }

    private void MoveToNextStage()
    {
        switch (mCurrentWorkStage)
        {
            case WorkStage.Waiting:
                //this is only done from BeginBake()
                break;

            case WorkStage.GetTriangles:
                InitializeSDFDimensions(weldMeshHelper.GetWeldedMeshBounds()); //we can't initilize this until we know the bounds of the welded mesh
                distanceFieldHelper.Initialize(mDistanceFieldGenerationShader, mTextureDimensions, mMinCorner, mPixelSize, weldMeshHelper.GetTriangleCount(), weldMeshHelper.GetVertexBuffer());
                mCurrentWorkStage = WorkStage.UnsignedDistance;

                break;

            case WorkStage.UnsignedDistance:
                signedHelper.Initialize(mUnsignedToSignedConversionShader, mPixelSize, distanceFieldHelper.GetDistanceField());
                mCurrentWorkStage = WorkStage.SignedDistance;

                break;

            case WorkStage.SignedDistance:
                shrinkWrapHelper.Initialize(mExpandDistanceFieldShader, mPixelSize, mShrinkWrapRadius, signedHelper.GetDistanceField());
                mCurrentWorkStage = WorkStage.Expansion;

                break;

            case WorkStage.Expansion:
                exportHelper.Initialize(mCopyDistanceFieldSliceShader, shrinkWrapHelper.GetDistanceField());
                mCurrentWorkStage = WorkStage.Exporting;

                break;

            case WorkStage.Exporting:
                mCurrentWorkStage = WorkStage.Done;

                break;

            default:
                break;
        }
    }

    private void InitializeSDFDimensions(Bounds weldedMeshBounds)
    {
        //calculate the required texture dimensions to enclose the welded mesh with padding 
        for (int i = 0; i < 3; i++)
        {
            mTextureDimensions[i] = Mathf.CeilToInt((weldedMeshBounds.extents[i] + mPadding) * 2 / mPixelSize);
        }

        //calculate the location and size of the box formed by our bounds
        mBoxSize = (Vector3)mTextureDimensions * mPixelSize;
        mMinCorner = weldedMeshBounds.center - (Vector3)mTextureDimensions * mPixelSize / 2.0f;
        Vector3 maxCorner = mMinCorner + mBoxSize;

        //calculate the matrix to transform into a texture coordinate for our SDF
        mObjToSDF = Matrix4x4.Scale(new Vector3(1.0f / mBoxSize.x, 1.0f / mBoxSize.y, 1.0f / mBoxSize.z));
        mObjToSDF *= Matrix4x4.Translate(-mMinCorner);

        //construct a mesh of the bounding box for debug visualization
        /*
        5-------6
        |\      |\
        | 1-------2  z x
        4-|-----7 |   \|
         \|      \|    *--y
          0-------3
        */

        List<Vector3> verts = new List<Vector3>();
        verts.Add(new Vector3(mMinCorner.x, mMinCorner.y, mMinCorner.z));
        verts.Add(new Vector3(mMinCorner.x, maxCorner.y, mMinCorner.z));
        verts.Add(new Vector3(maxCorner.x, maxCorner.y, mMinCorner.z));
        verts.Add(new Vector3(maxCorner.x, mMinCorner.y, mMinCorner.z));
        verts.Add(new Vector3(mMinCorner.x, mMinCorner.y, maxCorner.z));
        verts.Add(new Vector3(mMinCorner.x, maxCorner.y, maxCorner.z));
        verts.Add(new Vector3(maxCorner.x, maxCorner.y, maxCorner.z));
        verts.Add(new Vector3(maxCorner.x, mMinCorner.y, maxCorner.z));

        List<int> indices = new List<int>();
        //front
        indices.Add(0); indices.Add(1); indices.Add(2);
        indices.Add(0); indices.Add(2); indices.Add(3);
        //left
        indices.Add(4); indices.Add(5); indices.Add(1);
        indices.Add(4); indices.Add(1); indices.Add(0);
        //right
        indices.Add(3); indices.Add(2); indices.Add(6);
        indices.Add(3); indices.Add(6); indices.Add(7);
        //top
        indices.Add(1); indices.Add(5); indices.Add(6);
        indices.Add(1); indices.Add(6); indices.Add(2);
        //bottom
        indices.Add(4); indices.Add(0); indices.Add(3);
        indices.Add(4); indices.Add(3); indices.Add(7);
        //back
        indices.Add(7); indices.Add(6); indices.Add(5);
        indices.Add(7); indices.Add(5); indices.Add(4);

        mRenderBox = new Mesh();
        mRenderBox.SetVertices(verts);
        mRenderBox.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
        mRenderBox.OptimizeReorderVertexBuffer();
        mRenderBox.RecalculateBounds();
    }

    // Visualizaton Functions

    public void RenderVisualization(DebugVisualization type, float dialation)
    {
        //choose a visualization based on the current work being done
        if(type == DebugVisualization.Current)
        {
            switch (mCurrentWorkStage)
            {
                case WorkStage.GetTriangles:
                    type = DebugVisualization.Geometry;
                    break;

                case WorkStage.UnsignedDistance:
                    type = DebugVisualization.Unsigned;
                    break;

                case WorkStage.SignedDistance:
                    type = DebugVisualization.Signed;
                    break;

                case WorkStage.Expansion:
                case WorkStage.Exporting:
                case WorkStage.Done:
                    type = DebugVisualization.ShrinkWrapped;
                    break;

                default:
                    type = DebugVisualization.Geometry;
                    break;
            }
        }

        switch (type)
        {
            case DebugVisualization.Geometry:
                if (mCurrentWorkStage >= WorkStage.GetTriangles)
                {
                    RenderMeshVisualization(weldMeshHelper.GetVertexBuffer());
                }
                break;

            case DebugVisualization.Unsigned:
                if (mCurrentWorkStage >= WorkStage.UnsignedDistance)
                {
                    RenderSDFVisualization(distanceFieldHelper.GetDistanceField(), Mathf.Max(mPixelSize, dialation), true);
                }
                break;

            case DebugVisualization.Signed:
                if (mCurrentWorkStage >= WorkStage.SignedDistance)
                {
                    RenderSDFVisualization(signedHelper.GetDistanceField(), dialation);
                }
                break;

            case DebugVisualization.ShrinkWrapped:
                if (mCurrentWorkStage >= WorkStage.Expansion)
                {
                    RenderSDFVisualization(shrinkWrapHelper.GetDistanceField(), dialation);
                }
                break;
                
            default:
                break;
        }
    }

    private void RenderMeshVisualization(ComputeBuffer vertices)
    {
        if (mMeshVisualizationMaterial != null && vertices != null)
        {
            mMeshVisualizationMaterial.SetBuffer("_WeldedVertexPositions", vertices);
            mMeshVisualizationMaterial.SetMatrix("_VisualizationMatrix", transform.localToWorldMatrix);

            mMeshVisualizationMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Triangles, vertices.count, 1);
        }
    }

    private void RenderSDFVisualization(RenderTexture sdfTexture, float dialation, bool invert = false)
    {
        if (mSdfVisualizationMaterial != null && sdfTexture != null && mRenderBox != null && mObjToSDF != null)
        {
            mSdfVisualizationMaterial.SetTexture("_SDFTex", sdfTexture);
            mSdfVisualizationMaterial.SetMatrix("_ObjToSDF", mObjToSDF);
            mSdfVisualizationMaterial.SetMatrix("_SDFToObj", mObjToSDF.inverse);
            mSdfVisualizationMaterial.SetFloat("_Dilation", dialation);
            mSdfVisualizationMaterial.SetInt("_Invert", invert ? 1 : 0);

            mSdfVisualizationMaterial.SetPass(0);
            Graphics.DrawMeshNow(mRenderBox, transform.localToWorldMatrix);
        }
    }

    //Getters

    public bool IsWaiting()
    {
        return mCurrentWorkStage == WorkStage.Waiting;
    }

    public bool IsFinished()
    {
        return mCurrentWorkStage == WorkStage.Done;
    }

    public float GetPixelSize()
    {
        return mPixelSize;
    }

    public float GetPaddingSize()
    {
        return mPadding;
    }

    public float GetShrinkWrapRadius()
    {
        return mShrinkWrapRadius;
    }

    public GameObject GetTargetObject()
    {
        return mTargetObject;
    }

    public int GetTargetRendererCount()
    {
        return mTargetRendererInfos.Count;
    }

    public Renderer GetTargetRenderer(int index)
    {
        return mTargetRendererInfos[index].mRenderer;
    }

    public Mesh GetTargetMesh(int index)
    {
        return mTargetRendererInfos[index].mMesh;
    }

    public bool GetTargetRendererToggle(int index)
    {
        return mTargetRendererInfos[index].mEnabled;
    }

    public bool[] GetTargetSubmeshToggles(int index)
    {
        return (bool[])mTargetRendererInfos[index].mSubmeshToggles.Clone();
    }

    public Vector3 GetSDFLowerCorner()
    {
        return mMinCorner;
    }

    public Vector3 GetSDFSize()
    {
        return mBoxSize;
    }

    public Texture3D GetSDFTexture()
    {
        if (mCurrentWorkStage != WorkStage.Done)
        {
            return null;
        }

        return exportHelper.GetOutputexture();
    }

    public float GetPercentageDone()
    {
        float percentDone;

        switch (mCurrentWorkStage)
        {
            case WorkStage.UnsignedDistance:
                percentDone = distanceFieldHelper.GetPercentageDone();
                break;

            case WorkStage.SignedDistance:
                percentDone = 1.0f + signedHelper.GetPercentageDone();
                break;

            case WorkStage.Expansion:
                percentDone = 2.0f + shrinkWrapHelper.GetPercentageDone();
                break;

            case WorkStage.Exporting:
                percentDone = 3.0f + exportHelper.GetPercentageDone();
                break;

            case WorkStage.Done:
                percentDone = 4.0f;
                break;

            default:
                percentDone = 0.0f; ;
                break;
        }

        return percentDone / 4.0f;
    }
}