using Serilog.Core;
using Stateless;
using System;
using System.Net;

namespace AOSharp.Clientless.Chat.Net
{
    public enum Trigger
    {
        Stop,
        Connect,
        Disconnect,
        OnTcpConnected,
        OnTcpDisconnected,
        OnTcpConnectionError,
        OnTcpConnectError,
        FailedToRetreiveDimensionInfo,
        ConnectionEstablished,
        LoginOK
    }

    public enum State
    {
        Idle,
        Disconnected,
        Connecting,
        Connected,
        Authenticating,
        CharacterSelect,
        Chatting
    }

    public class NetworkStateMachine : StateMachine<State, Trigger>
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
            _logger.Debug($"Chat state transition from {obj.Source} to {obj.Destination} triggered by {obj.Trigger}. Re-entry is {obj.IsReentry}");
        }
    }
}
