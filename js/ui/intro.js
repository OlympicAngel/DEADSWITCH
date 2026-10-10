// The opening's stage: a full-screen set of layers with a camera in front of them. Every scene is a
// hard cut that moves the camera, lights different layers and restarts their animations, so the
// sequence plays like a cut scene rather than a slideshow. The beats themselves are in
// js/data/story.js and the score is js/ui/score.js.

const DUST = 34; // drifting motes in the ruins
const SHARDS = 16; // debris thrown at the camera by the raid
const RINGS = 9; // rings of the shaft the camera flies down

const rnd = (a, b) => a + Math.random() * (b - a);

// A ruined skyline, drawn as blocks: [left%, width%, height%, depth]. Depth drives the parallax.
const CITY = [
  [-4, 13, 38, 1], [9, 8, 54, 1], [17, 11, 30, 1], [27, 7, 62, 1], [33, 14, 44, 1],
  [46, 9, 72, 1], [54, 12, 36, 1], [65, 8, 58, 1], [72, 15, 46, 1], [86, 10, 66, 1], [95, 12, 34, 1],
];
const FAR_CITY = [
  [-6, 20, 26, 0], [14, 16, 40, 0], [30, 22, 22, 0], [50, 18, 34, 0], [68, 24, 28, 0], [88, 18, 44, 0],
];

const block = ([x, w, h, near]) => `<i class="blk${near ? '' : ' far'}" style="left:${x}%;width:${w}%;height:${h}%"></i>`;

/** The whole stage, built once. Nothing is added or removed later, only lit and moved. */
export function introStage() {
  const dust = [...Array(DUST)].map(() => `<i style="left:${rnd(-5, 105).toFixed(1)}%;top:${rnd(0, 100).toFixed(1)}%;--s:${rnd(0.4, 1.5).toFixed(2)};--d:${rnd(7, 20).toFixed(1)}s;--o:${rnd(-6, 6).toFixed(1)}s"></i>`).join('');
  const shards = [...Array(SHARDS)].map((_, k) => `<i style="--a:${(k * 360) / SHARDS + rnd(-9, 9) | 0}deg;--d:${rnd(0, 260).toFixed(0)}ms;--r:${rnd(0.6, 1.3).toFixed(2)}"></i>`).join('');
  const rings = [...Array(RINGS)].map((_, k) => `<i style="--i:${k};--d:${(k * -220).toFixed(0)}ms"></i>`).join('');
  return `
    <div class="intro" data-scene="gate">
      <div class="cam">
        <div class="lay sky"></div>
        <div class="lay city far">${FAR_CITY.map(block).join('')}</div>
        <div class="lay city near">${CITY.map(block).join('')}</div>
        <div class="lay dust">${dust}</div>
        <div class="lay shaft">${rings}</div>
        <div class="lay orb"><span class="halo"></span><span class="ring r1"></span><span class="ring r2"></span><span class="dot"></span><span class="sat"></span></div>
        <div class="lay people">${'<i></i>'.repeat(6)}</div>
        <div class="lay shards">${shards}</div>
        <div class="lay wave"><i></i><i></i><i></i></div>
        <h1 class="word"><span>DEADSWITCH</span><span class="ghost gr">DEADSWITCH</span><span class="ghost gc">DEADSWITCH</span></h1>
      </div>
      <div class="tears">${'<i></i>'.repeat(5)}</div>
      <div class="hline"></div>
      <div class="grain"></div>
      <div class="scan"></div>
      <div class="vig"></div>
      <div class="flash"></div>
      <div class="bars"><i></i><i></i></div>
    </div>`;
}

// Where the camera stands for each scene: a transform on the whole set, and how long it takes to
// drift there. The cut itself is instant; this is the slow move that happens after it.
const SHOT = {
  gate: ['scale(1.12)', 6000],
  dead: ['scale(1.3) translate3d(2%, -1%, 0)', 9000],
  surge: ['scale(1.04)', 2400],
  shaft: ['scale(1.5)', 5000],
  title: ['scale(1)', 4200],
  ruins: ['scale(1.25) translate3d(-7%, 2%, 0)', 11000],
  ruins2: ['scale(1.45) translate3d(6%, -3%, 0)', 11000],
  core: ['scale(1.35)', 9000],
  crowd: ['scale(1.45) translate3d(4%, -5%, 0)', 10000],
  raid: ['scale(1.18)', 3000],
  ready: ['scale(1)', 7000],
};
const FROM = {
  dead: 'scale(1.1) translate3d(-3%, 1%, 0)',
  surge: 'scale(1.4)',
  shaft: 'scale(0.75)',
  title: 'scale(1.9)',
  ruins: 'scale(1.1) translate3d(6%, -2%, 0)',
  ruins2: 'scale(1.22) translate3d(-8%, 2%, 0)',
  core: 'scale(1.05)',
  crowd: 'scale(1.15) translate3d(-5%, -9%, 0)',
  raid: 'scale(1.9)',
  ready: 'scale(1.5)',
};

/** Cuts to a scene: snaps the camera to its start, then lets it drift to the end of the shot. */
export function playScene(root, id) {
  const cam = root.querySelector('.cam');
  const [to, ms] = SHOT[id] || SHOT.gate;
  root.dataset.scene = id;
  // Restart every animation keyed off the scene by taking the attribute away for one frame.
  cam.style.transition = 'none';
  cam.style.transform = FROM[id] || to;
  void cam.offsetWidth;
  cam.style.transition = `transform ${ms}ms cubic-bezier(.22, .7, .3, 1)`;
  cam.style.transform = to;
}

/** Knocks the whole frame sideways once, for the beats that land hard. */
export function frameHit(root) {
  root.classList.remove('hit');
  void root.offsetWidth;
  root.classList.add('hit');
}
