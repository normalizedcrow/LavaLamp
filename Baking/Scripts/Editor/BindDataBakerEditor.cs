using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BindDataBaker))]
public class BindDataBakerEditor : Editor
{
    private const int cHighestUvChannel = 7;

    private static bool sEnableDebugVisualization = true;
    private static List<Material> sMaterialsToUpdate = new List<Material>(0);
    

    private int mTargetRendererIndex = 0;
    private string mErrorMessage = "";

    public override void OnInspectorGUI()
    {
        BindDataBaker baker = (BindDataBaker)target;

        if(baker.IsBakeFinished())
        {
            ResultsPanel();

            EditorGUILayout.Space(12);

            DebugPanel();

            EditorGUILayout.Space(12);

            UpdateRendererPanel();

            EditorGUILayout.Space(12);

            UpdateMaterialsPanel();

            EditorGUILayout.Space(30);

            if (GUILayout.Button("Reset Baker"))
            {
                baker.CleanupAndReset();
                SceneView.RepaintAll();
                mErrorMessage = "";
            }
        }
        else
        {
            SettingsPanel();
        }

        if (mErrorMessage.Length > 0)
        {
            EditorGUILayout.HelpBox(mErrorMessage, MessageType.Error);
        }
    }

    public void OnSceneGUI()
    {
        BindDataBaker baker = (BindDataBaker)target;

        if (sEnableDebugVisualization)
        {
            baker.RenderVisualization();
        }
    }

    public void SettingsPanel()
    {
        BindDataBaker baker = (BindDataBaker)target;

        //check if any fields are changed
        EditorGUI.BeginChangeCheck();

        GameObject newTargetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", baker.GetTargetObject(), typeof(GameObject), true);
        Renderer newTargetRenderer = null;
        Mesh newTargetMesh = null;

        //get the list of renderers on the target object
        if (newTargetObject != null)
        {
            List<Renderer> targetRenderersList = new List<Renderer>();
            List<Mesh> targetMeshesList = new List<Mesh>();
            List<string> targetRendererNames = new List<string>();

            //for each renderer on the target
            foreach (Renderer currentRenderer in newTargetObject.GetComponentsInChildren<Renderer>())
            {
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
                    //add them to the dropdown list
                    targetRenderersList.Add(currentRenderer);
                    targetMeshesList.Add(currentMesh);
                    targetRendererNames.Add(currentRenderer.name);

                    //if this is the currently selected target renderer and mesh
                    if (currentRenderer == baker.GetTargetRenderer() && currentMesh == baker.GetTargetMesh())
                    {
                        //set the index to this selection
                        mTargetRendererIndex = targetRenderersList.Count - 1;
                    }
                }
            }

            //choose the target renderer and mesh with a popup menu
            mTargetRendererIndex = Mathf.Max(0, Mathf.Min(mTargetRendererIndex, targetRenderersList.Count - 1));
            mTargetRendererIndex = EditorGUILayout.Popup("Renderer", mTargetRendererIndex, targetRendererNames.ToArray());

            if(mTargetRendererIndex >= 0 && targetRenderersList.Count > mTargetRendererIndex && targetMeshesList.Count > mTargetRendererIndex)
            {
                newTargetRenderer = targetRenderersList[mTargetRendererIndex];
                newTargetMesh = targetMeshesList[mTargetRendererIndex];
            }
        }

        EditorGUILayout.Space(12);

        //Get the UV channels that the bind data will be stored in
        BindDataBaker.BindDataUVSlot bindPositionsSlot = (BindDataBaker.BindDataUVSlot)EditorGUILayout.EnumPopup("Bind Positions Output UV Channel", baker.GetBindPositionsUVSlot());
        BindDataBaker.BindDataUVSlot bindNormalsSlot = (BindDataBaker.BindDataUVSlot)EditorGUILayout.EnumPopup("Bind Normals Output UV Channel", baker.GetBindNormalsUVSlot());
        BindDataBaker.BindDataUVSlot bindTangentsSlot = (BindDataBaker.BindDataUVSlot)EditorGUILayout.EnumPopup("Bind Tangents Output UV Channel", baker.GetBindTangentsUVSlot());

        if(bindPositionsSlot == bindNormalsSlot || bindPositionsSlot == bindTangentsSlot || bindNormalsSlot == bindTangentsSlot)
        {
            EditorGUILayout.HelpBox("Output uv channels should not overlap!", MessageType.Error);
        }

        EditorGUILayout.Space(12);

        //get the mask data
        Texture2D newMask = (Texture2D)EditorGUILayout.ObjectField("Subregion Mask Texture", baker.GetMaskTexture(), typeof(Texture2D), true);
        Color newInvalidMaskColor = baker.GetInvalidMaskColor();
        int newMaskColorCount = baker.GetMaskColorCount();
        Color[] newMaskColors = baker.GetMaskColors();

        if(newMaskColors.Length != BindDataBaker.cMaxMaskColors)
        {
            newMaskColors = new Color[BindDataBaker.cMaxMaskColors];
            mErrorMessage = "Don't change the baker settings outside of this editor!";
        }

        //only display mask colors if there is actually a mask
        if (newMask != null)
        {
            newInvalidMaskColor = EditorGUILayout.ColorField("Excluded Region Mask Color", newInvalidMaskColor);

            for (int colorIndex = 0; colorIndex < newMaskColorCount; colorIndex++)
            {
                newMaskColors[colorIndex] = EditorGUILayout.ColorField("Subregion " + colorIndex + " Mask Color", newMaskColors[colorIndex]);
            }

            //add buttons to add or remove colors
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+"))
            {
                if(newMaskColorCount < BindDataBaker.cMaxMaskColors)
                {
                    newMaskColorCount++;
                }
                else
                {
                    mErrorMessage = "Maximum number of subregions is " + BindDataBaker.cMaxMaskColors + "!";
                }
            }

            if (GUILayout.Button("-"))
            {
                if (newMaskColorCount > 1)
                {
                    newMaskColorCount--;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        //record an undo before we update the settings
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(baker, "Change Bind Data Baker Settings");
        }

        //update all the changed settings
        if (baker.SaveSettings(newTargetObject, newTargetRenderer, newTargetMesh, newMask, newMaskColorCount, newMaskColors, newInvalidMaskColor, bindPositionsSlot, bindNormalsSlot, bindTangentsSlot))
        {
            EditorUtility.SetDirty(baker);
        }

        EditorGUILayout.Space(12);

        //add a button to do the bake
        if (GUILayout.Button("Bake"))
        {
            baker.DoBake();

            if (baker.IsBakeFinished())
            {
                sEnableDebugVisualization = true;
                SceneView.RepaintAll();
                mErrorMessage = "";
            }
            else
            {
                mErrorMessage = "Bake Failed!";
            }
        }
    }

    private void ResultsPanel()
    {
        BindDataBaker baker = (BindDataBaker)target;

        //display the baked mesh
        GUILayout.Label("Result Mesh With Bind Data", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.ObjectField("Baked Mesh", baker.GetMeshWithBindData(), typeof(Mesh), false);
        EditorGUI.indentLevel--;

        if (GUILayout.Button("Save Mesh With Bind Data"))
        {
            Mesh bindDataMesh = baker.GetMeshWithBindData();

            string filepath = EditorUtility.SaveFilePanel("Save Mesh", "", bindDataMesh.name, "asset");

            if (filepath.Length > 0)
            {
                filepath = "Assets" + filepath.Remove(0, Application.dataPath.Length);

                AssetDatabase.CreateAsset(bindDataMesh, filepath);
            }
        }
    }

    private void DebugPanel()
    {
        GUILayout.Label("Debug Visualization", EditorStyles.boldLabel);

        if (GUILayout.Button(sEnableDebugVisualization ? "Disable" : "Enable"))
        {
            sEnableDebugVisualization = !sEnableDebugVisualization;
            SceneView.RepaintAll();
        }
    }

    private void UpdateMaterialsPanel()
    {
        BindDataBaker baker = (BindDataBaker)target;

        GUILayout.Label("Update Material Bind Data Configurations", EditorStyles.boldLabel);

        EditorGUI.indentLevel++;

        //draw a field for every current material
        for (int i = 0; i < sMaterialsToUpdate.Count; i++)
        {
            sMaterialsToUpdate[i] = (Material)EditorGUILayout.ObjectField("", sMaterialsToUpdate[i], typeof(Material), false);
        }

        //add an extra field for a new material
        Material newMaterial = (Material)EditorGUILayout.ObjectField("", null, typeof(Material), false);

        if (newMaterial != null)
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
            Undo.RecordObjects(sMaterialsToUpdate.ToArray(), "Update Material Bind Data Configurations");

            int bindPositionsSlot = (int)baker.GetBindPositionsUVSlot();
            int bindNormalsSlot = (int)baker.GetBindNormalsUVSlot();
            int bindTangentsSlot = (int)baker.GetBindTangentsUVSlot();

            foreach (Material material in sMaterialsToUpdate)
            {
                //set the parameters that drive the keywords in the material editor
                material.SetInt("_BindPositionsSlot", bindPositionsSlot);
                material.SetInt("_BindNormalsSlot", bindNormalsSlot);
                material.SetInt("_BindTangentsSlot", bindTangentsSlot);

                //set the appropriate keywords for the uv slot that each set of bind data will come from
                for (int i = 1; i <= cHighestUvChannel; i++)
                {
                    if (i == bindPositionsSlot)
                    {
                        material.EnableKeyword("LAVA_LAMP_BIND_POSITIONS_SLOT_" + i);
                    }
                    else
                    {
                        material.DisableKeyword("LAVA_LAMP_BIND_POSITIONS_SLOT_" + i);
                    }

                    if (i == bindNormalsSlot)
                    {
                        material.EnableKeyword("LAVA_LAMP_BIND_NORMALS_SLOT_" + i);
                    }
                    else
                    {
                        material.DisableKeyword("LAVA_LAMP_BIND_NORMALS_SLOT_" + i);
                    }

                    if (i == bindTangentsSlot)
                    {
                        material.EnableKeyword("LAVA_LAMP_BIND_TANGENTS_SLOT_" + i);
                    }
                    else
                    {
                        material.DisableKeyword("LAVA_LAMP_BIND_TANGENTS_SLOT_" + i);
                    }
                }
            }
        }
    }

    private void UpdateRendererPanel()
    {
        BindDataBaker baker = (BindDataBaker)target;

        GUILayout.Label("Update Target Renderer", EditorStyles.boldLabel);

        if (GUILayout.Button("Replace Mesh"))
        {
            Mesh newMesh = baker.GetMeshWithBindData();
            Renderer targetRenderer = baker.GetTargetRenderer();
            
            if(newMesh == null || targetRenderer == null)
            {
                return;
            }

            //Meshes are handled differently for SkinnedMeshRenderers and MeshRenderers. Other renderers are not supported
            if (targetRenderer is SkinnedMeshRenderer)
            {
                //record an undo
                Undo.RecordObject(targetRenderer, "Replace Mesh");

                ((SkinnedMeshRenderer)targetRenderer).sharedMesh = newMesh;
            }
            else if(targetRenderer is MeshRenderer)
            {
                MeshFilter targetMeshFilter = targetRenderer.GetComponent<MeshFilter>();

                if(targetMeshFilter != null)
                {
                    //record an undo
                    Undo.RecordObject(targetMeshFilter, "Replace Mesh");

                    targetMeshFilter.sharedMesh = newMesh;
                }
            }
        }
    }
}