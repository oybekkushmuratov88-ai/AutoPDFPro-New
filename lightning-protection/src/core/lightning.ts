/**
 * Ядро расчёта зон защиты молниеотводов.
 *
 * Реализованы три методики:
 *
 *  1. RU  — РУ 9504тм-Т1 «Указания по проектированию грозозащиты подстанций
 *           напряжением 35 кВ и выше» (Энергосетьпроект), Приложение 1, стр. 56–59.
 *  2. B   — РН 34-301-388:2016, зона Б (надёжность ≥ 95 %).
 *  3. A   — РН 34-301-388:2016, зона А (надёжность ≥ 99,5 %).
 *           Формулы РН 34-301-388:2016 идентичны РД 34.21.122-87.
 *
 * Все формулы сверены с первоисточниками — см. lightning.test.ts.
 * Модуль не зависит от DOM и может использоваться в Node, браузере,
 * Electron/Tauri renderer или worker.
 */

export type Method = 'RU' | 'A' | 'B';

export interface Rod {
  id: string;
  name: string;
  /** координата X в плане, м */
  x: number;
  /** координата Y в плане, м */
  y: number;
  /** полная высота молниеотвода от земли, м */
  h: number;
}

export interface Target {
  id: string;
  name: string;
  x: number;
  y: number;
  /** высота защищаемой точки, м */
  hx: number;
}

/** Характеристики зоны одиночного молниеотвода на уровне hx. */
export interface SingleZone {
  /** высота вершины конуса зоны, м (для RU равна h) */
  h0: number;
  /** радиус зоны на уровне земли, м */
  R0: number;
  /** радиус зоны на уровне hx, м */
  Rx: number;
  /** поправочный коэффициент P (методика RU; для А/Б равен 1) */
  P: number;
}

/** Характеристики совместной зоны пары молниеотводов на уровне hx. */
export interface PairZone {
  /** расчётное расстояние между молниеотводами, м */
  L: number;
  /** наименьшая высота совместной зоны («провис»), м */
  hc: number;
  /** ширина совместной зоны на уровне земли, м */
  Rc: number;
  /** ПОЛОВИНА наименьшей ширины совместной зоны на уровне hx (bx/2), м */
  Rcx: number;
  /** предельное расстояние, при котором совместная зона разрывается, м */
  Lcr: number;
  /** смещение фиктивного молниеотвода (методика RU, разные высоты), м */
  d: number;
  /** эффективное расстояние L - d (методика RU), м */
  Leff: number;
  /** положение «талии» вдоль оси rod1→rod2 в долях L (0…1) */
  tw: number;
  /** зона между молниеотводами существует */
  joined: boolean;
  a: SingleZone;
  b: SingleZone;
}

/** Условие защищённости площади между тремя молниеотводами (стр. 58). */
export interface TripleZone {
  /** имена молниеотводов */
  rods: [string, string, string];
  /** диаметр окружности через центры трёх молниеотводов, м */
  D: number;
  /** предельный диаметр 8·ha·P, м */
  Dmax: number;
  /** условие выполнено — вся площадь треугольника защищена */
  ok: boolean;
}

export interface CoverageResult {
  covered: boolean;
  /** запас до границы зоны (>0 внутри, <0 снаружи), м; NaN если не определён */
  margin: number;
  /** чем перекрыта точка: имя молниеотвода или пары */
  by: string;
}

/* ------------------------------------------------------------------ */
/*  Одиночный молниеотвод                                              */
/* ------------------------------------------------------------------ */

/** Поправочный коэффициент P методики РУ 9504тм-Т1 (стр. 56). */
export function coefP(h: number): number {
  return h <= 30 ? 1 : 5.5 / Math.sqrt(h);
}

/**
 * Зона защиты одиночного стержневого молниеотвода на уровне hx.
 *
 * RU (п.1.1, стр. 56):   rx = 1,6·P·h·(h − hx)/(h + hx)
 * Б:  h0 = 0,92h,  R0 = 1,5h,              Rx = R0·(h0 − hx)/h0
 * А:  h0 = 0,85h,  R0 = (1,1 − 0,002h)·h,  Rx = R0·(h0 − hx)/h0
 */
export function singleZone(h: number, hx: number, m: Method): SingleZone {
  if (m === 'RU') {
    const P = coefP(h);
    let Rx = h > hx ? (1.6 * P * h * (h - hx)) / (h + hx) : 0;
    // п.1.2, стр.57: зона молниеотвода 60…250 м усечена на Δh от вершины
    const dh = rodTruncation(h);
    if (dh > 0 && h - hx < dh) Rx = 0;
    return { h0: h, R0: 1.6 * P * h, Rx: Math.max(0, Rx), P };
  }
  const h0 = m === 'B' ? 0.92 * h : 0.85 * h;
  const R0 = m === 'B' ? 1.5 * h : (1.1 - 0.002 * h) * h;
  const Rx = h0 > hx ? (R0 * (h0 - hx)) / h0 : 0;
  return { h0, R0, Rx: Math.max(0, Rx), P: 1 };
}

/**
 * Усечение вершины зоны стержневого молниеотвода (п.1.2, стр. 57).
 *   Δh = 0,5·(h − 60)   при 60 < h <= 100 м
 *   Δh = 0,2·h          при 100 < h <= 250 м
 * При ha < Δh радиус защиты обращается в нуль.
 * Для h <= 60 м усечения нет.
 */
export function rodTruncation(h: number): number {
  if (h <= 60) return 0;
  if (h <= 100) return 0.5 * (h - 60);
  if (h <= 250) return 0.2 * h;
  return NaN;   // за пределами применимости методики
}

/** Коэффициент наклона провиса для методик А/Б. */
function sagCoef(h: number, m: Method): number {
  return m === 'B' ? 0.14 : 0.17 + 0.0003 * h;
}

/* ------------------------------------------------------------------ */
/*  Пара молниеотводов                                                 */
/* ------------------------------------------------------------------ */

/**
 * Совместная зона двух молниеотводов на уровне hx.
 *
 * Методика RU (стр. 57 и 59):
 *   критерий разрыва (дословно):  a/ha · 1/P ≥ 7
 *   молниеотвод большей высоты заменяется фиктивным высотой h_min,
 *   смещённым на d = 1,6·(h1 − h2)/(1 + h2/h1)·P1;
 *   ширина совместной зоны — гиперболическая интерполяция, точная
 *   в обеих крайних точках (a = 0 → rx ; a = 7·ha·P → 0):
 *       bx/2 = 2·rx·(7·ha·P − a)/(14·ha·P − a)
 *   Сверено с номограммами листов 11 (hx = 6,0 м) и 13 (hx = 11,3 м).
 *
 * Методики А/Б:
 *   hc = h0 − k·(L − h),  k = 0,14 (Б) либо 0,17 + 3·10⁻⁴·h (А)
 *   при разных высотах hc = (hc1 + hc2)/2, Rc = (R01 + R02)/2
 *   Rcx = Rc·(hc − hx)/hc
 */
export function pairZone(
  h1: number,
  h2: number,
  L: number,
  hx: number,
  m: Method,
): PairZone {
  const a = singleZone(h1, hx, m);
  const b = singleZone(h2, hx, m);

  if (m === 'RU') {
    const hs = Math.min(h1, h2);
    const ht = Math.max(h1, h2);
    const Ps = coefP(hs);
    const Pt = coefP(ht);
    const d = ht > hs ? (1.6 * (ht - hs)) / (1 + hs / ht) * Pt : 0;
    const Leff = Math.max(0, L - d);
    const ha = hs - hx;
    const Acr = 7 * ha * Ps;
    const Lcr = Acr + d;
    const rxs = singleZone(hs, hx, m).Rx;
    const Rcx = ha > 0 && Leff < Acr ? (2 * rxs * (Acr - Leff)) / (2 * Acr - Leff) : 0;

    // эквивалентная высота провиса — обратная задача, нужна только для разреза
    let hc = hx;
    if (Rcx > 0) {
      const K = 1.6 * Ps;
      const B1 = K * hx + Rcx;
      hc = (B1 + Math.sqrt(B1 * B1 + 4 * K * Rcx * hx)) / (2 * K);
    }
    let tw = L > 0 ? (d + Leff / 2) / L : 0.5;
    if (h1 < h2) tw = 1 - tw;

    return {
      L, hc, Rc: (a.R0 + b.R0) / 2, Rcx, Lcr, d, Leff, tw,
      joined: Rcx > 0 && L <= Lcr, a, b,
    };
  }

  const hc1 = a.h0 - sagCoef(h1, m) * (L - h1);
  const hc2 = b.h0 - sagCoef(h2, m) * (L - h2);
  const hc = (hc1 + hc2) / 2;
  const Rc = (a.R0 + b.R0) / 2;
  const Rcx = hc > hx ? (Rc * (hc - hx)) / hc : 0;

  let Lcr: number;
  if (m === 'B') {
    Lcr = (0.53 * (h1 + h2) - hx) / 0.14;
  } else {
    const k = (sagCoef(h1, m) + sagCoef(h2, m)) / 2;
    const hm = (h1 + h2) / 2;
    Lcr = hm + (0.85 * hm - hx) / k;
  }

  return {
    L, hc, Rc, Rcx: Math.max(0, Rcx), Lcr, d: 0, Leff: L, tw: 0.5,
    joined: Rcx > 0 && L <= Lcr, a, b,
  };
}

/* ------------------------------------------------------------------ */
/*  Тросовые молниеотводы — Приложение 2, стр. 60                      */
/* ------------------------------------------------------------------ */

/** Допускаемая вероятность прорыва молнии в зону тросового молниеотвода. */
export type CableReliability = '1e-2' | '1e-3';

/** Коэффициенты k1, k2 (стр. 60). */
export function cableCoefs(rel: CableReliability): { k1: number; k2: number } {
  return rel === '1e-2' ? { k1: 1.2, k2: 1.0 } : { k1: 0.6, k2: 1.3 };
}

/** Усечение зоны тросового молниеотвода (П.2.3). */
export function cableTruncation(h: number, rel: CableReliability): number {
  const { k2 } = cableCoefs(rel);
  if (h <= 30) return 0;
  if (h < 100) return 0.29 * (h - 30) * k2;
  if (h <= 250) return 0.2 * h * k2;
  return NaN;
}

/**
 * Полуширина зоны защиты одиночного тросового молниеотвода на уровне hx.
 *
 * П.2.1 (h < 30 м):        rx = k1·ha / (1 + hx/h)
 * П.2.2 (30…250 м):        rx = k1·ha / (1 + hx/h) · P,   P = 5,5/sqrt(h)
 * П.2.3: зона усекается сверху на Δh; при ha < Δh защиты нет.
 *
 * Зона ограничена двумя прямыми, параллельными тросу и отстоящими
 * от вертикальной плоскости троса на rx.
 *
 * @param h  высота подвеса троса в рассматриваемой точке, м
 *           (у троса с провисом — высота именно в этой точке, не у опор)
 */
export function cableZone(h: number, hx: number, rel: CableReliability = '1e-3'): number {
  const { k1 } = cableCoefs(rel);
  const ha = h - hx;
  if (ha <= 0) return 0;
  const dh = cableTruncation(h, rel);
  if (dh > 0 && ha < dh) return 0;
  const P = h <= 30 ? 1 : 5.5 / Math.sqrt(h);
  return (k1 * ha) / (1 + hx / h) * P;
}

export interface CableRod {
  id: string;
  name: string;
  /** начало троса в плане */
  x1: number; y1: number;
  /** конец троса в плане */
  x2: number; y2: number;
  /** высота подвеса у опор, м */
  h: number;
  /** стрела провиса в середине пролёта, м (0 — трос без провиса) */
  sag?: number;
  reliability?: CableReliability;
}

/** Высота троса над точкой пролёта: параболический провис. */
export function cableHeightAt(c: CableRod, t: number): number {
  const sag = c.sag ?? 0;
  const u = 2 * t - 1;                    // -1 … +1
  return c.h - sag * (1 - u * u);
}

/** Защищена ли точка плана тросовым молниеотводом. */
export function cableCoverage(
  px: number, py: number, hx: number, c: CableRod,
): CoverageResult {
  const dx = c.x2 - c.x1, dy = c.y2 - c.y1;
  const L2 = dx * dx + dy * dy;
  if (L2 <= 0) return { covered: false, margin: NaN, by: '—' };
  const t = ((px - c.x1) * dx + (py - c.y1) * dy) / L2;
  if (t < 0 || t > 1) return { covered: false, margin: NaN, by: '—' };
  const h = cableHeightAt(c, t);
  const rx = cableZone(h, hx, c.reliability ?? '1e-3');
  const perp = Math.abs((px - c.x1) * dy - (py - c.y1) * dx) / Math.sqrt(L2);
  return perp <= rx
    ? { covered: true, margin: rx - perp, by: c.name }
    : { covered: false, margin: rx - perp, by: '—' };
}

/* ------------------------------------------------------------------ */
/*  Геометрия зоны в плане                                             */
/* ------------------------------------------------------------------ */

/**
 * Полуширина совместной зоны в произвольной точке оси между молниеотводами.
 * Парабола Лагранжа через (0, Rx1), (tw, Rcx), (1, Rx2).
 * @param t доля пути вдоль оси rod1→rod2, 0…1
 */
export function halfWidth(
  Rx1: number, Rcx: number, Rx2: number, t: number, tw = 0.5,
): number {
  const w = Math.min(0.92, Math.max(0.08, tw));
  const L0 = ((t - w) * (t - 1)) / ((0 - w) * (0 - 1));
  const L1 = (t * (t - 1)) / (w * (w - 1));
  const L2 = (t * (t - w)) / (1 * (1 - w));
  return Rx1 * L0 + Rcx * L1 + Rx2 * L2;
}

const dist = (a: { x: number; y: number }, b: { x: number; y: number }) =>
  Math.hypot(a.x - b.x, a.y - b.y);

/**
 * Условие защищённости всей площади между тремя молниеотводами
 * (РУ 9504тм-Т1, стр. 58):
 *     h <= 30 м        Д <= 8·ha
 *     30 < h < 250 м   Д <= 8·ha·P
 * где Д — диаметр окружности, проходящей через центры трёх молниеотводов.
 * При разных высотах в запас принимается наименьшая (ha по h_min).
 *
 * Применимо только к методике RU. Для зон А/Б источник такого
 * условия не даёт — там площадь перекрывается попарными зонами.
 */
export function tripleZone(A: Rod, B: Rod, C: Rod, hx: number, m: Method): TripleZone {
  const names: [string, string, string] = [A.name, B.name, C.name];
  const a = dist(B, C), b = dist(A, C), c = dist(A, B);
  const area2 = Math.abs((B.x - A.x) * (C.y - A.y) - (C.x - A.x) * (B.y - A.y));
  if (m !== 'RU' || area2 < 1e-9) {
    return { rods: names, D: Infinity, Dmax: 0, ok: false };
  }
  const D = (a * b * c) / area2;          // = abc/(2·S), S = area2/2
  const hs = Math.min(A.h, B.h, C.h);
  const ha = hs - hx;
  const Dmax = ha > 0 ? 8 * ha * coefP(hs) : 0;
  return { rods: names, D, Dmax, ok: ha > 0 && D <= Dmax };
}

/** Лежит ли точка внутри треугольника (с допуском по краю). */
function inTriangle(
  px: number, py: number,
  A: { x: number; y: number }, B: { x: number; y: number }, C: { x: number; y: number },
): boolean {
  const s = (p: { x: number; y: number }, q: { x: number; y: number }) =>
    (px - q.x) * (p.y - q.y) - (p.x - q.x) * (py - q.y);
  const d1 = s(A, B), d2 = s(B, C), d3 = s(C, A);
  const neg = d1 < 0 || d2 < 0 || d3 < 0;
  const pos = d1 > 0 || d2 > 0 || d3 > 0;
  return !(neg && pos);
}

/**
 * Защищена ли точка плана (px, py) на уровне hx.
 * Точка защищена, если попадает в зону одиночного молниеотвода либо
 * в совместную зону какой-либо пары.
 */
export function coverageAt(
  px: number, py: number, hx: number, rods: Rod[], m: Method,
): CoverageResult {
  let worst: number | null = null;

  for (const r of rods) {
    if (!(r.h > hx)) continue;
    const s = singleZone(r.h, hx, m);
    const d = Math.hypot(px - r.x, py - r.y);
    if (d <= s.Rx) return { covered: true, margin: s.Rx - d, by: r.name };
  }

  // площадь между тремя молниеотводами (стр. 58)
  if (m === 'RU') {
    for (let i = 0; i < rods.length; i++)
      for (let j = i + 1; j < rods.length; j++)
        for (let k = j + 1; k < rods.length; k++) {
          const A = rods[i], B = rods[j], C = rods[k];
          if (!(A.h > hx && B.h > hx && C.h > hx)) continue;
          const t = tripleZone(A, B, C, hx, m);
          if (t.ok && inTriangle(px, py, A, B, C)) {
            return { covered: true, margin: t.Dmax - t.D, by: `${A.name}–${B.name}–${C.name}` };
          }
        }
  }

  for (let i = 0; i < rods.length; i++) {
    for (let j = i + 1; j < rods.length; j++) {
      const A = rods[i], B = rods[j];
      if (!(A.h > hx && B.h > hx)) continue;
      const L = dist(A, B);
      if (L <= 0) continue;
      const p = pairZone(A.h, B.h, L, hx, m);
      if (!p.joined) continue;

      const dx = B.x - A.x, dy = B.y - A.y;
      const t = ((px - A.x) * dx + (py - A.y) * dy) / (L * L);
      if (t < 0 || t > 1) continue;
      const perp = Math.abs((px - A.x) * dy - (py - A.y) * dx) / L;
      const w = halfWidth(p.a.Rx, p.Rcx, p.b.Rx, t, p.tw);
      if (perp <= w) {
        return { covered: true, margin: w - perp, by: `${A.name}–${B.name}` };
      }
      const gap = perp - w;
      if (worst === null || gap < worst) worst = gap;
    }
  }

  return { covered: false, margin: worst === null ? NaN : -worst, by: '—' };
}

/* ------------------------------------------------------------------ */
/*  Пакетный расчёт                                                    */
/* ------------------------------------------------------------------ */

export interface LevelReport {
  hx: number;
  singles: Array<{ rod: string } & SingleZone>;
  pairs: Array<{ pair: string } & PairZone>;
  /** тройки молниеотводов (только методика RU) */
  triples: TripleZone[];
  targets: Array<{ target: string; hx: number } & CoverageResult>;
}

/** Полный расчёт для одного уровня hx. */
export function computeLevel(
  hx: number, rods: Rod[], targets: Target[], m: Method,
): LevelReport {
  const singles = rods.map((r) => ({ rod: r.name, ...singleZone(r.h, hx, m) }));

  const pairs: Array<{ pair: string } & PairZone> = [];
  for (let i = 0; i < rods.length; i++) {
    for (let j = i + 1; j < rods.length; j++) {
      const A = rods[i], B = rods[j];
      const L = dist(A, B);
      if (L <= 0) continue;
      pairs.push({ pair: `${A.name}–${B.name}`, ...pairZone(A.h, B.h, L, hx, m) });
    }
  }

  const triples: TripleZone[] = [];
  if (m === 'RU') {
    for (let i = 0; i < rods.length; i++)
      for (let j = i + 1; j < rods.length; j++)
        for (let k = j + 1; k < rods.length; k++)
          triples.push(tripleZone(rods[i], rods[j], rods[k], hx, m));
  }

  const tgt = targets
    .filter((t) => Math.abs(t.hx - hx) < 1e-9)
    .map((t) => ({ target: t.name, hx: t.hx, ...coverageAt(t.x, t.y, t.hx, rods, m) }));

  return { hx, singles, pairs, triples, targets: tgt };
}

/** Растеризация зон: для каждой ячейки — индекс самого высокого покрывающего уровня. */
export function rasterise(
  bounds: { x0: number; y0: number; x1: number; y1: number },
  cell: number,
  levels: number[],
  rods: Rod[],
  m: Method,
): { nx: number; ny: number; data: Int8Array } {
  const asc = [...levels].sort((a, b) => a - b);
  const nx = Math.max(1, Math.ceil((bounds.x1 - bounds.x0) / cell));
  const ny = Math.max(1, Math.ceil((bounds.y1 - bounds.y0) / cell));
  const data = new Int8Array(nx * ny).fill(-1);
  for (let iy = 0; iy < ny; iy++) {
    const y = bounds.y0 + (iy + 0.5) * cell;
    for (let ix = 0; ix < nx; ix++) {
      const x = bounds.x0 + (ix + 0.5) * cell;
      for (let k = asc.length - 1; k >= 0; k--) {
        if (coverageAt(x, y, asc[k], rods, m).covered) { data[iy * nx + ix] = k; break; }
      }
    }
  }
  return { nx, ny, data };
}

/**
 * Профиль поверхности зоны для 3D: высота границы зоны над точкой плана.
 * Возвращает наибольшую высоту, на которой точка ещё защищена
 * (бинарный поиск по hx). Используется для построения меша.
 */
export function zoneCeiling(
  px: number, py: number, rods: Rod[], m: Method, eps = 0.05,
): number {
  let lo = 0;
  let hi = Math.max(...rods.map((r) => r.h), 0);
  if (!coverageAt(px, py, lo, rods, m).covered) return 0;
  while (hi - lo > eps) {
    const mid = (lo + hi) / 2;
    if (coverageAt(px, py, mid, rods, m).covered) lo = mid; else hi = mid;
  }
  return lo;
}
