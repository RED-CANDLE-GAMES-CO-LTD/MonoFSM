using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
public class DirectionRotater : MonoBehaviour
{
    public enum Direction
    {
        Up, Down, Left, Right
    }
    [EnumToggleButtons]
    public Direction direction;
    private void OnValidate()
    {
        switch (direction)
        {
            case Direction.Up:
                transform.rotation = Quaternion.Euler(0, 0, 90);
                break;
            case Direction.Right:
                transform.rotation = Quaternion.identity;
                break;
            case Direction.Left:
                transform.rotation = Quaternion.Euler(0, 0, -180);
                break;
            case Direction.Down:
                transform.rotation = Quaternion.Euler(0, 0, -90);
                break;
        }
    }
}
