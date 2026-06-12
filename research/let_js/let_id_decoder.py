"""
lastepochtools.com planner ID decoder.

LET encodes item / affix / set IDs with lz-string's
`compressToEncodedURIComponent` applied to a zero-padded, fixed-width
digit string, with a single-letter tag prepended.

Format (verified from planner.js on 2026-04-14):
  xb  (unique):  "I" + lzEnc("1" + pad(baseTypeId,3) + pad(subTypeId,3) + pad(rarity,1) + pad(uniqueId,2))
                 -> body has a leading '1' literal, then 9 digits = 10 total
  Kb  (rare):    "U" + lzEnc(pad(subTypeId,3) + pad(uniqueId,3))
                 -> 6 digits.  NOTE: LET calls the 2nd field 'uniqueId' but
                    for rares this is really the affix-set signature.
  Me  (affix):   "A" + lzEnc(pad(affixId,3))
                 -> 3+ digits (widens for affixId >= 1000)
  Rc  (set):     "S" + lzEnc(pad(setId,3))

Blessings share the unique format (tag "I"): baseTypeId=34 is the
blessing slot and subTypeId is the blessing row id.
Idols also share the unique format with baseTypeId in the idol range.

Usage:
    from let_id_decoder import decode_let_item_id, decode_let_affix_id
    decode_let_item_id("UAzBMBYEZSA")
    # -> {'tag':'rare','subTypeId':2,'uniqueId':412}  (uniqueId = affix sig)
    decode_let_item_id("IIwBhBYWAmMSA")
    # -> {'tag':'unique','baseTypeId':4,'subTypeId':12,'rarity':0,'uniqueId':0}
    decode_let_affix_id("AKwBgTEA")
    # -> 502
"""
import lzstring

_LZ = lzstring.LZString()


def _lz_decode(body: str) -> str:
    """Decode an lz-string compressToEncodedURIComponent payload."""
    s = _LZ.decompressFromEncodedURIComponent(body)
    if s is None:
        raise ValueError(f"lz-string decode failed for {body!r}")
    return s


def decode_let_item_id(let_id: str) -> dict:
    """
    Decode a lastepochtools item ID into game-internal integer fields.

    Returns a dict with:
      tag:        'unique' | 'rare' | 'set' | 'unknown'
      baseTypeId: int  (unique/blessing/idol only)
      subTypeId:  int
      rarity:     int  (unique only; 0 normal, 7 unique, 8 set, 9 legendary)
      uniqueId:   int  (unique: the unique variant id; rare: affix signature)
    """
    if not let_id or len(let_id) < 2:
        raise ValueError(f"let_id too short: {let_id!r}")
    tag, body = let_id[0], let_id[1:]
    digits = _lz_decode(body)

    if tag == "I":
        # must start with literal '1' per xb()
        if not digits.startswith("1"):
            raise ValueError(f"unique id missing '1' prefix: {digits!r}")
        d = digits[1:]
        # pad right if the encoder stripped trailing zeros (lz-string preserves
        # the exact string, but be defensive for short payloads)
        if len(d) < 9:
            d = d.ljust(9, "0")
        return {
            "tag": "unique",
            "baseTypeId": int(d[0:3]),
            "subTypeId":  int(d[3:6]),
            "rarity":     int(d[6:7]),
            "uniqueId":   int(d[7:9]),
            "_raw":       digits,
        }

    if tag == "U":
        # Kb(): pad(subTypeId,3) + pad(uniqueId,3).  6 digits.
        if len(digits) < 6:
            digits = digits.rjust(6, "0")
        return {
            "tag": "rare",
            "subTypeId": int(digits[0:3]),
            "uniqueId":  int(digits[3:6]),
            "_raw":      digits,
        }

    if tag == "S":
        return {"tag": "set", "setId": int(digits), "_raw": digits}

    return {"tag": "unknown", "_raw": digits}


def decode_let_affix_id(let_id: str) -> int:
    """Decode an LET affix ID to the game-internal integer affixId."""
    if not let_id or let_id[0] != "A":
        raise ValueError(f"not an affix id: {let_id!r}")
    return int(_lz_decode(let_id[1:]))


def _selftest():
    cases_items = {
        "UAzBMBYEZSA":   "rare head",
        "UAzAsCYFZKA":   "rare chest",
        "UAwRmGYRI":     "rare weapon1",
        "UAzDMwdgFiA":   "rare weapon2",
        "IIwBhBYWAmMSA": "unique hands",
        "UAzBMBZnI":     "rare waist",
        "UAzBMGZgNiA":   "rare feet",
        "UAzCcGYDZiA":   "rare ring1",
        "UAzDMwNgViA":   "rare ring2",
        "UAwRgzGBsAcQ":  "rare amulet",
        "UAwNgzALGZA":   "rare relic",
        "IIwBgLK4tQ":    "idol",
        "IIwBgzALMwGwvQ": "blessing",
        "IIwBgzALMBMEvQ": "blessing",
        "IIwBgzALCCsJyQ": "blessing",
    }
    for lid, label in cases_items.items():
        print(f"{lid:<18} ({label:<13}) -> {decode_let_item_id(lid)}")
    print()
    cases_affix = ["AKwBgTEA", "AIwBmA4g", "AAwNgnEA", "AIwBgTALEA"]
    for lid in cases_affix:
        print(f"{lid:<14} -> affixId={decode_let_affix_id(lid)}")


if __name__ == "__main__":
    _selftest()
