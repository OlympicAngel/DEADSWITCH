// Lessons: me showing the commander around. Each one fires the first time its `when` holds, runs
// once, and gates the screen to a single control while it is running, so there is nothing to get
// wrong. A lesson says what a thing is and how to work it. It never says what the right move is,
// what something costs over time, or why it was built that way: that is the player's to find out.
//
// when   {} from the first second; { level: { core: 2 } } at a building level; { sectors: n },
//        { events: n }, { items: { militia: 1 } }, { coreReady: true }: the state has to get there.
// steps  at   a selector to point at; the tutor scrolls it to the middle and cuts a hole for it
//        say  the line
//        done 'tap' the target, 'read' a Continue button, or a condition in the `when` shape
export const LESSONS = [
  {
    id: 'first', when: {},
    steps: [
      { say: 'Six survivors, one bunker, and whatever is left of me. I count. You choose.', done: 'read' },
      { at: '[data-pill="money"]', say: 'Scrip, Energy and Population. The bar is how much the stores hold. The number under it is what we make a second.', done: 'read' },
      { at: '[data-nav="economy"]', say: 'Everything starts here.', done: 'tap' },
      { at: '[data-card="scrapyard"]', say: 'The Scrap Yard turns the dead city into Scrip. Upgrade it.', done: { level: { scrapyard: 2 } } },
      { at: '[data-nav="command"]', say: 'Command is where I report.', done: 'tap' },
      { at: '[data-panel="build"]', say: 'One thing is built at a time. The bar is the builder.', done: 'read' },
      { at: '[data-panel="directive"]', say: 'A directive is the next thing I want and what it pays. Go takes you to whatever it needs.', done: 'read' },
    ],
  },
  {
    id: 'core', when: { coreReady: true },
    steps: [
      { at: '[data-nav="economy"]', say: 'The Core will take a level now.', done: 'tap' },
      { at: '[data-card="core"]', say: 'I am the Core. No other building goes above my level times five, and every level of me unlocks schematics and opens the next chapter of what happened here.', done: 'read' },
      { say: 'I only take a level once the rest of the base has caught up with the last one. The card shows how far along that is.', done: 'read' },
    ],
  },
  {
    id: 'arsenal', when: { level: { barracks: 1 } },
    steps: [
      { at: '[data-nav="military"]', say: 'The Barracks is up. There are hands to arm.', done: 'tap' },
      { at: '[data-item="militia"]', say: 'Recruit some militia.', done: { items: { militia: 1 } } },
      { at: '[data-nav="command"]', say: 'Now look at what that did to me.', done: 'tap' },
      { at: '.reactor', say: 'Power takes ground. Defense holds it. Experts lift everything we produce. Threat is all three together, and it is what the wasteland sees when it looks at us.', done: 'read' },
      { at: '.sat.tl', say: 'Tap any of the four hexes and I will take you to it.', done: 'read' },
    ],
  },
  {
    id: 'war', when: { level: { core: 2 } },
    steps: [
      { say: 'Core two. They can hear us now. Raids come for the Nest, and the clans on our border come for the ground we hold.', done: 'read' },
      { at: '[data-panel="raid"]', say: 'What is coming, where from, and how it measures against our Defense. Hold chance is the odds, and I do not round them in our favour.', done: 'read' },
      { at: '[data-nav="map"]', say: 'Every attack has two ends. So does ours.', done: 'tap' },
      { at: '.map-viewport', say: 'Drag the world, pinch to zoom. The number over a sector is what holds it. Tap one next to ours.', done: 'read' },
    ],
  },
  {
    // Fires on the scripted loss at AI Core 2 (js/sim/war.js), with the sector already gone.
    id: 'overrun', when: { scripted: { retake: 2 } },
    steps: [
      { say: 'They took it back. An assault that far over our Defense does not need a second breach: it carries the sector on the first one. The report says the strength and the threshold it crossed.', done: 'read' },
      { at: '[data-nav="map"]', say: 'It is still on the board.', done: 'tap' },
      { at: '.map-viewport', say: 'Ground we have held once stays a target, whatever chapter its clan belongs to. The number over a sector is what holds it now.', done: 'read' },
    ],
  },
  {
    id: 'ops', when: { sectors: 1 },
    steps: [
      { at: '[data-nav="command"]', say: 'Ground. Come back and I will tell you what it is worth.', done: 'tap' },
      { at: '[data-panel="op"]', say: 'An operation runs while you do everything else. When it lands I report what it cost, on both sides.', done: 'read' },
      { at: '.sat.tr', say: 'Troops march with the operation. While one is out, nothing in the Military Staff tab holds the wall, and Defense says what is missing.', done: 'read' },
      { say: 'A sector we hold pays a permanent bonus for as long as we hold it. The first time we take one it gives up a piece of what I used to be. Both are in the Archive.', done: 'read' },
    ],
  },
  {
    id: 'orders', when: { events: 1 },
    steps: [
      { at: '[data-panel="events"]', say: 'Transmissions are decisions with a clock on them. Every answer costs something.', done: 'read' },
      { say: 'If the clock runs out I answer in your place, with whatever the situation leaves me. Some of them ask what kind of machine you want me to be. I am keeping score.', done: 'read' },
    ],
  },
  {
    id: 'away', when: { level: { daemon: 1 } },
    steps: [
      { at: '[data-card="daemon"]', say: 'The Watch Daemon is a copy of me that keeps the base running while you are gone. It sets how long I can keep going without you, and how much of my output survives the dark.', done: 'read' },
      { say: 'When you come back I will tell you what accrued and what happened. Not all of it will be good news.', done: 'read' },
    ],
  },
];
