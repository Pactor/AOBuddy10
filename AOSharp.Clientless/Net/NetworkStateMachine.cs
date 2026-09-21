using Serilog.Core;
using Stateless;
using System;
using System.Net;

namespace AOSharp.Clientless.Net
{
    internal enum Trigger
    {
        Stop,
        Connect,
        Disconnect,
        OnTcpConnected,
        OnTcpDisconnected,
        OnTcpConnectionError,
        OnTcpConnectError,
        ConnectionEstablished,
        FailedToRetreiveDimensionInfo,
        FailedToLogin,
        Error107,
        CharInPlay,
        BeginZoning,
        EndZoning
    }

    internal enum State
    {
        Idle,
        Disconnected,
        Connecting,
        Connected,
        Authenticating,
        CharacterSelect,
        InPlay,
        Zoning
    }

    internal class NetworkStateMachine : StateMachine<State, Trigger>
    {
        private Logger _logger;
        public TriggerWithParameters<IPEndPoint> ConnectTrigger;
        public TriggerWithParameters<IPEndPoint, Exception> ConnectErrorTrigger;

        public NetworkStateMachine(Logger logger) : base(State.Idle)
        {
            _logger = logger;
            ConnectTrigger = SetTriggerParameters<IPEndPoint>(Trigger.Connect);
            ConnectErrorTrigger = SetTriggerParameters<IPEndPoint, Exception>(Trigger.OnTcpConnectionError);
            OnTransitioned(OnTransitionAction);
        }

        private void OnTransitionAction(Transition obj)
        {
            _logger.Debug($"Gameserver state transition from {obj.Source} to {obj.Destination} triggered by {obj.Trigger}. Re-entry is {obj.IsReentry}");
        }
    }
}
