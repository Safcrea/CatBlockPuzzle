#!/usr/bin/env python3
"""
Adds chapters 11-30 to meta_chapters.json (keeps the hand-written chapters 1-10).

Each new chapter covers ten of the generated levels (101-300), reuses one of the ten room pictures and that room's five
decoration pictures (same hotspots and sizes, so they still fit the picture), and has its own room name, cat, story and
decoration names. Decoration costs follow the same rules as CatMetaCatalogValidator: each one costs about 80% of the two
levels before its unlock and never more than they pay, so a chapter spends 75-85% of what its levels pay.

Usage:  python3 Tools/levelgen/generate_chapters.py      (after generate_levels.py; run from the project root)
"""
import json, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
META = ROOT / "Assets/_GameData/System/Resources/CatBlockPuzzle/meta_chapters.json"
LEVELS = ROOT / "Assets/_GameData/System/Resources/CatBlockPuzzle/levels_100.json"
HAND_WRITTEN = 10

# Season two: the cats of the neighbourhood. (id, title, cat, start story, completion story, decoration name prefix)
NEW_CHAPTERS = [
    ("bakery_loft", "Bakery Loft", "Biscuit", "Biscuit sleeps on warm flour sacks and trusts no one.", "The loft above the ovens becomes Biscuit's toasty home.", "Baker's"),
    ("lighthouse_lookout", "Lighthouse Lookout", "Pearl", "Pearl watches the sea alone from the lighthouse steps.", "With a cosy lookout, Pearl finally has company for the night watch.", "Lighthouse"),
    ("greenhouse_glade", "Greenhouse Glade", "Basil", "Basil hides between the tomato vines.", "The glade blooms and Basil naps in the sun at last.", "Greenhouse"),
    ("rooftop_garden", "Rooftop Garden", "Kiwi", "Kiwi only comes out when the city goes quiet.", "A garden in the sky gives Kiwi a calm place above the noise.", "Rooftop"),
    ("library_hideaway", "Library Hideaway", "Quill", "Quill curls up behind the dustiest shelf.", "Quill now reads along on a cushion by the window.", "Library"),
    ("music_room", "Music Room", "Bonbon", "Bonbon flinches at every note.", "Soft music and a snug corner turn Bonbon into the room's biggest fan.", "Melody"),
    ("art_attic", "Art Attic", "Hazel", "Hazel leaves paw prints on every canvas, then runs.", "The attic becomes Hazel's studio, paw prints and all.", "Painter's"),
    ("winter_cabin", "Winter Cabin", "Ember", "Ember shivers by a cold fireplace.", "A crackling fire and a deep blanket keep Ember warm all winter.", "Cabin"),
    ("picnic_porch", "Picnic Porch", "Daisy", "Daisy steals sandwiches and bolts into the hedge.", "Daisy now waits politely for her share on the porch.", "Picnic"),
    ("toy_workshop", "Toy Workshop", "Nugget", "Nugget chews every toy he finds.", "A workshop full of his own toys makes Nugget the happiest helper.", "Toymaker's"),
    ("tea_garden", "Tea Garden", "Lychee", "Lychee hides under the teahouse steps.", "Lychee now greets every guest by the steaming teapot.", "Teahouse"),
    ("starlight_observatory", "Starlight Observatory", "Velvet", "Velvet only trusts the stars.", "From her new perch Velvet counts the stars with a friend.", "Stargazer's"),
    ("harbour_boathouse", "Harbour Boathouse", "Saffron", "Saffron lives on a leaky rowing boat.", "A dry boathouse lets Saffron dream of the sea in comfort.", "Harbour"),
    ("flower_shop", "Flower Shop", "Rosie", "Rosie sneezes at every bouquet and hides.", "A quiet corner of the shop gives Rosie a home among the petals.", "Florist's"),
    ("cosy_cafe", "Cosy Café", "Churro", "Churro begs at the café door in the rain.", "Churro now has a reserved seat by the coffee machine.", "Café"),
    ("puppet_theatre", "Puppet Theatre", "Ziggy", "Ziggy is scared of the puppets' shadows.", "Ziggy now steals the show every night.", "Theatre"),
    ("candle_workshop", "Candle Workshop", "Juniper", "Juniper keeps away from every flicker.", "Gentle candlelight makes Juniper's workshop the calmest room in town.", "Candlemaker's"),
    ("orchard_barn", "Orchard Barn", "Acorn", "Acorn sleeps in the apple crates.", "The barn becomes Acorn's sweet-smelling home.", "Orchard"),
    ("snowy_chalet", "Snowy Chalet", "Marble", "Marble is lost in the snow.", "Marble now watches the snowflakes from a warm window seat.", "Chalet"),
    ("grand_festival_hall", "Grand Festival Hall", "Every Cat", "The whole neighbourhood gathers for one last celebration.", "Every cat now has a home, and the festival never ends.", "Festival"),
]


def fmt_number(v):
    return ("%.2f" % v) if isinstance(v, float) else json.dumps(v)


def fmt_decoration(d):
    return ('{ "id": %s, "displayName": %s, "cost": %d, "unlockAfterChapterLevel": %d, "spriteResourcePath": %s, '
            '"hotspot": { "x": %s, "y": %s }, "size": { "x": %s, "y": %s }, "sortOrder": %d }') % (
        json.dumps(d["id"], ensure_ascii=False), json.dumps(d["displayName"], ensure_ascii=False), d["cost"], d["unlockAfterChapterLevel"],
        json.dumps(d["spriteResourcePath"]), fmt_number(float(d["hotspot"]["x"])), fmt_number(float(d["hotspot"]["y"])),
        fmt_number(float(d["size"]["x"])), fmt_number(float(d["size"]["y"])), d["sortOrder"])


def fmt(meta):
    out = ['{', '  "version": %d,' % meta["version"], '  "chapters": [']
    for ci, ch in enumerate(meta["chapters"]):
        out.append('    {')
        keys = [k for k in ch if k != "decorations"]
        for k in keys:
            out.append('      %s: %s,' % (json.dumps(k), json.dumps(ch[k], ensure_ascii=False)))
        out.append('      "decorations": [')
        for di, d in enumerate(ch["decorations"]):
            out.append('        ' + fmt_decoration(d) + (',' if di < len(ch["decorations"]) - 1 else ''))
        out.append('      ]')
        out.append('    }' + (',' if ci < len(meta["chapters"]) - 1 else ''))
    out += ['  ]', '}']
    return "\n".join(out) + "\n"


def main():
    text = META.read_text()
    meta = json.loads(text)
    if fmt({"version": meta["version"], "chapters": meta["chapters"][:len(meta["chapters"])]}) != text and len(meta["chapters"]) == HAND_WRITTEN:
        print("note: the file's formatting differs slightly from the generator's")
    rewards = [lv["reward"] for lv in json.loads(LEVELS.read_text())["levels"]]
    total_chapters = len(rewards) // 10
    base = meta["chapters"][:HAND_WRITTEN]
    chapters = list(base)
    for ci in range(HAND_WRITTEN, total_chapters):
        cid, title, cat, start, done, prefix = NEW_CHAPTERS[ci - HAND_WRITTEN]
        model = base[(ci - HAND_WRITTEN) % HAND_WRITTEN]
        first = ci * 10
        decorations = []
        for d in model["decorations"]:
            unlock = d["unlockAfterChapterLevel"]
            pair = rewards[first + unlock - 2] + rewards[first + unlock - 1]
            cost = min(pair, int(round(pair * 0.8 / 5.0)) * 5)
            name = d["displayName"]
            for old_cat in ("Mochi", "Bean", "Pickle", "Nori", "Miso", "Waffle", "Taffy", "Sunny", "Pepper", "Forever Home"):
                name = name.replace(old_cat, cat if cat != "Every Cat" else "Festival")
            if not name.startswith(cat) and not name.startswith("Festival"):
                name = prefix + " " + name
            slug = d["id"].split("_", 1)[1]
            decorations.append({"id": "ch%02d_%s" % (ci + 1, slug), "displayName": name, "cost": cost, "unlockAfterChapterLevel": unlock,
                                "spriteResourcePath": d["spriteResourcePath"], "hotspot": d["hotspot"], "size": d["size"], "sortOrder": d["sortOrder"]})
        chapters.append({"index": ci, "id": cid, "title": title, "catName": cat, "startStory": start, "completionStory": done,
                         "firstLevelIndex": first, "lastLevelIndex": first + 9,
                         "backgroundResourcePath": model["backgroundResourcePath"], "thumbnailResourcePath": model["thumbnailResourcePath"],
                         "catPortraitIndex": model["catPortraitIndex"], "decorations": decorations})
        spend = sum(d["cost"] for d in decorations) / float(sum(rewards[first:first + 10]))
        if not 0.75 < spend < 0.85: sys.exit("%s spends %.3f of its rewards" % (cid, spend))
    meta["chapters"] = chapters
    META.write_text(fmt(meta))
    print("chapters: %d (%d new)" % (len(chapters), len(chapters) - HAND_WRITTEN))


if __name__ == "__main__":
    main()
