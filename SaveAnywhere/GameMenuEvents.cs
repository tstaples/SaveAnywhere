using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace SaveAnywhere
{
    //public enum GameMenuTab : int
    //{
    //    Inventory = 0,
    //    Skills = 1,
    //    Social = 2,
    //    Map = 3,
    //    Crafting = 4,
    //    Collections = 5,
    //    Options = 6,
    //    Exit = 7
    //}

    //public class EventArgsGameMenuTabChanged : EventArgs
    //{
    //    public EventArgsGameMenuTabChanged(GameMenuTab priorTab, GameMenuTab newTab)
    //    {
    //        PriorTab = priorTab;
    //        NewTab = newTab;
    //    }

    //    public GameMenuTab NewTab { get; private set; }
    //    public GameMenuTab PriorTab { get; private set; }
    //}

    //public class GameMenuEvents
    //{
    //    public static event EventHandler<EventArgsGameMenuTabChanged> TabChanged = delegate { };

    //    public static void InvokeTabChanged(GameMenuTab priorTab, GameMenuTab newTab)
    //    {
    //        TabChanged.Invoke(null, new EventArgsGameMenuTabChanged(priorTab, newTab));
    //    }
    //}

    //public class GameMenuEventsController
    //{
    //    private GameMenuTab previousTab = GameMenuTab.Inventory;

    //    public GameMenuEventsController()
    //    {
    //        GameEvents.UpdateTick += Update;
    //    }

    //    public void Update(object sender, EventArgs e)
    //    {

    //    }
    //}
}
