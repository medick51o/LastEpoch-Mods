"""
Extract LET name tables from the downloaded JS data files and i18n,
and write them as a compact JSON file for le_names.py.

Run this from inside research/let_data/.

Produces:
    ../../LEBuildConverter/le_names_data.json

Uses jsparse.py (tolerant JS-object-literal parser) to read the minified
JS data, then pulls the relevant i18n strings (Abilities/Skills/Item_Names/
Item_Affixes) and builds plain-JSON lookup tables keyed by integer / string
id.
"""
import json
import os
import re
import sys

sys.setrecursionlimit(400000)
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import jsparse  # noqa: E402


HERE = os.path.dirname(os.path.abspath(__file__))


def load_i18n():
    with open(os.path.join(HERE, 'i18n_full_en.json'), 'r', encoding='utf-8') as f:
        return json.load(f)


def parse_window_assignment(filename, name):
    """Parse `window.{name} = <value>` from filename and return the value."""
    with open(os.path.join(HERE, filename), 'r', encoding='utf-8') as f:
        src = f.read()
    m = re.search(r'window\.' + re.escape(name) + r'\s*=\s*', src)
    if not m:
        raise RuntimeError(f"window.{name} not found in {filename}")
    p = jsparse.JSParser(src)
    p.pos = m.end()
    return p.parse_value()


def translate(i18n, key, default=None):
    if key is None:
        return default
    v = i18n.get(key)
    if v is None:
        return default
    # Normalize doubled apostrophes that some Tolgee strings use ("don''t" -> "don't")
    return v.replace("''", "'")


def build_items(itemDB, i18n):
    """
    Extract item names. Return a dict keyed by "itemType/subType" -> name.
    Also return uniques keyed by str(uniqueId).
    """
    il = itemDB['itemList']
    items = {}

    # equippable
    eq = il.get('equippable', {})
    for type_id, type_obj in eq.items():
        type_key = type_obj.get('displayNameKey')
        type_name = translate(i18n, type_key, f"Item_{type_id}")
        subs = type_obj.get('subItems', {})
        for sub_id, sub in subs.items():
            nk = sub.get('displayNameKey')
            nm = translate(i18n, nk, f"{type_name} #{sub_id}")
            items[f"{type_id}/{sub_id}"] = nm

    # nonEquippable (shards, runes, glyphs, keys, woven echoes, bags, etc.)
    ne = il.get('nonEquippable', {})
    for type_id, type_obj in ne.items():
        type_key = type_obj.get('displayNameKey')
        type_name = translate(i18n, type_key, f"Item_{type_id}")
        subs = type_obj.get('subItems', {})
        for sub_id, sub in subs.items():
            nk = sub.get('displayNameKey')
            nm = translate(i18n, nk, f"{type_name} #{sub_id}")
            items[f"{type_id}/{sub_id}"] = nm

    # item type -> category name, e.g. itemType 0 -> "Helmet"
    item_types = {}
    for type_id, type_obj in list(eq.items()) + list(ne.items()):
        item_types[str(type_id)] = translate(
            i18n,
            type_obj.get('displayNameKey'),
            f"ItemType_{type_id}",
        )

    return items, item_types


def build_uniques(itemDB, i18n):
    uns = itemDB.get('uniqueList', {}).get('uniques', {})
    out = {}
    for uid, u in uns.items():
        nk = u.get('displayNameKey') or u.get('nameKey') or u.get('uniqueNameKey')
        nm = translate(i18n, nk, f"Unique_{uid}")
        out[str(uid)] = nm
    return out


def build_affixes(itemDB, i18n):
    al = itemDB.get('affixList', {})
    out = {}
    for bucket_name in ('singleAffixes', 'multiAffixes'):
        bucket = al.get(bucket_name, {})
        for aid, a in bucket.items():
            nk = a.get('affixDisplayNameKey') or a.get('displayNameKey') or a.get('nameKey')
            nm = translate(i18n, nk, f"Affix_{aid}")
            out[str(aid)] = nm
    return out


def build_set_bonuses(itemDB, i18n):
    sb = itemDB.get('setBonusList', [])
    if not isinstance(sb, list):
        return {}
    out = {}
    for i, s in enumerate(sb):
        if not isinstance(s, dict):
            continue
        nk = s.get('displayNameKey') or s.get('nameKey')
        nm = translate(i18n, nk, f"Set_{i}")
        out[str(i)] = nm
    return out


def build_abilities(LEAbilities, i18n):
    """LEAbilities.abilityList is keyed by short tree id like 'sb44eQ'."""
    alist = LEAbilities.get('abilityList', {})
    out = {}
    for tid, ab in alist.items():
        nk = ab.get('nameKey')
        nm = translate(i18n, nk, ab.get('internalName') or tid)
        out[tid] = nm
    return out


def build_passive_nodes(LECharTrees, i18n):
    """
    Build passive node names keyed by classID (0-4) and then node id.

    Classes:
        0 = Primalist (treeID 'pr-1')
        1 = Mage (treeID 'mg-1')
        2 = Sentinel (treeID 'kn-1')
        3 = Acolyte (treeID 'ac-1')
        4 = Rogue (treeID 'rg-1')
    """
    trees = LECharTrees.get('trees', [])
    out = {'by_class_id': {}, 'tree_ids': {}}
    for t in trees:
        ct = t.get('characterTree') or {}
        cclass = ct.get('characterClass') or {}
        class_id = cclass.get('classID')
        tree_id = cclass.get('treeID') or ct.get('treeID')
        class_name_key = cclass.get('classNameKey')
        class_name = translate(i18n, class_name_key, f"Class_{class_id}")
        nodes = ct.get('nodes', {})
        node_map = {}
        for node_id, node in nodes.items():
            nk = node.get('nodeNameKey')
            nm = translate(i18n, nk, f"Node_{node_id}")
            node_map[str(node_id)] = nm
        out['by_class_id'][str(class_id)] = {
            'class_name': class_name,
            'tree_id': tree_id,
            'nodes': node_map,
        }
        out['tree_ids'][tree_id] = str(class_id)
    return out


def build_skill_tree_nodes(LESkillTrees, i18n):
    """Node names for each skill tree (LESkillTrees[tree_id].nodes[node_id]).
    Keyed by "tree_id/node_id" -> name."""
    out = {}
    for tid, tree in LESkillTrees.items():
        if not isinstance(tree, dict):
            continue
        nodes = tree.get('nodes', {})
        if isinstance(nodes, list):
            # some trees might store nodes as list; emit list index as key
            iterable = enumerate(nodes)
        else:
            iterable = nodes.items()
        for nid, node in iterable:
            if not isinstance(node, dict):
                continue
            nk = node.get('nodeNameKey')
            nm = translate(i18n, nk, None)
            if nm is not None:
                out[f"{tid}/{nid}"] = nm
    return out


def build_masteries(LECharTrees, i18n):
    """Class masteries (Primalist, Beastmaster, ..., Runemaster)."""
    trees = LECharTrees.get('trees', [])
    out = {}
    for t in trees:
        ct = t.get('characterTree') or {}
        cclass = ct.get('characterClass') or {}
        class_id = cclass.get('classID')
        masteries = cclass.get('masteries', [])
        class_masteries = {}
        for i, m in enumerate(masteries):
            nk = m.get('nameKey')
            nm = translate(i18n, nk, m.get('name') or f"Mastery_{i}")
            class_masteries[str(i)] = nm
        out[str(class_id)] = class_masteries
    return out


def build_properties(coreDB, i18n):
    """
    Build a mapping from property id -> property name.
    Used for decoding affix / tree property references.
    """
    plist = coreDB.get('propertyList', [])
    out = {}
    for p in plist:
        if not isinstance(p, dict):
            continue
        pid = p.get('property')
        nk = p.get('propertyNameKey') or p.get('nameKey')
        nm = translate(i18n, nk, None)
        if pid is not None and nm is not None:
            out[str(pid)] = nm
    return out


def main():
    print("Loading i18n...")
    i18n = load_i18n()
    print(f"  {len(i18n)} translation keys")

    print("Parsing db_1.js / itemDB...")
    itemDB = parse_window_assignment('db_1.js', 'itemDB')

    print("Parsing db_2.js / coreDB...")
    coreDB = parse_window_assignment('db_2.js', 'coreDB')

    print("Parsing planner_2.js / LEAbilities...")
    LEAbilities = parse_window_assignment('planner_2.js', 'LEAbilities')

    print("Parsing planner_2.js / LECharTrees...")
    LECharTrees = parse_window_assignment('planner_2.js', 'LECharTrees')

    print("Parsing planner_2.js / LESkillTrees...")
    LESkillTrees = parse_window_assignment('planner_2.js', 'LESkillTrees')

    print("Building lookup tables...")
    items, item_types = build_items(itemDB, i18n)
    uniques = build_uniques(itemDB, i18n)
    affixes = build_affixes(itemDB, i18n)
    set_bonuses = build_set_bonuses(itemDB, i18n)
    abilities = build_abilities(LEAbilities, i18n)
    passive_nodes = build_passive_nodes(LECharTrees, i18n)
    skill_tree_nodes = build_skill_tree_nodes(LESkillTrees, i18n)
    masteries = build_masteries(LECharTrees, i18n)
    properties = build_properties(coreDB, i18n)

    data = {
        '_meta': {
            'gameVersion': 'Version 1.4.2',
            'dataVersion': 'version142',
            'language': 'en',
            'counts': {
                'items': len(items),
                'item_types': len(item_types),
                'uniques': len(uniques),
                'affixes': len(affixes),
                'set_bonuses': len(set_bonuses),
                'abilities': len(abilities),
                'passive_nodes_classes': len(passive_nodes.get('by_class_id', {})),
                'skill_tree_nodes': len(skill_tree_nodes),
                'masteries_classes': len(masteries),
                'properties': len(properties),
            },
        },
        'items': items,
        'item_types': item_types,
        'uniques': uniques,
        'affixes': affixes,
        'set_bonuses': set_bonuses,
        'abilities': abilities,
        'passive_nodes': passive_nodes,
        'skill_tree_nodes': skill_tree_nodes,
        'masteries': masteries,
        'properties': properties,
    }

    out_path = os.path.abspath(os.path.join(HERE, '..', '..', 'LEBuildConverter', 'le_names_data.json'))
    with open(out_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, separators=(',', ':'))

    print()
    print("Extraction summary:")
    for k, v in data['_meta']['counts'].items():
        print(f"  {k:30s} {v}")
    print()
    print(f"Wrote {out_path}")


if __name__ == '__main__':
    main()
