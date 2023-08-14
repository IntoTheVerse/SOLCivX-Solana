using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimKit
{
    public class PathFinder : MonoBehaviour
    {
        public static List<TileData> FindPath(TileData origin, TileData destination)
        {
            Dictionary<TileData, Node> nodesNotEvaluated = new();
            Dictionary<TileData, Node> nodesAlreadyEvaluated = new();

            Node startNode = new(origin, origin, destination, 0);

            nodesNotEvaluated.Add(origin, startNode);

            bool gotPath = EvaluateNextNode(nodesNotEvaluated, nodesAlreadyEvaluated, origin, destination, out List<TileData> path);

            while (!gotPath)
            {
                gotPath = EvaluateNextNode(nodesNotEvaluated, nodesAlreadyEvaluated, origin, destination, out path);
            }

            path.Reverse();
            return path;
        }

        private static bool EvaluateNextNode(Dictionary<TileData, Node> nodesNotEvaluated, Dictionary<TileData, Node> nodesAlreadyEvaluated, TileData origin, TileData destination, out List<TileData> path)
        {
            Node currentNode = GetCheapestNode(nodesNotEvaluated.Values.ToArray());

            if (currentNode == null)
            {
                path = new List<TileData>();
                return false;
            }

            nodesNotEvaluated.Remove(currentNode.target);
            nodesAlreadyEvaluated.Add(currentNode.target, currentNode);

            path = new List<TileData>();

            if (currentNode.target == destination)
            {
                path.Add(currentNode.target);
                while (currentNode.target != origin)
                {
                    path.Add(currentNode.parent.target);
                    currentNode = currentNode.parent;
                }
                return true;
            }

            List<Node> neighbours = new();
            foreach (TileData tile in currentNode.target.neighbours)
            {
                Node node = new(tile, origin, destination, currentNode.GetCost());

                if (!tile.selfInfo.Walkable)
                    node.baseCost = 9999999;

                neighbours.Add(node);
            }

            foreach (Node neighbour in neighbours)
            {
                if (nodesAlreadyEvaluated.Keys.Contains(neighbour.target)) continue;

                if (neighbour.GetCost() < currentNode.GetCost() || !nodesNotEvaluated.Keys.Contains(neighbour.target))
                {
                    neighbour.SetParent(currentNode);
                    if (!nodesNotEvaluated.Keys.Contains(neighbour.target))
                    {
                        nodesNotEvaluated.Add(neighbour.target, neighbour);
                    }
                }
            }

            return false;
        }

        private static Node GetCheapestNode(Node[] nodesNotEvaluated)
        {
            if (nodesNotEvaluated.Length == 0) return null;

            Node selectedNode = nodesNotEvaluated[0];

            for (int i = 1; i < nodesNotEvaluated.Length; i++)
            {
                var currentNode = nodesNotEvaluated[i];
                if (currentNode.GetCost() < selectedNode.GetCost())
                    selectedNode = currentNode;
                else if (currentNode.GetCost() == selectedNode.GetCost() && currentNode.costToDestination < selectedNode.costToDestination)
                    selectedNode = currentNode;
            }

            return selectedNode;
        }
    }
}