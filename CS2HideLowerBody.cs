using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2HideLowerBody
{
    [MinimumApiVersion(50)]
    public class CS2HideLowerBody : BasePlugin
    {
        public override string ModuleName => "HideLowerBody";
        public override string ModuleAuthor => "DRANIX";
        public override string ModuleDescription => "Allows players to hide their first person legs model. (lower body view model)";
        public override string ModuleVersion => "1.0.1";
        private static readonly Dictionary<int, bool> players = new Dictionary<int, bool>();

        public override void Load(bool hotReload)
        {
            this.AddCommand("legs", "Hides the lower body view model of a player.", CommandHideLowerBody);

            this.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            
            this.RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
            this.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);

            if (!hotReload) return;
            
            List<CCSPlayerController> controllers = Utilities.GetPlayers();

            foreach (var player in controllers) OnClientPutInServer(player.UserId??-1);
        }

        public override void Unload(bool hotReload)
        {
            players.Clear();
        }
        
        private void OnClientPutInServer(int playerSlot)
        {
            players.Add(playerSlot, false);
        }
        
        private void OnClientDisconnect(int playerSlot)
        {
            players.Remove(playerSlot);
        }
        
        [GameEventHandler]
        private static HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            CCSPlayerController controller = @event.Userid!;
            
            if (!controller!.IsValid || controller.IsBot || controller.TeamNum <= (byte)CsTeam.Spectator || !players.ContainsKey(controller.UserId??-1)) return HookResult.Continue;
            
            SetPawnAlphaRender(controller);

            return HookResult.Continue;
        }

        [ConsoleCommand("css_legs", "Hides the lower body view model of a player.")]
        [ConsoleCommand("css_lowerbody", "Hides the lower body view model of a player.")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private void CommandHideLowerBody(CCSPlayerController? controller, CommandInfo command)
        {
            int playerSlot = controller?.UserId??-1;
            
            players[playerSlot] ^= true;

            if (controller is { IsValid: true, PawnIsAlive: true })
            {
                SetPawnAlphaRender(controller);
                UpdatePlayer(controller);
            }
        }

        private static void SetPawnAlphaRender(CCSPlayerController controller) => controller.PlayerPawn.Value!.Render = Color.FromArgb(players[controller.UserId??-1] ? 254 : 255,
                controller.PlayerPawn.Value.Render.R, controller.PlayerPawn.Value.Render.G, controller.PlayerPawn.Value.Render.B);

        private static void UpdatePlayer(CCSPlayerController controller)
        {
            const string classNameHealthShot = "weapon_healthshot";

            controller.GiveNamedItem(classNameHealthShot);
            
            var healthShot = controller.PlayerPawn.Value!.WeaponServices!.MyWeapons.FirstOrDefault(weapon => weapon is { IsValid: true, Value: { IsValid: true, DesignerName: classNameHealthShot } });

            if (!healthShot!.IsValid) return;

            healthShot.Value!.Remove();
        }
    }
}