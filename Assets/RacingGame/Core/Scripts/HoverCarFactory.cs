using UnityEngine;

public static class HoverCarFactory
{
    public static Transform RebuildVisual(Transform root, string visualRootName = "VisualRoot")
    {
        if (root == null)
            return null;

        Transform existing = root.Find(visualRootName);
        if (existing != null)
            Object.Destroy(existing.gameObject);

        Transform visualRoot = new GameObject(visualRootName).transform;
        visualRoot.SetParent(root, false);

        CreatePart("Body", PrimitiveType.Cube, visualRoot, new Vector3(0f, 0f, 0f), new Vector3(1.25f, 0.24f, 2.4f), new Color(0.08f, 0.14f, 0.2f));
        CreatePart("Cabin", PrimitiveType.Cube, visualRoot, new Vector3(0f, 0.22f, 0.15f), new Vector3(0.72f, 0.22f, 0.9f), new Color(0.42f, 0.92f, 1f));
        CreatePart("Nose", PrimitiveType.Cube, visualRoot, new Vector3(0f, 0.05f, 1.15f), new Vector3(0.52f, 0.14f, 0.45f), new Color(0.95f, 0.28f, 0.62f));
        CreatePart("LeftWing", PrimitiveType.Cube, visualRoot, new Vector3(-0.92f, -0.02f, 0.15f), new Vector3(0.42f, 0.06f, 1.3f), new Color(0.18f, 0.82f, 1f));
        CreatePart("RightWing", PrimitiveType.Cube, visualRoot, new Vector3(0.92f, -0.02f, 0.15f), new Vector3(0.42f, 0.06f, 1.3f), new Color(0.18f, 0.82f, 1f));
        CreatePart("RearLeft", PrimitiveType.Cylinder, visualRoot, new Vector3(-0.46f, -0.18f, -1.05f), new Vector3(0.2f, 0.08f, 0.2f), new Color(1f, 0.72f, 0.22f));
        CreatePart("RearRight", PrimitiveType.Cylinder, visualRoot, new Vector3(0.46f, -0.18f, -1.05f), new Vector3(0.2f, 0.08f, 0.2f), new Color(1f, 0.72f, 0.22f));
        CreatePart("GlowLeft", PrimitiveType.Sphere, visualRoot, new Vector3(-0.55f, -0.17f, -1.18f), new Vector3(0.16f, 0.06f, 0.16f), new Color(1f, 0.5f, 0.18f));
        CreatePart("GlowRight", PrimitiveType.Sphere, visualRoot, new Vector3(0.55f, -0.17f, -1.18f), new Vector3(0.16f, 0.06f, 0.16f), new Color(1f, 0.5f, 0.18f));
        CreatePart("FrontGlow", PrimitiveType.Sphere, visualRoot, new Vector3(0f, 0.02f, 1.38f), new Vector3(0.18f, 0.05f, 0.18f), new Color(0.28f, 0.95f, 1f));

        return visualRoot;
    }

    private static void CreatePart(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;

        Collider colliderRef = part.GetComponent<Collider>();
        if (colliderRef != null)
            Object.Destroy(colliderRef);

        Renderer rendererRef = part.GetComponent<Renderer>();
        rendererRef.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendererRef.receiveShadows = false;
        rendererRef.sharedMaterial.color = color;
    }
}
