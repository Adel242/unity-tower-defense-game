using UnityEngine;

public class PathGizmo : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        if (transform.childCount < 2)
        {
            return;
        }

        for (int i = 0; i < transform.childCount - 1; i++)
        {
            Transform current = transform.GetChild(i);
            Transform next = transform.GetChild(i + 1);

            Gizmos.DrawSphere(current.position, 0.3f);
            Gizmos.DrawLine(current.position, next.position);
        }

        Transform last = transform.GetChild(transform.childCount - 1);
        Gizmos.DrawSphere(last.position, 0.3f);
    }
}