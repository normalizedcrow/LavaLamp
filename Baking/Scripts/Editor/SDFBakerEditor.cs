using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SDFBaker))]
public class SDFBakerEditor : Editor
{
    private static bool sRenderersFoldout = true;
    private static bool[] sSubmeshFoldouts = new bool[100];

    private static SDFBaker.DebugVisualization sVisualizationMode = SDFBaker.DebugVisualization.Current;
    private static float sVvisualizationDilation = 0.0f;

    private static List<Material> sMaterialsToUpdate = new List<Material>(0);
    
    private string mErrorMessage = "";

    public override void OnInspectorGUI()
    {
        SDFBaker baker = (SDFBaker)target;

        if (baker.IsWaiting())
        {
            SettingsPanel();
        }

        if (baker.IsFinished())
        {
            ResultsPanel();
        }

        if (!baker.IsWaiting() && !baker.IsFinished()) //is mid-bake
        {
            BakingPanel();

            EditorGUILayout.Space(12);

            DebugPanel();
        }

        if (mErrorMessage.Length > 0)
        {
            EditorGUILayout.HelpBox(mErrorMessage, MessageType.Error);
        }
    }

    public void OnSceneGUI()
    {
        SDFBaker baker = (SDFBaker)target;
        baker.RenderVisualization(sVisualizationMode, sVvisualizationDilation);
    }

    public void OnEnable()
    {
        EditorApplication.update += BakeUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= BakeUpdate;
    }

    private void SettingsPanel()
    {
        SDFBaker baker = (SDFBaker)target;

        //check if any fields are changed
        EditorGUI.BeginChangeCheck();

        //get the simple settings
        float newPixelSize = EditorGUILayout.FloatField("SDF Pixel Size", baker.GetPixelSize());
        float newPadding = EditorGUILayout.FloatField("SDF Padding", baker.GetPaddingSize());
        float newShrinkWrapRadius = EditorGUILayout.FloatField("SDF Shrink Wrap Radius", baker.GetShrinkWrapRadius());
        GameObject targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", baker.GetTargetObject(), typeof(GameObject), true);

        //get the list of renderers on the object and the stored settings for them if applicable
        List<SDFBaker.TargetRendererInfo> newTargetRendererInfos;
        if (!RebuildRendererList(targetObject, out newTargetRendererInfos))
        {
            mErrorMessage = "Don't change the baker settings outside of this editor!";
        }

        if(sSubmeshFoldouts.Length != newTargetRendererInfos.Count)
        {
            sSubmeshFoldouts = new bool[newTargetRendererInfos.Count];
        }

        if (targetObject != null)
        {
            //draw the renderer and submesh toggle settings
            sRenderersFoldout = EditorGUILayout.Foldout(sRenderersFoldout, "Renderers", true, EditorStyles.foldoutHeader);

            EditorGUI.indentLevel++;

            if (sRenderersFoldout)
            {
                //for every renderer on the target
                for (int rendererIndex = 0; rendererIndex < newTargetRendererInfos.Count; rendererIndex++)
                {
                    //get the current renderer target info from the list
                    SDFBaker.TargetRendererInfo currentTarget = newTargetRendererInfos[rendererIndex];

                    EditorGUILayout.BeginHorizontal();

                    currentTarget.mEnabled = EditorGUILayout.Toggle(currentTarget.mRenderer.name, currentTarget.mEnabled);

                    EditorGUILayout.BeginVertical();

                    //if this renderer is enabled
                    if (currentTarget.mEnabled)
                    {
                        //draw the submesh toggles as a foldout if we have more than one
                        if (currentTarget.mSubmeshToggles.Length > 1)
                        {
                            sSubmeshFoldouts[rendererIndex] = EditorGUILayout.Foldout(sSubmeshFoldouts[rendererIndex], "Submeshes", true, EditorStyles.foldoutHeader);

                            if (sSubmeshFoldouts[rendererIndex])
                            {
                                EditorGUI.indentLevel++;

                                for (int submeshIndex = 0; submeshIndex < currentTarget.mSubmeshToggles.Length; submeshIndex++)
                                {
                                    currentTarget.mSubmeshToggles[submeshIndex] = EditorGUILayout.Toggle("Index " + submeshIndex, currentTarget.mSubmeshToggles[submeshIndex]);
                                }

                                EditorGUI.indentLevel--;

                            }

                        }
                        else if (currentTarget.mSubmeshToggles.Length > 0) //if there is only one submesh then no reason for an additional toggle
                        {
                            currentTarget.mSubmeshToggles[0] = true;
                        }
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();

                    //store the updated version of the target info back into the list
                    newTargetRendererInfos[rendererIndex] = currentTarget;
                }
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(12);

            //add a button to kick off the bake
            if (GUILayout.Button("Start Bake"))
            {
                if (baker.BeginBake(newPixelSize, newPadding, newShrinkWrapRadius, targetObject, newTargetRendererInfos))
                {
                    sVisualizationMode = SDFBaker.DebugVisualization.Current;
                    sVvisualizationDilation = 0;

                    mErrorMessage = "";
                }
                else
                {
                    mErrorMessage = "Invalid bake settings!";
                }
            }
        }

        //record an undo before we update the settings
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(baker, "Change SDF Baker Settings");
        }

        //update all the changed settings
        if (baker.AttemptSaveSettings(newPixelSize, newPadding, newShrinkWrapRadius, targetObject, newTargetRendererInfos))
        {
            EditorUtility.SetDirty(baker);
        }
    }

    private void ResultsPanel()
    {
        SDFBaker baker = (SDFBaker)target;

        //display the resulting SDF texture and its dimensions
        GUILayout.Label("Result SDF", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.ObjectField("SDF Texture", baker.GetSDFTexture(), typeof(Texture3D), false);
        EditorGUILayout.FloatField("SDF Pixel Size", baker.GetPixelSize());
        EditorGUILayout.Vector3Field("SDF Lower Corner", baker.GetSDFLowerCorner());
        EditorGUILayout.Vector3Field("SDF Size", baker.GetSDFSize());
        EditorGUI.indentLevel--;

        //add a button to save the texture as an asset
        if (GUILayout.Button("Save SDF Texture"))
        {
            string filepath = EditorUtility.SaveFilePanel("Save SDF Texture", "", "SDF.asset", "asset");

            if (filepath.Length > 0)
            {
                filepath = "Assets" + filepath.Remove(0, Application.dataPath.Length);

                Texture3D texture = baker.GetSDFTexture();

                if (texture != null)
                {
                    AssetDatabase.CreateAsset(texture, filepath);
                    mErrorMessage = "";
                }
                else
                {
                    mErrorMessage = "Texture creation failed!";
                }
            }
        }

        EditorGUILayout.Space(12);

        DebugPanel();

        EditorGUILayout.Space(12);

        UpdateMaterialsPanel();

        EditorGUILayout.Space(30);

        if (GUILayout.Button("Reset Baker"))
        {
            baker.CleanupAndReset();

            SceneView.RepaintAll();
        }
    }

    private void BakingPanel()
    {
        SDFBaker baker = (SDFBaker)target;

        //draw a progress bar for the baking process
        Rect bar = EditorGUILayout.BeginVertical();
        EditorGUI.ProgressBar(bar, baker.GetPercentageDone(), "Baking SDF Texture...");
        GUILayout.Space(18);
        EditorGUILayout.EndVertical();

        if (GUILayout.Button("Stop Bake"))
        {
            baker.CleanupAndReset();

            SceneView.RepaintAll();
        }
    }
    private void DebugPanel()
    {
        GUILayout.Label("Debug Visualization", EditorStyles.boldLabel);

        EditorGUI.indentLevel++;

        SDFBaker.DebugVisualization newVisualization = (SDFBaker.DebugVisualization)EditorGUILayout.EnumPopup("Visualization Mode", sVisualizationMode);

        //only draw the dialation slider for visualizations that need it
        float newVvisualizationDilation = sVvisualizationDilation;
        if (sVisualizationMode != SDFBaker.DebugVisualization.Geometry && sVisualizationMode != SDFBaker.DebugVisualization.None)
        {
            float minValue = sVisualizationMode == SDFBaker.DebugVisualization.Unsigned ? 0.0f : -0.5f;
            newVvisualizationDilation = EditorGUILayout.Slider("Debug Dilation", sVvisualizationDilation, minValue, 0.5f);
        }

        EditorGUI.indentLevel--;

        //if the settings changed we need to tell the scene view to redraw
        if (newVisualization != sVisualizationMode || newVvisualizationDilation != sVvisualizationDilation)
        {
            SceneView.RepaintAll();
        }

        sVisualizationMode = newVisualization;
        sVvisualizationDilation = newVvisualizationDilation;
    }

    private void UpdateMaterialsPanel()
    {
        SDFBaker baker = (SDFBaker)target;

        GUILayout.Label("Auto Update Materials", EditorStyles.boldLabel);

        EditorGUI.indentLevel++;

        //draw a field for every current material
        for(int i = 0; i < sMaterialsToUpdate.Count; i++)
        {
            sMaterialsToUpdate[i] = (Material)EditorGUILayout.ObjectField("", sMaterialsToUpdate[i], typeof(Material), false);
        }

        //add an extra field for a new material
        Material newMaterial = (Material)EditorGUILayout.ObjectField("", null, typeof(Material), false);

        if(newMaterial != null)
        {
            sMaterialsToUpdate.Add(newMaterial);
        }

        //remove null materials
        for (int i = sMaterialsToUpdate.Count - 1; i >= 0; i--)
        {
            if (sMaterialsToUpdate[i] == null)
            {
                sMaterialsToUpdate.RemoveAt(i);
            }
        }

        EditorGUI.indentLevel--;

        if (GUILayout.Button("Update Materials"))
        {
            //record an undo for all the materials
            Undo.RecordObjects(sMaterialsToUpdate.ToArray(), "Update Materials");

            foreach(Material material in sMaterialsToUpdate)
            {
                material.SetTexture("_SDFTexture", baker.GetSDFTexture());
                material.SetFloat("_SDFPixelSize", baker.GetPixelSize());
                material.SetVector("_SDFLowerCorner", baker.GetSDFLowerCorner());
                material.SetVector("_SDFSize", baker.GetSDFSize());
            }
        }
    }

    private bool RebuildRendererList(GameObject newTargetObject, out List<SDFBaker.TargetRendererInfo> newTargetRendererInfos)
    {
        SDFBaker baker = (SDFBaker)target;

        newTargetRendererInfos = new List<SDFBaker.TargetRendererInfo>(0);

        if (newTargetObject == null)
        {
            return true;
        }

        bool isTargetSame = newTargetObject == baker.GetTargetObject();
        bool wereSubmeshSettingsTamperedWith = false;

        //for every renderer on the target
        Renderer[] newRenderers = newTargetObject.GetComponentsInChildren<Renderer>();
        for (int rendererIndex = 0; rendererIndex < newRenderers.Length; rendererIndex++)
        {
            Renderer currentRenderer = newRenderers[rendererIndex];
            Mesh currentMesh = null;

            //if this is a mesh renderer, get the mesh from a mesh filter
            if (currentRenderer is MeshRenderer)
            {
                MeshFilter filter = currentRenderer.GetComponent<MeshFilter>();
                if (filter != null)
                {
                    currentMesh = filter.sharedMesh;
                }
            }

            //if this is a skinned mesh renderer, get the shared mesh directly
            if (currentRenderer is SkinnedMeshRenderer)
            {
                currentMesh = ((SkinnedMeshRenderer)currentRenderer).sharedMesh;
            }

            //if both the renderer and mesh are valid
            if (currentRenderer != null && currentMesh != null)
            {
                //if this is the exact same mesh/renderer combo as one currently stored in the settings
                if (isTargetSame
                    && (rendererIndex < baker.GetTargetRendererCount())
                    && (currentRenderer == baker.GetTargetRenderer(rendererIndex))
                    && (currentMesh == baker.GetTargetMesh(rendererIndex)))
                {
                    bool currentRendererToggle = baker.GetTargetRendererToggle(rendererIndex); //carry forward the toggle setting from last frame

                    //also cary forward the submesh toggle settings...
                    bool[] currentSubmeshToggles = baker.GetTargetSubmeshToggles(rendererIndex);

                    //but check and see if the count was tampered with outside of this editor...
                    if (currentSubmeshToggles.Length != currentMesh.subMeshCount)
                    {
                        wereSubmeshSettingsTamperedWith = true;

                        //rebuild the submesh toggles array to be the correct length
                        currentSubmeshToggles = new bool[currentMesh.subMeshCount];
                        for (int submeshIndex = 0; submeshIndex < currentSubmeshToggles.Length; submeshIndex++)
                        {
                            currentSubmeshToggles[submeshIndex] = true;
                        }
                    }

                    newTargetRendererInfos.Add(new SDFBaker.TargetRendererInfo(currentRenderer, currentMesh, currentRendererToggle, currentSubmeshToggles));
                }
                else
                {
                    //add a new renderer info if these ones wern't already stored
                    newTargetRendererInfos.Add(new SDFBaker.TargetRendererInfo(currentRenderer, currentMesh));
                }
            }
        }

        return !wereSubmeshSettingsTamperedWith;
    }

    public void BakeUpdate()
    {
        SDFBaker baker = (SDFBaker)target;

        bool wasFinished = baker.IsFinished();
        baker.DoWork();

        //update the scene and editor debug views while the bake is in progress for a smooth progress bar and visualization
        if (!baker.IsWaiting() && !baker.IsFinished())
        {
            SceneView.RepaintAll();
            Repaint();
        }

        //redraw the inspector one last time when the bake finishes to display the results panel
        if(!wasFinished && baker.IsFinished())
        {
            Repaint();
        }
    }
}