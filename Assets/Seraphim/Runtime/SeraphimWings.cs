using UnityEngine;

namespace Seraphim
{
    public sealed class SeraphimWings : MonoBehaviour
    {
        readonly Transform[] joints = new Transform[6];
        readonly Quaternion[] rest = new Quaternion[6];
        SeraphimMotor motor;
        Mesh featherMesh;
        Transform halo;
        public float Ascension { get; set; }
        public int WingCount => joints.Length;

        public void Initialize(PrototypeModels art, SeraphimMotor movement)
        {
            motor = movement;
            featherMesh = CreateFeather();
            for (var tier = 0; tier < 3; tier++)
            for (var side = 0; side < 2; side++)
            {
                var index = tier * 2 + side;
                var sign = side == 0 ? -1 : 1;
                var joint = new GameObject($"Wing {tier + 1} {(sign < 0 ? "Left" : "Right")}").transform;
                joint.SetParent(transform, false);
                joint.localPosition = new Vector3(sign * .14f, 1.38f - tier * .25f, -.17f);
                rest[index] = Quaternion.Euler(-35, sign * (10 + tier * 13), sign * (40 - tier * 37));
                joint.localRotation = rest[index];
                joints[index] = joint;
                for (var feather = 0; feather < 9; feather++)
                {
                    var part = new GameObject("Ivory feather");
                    part.transform.SetParent(joint, false);
                    part.transform.localPosition = new Vector3(sign * (.12f + feather * .135f), .08f, -feather * .035f);
                    part.transform.localRotation = Quaternion.Euler(-12, sign * -18, sign * (-18 - feather * 4));
                    part.transform.localScale = new Vector3(sign * .34f, .5f + feather * .08f, 1);
                    part.AddComponent<MeshFilter>().sharedMesh = featherMesh;
                    part.AddComponent<MeshRenderer>().sharedMaterial = art.ivory;
                }
            }
            halo = SceneArt.Ring("Halo", transform, .32f, .045f, art.glowGold, 64).transform;
            halo.localPosition = new Vector3(0, 2.15f, 0);
            halo.localRotation = Quaternion.Euler(18, 0, 0);
            SceneArt.PointLight("Celestial glow", transform, Vector3.up * 1.2f, new Color(.6f,.8f,1), 1.2f, 3.5f);
        }

        void Update()
        {
            var moving = motor != null && motor.Speed > .1f;
            var speed = moving ? 5 : 2.4f;
            for (var i = 0; i < joints.Length; i++)
            {
                var sign = i % 2 == 0 ? -1 : 1;
                var tier=i/2;
                joints[i].localPosition=new Vector3(sign*.14f,1.38f-tier*.25f+Ascension*.4f,-.17f);
                joints[i].localScale=Vector3.one*(1+Ascension*.55f);
                joints[i].localRotation = rest[i] * Quaternion.Euler(Mathf.Sin(Time.time * speed + i / 2f) * 5,
                    sign * (Mathf.Sin(Time.time * speed + i / 2f) * (moving ? 14 : 6)+Ascension*25),sign*Ascension*12);
            }
            halo.localPosition=new Vector3(0,2.15f+Ascension*.6f,0);
            halo.localScale=Vector3.one*(1+Ascension*.8f);
        }

        static Mesh CreateFeather()
        {
            var mesh = new Mesh { name = "Seraphim feather" };
            mesh.vertices = new[] { new Vector3(0,0,0),new Vector3(-.48f,.3f,0),new Vector3(-.34f,.73f,0),
                new Vector3(0,1,0),new Vector3(.34f,.73f,0),new Vector3(.48f,.3f,0),new Vector3(0,.52f,.055f) };
            mesh.triangles = new[] {0,1,6,1,2,6,2,3,6,3,4,6,4,5,6,5,0,6,6,1,0,6,2,1,6,3,2,6,4,3,6,5,4,6,0,5};
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        void OnDestroy() { if (featherMesh != null) Destroy(featherMesh); }
    }
}
