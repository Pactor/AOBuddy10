# timed.py <pcap> <stream> <out.tsv>
# One line per AO message: epoch  dir(S/C)  seq  n3type-name  hex
# Server: first 16 bytes plaintext then one zlib stream (flush framing). Client: plaintext, 4-byte padded.
import sys, subprocess, zlib, re, struct

TSHARK = r"C:\Program Files\Wireshark\tshark.exe"
ENUM = r"E:\Funcom\OmniCell\OmniCell\Libraries\Source\AOtomation.Messaging\SmokeLounge.AOtomation.Messaging\Messages\N3MessageType.cs"

names = {}
for m in re.finditer(r"(\w+)\s*=\s*0x([0-9A-Fa-f]+)", open(ENUM, encoding="latin-1").read()):
    names[int(m.group(2), 16)] = m.group(1)

pcap, stream, out = sys.argv[1], sys.argv[2], sys.argv[3]
rows = subprocess.run([TSHARK, "-r", pcap, "-2", "-Y",
                       f"tcp.stream=={stream} and tcp.len>0 and !tcp.analysis.retransmission",
                       "-T", "fields", "-e", "frame.time_epoch", "-e", "tcp.srcport", "-e", "tcp.payload"],
                      capture_output=True, text=True).stdout.splitlines()

def is_server(port): return 7100 <= int(port) <= 7999

class Dir:
    def __init__(s, server):
        s.server, s.buf, s.raw, s.z, s.pending = server, b"", b"", None, []
    def feed(s, t, data):
        if s.server:
            s.raw += data
            if s.z is None:
                if len(s.raw) < 16: return
                s.buf += s.raw[:16]; s.raw = s.raw[16:]
                s.z = zlib.decompressobj()
            try:
                s.buf += s.z.decompress(s.raw)
            except zlib.error:
                pass
            s.raw = b""
        else:
            s.buf += data
        while len(s.buf) >= 8:
            size = struct.unpack(">H", s.buf[6:8])[0]
            if size < 16: s.buf = s.buf[1:]; continue
            if not s.server and size % 4: plen = size + (4 - size % 4)
            else: plen = size
            if len(s.buf) < size: return
            pkt = s.buf[:size]
            s.buf = s.buf[plen:] if len(s.buf) >= plen else s.buf[size:]
            seq = struct.unpack(">H", pkt[0:2])[0]
            mid = struct.unpack(">I", pkt[16:20])[0] if len(pkt) >= 20 else 0
            s.pending.append((t, "S" if s.server else "C", seq, names.get(mid, hex(mid)), pkt.hex()))

S, C = Dir(True), Dir(False)
for r in rows:
    p = r.split("\t")
    if len(p) < 3 or not p[2]: continue
    (S if is_server(p[1]) else C).feed(float(p[0]), bytes.fromhex(p[2].replace(":", "")))
allm = sorted(S.pending + C.pending, key=lambda x: x[0])
with open(out, "w") as f:
    for m in allm: f.write(f"{m[0]:.3f}\t{m[1]}\t{m[2]}\t{m[3]}\t{m[4]}\n")
print(f"{len(S.pending)} server, {len(C.pending)} client -> {out}")
