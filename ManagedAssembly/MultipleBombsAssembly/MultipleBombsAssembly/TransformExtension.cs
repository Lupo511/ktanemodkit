using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public static class TransformExtension
    {
        public static Transform FindRecursive(this Transform transform, string name)
        {
            Transform found = transform.Find(name);
            if (found != null)
                return found;
            foreach (Transform child in transform)
            {
                found = child.FindRecursive(name);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
