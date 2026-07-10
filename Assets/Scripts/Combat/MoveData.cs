using UnityEngine;
[CreateAssetMenu(menuName = "Combat/Move")]
public class MoveData : ScriptableObject
{
    public string moveName;
    public int power;
    public int accuracy;

    public MoveEffect effect;
}