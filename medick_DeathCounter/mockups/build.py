#!/usr/bin/env python3
"""Rebuild death-panel.html / .png from the REAL Core output.

    python3 medick_DeathCounter/mockups/build.py

Runs mockups/sample (sample fights through src/Core: DeathAnalyzer, Advisor,
DeathPatterns), then lays the result out with src/UI's palette and sizes.
The PNG needs headless Chromium (CHROME env var, default the Playwright one).
"""
import html, json, os, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
out = subprocess.run(["dotnet", "run", "--project", os.path.join(HERE, "sample")],
                     capture_output=True, text=True, check=True).stdout.strip().splitlines()[-1]
d = json.loads(out)

E = lambda s: html.escape(str(s))
COL = {'Physical': '#C8B9A6', 'Fire': '#E2683C', 'Cold': '#6FB7E8', 'Lightning': '#E8D04A',
       'Necrotic': '#4FB0A0', 'Void': '#9B6BD6', 'Poison': '#7BC043'}   # Theme.ElementColors
AILEL = {'Ignite': 'Fire', 'Bleed': 'Physical', 'Poison': 'Poison', 'Frostbite': 'Cold', 'Electrify': 'Lightning',
         'Time Rot': 'Void', 'Doom': 'Void', 'Damned': 'Necrotic', 'Freeze': 'Cold', 'Chill': 'Cold',
         'Shock': 'Lightning', 'Abyssal Decay': 'Void'}
NAMES = ['Physical', 'Fire', 'Cold', 'Lightning', 'Necrotic', 'Void', 'Poison']
L, P = d['last'], d['pat']

def defense_line(rec):
    """Mirror of DeathPanel.DefenseLine."""
    df, parts = rec['Defenses'], []
    for n in NAMES:
        if 'Res.' + n in df:
            v = df['Res.' + n]; parts.append(f"{n} {v:.0f}%" + (" (cap)" if v >= 74.5 else ""))
    for key, fmt in (('Armor', 'Armor {:,.0f}'), ('Dodge', 'Dodge {:,.0f}'), ('Block', 'Block {:.0f}%'), ('Endurance', 'Endurance {:.0f}%'),
                     ('CritAvoidance', 'Crit avoid {:.0f}%'), ('Ward', 'Ward {:,.0f} (after hit)')):
        if key in df: parts.append(fmt.format(df[key]))
    return "  ·  ".join(parts)

def chip(t, c): return f'<span class="chip" style="--c:{c}">{E(t)}</span>'
def acol(a): return COL.get(AILEL.get(a, ''), '#C6C2B6')
def bars(items):
    return ''.join(f'<div class="bar"><span class="bl">{E(n)}</span><span class="track"><i style="width:{s*100:.1f}%;background:{COL[n]}"></i></span>'
                   f'<span class="pct">{s*100:.0f}%</span></div>' for n, s in items if s >= 0.01)
def tip(i, title, body): return f'<div class="tip"><b class="n">{i}.</b><div><div class="tt">{E(title)}</div><div class="tb">{E(body)}</div></div></div>'

tot = sum(L['DamageByElement'])
mix = sorted(((NAMES[i], v / tot) for i, v in enumerate(L['DamageByElement']) if v > 0), key=lambda x: -x[1])
how = [L['KillerAbility'], f"{L['KillingBlow']:,.0f} {L['KillingElement'].lower()} damage ({L['KillingBlow']/L['MaxHealth']*100:.0f}% of your life)"]
if L['KillingCrit']: how.append('critical strike')

def panel(tab, body, nav=True):
    navh = (f'<span class="nav"><span class="btn">‹</span><span class="pos">{d["count"]} / {d["count"]}</span>'
            f'<span class="dim">›</span></span>') if nav else ''
    return (f'<div class="panel"><div class="blood"></div>'
            f'<div class="hdr"><span>TERRIBLE DEATHS&nbsp; ·&nbsp; {E(L["Character"])}</span>{navh}<span class="btn">✕</span></div>'
            f'<div class="tabs"><span class="tab{" on" if tab == 0 else ""}">LAST DEATH</span><span class="tab{" on" if tab == 1 else ""}">PATTERNS</span></div>'
            f'{body}<div class="foot"><span>Log: UserData/medick_DeathCounter/deaths.txt</span><span class="btn">OPEN LOG FOLDER</span></div></div>')

last_body = (
    f'<div class="title">Killed by {E(d["killerLine"])}</div><div class="how">{E("  ·  ".join(how))}</div>'
    f'<div class="meta"><span>3 min ago&nbsp; ·&nbsp; {E(L["Zone"])}&nbsp; ·&nbsp; lvl {L["Level"]} {E(L["CharacterClass"])}</span>{chip(d["kind"], "#C23B3B")}</div>'
    + (f'<div class="sec">YOUR DEFENCES AT DEATH</div><div class="how">{E(defense_line(L))}</div>' if L.get('Defenses') else '')
    + f'<div class="sec">AILMENTS ON YOU</div><div class="chips">{"".join(chip(a, acol(a)) for a in L["AilmentsOnYou"])}</div>'
    f'<div class="sec">INCOMING DAMAGE, LAST 5s&nbsp; ·&nbsp; {L["WindowDamage"]:,.0f} in {L["Hits"]} hits</div>{bars(mix)}'
    f'<div class="sec">TOP THREATS</div>'
    + ''.join(f'<div class="row">{E(s["Name"])}&nbsp;&nbsp; {s["Amount"]:,.0f} damage, {s["Hits"]} hit{"s" if s["Hits"] != 1 else ""}</div>' for s in L['TopSources'])
    + '<div class="sec">TO SURVIVE NEXT TIME</div>'
    + ''.join(tip(i + 1, t['Title'], t['Body']) for i, t in enumerate(d['tips'])))

ail_chips = "".join(chip(a["Name"] + " ×" + str(a["Count"]), acol(a["Name"])) for a in P["ailments"])
kinds_line = "  ·  ".join(str(k["n"]) + " " + k["k"] for k in sorted(P["kinds"], key=lambda k: -k["n"]))

pat_body = (
    f'<div class="sec">BUILD PRIORITIES&nbsp; ·&nbsp; LAST {P["Deaths"]} DEATHS</div>'
    + ''.join(tip(i + 1, f'{p["Title"]}  ({p["Deaths"]} of {P["Deaths"]} deaths)', p['Body']) for i, p in enumerate(P['prio']))
    + '<div class="sec">WHO KEEPS KILLING YOU</div>'
    + ''.join(f'<div class="row">{E(k["Name"])}&nbsp;&nbsp; {k["Count"]} death{"s" if k["Count"] != 1 else ""}</div>' for k in P['killers'])
    + f'<div class="sec">WHAT KILLS YOU&nbsp; ·&nbsp; INCOMING DAMAGE BY TYPE</div>{bars([(s["el"], s["Share"]) for s in P["shares"]])}'
    + f'<div class="sec">AILMENTS ON YOU MOST</div><div class="chips">{ail_chips}</div>'
    + f'<div class="kinds">{E(kinds_line)}</div>')

CSS = """
:root{--bg:#0C0E13;--surface:#161A24;--inset:#10131B;--border:#2A3040;--borderhi:#3C4456;--accent:#C9A653;--accentdim:#8A7339;--texthi:#EDE6D4;--text:#C6C2B6;--textmut:#807D8C;--blood:#C23B3B;--blooddim:#6E2226}
*{box-sizing:border-box}body{margin:0;padding:24px;background:#05060a radial-gradient(circle at 30% 20%,#1d1a26,#05060a 70%);color:var(--text);font-family:Arial,Helvetica,sans-serif}
.note{font:italic 13px Georgia,serif;color:var(--textmut);margin:0 0 4px;max-width:1000px}h1{font:bold 20px Georgia,serif;color:var(--accent);margin:0 0 4px}
.hud{display:flex;flex-direction:column;align-items:center;gap:4px;margin:14px 0 22px}
.pill{display:flex;align-items:center;gap:7px;height:28px;padding:0 10px;background:rgba(12,14,19,.96);border:2px solid var(--blood)}
.pill .l{font:bold 10px Arial;color:var(--textmut)}.pill .v{font:bold 17px Georgia,serif;color:var(--blood)}.pill .s{font:9px Arial;color:var(--textmut)}
.toast{background:rgba(12,14,19,.96);border:1px solid var(--border);padding:4px 10px;font:bold 11px Arial;color:var(--texthi)}
.deck{display:flex;gap:24px;flex-wrap:wrap;align-items:flex-start}
.panel{width:480px;max-width:100%;background:rgba(12,14,19,.96);border:1px solid var(--border);padding:14px;position:relative}
.blood{position:absolute;left:0;right:0;top:0;height:2px;background:var(--blooddim)}
.hdr{display:flex;align-items:center;gap:8px;font:bold 9px Arial;color:var(--accentdim);height:20px}.hdr>span:first-child{flex:1}
.nav{display:flex;align-items:center}.pos{font:10px Arial;color:var(--textmut);padding:0 6px}.dim{color:var(--border);width:20px;text-align:center;font-size:13px}
.btn{display:inline-flex;align-items:center;justify-content:center;min-width:20px;height:20px;padding:0 6px;background:var(--surface);border:1px solid var(--border);color:var(--text);font:11px Arial}
.tabs{display:flex;gap:4px;margin:4px 0 10px}.tab{flex:1;height:20px;display:flex;align-items:center;justify-content:center;background:var(--surface);border:1px solid var(--border);font:9px Arial;color:var(--text)}.tab.on{box-shadow:inset 0 -2px 0 var(--accent)}
.title{font:bold 19px Georgia,serif;color:var(--texthi);margin:2px 0 4px}.how{font:11px Arial;color:var(--text);margin-bottom:4px}
.meta{display:flex;align-items:center;gap:10px;font:10px Arial;color:var(--textmut);margin:4px 0 10px}
.chip{display:inline-flex;align-items:center;height:18px;padding:0 7px;background:var(--inset);border:1px solid var(--borderhi);box-shadow:inset 2px 0 0 var(--c);font:bold 9px Arial;color:var(--c);margin:0 6px 4px 0}
.chips{margin-bottom:8px}
.sec{display:flex;align-items:center;gap:8px;font:bold 9px Arial;color:var(--accentdim);margin:6px 0}.sec::after{content:"";flex:1;height:1px;background:var(--border)}
.bar{display:flex;align-items:center;height:18px;font:10px Arial}.bl{width:78px}.track{flex:1;height:10px;background:var(--inset)}.track i{display:block;height:100%}.pct{width:40px;padding-left:6px;color:var(--textmut)}
.row{font:11px Arial;color:var(--text);min-height:17px}
.tip{display:flex;gap:4px;margin-bottom:7px}.n{width:14px;font:bold 12px Arial;color:var(--accent)}.tt{font:bold 12px Arial;color:var(--texthi)}.tb{font:10px Arial;color:var(--textmut);line-height:1.35}
.kinds{font:10px Arial;color:var(--textmut);margin:4px 0 6px}
.foot{display:flex;justify-content:space-between;align-items:center;border-top:1px solid var(--border);margin-top:6px;padding-top:6px;font:9px Arial;color:var(--textmut)}.foot .btn{font-size:9px;width:124px}
.log{font:11px Consolas,monospace;color:var(--text);background:var(--inset);border:1px solid var(--border);padding:8px 10px;margin-top:22px;max-width:984px;white-space:pre-wrap;word-break:break-word}
"""

page = f"""<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Terrible Deaths mockup</title><style>{CSS}</style></head><body>
<h1>Terrible Deaths v0.1.0: mockup</h1>
<p class="note">Sample data. Every line of text, advice and ranking below is the mod's real output: the sample fights in mockups/sample were run through the actual Core code (DeathAnalyzer, Advisor, DeathPatterns). Colours, sizes and layout mirror src/UI. Not a game screenshot: the mod has not run in game yet.</p>
<div class="hud"><div class="pill"><span class="l">DEATHS</span><span class="v">{d["count"]}</span><span class="s">+1 this session</span></div>
<div class="toast">Killed by {E(d["killerLine"])}&nbsp; ·&nbsp; Insert for details</div></div>
<div class="deck">{panel(0, last_body)}{panel(1, pat_body, nav=False)}</div>
<div class="log">deaths.txt
{E(d["logLine"])}</div></body></html>"""

html_path = os.path.join(HERE, "death-panel.html")
open(html_path, "w").write(page)
chrome = os.environ.get("CHROME", "/opt/pw-browsers/chromium-1194/chrome-linux/chrome")
if os.path.exists(chrome):
    subprocess.run([chrome, "--headless", "--no-sandbox", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=2",
                    "--window-size=1060,1000", "--screenshot=" + os.path.join(HERE, "death-panel.png"), "file://" + html_path],
                   capture_output=True, check=True)
print("wrote", html_path)
