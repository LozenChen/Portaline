using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.Portaline;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using static Celeste.Mod.Portaline.PortalineModule;

[CustomEntity("Portaline/PortalBullet")]
public class PortalBullet : Actor {
    private readonly bool isOrangePortal;
    private Vector2 velocity;
    private readonly Actor owner;
    private bool dead = false;

    private readonly Collision onCollideH;
    private readonly Collision onCollideV;

    private bool building = false;
    private Hitbox buildDataBefore;
    private Hitbox buildDataAfter;
    private Vector2 lastPosition;
    private Vector2 halfway;

    public PortalBullet(Vector2 position, Vector2 velocity, bool isOrangePortal, Actor owner) : base(position) {
        Position = position;
        this.velocity = velocity;
        this.isOrangePortal = isOrangePortal;
        this.owner = owner;

        Depth = 100;
        Collider = new Hitbox(4f, 4f, -2f, -2f);

        onCollideH += OnCollideH;
        onCollideV += OnCollideV;

        (owner.Scene as Level)?.Add(this);
    }

    public override void Update() {
        if (dead) return;
        lastPosition = Position;
        MoveH(velocity.X, onCollideH);
        if (dead) return;
        halfway = Position;
        MoveV(velocity.Y, onCollideV);

        Camera camera = (Scene as Level).Camera;
        if (Position.X < camera.X || Position.X > camera.X + 320f ||
            Position.Y < camera.Y || Position.Y > camera.Y + 180f) {
            Kill();
        }

        base.Update();
    }

    public override void Render() {
        // owner can die and be removed, so scene can be null
        (owner?.Scene as Level)?.Particles.Emit(ParticleTypes.SparkyDust, Position, isOrangePortal ? Color.Orange : Color.Aqua);
        base.Render();
    }

    public void DebugRenderAt(Vector2 position, Camera camera, Color color) {
        Vector2 orig = Position;
        Position = position;
        Collider.Render(camera, color);
        Position = orig;
    }

    public override void DebugRender(Camera camera) {
        if (PortalineModuleSettings.ShowAfterImage) {
            if (halfway != Position) {
                DebugRenderAt(halfway, camera, Color.Red * 0.5f);
            }

            if (lastPosition != halfway) {
                DebugRenderAt(lastPosition, camera, Color.Red * 0.2f);
            }
        }

        if (building) {
            Collider.Render(camera, Color.Yellow);
            if (buildDataBefore.AbsolutePosition == buildDataAfter.AbsolutePosition) {
                Monocle.Draw.HollowRect(buildDataBefore, Color.Yellow);
            }
            else {
                Monocle.Draw.HollowRect(buildDataBefore, Color.Green * 0.5f);
                Monocle.Draw.HollowRect(buildDataAfter, Color.Yellow);
            }
            return;
        }
        if (dead){
            Collider.Render(camera, Color.White);
        }
        else {
            Collider.Render(camera, Color.Red);
        }
    }

    private void OnCollideH(CollisionData data) {
        SpawnPortalAndDie(data);
    }

    private void OnCollideV(CollisionData data) {
        SpawnPortalAndDie(data);
    }

    private void SpawnPortalAndDie(CollisionData data) {
        if (dead) return;
        if (!(data.Hit is SolidTiles || data.Hit is FloatySpaceBlock)) {
            Kill();
            return;
        }

        int orientation;

        if (Math.Abs(data.Direction.X) > Math.Abs(data.Direction.Y)) {
            if (data.Direction.X < 0) {
                orientation = 0; // left
            }
            else {
                orientation = 1; // right
            }
        }
        else {
            if (data.Direction.Y < 0) {
                orientation = 2; // up
            }
            else {
                orientation = 3; // down
            }
        }

        Vector2 portalPos = Position;

        // collision pos do be weird like that
        if (orientation == 0) portalPos.X -= 1;
        else if (orientation == 1) portalPos.X += 1;
        else if (orientation == 3) portalPos.Y += 1;

        portalPos = new Vector2((float)Math.Floor(portalPos.X), (float)Math.Floor(portalPos.Y));

        PortalEntity newPortal = new(portalPos, orientation, isOrangePortal, data.Hit as Solid);
        buildDataAfter = newPortal.Collider as Hitbox;
        buildDataBefore = buildDataAfter.Clone() as Hitbox;
        building = true;

        Kill();

        if (!newPortal.PlacementCheck()) return;

        if (isOrangePortal) {
            Instance.orangePortal?.Kill();
            Instance.orangePortal = newPortal;
        }
        else {
            Instance.bluePortal?.Kill();
            Instance.bluePortal = newPortal;
        }
    }

    private void Kill() {
        dead = true;
        RemoveSelf();
    }
}