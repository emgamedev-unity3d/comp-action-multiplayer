using System.Collections.Generic;
using System.Diagnostics;
using Unity.Entities;
using UnityEngine;
using UnityEngine.VFX;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    struct VfxDictionaryValueStruct
    {
        public VisualEffect Vfx;
        public VFXEventAttribute Payload;
    }

    public enum VfxType
    {
        MachineGunBullet,
        ShotgunBullet,
        Laser,
        Plasma,
        Rocket,
        Death,
    }
    public enum BulletType
    {
        Other,
        MachineGun,
        Shotgun,
    }

    public struct VfxHitRequest : IComponentData
    {
        public VfxType VfxHitType;
        public float LowCount;
        public float MidCount;
        public float HighCount;
        public Vector3 Position;
        public Vector3 HitNormal;
        public float HitRadius; //Used only in the Rocket projectile
    }

    static class VfxPropertyNames
    {
        public const string OnStartLowCount = "OnStartLowCount";
        public const string OnStartMidCount = "OnStartMidCount";
        public const string OnStartHighCount = "OnStartHighCount";
        public const string Position = "position";
        public const string Direction = "direction";
        public const string SpawnCount = "spawnCount";
        public const string HitRadius = "DamageRadius";
    }

    /// <summary>
    /// This system plays the Vfx.
    /// It is in charge of creating the <see cref="VisualEffect"/> instances associated with the requests
    /// and triggering them when required.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class HitVfxSystem : SystemBase
    {
        //Vfx graph ids
        int m_OnStartLowCountId;
        int m_OnStartMidCountId;
        int m_OnStartHighCountId;

        int m_SpawnCountId;
        int m_PositionId;
        int m_HitNormalId;

        Dictionary<VfxType, VfxDictionaryValueStruct> m_VfxDictionary;

        protected override void OnCreate()
        {
            RequireForUpdate<VfxHitRequest>();
            RequireForUpdate<VfxHitResources>();

            m_VfxDictionary = new Dictionary<VfxType, VfxDictionaryValueStruct>();

            m_OnStartLowCountId = Shader.PropertyToID(VfxPropertyNames.OnStartLowCount);
            m_OnStartMidCountId = Shader.PropertyToID(VfxPropertyNames.OnStartMidCount);
            m_OnStartHighCountId = Shader.PropertyToID(VfxPropertyNames.OnStartHighCount);

            m_PositionId = Shader.PropertyToID(VfxPropertyNames.Position);
            m_HitNormalId = Shader.PropertyToID(VfxPropertyNames.Direction);
            m_SpawnCountId = Shader.PropertyToID(VfxPropertyNames.SpawnCount);
        }

        protected override void OnDestroy()
        {
            foreach (var type in m_VfxDictionary)
            {
                //Checking if Payload and Vfx are not null because the cleanup order is not deterministic when leaving playmode in the editor
                type.Value.Payload?.Dispose();
                if (type.Value.Vfx != null)
                    Object.Destroy(type.Value.Vfx.gameObject);
            }
        }

        protected override void OnUpdate()
        {
            var ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(World.Unmanaged);
            var rateRatio = SystemAPI.Time.DeltaTime / UnityEngine.Time.deltaTime;
            var vfxPrefabs = SystemAPI.GetSingletonBuffer<VfxHitResources>();
            foreach (var (hitVfxRequest, entity) in SystemAPI.Query<RefRO<VfxHitRequest>>().WithEntityAccess())
            {
                if (!m_VfxDictionary.TryGetValue(hitVfxRequest.ValueRO.VfxHitType, out var vfxValue))
                {
                    vfxValue.Vfx = Object.Instantiate(vfxPrefabs[(int)hitVfxRequest.ValueRO.VfxHitType].VfxPrefab.Value).GetComponent<VisualEffect>();
                    vfxValue.Payload = vfxValue.Vfx.CreateVFXEventAttribute();
                    m_VfxDictionary.Add(hitVfxRequest.ValueRO.VfxHitType, vfxValue);

                    DebugVfxMissingUsedProperties(hitVfxRequest.ValueRO.VfxHitType, vfxValue.Vfx);

                    //Rocket has a damage radius set in its prefab, used in the Vfx for damage radius visual feedback
                    if (hitVfxRequest.ValueRO.VfxHitType == VfxType.Rocket)
                        vfxValue.Vfx.SetFloat(VfxPropertyNames.HitRadius, hitVfxRequest.ValueRO.HitRadius);
                }

                vfxValue.Vfx.playRate = rateRatio;
                vfxValue.Payload.SetVector3(m_PositionId, hitVfxRequest.ValueRO.Position);
                vfxValue.Payload.SetVector3(m_HitNormalId, hitVfxRequest.ValueRO.HitNormal);

                vfxValue.Payload.SetFloat(m_SpawnCountId, hitVfxRequest.ValueRO.LowCount);
                vfxValue.Vfx.SendEvent(m_OnStartLowCountId, vfxValue.Payload);

                vfxValue.Payload.SetFloat(m_SpawnCountId, hitVfxRequest.ValueRO.MidCount);
                vfxValue.Vfx.SendEvent(m_OnStartMidCountId, vfxValue.Payload);

                vfxValue.Payload.SetFloat(m_SpawnCountId, hitVfxRequest.ValueRO.HighCount);
                vfxValue.Vfx.SendEvent(m_OnStartHighCountId, vfxValue.Payload);

                ecb.DestroyEntity(entity);
            }
        }

        [Conditional("UNITY_EDITOR")]
        void DebugVfxMissingUsedProperties(VfxType vfxType, VisualEffect vfx)
        {
            List<string> eventNames = new List<string>();
            vfx.visualEffectAsset.GetEvents(eventNames);
            if (!eventNames.Contains(VfxPropertyNames.OnStartLowCount)
                && !eventNames.Contains(VfxPropertyNames.OnStartMidCount)
                && !eventNames.Contains(VfxPropertyNames.OnStartHighCount))
                Debug.LogWarning($"[VfxGraph] None of the send vfx events was found in the " +
                                 $"{vfx.visualEffectAsset.name} vfx graph, " +
                                 $"{vfxType} hit vfx will not show! " +
                                 $"Make sure to use one of the following event names : \"{VfxPropertyNames.OnStartLowCount}\"" +
                                 $" \"{VfxPropertyNames.OnStartMidCount}\" \"{VfxPropertyNames.OnStartHighCount}\"");

            if (vfxType != VfxType.Rocket)
                return;
            if(!vfx.HasFloat(VfxPropertyNames.HitRadius))
                Debug.LogWarning($"[VfxGraph] \"{VfxPropertyNames.HitRadius}\" not found in " +
                                 $"{vfx.visualEffectAsset.name} vfx graph! This will trigger an error.");
        }
    }
}
