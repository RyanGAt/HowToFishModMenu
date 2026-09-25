using System;
using System.Collections.Generic;
using UnityEngine;

namespace HowToFishCustomMenu
{
    internal enum OptionKind { Action, Toggle, Slider, Choice, Submenu, Input, Label }

    // One row in a menu page. Built by the menu definitions in Plugin.Menus.cs.
    internal sealed class Option
    {
        public OptionKind Kind;
        public string Name;
        public Func<string> NameFn; // live label, e.g. current troll target
        public string Display => NameFn != null ? NameFn() : Name;
        public Action OnSelect;
        public Func<bool> Get;
        public Action<bool> Set;
        public Func<float> GetValue;
        public Action<float> SetValue;
        public float Min, Max, Step;
        public string Format = "0.0";
        public string[] Choices;
        public Func<Menu> Open;
        public Func<string> GetText;
        public Action<string> SetText;
        public Func<bool> Available = () => true;
        public string Requirement = "";

        public string Value()
        {
            switch (Kind)
            {
                case OptionKind.Toggle: return Get() ? "ON" : "OFF";
                case OptionKind.Slider: return "< " + GetValue().ToString(Format) + " >";
                case OptionKind.Choice: return "< " + Choices[Mathf.Clamp((int)GetValue(), 0, Choices.Length - 1)] + " >";
                case OptionKind.Submenu: return ">";
                case OptionKind.Input: return "[" + GetText() + "]";
                default: return "";
            }
        }
    }

    internal sealed class Menu
    {
        public readonly string Title;
        public readonly List<Option> Options = new List<Option>();
        public int Selected, Scroll;
        public Func<List<Option>> Dynamic; // rebuilt each time the page is drawn (player lists, searches)
        public string Footer = "";
        public bool Targeted; // actions here act on the selected player (used by the chat kill-feed)

        public Menu(string title) { Title = title; }

        public List<Option> Rows => Dynamic != null ? Dynamic() : Options;

        public Menu Add(Option o) { Options.Add(o); return this; }
        public Menu Action(string name, Action act, Func<bool> available = null, string requirement = "")
            => Add(new Option { Kind = OptionKind.Action, Name = name, OnSelect = act, Available = available ?? True, Requirement = requirement });
        public Menu Toggle(string name, Func<bool> get, Action<bool> set, Func<bool> available = null, string requirement = "")
            => Add(new Option { Kind = OptionKind.Toggle, Name = name, Get = get, Set = set, Available = available ?? True, Requirement = requirement });
        public Menu Slider(string name, Func<float> get, Action<float> set, float min, float max, float step, string format = "0.0")
            => Add(new Option { Kind = OptionKind.Slider, Name = name, GetValue = get, SetValue = set, Min = min, Max = max, Step = step, Format = format });
        public Menu Choice(string name, string[] choices, Func<int> get, Action<int> set)
            => Add(new Option { Kind = OptionKind.Choice, Name = name, Choices = choices, GetValue = () => get(), SetValue = v => set((int)v), Min = 0, Max = choices.Length - 1, Step = 1 });
        public Menu Sub(string name, Func<Menu> open, Func<bool> available = null, string requirement = "")
            => Add(new Option { Kind = OptionKind.Submenu, Name = name, Open = open, Available = available ?? True, Requirement = requirement });
        public Menu Input(string name, Func<string> get, Action<string> set)
            => Add(new Option { Kind = OptionKind.Input, Name = name, GetText = get, SetText = set });
        public Menu Label(string text) => Add(new Option { Kind = OptionKind.Label, Name = text });

        private static bool True() => true;
    }
}
