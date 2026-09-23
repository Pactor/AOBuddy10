import sys,time
for line in open(sys.argv[1]):
    t,d,seq,name,h=line.rstrip("\n").split("\t")
    if name!="CharacterAction": continue
    b=bytes.fromhex(h)
    # header16, id4, identity8, unk1, action4, unk4, target8, p1 4, p2 4
    o=16+4+8+1
    act=int.from_bytes(b[o:o+4],'big'); o+=8
    tt=b[o:o+4].hex(); ti=b[o+4:o+8].hex(); o+=8
    p1=int.from_bytes(b[o:o+4],'big',signed=True); p2=int.from_bytes(b[o+4:o+8],'big',signed=True)
    who=b[24:28].hex()
    if act in (0x69,): continue
    print(time.strftime("%H:%M:%S",time.localtime(float(t)))+t[10:14], d, seq, "who="+who, "act=%d(0x%x)"%(act,act), "tgt=%s:%s"%(tt,ti), "p1=%d p2=%d"%(p1,p2))
