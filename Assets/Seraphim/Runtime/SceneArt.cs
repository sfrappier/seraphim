using UnityEngine;
using UnityEngine.Rendering;

namespace Seraphim
{
    internal static class SceneArt
    {
        public static GameObject Shape(PrimitiveType type, string name, Transform parent,
            Vector3 position, Vector3 scale, Material material, bool collision = false)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.Destroy(obj.GetComponent<Collider>());
            return obj;
        }

        public static LineRenderer Ring(string name, Transform parent, float radius,
            float width, Material material, int segments = 96)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.widthMultiplier = width;
            line.sharedMaterial = material;
            line.positionCount = segments;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (var i = 0; i < segments; i++)
            {
                var angle = i * Mathf.PI * 2 / segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
            }
            return line;
        }

        public static Light PointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = position;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }
    }
}
