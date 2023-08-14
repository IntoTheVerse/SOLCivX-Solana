using UnityEngine;
using UnityEngine.Pool;

namespace SimKit
{
    public class Pooling : MonoBehaviour
    {
        private ObjectPool<GameObject> _pool;

        public void InitPool(GameObject prefab)
        {
            _pool = new ObjectPool<GameObject>(() =>
            {
                return Instantiate(prefab);
            }, shape =>
            {
                shape.SetActive(true);
            }, shape =>
            {
                shape.SetActive(false);
            }, shape =>
            {
                Destroy(shape);
            }, false, 10, 25);
        }

        public GameObject GetFromPool()
        {
            return _pool.Get();
        }

        public void ReleaseToPool(GameObject element)
        {
            _pool.Release(element);
        }
    }
}