using UnityEngine;

namespace Seraphim
{
    public sealed class SanctuaryCamera : MonoBehaviour
    {
        Transform target;
        SanctuarySettings settings;
        float distance;
        Vector3 focus;
        Vector3 velocity;

        public void Initialize(Transform follow, SanctuarySettings config)
        {
            target = follow;
            settings = config;
            distance = settings.cameraDistance;
            focus = target.position;
            transform.rotation = Quaternion.Euler(settings.cameraPitch, settings.cameraYaw, 0);
            transform.position = focus - transform.forward * distance;
        }

        void LateUpdate()
        {
            if (target == null) return;
            distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * 1.5f, settings.minCameraDistance, settings.maxCameraDistance);
            focus = Vector3.SmoothDamp(focus, target.position + Vector3.up * .6f, ref velocity, .16f);
            transform.position = focus - transform.forward * distance;
        }
    }
}
