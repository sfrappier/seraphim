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
        public float Speed => keyboardControl ? keyboardSpeed : Agent != null ? Agent.velocity.magnitude : 0;
        public bool IsKeyboardMoving => keyboardControl;
        public bool IsAnimationPlaying(string state) => animationPlayer != null && animationPlayer.GetCurrentAnimatorStateInfo(0).IsName(state);
        Animator animationPlayer;
        LineRenderer marker;
        float nextCommand;
        float markerLife;
        bool keyboardControl;
        float keyboardSpeed;

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
            StopKeyboardMovement();
            Agent.SetPath(path);
            Destination = hit.position;
            HasDestination = true;
            marker.transform.position = Destination + Vector3.up * .05f;
            markerLife = 1;
            return true;
        }

        public bool MoveFromScreen(Vector2 screenPosition)
        {
            if (SanctuaryHud.BlocksScreenPoint(screenPosition)) return false;
            var camera = Camera.main;
            if (camera == null) return false;
            var ray = camera.ScreenPointToRay(screenPosition);
            return Physics.Raycast(ray,out var hit,150,1 << 8,QueryTriggerInteraction.Ignore) && MoveTo(hit.point);
        }

        public Vector3 KeyboardDirection(Vector2 input)
        {
            var camera=Camera.main;
            var forward=camera!=null ? camera.transform.forward : Vector3.forward;
            forward.y=0;
            forward.Normalize();
            var right=Vector3.Cross(Vector3.up,forward);
            return Vector3.ClampMagnitude(right*input.x+forward*input.y,1);
        }

        public void MoveWithKeyboard(Vector2 input,float deltaTime)
        {
            if (!Agent.isOnNavMesh || deltaTime<=0) return;
            var direction=KeyboardDirection(input);
            if (direction.sqrMagnitude<.001f) { StopKeyboardMovement(); return; }
            if (!keyboardControl) Agent.ResetPath();
            keyboardControl=true;
            Agent.updateRotation=false;
            HasDestination=false;
            markerLife=0;
            var before=transform.position;
            // Sweep the complete frame displacement so heavy effects do not
            // slow keyboard movement when a frame takes more than 100 ms.
            var movement=direction*Agent.speed*deltaTime;
            if (Agent.Raycast(before+movement,out var edge))
            {
                movement=edge.position-before;
                movement.y=0;
                if (movement.magnitude>.025f) movement-=movement.normalized*.025f;
                else movement=Vector3.zero;
                // Slide along the boundary while respecting the baked agent radius.
                var slide=Vector3.ProjectOnPlane(direction,edge.normal)*Agent.speed*deltaTime;
                if (slide.sqrMagnitude>movement.sqrMagnitude && !Agent.Raycast(before+slide,out _)) movement=slide;
            }
            Agent.Move(movement);
            keyboardSpeed=movement.magnitude/Mathf.Max(deltaTime,.001f);
            transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),Agent.angularSpeed*deltaTime);
        }

        public void StopKeyboardMovement()
        {
            if (!keyboardControl) return;
            keyboardControl=false;
            keyboardSpeed=0;
            Agent.updateRotation=true;
            if (Agent.isOnNavMesh) { Agent.ResetPath(); Agent.velocity=Vector3.zero; }
        }

        void Update()
        {
            if (InputEnabled)
            {
                var keys=new Vector2((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),
                    (Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0));
                if (keys.sqrMagnitude>.01f) MoveWithKeyboard(keys,Time.deltaTime);
                else
                {
                    StopKeyboardMovement();
                    if (Input.GetMouseButton(0) && Time.unscaledTime>=nextCommand)
                    {
                        nextCommand=Time.unscaledTime+.08f;
                        MoveFromScreen(Input.mousePosition);
                    }
                }
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
