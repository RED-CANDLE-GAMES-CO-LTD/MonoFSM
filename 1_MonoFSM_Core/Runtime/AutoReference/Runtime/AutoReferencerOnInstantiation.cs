/* Author: Oran Bar
 * Summary: If the instantiated object has this script prior to its instantiation, auto will reference all variables of attached components correctly.
 * The alsoReferenceChildren boolean will determine if the referencing has to be done recursively to all its children, or only on this gameobject.
 */

using System;
using UnityEngine;
using Auto.Utils;

[ScriptTiming(-20000)]
[Obsolete] //MonoPoolObj 已經有類似功能了
public class AutoReferencerOnInstantiation : MonoBehaviour
{
    // public bool alsoReferenceChildren = true;

    // private void Awake() //hmm...和prefabcache合併？
    // {
    //     AutoAttributeManager.AutoReferenceAllChildren(gameObject);
    // }
}
