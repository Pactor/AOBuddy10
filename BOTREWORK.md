Outline for new Bot structure:

Main loop just code which receives data from the server and handles the hearbeat. This opens it up for fully asnyc code in the other classes.
- The other classes can register for packet types they wish to receive (multiple recipients possible, but with a optional 'end sequence' flag, just like windows event system with handled=true does). This makes different fighting brain classes possible for different professions, also with their special commands to change their behavior, for example pet professions though they are 1hb/2hb utilizing they're nano attacks more or fixers using their roots and hold distance or how to handle multiple mobs (lowest first to diminish their numbers first or highest first to get rid of the biggest threat)
- High level functions for "am i in a shop" (check via pf id), "am i in a mission building", "which are my pets" (including level and running nanos), "are my pets being attacked", each pet with its own state machine (following you, guarding you, location etc)

Then separate classes for travel (handling all movement stuff besides owner follow), called from for example the fighting brains, for mission handling (get mission, finish mission goal - for example repair machine/find item etc), for buffing, for twinking and other things.

Packet are received and sent by Smokelounge.Aotomation and AOSharp.clientless classes.

Probably also extend this so there is a distinction between packetsubscribers which return a packet to server and ones which do only read the packets without further reaction to them (Inventory for example). The ones which only consume the packet without sending a reply shouldn't be able to set handled to true but also should have it delivered even if another class sets handled to true.

Composition with Microsoft.Extensions.DependencyInjection

Examples (no hard requirement) for interfaces/classes:


````
// ─── Observer: read-only, never sends ────────────────────────────────
public interface IPacketObserver
{
    void RegisterWith(PacketRouter router);
}

// ─── Actor: receives AND can send response packets ───────────────────
public interface IPacketActor
{
    void RegisterWith(PacketRouter router, IPacketSender sender);
    // actor is also the "exclusive" party — it owns endSequence
}

public interface IPacketSender
{
    void Send(Packet p);
    Task SendAsync(Packet p, CancellationToken ct);
}
````

````
public sealed class PacketRouter
{
    private readonly Dictionary<PacketType, List<PacketHandlerEntry>> _handlers = new();

    // Observers: always endSequence: false, no sender access
    public void RegisterObserver(PacketType type, Action<Packet> handler)
    {
        _handlers.GetOrAdd(type, _ => new())
                 .Add(new PacketHandlerEntry(handler, endSequence: false));
    }

    // Actors: can claim exclusive handling, get a sender
    public void RegisterActor(PacketType type, Action<Packet> handler,
                              bool endSequence = true)
    {
        _handlers.GetOrAdd(type, _ => new())
                 .Add(new PacketHandlerEntry(handler, endSequence));
    }

    public void Dispatch(Packet packet) { /* same as before */ }
}
````

````
public sealed class MainLoop
{
    private readonly PacketReceiver _receiver;
    private readonly PacketRouter _router;
    private readonly List<IHeartbeatHandler> _hbHandlers;

    public async Task RunAsync(CancellationToken ct)
    {
        // subscribe to _receiver.PacketReceived → _router.Dispatch
        // subscribe to _receiver.HeartbeatReceived → _hbHandlers
        // await until cancelled
    }
}

public interface IHeartbeatHandler
{
    void OnHeartbeat(int hbCount);
}

// ─── PacketRouter (event bus with "handled" semantics) ──────────────
public sealed class PacketRouter
{
    private readonly Dictionary<PacketType, List<PacketHandlerEntry>> _handlers
        = new();

    public void Register(PacketType type, Action<Packet> handler, bool endSequence = false)
    {
        _handlers.GetOrAdd(type, _ => new()).Add(new PacketHandlerEntry(handler, endSequence));
    }

    public void Dispatch(Packet packet)
    {
        if (!_handlers.TryGetValue(packet.Type, out var list)) return;
        foreach (var entry in list)
        {
            entry.Handler(packet);
            if (entry.EndSequence) break;   // ← handled=true, stop propagation
        }
    }
}

internal record PacketHandlerEntry(Action<Packet> Handler, bool EndSequence);

// ─── Fighting Brains ─────────────────────────────────────────────────
public interface IFightingBrain
{
    void OnPacket(Packet p);           // registered via router
    void StartCombat();
    void StopCombat();
    void SetMobPriority(MobPriorityStrategy strategy);
}

public enum MobPriorityStrategy { LowestHpFirst, HighestThreatFirst, ClosestFirst }

public class PetFightingBrain : IFightingBrain
{
    // 1hb / 2hb nano attack logic
    // registers for: EnemySpawn, EnemyHpChanged, NanoCooldownReady, ...
}

public class FixerFightingBrain : IFightingBrain
{
    // roots, hold-distance, kiting
    // registers for: EnemyPosition, RootCooldownReady, ...
}

// ─── High-Level Queries ──────────────────────────────────────────────
public sealed class GameQueries
{
    // cached state, updated by subscribing to relevant packets
    public bool AmInShop { get; private set; }          // checked via PF id
    public bool AmInMissionBuilding { get; private set; }
    public IReadOnlyList<PetState> GetPets() => _pets;  // level, running nanos, etc.
    public bool ArePetsBeingAttacked() => _pets.Any(p => p.State == PetState.BeingAttacked);
}

// ─── Pet State Machine ───────────────────────────────────────────────
public enum PetState { Following, Guarding, AtLocation, BeingAttacked, Idle }

public sealed class PetStateMachine
{
    public PetState State { get; private set; }
    public int Level { get; }
    public int RunningNanos { get; }

    public void Transition(PetState next, Packet trigger);
}

// ─── Controllers ─────────────────────────────────────────────────────
public sealed class TravelController
{
    // all movement EXCEPT owner-follow
    // called by fighting brains, mission controller, etc.
    public Task TravelTo(Vector2 target, CancellationToken ct);
    public Task FollowOwner(CancellationToken ct);  // owner-follow lives here too
}

public sealed class MissionController
{
    // get mission → work goal → finish
    public async Task AcceptMission(int missionId, CancellationToken ct);
    public async Task WorkGoal(CancellationToken ct);   // repair machine, find item, …
    public async Task FinishMission(CancellationToken ct);
}

public sealed class BuffController
{
    public async Task ApplyAllBuffs(CancellationToken ct);
}

public sealed class TwinkController
{
    public async Task TwinkLoop(CancellationToken ct);
}
````

````
// ─── Holder (what login actually injects) ────────────────────────────
public sealed class FightingBrainHolder
{
    private IFightingBrain _current;

    public IFightingBrain Current => _current
        ?? throw new InvalidOperationException("Brain not activated yet");

    public bool IsReady => _current != null;

    public void SetActive(IFightingBrain brain)
    {
        _current?.StopCombat();
        _current = brain;
    }
}
````
