using UnityEngine;

namespace SimKit
{
    public class Node
    {
        public Node parent;
        public TileData target;
        public TileData destination;
        public TileData origin;

        public int baseCost;
        public int costFromOrigin;
        public int costToDestination;
        public int pathCost;

        public Node(TileData current, TileData origin, TileData destination, int pathCost)
        {
            parent = null;
            this.target = current;
            this.origin = origin;
            this.destination = destination;

            baseCost = 1;
            costFromOrigin = (int)Vector3Int.Distance(current.cubeCoordinate, origin.cubeCoordinate);
            costToDestination = (int)Vector3Int.Distance(current.cubeCoordinate, destination.cubeCoordinate);
            this.pathCost = pathCost;
        }

        public int GetCost()
        {
            return pathCost + baseCost + costFromOrigin + costToDestination;
        }

        public void SetParent(Node node)
        {
            this.parent = node;
        }
    }
}