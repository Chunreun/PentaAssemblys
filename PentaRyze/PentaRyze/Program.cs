#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using LeagueSharp;
using LeagueSharp.Common;

#endregion


namespace PentaRyze
{
    class Program
    {
        private static Obj_AI_Hero Player = ObjectManager.Player;
        private static String championName = "Ryze";
        private static Menu _Menu;
        private static Orbwalking.Orbwalker Orbwalker;
        public static Menu Config;
        public static SpellSlot IgniteSlot;

        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        private static Items.Item Seraph;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            {
                if (Player.ChampionName != championName)

                    return;
            }
            Q = new Spell(SpellSlot.Q, 625);
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R);

            Seraph = new Items.Item(3040);
            IgniteSlot = Player.GetSpellSlot("summonerdot");

            Config = new Menu("Penta Ryze", "ryze", true);

            // Ts gyal
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            //Orbwalker
            Config.AddSubMenu(new Menu("[PR]Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            //Combo gyal
            Config.AddSubMenu(new Menu("[PR]Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseR", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));
            //Harass gyal
            Config.AddSubMenu(new Menu("[PR]Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Q", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("W", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("E", "Use E").SetValue(true));
            // Farm gyal
            Config.AddSubMenu(new Menu("[PR]Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Farm").AddItem(new MenuItem("Q1", "Use Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("W2", "Use W").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("E3", "Use E").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("R4", "Use R").SetValue(true));
            // Ks gyal
            Config.AddSubMenu(new Menu("[PR]KillSteal", "KillSteal"));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("Q5", "Use Q").SetValue(true));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("E6", "Use E").SetValue(true));

            Config.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += OrbwalkingOnBeforeAttack;
            Game.PrintChat("Penta Ryze by Chureun loaded ! GL&HF ! <3");
        }

        private static void OrbwalkingOnBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
                args.Process = !(Q.IsReady() || W.IsReady() || E.IsReady() || Player.Distance(args.Target) >= 600);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            //Draw the ranges of the spells.
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                {
                    Render.Circle.DrawCircle(Player.Position, spell.Range, menuItem.Color);
                }

            }
        }
             private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();

                var lc = Config.Item("LaneClearActive").GetValue<KeyBind>().Active;
                if (lc || Config.Item("FreezeActive").GetValue<KeyBind>().Active)
                    Farm(lc);


            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var qCd = Player.Spellbook.GetSpell(SpellSlot.Q).CooldownExpires - Game.Time;

            if (target != null)
            {
                if (Player.Distance(target) <= 600)
                {
                    if (Player.Distance(target) >= 575 && W.IsReady() && target.Path.Count() > 0 &&
                        target.Path[0].Distance(Player.ServerPosition) >
                        Player.Distance(target))
                    {
                        W.CastOnUnit(target);
                    }
                    else if (Q.IsReady())
                    {
                        Q.CastOnUnit(target);
                    }
                    else
                    {
                        if (qCd > 1.25f || true)
                        {
                            if (W.IsReady())
                            {
                                W.CastOnUnit(target);
                            }
                            else if (E.IsReady())
                            {
                                E.CastOnUnit(target);
                            }
                        }

                    }
                }
                else if (Player.GetSpellDamage(target, SpellSlot.Q) > target.Health)
                {
                    Q.CastOnUnit(target);
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (target != null && Config.Item("Q").GetValue<bool>())
            {
                Q.CastOnUnit(target);
            }
        }

        private static void Farm(bool laneClear)
        {
            if (!Orbwalking.CanMove(40)) return;
            var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);
            var useQi = Config.Item("Q1").GetValue<StringList>().SelectedIndex;
            var useWi = Config.Item("Q2").GetValue<StringList>().SelectedIndex;
            var useEi = Config.Item("Q3").GetValue<StringList>().SelectedIndex;
            var useQ = (laneClear && (useQi == 1 || useQi == 2)) || (!laneClear && (useQi == 0 || useQi == 2));
            var useW = (laneClear && (useWi == 1 || useWi == 2)) || (!laneClear && (useWi == 0 || useWi == 2));
            var useE = (laneClear && (useEi == 1 || useEi == 2)) || (!laneClear && (useEi == 0 || useEi == 2));

            if (useQ && Q.IsReady())
            {
                foreach (var minion in allMinions)
                {
                    if (minion.IsValidTarget() &&
                        HealthPrediction.GetHealthPrediction(minion,
                            (int)(Player.Distance(minion) * 1000 / 1400)) <
                         Player.GetSpellDamage(minion, SpellSlot.Q))
                    {
                        Q.CastOnUnit(minion);
                        return;
                    }
                }
            }
            else if (useW && W.IsReady())
            {
                foreach (var minion in allMinions)
                {
                    if (minion.IsValidTarget(W.Range) &&
                        minion.Health < Player.GetSpellDamage(minion, SpellSlot.Q))
                    {
                        W.CastOnUnit(minion);
                        return;
                    }
                }
            }
            else if (useE && E.IsReady())
            {
                foreach (var minion in allMinions)
                {
                    if (minion.IsValidTarget(E.Range) &&
                        HealthPrediction.GetHealthPrediction(minion,
                            (int)(Player.Distance(minion) * 1000 / 1000)) <
                        Player.GetSpellDamage(minion, SpellSlot.Q) - 10)
                    {
                        E.CastOnUnit(minion);
                        return;
                    }
                }
            }

            if (laneClear)
            {
                foreach (var minion in allMinions)
                {
                    if (useQ)
                        Q.CastOnUnit(minion);

                    if (useW)
                        W.CastOnUnit(minion);

                    if (useE)
                        E.CastOnUnit(minion);
                }
            }
        }

        }
    }




