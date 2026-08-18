using UnityEngine;
using System.Collections;
public class Unit : MonoBehaviour
{
    public Transform target;
    Vector3[] path;
    int targetIndex;
    void Awake()
    {
        if (target == null)
        {
            Debug.LogError($"{nameof(Unit)} requires a target to be assigned before requesting a path.", this);
            return;
        }
        if (transform == null)
        {
            Debug.LogError($"{nameof(Unit)} requires a valid Transform component.", this);
            return;
        }
    }

    private void Start()
    {
        PathRequestManager.RequestPath(transform.position, target.position, OnPathFound);
    }

    public void OnPathFound(Vector3[] newPath, bool pathSuccessful)
    {
        if (!pathSuccessful || newPath == null || newPath.Length == 0)
        {
            Debug.LogWarning($"{nameof(Unit)} received an invalid path. Ensure the target is reachable.", this);
            return;
        }

        path = newPath;
        targetIndex = 0;
    } 

    public void OnDrawGizmos()
    {
        if (path != null)
        {
            for (int i = targetIndex; i < path.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(path[i], Vector3.one);
                if (i == targetIndex)
                {
                    Gizmos.DrawLine(transform.position, path[i]);
                }
                else
                {
                    Gizmos.DrawLine(path[i - 1], path[i]);
                }
            }
        }
    }
}

