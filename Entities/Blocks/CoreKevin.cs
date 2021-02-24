using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using MonoMod;
using MonoMod.Cil;
using Mono.Cecil;
using Mono.Cecil.Cil;

using NyahHelper.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;

namespace NyahHelper.Entities.Blocks
{
    public static class CoreKevinHook
    {
        public static string CurrentLeftPath = "objects/crushblock/lit_left";
        public static string CurrentRightPath = "objects/crushblock/lit_right";
        public static string CurrentTopPath = "objects/crushblock/lit_top";
        public static string CurrentBottomPath = "objects/crushblock/lit_bottom";

        public static bool CurrentLeftVisibility;
        public static bool CurrentRightVisibility;
        public static bool CurrentTopVisibility;
        public static bool CurrentBottomVisibility;

        #region Reflection Fields
        private static readonly FieldInfo _currentLeftPath = typeof(CoreKevinHook).GetField(nameof(CurrentLeftPath), Constants.PublicStatic);
        private static readonly FieldInfo _currentRightPath = typeof(CoreKevinHook).GetField(nameof(CurrentRightPath), Constants.PublicStatic);
        private static readonly FieldInfo _currentTopPath = typeof(CoreKevinHook).GetField(nameof(CurrentTopPath), Constants.PublicStatic);
        private static readonly FieldInfo _currentBottomPath = typeof(CoreKevinHook).GetField(nameof(CurrentBottomPath), Constants.PublicStatic);

        private static readonly FieldInfo _currentLeftVisibility = typeof(CoreKevinHook).GetField(nameof(CurrentLeftVisibility), Constants.PublicStatic);
        private static readonly FieldInfo _currentRightVisibility = typeof(CoreKevinHook).GetField(nameof(CurrentRightVisibility), Constants.PublicStatic);
        private static readonly FieldInfo _currentTopVisibility = typeof(CoreKevinHook).GetField(nameof(CurrentTopVisibility), Constants.PublicStatic);
        private static readonly FieldInfo _currentBottomVisibility = typeof(CoreKevinHook).GetField(nameof(CurrentBottomVisibility), Constants.PublicStatic);
        #endregion

        public static void Hook()
        {
            // Keep track of "active" images state
            On.Celeste.CrushBlock.Attack += CrushBlock_Attack;
            On.Celeste.CrushBlock.TurnOffImages += CrushBlock_TurnOffImages;

            // Sync AddImage with the core mode
            IL.Celeste.CrushBlock.AddImage += CrushBlock_AddImage;
            On.Celeste.CrushBlock.AddImage += CrushBlock_AddImage1;

            // Particles
            On.Celeste.ParticleTypes.Load += ParticleTypes_Load;

            Logger.Log("Nyah Helper", "Core Kevin hooks executed");
        }

        #region AddImage Hooks
        private static void CrushBlock_AddImage1(On.Celeste.CrushBlock.orig_AddImage orig, CrushBlock self, MTexture idle, int x, int y, int tx, int ty, int borderX, int borderY)
        {
            if (self is CoreKevin coreSelf)
            {
                // In the IL hook we read from these fields
                // I think you can do this better by checking crushDir but this works fine, for now at least
                CurrentLeftVisibility = coreSelf.ActiveLeftImages.Count > 0 && coreSelf.ActiveLeftImages[0].Visible;
                CurrentRightVisibility = coreSelf.ActiveRightImages.Count > 0 && coreSelf.ActiveRightImages[0].Visible;
                CurrentTopVisibility = coreSelf.ActiveTopImages.Count > 0 && coreSelf.ActiveTopImages[0].Visible;
                CurrentBottomVisibility = coreSelf.ActiveBottomImages.Count > 0 && coreSelf.ActiveBottomImages[0].Visible;
            }
            else
            {
                // In the IL hook we edit the strings even for all Kevins
                // So we need this to set them correctly for vanilla Kevin
                CurrentLeftPath = "objects/crushblock/lit_left";
                CurrentRightPath = "objects/crushblock/lit_right";
                CurrentTopPath = "objects/crushblock/lit_top";
                CurrentBottomPath = "objects/crushblock/lit_bottom";
            }

            orig(self, idle, x, y, tx, ty, borderX, borderY);
        }

        private static void CrushBlock_AddImage(ILContext il)
        {
            FieldInfo visibleField = typeof(Component).GetField("Visible", Constants.PublicInstance);
            ILCursor cursor = new ILCursor(il);

            // 1. Replaces those strings with the static fields defined in this class
            // This allows for dynamically editing those strings at runtime
            // 2. Retain visibility of lit images

            #region Left
            cursor.GotoNext((inst) => inst.MatchLdstr("objects/crushblock/lit_left"));
            cursor.Remove();
            cursor.Emit(OpCodes.Ldsfld, _currentLeftPath);

            cursor.GotoNext((inst) => inst.MatchLdloc(5),
                (inst) => inst.Match(OpCodes.Ldc_I4_0),
                (inst) => inst.MatchStfld(visibleField));
            cursor.Goto(cursor.Next, MoveType.After);
            cursor.Remove();
            cursor.Emit(OpCodes.Ldsfld, _currentLeftVisibility);
            #endregion

            #region Right
            cursor.GotoNext((inst) => inst.MatchLdstr("objects/crushblock/lit_right"));
            cursor.Remove();
            cursor.Emit(OpCodes.Ldsfld, _currentRightPath);

            cursor.GotoNext((inst) => inst.MatchLdloc(6),
                (inst) => inst.Match(OpCodes.Ldc_I4_0),
                (inst) => inst.MatchStfld(visibleField));
            cursor.Goto(cursor.Next, MoveType.After);
            cursor.Remove();
            cursor.Emit(OpCodes.Ldsfld, _currentRightVisibility);
            #endregion

            #region Top
            cursor.GotoNext((inst) => inst.MatchLdstr("objects/crushblock/lit_top"));
            cursor.Remove();
            cursor.Emit(OpCodes.Ldsfld, _currentTopPath);

            cursor.GotoNext((inst) => inst.MatchLdloc(7),
                (inst) => inst.Match(OpCodes.Ldc_I4_0),
                (inst) => inst.MatchStfld(visibleField));
            cursor.Goto(cursor.Next, MoveType.After);
            cursor.Remove();
            cursor.Emit(OpCodes.Ldsfld, _currentTopVisibility);
            #endregion

            #region Bottom
            cursor.GotoNext((inst) => inst.MatchLdstr("objects/crushblock/lit_bottom"));
            cursor.Remove();
            cursor.Emit(OpCodes.Ldsfld, _currentBottomPath);

            cursor.GotoNext((inst) => inst.MatchLdloc(8),
                (inst) => inst.Match(OpCodes.Ldc_I4_0),
                (inst) => inst.MatchStfld(visibleField));
            cursor.Goto(cursor.Next, MoveType.After);
            cursor.Remove();
            cursor.Emit(OpCodes.Ldsfld, _currentBottomVisibility);
            #endregion
        }
        #endregion

        #region Other Hooks
        private static void ParticleTypes_Load(On.Celeste.ParticleTypes.orig_Load orig)
        {
            orig();
            // Decompiled code for particles
            CoreKevin.P_HotActivate = new ParticleType
            {
                Source = GFX.Game["particles/rect"],
                Color = Calc.HexToColor("e45e5e"), // Changed color from blue to red here
                Color2 = Color.White,
                ColorMode = ParticleType.ColorModes.Blink,
                RotationMode = ParticleType.RotationModes.SameAsDirection,
                Size = 0.5f,
                SizeRange = 0.2f,
                DirectionRange = (float)Math.PI / 6f,
                FadeMode = ParticleType.FadeModes.Late,
                LifeMin = 0.5f,
                LifeMax = 1.1f,
                SpeedMin = 60f,
                SpeedMax = 100f,
                SpeedMultiplier = 0.2f
            };
            CoreKevin.P_HotCrushing = new ParticleType
            {
                Source = GFX.Game["particles/rect"],
                Color = Calc.HexToColor("fe6767"), // Changed color from Reflection-y pink to light red here
                Color2 = Calc.HexToColor("ff6666"), // Changed color from blue to red here
                ColorMode = ParticleType.ColorModes.Blink,
                RotationMode = ParticleType.RotationModes.SameAsDirection,
                Size = 0.5f,
                SizeRange = 0.2f,
                DirectionRange = (float)Math.PI / 6f,
                FadeMode = ParticleType.FadeModes.Late,
                LifeMin = 0.5f,
                LifeMax = 1.2f,
                SpeedMin = 30f,
                SpeedMax = 50f,
                SpeedMultiplier = 0.4f,
                Acceleration = new Vector2(0f, 10f)
            };

            // Copy original particles
            CoreKevin.P_ColdActivate = CrushBlock.P_Activate;
            CoreKevin.P_ColdCrushing = CrushBlock.P_Crushing;
        }

        private static void CrushBlock_TurnOffImages(On.Celeste.CrushBlock.orig_TurnOffImages orig, CrushBlock self)
        {
            orig(self);
            if (self is CoreKevin kevin) kevin.Attacking = false;
        }

        private static void CrushBlock_Attack(On.Celeste.CrushBlock.orig_Attack orig, CrushBlock self, Vector2 direction)
        {
            orig(self, direction);
            if (self is CoreKevin kevin) kevin.Attacking = true;
        }
        #endregion
    }

    [CustomEntity("nyahhelper/corekevin")]
    public class CoreKevin : CrushBlock
    {
        public static ParticleType P_HotActivate;
        public static ParticleType P_HotCrushing;
        public static ParticleType P_ColdActivate;
        public static ParticleType P_ColdCrushing;

        // I know Vector2.UnitX and .UnitY exist, but I'm dumb
        static readonly Vector2 VUp = new Vector2(0, -1);
        static readonly Vector2 VDown = new Vector2(0, 1);
        static readonly Vector2 VRight = new Vector2(1, 0);
        static readonly Vector2 VLeft = new Vector2(-1, 0);

        // TODO: Maybe use a different fill?
        static readonly Color AngryFill = Calc.HexToColor("82010d");
        static readonly Color OrigFill = Calc.HexToColor("62222b");

        public CoreModeListener Listener;
        public Session.CoreModes CoreMode;

        public string BlockPath => CoreMode == Session.CoreModes.Hot
            ? "objects/nyahhelper/corekevin/block"
            : "objects/crushblock/block";

        public string AngryPath => Giant ? "giant_corekevin_face" : "corekevin_face";

        // TODO: Implement different speeds (with a hook)
        public float SpeedMultiplier => Angry ? 1.5f : 1f;

        public bool Angry => AlwaysAngry || (
            DefaultToAngry
                ? CoreMode != Session.CoreModes.Cold
                : CoreMode == Session.CoreModes.Hot
        );

        public bool AlwaysAngry;
        public bool DefaultToAngry;

        public bool Giant;

        public MTexture Idle;
        public Axes Mode;

        public Sprite AngryFace;

        public bool Attacking = false;

        #region Reflection Methods
        static readonly MethodInfo _AddImage = typeof(CrushBlock).GetMethod("AddImage", Constants.PrivateInstance);
        static readonly MethodInfo _Attack = typeof(CrushBlock).GetMethod("Attack", Constants.PrivateInstance);
        static readonly MethodInfo _CanActivate = typeof(CrushBlock).GetMethod("CanActivate", Constants.PrivateInstance);

        public void AddImage(params object[] args)
        {
            _AddImage.Invoke(this, args);
        }

        public bool CanActivate(params object[] args)
        {
            return (bool)_CanActivate.Invoke(this, args);
        }

        public void Attack(params object[] args)
        {
            _Attack.Invoke(this, args);
        }
        #endregion

        #region Reflection Fields
        static readonly FieldInfo _canMoveV = typeof(CrushBlock).GetField("canMoveVertically", Constants.PrivateInstance);
        static readonly FieldInfo _canMoveH = typeof(CrushBlock).GetField("canMoveHorizontally", Constants.PrivateInstance);
        static readonly FieldInfo _face = typeof(CrushBlock).GetField("face", Constants.PrivateInstance);
        static readonly FieldInfo _crushDir = typeof(CrushBlock).GetField("crushDir", Constants.PrivateInstance);
        static readonly FieldInfo _fill = typeof(CrushBlock).GetField("fill", Constants.PrivateInstance);

        static readonly FieldInfo _activeLeft = typeof(CrushBlock).GetField("activeLeftImages", Constants.PrivateInstance);
        static readonly FieldInfo _activeRight = typeof(CrushBlock).GetField("activeRightImages", Constants.PrivateInstance);
        static readonly FieldInfo _activeTop = typeof(CrushBlock).GetField("activeTopImages", Constants.PrivateInstance);
        static readonly FieldInfo _activeBottom = typeof(CrushBlock).GetField("activeBottomImages", Constants.PrivateInstance);

        public bool CanMoveV
        {
            get
            {
                return (bool)_canMoveV.GetValue(this);
            }
            set
            {
                _canMoveV.SetValue(this, value);
            }
        }

        public bool CanMoveH
        {
            get
            {
                return (bool)_canMoveH.GetValue(this);
            }
            set
            {
                _canMoveH.SetValue(this, value);
            }
        }

        public Sprite Face
        {
            get
            {
                return (Sprite)_face.GetValue(this);
            }
            set
            {
                _face.SetValue(this, value);
            }
        }

        public Vector2 CrushDir
        {
            get
            {
                return (Vector2)_crushDir.GetValue(this);
            }
            set
            {
                _crushDir.SetValue(this, value);
            }
        }

        public Color Fill
        {
            get
            {
                return (Color)_fill.GetValue(this);
            }
            set
            {
                _fill.SetValue(this, value);
            }
        }

        public List<Image> ActiveLeftImages
        {
            get
            {
                return (List<Image>)_activeLeft.GetValue(this);
            }
        }

        public List<Image> ActiveRightImages
        {
            get
            {
                return (List<Image>)_activeRight.GetValue(this);
            }
        }

        public List<Image> ActiveTopImages
        {
            get
            {
                return (List<Image>)_activeTop.GetValue(this);
            }
        }

        public List<Image> ActiveBottomImages
        {
            get
            {
                return (List<Image>)_activeBottom.GetValue(this);
            }
        }
        #endregion

        public CoreKevin(EntityData data, Vector2 offset) : base(data, offset)
        {
            Add(Listener = new CoreModeListener(OnCoreMode));
            AlwaysAngry = data.Bool("AlwaysAngry");
            DefaultToAngry = data.Bool("DefaultToAngry");
            Mode = data.Enum("axes", Axes.Both);
            Giant = Width >= 48f && Height >= 48f && data.Bool("chillout");

            // Add an angry face
            Add(AngryFace = GFX.SpriteBank.Create(AngryPath));
            AngryFace.Position = new Vector2(Width, Height) / 2f;
            AngryFace.Play("idle");
            AngryFace.OnLastFrame = Face.OnLastFrame;
            AngryFace.Visible = false;

            // Synchronize faces
            Face.OnChange = (lastID, newID) => {
                AngryFace.Play(newID);
            };
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            // Initialize
            OnCoreMode(SceneAs<Level>().Session.CoreMode);
        }

        public void OnCoreMode(Session.CoreModes mode)
        {
            // Skip updating when the new mode is cold and the previous is none, because they have the same sprites
            if (CoreMode == Session.CoreModes.None && mode == Session.CoreModes.Cold)
            {
                CoreMode = mode;
                return;
            }

            CoreMode = mode;

            // Change the face and the particles
            if (CoreMode == Session.CoreModes.Hot)
            {
                Face.Visible = false;
                AngryFace.Visible = true;
                P_Activate = P_HotActivate;
                P_Crushing = P_HotCrushing;
            }
            else
            {
                Face.Visible = true;
                AngryFace.Visible = false;
                P_Activate = P_ColdActivate;
                P_Crushing = P_ColdCrushing;
            }

            // Change the "lit" images
            if (CoreMode == Session.CoreModes.Hot)
            {
                CoreKevinHook.CurrentLeftPath = "objects/nyahhelper/corekevin/lit_left";
                CoreKevinHook.CurrentRightPath = "objects/nyahhelper/corekevin/lit_right";
                CoreKevinHook.CurrentTopPath = "objects/nyahhelper/corekevin/lit_top";
                CoreKevinHook.CurrentBottomPath = "objects/nyahhelper/corekevin/lit_bottom";
            }
            else
            {
                CoreKevinHook.CurrentLeftPath = "objects/crushblock/lit_left";
                CoreKevinHook.CurrentRightPath = "objects/crushblock/lit_right";
                CoreKevinHook.CurrentTopPath = "objects/crushblock/lit_top";
                CoreKevinHook.CurrentBottomPath = "objects/crushblock/lit_bottom";
            }


            #region Decompiled block images generator
            List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(BlockPath);

            switch (Mode)
            {
                default:
                    Idle = atlasSubtextures[3];
                    CanMoveH = (CanMoveV = true);
                    break;
                case Axes.Horizontal:
                    Idle = atlasSubtextures[1];
                    CanMoveH = true;
                    CanMoveV = false;
                    break;
                case Axes.Vertical:
                    Idle = atlasSubtextures[2];
                    CanMoveH = false;
                    CanMoveV = true;
                    break;
            }

            // Remove all images, not decompiled
            new List<Image>(Components.GetAll<Image>()).ForEach((img) => { if (img != null && img.GetType() == typeof(Image)) Remove(img); });

            int num = (int)(Width / 8f) - 1;
            int num2 = (int)(Height / 8f) - 1;
            AddImage(Idle, 0, 0, 0, 0, -1, -1);
            AddImage(Idle, num, 0, 3, 0, 1, -1);
            AddImage(Idle, 0, num2, 0, 3, -1, 1);
            AddImage(Idle, num, num2, 3, 3, 1, 1);
            for (int i = 1; i < num; i++)
            {
                AddImage(Idle, i, 0, Calc.Random.Choose(1, 2), 0, 0, -1);
                AddImage(Idle, i, num2, Calc.Random.Choose(1, 2), 3, 0, 1);
            }
            for (int j = 1; j < num2; j++)
            {
                AddImage(Idle, 0, j, 0, Calc.Random.Choose(1, 2), -1, 0);
                AddImage(Idle, num, j, 3, Calc.Random.Choose(1, 2), 1, 0);
            }
            #endregion
        }

        public override void Update()
        {
            var player = Scene.Tracker.GetEntity<Player>();

            #region Decompiled Face Position Adjust
            if (CrushDir == Vector2.Zero)
            {
                AngryFace.Position = new Vector2(Width, Height) / 2f;
                if (CollideCheck<Player>(Position + new Vector2(-1f, 0f)))
                {
                    AngryFace.X -= 1f;
                }
                else if (CollideCheck<Player>(Position + new Vector2(1f, 0f)))
                {
                    AngryFace.X += 1f;
                }
                else if (CollideCheck<Player>(Position + new Vector2(0f, -1f)))
                {
                    AngryFace.Y -= 1f;
                }
            }
            #endregion

            #region Anger Attacks
            if (Angry && player != null)
            {
                // TODO: Reorder the checks so that the vertical checks are first for vertical
                // attacking and same for horizontal

                // TODO: Add Theo and Seeker checks

                // If madeline's left edge is more to the right than the kevin's left edge
                // (if she's to the right of the kevin's left)
                if (player.Collider.AbsoluteRight >= Collider.AbsoluteLeft

                    // and her left edge is more to the left than the kevin's right edge
                    // (if she's to the left of the kevin's right)
                    && player.Collider.AbsoluteLeft <= Collider.AbsoluteRight

                    // and her bottom edge is higher than than the kevin's top edge
                    // (if she's higher than the kevin's top)
                    && player.Collider.AbsoluteBottom - 1f <= Collider.AbsoluteTop)

                // In other words, if the kevin's top facette see madeline
                {
                    if (CanActivate(VUp) &&
                        !Scene.CollideCheck<Solid>(new Vector2(player.Collider.AbsoluteLeft, Top),
                            new Vector2(player.Collider.AbsoluteLeft, player.Collider.AbsoluteBottom),
                            this))
                        Attack(VUp);
                }
                // If madeline's right edge is more to the right than the kevin's left edge
                // (if she's to the right of the kevin's left)
                else if (player.Collider.AbsoluteRight >= Collider.AbsoluteLeft

                    // and her left edge is more to the left than the kevin's right edge
                    // (if she's to the left of the kevin's right)
                    && player.Collider.AbsoluteLeft <= Collider.AbsoluteRight

                    // and her bottom edge is lower than than the kevin's top edge
                    // (if she's lower than the kevin's bottom)
                    && player.Collider.AbsoluteTop + 1f >= Collider.AbsoluteBottom)

                // In other words, if the kevin's bottom facette see madeline
                {
                    if (CanActivate(VDown) &&
                        !Scene.CollideCheck<Solid>(new Vector2(player.Collider.AbsoluteLeft,
                            Bottom),
                        new Vector2(player.Collider.AbsoluteLeft, player.Collider.AbsoluteTop), this))
                        Attack(VDown);
                }
                // if madeline's top edge is higher than the kevin's bottom edge
                // (if she's higher than the bottom of the kevin)
                else if (player.Collider.AbsoluteTop <= Collider.AbsoluteBottom

                    // and her bottom edge is lower than the kevin's top edge
                    // (if she's lower than the top of the kevin)
                    && player.Collider.AbsoluteBottom >= Collider.AbsoluteTop

                    // and her left edge is more to the right than the kevin's right edge
                    // (if she's to the kevin's right, or she's more right than the kevin's right)
                    && player.Collider.AbsoluteLeft + 1f > Collider.AbsoluteRight)

                // In other words, if the kevin's right facette see madeline
                {
                    if (CanActivate(VRight) &&
                        !Scene.CollideCheck<Solid>(new Vector2(Right, player.Collider.AbsoluteTop),
                        new Vector2(player.Collider.AbsoluteLeft, player.Collider.AbsoluteTop), this))
                        Attack(VRight);
                }
                // if madeline's top edge is higher than the kevin's bottom edge
                // (if she's higher than the bottom of the kevin)
                else if (player.Collider.AbsoluteTop <= Collider.AbsoluteBottom

                    // and her bottom edge is lower than the kevin's top edge
                    // (if she's lower than the top of the kevin)
                    && player.Collider.AbsoluteBottom >= Collider.AbsoluteTop

                    // and her right edge is more to the left than the kevin's left edge
                    // (if she's to the kevin's left, or she's more left than the kevin's left)
                    && player.Collider.AbsoluteRight - 1f < Collider.AbsoluteLeft)

                // In other words, if the kevin's left facette see madeline
                {
                    if (CanActivate(VLeft) &&
                        !Scene.CollideCheck<Solid>(new Vector2(Left, player.Collider.AbsoluteTop),
                        new Vector2(player.Collider.AbsoluteRight, player.Collider.AbsoluteTop), this))
                        Attack(VLeft);
                }
            }
            #endregion

            base.Update();
        }
    }
}
