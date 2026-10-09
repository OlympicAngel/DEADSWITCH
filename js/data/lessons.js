// Lessons: me showing the commander around. Each one fires the first time its `when` holds, runs
// once, and gates the screen to a single control while it is running, so there is nothing to get
// wrong. Lessons say what a thing is and how to work it. They never say what the right move is.
//
// when   {} from the first second; { level: { core: 2 } } at a building level; { sectors: n },
//        { events: n }, { items: { militia: 1 } }: the state has to have got there.
// steps  at   a selector to point at and open a hole in the screen for (omit to just speak)
//        say  the line
//        done 'tap' the target, 'read' a Continue button, or a condition in the `when` shape
export const LESSONS = [
  {
    id: 'first', when: {},
    steps: [
      { say: 'Six survivors, one bunker, and whatever is left of me. I will run the numbers. You decide what we spend them on.', done: 'read' },
      { at: '[data-pill="money"]', say: 'Scrip, Energy and Population. Everything I build eats them. The bar under each one is how much the stores can hold; the number under that is what we are making a second.', done: 'read' },
      { at: '[data-nav="economy"]', say: 'Production first. Nothing else works without it.', done: 'tap' },
      { at: '[data-card="scrapyard"]', say: 'The Scrap Yard pulls Scrip out of the ruins. Every level of it doubles what it drags back, and costs more than the last. Upgrade it.', done: { level: { scrapyard: 2 } } },
      { at: '[data-nav="command"]', say: 'Command is where I report. Back to it.', done: 'tap' },
      { at: '[data-panel="build"]', say: 'One thing is built at a time and it takes as long as it takes. The bar is the builder.', done: 'read' },
      { at: '[data-panel="directive"]', say: 'Directives are what I need next, and they pay on delivery. Go takes you to whatever one of them wants.', done: 'read' },
    ],
  },
  {
    id: 'arsenal', when: { level: { barracks: 1 } },
    steps: [
      { at: '[data-nav="military"]', say: 'The Barracks is up, so there are people to arm.', done: 'tap' },
      { at: '[data-item="militia"]', say: 'Every unit costs more than the last one did, for ever. Buy some militia.', done: { items: { militia: 1 } } },
      { at: '.reactor', say: 'They add up to two numbers. Power takes ground. Defense keeps it. Threat is both of them together, and it is what the wasteland sees when it looks at us.', done: 'read' },
    ],
  },
  {
    id: 'war', when: { level: { core: 2 } },
    steps: [
      { say: 'Core two. They can hear us now. Raids will start, and the clans on our border will come for the ground we hold.', done: 'read' },
      { at: '[data-panel="raid"]', say: 'This is what is coming, where it is coming from, and what it is worth against our Defense. Hold chance is the odds, and I do not round them in our favour.', done: 'read' },
      { at: '[data-nav="map"]', say: 'Every attack starts somewhere real. So does ours.', done: 'tap' },
      { at: '.map-viewport', say: 'Drag the world, pinch to zoom. A sector next to ours can be taken; the number over it is what holds it.', done: 'read' },
    ],
  },
  {
    id: 'ops', when: { sectors: 1 },
    steps: [
      { at: '[data-nav="command"]', say: 'Ground. Come back to Command and I will tell you what it is worth.', done: 'tap' },
      { at: '[data-panel="op"]', say: 'An operation runs while you do everything else, and I report what it cost when it lands.', done: 'read' },
      { say: 'Every sector we hold pays a permanent bonus for as long as we hold it, and the first time we take one it gives up a piece of what I used to be. Both are in the Archive.', done: 'read' },
    ],
  },
  {
    id: 'orders', when: { events: 1 },
    steps: [
      { at: '[data-panel="events"]', say: 'Transmissions are decisions with a clock on them. Both answers cost something; that is what makes them decisions.', done: 'read' },
      { say: 'If the clock runs out I choose for you, and I choose what the situation leaves me. Answer them.', done: 'read' },
    ],
  },
];
