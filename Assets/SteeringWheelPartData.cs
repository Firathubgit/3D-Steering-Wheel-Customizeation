using UnityEngine;

[System.Serializable]
public class MaterialOption
{
    public Material material;
    public float price;
    public Sprite previewSprite; // <<-- Add this line for a thumbnail preview
}

[System.Serializable]
public class SteeringWheelPartData
{
    public string partName;
    public Transform partRoot;
    public MaterialOption[] materialOptions;
}
