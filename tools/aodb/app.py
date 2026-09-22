#!/usr/bin/env python3
"""
AODB - Anarchy Online character reference web app (AOBuddy10).

Data-driven: reads the JSON the profile/reference agents generate under
  AOBuddy/GameData/profiles\\
and renders a browsable UI (pick a profession -> breeds it can be, skill caps,
implants, equippable weapons + their specials, and every castable nano).

Nothing here is invented: the profession id/name list is the verified
AOSharp.Common Profession enum; everything else comes from the JSON data files,
and sections that have no data yet show as "not generated yet" until an agent
writes them. Zero third-party dependencies - Python 3 standard library only.

Run:  aodb.bat              (serves http://localhost:8888)
      aodb.bat 9000         (or any other port, when 8888 is taken)
      python app.py 9000    (the same thing without the batch file)

The port may also be set with the AODB_PORT environment variable; an argument wins over it.
"""

import http.server
import socketserver
import json
import os
import sys
import urllib.parse

DEFAULT_PORT = 8888


def _port():
    """Port to serve on: the first command-line argument, else AODB_PORT, else the default.

    A bad value is worth stopping for rather than silently falling back — being handed a port and
    quietly serving on a different one is how you end up reading a stale page for ten minutes."""
    raw = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("AODB_PORT", "")
    if not raw:
        return DEFAULT_PORT
    try:
        port = int(raw)
    except ValueError:
        raise SystemExit(f"AODB: '{raw}' is not a port number.")
    if not 1 <= port <= 65535:
        raise SystemExit(f"AODB: port {port} is out of range (1-65535).")
    return port
BASE = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))),
                    "AOBuddy", "GameData", "profiles")
REF = os.path.join(BASE, "reference")

# Verified from AOSharp.Common/GameData/Profession.cs (13 = Monster is not playable).
PROFESSIONS = [
    {"id": 1,  "name": "Soldier",        "slug": "soldier"},
    {"id": 2,  "name": "Martial Artist", "slug": "martialartist"},
    {"id": 3,  "name": "Engineer",       "slug": "engineer"},
    {"id": 4,  "name": "Fixer",          "slug": "fixer"},
    {"id": 5,  "name": "Agent",          "slug": "agent"},
    {"id": 6,  "name": "Adventurer",     "slug": "adventurer"},
    {"id": 7,  "name": "Trader",         "slug": "trader"},
    {"id": 8,  "name": "Bureaucrat",     "slug": "bureaucrat"},
    {"id": 9,  "name": "Enforcer",       "slug": "enforcer"},
    {"id": 10, "name": "Doctor",         "slug": "doctor"},
    {"id": 11, "name": "Nano-Technician","slug": "nanotechnician"},
    {"id": 12, "name": "Meta-Physicist", "slug": "metaphysicist"},
    {"id": 14, "name": "Keeper",         "slug": "keeper"},
    {"id": 15, "name": "Shade",          "slug": "shade"},
]


def load_json(path):
    # utf-8-sig tolerates a UTF-8 BOM (some generated files carry one).
    try:
        with open(path, "r", encoding="utf-8-sig") as f:
            return json.load(f)
    except Exception:
        return None


def profile_for(slug):
    """Merge the per-profession data files: nanos, weapons, their acquisition-source
    companions, and the class-specific recommended implant build."""
    out = {"slug": slug}
    parts = (
        ("nanos", "-nanos.json"),
        ("weapons", "-weapons.json"),
        ("nanoSources", "-nano-sources.json"),
        ("weaponSources", "-weapon-sources.json"),
        ("implantBuild", "-implants.json"),
        ("symbiants", "-symbiants.json"),
        ("build", "-build.json"),
        ("pets", "-pets.json"),
        ("gear", "-gear.json"),
        ("buffs", "-buffs.json"),
        ("leveling", "-leveling.json"),
        ("endgame", "-endgame.json"),
        ("weaponsEndgame", "-weapons-endgame.json"),
    )
    for key, suffix in parts:
        out[key] = load_json(os.path.join(BASE, slug + suffix))
    return out


def status():
    files = {
        "breeds": os.path.join(REF, "breeds.json"),
        "professions": os.path.join(REF, "professions.json"),
        "skillcaps": os.path.join(REF, "skillcaps.json"),
        "implants": os.path.join(REF, "implants.json"),
    }
    st = {k: os.path.exists(v) for k, v in files.items()}
    st["profiles"] = {}
    for p in PROFESSIONS:
        st["profiles"][p["slug"]] = {
            "nanos": os.path.exists(os.path.join(BASE, p["slug"] + "-nanos.json")),
            "weapons": os.path.exists(os.path.join(BASE, p["slug"] + "-weapons.json")),
        }
    return st


INDEX_HTML = r"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>AODB - AO Character Reference</title>
<style>
  :root{--bg:#0f1216;--panel:#171b21;--panel2:#1e242c;--line:#2a323c;--fg:#e6edf3;--mut:#8b98a5;--acc:#4aa3df;--acc2:#7ee081;--warn:#e0b64a;}
  *{box-sizing:border-box}
  body{margin:0;font:14px/1.5 -apple-system,Segoe UI,Roboto,sans-serif;background:var(--bg);color:var(--fg)}
  header{padding:14px 18px;border-bottom:1px solid var(--line);display:flex;align-items:center;gap:12px}
  header h1{font-size:16px;margin:0;letter-spacing:.5px}
  header .sub{color:var(--mut);font-size:12px}
  .wrap{display:flex;min-height:calc(100vh - 52px)}
  nav{width:210px;border-right:1px solid var(--line);padding:10px;flex:none;background:var(--panel)}
  nav .p{padding:8px 10px;border-radius:7px;cursor:pointer;display:flex;justify-content:space-between;align-items:center;color:var(--fg)}
  nav .p:hover{background:var(--panel2)}
  nav .p.active{background:var(--acc);color:#04121f;font-weight:600}
  nav .p .id{color:var(--mut);font-size:11px}
  nav .p.active .id{color:#04121f}
  main{flex:1;padding:18px 22px;max-width:1100px}
  h2{margin:0 0 4px}
  .role{color:var(--mut);margin-bottom:14px}
  .tabs{display:flex;gap:6px;border-bottom:1px solid var(--line);margin-bottom:16px;flex-wrap:wrap}
  .tab{padding:8px 14px;cursor:pointer;border-bottom:2px solid transparent;color:var(--mut)}
  .tab:hover{color:var(--fg)}
  .tab.active{color:var(--fg);border-bottom-color:var(--acc)}
  .card{background:var(--panel);border:1px solid var(--line);border-radius:10px;padding:14px 16px;margin-bottom:14px}
  .badge{display:inline-block;background:var(--panel2);border:1px solid var(--line);border-radius:20px;padding:3px 11px;margin:3px 4px 3px 0;font-size:12px}
  .badge.on{border-color:var(--acc2);color:var(--acc2)}
  .badge.off{color:var(--mut);opacity:.6}
  table{border-collapse:collapse;width:100%;font-size:13px}
  th,td{text-align:left;padding:6px 10px;border-bottom:1px solid var(--line);vertical-align:top}
  th{color:var(--mut);font-weight:600;position:sticky;top:0;background:var(--panel)}
  .pending{color:var(--warn);background:rgba(224,182,74,.08);border:1px dashed var(--warn);border-radius:8px;padding:12px 14px}
  .muted{color:var(--mut)}
  .grp{font-weight:600;color:var(--acc);margin:16px 0 6px;font-size:13px;text-transform:uppercase;letter-spacing:.5px}
  input.search{width:100%;padding:8px 10px;background:var(--panel2);border:1px solid var(--line);border-radius:7px;color:var(--fg);margin-bottom:10px}
  .pill{font-size:11px;color:var(--mut)}
  a{color:var(--acc)}
</style>
</head>
<body>
<header>
  <h1 style="cursor:pointer" onclick="home()">AODB</h1><span class="sub">Anarchy Online character reference &middot; AOBuddy10</span>
  <span style="flex:1"></span>
  <button id="teambtn" onclick="showTeam()" style="background:var(--panel2);border:1px solid var(--line);color:var(--fg);border-radius:7px;padding:7px 12px;cursor:pointer;font-size:13px">&#129518; Team Level Range</button>
</header>
<div class="wrap">
  <nav id="nav"></nav>
  <main id="main"><p class="muted">Pick a profession on the left.</p></main>
</div>
<script>
let STATUS=null, BREEDS=null, PROFS=null, SKILLCAPS=null, IMPLANTS=null;
let cur=null, curTab='overview', curProfile=null;

async function j(u){ try{const r=await fetch(u); if(!r.ok) return null; return await r.json();}catch(e){return null;} }

async function boot(){
  const pr = await j('/api/professions');
  PROFS = (pr && pr.list) ? pr.list : [];
  window.__profmeta = (pr && pr.meta) ? pr.meta : {};
  STATUS = await j('/api/status');
  BREEDS = await j('/api/breeds');
  SKILLCAPS = await j('/api/skillcaps');
  IMPLANTS = await j('/api/implants');
  const nav=document.getElementById('nav');
  nav.innerHTML='';
  PROFS.forEach(p=>{
    const d=document.createElement('div'); d.className='p'; d.dataset.slug=p.slug;
    d.innerHTML=`<span>${p.name}</span><span class="id">#${p.id}</span>`;
    d.onclick=()=>select(p); nav.appendChild(d);
  });
}

async function select(p){
  cur=p; curTab='overview';
  document.querySelectorAll('nav .p').forEach(e=>e.classList.toggle('active', e.dataset.slug===p.slug));
  // Lazily (re)fetch shared reference data so a page opened before the data existed self-heals.
  if(!BREEDS) BREEDS = await j('/api/breeds');
  if(!SKILLCAPS) SKILLCAPS = await j('/api/skillcaps');
  if(!IMPLANTS) IMPLANTS = await j('/api/implants');
  curProfile = await j('/api/profile?prof='+encodeURIComponent(p.slug));
  render();
}

function tabList(){
  return [['overview','Overview'],['build','Build'],['skills','Skill Caps'],['pets','Pets'],['nanos','Nanos'],['weapons','Weapons'],['implants','Implants & Symbiants'],['gear','Gear'],['buffs','Buffs'],['leveling','Leveling'],['endgame','Endgame']];
}
function render(){
  const m=document.getElementById('main');
  if(!cur){m.innerHTML='<p class="muted">Pick a profession.</p>';return;}
  const profMeta = (PROFS_META()||{})[String(cur.id)] || {};
  let h=`<h2>${cur.name} <span class="pill">#${cur.id}</span></h2>`;
  h+=`<div class="role">${profMeta.role||''}</div>`;
  h+='<div class="tabs">'+tabList().map(([k,l])=>`<div class="tab ${k===curTab?'active':''}" onclick="setTab('${k}')">${l}</div>`).join('')+'</div>';
  h+='<div id="tabbody"></div>';
  m.innerHTML=h; renderTab();
}
function PROFS_META(){ return window.__profmeta||{}; }
function setTab(t){curTab=t; renderTab();}

function pending(what){ return `<div class="pending">The <b>${what}</b> data for ${cur.name} has not been generated yet. A background research pass writes it into the profiles folder; refresh once it lands.</div>`; }
function pendingInline(what){ return `<div class="pending" style="margin-bottom:14px">${what} for ${cur.name} is still being generated &mdash; refresh once the pass lands.</div>`; }

function renderTab(){
  const b=document.getElementById('tabbody'); if(!b)return;
  if(curTab==='overview') return b.innerHTML=renderOverview();
  if(curTab==='build')    return b.innerHTML=renderBuild();
  if(curTab==='skills')   return b.innerHTML=renderSkills();
  if(curTab==='pets')     return b.innerHTML=renderPets();
  if(curTab==='implants') return b.innerHTML=renderImplants();
  if(curTab==='weapons')  return b.innerHTML=renderWeapons();
  if(curTab==='nanos')    return b.innerHTML=renderNanos();
  if(curTab==='gear')     return b.innerHTML=renderGearTab();
  if(curTab==='buffs')    return b.innerHTML=renderBuffs();
  if(curTab==='leveling') return b.innerHTML=renderLeveling();
  if(curTab==='endgame')  return b.innerHTML=renderEndgame();
}

function renderBuffs(){
  const d = curProfile && curProfile.buffs;
  if(!d) return pending('buffs');
  let h='';
  const tbl=(title,arr,cols)=>{ if(!arr||!arr.length) return ''; let s=`<div class="grp">${title}</div><div class="card" style="padding:0"><table><tr>${cols.map(c=>`<th>${c[1]}</th>`).join('')}</tr>`;
    arr.forEach(x=>{ s+='<tr>'+cols.map(c=>`<td class="${c[0]==='name'||c[0]==='buff'?'':'muted'}">${(x[c[0]]??'')}</td>`).join('')+'</tr>'; }); return s+'</table></div>'; };
  h+=tbl('Wanted from other professions (twink / raid)', d.wantedFromOthers, [['buff','Buff'],['fromProfession','From'],['effect','Effect'],['useCase','Use']]);
  h+=tbl('Self-buffs (keep up)', d.selfBuffs, [['name','Buff'],['effect','Effect'],['note','Note'],['id','id']]);
  h+=tbl('General / consumable buffs', d.generalBuffs, [['name','Buff'],['effect','Effect'],['source','Source']]);
  h+=tbl('Buffs this class gives others', d.givesToOthers, [['name','Buff'],['effect','Effect'],['wantedBy','Wanted by'],['id','id']]);
  return h||pending('buffs');
}

function renderLeveling(){
  const d = curProfile && curProfile.leveling;
  if(!d) return pending('leveling');
  let h='';
  if(d.froobVsPaid) h+=`<div class="card"><div class="grp">Froob vs paid</div><p class="muted">${d.froobVsPaid}</p></div>`;
  if(d.brackets){ h+='<div class="grp">Level path</div><div class="card" style="padding:0"><table><tr><th>Levels</th><th>Where</th><th>Fight</th><th>Pet notes</th><th>XP</th></tr>'+
    (d.brackets||[]).map(b=>`<tr><td><b>${b.levelRange}</b></td><td>${Array.isArray(b.where)?b.where.join(', '):(b.where||'')}</td><td class="muted">${b.fight||''}</td><td class="muted">${b.petNotes||''}</td><td class="muted">${b.xpType||''}</td></tr>`).join('')+'</table></div>'; }
  if(d.keyQuests){ h+='<div class="grp">Key quests</div><div class="card" style="padding:0"><table><tr><th>Quest</th><th>Where</th><th>Reward</th><th>Level</th></tr>'+
    (d.keyQuests||[]).map(q=>`<tr><td>${q.name}</td><td class="muted">${q.where||''}</td><td class="muted">${q.reward||''}</td><td>${q.level||''}</td></tr>`).join('')+'</table></div>'; }
  if(d.keys){ h+='<div class="grp">Keys</div><div class="card" style="padding:0"><table><tr><th>Key</th><th>For</th><th>How</th></tr>'+
    (d.keys||[]).map(k=>`<tr><td>${k.name}</td><td class="muted">${k.forWhat||''}</td><td class="muted">${k.how||''}</td></tr>`).join('')+'</table></div>'; }
  if(d.tips){ h+='<div class="card"><div class="grp">Tips</div><ul>'+(d.tips||[]).map(t=>`<li class="muted">${t}</li>`).join('')+'</ul></div>'; }
  return h;
}

function renderEndgame(){
  const d = curProfile && curProfile.endgame;
  if(!d) return pending('endgame');
  let h='';
  if(d.content){ h+='<div class="grp">Content &amp; raids</div><div class="card" style="padding:0"><table><tr><th>Content</th><th>Type</th><th>Lvl</th><th>Drops for this class</th><th>Role</th></tr>'+
    (d.content||[]).map(c=>`<tr><td><b>${c.name}</b></td><td class="muted">${c.type||''}</td><td>${c.level||''}</td><td class="muted">${c.whatDropsForMP||c.whatDrops||''}</td><td class="muted">${c.mpRole||c.role||''}</td></tr>`).join('')+'</table></div>'; }
  if(d.progressionOrder){ h+='<div class="grp">Progression order</div><div class="card" style="padding:0"><table><tr><th>#</th><th>What</th><th>Why</th></tr>'+
    (d.progressionOrder||[]).map((s,i)=>`<tr><td>${i+1}</td><td>${s.what||s.step||''}</td><td class="muted">${s.why||''}</td></tr>`).join('')+'</table></div>'; }
  if(d.notes){ h+='<div class="card"><div class="grp">Notes</div><ul>'+(d.notes||[]).map(n=>`<li class="muted">${n}</li>`).join('')+'</ul></div>'; }
  return h;
}

function renderBuild(){
  const d = curProfile && curProfile.build;
  if(!d) return pending('build (breed / perks / research / playstyle)');
  let h='';
  if(d.playstyle){ const p=d.playstyle;
    h+='<div class="card"><div class="grp">Playstyle &amp; role</div>';
    if(p.role) h+=`<p><b>${p.role}</b></p>`;
    if(p.summary) h+=`<p class="muted">${p.summary}</p>`;
    if(p.strengths) h+=`<div><b>Strengths:</b> ${(p.strengths||[]).join(', ')}</div>`;
    if(p.weaknesses) h+=`<div><b>Weaknesses:</b> ${(p.weaknesses||[]).join(', ')}</div>`;
    if(p.tips) h+='<ul>'+(p.tips||[]).map(t=>`<li class="muted">${t}</li>`).join('')+'</ul>';
    h+='</div>';
  }
  if(d.breed){ const br=d.breed;
    h+='<div class="card"><div class="grp">Breed choice</div>';
    h+=`<p>Recommended: <b style="color:var(--acc2)">${br.recommended||''}</b> <span class="muted">${br.reasoning||''}</span></p>`;
    if(br.alternatives) h+='<div class="muted">Alternatives: '+(br.alternatives||[]).map(a=>`${a.breed}${a.note?` (${a.note})`:''}`).join('; ')+'</div>';
    h+='</div>';
  }
  if(d.skillPriorities){ h+='<div class="card"><div class="grp">Skill priorities</div><table><tr><th>Skill</th><th>Target</th><th>Why</th></tr>'+
    (d.skillPriorities||[]).map(s=>`<tr><td>${s.skill}</td><td>${s.target||''}</td><td class="muted">${s.why||''}</td></tr>`).join('')+'</table></div>'; }
  if(d.perks){ h+='<div class="grp">Perks</div><div class="card" style="padding:0"><table><tr><th>Perk</th><th>Type</th><th>Line</th><th>Effect</th><th>When</th><th>Pri</th></tr>'+
    (d.perks||[]).map(p=>`<tr><td>${p.name}</td><td class="muted">${p.type||''}</td><td class="muted">${p.line||''}</td><td class="muted">${p.effect||''}</td><td>${p.recommendedAtLevel||''}</td><td>${p.priority||''}</td></tr>`).join('')+'</table></div>'; }
  if(d.research){ h+='<div class="grp">Research</div><div class="card" style="padding:0"><table><tr><th>Research</th><th>Track</th><th>Effect</th></tr>'+
    (d.research||[]).map(r=>`<tr><td>${r.name}</td><td class="muted">${r.track||''}</td><td class="muted">${r.effect||''}</td></tr>`).join('')+'</table></div>'; }
  return h;
}

function renderPets(){
  const d = curProfile && curProfile.pets;
  if(!d) return pending('pet');
  let h='';
  if(d.summary) h+=`<div class="card muted">${d.summary}</div>`;
  (d.petTypes||[]).forEach(pt=>{
    h+=`<div class="grp">${pt.role} pet <span class="pill">${pt.nanoLine||''}</span></div>`;
    if(pt.description) h+=`<p class="muted" style="margin:0 0 6px">${pt.description}</p>`;
    if(pt.mechanics) h+=`<p class="muted" style="margin:0 0 6px">${pt.mechanics}</p>`;
    const prog=pt.progression||[];
    if(prog.length){ h+='<div class="card" style="padding:0"><table><tr><th>Summon</th><th>Lvl</th><th>Note</th><th>id</th></tr>'+
      prog.map(x=>`<tr><td>${x.nano}</td><td>${x.minLevel??''}</td><td class="muted">${x.note||''}</td><td class="muted">${x.id??''}</td></tr>`).join('')+'</table></div>'; }
  });
  if(d.petBuffs){ h+='<div class="grp">Pet buffs</div><div class="card" style="padding:0"><table><tr><th>Buff</th><th>Effect</th><th>id</th></tr>'+
    (d.petBuffs||[]).map(b=>`<tr><td>${b.name}</td><td class="muted">${b.effect||''}</td><td class="muted">${b.id??''}</td></tr>`).join('')+'</table></div>'; }
  if(d.commands){ h+='<div class="card"><div class="grp">Commands</div>'+(d.commands||[]).map(c=>`<div><b>${c.command}</b> <span class="muted">${c.what||''}</span></div>`).join('')+'</div>'; }
  if(d.mechanics){ h+='<div class="card"><div class="grp">Mechanics</div><ul>'+(d.mechanics||[]).map(m=>`<li class="muted">${m}</li>`).join('')+'</ul></div>'; }
  return h;
}

function renderGearTab(){
  const d = curProfile && curProfile.gear;
  if(!d) return pending('gear / armor');
  let h='';
  if(d.summary) h+=`<div class="card muted">${d.summary}</div>`;
  if(d.armorSets){ h+='<div class="grp">Armor sets</div><div class="card" style="padding:0"><table><tr><th>Set</th><th>Slots</th><th>Key bonuses</th><th>Level</th><th>Source</th><th>Pri</th></tr>'+
    (d.armorSets||[]).map(a=>{ const kb=a.keyBonuses?Object.entries(a.keyBonuses).map(([k,v])=>`${k}: ${v}`).join(', '):''; return `<tr><td><b>${a.name}</b></td><td class="muted">${(a.slotsCovered||[]).join(', ')}</td><td class="muted" style="max-width:240px">${kb}</td><td>${a.levelRange||''}</td><td class="muted">${a.source||''}</td><td>${a.priority||''}</td></tr>`; }).join('')+'</table></div>'; }
  const grp=(title,arr,cols)=>{ if(!arr||!arr.length) return ''; let s=`<div class="grp">${title}</div><div class="card" style="padding:0"><table><tr>${cols.map(c=>`<th>${c[1]}</th>`).join('')}</tr>`;
    arr.forEach(x=>{ s+='<tr>'+cols.map(c=>`<td class="${c[0]==='name'?'':'muted'}">${x[c[0]]??''}</td>`).join('')+'</tr>'; }); return s+'</table></div>'; };
  h+=grp('Headwear', d.headwear, [['name','Item'],['effect','Effect'],['id','id']]);
  h+=grp('NCU &amp; belt', d.ncuAndBelt, [['name','Item'],['effect','Effect'],['id','id']]);
  h+=grp('Misc / utility', d.misc, [['name','Item'],['slot','Slot'],['effect','Effect'],['id','id']]);
  return h;
}

function renderOverview(){
  let h='';
  // Breeds this profession can be
  h+='<div class="card"><div class="grp">Breeds available</div>';
  const avail = professionBreeds(cur.id);
  if(avail){ h+=['Solitus','Opifex','Nanomage','Atrox'].map(br=>`<span class="badge ${avail.includes(br)?'on':'off'}">${avail.includes(br)?'&#10003; ':''}${br}</span>`).join(''); }
  else h+='<span class="muted">breed availability not generated yet</span>';
  h+='</div>';
  // Breed stat caps quick table
  if(BREEDS){
    h+='<div class="card"><div class="grp">Breed ability caps</div>'+breedTable()+'</div>';
  }
  return h;
}
function professionBreeds(id){
  const meta=PROFS_META()[String(id)];
  if(meta && meta.allowedBreeds) return meta.allowedBreeds;
  return null;
}
function breedTable(){
  const abils=['Strength','Agility','Stamina','Intelligence','Sense','Psychic'];
  const list = BREEDS.breeds||BREEDS;
  let rows = Array.isArray(list)?list:Object.values(list);
  let h='<table><tr><th>Breed</th>'+abils.map(a=>`<th>${a}</th>`).join('')+'<th>Traits</th></tr>';
  rows.forEach(br=>{
    const caps=br.trainedAbilityCap||br.caps||br.abilityCaps||{};
    const max=br.maxAbility220Unverified||{};
    const traits=Array.isArray(br.traits)?br.traits.join('; '):(br.notes||'');
    h+=`<tr><td><b>${br.name||br.breed}</b></td>`+abils.map(a=>{
      const c=caps[a]??caps[a.toLowerCase()], m=max[a];
      return `<td>${c??'<span class=muted>?</span>'}${m?` <span class="pill">/${m}*</span>`:''}</td>`;
    }).join('')+`<td class="muted" style="max-width:260px">${traits}</td></tr>`;
  });
  h+='</table><p class="muted" style="font-size:12px">Value = trained ability cap; <span class="pill">/N*</span> = est. max at 220 (unverified).</p>';
  return h;
}

function normp(s){return (s||'').toLowerCase().replace(/[^a-z0-9]/g,'');}
function renderSkills(){
  if(!SKILLCAPS) return pending('skill cap');
  const order = SKILLCAPS._professionOrder||[];
  const idx = order.findIndex(n=>normp(n)===normp(cur.slug)||normp(n)===normp(cur.name));
  if(idx<0) return pending('skill cap');
  const skills = SKILLCAPS.skills||[];
  const cats={};
  skills.forEach(sk=>{ const c=sk.category||'Other'; (cats[c]=cats[c]||[]).push(sk); });
  let h='<div class="card"><input class="search" placeholder="filter skills..." oninput="filt(this,\'skrow\')">';
  h+=`<p class="muted" style="font-size:12px">Skill cost factor for <b>${cur.name}</b> — lower = cheaper to raise (the profession’s cheap/“green” skills, highlighted). ${SKILLCAPS._costFieldNote||''}</p></div>`;
  Object.entries(cats).forEach(([cat,arr])=>{
    h+=`<div class="grp">${cat}</div><div class="card" style="padding:0"><table><tr><th>Skill</th><th>Cost</th><th>Depends on</th></tr>`;
    arr.forEach(sk=>{
      const c=Array.isArray(sk.cost)?sk.cost[idx]:(sk.cost??'?');
      const cheap=(typeof c==='number' && c<=1.5);
      h+=`<tr class="skrow"><td>${sk.name}</td><td style="${cheap?'color:var(--acc2);font-weight:600':''}">${c??'<span class=muted>?</span>'}</td><td class="muted">${sk.dependency||''}</td></tr>`;
    });
    h+='</table></div>';
  });
  return h;
}

function renderImplants(){
  let h='';
  // Symbiants first — for an endgame character these are the best-in-slot fillers.
  const sy = curProfile && curProfile.symbiants;
  if(sy){
    const rows = sy.bestInSlot||sy.slots||sy.build||[];
    h+=`<div class="card"><div class="grp">Best-in-slot symbiants (endgame ${cur.name})</div>`;
    if(sy.summary) h+=`<p class="muted">${sy.summary}</p>`;
    h+='<table><tr><th>Slot</th><th>Symbiant</th><th>Line/Unit</th><th>QL</th><th>Key bonuses</th><th>Requires</th><th>Where</th></tr>';
    rows.forEach(s=>{
      const kb=s.keyBonuses?Object.entries(s.keyBonuses).map(([k,v])=>`${k} +${v}`).join(', '):'';
      const rq=s.reqs?Object.entries(s.reqs).map(([k,v])=>`${k} ${v}`).join(', '):'';
      const lu=[s.line,s.unit].filter(Boolean).join(' / ');
      h+=`<tr><td><b>${s.slotLabel||s.slot}</b></td><td style="color:var(--acc2)">${s.symbiant||'-'}</td><td class="muted">${lu}</td><td>${s.ql??''}</td><td class="muted" style="max-width:220px">${kb}</td><td class="muted">${rq}</td><td class="muted">${s.source||''}</td></tr>`;
    });
    h+='</table>';
    if(sy.xanAlphaNote) h+=`<p class="muted" style="font-size:12px">${sy.xanAlphaNote}</p>`;
    h+='</div>';
  } else {
    h+=pendingInline('Best-in-slot symbiants (the top-tier pick)');
  }
  // Cluster-implant build (leveling / alternative) next.
  const build = curProfile && curProfile.implantBuild;
  if(build){
    const rows = build.build||build.slots||build;
    h+=`<div class="card"><div class="grp">Recommended ${cur.name} implant build (top-of-the-line)</div>`;
    if(build.summary) h+=`<p class="muted">${build.summary}</p>`;
    h+='<table><tr><th>Slot</th><th>Shiny</th><th>Bright</th><th>Faded</th><th>QL/notes</th></tr>';
    (Array.isArray(rows)?rows:Object.entries(rows).map(([k,v])=>Object.assign({slot:k},v))).forEach(s=>{
      const cl=x=>{ if(!x) return '<span class=muted>-</span>'; if(typeof x==='object') return (x.skill||x.cluster||'')+(x.note?` <span class="pill">${x.note}</span>`:''); return x; };
      h+=`<tr><td><b>${s.slotLabel||s.label||s.slot}</b></td><td style="color:var(--acc2)">${cl(s.shiny)}</td><td style="color:var(--acc2)">${cl(s.bright)}</td><td style="color:var(--acc2)">${cl(s.faded)}</td><td class="muted">${s.ql||s.notes||''}</td></tr>`;
    });
    h+='</table>';
    if(build.ladder) h+=`<p class="muted" style="font-size:12px">${typeof build.ladder==='string'?build.ladder:JSON.stringify(build.ladder)}</p>`;
    h+='</div>';
  } else {
    h+=pendingInline('a class-specific implant build');
  }
  // Generic cluster reference (same for every class) below.
  if(!IMPLANTS) return h+pending('implant');
  const slots = IMPLANTS.slots||IMPLANTS;
  let list = Array.isArray(slots)?slots:Object.entries(slots).map(([k,v])=>Object.assign({slot:k},v));
  h+='<div class="card"><div class="grp">Cluster reference (all classes) &mdash; which skills each slot can hold</div>';
  h+='<table><tr><th>Slot</th><th>Shiny</th><th>Bright</th><th>Faded</th></tr>';
  list.forEach(s=>{ h+=`<tr><td><b>${s.label||s.slot||s.name}</b></td><td>${clu(s.shiny)}</td><td>${clu(s.bright)}</td><td>${clu(s.faded)}</td></tr>`; });
  h+='</table></div>';
  const eq=IMPLANTS.equipRequirementModel;
  if(eq){
    h+='<div class="card"><div class="grp">Equip requirements</div>';
    h+= (typeof eq==='object') ? Object.entries(eq).map(([k,v])=>`<div><b>${k}:</b> <span class="muted">${typeof v==='object'?JSON.stringify(v):v}</span></div>`).join('') : `<p class="muted">${eq}</p>`;
    h+='</div>';
  }
  return h;
}
function clu(x){ if(!x) return '<span class=muted>-</span>'; return Array.isArray(x)?x.join(', '):x; }

// Acquisition-source lookups (companion files keyed by item/nano id).
function srcLookup(store,id){ if(!store||id==null) return null; const s=store.sources||store; return s[String(id)]||s[id]||null; }
function wsrc(id){ return srcLookup(curProfile&&curProfile.weaponSources, id); }
function nsrc(id){ return srcLookup(curProfile&&curProfile.nanoSources, id); }
function srcText(o){
  if(!o) return '';
  if(typeof o==='string') return o;
  const how=o.how||o.method||o.acquisition||''; const where=o.where||o.location||o.from||'';
  let s=[how,where].filter(Boolean).join(' — ');
  const tags=[];
  if(o.expansion){ tags.push(/shadow/i.test(o.expansion)?'SL':o.expansion); }
  if(o.tier) tags.push('tier '+o.tier);
  return s+(tags.length?' '+tags.map(t=>`<span class="pill" style="color:var(--warn)">${t}</span>`).join(''):'');
}

function renderWeapons(){
  const w = curProfile && curProfile.weapons;
  if(!w) return pending('weapon');
  let h='';
  const types = w.usableWeaponTypes||w.weaponTypes||[];
  // Summary chips of usable weapon skills.
  h+='<div class="card"><div class="grp">Weapon types this profession can use</div>'+
     types.map(t=>`<span class="badge on">${t.weaponSkill||t.skill||t.name} <span class="pill">${t.weaponsMpUsable??''}</span></span>`).join('')+
     '<p class="muted" style="font-size:12px">A weapon\'s allowed specials come from its own criteria at runtime; below is the space per type.</p></div>';
  // One card per weapon type with its representative weapons.
  types.forEach(t=>{
    h+=`<div class="grp">${t.weaponSkill||t.skill} `+
       `<span class="pill">${t.weaponsMpUsable??'?'} usable / ${t.weaponsTotalInData??'?'} total${t.mpSkillCost?(' &middot; '+t.mpSkillCost):''}</span></div>`;
    if(t.role) h+=`<p class="muted" style="margin:0 0 6px">${t.role}</p>`;
    if(t.allowedSpecialsObservedForType) h+='<div style="margin-bottom:6px">Specials seen: '+(t.allowedSpecialsObservedForType.map(s=>`<span class="badge" style="color:var(--acc2)">${s}</span>`).join(''))+'</div>';
    const reps = t.representativeWeapons||t.weapons||[];
    if(reps.length){
      h+='<div class="card" style="padding:0"><table><tr><th>Weapon</th><th>QL</th><th>Wield</th><th>Specials</th><th>Where to get</th><th>MP-only</th><th>id</th></tr>';
      reps.forEach(it=>{
        const wield=(it.wield||[]).map(x=>`${x.skill} ${x.requiredValue}`).join(', ')||it.primaryWield||'';
        const specials=(it.specials||[]).map(s=>(s&&typeof s==='object')?`${s.special||s.skill} ${s.requiredValue??''}`.trim():s).join(', ');
        const lock=(it.professionLock&&it.professionLock.length)?'yes':'';
        h+=`<tr class="wrow"><td>${it.name}</td><td>${it.ql??''}</td><td class="muted">${wield}</td><td class="pill" style="color:var(--acc2)">${specials}</td><td>${srcText(wsrc(it.id))}</td><td class="muted">${lock}</td><td class="muted">${it.id??''}</td></tr>`;
      });
      h+='</table></div>';
    }
  });
  if(w.metaphysicistOnlyWeapons){ const mo=w.metaphysicistOnlyWeapons; const bws=Array.isArray(mo.byWeaponSkill)?mo.byWeaponSkill.map(x=>`${x.weaponSkill}:${x.count}`).join(', '):''; h+=`<p class="muted" style="font-size:12px">Profession-locked (MP-only) weapons: ${mo.count??''} &mdash; ${bws}</p>`; }

  // Exhaustive endgame / Xan weapons (companion file).
  const eg = curProfile && curProfile.weaponsEndgame;
  h+='<div class="grp" style="margin-top:20px">Endgame &amp; Xan weapons (exhaustive)</div>';
  if(!eg){ h+=pendingInline('The full endgame/Xan weapon list'); }
  else {
    if(eg.totalModels!=null) h+=`<p class="muted" style="font-size:12px">${eg.totalModels} endgame weapon models an MP can wield.</p>`;
    (eg.byType||[]).forEach(t=>{
      const arr=t.weapons||[]; if(!arr.length) return;
      h+=`<div class="grp">${t.weaponSkill} <span class="pill">${t.count??arr.length}</span></div><div class="card" style="padding:0"><table><tr><th>Weapon</th><th>QL</th><th>Line</th><th>Wield</th><th>Specials</th><th>Exp</th><th>Where</th><th>id</th></tr>`;
      arr.forEach(it=>{
        const wield=(it.wield||[]).map(x=>`${x.skill} ${x.requiredValue}`).join(', ');
        const sp=(it.specials||[]).map(s=>(s&&typeof s==='object')?`${s.special||s.skill} ${s.requiredValue??''}`.trim():s).join(', ');
        h+=`<tr class="wrow"><td>${it.name}</td><td>${it.qlRange||it.ql||''}</td><td class="muted">${it.line||''}</td><td class="muted">${wield}</td><td class="pill" style="color:var(--acc2)">${sp}</td><td class="muted">${it.expansion||''}</td><td class="muted">${it.source||''}</td><td class="muted">${it.id??''}</td></tr>`;
      });
      h+='</table></div>';
    });
  }
  if(w.gear){ h+='<div class="card"><div class="grp">Key gear</div>'+renderGear(w.gear)+'</div>'; }
  return h;
}
function renderGear(g){
  let list=Array.isArray(g)?g:Object.entries(g).map(([k,v])=>({cat:k,items:v}));
  return list.map(c=>`<div><b>${c.cat||c.category||''}</b> <span class="muted">${(c.note||'')}</span><br>${(c.items||[]).map(i=>`<span class="badge">${i.name||i}</span>`).join('')}</div>`).join('');
}

function renderNanos(){
  const n = curProfile && curProfile.nanos;
  if(!n) return pending('nano');
  const cats = n.categories||n;
  let groups = Array.isArray(cats)?cats:Object.entries(cats).map(([k,v])=>({group:k,nanos:v}));
  let h='<div class="card"><input class="search" placeholder="filter nanos..." oninput="filt(this,\'nrow\')"></div>';
  groups.forEach(g=>{
    const arr = g.nanos||g.items||[];
    if(!Array.isArray(arr)||!arr.length) return;
    h+=`<div class="grp">${g.group||g.name} <span class="pill">(${arr.length})</span></div>`;
    h+='<div class="card" style="padding:0"><table><tr><th>Nano</th><th>Line</th><th>Lvl</th><th>Where to get</th><th>Effect</th><th>id</th></tr>';
    arr.forEach(x=>{ h+=`<tr class="nrow"><td>${x.name}</td><td class="muted">${x.nanoLine||x.line||''}</td><td>${x.minLevel??x.ql??x.level??''}</td><td>${srcText(nsrc(x.id))||'<span class=muted>&mdash;</span>'}</td><td class="muted">${x.effectSummary||x.effect||''}</td><td class="muted">${x.id??''}</td></tr>`; });
    h+='</table></div>';
  });
  return h;
}

function filt(inp,cls){
  const q=inp.value.toLowerCase();
  document.querySelectorAll('.'+cls).forEach(r=>{ r.style.display = r.textContent.toLowerCase().includes(q)?'':'none'; });
}

// ---- Team / raid level range -------------------------------------------------
// Who a character of level L can group with (share XP / Shadow Knowledge).
//  1..200 (Rubi-Ka):  min = max(1, floor(L*0.7) - 1);  max = min(200, floor(L*1.4) + 4)
//  201..220 (Shadowlands): baseline-constrained; confirmed anchor L220 -> teams 159..220.
function teamRange(L){
  L=Math.floor(L);
  if(!(L>=1)) return null;
  if(L<=200){
    return {bracket:'Rubi-Ka (1&ndash;200)',
            min:Math.max(1,Math.floor(L*0.7)-1),
            max:Math.min(200,Math.floor(L*1.4)+4), confirmed:true};
  }
  if(L<=220){
    // Only the L=220 anchor is confirmed (min 159). The SL bracket uses hard baselines;
    // the exact per-level curve for 201-219 is not derived here, so don't assert it.
    return {bracket:'Shadowlands (201&ndash;220)', min:159, max:220,
            confirmed:(L===220),
            note:L===220 ? 'Confirmed: a level 220 can team anyone level 159&ndash;220.'
                         : 'Shadowlands bracket &mdash; only the level-220 anchor (159&ndash;220) is confirmed; treat 201&ndash;219 as approximate.'};
  }
  return null;
}
function teamCalc(){
  const el=document.getElementById('teamlvl'); if(!el) return;
  const L=parseInt(el.value,10);
  const out=document.getElementById('teamout');
  const r=teamRange(L);
  if(!r){ out.innerHTML='<span class="muted">Enter a level from 1 to 220.</span>'; return; }
  out.innerHTML=`A level <b>${L}</b> character can team with anyone from `+
    `<b style="color:var(--acc2);font-size:18px">${r.min}</b> to `+
    `<b style="color:var(--acc2);font-size:18px">${r.max}</b>`+
    ` <span class="pill">${r.bracket}${r.confirmed?'':' &middot; approx'}</span>`+
    (r.note?`<div class="muted" style="margin-top:6px;font-size:12px">${r.note}</div>`:'');
}
function showTeam(){
  cur=null;
  document.querySelectorAll('nav .p').forEach(e=>e.classList.remove('active'));
  const m=document.getElementById('main');
  m.innerHTML=`
   <h2>Team Level Range &#129518;</h2>
   <div class="role">The level spread you can group with to share XP / Shadow Knowledge (SK).</div>
   <div class="card">
     <div class="grp">Calculator</div>
     <div style="display:flex;align-items:center;gap:10px;flex-wrap:wrap">
       <label>Your level:
         <input id="teamlvl" type="number" min="1" max="220" value="100"
                oninput="teamCalc()"
                style="width:90px;padding:7px 9px;background:var(--panel2);border:1px solid var(--line);border-radius:7px;color:var(--fg);font-size:15px">
       </label>
     </div>
     <div id="teamout" style="margin-top:12px;font-size:15px"></div>
   </div>
   <div class="card">
     <div class="grp">1. Levels 1&ndash;200 (Rubi-Ka)</div>
     <p class="muted">Uses a 0.7 floor multiplier and a 1.4 ceiling multiplier, with flat baselines so low-level
        characters can still form groups.</p>
     <table>
       <tr><th>Bound</th><th>Formula</th></tr>
       <tr><td><b>Minimum teammate level</b></td><td><code>max(1, floor(YourLevel &times; 0.7) &minus; 1)</code></td></tr>
       <tr><td><b>Maximum teammate level</b></td><td><code>min(200, floor(YourLevel &times; 1.4) + 4)</code></td></tr>
     </table>
     <p class="muted" style="font-size:12px;margin-top:8px"><b>Example, level 100:</b>
        min = floor(100&times;0.7)&minus;1 = <b style="color:var(--acc2)">69</b>,
        max = floor(100&times;1.4)+4 = <b style="color:var(--acc2)">144</b> &rarr; a level 100 teams 69&ndash;144.</p>
   </div>
   <div class="card">
     <div class="grp">2. Levels 201&ndash;220 (Shadowlands)</div>
     <p class="muted">In the Shadowlands end-game bracket the range locks into hard-coded baselines rather than
        the 0.7/1.4 curve. The confirmed anchor: a <b>level 220</b> can share XP/SK with anyone
        <b style="color:var(--acc2)">159&ndash;220</b>.</p>
     <p class="muted" style="font-size:12px">Only the level-220 anchor is confirmed here &mdash; the calculator marks
        201&ndash;219 as <i>approx</i> rather than inventing a per-level curve.</p>
   </div>`;
  teamCalc();
}
function home(){
  cur=null;
  document.querySelectorAll('nav .p').forEach(e=>e.classList.remove('active'));
  document.getElementById('main').innerHTML='<p class="muted">Pick a profession on the left, or use <b>Team Level Range</b> at the top right.</p>';
}

boot();
</script>
</body>
</html>
"""


class Handler(http.server.BaseHTTPRequestHandler):
    def log_message(self, *a):
        pass

    def _json(self, obj, code=200):
        body = json.dumps(obj).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def _html(self, s):
        body = s.encode("utf-8")
        self.send_response(200)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", "no-store, no-cache, must-revalidate")
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        u = urllib.parse.urlparse(self.path)
        q = urllib.parse.parse_qs(u.query)
        path = u.path
        if path == "/" or path == "/index.html":
            return self._html(INDEX_HTML)
        if path == "/api/professions":
            data = load_json(os.path.join(REF, "professions.json"))
            plist = [dict(p) for p in PROFESSIONS]
            meta = {}
            if data:
                src = data.get("professions", data)
                rows = src if isinstance(src, list) else list(src.values())
                for r in rows:
                    pid = r.get("id")
                    if pid is not None:
                        meta[str(pid)] = r
            return self._json({"list": plist, "meta": meta})
        if path == "/api/breeds":
            return self._json(load_json(os.path.join(REF, "breeds.json")) or None)
        if path == "/api/skillcaps":
            return self._json(load_json(os.path.join(REF, "skillcaps.json")) or None)
        if path == "/api/implants":
            return self._json(load_json(os.path.join(REF, "implants.json")) or None)
        if path == "/api/profile":
            slug = (q.get("prof") or [""])[0]
            return self._json(profile_for(slug))
        if path == "/api/status":
            return self._json(status())
        self.send_response(404)
        self.end_headers()


class ThreadingHTTPServer(socketserver.ThreadingMixIn, http.server.HTTPServer):
    daemon_threads = True
    allow_reuse_address = True


if __name__ == "__main__":
    os.makedirs(REF, exist_ok=True)
    port = _port()
    try:
        httpd = ThreadingHTTPServer(("127.0.0.1", port), Handler)
    except OSError as err:
        raise SystemExit(f"AODB: can't serve on port {port} ({err}). Pass another one: aodb.bat 9000")
    print(f"AODB serving on http://localhost:{port}  (data: {BASE})")
    print("Ctrl+C to stop.")
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        pass
