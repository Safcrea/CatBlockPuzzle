#!/usr/bin/env python3
"""
Cat Block Puzzle level generator: keeps the hand-made levels 1-100 of levels_100.json and (re)builds levels 101-TOTAL.

Every generated level is solvable by construction: a compact board is carved inside a grid of at most 8x8 and tiled
exactly with 7-8 cats from the game's own shape table (read from ShapeLibrary.cs), so the tiling is the solution. On top
come the mechanics levels 51-100 already use, at similar rates: stones in the board's notches, padlocks (counter,
neighbour and key), colour zones, fog, a spare stray cat, a three-card tray window and move limits. Each level is then
checked against the same rules as LevelValidator.cs (bounds, overlaps, lock completion, zones, spares, moves) and
rebuilt with another seed if anything fails.

Usage:  python3 Tools/levelgen/generate_levels.py [--total 300] [--seed 7]
Run from the project root. Then in Unity: Cat Block Puzzle > Refresh Level Data, then Rebake Level Prefabs.
"""
import argparse, json, random, re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LEVELS = ROOT / "Assets/_GameData/System/Resources/CatBlockPuzzle/levels_100.json"
SHAPES_CS = ROOT / "Assets/_GameData/System/Scripts/CatBlockPuzzle/Gameplay/ShapeLibrary.cs"
AUTHORED = 100
MAX_GRID = 8
MAX_PIECES = 8

CAT_NAMES = ["Basil", "Bean", "Berry", "Biscuit", "Boba", "Button", "Chili", "Clover", "Cocoa", "Cookie", "Dumpling", "Fig",
             "Fudge", "Ginger", "Honey", "Jelly", "Kiki", "Latte", "Lulu", "Mango", "Maple", "Mimi", "Mint", "Miso", "Mochi",
             "Muffin", "Noodle", "Nori", "Olive", "Peach", "Peanut", "Pearl", "Pebble", "Pepper", "Pickle", "Plum", "Poppy",
             "Pudding", "Pumpkin", "Ribbon", "Sesame", "Socks", "Sprout", "Sunny", "Taco", "Taffy", "Toast", "Tuna", "Waffle",
             "Yuzu", "Acorn", "Bonbon", "Churro", "Daisy", "Ember", "Hazel", "Juniper", "Kiwi", "Lychee", "Marble", "Nugget",
             "Oreo", "Praline", "Quill", "Rosie", "Saffron", "Truffle", "Umeko", "Velvet", "Willow", "Ziggy"]
ADJECTIVES = ["Cosy", "Sleepy", "Sunny", "Velvet", "Honey", "Misty", "Snug", "Starry", "Maple", "Cocoa", "Breezy", "Golden",
              "Pebble", "Moonlit", "Garden", "Ribbon", "Toasty", "Lantern", "Clover", "Pastel", "Purring", "Frosty", "Petal",
              "Cinnamon", "Twilight", "Meadow", "Harbour", "Willow", "Candle", "Cloud"]
NOUNS = ["Corner", "Window", "Cushion", "Basket", "Bookshelf", "Staircase", "Quilt", "Garden Path", "Teacup", "Porch",
         "Attic", "Hammock", "Pantry", "Balcony", "Rooftop", "Greenhouse", "Lighthouse", "Bakery", "Library", "Bridge",
         "Courtyard", "Treehouse", "Market", "Studio", "Harbour", "Picnic", "Carousel", "Bandstand", "Orchard", "Workshop"]


def load_shapes():
    text = SHAPES_CS.read_text()
    shapes = {}
    for name, body in re.findall(r'\{\s*"(\w+)",\s*Cells\(([^}]*)\)\s*\}', text):
        cells = [tuple(map(int, m)) for m in re.findall(r'\((\d+),\s*(\d+)\)', body)]
        shapes[name] = cells
    if len(shapes) < 20:
        sys.exit("Could not read the shape table from " + str(SHAPES_CS))
    return shapes


# ---------------------------------------------------------------- level plan
def plan_difficulty(n):
    """A sawtooth inside every ten levels (with a breather), climbing slowly to the top difficulties."""
    i = n - (AUTHORED + 1)
    pattern = [7, 8, 8, 9, 10, 7, 8, 9, 9, 10]
    d = pattern[i % 10]
    chapter = i // 10  # 0 = chapter 11
    if chapter < 2: d -= 1
    return max(1, min(10, d))


def plan_reward(n, difficulty):
    chapter = (n - (AUTHORED + 1)) // 10
    return 50 + 5 * difficulty + chapter


def choose_features(rng, difficulty):
    feats = []
    for name, p in (("stones", .45), ("locks", .55), ("zones", .35), ("fog", .2), ("spare", .3), ("tray", .35), ("moves", .15)):
        if rng.random() < p: feats.append(name)
    rng.shuffle(feats)
    cap = 4 if difficulty >= 10 else 3
    feats = feats[:cap]
    if "moves" in feats and "tray" in feats and rng.random() < .5: feats.remove("tray")
    return set(feats)


# ---------------------------------------------------------------- board and tiling
def carve_mask(rng, rows, cols, area):
    """A full rectangle with notches bitten from its edges until it has `area` cells; stays one connected piece."""
    cells = {(r, c) for r in range(rows) for c in range(cols)}
    def connected(cs):
        if not cs: return False
        start = next(iter(cs)); seen = {start}; stack = [start]
        while stack:
            r, c = stack.pop()
            for nb in ((r + 1, c), (r - 1, c), (r, c + 1), (r, c - 1)):
                if nb in cs and nb not in seen: seen.add(nb); stack.append(nb)
        return len(seen) == len(cs)
    tries = 0
    while len(cells) > area and tries < 4000:
        tries += 1
        # bite next to the outside (a cell on the border, or next to an already-removed cell that touches the border)
        border = [(r, c) for (r, c) in cells if r in (0, rows - 1) or c in (0, cols - 1) or
                  any(nb not in cells for nb in ((r + 1, c), (r - 1, c), (r, c + 1), (r, c - 1)))]
        r, c = rng.choice(border)
        trial = cells - {(r, c)}
        # keep every row and column of the grid in use, so the grid is not bigger than the board
        if not any(rr == r for rr, _ in trial) or not any(cc == c for _, cc in trial): continue
        if connected(trial): cells = trial
    return cells if len(cells) == area else None


def tile(rng, mask, shapes, sizes_needed, max_same=2, node_limit=60000):
    """Exact cover of `mask` with shapes whose sizes use up `sizes_needed` (size -> count). Returns [(shape, row, col)]."""
    by_size = {}
    for name, cells in shapes.items(): by_size.setdefault(len(cells), []).append(name)
    order = sorted(mask)
    nodes = [0]
    used = {}
    placed = []
    filled = set()

    def anchor(cells):  # the shape's first cell in reading order
        return min(cells)

    def rec():
        nodes[0] += 1
        if nodes[0] > node_limit: return False
        free = [cell for cell in order if cell not in filled]
        if not free: return all(v == 0 for v in sizes_needed.values())
        r0, c0 = free[0]
        size_order = [s for s in sizes_needed if sizes_needed[s] > 0]
        rng.shuffle(size_order)
        for s in size_order:
            names = by_size.get(s, [])[:]
            rng.shuffle(names)
            for name in names:
                if used.get(name, 0) >= max_same: continue
                cells = shapes[name]
                ar, ac = anchor(cells)
                br, bc = r0 - ar, c0 - ac
                abs_cells = [(br + r, bc + c) for r, c in cells]
                if any(cell not in mask or cell in filled for cell in abs_cells): continue
                filled.update(abs_cells); sizes_needed[s] -= 1; used[name] = used.get(name, 0) + 1
                placed.append((name, br, bc))
                if rec(): return True
                placed.pop(); used[name] -= 1; sizes_needed[s] += 1; filled.difference_update(abs_cells)
        return False

    return placed[:] if rec() else None


# ---------------------------------------------------------------- validation (mirrors LevelValidator.cs)
def piece_cells(shapes, p):
    return [(p["row"] + r, p["col"] + c) for r, c in shapes[p["shape"]]]


def validate(level, shapes):
    rows, cols = level["rows"], level["cols"]
    if not (1 <= rows <= MAX_GRID and 1 <= cols <= MAX_GRID): return "grid size"
    if not (1 <= level["difficulty"] <= 10): return "difficulty"
    owner, cells_of = {}, {}
    ids = set()
    for i, p in enumerate(level["pieces"]):
        if p["id"] in ids: return "duplicate id"
        ids.add(p["id"])
        if p["shape"] not in shapes: return "shape"
        cs = piece_cells(shapes, p)
        for cell in cs:
            if not (0 <= cell[0] < rows and 0 <= cell[1] < cols): return "out of bounds"
            if cell in owner: return "overlap"
            owner[cell] = i
        cells_of[i] = cs
    for b in level.get("blocked", []):
        cell = (b["row"], b["col"])
        if not (0 <= cell[0] < rows and 0 <= cell[1] < cols) or cell in owner: return "stone"
    largest = max(len(shapes[p["shape"]]) for p in level["pieces"])
    for s in level.get("spares", []):
        if len(shapes[s["shape"]]) > largest: return "spare too big"
    index = {p["id"]: i for i, p in enumerate(level["pieces"])}
    for z in level.get("zones", []):
        if z["piece"] not in index or (z["row"], z["col"]) not in cells_of[index[z["piece"]]]: return "zone"
    if level.get("moves", 0) and level["moves"] < len(level["pieces"]): return "moves"
    for f in level.get("fog", []):
        if not (0 <= f["row"] < rows and 0 <= f["col"] < cols): return "fog"
    locks = level.get("locked", [])
    if locks:
        locked = {}
        for lk in locks:
            cell = (lk["row"], lk["col"])
            if cell not in owner or cell in locked: return "lock cell"
            if lk.get("key"):
                if lk["key"] not in index or index[lk["key"]] == owner[cell]: return "lock key"
            locked[cell] = lk
        if len({owner[c] for c in locked}) == len(level["pieces"]): return "all locked"
        for cell, lk in locked.items():
            if lk.get("key") or lk["after"] > 0: continue
            r, c = cell
            if not any(nb in owner and nb not in locked and owner[nb] != owner[cell] for nb in ((r - 1, c), (r + 1, c), (r, c - 1), (r, c + 1))):
                return "neighbour lock"
        # Same as LevelValidator.CanComplete: a neighbour lock opens once ALL its unlocked neighbours from other cats are
        # filled, and a tray window only ever offers the first `trayWindow` unplaced cats, one placement at a time.
        unlockers = {}
        for cell, lk in locked.items():
            if lk.get("key") or lk["after"] > 0: continue
            r, c = cell
            unlockers[cell] = [nb for nb in ((r - 1, c), (r + 1, c), (r, c - 1), (r, c + 1)) if nb in owner and owner[nb] != owner[cell] and nb not in locked]
        placed, filled, opened = set(), set(), set()
        def open_locks():
            changed = True
            while changed:
                changed = False
                for cell, lk in locked.items():
                    if cell in opened: continue
                    if lk.get("key"): ok = index[lk["key"]] in placed
                    elif lk["after"] > 0: ok = len(placed) >= lk["after"]
                    else: ok = all(nb in filled for nb in unlockers[cell])
                    if ok: opened.add(cell); changed = True
        open_locks()
        window = level.get("trayWindow", 0)
        progress = True
        while progress and len(placed) < len(level["pieces"]):
            progress = False
            rank = 0
            for i in range(len(level["pieces"])):
                if i in placed: continue
                if window > 0:
                    if rank >= window: break
                    rank += 1
                if any(cell in locked and cell not in opened for cell in cells_of[i]): continue
                placed.add(i); filled.update(cells_of[i]); open_locks(); progress = True
                break
        if len(placed) != len(level["pieces"]): return "locks never open"
    return None


# ---------------------------------------------------------------- one level
def build_level(n, shapes, rng, used_titles):
    difficulty = plan_difficulty(n)
    k = MAX_PIECES if rng.random() < .75 else 7
    for attempt in range(200):
        n5 = min(k, rng.choice([k - 2, k - 1, k - 1, k, k - 3]))
        n4 = k - n5
        n3 = 0
        if n4 > 0 and rng.random() < .15: n3, n4 = 1, n4 - 1
        area = n5 * 5 + n4 * 4 + n3 * 3
        dims = [(r, c) for r in range(5, MAX_GRID + 1) for c in range(5, MAX_GRID + 1) if area + 2 <= r * c <= area + 12]
        if not dims: continue
        rows, cols = rng.choice(dims)
        mask = carve_mask(rng, rows, cols, area)
        if not mask: continue
        sizes = {5: n5, 4: n4, 3: n3}
        tiling = tile(rng, mask, shapes, sizes)
        if not tiling: continue
        names = rng.sample(CAT_NAMES, k)
        pieces = [{"id": "l%03d_p%d" % (n, i + 1), "name": names[i], "shape": s, "row": r, "col": c} for i, (s, r, c) in enumerate(tiling)]
        rng.shuffle(pieces)
        for i, p in enumerate(pieces): p["id"] = "l%03d_p%d" % (n, i + 1)
        title = rng.choice(ADJECTIVES) + " " + rng.choice(NOUNS)
        while title in used_titles: title = rng.choice(ADJECTIVES) + " " + rng.choice(NOUNS)
        level = {"id": "level_%03d" % n, "title": title, "difficulty": difficulty,
                 "rows": rows, "cols": cols, "reward": plan_reward(n, difficulty), "pieces": pieces, "trayWindow": 0, "hearts": 0}
        feats = choose_features(rng, difficulty)
        owner = {}
        for i, p in enumerate(pieces):
            for cell in piece_cells(shapes, p): owner[cell] = i
        if "tray" in feats: level["trayWindow"] = 3
        if "stones" in feats:
            notches = [(r, c) for r in range(rows) for c in range(cols) if (r, c) not in owner and
                       sum(nb in owner for nb in ((r + 1, c), (r - 1, c), (r, c + 1), (r, c - 1))) >= 2]
            if notches: level["blocked"] = [{"row": r, "col": c} for r, c in rng.sample(notches, min(len(notches), rng.choice([1, 1, 2])))]
        if "locks" in feats:
            locks = []
            for _ in range(rng.choice([1, 2, 2, 3])):
                cell = rng.choice(list(owner))
                if any((lk["row"], lk["col"]) == cell for lk in locks): continue
                kind = rng.random()
                if kind < .45: locks.append({"row": cell[0], "col": cell[1], "after": rng.randint(2, max(2, min(5, k - 2)))})
                elif kind < .75: locks.append({"row": cell[0], "col": cell[1], "after": 0})
                else:
                    others = [p["id"] for i, p in enumerate(pieces) if i != owner[cell]]
                    locks.append({"row": cell[0], "col": cell[1], "after": 0, "key": rng.choice(others)})
            level["locked"] = locks
        if "zones" in feats:
            zones = []
            for i in rng.sample(range(k), rng.choice([1, 1, 2])):
                cs = piece_cells(shapes, pieces[i])
                for cell in rng.sample(cs, min(len(cs), rng.choice([2, 3]))):
                    zones.append({"row": cell[0], "col": cell[1], "piece": pieces[i]["id"]})
            level["zones"] = zones
        if "spare" in feats:
            largest = max(len(shapes[p["shape"]]) for p in pieces)
            options = [s for s, cs in shapes.items() if len(cs) <= largest and len(cs) >= 4]
            level["spares"] = [{"id": "l%03d_s1" % n, "name": "Stray", "shape": rng.choice(options), "row": 0, "col": 0}]
        if "fog" in feats:
            h, w = rng.choice([(2, 3), (3, 3), (3, 2)])
            r0, c0 = rng.randint(0, rows - h), rng.randint(0, cols - w)
            fog = [{"row": r0 + dr, "col": c0 + dc} for dr in range(h) for dc in range(w) if (r0 + dr, c0 + dc) in owner]
            if len(fog) >= 4: level["fog"] = fog
        if "moves" in feats: level["moves"] = k + 2
        error = validate(level, shapes)
        if error is None:
            used_titles.add(title)
            return level
    sys.exit("Could not build level %d" % n)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--total", type=int, default=300)
    ap.add_argument("--seed", type=int, default=7)
    args = ap.parse_args()
    shapes = load_shapes()
    pack = json.loads(LEVELS.read_text())
    authored = pack["levels"][:AUTHORED]
    for lv in authored:
        err = validate(lv, shapes)
        if err: sys.exit("Authored %s fails the checks: %s" % (lv["id"], err))
    generated = []
    used_titles = {lv["title"] for lv in authored}
    for n in range(AUTHORED + 1, args.total + 1):
        generated.append(build_level(n, shapes, random.Random(args.seed * 100003 + n), used_titles))
    pack["levels"] = authored + generated
    LEVELS.write_text(json.dumps(pack, indent=2) + "\n")
    print("levels: %d (%d generated)" % (len(pack["levels"]), len(generated)))


if __name__ == "__main__":
    main()
