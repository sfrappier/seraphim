using UnityEngine;

namespace Seraphim
{
    public sealed class PrototypeModels : ScriptableObject
    {
        public GameObject character;
        public GameObject dungeonGate;
        public AnimationClip idle;
        public AnimationClip run;
        public RuntimeAnimatorController animatorController;
        public Avatar humanoidAvatar;
        public Texture2D characterTexture;
        public Material stone;
        public Material ivory;
        public Material darkStone;
        public Material gold;
        public Material cyan;
        public Material glowGold;
        public Material glowCyan;
        public Material celestialMote;
        public Font hudDisplayFont;
        public Material marble;
        public Material cathedralStone;
        public Material cathedralWood;
        public Material vestment;
        public Material angelArmor;
        public CharacterModelDefinition playerModel;
        public CharacterModelDefinition[] demonModels;
        public CharacterModelDefinition bossModel;
    }
}
