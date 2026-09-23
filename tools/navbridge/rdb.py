import os,struct
class Rdb:
    def __init__(self,d):
        idx=open(os.path.join(d,'ResourceDatabase.idx'),'rb').read()
        self.partHeader=struct.unpack_from('<I',idx,12)[0]
        self.partSize=struct.unpack_from('<I',idx,184)[0]
        self.files=[]
        for n in sorted(os.listdir(d)):
            if n.lower().startswith('resourcedatabase.dat'):
                self.files.append(open(os.path.join(d,n),'rb'))
        self.off={}
        block=struct.unpack_from('<I',idx,72)[0]
        nxt=struct.unpack_from('<I',idx,block)[0]
        while nxt>0:
            cnt=struct.unpack_from('<h',idx,block+8)[0]
            e=block+28
            for i in range(cnt):
                hi,lo=struct.unpack_from('<II',idx,e)
                o=(hi<<32)|lo
                t=struct.unpack_from('>i',idx,e+8)[0]
                ins=struct.unpack_from('>i',idx,e+12)[0]
                self.off.setdefault(t,{})[ins]=o
                e+=16
            block=nxt; nxt=struct.unpack_from('<I',idx,block)[0]
    def readat(self,lo,n):
        out=b''; part=lo//self.partSize; pos=lo-part*(self.partSize-self.partHeader)
        while len(out)<n:
            f=self.files[part]; f.seek(pos); c=f.read(n-len(out))
            out+=c
            if len(out)<n:
                part+=1; pos=self.partHeader
        return out
    def header(self,t,i):
        return self.readat(self.off[t][i],34)
    def read(self,t,i):
        o=self.off[t][i]; h=self.readat(o,34)
        ln=struct.unpack_from('<i',h,18)[0]-12
        return h,self.readat(o+34,ln)
