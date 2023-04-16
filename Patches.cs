using HarmonyLib;
using UnityEngine;

namespace DeathPinRemoval;

[HarmonyPatch(typeof(TombStone), nameof(TombStone.GiveBoost))]
static class TombstoneGiveBoostPatch
{
    static void Prefix(TombStone __instance)
    {
        if (DeathPinRemovalPlugin.removeOnEmpty.Value != DeathPinRemovalPlugin.Toggle.On) return;
        // Mimic the logic from the original method
        Minimap.PinData closestPin = Minimap.instance.GetClosestPin(__instance.transform.position, 10);
        if (closestPin == null)
            return;
        if (closestPin.m_type == Minimap.PinType.Death)
            Minimap.instance.RemovePin(closestPin);
        else
        {
            Minimap.instance.RemovePin(__instance.transform.position, 5);
        }
    }
}

[HarmonyPatch(typeof(Minimap), nameof(Minimap.AddPin))]
static class MinimapAddPinPatch
{
    static bool Prefix(Minimap __instance, Vector3 pos,
        Minimap.PinType type,
        string name,
        bool save,
        bool isChecked,
        long ownerID = 0)
    {
        if (DeathPinRemovalPlugin.totalPinRemoval.Value != DeathPinRemovalPlugin.Toggle.On) return true;
        return type != Minimap.PinType.Death;
    }
}