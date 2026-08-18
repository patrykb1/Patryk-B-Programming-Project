using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
public class PathRequestManager : MonoBehaviour
{
    Queue<PathRequest> pathRequestQueue = new();
    PathRequest currentPathRequest;
    static PathRequestManager instance;
    Pathfinding pathfinding;
    bool isProcessingPath;

    private void Awake()
    {   // Create an instance, reference pathfinding script
        instance = this;
        pathfinding = GetComponent<Pathfinding>();
    }

    struct PathRequest
    {   // Structure to hold path request data
        public Vector3 pathStart;   // Start world position
        public Vector3 pathEnd;     // End world position
        public Action<Vector3[], bool> callback;    // Stores the path and if pathfinding was successful
        public PathRequest(Vector3 _start, Vector3 _end, Action<Vector3[], bool> _callback)
        {
            pathStart = _start;
            pathEnd = _end;
            callback = _callback;
        }
    }

    public static void RequestPath(Vector3 start, Vector3 end, Action<Vector3[], bool> callback)
    {   // Path error handling, ensure everything works as intended
        if(instance == null)
        {
            Debug.LogError("No PathRequestManager instance found in the scene.");
            return;
        }
        if (start == null || end == null)
        {
            Debug.LogError("Start or end position is null in PathRequestManager.RequestPath.");
            return;
        }
        if (callback == null)
        {
            Debug.LogError("Callback function is null in PathRequestManager.RequestPath.");
            return;
        }   // Add path to queue
        PathRequest newRequest = new(start, end, callback);
        instance.pathRequestQueue.Enqueue(newRequest);
        instance.TryProcessNext();
    }

    void TryProcessNext()
    {   // Process the next path request in the queue
        if(!isProcessingPath && pathRequestQueue.Count > 0)
        {   // If not currently processing a path and there is a path to process
            currentPathRequest = pathRequestQueue.Dequeue();
            isProcessingPath = true;
            pathfinding.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd);
        }
    }

    public void FinishedProcessingPath(Vector3[] path, bool success)
    {   // Call the callback with the path and success status
        currentPathRequest.callback(path, success);
        isProcessingPath = false;
        TryProcessNext();
    }
}
