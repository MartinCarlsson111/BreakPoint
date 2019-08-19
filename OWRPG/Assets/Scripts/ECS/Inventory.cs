using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public enum ItemType
{
    Food,
    Resource,
    Armor,
    Weapon,
}



//this exists on a seperate entity together with the ItemData Buffer
public struct ItemHeader : IComponentData
{
    int itemID;
    ItemType type;
}

public enum ItemDataType
{
    BaseDamage,
    AttackSpeed,
    Strength,
    Spirit,
    Intellect,
    Agility,
}

public struct ItemData : IBufferElementData
{
    ItemDataType type;
    int Value;
}

//this exists on the entity
public struct InventoryItem : IBufferElementData
{
    int itemID;
}

//same thing needs to happen with this

//only keep the data in one location
//this would make data storage very simple

//identifying information is kept on entity
//actual data is kept on separate entity.
//how to make accessing this data fast and simply is the problem


//Items are added to the Add queue
//The AddItemToInventory System checks if the item can be added
//If the item cannot be added it is returned to the entity it came from

public struct InventoryAddQueue : IBufferElementData
{
    int itemID;
    int fromEntity;
}

public class Inventory : JobComponentSystem
{





}
