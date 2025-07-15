using UnityEngine;

public static class GameObjectUtils
{
    public static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    public static void SetLayerRecursively(GameObject obj, LayerMask layerMask)
    {
        // first get the mask index
        int layerIndex = LayerMaskToLayer(layerMask);
        SetLayerRecursively(obj, layerIndex);
    }

    /// <summary>
    /// Converts a LayerMask to a layer number
    /// </summary>
    /// <param name="layerMask">The LayerMask to convert</param>
    /// <returns>The layer number</returns>
    public static int LayerMaskToLayer(LayerMask layerMask)
    {
        if (layerMask.value == 0) return 0;

        int layerNumber = 0;
        int layer = layerMask.value;

        // Find the first set bit (rightmost)
        while ((layer & 1) == 0 && layerNumber < 32)
        {
            layer >>= 1;
            layerNumber++;
        }

        return layerNumber;
    }
}