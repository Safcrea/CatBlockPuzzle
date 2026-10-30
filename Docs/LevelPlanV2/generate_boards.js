// Re-draws the board outlines of chosen levels in levels_100.json so the campaign stops reusing the same
// rectangles. Every new board is built from the shape library, so it is solvable by construction.
//
//   node Docs/LevelPlanV2/generate_boards.js <path to levels_100.json> [--dry]
//
// Only levels whose mechanics do not depend on exact cell positions are redrawn: plain boards, bowls (blocked),
// hearts, busy shelf, drop limit, spare cats and counter padlocks. Padlocks are re-placed so the shelf order can
// always satisfy them. Piece count, difficulty, reward and the other settings of a level are kept.
const fs = require("fs");

const SHAPES = {
  i3v: [[0,0],[1,0],[2,0]], i3h: [[0,0],[0,1],[0,2]], i4v: [[0,0],[1,0],[2,0],[3,0]], i4h: [[0,0],[0,1],[0,2],[0,3]],
  i5h: [[0,0],[0,1],[0,2],[0,3],[0,4]], l3: [[0,0],[1,0],[1,1]], l4: [[0,0],[1,0],[2,0],[2,1]],
  l4Top: [[0,0],[0,1],[0,2],[1,2]], l4Corner: [[0,0],[1,0],[1,1],[1,2]], j4: [[0,1],[1,1],[2,0],[2,1]],
  l5: [[0,0],[1,0],[2,0],[3,0],[3,1]], l5Top: [[0,0],[0,1],[0,2],[0,3],[1,3]], o4: [[0,0],[0,1],[1,0],[1,1]],
  p5: [[0,0],[0,1],[1,0],[1,1],[2,1]], t4: [[0,0],[0,1],[0,2],[1,1]], t5: [[0,1],[1,0],[1,1],[1,2],[2,1]],
  s4: [[0,1],[0,2],[1,0],[1,1]], z4: [[0,0],[0,1],[1,1],[1,2]], s5: [[0,1],[0,2],[1,0],[1,1],[2,0]],
  z5: [[0,0],[0,1],[1,1],[1,2],[2,2]], u5: [[0,0],[0,2],[1,0],[1,1],[1,2]], plus5: [[0,1],[1,0],[1,1],[1,2],[2,1]],
  stair5: [[0,0],[1,0],[1,1],[2,1],[2,2]], l3Top: [[0,0],[0,1],[1,0]], l3TopRight: [[0,0],[0,1],[1,1]],
  j3: [[0,1],[1,0],[1,1]], l4TopLeft: [[0,0],[0,1],[1,0],[2,0]], j4Top: [[0,0],[0,1],[1,1],[2,1]],
  s4v: [[0,0],[1,0],[1,1],[2,1]], t4Left: [[0,1],[1,0],[1,1],[2,1]]
};
const IDS = Object.keys(SHAPES);
// Row-major first cell of each shape: the anchor used when tiling.
const FIRST = {};
IDS.forEach(id => { const s = SHAPES[id].slice().sort((a, b) => a[0] - b[0] || a[1] - b[1]); FIRST[id] = s[0]; });

const NAMES = "Mint,Ribbon,Peanut,Tuna,Pebble,Kiki,Poppy,Chili,Pudding,Bean,Pickle,Fig,Waffle,Muffin,Mimi,Dumpling,Pumpkin,Basil,Pepper,Maple,Pearl,Toast,Miso,Button,Olive,Sunny,Taffy,Boba,Mango,Socks,Yuzu,Jelly,Taco,Clover,Cocoa,Nori,Sprout,Plum,Ginger,Lulu,Sesame,Berry,Mochi,Honey,Biscuit,Noodle,Peach,Cookie,Fudge,Latte".split(",");
const ORGANIC_TITLES = "Pebble Path,Cloud Nap,Leaf Pile,Moon Rug,Acorn Cap,Kite Tail,Mossy Stone,Puddle Hop,Cozy Patch,Drift Wood,Lantern Glow,Tumble Rock,Bean Bag,Cushion Trail,Sunny Spot,Breeze Tile,Quilt Square,Tea Tray,Honey Comb,Dune Ridge,Snow Drift,Maple Leaf,Wool Scrap,Hill Top".split(",");

const TEMPLATES = [
  ["Plus Plate", [".###.", "#####", "#####", "#####", ".###."]],
  ["Ring Roll", ["#####", "##.##", "#...#", "##.##", "#####"].map(r => r)],
  ["Diamond Gem", ["..#..", ".###.", "#####", ".###.", "..#.."]],
  ["Corner Nook", ["##...", "##...", "##...", "#####", "#####"]],
  ["Stair Climb", ["#....", "##...", "###..", "####.", "#####"]],
  ["Two Islands", ["###..", "###..", "###..", "..###", "..###", "..###"]],
  ["Heart Pillow", [".#.#.", "#####", "#####", ".###.", "..#.."]],
  ["Twin Towers", ["#...#", "#...#", "#####", "#####", "#...#", "#...#"]],
  ["Big Diamond", ["...#...", "..###..", ".#####.", "#######", ".#####.", "..###..", "...#..."]],
  ["Fat Cross", ["..###..", "..###..", "#######", "#######", "#######", "..###..", "..###.."]],
  ["Big Ring", ["#######", "#######", "##...##", "##...##", "##...##", "#######", "#######"]],
  ["Arrow Up", ["...#...", "..###..", ".#####.", "...#...", "...#...", "..###..", "..###.."]],
  ["T Shelf", ["#######", "#######", "..###..", "..###..", "..###.."]],
  ["Zigzag Rug", ["####...", "####...", "..#####", "..#####", "......."].map(r => r.replace(/\.$/, "."))],
  ["Hourglass", ["#####", ".###.", "..#..", ".###.", "#####"]],
  ["Stepping Stones", ["##..##", "##..##", "..##..", "..##..", "##..##"]],
  ["Plus Ring", [".#####.", "###.###", "##...##", "###.###", ".#####."]],
  ["Chevron", ["#...#", "##.##", ".###.", "..#..", "..#.."]],
  ["Cat Head", ["#.....#", "##...##", "#######", "#######", "#######", ".#####.", "..###.."]],
  ["Little House", ["...#...", "..###..", ".#####.", "#######", ".#####.", ".##.##."]],
  ["Fish Dinner", [".##....", "#####.#", "#######", "#####.#", ".##...."]],
  ["Paw Print", ["##..##", "##..##", "......", ".####.", "######", ".####."]],
  ["Big Heart", ["##.##", "#####", "#####", ".###.", "..#.."]],
  ["Bone Treat", ["##...##", "#######", "#######", "##...##"]],
  ["Sailboat", ["..#....", "..##...", "..###..", "#######", ".#####."]],
  ["Mitten", [".###.", ".###.", "#####", "#####", ".###."]]
];

function mulberry32(a) { return function () { a |= 0; a = a + 0x6D2B79F5 | 0; let t = Math.imul(a ^ a >>> 15, 1 | a); t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; }; }
function shuffled(arr, rng) { const a = arr.slice(); for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(rng() * (i + 1)); [a[i], a[j]] = [a[j], a[i]]; } return a; }

function maskCells(rows) { const out = []; rows.forEach((r, i) => r.split("").forEach((c, j) => { if (c === "#") out.push([i, j]); })); return out; }
function transform(cells, t) {
  // 8 symmetries of the square: rotate by t%4 quarter turns, mirror when t>=4.
  let c = cells.map(x => x.slice());
  for (let k = 0; k < t % 4; k++) c = c.map(([r, q]) => [q, -r]);
  if (t >= 4) c = c.map(([r, q]) => [r, -q]);
  const mr = Math.min(...c.map(x => x[0])), mc = Math.min(...c.map(x => x[1]));
  return c.map(([r, q]) => [r - mr, q - mc]);
}

// Exact cover of a mask by exactly `count` library shapes.
function tile(cells, count, rng, nodeBudget) {
  const key = (r, c) => r + "," + c;
  const free = new Set(cells.map(([r, c]) => key(r, c)));
  const order = cells.slice().sort((a, b) => a[0] - b[0] || a[1] - b[1]);
  const placed = [];
  let nodes = 0;
  function rec(idx, remaining) {
    if (++nodes > nodeBudget) return false;
    while (idx < order.length && !free.has(key(order[idx][0], order[idx][1]))) idx++;
    if (idx === order.length) return placed.length === count;
    const left = count - placed.length;
    if (left <= 0 || remaining < 3 * left || remaining > 5 * left) return false;
    const [r, c] = order[idx];
    for (const id of shuffled(IDS, rng)) {
      const f = FIRST[id], origin = [r - f[0], c - f[1]];
      const cs = SHAPES[id].map(([a, b]) => [origin[0] + a, origin[1] + b]);
      if (!cs.every(([a, b]) => free.has(key(a, b)))) continue;
      cs.forEach(([a, b]) => free.delete(key(a, b)));
      placed.push({ shape: id, row: origin[0], col: origin[1] });
      if (rec(idx + 1, remaining - cs.length)) return true;
      placed.pop();
      cs.forEach(([a, b]) => free.add(key(a, b)));
    }
    return false;
  }
  return rec(0, cells.length) ? placed : null;
}

// Grow a compact blob piece by piece; touching pieces keep it connected and tiled by construction.
function organic(count, rng, maxDim) {
  for (let attempt = 0; attempt < 300; attempt++) {
    const occ = new Set(), placed = [];
    const add = (id, r, c) => { SHAPES[id].forEach(([a, b]) => occ.add((r + a) + "," + (c + b))); placed.push({ shape: id, row: r, col: c }); };
    add(IDS[Math.floor(rng() * IDS.length)], 0, 0);
    let ok = true;
    for (let k = 1; k < count && ok; k++) {
      const minR = Math.min(...placed.map(p => p.row)) - 3, maxR = Math.max(...placed.map(p => p.row)) + 3;
      const minC = Math.min(...placed.map(p => p.col)) - 3, maxC = Math.max(...placed.map(p => p.col)) + 3;
      const cand = [];
      for (const id of IDS) for (let r = minR; r <= maxR; r++) for (let c = minC; c <= maxC; c++) {
        const cs = SHAPES[id].map(([a, b]) => [r + a, c + b]);
        if (cs.some(([a, b]) => occ.has(a + "," + b))) continue;
        let contact = 0;
        cs.forEach(([a, b]) => [[1,0],[-1,0],[0,1],[0,-1]].forEach(([da, db]) => { if (occ.has((a + da) + "," + (b + db))) contact++; }));
        if (contact === 0) continue;
        const all = [...occ].map(s => s.split(",").map(Number)).concat(cs);
        const h = Math.max(...all.map(x => x[0])) - Math.min(...all.map(x => x[0])) + 1;
        const w = Math.max(...all.map(x => x[1])) - Math.min(...all.map(x => x[1])) + 1;
        if (h > maxDim || w > maxDim) continue;
        cand.push({ id, r, c, score: contact * 1.0 - h * w * 0.12 + rng() * 2.2 });
      }
      if (!cand.length) { ok = false; break; }
      cand.sort((a, b) => b.score - a.score);
      const pick = cand[Math.floor(rng() * Math.min(5, cand.length))];
      add(pick.id, pick.r, pick.c);
    }
    if (!ok) continue;
    const cells = [...occ].map(s => s.split(",").map(Number));
    const h = Math.max(...cells.map(x => x[0])) - Math.min(...cells.map(x => x[0])) + 1;
    const w = Math.max(...cells.map(x => x[1])) - Math.min(...cells.map(x => x[1])) + 1;
    if (cells.length / (h * w) < 0.5 || h < 3 || w < 3) continue;
    return placed;
  }
  return null;
}

function distinctOk(placed) { return new Set(placed.map(p => p.shape)).size >= Math.max(2, placed.length - 2); }
function bbox(placed) {
  const cells = []; placed.forEach(p => SHAPES[p.shape].forEach(([a, b]) => cells.push([p.row + a, p.col + b])));
  return { minR: Math.min(...cells.map(x => x[0])), minC: Math.min(...cells.map(x => x[1])), maxR: Math.max(...cells.map(x => x[0])), maxC: Math.max(...cells.map(x => x[1])), cells };
}

function buildBoard(level, index, rng, templateCursor) {
  const count = level.pieces.filter(p => !p.fixed).length;
  const useTemplate = index % 2 === 0;
  let placed = null, title = null;
  if (useTemplate) {
    for (let n = 0; n < TEMPLATES.length && !placed; n++) {
      const [name, rows] = TEMPLATES[(templateCursor.i + n) % TEMPLATES.length];
      if (templateCursor.used.has(name)) continue;
      const base = maskCells(rows);
      if (base.length < 3 * count || base.length > 5 * count) continue;
      for (let t = 0; t < 8 && !placed; t++) {
        const cells = transform(base, Math.floor(rng() * 8));
        const res = tile(cells, count, rng, 60000);
        if (res && distinctOk(res)) { placed = res; title = name; templateCursor.used.add(name); templateCursor.i = (templateCursor.i + n + 1) % TEMPLATES.length; }
      }
    }
  }
  if (!placed) {
    for (let n = 0; n < 40 && !placed; n++) { const res = organic(count, rng, 7); if (res && distinctOk(res)) placed = res; }
    const free = ORGANIC_TITLES.filter(t => !templateCursor.used.has(t));
    title = free.length ? free[Math.floor(rng() * free.length)] : "Patch " + (templateCursor.used.size + 1);
    templateCursor.used.add(title);
  }
  if (!placed) return null;
  const b = bbox(placed);
  placed = placed.map(p => ({ shape: p.shape, row: p.row - b.minR, col: p.col - b.minC }));
  return { placed, rows: b.maxR - b.minR + 1, cols: b.maxC - b.minC + 1, title };
}

function main() {
  const file = process.argv[2], dry = process.argv.includes("--dry");
  const raw = fs.readFileSync(file, "utf8"), crlf = raw.includes("\r\n");
  const pack = JSON.parse(raw), L = pack.levels;
  const has = (l, k) => l[k] && l[k].length;
  const eligible = [];
  L.forEach((l, i) => {
    const n = i + 1;
    if (n <= 3 || has(l, "zones") || has(l, "fog") || l.pieces.some(p => p.fixed)) return;
    if ((l.locked || []).some(x => x.key || x.after === 0)) return;
    eligible.push(i);
  });
  const templateCursor = { i: 0, used: new Set() };
  const report = [];
  eligible.forEach((i, k) => {
    const l = L[i], rng = mulberry32(9100 + i * 77);
    const board = buildBoard(l, k, rng, templateCursor);
    if (!board) { report.push((i + 1) + ": skipped (no board found)"); return; }
    const num = String(i + 1).padStart(3, "0");
    const names = shuffled(NAMES, rng);
    const fixedPieces = l.pieces.filter(p => p.fixed);
    const order = shuffled(board.placed, rng);
    const pieces = order.map((p, j) => ({ id: "l" + num + "_p" + (j + 1), name: names[j], shape: p.shape, row: p.row, col: p.col }));
    // Counter padlocks keep their counts but move to a cat that is placed late enough to satisfy them.
    let locked;
    if (has(l, "locked")) {
      locked = []; const usedPieces = new Set();
      l.locked.slice().sort((a, b) => b.after - a.after).forEach(lock => {
        for (let idx = Math.min(pieces.length - 1, Math.max(lock.after, 0)); idx < pieces.length; idx++) {
          if (usedPieces.has(idx)) continue;
          usedPieces.add(idx);
          const p = pieces[idx], cs = SHAPES[p.shape], cell = cs[Math.floor(rng() * cs.length)];
          locked.push({ row: p.row + cell[0], col: p.col + cell[1], after: Math.min(lock.after, idx) });
          return;
        }
      });
    }
    // Bowls: same count as before, in empty cells of the new grid.
    let blocked;
    if (has(l, "blocked")) {
      const filled = new Set(); pieces.forEach(p => SHAPES[p.shape].forEach(([a, b]) => filled.add((p.row + a) + "," + (p.col + b))));
      const empties = []; for (let r = 0; r < board.rows; r++) for (let c = 0; c < board.cols; c++) if (!filled.has(r + "," + c)) empties.push({ row: r, col: c });
      blocked = shuffled(empties, rng).slice(0, Math.min(l.blocked.length, empties.length));
    }
    const out = Object.assign({}, l, { title: board.title, rows: board.rows, cols: board.cols, pieces: pieces.concat(fixedPieces) });
    if (locked) out.locked = locked;
    if (blocked) out.blocked = blocked;
    if (has(l, "blocked") && !blocked.length) delete out.blocked;
    L[i] = out;
    report.push((i + 1) + ": " + l.rows + "x" + l.cols + " -> " + board.rows + "x" + board.cols + " " + board.title + " (" + pieces.length + " cats)");
  });
  console.log(report.join("\n"));
  if (dry) return;
  let text = JSON.stringify(pack, null, 2);
  if (crlf) text = text.replace(/\n/g, "\r\n");
  if (raw.endsWith("\n") && !text.endsWith("\n")) text += crlf ? "\r\n" : "\n";
  fs.writeFileSync(file, text);
}
main();
