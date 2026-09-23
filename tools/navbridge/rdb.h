// Minimal read-only reader for the classic AO client's ResourceDatabase.
// Port of OmniCell/Tools/AssetDecoder/AssetDecoder/Rdb/ResourceDatabase.cs.
// Opened with full sharing so the game client can have the same files open.
#pragma once
#include <windows.h>
#include <string>
#include <vector>
#include <map>
#include <stdio.h>

struct Rdb
{
    std::vector<HANDLE> parts;
    std::map<int, std::map<int, long long> > offsets;
    unsigned int partSize, partHeader;

    static unsigned int u32(const std::vector<unsigned char>& b, size_t at)
    { return b[at] | (b[at+1] << 8) | (b[at+2] << 16) | ((unsigned int)b[at+3] << 24); }
    static int be32(const std::vector<unsigned char>& b, size_t at)
    { return (b[at] << 24) | (b[at+1] << 16) | (b[at+2] << 8) | b[at+3]; }

    bool Open(const std::string& dir)
    {
        std::string ip = dir + "\\ResourceDatabase.idx";
        HANDLE h = CreateFileA(ip.c_str(), GENERIC_READ,
            FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, 0, OPEN_EXISTING, 0, 0);
        if (h == INVALID_HANDLE_VALUE) { printf("no idx at %s\n", ip.c_str()); return false; }
        DWORD sz = GetFileSize(h, 0);
        std::vector<unsigned char> idx(sz);
        DWORD got = 0;
        ReadFile(h, &idx[0], sz, &got, 0);
        CloseHandle(h);

        // .dat, then .dat.001, .dat.002 ... : one logical stream split into parts.
        for (int n = -1; ; ++n)
        {
            char name[MAX_PATH];
            if (n < 0) sprintf(name, "%s\\ResourceDatabase.dat", dir.c_str());
            else       sprintf(name, "%s\\ResourceDatabase.dat.%03d", dir.c_str(), n + 1);
            HANDLE f = CreateFileA(name, GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, 0, OPEN_EXISTING, 0, 0);
            if (f == INVALID_HANDLE_VALUE) break;
            parts.push_back(f);
        }
        if (parts.empty()) { printf("no .dat files in %s\n", dir.c_str()); return false; }

        partHeader = u32(idx, 12);
        partSize   = u32(idx, 184);

        // The index is a chain of blocks; each holds 16-byte entries of
        // (offset hi, offset lo, type, instance) - offsets LE, ids big-endian.
        unsigned int block = u32(idx, 72);
        unsigned int next  = u32(idx, block);
        while (next > 0)
        {
            int count = (short)(idx[block + 8] | (idx[block + 9] << 8));
            size_t e = block + 28;
            for (int i = 0; i < count; ++i, e += 16)
            {
                long long off = ((long long)u32(idx, e) << 32) | u32(idx, e + 4);
                offsets[be32(idx, e + 8)][be32(idx, e + 12)] = off;
            }
            block = next;
            next = u32(idx, block);
        }
        return true;
    }

    bool ReadAt(long long logical, size_t len, std::vector<unsigned char>& out)
    {
        out.resize(len);
        if (!len) return true;
        size_t part = (size_t)(logical / partSize);
        long long pos = logical - (long long)part * (partSize - partHeader);
        size_t filled = 0;
        while (filled < len)
        {
            if (part >= parts.size()) return false;
            LARGE_INTEGER li;
            li.QuadPart = pos;
            SetFilePointerEx(parts[part], li, 0, FILE_BEGIN);
            DWORD got = 0;
            if (!ReadFile(parts[part], &out[filled], (DWORD)(len - filled), &got, 0)) return false;
            filled += got;
            if (filled < len) { ++part; pos = partHeader; }
        }
        return true;
    }

    // The record as the client's DbObject readers appear to want it: the size field at
    // header offset 18 counts 12 header bytes BEFORE the payload, and those 12 bytes are
    // (type, instance, flag) at header offset 22. A DbObject reader checks the instance
    // in the data against the identity it was constructed with, so it needs them.
    bool ReadFramed(int type, int instance, std::vector<unsigned char>& out)
    {
        std::map<int, std::map<int, long long> >::iterator t = offsets.find(type);
        if (t == offsets.end()) return false;
        std::map<int, long long>::iterator i = t->second.find(instance);
        if (i == t->second.end()) return false;
        std::vector<unsigned char> hdr;
        if (!ReadAt(i->second, 34, hdr)) return false;
        int len = (int)u32(hdr, 18);
        if (len < 12 || len > 64 * 1024 * 1024) return false;
        return ReadAt(i->second + 22, len, out);
    }

    // The record payload, byte for byte. The 34-byte header's size field counts
    // 12 header bytes that precede the payload.
    bool Read(int type, int instance, std::vector<unsigned char>& out)
    {
        std::map<int, std::map<int, long long> >::iterator t = offsets.find(type);
        if (t == offsets.end()) return false;
        std::map<int, long long>::iterator i = t->second.find(instance);
        if (i == t->second.end()) return false;
        std::vector<unsigned char> hdr;
        if (!ReadAt(i->second, 34, hdr)) return false;
        int len = (int)u32(hdr, 18) - 12;
        if (len < 0 || len > 64 * 1024 * 1024) return false;
        return ReadAt(i->second + 34, len, out);
    }
};
