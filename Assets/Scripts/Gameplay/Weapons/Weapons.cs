using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [GhostComponent]
    public struct BaseWeapon : IComponentData
    {
        public Entity ShotOrigin;
        public bool Automatic;
        public float FiringRate;
        public float SpreadRadians;
        public int ProjectilesPerShot;

        [GhostField]
        public float ShotTimer;
        [GhostField]
        public bool IsFiring;
        [GhostField]
        public uint TotalShotsCount;
        [GhostField]
        public uint TotalProjectilesCount;

        // Local data
        public byte LastVisualTotalShotsCountInitialized;
        public uint LastVisualTotalShotsCount;
        public byte LastVisualTotalProjectilesCountInitialized;
        public uint LastVisualTotalProjectilesCount;
    }

    public enum RaycastWeaponVisualsSyncMode
    {
        Precise,
        BandwidthEfficient,
    }

    public struct RaycastWeapon : IComponentData
    {
        public Entity ProjectilePrefab;
        public RaycastWeaponVisualsSyncMode VisualsSyncMode;

        public uint LastProcessedProjectileVisualEventTick;
    }

    public struct PrefabWeapon : IComponentData
    {
        public Entity ProjectilePrefab;
    }

    public struct WeaponProjectileEvent : IBufferElementData
    {
        public uint Id;
        public float3 SimulationPosition;
        public float3 VisualPosition;
        public float3 SimulationDirection;
    }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToNonOwner)]
    public struct RaycastWeaponVisualProjectileEvent : IBufferElementData
    {
        [GhostField]
        public uint Tick;
        [GhostField]
        public byte DidHit;
        [GhostField]
        public float3 EndPoint;
        [GhostField]
        public float3 HitNormal;
    }

    [GhostComponent]
    public struct ActiveWeapon : IComponentData
    {
        [GhostField]
        public Entity Entity;
        public Entity PreviousEntity;
    }

    [Serializable]
    public struct WeaponVisualFeedback : IComponentData
    {
        [Serializable]
        public struct Authoring
        {
            [Header("Bobbing")]
            public float WeaponBobHAmount;
            public float WeaponBobVAmount;
            public float WeaponBobFrequency;
            public float WeaponBobSharpness;
            public float WeaponBobAimRatio;

            [Header("Recoil")]
            public float RecoilStrength;
            public float RecoilMaxDistance;
            public float RecoilSharpness;
            public float RecoilRestitutionSharpness;

            [Header("Aiming")]
            public float AimFovRatio;
            public float AimFovSharpness;
            public float LookSensitivityMultiplierWhileAiming;

            [Header("Fov Kick")]
            public float RecoilFovKick;
            public float RecoilMaxFovKick;
            public float RecoilFovKickSharpness;
            public float RecoilFovKickRestitutionSharpness;

            public static Authoring GetDefault()
            {
                return new Authoring
                {
                    WeaponBobHAmount = 0.08f,
                    WeaponBobVAmount = 0.06f,
                    WeaponBobFrequency = 10f,
                    WeaponBobSharpness = 10f,
                    WeaponBobAimRatio = 0.25f,

                    RecoilStrength = 1f,
                    RecoilMaxDistance = 0.5f,
                    RecoilSharpness = 100f,
                    RecoilRestitutionSharpness = 5f,

                    AimFovRatio = 0.5f,
                    AimFovSharpness = 10f,
                    LookSensitivityMultiplierWhileAiming = 0.7f,

                    RecoilFovKick = 10f,
                    RecoilMaxFovKick = 10f,
                    RecoilFovKickSharpness = 150f,
                    RecoilFovKickRestitutionSharpness = 15f,
                };
            }
        }

        public WeaponVisualFeedback(Authoring authoring)
        {
            WeaponBobHAmount = authoring.WeaponBobHAmount;
            WeaponBobVAmount = authoring.WeaponBobVAmount;
            WeaponBobFrequency = authoring.WeaponBobFrequency;
            WeaponBobSharpness = authoring.WeaponBobSharpness;
            WeaponBobAimRatio = authoring.WeaponBobAimRatio;

            RecoilStrength = authoring.RecoilStrength;
            RecoilMaxDistance = authoring.RecoilMaxDistance;
            RecoilSharpness = authoring.RecoilSharpness;
            RecoilRestitutionSharpness = authoring.RecoilRestitutionSharpness;

            AimFovRatio = authoring.AimFovRatio;
            AimFovSharpness = authoring.AimFovSharpness;
            LookSensitivityMultiplierWhileAiming = authoring.LookSensitivityMultiplierWhileAiming;

            RecoilFovKick = authoring.RecoilFovKick;
            RecoilMaxFovKick = authoring.RecoilMaxFovKick;
            RecoilFovKickSharpness = authoring.RecoilFovKickSharpness;
            RecoilFovKickRestitutionSharpness = authoring.RecoilFovKickRestitutionSharpness;
        }

        public float WeaponBobHAmount;
        public float WeaponBobVAmount;
        public float WeaponBobFrequency;
        public float WeaponBobSharpness;
        public float WeaponBobAimRatio;

        public float RecoilStrength;
        public float RecoilMaxDistance;
        public float RecoilSharpness;
        public float RecoilRestitutionSharpness;

        public float AimFovRatio;
        public float AimFovSharpness;
        public float LookSensitivityMultiplierWhileAiming;

        public float RecoilFovKick;
        public float RecoilMaxFovKick;
        public float RecoilFovKickSharpness;
        public float RecoilFovKickRestitutionSharpness;
    }

    public struct WeaponControl : IComponentData
    {
        public bool ShootPressed;
        public bool ShootReleased;
        public bool AimHeld;
    }

    public struct WeaponOwner : IComponentData
    {
        public Entity Entity;
    }

    public struct WeaponShotSimulationOriginOverride : IComponentData
    {
        public Entity Entity;
    }

    public struct WeaponShotIgnoredEntity : IBufferElementData
    {
        public Entity Entity;
    }
}
