"""
Extract maxroll name tables from the downloaded leplanner data.json,
and write them as a compact JSON file at
LEBuildConverter/maxroll_names_data.json with the same top-level schema
as LEBuildConverter/le_names_data.json.

Data source: https://assets-ng.maxroll.gg/leplanner/game/data.json
(found by inspecting https://maxroll.gg/last-epoch/database and its
associated planner bundle at
https://assets-ng.maxroll.gg/leplanner/static/js/tools.js ).
"""
from __future__ import annotations

import json
import os

HERE = os.path.dirname(os.path.abspath(__file__))
DATA_JSON = os.path.join(HERE, 'data.json')
OUT = os.path.normpath(
    os.path.join(HERE, '..', '..', 'LEBuildConverter', 'maxroll_names_data.json')
)


def _name_or_display(obj: dict, keys=('displayName', 'name')) -> str:
    """Prefer displayName when present and non-empty, else fall back to name."""
    for k in keys:
        v = obj.get(k)
        if v:
            return v
    return ''


def main() -> int:
    with open(DATA_JSON, 'r', encoding='utf-8') as f:
        d = json.load(f)

    # ----- items: {baseTypeID}/{subTypeID} -> displayName (or name) -----
    items: dict[str, str] = {}
    item_types: dict[str, str] = {}
    for it in d['itemTypes']:
        tid = it.get('baseTypeID')
        if tid is None:
            continue
        item_types[str(tid)] = it.get('displayName') or it.get('BaseTypeName') or ''
        for sub in it.get('subItems', []) or []:
            sid = sub.get('subTypeID')
            if sid is None:
                continue
            nm = _name_or_display(sub, ('displayName', 'name'))
            if nm:
                items[f'{tid}/{sid}'] = nm

    # ----- uniques: uniqueID -> displayName/name -----
    uniques: dict[str, str] = {}
    for u in d['uniques']:
        uid = u.get('uniqueID')
        if uid is None:
            continue
        nm = _name_or_display(u, ('displayName', 'name'))
        if nm:
            uniques[str(uid)] = nm
    # Also include set items (they live in setBonuses.itemsInSet).
    for sb in d['setBonuses']:
        for u in sb.get('itemsInSet', []) or []:
            uid = u.get('uniqueID')
            if uid is None:
                continue
            if str(uid) in uniques:
                continue
            nm = _name_or_display(u, ('displayName', 'name'))
            if nm:
                uniques[str(uid)] = nm

    # ----- affixes: affixId -> affixDisplayName/affixName -----
    # Keep both names: maxroll's affixDisplayName is the user-facing string
    # (what the database page shows, matches LET most of the time), while
    # affixName is the internal name used in engine scripts (handy when
    # affixDisplayName is empty or appears stale).
    affixes: dict[str, str] = {}
    affixes_internal: dict[str, str] = {}
    for a in d['affixes']:
        aid = a.get('affixId')
        if aid is None:
            continue
        disp = a.get('affixDisplayName') or ''
        internal = a.get('affixName') or ''
        if disp:
            affixes[str(aid)] = disp
        elif internal:
            affixes[str(aid)] = internal
        if internal:
            affixes_internal[str(aid)] = internal

    # ----- abilities (skill trees): tree_id -> abilityName -----
    # maxroll indexes abilities by the in-engine name ("EnchantWeapon"); the
    # tree id lives on each skillTree entry as tree.ability = "EnchantWeapon".
    abilities_by_tree: dict[str, str] = {}
    skill_tree_nodes: dict[str, str] = {}
    for tree_id, tree in d['skillTrees'].items():
        ab_key = tree.get('ability')
        if ab_key:
            ab = d['abilities'].get(ab_key, {})
            nm = ab.get('abilityName')
            if nm:
                abilities_by_tree[tree_id] = nm
        # Collect node names for every tree (passive + skill trees) so
        # skill_tree_nodes ends up covering both ability nodes and class
        # passive tree nodes (pr-1, mg-1, kn-1, ac-1, rg-1).
        nodes = tree.get('nodes') or {}
        if isinstance(nodes, dict):
            for node_id, node in nodes.items():
                if isinstance(node, dict):
                    nn = node.get('nodeName') or node.get('name')
                    if nn:
                        skill_tree_nodes[f'{tree_id}/{node_id}'] = nn

    # ----- passive_nodes mirror of LET layout -----
    passive_by_class: dict[str, dict] = {}
    tree_ids_by_class_id: dict[str, str] = {}
    for c in d['classes']:
        cid = c.get('classID')
        tree_id = c.get('treeID')
        if cid is None:
            continue
        nodes = {}
        tree = d['skillTrees'].get(tree_id) if tree_id else None
        if tree and isinstance(tree.get('nodes'), dict):
            for node_id, node in tree['nodes'].items():
                if isinstance(node, dict):
                    nn = node.get('nodeName') or node.get('name')
                    if nn:
                        nodes[str(node_id)] = nn
        passive_by_class[str(cid)] = {
            'class_name': c.get('className') or '',
            'tree_id': tree_id or '',
            'nodes': nodes,
        }
        if tree_id:
            tree_ids_by_class_id[tree_id] = str(cid)

    # ----- masteries: classID -> {index -> name} -----
    masteries: dict[str, dict[str, str]] = {}
    for c in d['classes']:
        cid = c.get('classID')
        if cid is None:
            continue
        m_map = {}
        for i, m in enumerate(c.get('masteries') or []):
            nm = m.get('name') if isinstance(m, dict) else None
            if nm:
                m_map[str(i)] = nm
        masteries[str(cid)] = m_map

    # ----- properties: id -> propertyName -----
    properties: dict[str, str] = {}
    for p in d['properties']:
        pid = p.get('property')
        if pid is None:
            continue
        nm = p.get('propertyName') or p.get('textKey')
        if nm:
            properties[str(pid)] = nm

    # ----- set_bonuses: setID -> derived name (shortest common prefix of items) -----
    # maxroll does not expose a setBonus title; derive "Isadora's" etc. from
    # the items in the set so we can still compare against LET.
    def _set_name(sb: dict) -> str:
        items_ = sb.get('itemsInSet') or []
        names = [
            (_name_or_display(x, ('displayName', 'name')) or '').strip()
            for x in items_
        ]
        names = [n for n in names if n]
        if not names:
            return ''
        # Take the longest common prefix, trim trailing space.
        pref = names[0]
        for n in names[1:]:
            i = 0
            while i < len(pref) and i < len(n) and pref[i] == n[i]:
                i += 1
            pref = pref[:i]
        return pref.rstrip(" '-")

    set_bonuses: dict[str, str] = {}
    for sb in d['setBonuses']:
        sid = sb.get('setID')
        if sid is None:
            continue
        set_bonuses[str(sid)] = _set_name(sb)

    out = {
        '_meta': {
            'source': 'https://assets-ng.maxroll.gg/leplanner/game/data.json',
            'dataVersion': 'maxroll leplanner data.json',
        },
        'items': items,
        'item_types': item_types,
        'uniques': uniques,
        'affixes': affixes,
        'affixes_internal': affixes_internal,
        'set_bonuses': set_bonuses,
        'abilities': abilities_by_tree,
        'passive_nodes': {
            'by_class_id': passive_by_class,
            'tree_ids': tree_ids_by_class_id,
        },
        'skill_tree_nodes': skill_tree_nodes,
        'masteries': masteries,
        'properties': properties,
    }

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, 'w', encoding='utf-8') as f:
        json.dump(out, f, ensure_ascii=False, separators=(',', ':'))
    sz = os.path.getsize(OUT)
    print(f'wrote {OUT}  ({sz:,} bytes)')
    print(
        f'  items={len(items)}  item_types={len(item_types)}  '
        f'uniques={len(uniques)}  affixes={len(affixes)}  '
        f'set_bonuses={len(set_bonuses)}'
    )
    print(
        f'  abilities(tree_id)={len(abilities_by_tree)}  '
        f'skill_tree_nodes={len(skill_tree_nodes)}  '
        f'properties={len(properties)}  masteries={len(masteries)}'
    )
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
