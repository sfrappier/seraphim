using UnityEngine;

namespace Seraphim
{
    public enum DemonRig { GameRig,Minotaur,Scourger }

    // Imported presentation and class tuning stay separate from encounter/combat logic.
    public sealed class CharacterModelDefinition : ScriptableObject
    {
        public GameObject prefab;
        public string displayName;
        public DemonRig rig;
        public float height=2;
        public float baseHealth=42;
        public float moveSpeed=2.6f;
        public int nearTriangles;
        public int distantTriangles;
    }
}
