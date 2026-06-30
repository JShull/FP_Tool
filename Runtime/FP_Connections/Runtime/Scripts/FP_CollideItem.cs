// Copyright (c) 2026 John B. Shull
// FuzzPhyte LLC is a company associated with John B. Shull
// This file is part of FP_Tool Package.
//
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
//
// See LICENSE.md COMMERCIAL-LICENSE.md, and NOTICE.md.

using UnityEngine;
using FuzzPhyte.Utility;
using UnityEngine.EventSystems;
using System.Collections.Generic;
namespace FuzzPhyte.Tools.Connections
{
    public class FP_CollideItem : MonoBehaviour, IFPToolEndPoint
    {
        public FP_MoveRotateItem MoveRotateItem;
        [SerializeField]
        protected GameObject rootItem;
        [SerializeField]
        protected List<GameObject> endPoints = new List<GameObject>();
        [SerializeField]
        protected GameObject leftEndPoint;
        [SerializeField]
        protected GameObject rightEndPoint;
        #region Interface for IFPToolEndPoint
        public GameObject OnToolSelectionReturnClosestEndPoint(PointerEventData eventData)
        {
            //closest gameobject to the mouse click position
            float closestDistance = float.MaxValue;
            GameObject closestEndPoint = null;
            for (int i=0; i < endPoints.Count; i++)
            {
                var endPoint = endPoints[i];
                var distance = Vector3.Distance(eventData.pointerCurrentRaycast.worldPosition, endPoint.transform.position);
                if(distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEndPoint = endPoint;
                }
            }
            return closestEndPoint;
        }

        public List<GameObject> OnToolSelectionReturnEndPoints()
        {
            return endPoints;
        }

        public (bool, Vector3) ReturnWorldMidPoint()
        {
            if (rightEndPoint != null && leftEndPoint != null)
            {
                return (true, (rightEndPoint.transform.position + leftEndPoint.transform.position) * 0.5f);
            }
            return (false, Vector3.zero);
        }
        public GameObject OnToolSelectionReturnRootGameObject()
        {
            return rootItem;
        }
        #endregion
    }
}
