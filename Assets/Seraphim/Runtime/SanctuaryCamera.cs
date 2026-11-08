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
        float shake;
        public float DivinePower { get; set; }
        public float ShowcaseZoom { get; set; }
        public void Impact() { shake=.16f; }

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
            if (ShowcaseZoom<=0) distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * 1.5f, settings.minCameraDistance, settings.maxCameraDistance);
            focus = Vector3.SmoothDamp(focus, target.position + Vector3.up * .6f, ref velocity, .16f);
            shake=Mathf.MoveTowards(shake,0,Time.deltaTime*.35f);
            var tremor=transform.right*(Mathf.PerlinNoise(Time.time*31,0)-.5f)*shake
                +transform.up*(Mathf.PerlinNoise(0,Time.time*29)-.5f)*shake;
            transform.position = focus - transform.forward * (distance+ShowcaseZoom+DivinePower*2.2f)+tremor;
        }
    }
}
