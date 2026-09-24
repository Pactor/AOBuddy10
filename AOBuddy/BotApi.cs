using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace AOBuddy
{
    /// <summary>
    /// LOCAL CONTROL API (owner, 2026-09-24): lets the aobuddy MCP server (tools/aobuddy-mcp) steer the bot the way
    /// the owner does by tell. Listens on 127.0.0.1 only - nothing off this PC can reach it - on Config.BotApiPort
    /// (0 = off). A plain TcpListener with a minimal HTTP reader, so no URL reservation or admin rights are needed.
    ///   GET  /status          -> JSON: the heartbeat line, the mission run's status, zone, position, credits, free slots
    ///   POST /command  (body) -> runs the text exactly as an owner tell; JSON: the replies it produced within ~2 s
    /// Every command taken this way is logged as "API CMD: ...".
    /// </summary>
    public sealed class BotApi
    {
        private readonly int _port;
        private readonly Action<string> _log;
        private readonly Func<JObject> _status;
        private readonly Action<string, Action<string>> _command;
        private TcpListener _listener;
        private Thread _thread;
        private volatile bool _running;

        public BotApi(int port, Action<string> log, Func<JObject> status, Action<string, Action<string>> command)
        {
            _port = port; _log = log; _status = status; _command = command;
        }

        public void Start()
        {
            if (_port <= 0) return;
            try
            {
                _listener = new TcpListener(IPAddress.Loopback, _port);
                _listener.Start();
                _running = true;
                _thread = new Thread(Loop) { IsBackground = true, Name = "AOBuddy BotApi" };
                _thread.Start();
                _log($"API: listening on 127.0.0.1:{_port} (status, command) for the aobuddy MCP server.");
            }
            catch (Exception ex) { _log($"API: couldn't listen on 127.0.0.1:{_port}: {ex.Message}"); }
        }

        public void Stop()
        {
            _running = false;
            try { _listener?.Stop(); } catch { }
        }

        private void Loop()
        {
            while (_running)
            {
                TcpClient c;
                try { c = _listener.AcceptTcpClient(); } catch { if (!_running) return; continue; }
                ThreadPool.QueueUserWorkItem(_ => Serve(c));
            }
        }

        private void Serve(TcpClient c)
        {
            using (c)
            {
                try
                {
                    c.ReceiveTimeout = 5000;
                    var s = c.GetStream();
                    // Request line + headers up to the blank line, then Content-Length bytes of body.
                    var head = new StringBuilder();
                    int b; int crlf = 0;
                    while (crlf < 4 && (b = s.ReadByte()) >= 0)
                    {
                        head.Append((char)b);
                        crlf = (b == '\r' || b == '\n') ? crlf + 1 : 0;
                    }
                    string[] lines = head.ToString().Split(new[] { "\r\n" }, StringSplitOptions.None);
                    string[] req = lines[0].Split(' ');
                    string method = req.Length > 0 ? req[0] : "", path = req.Length > 1 ? req[1] : "/";
                    int len = 0;
                    foreach (var l in lines)
                        if (l.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) int.TryParse(l.Substring(15).Trim(), out len);
                    var body = new byte[Math.Max(0, Math.Min(len, 65536))];
                    int got = 0;
                    while (got < body.Length) { int n = s.Read(body, got, body.Length - got); if (n <= 0) break; got += n; }
                    string text = Encoding.UTF8.GetString(body, 0, got);

                    JObject result;
                    if (method == "GET" && path.StartsWith("/status")) result = _status();
                    else if (method == "POST" && path.StartsWith("/command")) result = RunCommand(text);
                    else result = new JObject { ["error"] = "GET /status or POST /command" };
                    Reply(s, result.ToString());
                }
                catch (Exception ex) { try { _log($"API: request failed: {ex.Message}"); } catch { } }
            }
        }

        // The command runs as an owner tell would. Replies can come later than the call (the run answers from its
        // tick), so collect them until 0.6 s pass with none, at most 2.5 s.
        private JObject RunCommand(string text)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0) return new JObject { ["error"] = "empty command" };
            var replies = new List<string>();
            var gate = new object();
            DateTime last = DateTime.UtcNow, start = DateTime.UtcNow;
            _log($"API CMD: '{text}'");
            try { _command(text, r => { lock (gate) { replies.Add(r); last = DateTime.UtcNow; } }); }
            catch (Exception ex) { return new JObject { ["error"] = ex.Message }; }
            while (true)
            {
                Thread.Sleep(100);
                lock (gate)
                {
                    var now = DateTime.UtcNow;
                    if ((now - start).TotalSeconds > 2.5) break;
                    if ((now - last).TotalSeconds > 0.6 && (replies.Count > 0 || (now - start).TotalSeconds > 1.2)) break;
                }
            }
            lock (gate) return new JObject { ["command"] = text, ["replies"] = new JArray(replies.ToArray()) };
        }

        private static void Reply(Stream s, string json)
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            byte[] head = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
            s.Write(head, 0, head.Length);
            s.Write(body, 0, body.Length);
            s.Flush();
        }
    }
}
