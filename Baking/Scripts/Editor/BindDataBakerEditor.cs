using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BindDataBaker))]
public class BindDataBakerEditor : Editor
{
    private static bool sEnableDebugVisualization = true;
    private static List<Material> sMaterialsToUpdate = new List<Material>(0);

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
            int targetRendererIndex = 0;

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
                        targetRendererIndex = targetRenderersList.Count - 1;
                    }
                }
            }

            ///choose the target renderer and mesh with a popup menu
            targetRendererIndex = EditorGUILayout.Popup("Renderer", targetRendererIndex, targetRendererNames.ToArray());

            if(targetRenderersList.Count > targetRendererIndex && targetMeshesList.Count > targetRendererIndex)
            {
                newTargetRenderer = targetRenderersList[targetRendererIndex];
                newTargetMesh = targetMeshesList[targetRendererIndex];
            }
        }

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
        if (baker.SaveSettings(newTargetObject, newTargetRenderer, newTargetMesh, newMask, newMaskColorCount, newMaskColors, newInvalidMaskColor))
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

        //display the baked textures
        GUILayout.Label("Result Bind Textures", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.ObjectField("Positions", baker.GetBindPositions(), typeof(Texture2D), false);
        EditorGUILayout.ObjectField("Normals", baker.GetBindNormals(), typeof(Texture2D), false);
        EditorGUILayout.ObjectField("Tangents", baker.GetBindTangents(), typeof(Texture2D), false);
        EditorGUI.indentLevel--;

        if (GUILayout.Button("Save Bind Data Textures"))
        {
            string filepath = EditorUtility.SaveFilePanel("Save SDF Texture", "", "Bind", "");

            //save all 3 textures with the same start to their name and different suffixes
            if (filepath.Length > 0)
            {
                filepath = "Assets" + filepath.Remove(0, Application.dataPath.Length);

                AssetDatabase.CreateAsset(baker.GetBindPositions(), filepath + "Positions.asset");
                AssetDatabase.CreateAsset(baker.GetBindNormals(), filepath + "Normals.asset");
                AssetDatabase.CreateAsset(baker.GetBindTangents(), filepath + "Tangents.asset");
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

        GUILayout.Label("Auto Update Materials", EditorStyles.boldLabel);

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
            Undo.RecordObjects(sMaterialsToUpdate.ToArray(), "Update Materials");

            foreach (Material material in sMaterialsToUpdate)
            {
                material.SetTexture("_VertexBindPositions", baker.GetBindPositions());
                material.SetTexture("_VertexBindNormals", baker.GetBindNormals());
                material.SetTexture("_VertexBindTangents", baker.GetBindTangents());
            }
        }
    }
}