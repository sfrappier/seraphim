using UnityEngine;
using UnityEngine.AI;

namespace Seraphim
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class SeraphimMotor : MonoBehaviour
    {
        public NavMeshAgent Agent { get; private set; }
        public Vector3 Destination { get; private set; }
        public bool HasDestination { get; private set; }
        public bool InputEnabled { get; set; } = true;
        public float Speed => Agent != null ? Agent.velocity.magnitude : 0;
        public bool IsAnimationPlaying(string state) => animationPlayer != null && animationPlayer.GetCurrentAnimatorStateInfo(0).IsName(state);
        Animator animationPlayer;
        LineRenderer marker;
        float nextCommand;
        float markerLife;

        public void Initialize(SanctuarySettings settings, Animator avatarAnimation, Material markerMaterial)
        {
            Agent = GetComponent<NavMeshAgent>();
            Agent.speed = settings.moveSpeed;
            Agent.acceleration = settings.acceleration;
            Agent.angularSpeed = 720;
            Agent.radius = .4f;
            Agent.height = 1.8f;
            Agent.stoppingDistance = .14f;
            Agent.autoBraking = true;
            Agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            animationPlayer = avatarAnimation;
            marker = SceneArt.Ring("Destination", null, .42f, .035f, markerMaterial, 48);
            marker.enabled = false;
        }

        public bool MoveTo(Vector3 point)
        {
            if (!Agent.isOnNavMesh || !NavMesh.SamplePosition(point, out var hit, 1.5f, NavMesh.AllAreas)) return false;
            var path = new NavMeshPath();
            if (!Agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) return false;
            Agent.SetPath(path);
            Destination = hit.position;
            HasDestination = true;
            marker.transform.position = Destination + Vector3.up * .05f;
            markerLife = 1;
            return true;
        }

        public bool MoveFromScreen(Vector2 screenPosition)
        {
            var camera = Camera.main;
            if (camera == null) return false;
            var ray = camera.ScreenPointToRay(screenPosition);
            return Physics.Raycast(ray,out var hit,150,1 << 8,QueryTriggerInteraction.Ignore) && MoveTo(hit.point);
        }

        void Update()
        {
            if (InputEnabled && Input.GetMouseButton(0) && Time.unscaledTime >= nextCommand && Input.mousePosition.y > 125)
            {
                nextCommand = Time.unscaledTime + .08f;
                MoveFromScreen(Input.mousePosition);
            }
            if (animationPlayer != null)
            {
                animationPlayer.SetFloat("Speed",Speed,.1f,Time.deltaTime);
            }
            if (HasDestination && !Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance + .08f) HasDestination = false;
            markerLife = Mathf.Max(0, markerLife - Time.deltaTime * .9f);
            marker.enabled = markerLife > 0;
            marker.startColor = marker.endColor = new Color(1, .84f, .4f, markerLife);
        }

        void OnDestroy()
        {
            if (marker != null) Destroy(marker.gameObject);
        }
    }
}
