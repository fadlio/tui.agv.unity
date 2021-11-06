using System.Collections.Generic;
using RosMessageTypes.Geometry;
using Unity.Robotics.Core;
using Unity.Robotics.UrdfImporter;
using UnityEngine;

namespace Unity.Robotics.SlamExample
{
    class TransformTreeNode
    {
        public readonly GameObject SceneObject;
        public readonly List<TransformTreeNode> Children;
        public Transform Transform => SceneObject.transform;
        public string Name;
        public bool IsALeafNode => Children.Count == 0;

        public TransformTreeNode(GameObject sceneObject) : this(sceneObject, "")
        {
        }
        public TransformTreeNode(GameObject sceneObject, string prefix)
        {
            SceneObject = sceneObject;
            Children = new List<TransformTreeNode>();

            if (prefix != "" && !prefix.EndsWith("_"))
                prefix += "_";

            PopulateChildNodes(this, prefix);
            Name = prefix + sceneObject.name;
        }

        public static TransformStampedMsg ToTransformStamped(TransformTreeNode node)
        {
            return node.Transform.ToROSTransformStamped(Clock.time);
        }
        static void PopulateChildNodes(TransformTreeNode tfNode, string prefix)
        {
            var parentTransform = tfNode.Transform;
            for (var childIndex = 0; childIndex < parentTransform.childCount; ++childIndex)
            {
                var childTransform = parentTransform.GetChild(childIndex);
                var childGO = childTransform.gameObject;

                // If game object has a URDFLink attached, it's a link in the transform tree
                if (childGO.TryGetComponent(out UrdfLink _))
                {
                    var childNode = new TransformTreeNode(childGO, prefix);
                    tfNode.Children.Add(childNode);
                }
            }
        }
    }
}