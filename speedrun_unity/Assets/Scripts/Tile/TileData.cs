using System.Collections.Generic;
using UnityEngine;

namespace SimKit
{
    public class TileData : MonoBehaviour, ISelectable
    {
        public Vector2Int offsetCoordinate;
        public Vector3Int cubeCoordinate;
        public Tile selfInfo;
        public GameObject fow = null;
        public List<TileData> neighbours = new();
        public List<GameObject> playersOnTop = new();

        [HideInInspector] public ResourceOnTileManager resourceSpawner;
        [HideInInspector] public ResourceOnTileManager spawnedResourceUIOnTile = null;
        [HideInInspector] public bool hasResources;
        [HideInInspector] public bool isVisible;

        private List<GameObject> _spawnedOnTop = new();
        private MeshRenderer _renderer;

        public void OnHighlight()
        {
            TileManager.instance.OnHighlightTile(this);
        }

        public void OnSelect()
        {
            TileManager.instance.OnSelectTile(this);
        }

        public void OnDehighlight()
        {
            //Do something
        }

        public void OnDeselect(bool noOtherSelected = false)
        {
            //Do something
        }

        public void SetupTile()
        {
            _renderer = GetComponent<MeshRenderer>();
            if (selfInfo.maximumObjectsSpawnOnTile > 0) SetObjectsOnTop();
            if (selfInfo.resources.Length > 0) SetResourcesOnTile();
        }

        private void SetObjectsOnTop()
        {
            MeshCollider collider = GetComponent<MeshCollider>();
            if (selfInfo.objsOnTile.Length == 0) return;

            int numOfObjsOnTile = Random.Range(selfInfo.minimunObjectsSpawnOnTile, selfInfo.maximumObjectsSpawnOnTile + 1);

            float[] weights = new float[selfInfo.objsOnTile.Length];
            for (int i = 0; i < selfInfo.objsOnTile.Length; i++)
            {
                weights[i] = selfInfo.objsOnTile[i].weight;
            }

            for (int i = 0; i < numOfObjsOnTile; i++)
            {
                GameObject obj = Instantiate(selfInfo.objsOnTile[weights.GetRandomWeightedIndex()].obj, transform);
                Vector3 pos = transform.position + new Vector3(Random.Range(-selfInfo.OuterSize * 0.8f, selfInfo.OuterSize * 0.8f), 0, Random.Range(-selfInfo.OuterSize * 0.8f, selfInfo.OuterSize * 0.8f));
                Collider[] colliders = Physics.OverlapSphere(pos, 0f);

                bool canPass = false;
                foreach (var item in colliders)
                {
                    if (item == collider) canPass = true; break;
                }

                while (!canPass)
                {
                    pos = transform.position + new Vector3(Random.Range(-selfInfo.OuterSize * 0.8f, selfInfo.OuterSize * 0.8f), 0, Random.Range(-selfInfo.OuterSize * 0.8f, selfInfo.OuterSize * 0.8f));
                    colliders = Physics.OverlapSphere(pos, 0f);
                    foreach (var item in colliders)
                    {
                        if (item == collider) canPass = true; break;
                    }
                }

                obj.transform.position = pos + new Vector3(0, selfInfo.Height / 2, 0);
                obj.SwapLayer("Hidden");
                _spawnedOnTop.Add(obj);
            }
        }

        private void SetResourcesOnTile()
        {
            for (int i = 0; i < selfInfo.resources.Length; i++)
            {
                float probability = Random.Range(0, 0.999999f);
                if (probability <= selfInfo.resources[i].probabilityOfSpawning)
                {
                    hasResources = true;
                    spawnedResourceUIOnTile = Instantiate(resourceSpawner, transform);
                    spawnedResourceUIOnTile.InitResourceOnTile(selfInfo.resources[i].resource, Random.Range(selfInfo.resources[i].lowestPossibleSpawnAmount, selfInfo.resources[i].highestPossibleSpawnAmount));
                    spawnedResourceUIOnTile.gameObject.SetActive(false);
                }
            }
        }

        public void RemoveResourceUIOnTile()
        {
            spawnedResourceUIOnTile?.gameObject.SetActive(false);
        }

        public bool Reveal()
        {
            if (fow == null) return false;
            gameObject.SwapLayer("Tiles");
            foreach (var obj in _spawnedOnTop)
            {
                obj.SwapLayer("Default");
            }
            fow.SetActive(false);
            fow = null;
            spawnedResourceUIOnTile?.gameObject.SetActive(true);
            return true;
        }

        public void SetOutOfView()
        {
            isVisible = false;
            _renderer.material = selfInfo.OutOfViewMaterial;
        }

        public void SetInView()
        {
            isVisible = true;
            _renderer.material = selfInfo.Material;
        }
    }
}