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


Proposal for priority-based async gate system:

````
// ─── Priority levels ─────────────────────────────────────────────────
public enum ControlPriority
{
    None        = 0,
    Travel      = 1,
    Mission     = 2,
    Combat      = 3,   // interrupts everything below it
    Emergency   = 4,   // e.g. player dying, disconnect
}

// ─── ControlArbiter ──────────────────────────────────────────────────
public sealed class ControlArbiter
{
    private int _activePriority = (int)ControlPriority.None;
    private TaskCompletionSource _resume;

    /// <summary>
    /// Runs a step. If a higher-priority system is active (or becomes active),
    /// the step suspends until control is released back down.
    /// </summary>
    public async Task RunStepAsync(ControlPriority priority, Func<Task> step, CancellationToken ct)
    {
        await YieldUntilAvailableAsync(priority, ct);
        await step();
    }

    /// <summary>
    /// For long-running steps that tick (e.g. "clear mission" over many heartbeats).
    /// Each tick checks if control is still available.
    /// </summary>
    public async Task RunTicksAsync(ControlPriority priority, Func<Task> tick,
                                    Func<bool> shouldContinue, CancellationToken ct)
    {
        while (shouldContinue() && !ct.IsCancellationRequested)
        {
            await YieldUntilAvailableAsync(priority, ct);
            await tick();
        }
    }

    private async Task YieldUntilAvailableAsync(ControlPriority priority, CancellationToken ct)
    {
        while (Volatile.Read(ref _activePriority) >= (int)priority)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _resume = tcs;
            // re-check after registering (avoid missed signal)
            if (Volatile.Read(ref _activePriority) < (int)priority)
                return;
            await tcs.Task.WaitAsync(ct);
        }
    }

    // ── called by interrupting systems ──
    public void TakeControl(ControlPriority priority)
    {
        Volatile.Write(ref _activePriority, (int)priority);
    }

    public void ReleaseControl()
    {
        Volatile.Write(ref _activePriority, (int)ControlPriority.None);
        _resume?.TrySetResult();
    }

    public bool HasControl(ControlPriority priority)
        => Volatile.Read(ref _activePriority) < (int)priority;
}
````

The sequence — a mission run (an example, more steps like resupply, buff up would be needed):

````
public sealed class MissionSequence
{
    private readonly ControlArbiter _arbiter;
    private readonly MissionController _missions;
    private readonly TravelController _travel;
    private readonly GameQueries _queries;

    public MissionSequence(ControlArbiter arbiter, MissionController missions,
                           TravelController travel, GameQueries queries)
    { /* … */ }

    public async Task RunAsync(CancellationToken ct)
    {
        // 1. Roll
        await _arbiter.RunStepAsync(ControlPriority.Mission,
            () => _missions.RollMissionAsync(ct), ct);

        // 2. Take
        await _arbiter.RunStepAsync(ControlPriority.Mission,
            () => _missions.AcceptMissionAsync(ct), ct);

        // 3. Travel to entrance
        await _arbiter.RunStepAsync(ControlPriority.Mission,
            () => _travel.TravelTo(_entrance, ct), ct);

        // 4. Enter
        await _arbiter.RunStepAsync(ControlPriority.Mission,
            () => _missions.EnterAsync(ct), ct);

        // 5. Clear — long-running, ticks per heartbeat
        await _arbiter.RunTicksAsync(
            ControlPriority.Mission,
            tick: () => _missions.MissionTickAsync(ct),
            shouldContinue: () => _queries.MissionActive,
            ct);

        // 6. Exit
        await _arbiter.RunStepAsync(ControlPriority.Mission,
            () => _missions.ExitAsync(ct), ct);

        // 7. Return
        await _arbiter.RunStepAsync(ControlPriority.Mission,
            () => _travel.TravelTo(_terminal, ct), ct);
    }
}
````

Combat brain takes an releases control:

````
public sealed class MetaphysicistBrain : IFightingBrain
{
    private readonly ControlArbiter _arbiter;
    private bool _inCombat;

    public bool IsInCombat => _inCombat;

    public void StartCombat()
    {
        _inCombat = true;
        _arbiter.TakeControl(ControlPriority.Combat);  // ← suspends mission step
    }

    public void StopCombat()
    {
        _inCombat = false;
        _arbiter.ReleaseControl();  // ← mission step resumes where it left off
    }

    // … nano logic, etc.
}   
````

What actually happens during "Clear Mission" when an enemy spawns:
````Time →

MissionSequence.ClearMission tick 1  ──┐
                                       │  enemy spawns
                                       │  brain.StartCombat()
                                       │  arbiter.TakeControl(Combat)
                                       │
                                       │  ← tick 2 sees priority >= Mission, yields
                                       │  ← tick 3 yields
                                       │  ← tick 4 yields
                                       │
                                       │  brain.StopCombat()
                                       │  arbiter.ReleaseControl()
                                       │
MissionSequence.ClearMission tick 5  ──┘  resumes, continues clearing   
````

Travel can also be interrupted (aggro mid-travel for example) - very simplified travelto
````
// Inside TravelController.TravelTo:
public async Task TravelTo(Vector2 target, CancellationToken ct)
{
    while (DistanceTo(target) > threshold && !ct.IsCancellationRequested)
    {
        // travel operates at Travel priority — combat (3) > travel (1)
        // so if combat starts mid-travel, this loop yields
        await _arbiter.YieldUntilAvailableAsync(ControlPriority.Travel, ct);
        SendMovePacket(target);
        await Task.Delay(HeartbeatMs, ct);
    }
}   
````

