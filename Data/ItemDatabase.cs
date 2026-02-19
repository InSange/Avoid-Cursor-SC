using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "CursorReboot/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ActiveItemData> AllItems;

    /// <summary>
    /// ID로 아이템 데이터를 찾습니다.
    /// </summary>
    public ActiveItemData GetItemByID(UnlockID id)
    {
        return AllItems.FirstOrDefault(item => item.ID == id);
    }
}