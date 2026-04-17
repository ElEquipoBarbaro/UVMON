using UnityEngine;

[CreateAssetMenu(fileName = "NewUVmon", menuName = "UVmon/Database/UVmon")]
public class UVmonData : ScriptableObject
{
    [Header("Identidad")]
    public string uvmonName;
    public Sprite portrait;
    public string elementalType;

    [Header("Stats base")]
    public int baseMaxHP = 30;
    public int baseAttack = 10;
    public int baseDefense = 8;
}