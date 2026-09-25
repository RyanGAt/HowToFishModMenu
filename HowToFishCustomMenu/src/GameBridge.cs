using System;
using System.Collections.Generic;
using UnityEngine;

namespace HowToFishCustomMenu
{
    // Game-specific integration boundary. No guessed private method names or network RPCs.
    // Wire the actual game methods, after inspecting your installed Assembly-CSharp.dll.
    internal interface IGameBridge
    {
        bool IsHost { get; }
        bool Ready { get; }
        string Status { get; }
        Transform Player { get; }
        Transform Boat { get; }
        bool InstantCatch();
        bool SetMoney(int amount);
        bool SpawnItem(string name, int quantity);
        bool SetGodMode(bool enabled);
        bool SetRoulette(string colour);
        IEnumerable<Transform> Fish();
        IEnumerable<Transform> Items();
    }

    // Intentionally refuses to claim success for game-specific actions that are not hooked yet.
    internal sealed class UnboundGameBridge : IGameBridge
    {
        public bool IsHost => false;
        public bool Ready => false;
        public string Status => "Game hooks not connected - see README / GameBridge.cs";
        public Transform Player => null;
        public Transform Boat => null;
        public bool InstantCatch() => false;
        public bool SetMoney(int amount) => false;
        public bool SpawnItem(string name, int quantity) => false;
        public bool SetGodMode(bool enabled) => false;
        public bool SetRoulette(string colour) => false;
        public IEnumerable<Transform> Fish() { yield break; }
        public IEnumerable<Transform> Items() { yield break; }
    }
}
