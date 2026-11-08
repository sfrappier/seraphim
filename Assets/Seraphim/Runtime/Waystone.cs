using UnityEngine;

namespace Seraphim
{
    public sealed class Waystone : MonoBehaviour
    {
        public bool IsAttuned { get; private set; }
        public Transform crystal;
        public Light beacon;
        public LineRenderer ring;

        public void Attune()
        {
            if (IsAttuned) return;
            IsAttuned = true;
            beacon.color = new Color(1,.78f,.3f);
            beacon.intensity = 4;
            ring.startColor = ring.endColor = new Color(1,.8f,.3f);
        }

        void Update()
        {
            crystal.localPosition = new Vector3(0, 1.8f + Mathf.Sin(Time.time * 1.7f) * .12f, 0);
            crystal.Rotate(Vector3.up, Time.deltaTime * 25, Space.World);
        }
    }
}
