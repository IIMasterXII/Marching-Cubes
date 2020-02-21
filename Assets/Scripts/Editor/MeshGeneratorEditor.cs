using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(MeshGenerator))]
public class MeshGeneratorEditor : Editor
{
    public override void OnInspectorGUI(){
        MeshGenerator meshGenerator = (MeshGenerator)target;

        if(DrawDefaultInspector()){
            if(meshGenerator.autoUpdateInEditor){
                meshGenerator.RequestMeshUpdate();
            }
        }

        if(GUILayout.Button("Generate")){
            meshGenerator.RequestMeshUpdate();
        }
    }
}
