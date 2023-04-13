using Unity.Entities;

// TODO: This probably excessively large.  This should be scaled down, but note that it should always be larger than the max possible
[InternalBufferCapacity(128)]
public struct PlayerZoneInterestBufferComp : IBufferElementData {
    public int Value;
    
    public static implicit operator PlayerZoneInterestBufferComp(int value) {
        return new PlayerZoneInterestBufferComp { Value = value };
    }

    public static implicit operator int(PlayerZoneInterestBufferComp element) {
        return element.Value;
    }
}