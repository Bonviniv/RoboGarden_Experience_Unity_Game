using UnityEngine;

public class TurtleState
{
    public Vector3 position;
    public Quaternion rotation;

    public TurtleState(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotation = rot;
    }
}