using Microsoft.Xna.Framework.Input;
using System.Security.Cryptography.X509Certificates;

namespace Celeste.Mod.Portaline {
    [SettingName("Portaline Settings")]
    public class PortalineModuleSettings : EverestModuleSettings {
        [SettingName("Portal Gun Enabled")]
        [SettingSubText("If you disable it in-game, your portals will also disappear. \nNote: In addition to the keyboard and joystick settings below,\nyou can also shoot blue portals with the left mouse button,\nand orange portals with the right mouse button.")]
        public bool PortalGunEnabled { get; set; } = false;

        [DefaultButtonBinding(Buttons.LeftShoulder, Keys.O)]
        public ButtonBinding ShootBluePortal { get; set; }

        [DefaultButtonBinding(Buttons.RightShoulder, Keys.P)]
        public ButtonBinding ShootOrangePortal { get; set; }

        [DefaultButtonBinding(Buttons.RightStick, Keys.Q)]
        public ButtonBinding RemovePortals { get; set;}

        [SettingSubText("How to implement Vector2.Rotate((float) Math.PI / 2f). If true then it will be imprecise.")]
        public static bool RotateFeature { get; set; } = true;

        [SettingSubText("DebugRender AfterImage of PortalBullet.")]
        public static bool ShowAfterImage { get; set; } = true;
    }
}
