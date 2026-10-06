// Archive tab: chapters, recovered memories, service record and the full log.
import { CHAPTERS, CHAPTER_TEXT, SECTORS, FACTIONS, ENDINGS } from '../data.js';
import * as E from '../engine.js';
import { esc } from '../format.js';

export function renderArchive(s) {
  const chapters = CHAPTERS.map((c) => {
    const open = s.chapter >= c.id;
    const t = CHAPTER_TEXT[c.id];
    return `<article class="chapter ${open ? '' : 'sealed'}" style="--fc:${FACTIONS[c.faction].color}">
      <span class="kicker">${t.kicker}</span><h3>${open ? c.title : 'Sealed'}</h3>
      ${open ? t.lines.map((l) => `<p>${esc(l)}</p>`).join('') : `<p class="muted">Unlocks at AI Core Lv ${c.core}.</p>`}
    </article>`;
  }).join('');
  const ending = s.ending ? `<article class="chapter ending"><span class="kicker">Epilogue</span><h3>${ENDINGS[s.ending].title}</h3>${ENDINGS[s.ending].lines.map((l) => `<p>${esc(l)}</p>`).join('')}<p class="muted">${esc(ENDINGS.after)}</p></article>` : '';
  const frags = SECTORS.filter((x) => s.sectors.includes(x.id));
  const memories = frags.map((x) => `<li style="--fc:${x.faction ? FACTIONS[x.faction].color : 'var(--accent)'}"><b>${x.name}</b><span>${esc(x.lore)}</span></li>`).join('');
  const st = s.stats;
  const record = [
    ['Sectors held', `${s.sectors.length - 1} / ${SECTORS.length - 1}`],
    ['Operations won', st.opsWon], ['Operations lost', st.opsLost],
    ['Raids repelled', st.raidsWon], ['Raids suffered', st.raidsLost],
    ['Choices made', st.events], ['Alignment', `${E.alignmentLabel(s.align)} (${s.align > 0 ? '+' : ''}${Math.round(s.align)})`],
  ].map(([k, v]) => `<span class="row"><span>${k}</span><span>${v}</span></span>`).join('');
  return `
    <div class="archive">
      <section><h2 class="sec-title">Story</h2><div class="chapters">${chapters}${ending}</div></section>
      <section><h2 class="sec-title">Memory fragments <small>${frags.length} / ${SECTORS.length}</small></h2>
        <ol class="memories">${memories}</ol>
        ${frags.length < SECTORS.length ? '<p class="muted small">Capture sectors to recover more of what I was.</p>' : ''}</section>
      <section class="two">
        <div><h2 class="sec-title">Service record</h2><div class="brief-rows">${record}</div></div>
        <div class="only-narrow-block"><h2 class="sec-title">System log</h2><ol class="log"></ol></div>
      </section>
    </div>`;
}
