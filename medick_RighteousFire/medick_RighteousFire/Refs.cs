// ================================================================
//  Refs.cs  —  medick_RighteousFire
//
//  Lightweight reference manager — keeps live handles to the
//  game singletons we need. Populated lazily in Update().
// ================================================================

namespace medick_RighteousFire;

public static class Refs
{
    public static Actor         player_actor  = null;
    public static ItemList      item_list     = null;
    public static UniqueList    unique_list   = null;

    public static bool IsReady =>
        player_actor != null &&
        item_list    != null &&
        unique_list  != null;

    public static void Refresh()
    {
        try
        {
            if (player_actor == null)
                player_actor = PlayerFinder.getPlayerActor();

            if (item_list == null)
                item_list = UnityEngine.Object.FindObjectOfType<ItemList>(true);

            if (unique_list == null)
                unique_list = UnityEngine.Object.FindObjectOfType<UniqueList>(true);
        }
        catch { }
    }
}
