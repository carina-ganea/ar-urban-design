using Auki.ConjureKit;
using Auki.ConjureKit.ECS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

public class ObjectScale : SystemBase
{
    private const string SCALE_COMPONENT_NAME = "scale";

    public event Action<uint, Vector3> OnScaleComponentUpdated;

    private readonly IDictionary<uint, Vector3> _entityScaleDataMap = new Dictionary<uint, Vector3>();

    public override string[] GetComponentTypeNames()
    {
        return new string[] { SCALE_COMPONENT_NAME };
    }
    public ObjectScale(Session session) : base(session)
    { }

    public override void Update(IReadOnlyList<(EntityComponent component, bool localChange)> updated)
    {
        foreach(var (entityComponent,  localChange) in updated)
        {
            _entityScaleDataMap[entityComponent.EntityId] = ByteArrayToVector3(entityComponent.Data);
            OnScaleComponentUpdated?.Invoke(entityComponent.EntityId, _entityScaleDataMap[entityComponent.EntityId]);
        }
    }

    public override void Delete(IReadOnlyList<(EntityComponent component, bool localChange)> deleted)
    {
        foreach(var (entityComponent, localChange) in  deleted)
        {
            var entity = _session.GetEntity(entityComponent.EntityId);
            if (entity != null) { continue; }

            _entityScaleDataMap.Remove(entity.Id);
        }
    }

    public bool SetScale(uint entityId,  Vector3 scale)
    {
        var entity = _session.GetEntity(entityId);
        if(entity == null) { return false; }

        _entityScaleDataMap[entityId] = scale;

        var component = _session.GetEntityComponent(entityId, SCALE_COMPONENT_NAME);

        if(component == null)
        {
            _session.AddComponent(
                SCALE_COMPONENT_NAME,
                entityId,
                Vector3ToByteArray(scale),
                () => { },
                error => Debug.LogError(error)
                );
            return true;
        }
        else
        {
            return _session.UpdateComponent(
                SCALE_COMPONENT_NAME,
                entityId,
                Vector3ToByteArray(scale)
                );
        }
    }

    public Vector3 GetScale(uint entityId)
    {
        if( _session.GetEntity(entityId) == null || !_entityScaleDataMap.ContainsKey(entityId))
        {
            return Vector3.one;
        }
        return _entityScaleDataMap[entityId];
    }

    private Vector3 ByteArrayToVector3(byte[] data)
    {
        byte[] bytesx = new byte[4];
        byte[] bytesy = new byte[4];
        byte[] bytesz = new byte[4];
        Vector3 vec = new Vector3();

        System.Buffer.BlockCopy(data, 0, bytesx, 0, 4);
        vec.x = System.BitConverter.ToSingle(bytesx);

        System.Buffer.BlockCopy(data, 4, bytesy, 0, 4);
        vec.y = System.BitConverter.ToSingle(bytesy);

        System.Buffer.BlockCopy(data, 8, bytesz, 0, 4);
        vec.z = System.BitConverter.ToSingle(bytesz);

        return vec;
    }

    private byte[] Vector3ToByteArray(Vector3 vec)
    {
        byte[] bytes = new byte[12];

        System.Buffer.BlockCopy(System.BitConverter.GetBytes(vec.x), 0, bytes, 0, 4);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(vec.y), 0, bytes, 4, 4);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(vec.z), 0, bytes, 8, 4);

        return bytes;
    }
}
