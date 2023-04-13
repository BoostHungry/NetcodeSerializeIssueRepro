using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class NetcodeBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaulWorldName) {
        Debug.Log($"~~~ NetcodeBootstrap Initialize -- defaulWorldName: {defaulWorldName} -- RequestedPlayType: {RequestedPlayType} ~~~");
        AutoConnectPort = 7777; 
        return base.Initialize(defaulWorldName);
    }
}
