using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = Random.Range(0, n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    // public static T PopLast<T>(this List<T> list)
    // {
    //     if (list.Count == 0) Debug.LogError("No element to pop");
    //     var lastIndex = list.Count - 1;
    //     var lastElement = list[lastIndex];
    //     list.RemoveAt(lastIndex);
    //     return lastElement;
    // }
    public static int PreviousIndex<T>(this List<T> array, ref int current)
    {
        current--;
        if (current < 0) current = array.Count - 1;
        return current;
    }

    public static int NextIndex<T>(this List<T> list, ref int current)
    {
        current++;
        if (current >= list.Count) current = 0;
        return current;
    }

    public static T RandomSelect<T>(this List<T> list)
    {
        var index = Random.Range(0, list.Count);
        return list[index];
    }

    public static T First<T>(this List<T> list)
    {
        return list[0];
    }

    public static T Last<T>(this List<T> list)
    {
        return list[list.Count - 1];
    }

    public static T Pop<T>(this List<T> list)
    {
        var item = list[^1];
        list.RemoveAt(list.Count - 1);
        return item;
    }
}