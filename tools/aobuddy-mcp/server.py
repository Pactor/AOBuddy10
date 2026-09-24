"""aobuddy MCP server: lets Claude Code watch and steer the AOBuddy bot.

Talks to the bot's local control API (AOBuddy/BotApi.cs, 127.0.0.1 only) and reads its log file.
Standard library only; MCP over stdio (newline-delimited JSON-RPC 2.0).

Tools
  bot_status              zone, position, HP, credits, free slots, the mission run's phase, the heartbeat line
  bot_command  text       runs text exactly as an owner tell (e.g. "mission run skip") and returns the bot's replies
  bot_log      lines,grep the latest lines of aobuddy.log, optionally only those matching grep (regex, case-insensitive)

Environment
  AOBUDDY_API   default http://127.0.0.1:5591
  AOBUDDY_LOG   default E:/Funcom/AOBuddy10/Build/Plugins/AOBuddy/aobuddy.log

Register:  claude mcp add aobuddy -- python E:/Funcom/AOBuddy10/tools/aobuddy-mcp/server.py
"""
import json
import os
import re
import sys
import urllib.error
import urllib.request

API = os.environ.get("AOBUDDY_API", "http://127.0.0.1:5591").rstrip("/")
LOG = os.environ.get("AOBUDDY_LOG", r"E:/Funcom/AOBuddy10/Build/Plugins/AOBuddy/aobuddy.log")

TOOLS = [
    {
        "name": "bot_status",
        "description": "The AOBuddy bot's state right now: zone, position, HP %, credits, level, free inventory slots, "
                       "whether it is dead, the mission run's phase, and its heartbeat line.",
        "inputSchema": {"type": "object", "properties": {}},
    },
    {
        "name": "bot_command",
        "description": "Send the AOBuddy bot a command exactly as its owner would by tell, e.g. 'mission run status', "
                       "'mission run skip', 'mission run stop', 'travelto 635 548 800'. Returns the bot's replies.",
        "inputSchema": {
            "type": "object",
            "properties": {"text": {"type": "string", "description": "The command text."}},
            "required": ["text"],
        },
    },
    {
        "name": "bot_log",
        "description": "The latest lines of the bot's log (aobuddy.log). Optional grep: a case-insensitive regex; "
                       "only matching lines are returned (e.g. 'MISSIONRUN|DIED|OVERLAND').",
        "inputSchema": {
            "type": "object",
            "properties": {
                "lines": {"type": "integer", "description": "How many lines to return (default 60, max 500)."},
                "grep": {"type": "string", "description": "Case-insensitive regex filter."},
            },
        },
    },
]


def http(method, path, body=None, timeout=8):
    data = body.encode("utf-8") if body is not None else None
    req = urllib.request.Request(API + path, data=data, method=method,
                                 headers={"Content-Type": "text/plain; charset=utf-8"})
    try:
        with urllib.request.urlopen(req, timeout=timeout) as r:
            return r.read().decode("utf-8")
    except (urllib.error.URLError, ConnectionError, TimeoutError) as e:
        return json.dumps({"error": f"bot not reachable at {API} ({e}). Is the bot running with BotApiPort set?"})


def tail(path, lines, grep):
    if not os.path.exists(path):
        return f"no log at {path}"
    rx = re.compile(grep, re.I) if grep else None
    want = max(1, min(int(lines or 60), 500))
    out = []
    with open(path, "rb") as f:
        f.seek(0, os.SEEK_END)
        pos, buf = f.tell(), b""
        # read backwards in chunks until enough (matching) lines are collected or 8 MB were scanned
        scanned = 0
        while pos > 0 and len(out) < want and scanned < 8_000_000:
            step = min(65536, pos)
            pos -= step
            f.seek(pos)
            buf = f.read(step) + buf
            scanned += step
            parts = buf.split(b"\n")
            buf = parts[0]
            for raw in reversed(parts[1:]):
                line = raw.decode("utf-8", "replace").rstrip("\r")
                if line and (rx is None or rx.search(line)):
                    out.append(line)
                    if len(out) >= want:
                        break
    return "\n".join(reversed(out)) if out else "(no matching lines)"


def call(name, args):
    if name == "bot_status":
        return http("GET", "/status")
    if name == "bot_command":
        return http("POST", "/command", args.get("text", ""))
    if name == "bot_log":
        return tail(LOG, args.get("lines", 60), args.get("grep"))
    raise ValueError(f"unknown tool {name}")


def send(msg):
    sys.stdout.write(json.dumps(msg) + "\n")
    sys.stdout.flush()


def main():
    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue
        try:
            msg = json.loads(line)
        except json.JSONDecodeError:
            continue
        mid, method = msg.get("id"), msg.get("method")
        if mid is None:
            continue  # notifications (initialized, cancelled) need no answer
        try:
            if method == "initialize":
                pv = (msg.get("params") or {}).get("protocolVersion", "2024-11-05")
                result = {"protocolVersion": pv, "capabilities": {"tools": {}},
                          "serverInfo": {"name": "aobuddy", "version": "1.0"}}
            elif method == "tools/list":
                result = {"tools": TOOLS}
            elif method == "tools/call":
                p = msg.get("params") or {}
                text = call(p.get("name"), p.get("arguments") or {})
                result = {"content": [{"type": "text", "text": text}]}
            elif method == "ping":
                result = {}
            else:
                send({"jsonrpc": "2.0", "id": mid, "error": {"code": -32601, "message": f"no method {method}"}})
                continue
            send({"jsonrpc": "2.0", "id": mid, "result": result})
        except Exception as e:  # a tool error is reported to the caller, not a crash
            send({"jsonrpc": "2.0", "id": mid, "result": {"content": [{"type": "text", "text": f"error: {e}"}], "isError": True}})


if __name__ == "__main__":
    main()
