using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [Serializable]
    public struct CharacterWeaponVisualFeedback : IComponentData
    {
        public float3 WeaponLocalPosBob;
        public float3 WeaponLocalPosRecoil;

        public float CurrentRecoil;

        public float TargetRecoilFovKick;
        public float CurrentRecoilFovKick;
    }
}
