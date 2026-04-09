using UnityEditor;
using UnityEngine;

public class CreateRedCube
{
    [MenuItem("Tools/Create Red Cube")]
    static void Execute()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "RedCube";
        cube.transform.position = new Vector3(0, 2, 0);

        Material redMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        redMaterial.color = Color.red;
        cube.GetComponent<Renderer>().material = redMaterial;

        cube.AddComponent<Rigidbody>();

        Selection.activeGameObject = cube;
        Undo.RegisterCreatedObjectUndo(cube, "Create Red Cube");

        Debug.Log("Red cube created at (0, 2, 0) with Rigidbody.");
    }
}
