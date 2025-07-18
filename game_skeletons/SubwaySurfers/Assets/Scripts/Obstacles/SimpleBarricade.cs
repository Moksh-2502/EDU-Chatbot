﻿using System.Collections;
using SubwaySurfers.Tutorial.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SimpleBarricade : Obstacle
{
    protected const int k_MinObstacleCount = 1;
    protected const int k_MaxObstacleCount = 2;
    protected const int k_LeftMostLaneIndex = -1;
    protected const int k_RightMostLaneIndex = 1;

    public override IEnumerator Spawn(TrackSegment segment, float t)
    {
        int count = Random.Range(k_MinObstacleCount, k_MaxObstacleCount + 1);
        int startLane = Random.Range(k_LeftMostLaneIndex, k_RightMostLaneIndex + 1);

        Vector3 position;
        Quaternion rotation;
        segment.GetPointAt(t, out position, out rotation);

        for (int i = 0; i < count; ++i)
        {
            int lane = startLane + i;
            lane = lane > k_RightMostLaneIndex ? k_LeftMostLaneIndex : lane;

            AsyncOperationHandle op = Addressables.InstantiateAsync(gameObject.name, position, rotation);
            yield return op;
            if (op.Result == null || !(op.Result is GameObject))
            {
                Debug.LogWarning(string.Format("Unable to load obstacle {0}.", gameObject.name));
                yield break;
            }

            GameObject obj = op.Result as GameObject;

            if (obj == null)
                Debug.Log(gameObject.name);
            else
            {
                obj.transform.position += obj.transform.right * lane * segment.manager.laneOffset;

                obj.transform.SetParent(segment.objectRoot, true);

                //TODO : remove that hack related to #issue7
                Vector3 oldPos = obj.transform.position;
                obj.transform.position += Vector3.back;
                obj.transform.position = oldPos;
            }
        }
    }
}