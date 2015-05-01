﻿using System;
using System.Collections.Generic;
using System.Collections;

namespace EEPhysics
{
    public class PhysicsPlayer
    {
        internal PhysicsWorld HostWorld { get; set; }
        internal const int Width = 16;
        internal const int Height = 16;
        /// <summary>
        /// The precise player X position.
        /// </summary>
        public double X { get; internal set; }
        /// <summary>
        /// The precise player Y position.
        /// </summary>
        public double Y { get; internal set; }
        public int Horizontal { get; internal set; }
        public int Vertical { get; internal set; }
        private int current, currentBelow;
        private double oldX, oldY;
        private double speedX = 0;
        private double speedY = 0;
        private double modifierX = 0;
        private double modifierY = 0;
        private int gravity;
        private double mox, moy;
        private int morx, mory;
        private int pastx, pasty;
        internal int overlapy;
        private double mx, my;
        private int tx = -1;
        private int ty = -1;
        private Queue<int> queue = new Queue<int>();
        private bool isInvulnerable;
        private bool donex, doney;
        internal BitArray Switches { get; set; }
        internal int deaths = 0;
        private const double portalMultiplier = 1.42;
        private bool hasLastPortal = false;
        private Point lastPortal;
        private List<Point> gotCoins = new List<Point>();
        private List<Point> gotBlueCoins = new List<Point>();
        private bool isOnFire;
        private int fireDeath = 200;
        private const double maxThrust = 0.2;
        private bool hasLevitation, isThrusting;
        private double currentThrust, thrustBurnOff;

        /// <summary>Also includes moderator and guardian mode.</summary>
        public bool InGodMode { get; internal set; }
        public bool IsDead { get; internal set; }
        public int Team { get; internal set; }
        //public bool Zombie { get; internal set; }
        public bool HasChat { get; internal set; }

        internal double GravityMultiplier { get { return HostWorld.WorldGravity; } }
        internal double SpeedMultiplier
        {
            get
            {
                /*double d = 1;
                if (Zombie) {
                    d *= 1.2;
                }
                return d;*/
                return 1;
            }
        }
        public double SpeedX { get { return speedX * PhysicsConfig.VariableMultiplier; } internal set { speedX = value / PhysicsConfig.VariableMultiplier; } }
        public double SpeedY { get { return speedY * PhysicsConfig.VariableMultiplier; } internal set { speedY = value / PhysicsConfig.VariableMultiplier; } }
        public double ModifierX { get { return modifierX * PhysicsConfig.VariableMultiplier; } internal set { modifierX = value / PhysicsConfig.VariableMultiplier; } }
        public double ModifierY { get { return modifierY * PhysicsConfig.VariableMultiplier; } internal set { modifierY = value / PhysicsConfig.VariableMultiplier; } }

        public int LastCheckpointX { get; private set; }
        public int LastCheckpointY { get; private set; }

        /// <summary>
        /// The player ID in PlayerIO Messages
        /// </summary>
        public int ID { get; private set; }
        /// <summary>
        /// Player name in the world.
        /// </summary>
        public string Name { get; protected set; }
        public int Coins { get; set; }
        public int BlueCoins { get; set; }
        public bool IsClubMember { get; set; }


        public delegate void PlayerEvent(PlayerEventArgs e);

        private Dictionary<int, PlayerEvent> blockIdEvents = new Dictionary<int, PlayerEvent>();
        private Dictionary<int, PlayerEvent> bgblockIdEvents = new Dictionary<int, PlayerEvent>();
        private Dictionary<int, PlayerEvent> touchBlockEvents = new Dictionary<int, PlayerEvent>();
        private List<Point> touchedPoints = new List<Point>();

        public event PlayerEvent OnHitCrown = delegate { };

        /// <summary>
        /// Note: This will be called every time player hits coin, even if the coin is already got by that player. If you want to get only first time coin is hit, use event OnGetCoin.
        /// </summary>
        public event PlayerEvent OnHitCoin = delegate { };
        /// <summary>
        /// Will be called only when player hits a coin first time. After first time, only event OnHitCoin will be called.
        /// </summary>
        public event PlayerEvent OnGetCoin = delegate { };

        /// <summary>
        /// Note: This will be called every time player hits blue coin, even if the coin is already got by that player. If you want to get only first time coin is hit, use event OnGetBlueCoin.
        /// </summary>
        public event PlayerEvent OnHitBlueCoin = delegate { };
        /// <summary>
        /// Will be called only when player hits a blue coin first time. After first time, only event OnHitBlueCoin will be called.
        /// </summary>
        public event PlayerEvent OnGetBlueCoin = delegate { };

        /// <summary>
        /// Includes invisible portals.
        /// </summary>
        public event PlayerEvent OnHitPortal = delegate { };

        public event PlayerEvent OnHitRedKey = delegate { };
        public event PlayerEvent OnHitBlueKey = delegate { };
        public event PlayerEvent OnHitGreenKey = delegate { };
        public event PlayerEvent OnHitCyanKey = delegate { };
        public event PlayerEvent OnHitMagentaKey = delegate { };
        public event PlayerEvent OnHitYellowKey = delegate { };

        public event PlayerEvent OnHitPiano = delegate { };
        public event PlayerEvent OnHitDrum = delegate { };
        public event PlayerEvent OnHitSwitch = delegate { };
        public event PlayerEvent OnHitDiamond = delegate { };
        public event PlayerEvent OnHitCake = delegate { };

        public event PlayerEvent OnHitCompleteLevelBrick = delegate { };
        public event PlayerEvent OnHitCheckpoint = delegate { };

        public event PlayerEvent OnDie = delegate { };


        public PhysicsPlayer(int id, string name)
        {
            ID = id;
            Name = name;
            X = 16;
            Y = 16;
            gravity = (int)PhysicsConfig.Gravity;
            Switches = new BitArray(100);
        }

        internal void Tick()
        {
            double reminderX;
            double currentSX;
            double osx;
            double ox;
            double tx;
            double reminderY;
            double currentSY;
            double osy;
            double oy;
            double ty;

            int cx = ((int)(X + 8) >> 4);
            int cy = ((int)(Y + 8) >> 4);

            int delayed;
            if (queue.Count > 0)
                delayed = queue.Dequeue();
            else
                delayed = 0;
            current = HostWorld.GetBlock(0, cx, cy);
            if (this.tx != -1)
            {
                updateTeamDoors(this.tx, this.ty);
            }
            currentBelow = 0;
            if (current == 1 || current == 411)
            {
                currentBelow = HostWorld.GetBlock(0, cx - 1, cy);
            }
            else if (current == 2 || current == 412)
            {
                currentBelow = HostWorld.GetBlock(0, cx, cy - 1);
            }
            else if (current == 3 || current == 413)
            {
                currentBelow = HostWorld.GetBlock(0, cx + 1, cy);
            }
            else
            {
                currentBelow = HostWorld.GetBlock(0, cx, cy + 1);
            }

            queue.Enqueue(current);
            if (current == 4 || current == 414 || ItemId.isClimbable(current))
            {
                delayed = queue.Dequeue();
                queue.Enqueue(current);
            }
            /*var queue_length:int = this.tilequeue.length;
         while(queue_length--)
         {
            this.UpdatePurpleSwitchs(this.tilequeue.shift());
         }*/
            if (isOnFire && !isInvulnerable)
            {
                if (fireDeath <= 0)
                {
                    fireDeath = 200;
                    KillPlayer();
                }
                else
                {
                    fireDeath--;
                }
            }
            else
            {
                fireDeath = 200;
            }

            if (IsDead)
            {
                Horizontal = 0;
                Vertical = 0;
            }

            bool isGodMode = InGodMode;
            if (InGodMode)
            {
                morx = 0;
                mory = 0;
                mox = 0;
                moy = 0;
            }
            else
            {
                switch (current)
                {
                    case 1:
                    case ItemId.InvisibleLeftArrow:
                        morx = -((int)gravity);
                        mory = 0;
                        break;
                    case 2:
                    case ItemId.InvisibleUpArrow:
                        morx = 0;
                        mory = -((int)gravity);
                        break;
                    case 3:
                    case ItemId.InvisibleRightArrow:
                        morx = (int)gravity;
                        mory = 0;
                        break;
                    case ItemId.SpeedLeft:
                    case ItemId.SpeedRight:
                    case ItemId.SpeedUp:
                    case ItemId.SpeedDown:
                    case ItemId.Chain:
                    case ItemId.NinjaLadder:
                    case ItemId.WineH:
                    case ItemId.WineV:
                    case ItemId.InvisibleDot:
                    case 4:
                        morx = 0;
                        mory = 0;
                        break;
                    case ItemId.Water:
                        morx = 0;
                        mory = (int)PhysicsConfig.WaterBuoyancy;
                        break;
                    case ItemId.Mud:
                        morx = 0;
                        mory = (int)PhysicsConfig.MudBuoyancy;
                        break;
                    case ItemId.Fire:
                    case ItemId.Spike:
                        if (!IsDead && !isInvulnerable)
                        {
                            KillPlayer();
                        };
                        break;
                    case ItemId.EffectProtection:
                        // TODO
                        /*if (HostWorld.GetOnStatus(cx, cy) && isOnFire)
                        {
                            isOnFire = false;
                        }*/
                        break;
                    default:
                        morx = 0;
                        mory = gravity;
                        break;
                }

                switch (delayed)
                {
                    case 1:
                    case ItemId.InvisibleLeftArrow:
                        mox = -gravity;
                        moy = 0;
                        break;
                    case 2:
                    case ItemId.InvisibleUpArrow:
                        mox = 0;
                        moy = -gravity;
                        break;
                    case 3:
                    case ItemId.InvisibleRightArrow:
                        mox = gravity;
                        moy = 0;
                        break;
                    case ItemId.SpeedLeft:
                    case ItemId.SpeedRight:
                    case ItemId.SpeedUp:
                    case ItemId.SpeedDown:
                    case ItemId.Chain:
                    case ItemId.NinjaLadder:
                    case ItemId.WineH:
                    case ItemId.WineV:
                    case ItemId.InvisibleDot:
                    case 4:
                        mox = 0;
                        moy = 0;
                        break;
                    case ItemId.Water:
                        mox = 0;
                        moy = PhysicsConfig.WaterBuoyancy;
                        break;
                    case ItemId.Mud:
                        mox = 0;
                        moy = PhysicsConfig.MudBuoyancy;
                        break;
                    case ItemId.Lava:
                        mox = 0;
                        moy = PhysicsConfig.LavaBuoyancy;
                        break;
                    default:
                        mox = 0;
                        moy = gravity;
                        break;
                }
            }

            if (moy == PhysicsConfig.WaterBuoyancy || moy == PhysicsConfig.MudBuoyancy || moy == PhysicsConfig.LavaBuoyancy)
            {
                mx = Horizontal;
                my = Vertical;
            }
            else if (moy != 0)
            {
                mx = Horizontal;
                my = 0;
            }
            else if (mox != 0)
            {
                mx = 0;
                my = Vertical;
            }
            else
            {
                mx = Horizontal;
                my = Vertical;
            }

            mx *= SpeedMultiplier;
            my *= SpeedMultiplier;
            mox *= GravityMultiplier;
            moy *= GravityMultiplier;
            
            ModifierX = (mox + mx);
            ModifierY = (moy + my);

            if (!DoubleIsEqual(speedX, 0) || !DoubleIsEqual(modifierX, 0))
            {
                speedX = (speedX + modifierX);
                speedX = (speedX * PhysicsConfig.BaseDrag);
                if (currentBelow != 216 && !isGodMode)
                {
                    if ((mx == 0 && moy != 0) || (speedX < 0 && mx > 0) || (speedX > 0 && mx < 0) || ItemId.isClimbable(current))
                    {
                        speedX = (speedX * PhysicsConfig.NoModifierDrag);
                    }
                    else if (current == ItemId.Water)
                    {
                        speedX = (speedX * PhysicsConfig.WaterDrag);
                    }
                    else if (current == ItemId.Mud)
                    {
                        speedX = (speedX * PhysicsConfig.MudDrag);
                    }
                    else if (current == ItemId.Lava)
                    {
                        speedX = (speedX * PhysicsConfig.LavaDrag);
                    }
                }

                if (speedX > 16)
                {
                    speedX = 16;
                }
                else if (speedX < -16)
                {
                    speedX = -16;
                }
                else if (speedX < 0.0001 && speedX > -0.0001)
                {
                    speedX = 0;
                }
            }
            if (!DoubleIsEqual(speedY, 0) || !DoubleIsEqual(modifierY, 0))
            {
                speedY = (speedY + modifierY);
                speedY = (speedY * PhysicsConfig.BaseDrag);
                if (currentBelow != 216 && !isGodMode)
                {
                    if ((my == 0 && moy != 0) || (speedY < 0 && my > 0) || (speedY > 0 && my < 0) || ItemId.isClimbable(current))
                    {
                        speedY = (speedY * PhysicsConfig.NoModifierDrag);
                    }
                    else if (current == ItemId.Water)
                    {
                        speedY = (speedY * PhysicsConfig.WaterDrag);
                    }
                    else if (current == ItemId.Mud)
                    {
                        speedY = (speedY * PhysicsConfig.MudDrag);
                    }
                    else if (current == ItemId.Lava)
                    {
                        speedY = (speedY * PhysicsConfig.LavaDrag);
                    }
                }

                if (speedY > 16)
                {
                    speedY = 16;
                }
                else if (speedY < -16)
                {
                    speedY = -16;
                }
                else if (speedY < 0.0001 && speedY > -0.0001)
                {
                    speedY = 0;
                }
            }

            if (!isGodMode)
            {
                switch (this.current)
                {
                    case ItemId.SpeedLeft:
                        speedX = -PhysicsConfig.Boost;
                        break;
                    case ItemId.SpeedRight:
                        speedX = PhysicsConfig.Boost;
                        break;
                    case ItemId.SpeedUp:
                        speedY = -PhysicsConfig.Boost;
                        break;
                    case ItemId.SpeedDown:
                        speedY = PhysicsConfig.Boost;
                        break;
                }
            }

            reminderX = X % 1;
            currentSX = speedX;
            reminderY = Y % 1;
            currentSY = speedY;
            donex = false;
            doney = false;

            while ((currentSX != 0 && !donex) || (currentSY != 0 && !doney))
            {
                #region processPortals()
                current = HostWorld.GetBlock(cx, cy);
                if (!isGodMode && (current == ItemId.Portal || current == ItemId.PortalInvisible))
                {
                    if (!hasLastPortal)
                    {
                        OnHitPortal(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                        lastPortal = new Point(cx, cy);
                        hasLastPortal = true;
                        int[] data = HostWorld.GetBlockData(cx, cy);
                        if (data != null && data.Length == 3)
                        {
                            Point portalPoint;
                            if (HostWorld.TryGetPortalById(data[2], out portalPoint))
                            {
                                int rot1 = HostWorld.GetBlockData(lastPortal.x, lastPortal.y)[0];
                                int rot2 = HostWorld.GetBlockData(portalPoint.x, portalPoint.y)[0];
                                if (rot1 < rot2)
                                {
                                    rot1 += 4;
                                }
                                switch (rot1 - rot2)
                                {
                                    case 1:
                                        SpeedX = SpeedY * portalMultiplier;
                                        SpeedY = -SpeedX * portalMultiplier;
                                        ModifierX = ModifierY * portalMultiplier;
                                        ModifierY = -ModifierX * portalMultiplier;
                                        reminderY = -reminderY;
                                        currentSY = -currentSY;
                                        break;
                                    case 2:
                                        SpeedX = -SpeedX * portalMultiplier;
                                        SpeedY = -SpeedY * portalMultiplier;
                                        ModifierX = -ModifierX * portalMultiplier;
                                        ModifierY = -ModifierY * portalMultiplier;
                                        reminderY = -reminderY;
                                        currentSY = -currentSY;
                                        reminderX = -reminderX;
                                        currentSX = -currentSX;
                                        break;
                                    case 3:
                                        SpeedX = -SpeedY * portalMultiplier;
                                        SpeedY = SpeedX * portalMultiplier;
                                        ModifierX = -ModifierY * portalMultiplier;
                                        ModifierY = ModifierX * portalMultiplier;
                                        reminderX = -reminderX;
                                        currentSX = -currentSX;
                                        break;
                                }
                                X = portalPoint.x * 16;
                                Y = portalPoint.y * 16;
                                lastPortal = portalPoint;
                            }
                        }
                    }
                }
                else
                {
                    hasLastPortal = false;
                }
                #endregion

                ox = X;
                oy = Y;
                osx = currentSX;
                osy = currentSY;

                #region stepX()
                if (currentSX > 0)
                {
                    if ((currentSX + reminderX) >= 1)
                    {
                        X += 1 - reminderX;
                        X = Math.Floor(X);
                        currentSX -= 1 - reminderX;
                        reminderX = 0;
                    }
                    else
                    {
                        X += currentSX;
                        currentSX = 0;
                    }
                }
                else
                {
                    if (currentSX < 0)
                    {
                        if (!DoubleIsEqual(reminderX, 0) && (reminderX + currentSX) < 0)
                        {
                            currentSX += reminderX;
                            X -= reminderX;
                            X = Math.Floor(X);
                            reminderX = 1;
                        }
                        else
                        {
                            X += currentSX;
                            currentSX = 0;
                        }
                    }
                }
                if (HostWorld.Overlaps(this))
                {
                    X = ox;
                    speedX = 0;
                    currentSX = osx;
                    donex = true;
                }
                #endregion

                #region stepY()
                if (currentSY > 0)
                {
                    if ((currentSY + reminderY) >= 1)
                    {
                        Y += 1 - reminderY;
                        Y = Math.Floor(Y);
                        currentSY -= 1 - reminderY;
                        reminderY = 0;
                    }
                    else
                    {
                        Y += currentSY;
                        currentSY = 0;
                    }
                }
                else
                {
                    if (currentSY < 0)
                    {
                        if (!DoubleIsEqual(reminderY, 0) && (reminderY + currentSY) < 0)
                        {
                            Y -= reminderY;
                            Y = Math.Floor(Y);
                            currentSY += reminderY;
                            reminderY = 1;
                        }
                        else
                        {
                            Y += currentSY;
                            currentSY = 0;
                        }
                    }
                }
                if (HostWorld.Overlaps(this))
                {
                    Y = oy;
                    speedY = 0;
                    currentSY = osy;
                    doney = true;
                }
                #endregion
            }

            if (!IsDead)
            {
                if (pastx != cx || pasty != cy)
                {
                    PlayerEvent e;
                    if (blockIdEvents.Count != 0 && blockIdEvents.TryGetValue(current, out e))
                    {
                        e(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                    }
                    if (bgblockIdEvents.Count != 0 && bgblockIdEvents.TryGetValue(HostWorld.GetBlock(1, cx, cy), out e))
                    {
                        e(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                    }

                    // Might remove specific events soon, because you can make them now with void AddBlockEvent. (except OnGetCoin and OnGetBlueCoin)
                    switch (current)
                    {
                        case 100:   //coin
                            OnHitCoin(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            for (int i = 0; i < gotCoins.Count; i++)
                            {
                                if (gotCoins[i].x == cx && gotCoins[i].y == cy)
                                {
                                    goto found;
                                }
                            }
                            OnGetCoin(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            gotCoins.Add(new Point(cx, cy));
                        found:
                            break;
                        case 101:   // bluecoin
                            OnHitBlueCoin(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            for (int i = 0; i < gotBlueCoins.Count; i++)
                            {
                                if (gotBlueCoins[i].x == cx && gotBlueCoins[i].y == cy)
                                {
                                    goto found2;
                                }
                            }
                            OnGetBlueCoin(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            gotBlueCoins.Add(new Point(cx, cy));
                        found2:
                            break;
                        case 5:
                            // crown
                            OnHitCrown(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            break;
                        case 6:
                            // red key
                            OnHitRedKey(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            break;
                        case 7:
                            // green key
                            OnHitGreenKey(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            break;
                        case 8:
                            // blue key
                            OnHitBlueKey(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            break;
                        case ItemId.CyanKey:
                            OnHitCyanKey(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            break;
                        case ItemId.MagentaKey:
                            OnHitMagentaKey(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            break;
                        case ItemId.YellowKey:
                            OnHitYellowKey(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            break;
                        case ItemId.SwitchPurple:
                            int sid = HostWorld.GetBlockData(cx, cy)[0];
                            Switches[sid] = !Switches[sid];
                            OnHitSwitch(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            break;
                        case ItemId.Piano:
                            OnHitPiano(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            break;
                        case ItemId.Drum:
                            OnHitDrum(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            break;
                        case ItemId.Diamond:
                            OnHitDiamond(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            break;
                        case ItemId.Cake:
                            OnHitCake(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            break;
                        case ItemId.Checkpoint:
                            if (!isGodMode)
                            {
                                LastCheckpointX = cx;
                                LastCheckpointY = cy;
                                OnHitCheckpoint(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            }
                            break;
                        case ItemId.BrickComplete:
                            OnHitCompleteLevelBrick(new PlayerEventArgs() { Player = this, BlockX = cx, BlockY = cy });
                            break;
                    }
                    pastx = cx;
                    pasty = cy;
                }
                if (touchBlockEvents.Count > 0)
                {
                    PlayerEvent e;
                    Point p;
                    if (oldX != X || oldY != Y)
                    {
                        for (int i = 0; i < touchedPoints.Count; i++)
                        {
                            if (touchedPoints[i].y == cy)
                            {
                                if (X % 16 == 0 && (touchedPoints[i].x == cx - 1 || touchedPoints[i].x == cx + 1) && touchedPoints[i].y == cy)
                                {

                                }
                                else
                                {
                                    touchedPoints.RemoveAt(i--);
                                }
                            }
                            else if (touchedPoints[i].x == cx)
                            {
                                if (Y % 16 == 0 && (touchedPoints[i].y == cy - 1 || touchedPoints[i].y == cy + 1) && touchedPoints[i].x == cx)
                                {

                                }
                                else
                                {
                                    touchedPoints.RemoveAt(i--);
                                }
                            }
                            else
                            {
                                touchedPoints.RemoveAt(i--);
                            }
                        }
                        if (X % 16 == 0)
                        {
                            p = new Point(cx - 1, cy);
                            if (ItemId.isSolid(HostWorld.GetBlock(0, p.x, p.y)) && touchBlockEvents.TryGetValue(HostWorld.GetBlock(0, p.x, p.y), out e))
                            {
                                if (!touchedPoints.Contains(p))
                                {
                                    touchedPoints.Add(p);
                                    e(new PlayerEventArgs() { Player = this, BlockX = p.x, BlockY = p.y });
                                }
                            }
                            p = new Point(cx + 1, cy);
                            if (ItemId.isSolid(HostWorld.GetBlock(0, p.x, p.y)) && touchBlockEvents.TryGetValue(HostWorld.GetBlock(0, p.x, p.y), out e))
                            {
                                if (!touchedPoints.Contains(p))
                                {
                                    touchedPoints.Add(p);
                                    e(new PlayerEventArgs() { Player = this, BlockX = p.x, BlockY = p.y });
                                }
                            }
                        }
                        if (Y % 16 == 0)
                        {
                            p = new Point(cx, cy - 1);
                            if (ItemId.isSolid(HostWorld.GetBlock(0, p.x, p.y)) && touchBlockEvents.TryGetValue(HostWorld.GetBlock(0, p.x, p.y), out e))
                            {
                                if (!touchedPoints.Contains(p))
                                {
                                    touchedPoints.Add(p);
                                    e(new PlayerEventArgs() { Player = this, BlockX = p.x, BlockY = p.y });
                                }
                            }
                            p = new Point(cx, cy + 1);
                            if (ItemId.isSolid(HostWorld.GetBlock(0, p.x, p.y)) && touchBlockEvents.TryGetValue(HostWorld.GetBlock(0, p.x, p.y), out e))
                            {
                                if (!touchedPoints.Contains(p))
                                {
                                    touchedPoints.Add(p);
                                    e(new PlayerEventArgs() { Player = this, BlockX = p.x, BlockY = p.y });
                                }
                            }
                        }
                    }
                }
            }

            if (hasLevitation)
            {
                updateThrust();
            }

            if ((current != ItemId.Water && current != ItemId.Mud && current != ItemId.Lava) || InGodMode)
            {
                int imx = ((int)speedX << 8);
                if (imx == 0)
                {
                    if (modifierX < 0.1 && modifierX > -0.1)
                    {
                        tx = (X % 16);
                        if (tx < 2)
                        {
                            if (tx < 0.2)
                            {
                                X = Math.Floor(X);
                            }
                            else
                            {
                                X -= tx / 15;
                            }
                        }
                        else
                        {
                            if (tx > 14)
                            {
                                if (tx > 15.8)
                                {
                                    X = Math.Ceiling(X);
                                }
                                else
                                {
                                    X += (tx - 14) / 15;
                                }
                            }
                        }
                    }
                }

                int imy = ((int)speedY << 8);
                if (imy == 0)
                {
                    if ((modifierY < 0.1) && (modifierY > -0.1))
                    {
                        ty = (Y % 16);
                        if (ty < 2)
                        {
                            if (ty < 0.2)
                            {
                                Y = Math.Floor(Y);
                            }
                            else
                            {
                                Y -= ty / 15;
                            }
                        }
                        else
                        {
                            if (ty > 14)
                            {
                                if (ty > 15.8)
                                {
                                    Y = Math.Ceiling(Y);
                                }
                                else
                                {
                                    Y += (ty - 14) / 15;
                                }
                            }
                        }
                    }
                }
            }
            oldX = X;
            oldY = Y;
        }

        /// <summary>
        /// Makes PhysicsPlayer raise event when player moves inside blockId block. Event is not raised every tick, but only at when player first touches the block.
        /// (Touching doesn't count! Only the block that is at center of player is checked, no multiple blocks at same time.)
        /// </summary>
        /// <param name="blockId">Block ID to check for.</param>
        /// <param name="e">Method which is run when event occurs.</param>
        public void AddBlockEvent(int blockId, PlayerEvent e)
        {
            if (!ItemId.IsBackground(blockId))
                blockIdEvents[blockId] = e;
            else
                bgblockIdEvents[blockId] = e;
        }
        /// <returns>Whether there's block event with specified blockId.</returns>
        public bool HasBlockEvent(int blockId)
        {
            if (!ItemId.IsBackground(blockId))
                return blockIdEvents.ContainsKey(blockId);
            else
                return bgblockIdEvents.ContainsKey(blockId);
        }
        /// <summary>
        /// Removes block event added with AddBlockEvent with specified blockId.
        /// </summary>
        public void RemoveBlockEvent(int blockId)
        {
            if (!ItemId.IsBackground(blockId))
                blockIdEvents.Remove(blockId);
            else
                bgblockIdEvents.Remove(blockId);
        }

        public bool GetSwitchState(int switchId)
        {
            return Switches[switchId];
        }

        /// <summary>
        /// Makes PhysicsPlayer raise event when player touches blockId block (doesn't need to overlap). Event is not raised every tick, but at least when player first touches the block. It is possible that it's raised multiple times for same block in some cases.
        /// </summary>
        /// <param name="blockId">Block ID to check for.</param>
        /// <param name="e">Method which is run when event occurs.</param>
        public void AddBlockTouchEvent(int blockId, PlayerEvent e)
        {
            touchBlockEvents[blockId] = e;
        }
        /// <returns>Whether there's block event with specified blockId.</returns>
        public bool HasBlockTouchEvent(int blockId)
        {
            return touchBlockEvents.ContainsKey(blockId);
        }
        /// <summary>
        /// Removes block event added with AddBlockTouchEvent with specified blockId.
        /// </summary>
        public void RemoveBlockTouchEvent(int blockId)
        {
            touchBlockEvents.Remove(blockId);
        }

        /// <returns>True if player overlaps block at x,y.</returns>
        public bool OverlapsTile(int tx, int ty)
        {
            int xx = tx * 16;
            int yy = ty * 16;
            return ((X > xx - 16 && X <= xx + 16) && (Y > yy - 16 && Y <= yy + 16));
        }

        internal void Respawn()
        {
            ModifierX = 0;
            ModifierY = 0;
            SpeedX = 0;
            SpeedY = 0;
            IsDead = false;
        }
        internal void KillPlayer()
        {
            deaths++;
            IsDead = true;
            OnDie(new PlayerEventArgs() { Player = this, BlockX = ((int)(X + 8) >> 4), BlockY = ((int)(Y + 8) >> 4) });
        }
        internal void Reset()
        {
            gotCoins.Clear();
            gotBlueCoins.Clear();
            deaths = 0;
        }
        internal void RemoveCoin(int xx, int yy)
        {
            for (int i = 0; i < gotCoins.Count; i++)
            {
                if (gotCoins[i].x == xx && gotCoins[i].y == yy)
                {
                    gotCoins.RemoveAt(i);
                    break;
                }
            }
        }
        internal void RemoveBlueCoin(int xx, int yy)
        {
            for (int i = 0; i < gotCoins.Count; i++)
            {
                if (gotCoins[i].x == xx && gotCoins[i].y == yy)
                {
                    gotCoins.RemoveAt(i);
                    break;
                }
            }
        }

        internal void updateTeamDoors(int x, int y)
        {
            int _loc3_ = HostWorld.GetBlockData(x, y)[0];
            int _loc4_ = Team;
            if (Team != _loc3_)
            {
                Team = _loc3_;
                if (!HostWorld.Overlaps(this))
                {
                    //this.connection.send("team",param1,param2,_loc3_);
                    this.tx = -1;
                    this.ty = -1;
                }
                else
                {
                    Team = _loc4_;
                    this.tx = x;
                    this.ty = y;
                }
            }
        }
        internal void updateThrust()
        {
            if (this.mory != 0)
            {
                this.speedY = this.speedY - currentThrust * PhysicsConfig.JumpHeight / 2 * this.mory * 0.5;
            }
            if (this.morx != 0)
            {
                this.speedX = this.speedX - currentThrust * PhysicsConfig.JumpHeight / 2 * this.morx * 0.5;
            }
            if (isThrusting)
            {
                if (currentThrust > 0)
                {
                    currentThrust = currentThrust - thrustBurnOff;
                }
                else
                {
                    currentThrust = 0;
                }
            }
        }
        public void ApplyThrust()
        {
            currentThrust = maxThrust;
        }

        // this is used because: http://stackoverflow.com/questions/3103782/rule-of-thumb-to-test-the-equality-of-two-doubles-in-c
        internal bool DoubleIsEqual(double d1, double d2)
        {
            return Math.Abs(d1 - d2) < 0.00000001;
        }
    }

    public class PlayerEventArgs
    {
        /// <summary>
        /// Player which caused the event.
        /// </summary>
        public PhysicsPlayer Player { get; set; }
        /// <summary>
        /// Block X where event happened.
        /// </summary>
        public int BlockX { get; set; }
        /// <summary>
        /// Block Y where event happened.
        /// </summary>
        public int BlockY { get; set; }
    }

    internal struct Point
    {
        public int x, y;
        public Point(int xx, int yy)
        {
            x = xx;
            y = yy;
        }

        public bool Equals(Point p)
        {
            return (x == p.x && y == p.y);
        }

        public static bool operator ==(Point p, Point p2)
        {
            return p.Equals(p2);
        }
        public static bool operator !=(Point p, Point p2)
        {
            return !p.Equals(p2);
        }
    }
}
