
using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.Portaline;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using System;
using static Celeste.Mod.Portaline.PortalineModule;

[CustomEntity("Portaline/PortalEntity")]
public class PortalEntity : Entity {
    public bool dead;
    public int orientation;
    public bool isOrangePortal;
    public Solid owner;

    private Vector2 oldOwnerPos;
    private float scale = 0;

    public Vector2 TpPosition {
        get {
            Vector2 finalPos = Position;

            switch (orientation) {
                case 0: finalPos.X += 8; break;
                case 1: finalPos.X -= 8; break;
                case 2: finalPos.Y += 12; break;
                case 3: finalPos.Y -= 8; break;
            }

            finalPos.Y += 4;

            return finalPos;
        }
    }

    public PortalEntity(Vector2 position, int orientation, bool isOrangePortal, Solid owner) {
        Position = position;
        this.orientation = orientation;
        this.isOrangePortal = isOrangePortal;
        this.owner = owner;
        Collider = orientation < 2 ? new Hitbox(4, 8, -2, -4) : new Hitbox(8, 4, -4, -2);
        oldOwnerPos = owner.Position;
        (owner.Scene as Level)?.Add(this);
    }

    public bool CollisionCheck(Player executer) {
        if (Scene == null) return false;

        Player player = CollideFirst<Player>();

        if (player != null) {
            PortalEntity opposingPortal = isOrangePortal ? Instance.bluePortal : Instance.orangePortal;

            // check if opposing portal exists
            if (opposingPortal == null) return false;

            // check if opposing portal is obstructed
            int oriRotation = 0;

            switch (opposingPortal.orientation) {
                case 0: oriRotation = 0; break;
                case 1: oriRotation = 2; break;
                case 2: oriRotation = 1; break;
                case 3: oriRotation = 3; break;
            }

            Rectangle frontRect = opposingPortal.RectFromVectors(new Vector2(2, 0).RotatePI(oriRotation), new Vector2(3, 16).RotatePI(oriRotation));

            if (Scene.CollideFirst<Solid>(frontRect) != null) return false;

            // same orientation portals
            if (orientation == opposingPortal.orientation) {
                if (orientation < 2) {
                    player.Speed.X *= -1;
                    player.DashDir.X *= -1;
                    player.Facing = player.Facing == Facings.Left ? Facings.Right : Facings.Left;
                }
                else {
                    player.Speed.Y *= -1;
                    player.DashDir.Y *= -1;
                }
            }

            // this portal has top-bottom orientation and the other portal has left-right orientation
            if (orientation > 1 && opposingPortal.orientation < 2) {
                if ((opposingPortal.orientation == 0 && orientation == 3) ||
                    (opposingPortal.orientation == 1 && orientation == 2)) {
                    player.Speed = player.Speed.RotatePI(-1);
                    player.DashDir = player.DashDir.RotatePI(-1);
                }
                else {
                    player.Speed = player.Speed.RotatePI(1);
                    player.DashDir = player.DashDir.RotatePI(1);
                }
                player.Facing = player.Speed.X >= 0 ? Facings.Right : Facings.Left;
            }

            // this portal has left-right orientation and the other portal has top-bottom orientation
            if (orientation < 2 && opposingPortal.orientation > 1) {
                if ((opposingPortal.orientation == 2 && orientation == 0) ||
                    (opposingPortal.orientation == 3 && orientation == 1)) {
                    player.Speed = player.Speed.RotatePI(-1);
                    player.DashDir = player.DashDir.RotatePI(-1);
                }
                else {
                    player.Speed = player.Speed.RotatePI(1);
                    player.DashDir = player.DashDir.RotatePI(1);
                }
                player.Facing = player.Speed.X >= 0 ? Facings.Right : Facings.Left;
            }

            // this is to prevent auto jump thing when entering a top portal that leads to another top portal
            DynamicData.For(player).Set("varJumpTimer", 0f);

            // change player position to portal
            player.Position = opposingPortal.TpPosition;

            Audio.Play("event:/sneezingcactus/portal_travel");

            return player == executer;
        }

        return false;
    }

    public bool PlacementCheck() {
        Scene scene = Engine.Scene;

        // portal overlap
        if ((isOrangePortal && Instance.bluePortal != null && (Position - Instance.bluePortal.Position).Length() < 16f) ||
            (!isOrangePortal && Instance.orangePortal != null && (Position - Instance.orangePortal.Position).Length() < 16f)) {
            Kill();
            return false;
        }

        int oriRotation = 0;

        switch (orientation) {
            case 0: oriRotation = 0; break;
            case 1: oriRotation = 2; break;
            case 2: oriRotation = 1; break;
            case 3: oriRotation = 3; break;
        }

        for (int i = 0; i < 100; i++) {
            // obstruction (stuff in front of the portal)
            bool noObstruction = false;

            Rectangle frontLeft = RectFromVectors(new Vector2(2, -4).RotatePI(oriRotation), new Vector2(3, 8).RotatePI(oriRotation));
            Rectangle frontRight = RectFromVectors(new Vector2(2, 4).RotatePI(oriRotation), new Vector2(3, 8).RotatePI(oriRotation));

            Solid frontLeftSolid = scene.CollideFirst<Solid>(frontLeft);
            Solid frontRightSolid = scene.CollideFirst<Solid>(frontRight);

            if (frontLeftSolid == null && frontRightSolid == null) {
                noObstruction = true;
            }
            else if (frontLeftSolid != null && frontRightSolid != null) {
                Kill();
                return false;
            }
            else if (frontLeftSolid != null) {
                Position += new Vector2(0, 1).RotatePI(oriRotation);
            }
            else {
                Position -= new Vector2(0, 1).RotatePI(oriRotation);
            }

            // off-surface (portal not being completely sticked to a surface)
            bool noOffSurface = false;

            Rectangle backLeft = RectFromVectors(new Vector2(-2, -8).RotatePI(oriRotation), new Vector2(3, 2).RotatePI(oriRotation));
            Rectangle backRight = RectFromVectors(new Vector2(-2, 8).RotatePI(oriRotation), new Vector2(3, 2).RotatePI(oriRotation));

            Solid backLeftSolid = scene.CollideFirst<Solid>(backLeft);
            Solid backRightSolid = scene.CollideFirst<Solid>(backRight);

            if (backLeftSolid != null && backRightSolid != null) {
                noOffSurface = true;
            }
            else if (backLeftSolid == null && backRightSolid == null) {
                Kill();
                return false;
            }
            else if (backLeftSolid == null) {
                Position += new Vector2(0, 1).RotatePI(oriRotation);
            }
            else {
                Position -= new Vector2(0, 1).RotatePI(oriRotation);
            }

            if (noObstruction && noOffSurface) {
                return true;
            }
        }

        // it shouldn't reach the end of the for loop if it was placed correctly, therefore Kill
        Kill();
        return false;
    }

    public Rectangle RectFromVectors(Vector2 position, Vector2 size) {
        return new(
          (int)Math.Round(Position.X + position.X - Math.Abs(size.X) / 2),
          (int)Math.Round(Position.Y + position.Y - Math.Abs(size.Y) / 2),
          (int)Math.Round(Math.Abs(size.X)),
          (int)Math.Round(Math.Abs(size.Y))
        );
    }

    public bool HighPriorityUpdate(Player executer) {
        return this?.CollisionCheck(executer) ?? false;
    }

    public override void Update() {
        base.Update();

        if (!dead) {
            if (owner != null) Position += owner.Position - oldOwnerPos;
            oldOwnerPos = owner.Position;
            CollisionCheck(null);
        }

        if (dead && Scene != null) {
            RemoveSelf();
        }
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        Kill();
    }

    public override void SceneEnd(Scene scene) {
        base.SceneEnd(scene);
        Kill();
    }

    public override void Render() {
        base.Render();

        if (dead) return;

        Instance.portalTex.DrawCentered(
          Position,
          isOrangePortal ? Color.Orange : Color.Aqua,
          new Vector2(1, scale),
          orientation > 1 ? ((float)Math.PI) * 0.5f : 0,
          orientation == 1 || orientation == 3 ? SpriteEffects.FlipHorizontally : SpriteEffects.None
        );
        scale += (1 - scale) * 0.25f;
    }

    public override void DebugRender(Camera camera) {
        base.DebugRender(camera);
        Monocle.Draw.HollowRect(TpPosition - new Vector2(4f, 11f), 8f, 11f, Color.Red * 0.5f);
    }

    public void Kill() {
        dead = true;
        RemoveSelf();
    }
}

public static class Vector2Extension  {
    public static Vector2 RotatePI(this Vector2 vec, int n) {
        if (PortalineModuleSettings.RotateFeature) {
            return vec.Rotate((float)Math.PI / 2f * n);
        } else 
            return n switch {
            0 => vec,
            1 => new Vector2(-vec.Y, vec.X),
            2 => -vec,
            3 => new Vector2(vec.Y, -vec.X),
            -1 => new Vector2(vec.Y, -vec.X),
            4 => vec,
            _ => RotatePI(vec, n % 4 + 4),
        };
    }
    // bugfix: if use Vector2.Rotate, then left dash rotate to up dash, but can't wall bounce coz DashDir.X = 0.000001 not zero!
}