using UnityEngine;

namespace SimKit
{
    public static class Utils
    {
        public static int GetRandomWeightedIndex(this float[] weights)
        {
            float weightSum = 0f;
            for (int i = 0; i < weights.Length; ++i)
            {
                weightSum += weights[i];
            }

            int index = 0;
            float lastIndex = weights.Length - 1;
            while (index < lastIndex)
            {
                if (Random.Range(0, weightSum) < weights[index])
                {
                    return index;
                }

                weightSum -= weights[index++];
            }

            return index;
        }

        public static Vector3Int OffsetToCube(this Vector2Int offset)
        {
            var q = offset.x - (offset.y + (offset.y % 2)) / 2;
            var r = offset.y;
            return new Vector3Int(q, r, -q - r);
        }

        public static T GetRandom<T>(this T[] array)
        {
            return array[Random.Range(0, array.Length)];
        }

        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static void SwapLayer(this GameObject go, string layer)
        {
            go.layer = LayerMask.NameToLayer(layer);
        }
    }
}