#!/bin/bash
# decode.sh <pcap>  ->  $SP/decoded/<tag>_s<N>.csv/.txt  (PcapDecode --ordered)
SP="${SP:-$(cd "$(dirname "$0")" && pwd)}"
TSHARK="/c/Program Files/Wireshark/tshark.exe"
BIN=/e/Funcom/OmniCell/Tools/Capture/bin
PCAP="$1"
STEM=$(basename "$PCAP" .pcapng); TAG=${STEM#marked-}; TAG=${TAG%%_*}
OUT=$SP/decoded
mkdir -p "$OUT"
streams=$("$TSHARK" -r "$PCAP" -T fields -e tcp.stream -Y "tcp.port>=7100 and tcp.port<=7999 and tcp.len>0" | tr -d '\r' | sort -n -u)
args=""
for s in $streams; do args="$args -z follow,tcp,raw,$s"; done
"$TSHARK" -r "$PCAP" -q $args > "$OUT/follow_$TAG.txt"
mkdir -p "$OUT/split_$TAG"
"$BIN/FollowToCsv.exe" "$OUT/follow_$TAG.txt" --split "$OUT/split_$TAG" "${TAG}_s"
for c in "$OUT/split_$TAG"/*.csv; do
  [ -f "$c" ] || continue
  n=$(basename "$c" .csv)
  sz=$(stat -c %s "$c")
  [ "$sz" -lt 2000 ] && continue
  cp "$c" "$OUT/$n.csv"
  "$BIN/PcapDecode.exe" "$OUT/$n.csv" "$BIN/SmokeLounge.AOtomation.Messaging.dll" --ordered > "$OUT/$n.txt" 2>&1
  echo "$n: $(wc -l < "$OUT/$n.txt") lines"
done
rm -f "$OUT/follow_$TAG.txt"
