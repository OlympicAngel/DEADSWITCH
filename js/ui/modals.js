// Dialogs: boot, chapters, endings, battle reports, forced events, missed orders, offline report, settings.
import {
  RESOURCES, FACTIONS, BOOT, CHAPTER_TEXT, CHAPTERS, ENDINGS, ITEM_BY_ID, BALANCE, BY_ID, SHOP_TABS, SECTORS, NODES,
} from '../data.js';
import * as E from '../engine.js';
import { num, time, pct, esc } from '../format.js';
import { icon, labeled } from './icons.js';
import { tags, bonusText, bonusChips, chanceClass, clock, factorTag } from './common.js';
import { mapBackdrop } from './minimap.js';
import { sfx, isMuted, setMuted } from './sfx.js';
import { shake, burst, vibrate, hapticsOn, setHaptics, screenFlash } from './fx.js';
import { setFocus } from './focus.js';

export function createModals(dialog, game, onChange) {
  let onClose = null;
  let typing = null;
  let finished = true;
  let ticker = null;

  // Runs once per modal. The native 'close' event is async, so by the time it fires a follow-up
  // modal may already be open; in that case it belongs to the old modal and is ignored.
  function finish() {
    if (finished) {
      return;
    }
    finished = true;
    stopTyping(true);
    clearInterval(ticker);
    const cb = onClose;
    onClose = null;
    dialog.className = '';
    if (cb) {
      cb();
    }
    onChange();
  }

  dialog.addEventListener('close', () => {
    if (!dialog.open) {
      finish();
    }
  });
  dialog.addEventListener('cancel', (e) => {
    // Story beats and reports must be acknowledged with the button.
    if (dialog.classList.contains('locked')) {
      e.preventDefault();
    }
  });
  dialog.addEventListener('click', (e) => {
    if (typing && !e.target.closest('button')) {
      stopTyping(true);
    }
    if (e.target.closest('[data-close]')) {
      dialog.close();
      finish();
    }
  });

  function open(html, { cls = '', then = null } = {}) {
    stopTyping(true);
    clearInterval(ticker);
    finished = false;
    dialog.innerHTML = `<div class="modal-body">${html}</div>`;
    dialog.className = cls;
    onClose = then;
    if (!dialog.open) {
      dialog.showModal();
    }
    dialog.scrollTop = 0; // showModal focuses the last button, which would scroll a long dialog past its title
  }

  function close() {
    if (dialog.open) {
      dialog.close();
      finish();
    }
  }

  // Types lines into el one character at a time; a tap finishes instantly.
  function typeLines(el, lines, done) {
    const paras = lines.map((l) => {
      const p = document.createElement('p');
      p.className = l.startsWith('>') ? 'term' : '';
      el.appendChild(p);
      return [p, l];
    });
    let li = 0;
    let ci = 0;
    const finishTyping = () => {
      paras.forEach(([p, l]) => { p.textContent = l; });
      el.classList.add('typed');
      typing = null;
      if (done) {
        done();
      }
    };
    const tick = () => {
      if (li >= paras.length) {
        finishTyping();
        return;
      }
      const [p, l] = paras[li];
      ci++;
      p.textContent = l.slice(0, ci);
      if (ci % 3 === 0) {
        sfx.type();
      }
      if (ci >= l.length) {
        li++;
        ci = 0;
        typing.t = setTimeout(tick, l === '' ? 100 : l.startsWith('>') ? 90 : 320);
      } else {
        typing.t = setTimeout(tick, l.startsWith('>') ? 8 : 18);
      }
    };
    typing = { finish: finishTyping, t: setTimeout(tick, 400) };
  }

  function stopTyping(complete) {
    if (!typing) {
      return;
    }
    clearTimeout(typing.t);
    const t = typing;
    typing = null;
    if (complete) {
      t.finish();
    }
  }

  // ---------- story ----------

  function cinematic(inner, lines, then, btn) {
    open(`${inner}<div class="typed-lines" id="tl"></div><button class="btn primary wide reveal" data-close>${btn}</button>`, { cls: 'cinematic locked', then });
    sfx.story();
    typeLines(dialog.querySelector('#tl'), lines, () => dialog.querySelector('.reveal').classList.add('in'));
  }

  // Callsign field + confirm; used at the end of the boot sequence and for older saves.
  const nameForm = (btn) => `
    <form class="callsign reveal" id="callsign">
      <label for="cs">Callsign</label>
      <input id="cs" maxlength="16" autocomplete="off" spellcheck="false" placeholder="Name your commander" required>
      <button class="btn primary wide" type="submit" disabled>${btn}</button>
    </form>`;

  function bindName(then) {
    const form = dialog.querySelector('#callsign');
    const input = form.querySelector('input');
    const btn = form.querySelector('button');
    input.addEventListener('input', () => { btn.disabled = !input.value.trim(); });
    form.addEventListener('submit', (e) => {
      e.preventDefault();
      if (!input.value.trim()) return;
      game.act.setName(input.value);
      sfx.click();
      dialog.close();
      finish();
    });
    return () => { form.classList.add('in'); input.focus(); };
  }

  function showBoot(then) {
    open(`<div class="boot-core"><i></i><i></i><i></i></div><h2 class="boot-title">DEADSWITCH</h2>
      <div class="typed-lines" id="tl"></div>${nameForm(`${icon('command')}Take command`)}`, { cls: 'cinematic locked', then });
    sfx.story();
    const reveal = bindName();
    typeLines(dialog.querySelector('#tl'), BOOT, reveal);
  }

  function showName(then) {
    open(`<span class="kicker">${icon('command')}Identify</span><h2>Who holds command?</h2>${nameForm('Confirm')}`, { cls: 'cinematic locked', then });
    bindName()();
  }

  function showChapter(id, then) {
    const c = CHAPTERS.find((x) => x.id === id);
    const t = CHAPTER_TEXT[id];
    const f = FACTIONS[c.faction];
    cinematic(`<div class="chapter-screen" style="--fc:${f.color}">
        <span class="kicker">${t.kicker}</span><h2 class="chapter-title">${c.title}</h2>
        <div class="enemy">${icon(f.icon)}<div><b>${f.name}</b><p>${esc(f.desc)}</p></div></div></div>`, t.lines, then, 'Continue');
  }

  function showEnding(key, then) {
    const end = ENDINGS[key];
    cinematic(`<div class="chapter-screen"><span class="kicker">Epilogue</span><h2 class="chapter-title">${end.title.replace('Ending: ', '')}</h2></div>`,
      [...end.lines, '', ENDINGS.after], then, 'Keep building');
  }

  // ---------- battle reports ----------

  function battle(r, then) {
    const max = Math.max(r.you, r.them, 1);
    open(`
      <div class="battle ${r.mode} ${r.win ? 'win' : 'loss'} ${r.shot ? 'has-map' : ''}">
        ${r.shot || ''}
        <div class="battle-banner">${icon(r.mode === 'op' ? 'power' : 'defense')}<span>${r.kicker}</span></div>
        <div class="modal-head"><h2>${esc(r.title)}</h2></div>
        <div class="versus">
          <div class="side you"><span>${icon(r.youIcon)}${r.youLabel}</span><b>${num(r.you)}</b><div class="vbar"><i style="--w:${pct(r.you / max)}"></i></div></div>
          <div class="vs">VS</div>
          <div class="side them"><span>${icon(r.themIcon)}${r.themLabel}</span><b>${num(r.them)}</b><div class="vbar"><i style="--w:${pct(r.them / max)}"></i></div></div>
        </div>
        <div class="roll"><div class="zone bg-${chanceClass(r.chance)}" style="width:${pct(r.chance)}"></div><i class="needle" style="--to:${(r.roll * 100).toFixed(1)}%"></i></div>
        <p class="roll-cap"><span>Success ${pct(r.chance)}</span><span>Roll ${Math.round(r.roll * 100)}</span></p>
        <div class="stamp ${r.stampCls || ''}">${r.stamp}</div>
        <div class="spoils">${r.spoils}</div>
        ${r.note ? `<p class="note">${r.note}</p>` : ''}
        <button class="btn primary wide" data-close>Continue</button>
      </div>`, { cls: 'report locked', then });
    sfx.launch();
    setTimeout(() => {
      if (!dialog.open) {
        return;
      }
      if (r.win) {
        sfx.win();
        vibrate(40);
        burst(dialog.querySelector('.stamp'), 'var(--ok)', 22);
      } else {
        sfx.lose();
        vibrate([200, 80, 300]);
        shake(true);
        screenFlash('alert');
      }
    }, 1500);
  }

  // What a battle cost me: stockpiles taken, the Power or Defense that died with the units, then the units.
  function losses(r) {
    const parts = [];
    if (r.lost) parts.push(tags(r.lost, '−'));
    for (const [k, v] of Object.entries(r.cost || {})) {
      if (v) parts.push(factorTag(k, v, '−'));
    }
    for (const [id, n] of Object.entries(r.units || {})) parts.push(`<span class="tag t-bad tap-name" data-label="${esc(ITEM_BY_ID[id].name)}">${icon(id)}−${num(n)}</span>`);
    if (r.staffLost) parts.push(`<span class="tag t-bad">${icon('militia')}−${num(r.staffLost)} troops</span>`); // reports saved before per-unit losses
    return parts.join('') || 'None';
  }

  // Both sides' butcher's bill. `theirs` names what the attacker's losses are counted in.
  function lossRows(r, theirs) {
    const mine = losses(r);
    return `${mine === 'None' ? '' : `<div class="r losses"><span>${icon('skull')}Our losses</span><b class="loss-list">${mine}</b></div>`}
      ${r.theirLoss ? `<div class="r"><span>${icon('skull')}Their losses</span><b class="good-t">~${num(r.theirLoss)} ${theirs}</b></div>` : ''}`;
  }

  function showOp(r, then) {
    const s = game.state;
    const sec = E.sectorById(r.sector);
    const from = sec.links.find((l) => s.sectors.includes(l));
    battle({
      mode: 'op', stamp: r.win ? 'Captured' : 'Repelled',
      kicker: 'Our offensive', title: sec.name,
      shot: mapBackdrop(s, [sec.id, from].filter(Boolean), { beam: from ? [from, sec.id] : null }),
      youLabel: 'Your power', youIcon: 'power', you: r.power,
      themLabel: 'Their defense', themIcon: 'defense', them: r.defense,
      chance: r.chance, roll: r.roll, win: r.win,
      spoils: `${r.win
        ? `${r.retaken ? `<div class="r"><span>${icon('check')}Retaken</span><b class="muted">No spoils</b></div>` : `<div class="r"><span>${icon('spark')}Spoils</span><b>${tags(r.loot, '+')}</b></div>`}
           ${sec.bonus ? `<div class="perm"><span>${icon('trend')}Permanent bonus</span>${bonusChips(sec.bonus)}</div>` : ''}`
        : `${r.strength ? `<div class="r"><span>${icon('trend')}${esc(sec.name)} strength</span><b class="bad-t">×${r.strength.toFixed(2)}</b></div>` : ''}`}
        ${lossRows(r, 'defense')}
        ${r.win && !r.retaken ? `<blockquote class="lore">${esc(sec.lore)}</blockquote>` : ''}`,
    }, then);
  }

  function showRaid(r, then) {
    const f = FACTIONS[r.faction];
    const place = r.assault && E.sectorById(r.target) ? esc(E.sectorById(r.target).name) : '';
    battle({
      mode: 'def', stamp: r.win ? 'Held' : r.overrun ? 'Overrun' : r.plundered ? 'Plundered' : 'Breached',
      stampCls: r.overrun ? 'overrun' : '',
      kicker: r.offline ? 'Attacked while away' : 'Under attack', title: r.name || f.raidName,
      shot: r.assault ? mapBackdrop(game.state, [r.target, r.from], { beam: [r.from, r.target] }) : '',
      youLabel: 'Your defense', youIcon: 'defense', you: r.defense,
      themLabel: 'Their strength', themIcon: 'power', them: r.strength,
      chance: r.chance, roll: r.roll, win: r.win,
      spoils: `${r.win ? `<div class="r"><span>${icon('spark')}Salvage</span><b>${tags(r.loot, '+')}</b></div>` : ''}
        ${r.assault && r.strengthAfter ? `<div class="r"><span>${icon('trend')}${esc(E.sectorById(r.from).name)} strength</span><b class="${r.win ? 'good-t' : 'bad-t'}">×${r.strengthAfter.toFixed(2)}</b></div>` : ''}
        ${!r.win && r.assault && !r.plundered ? `<div class="r"><span>${icon('map')}${place}</span><b class="bad-t">${r.fell ? 'Lost' : `Breach ${r.breaches} / ${NODES.breachesToFall}`}</b></div>` : ''}
        ${lossRows(r, 'strength')}`,
      note: r.win ? '' : r.overrun
        ? `${icon('skull')}${place} fell at once: they came with ${(r.strength / Math.max(1, r.defense)).toFixed(1)}× your Defense, and from ${NODES.overrunRatio}× a sector falls on the first breach.`
        : r.plundered ? `${icon('money')}The stores at ${place} are empty. The walls held.`
          : r.assault ? (r.fell ? `${icon('fire')}${place} has fallen.` : '') : `${icon('fire')}Damage reports incoming.`,
    }, then);
  }

  // ---------- events ----------

  function preview(out, ch, inst) {
    const p = [];
    p.push(tags(out.cost, '−'), tags(out.lose, '−'), tags(out.gain, '+'));
    for (const id of out.levels) {
      p.push(`<span class="tag t-bad">${icon(id)}−1 Lv ${BY_ID[id].name}</span>`);
    }
    for (const [tab, n] of Object.entries(out.units)) {
      if (n) p.push(`<span class="tag t-bad">${icon(tab === 'staff' ? 'militia' : tab === 'weapons' ? 'power' : 'defense')}−${num(n)} ${SHOP_TABS.find((t) => t.id === tab).name.toLowerCase()}</span>`);
    }
    if (ch.items) {
      p.push(Object.entries(ch.items).map(([id, n]) => `<span class="tag t-good">${icon(id)}+${n} ${ITEM_BY_ID[id].name}</span>`).join(''));
    }
    const buff = out.buff || ch.buff;
    if (buff) {
      p.push(`<span class="tag ${buff.amount < 0 ? 't-bad' : 't-good'}">${icon(buff.key)}${bonusText({ [buff.key]: buff.amount })} · ${time(buff.duration)}</span>`);
    }
    if (out.callOff) {
      p.push(`<span class="tag t-good">${icon('check')}Attack called off</span>`);
    }
    if (ch.raidDelay) {
      p.push(`<span class="tag ${ch.raidDelay > 0 ? 't-good' : 't-bad'}">${icon('threat')}Next raid ${ch.raidDelay > 0 ? 'later' : 'sooner'} (${time(Math.abs(ch.raidDelay))})</span>`);
    }
    if (out.grudge) {
      const f = FACTIONS[out.grudge.faction];
      p.push(`<span class="tag t-bad">${icon(f.icon)}${f.short} vengeance: raids ×${out.grudge.mult}</span>`);
    }
    const sname = (id) => esc((E.sectorById(id) || { name: '?' }).name);
    if (out.strength) {
      p.push(`<span class="tag ${out.strength.delta < 0 ? 't-good' : 't-bad'}">${icon('defense')}${sname(out.strength.id)} strength ${out.strength.delta > 0 ? '+' : '−'}${pct(Math.abs(out.strength.delta))}</span>`);
    }
    if (out.clearMarks) {
      p.push(`<span class="tag t-good">${icon('check')}${sname(out.clearMarks)} breaches cleared</span>`);
    }
    if (out.assault) {
      p.push(`<span class="tag t-bad">${labeled('power')}${sname(out.assault.from)} attacks in ${time(out.assault.delay)}</span>`);
    }
    if (out.cede) {
      p.push(`<span class="tag t-bad">${icon('map')}Lose ${sname(out.cede)}</span>`);
    }
    const al = out.align ?? ch.align;
    if (al) {
      p.push(`<span class="tag ${al > 0 ? 't-hum' : 't-mach'}">${icon('heart')}Humanity ${al > 0 ? '+' : ''}${al}</span>`);
    }
    const html = p.filter(Boolean).join('');
    return html || '<span class="tag t-dim">No immediate effect</span>';
  }

  function showEvent(uid) {
    const s = game.state;
    const inst = (uid && s.events.find((x) => x.uid === uid)) || s.events[0];
    if (!inst) {
      return;
    }
    const ev = E.eventById(inst.id);
    setFocus('order', inst.uid);
    // Orders about a border open with a close-up of it, so the names in the text have a place.
    const where = [(inst.params || {}).held, (inst.params || {}).node].filter((id) => E.sectorById(id));
    const choices = ev.choices.map((ch, i) => `
      <button class="choice ${i === ev.def ? 'is-default' : ''}" data-choice="${i}">
        <b>${esc(E.fillText(inst, ch.label))}</b><span class="fx">${preview(E.choiceOutcome(s, inst, ch), ch, inst)}</span>
      </button>`).join('');
    const shot = where.length ? mapBackdrop(s, where) : '';
    open(`
      <div class="event ${ev.aftermath ? 'crisis' : ''} ${ev.urgent ? 'urgent' : ''} ${shot ? 'has-map' : ''}">
        ${shot}
        <div class="modal-head">
          <div class="ev-top"><span class="kicker">${icon(ev.aftermath ? 'fire' : ev.urgent ? 'alert' : 'message')}${ev.aftermath ? 'Damage report' : ev.urgent ? 'Urgent' : 'Incoming transmission'}</span>
            <span class="deadline" data-deadline></span></div>
          <div class="deadbar"><i data-deadbar></i></div>
          <h2>${esc(E.fillText(inst, ev.title))}</h2>
        </div>
        <p class="ev-text">${esc(E.fillText(inst, ev.text))}</p>
        ${ev.threat ? `<div class="threat-box">
          <div class="vs-row them"><span>${icon('power')}Their force</span><div class="vbar"><i data-tfbar></i></div><b>${num(inst.params.strength)}</b></div>
          <div class="vs-row you"><span>${icon('defense')}Your defense</span><div class="vbar"><i data-tdbar></i></div><b data-tdef></b></div>
          <div class="threat-hold">Hold if attacked <b data-thold></b></div>
        </div>` : ''}
        <div class="choices">${choices}</div>
        <p class="default-note">${ev.def === null
    ? `${icon('alert')}They attack when this runs out.`
    : `${icon('alert')}No order in time and I choose: <b>${esc(E.fillText(inst, ev.choices[ev.def].label))}</b>`}</p>
        ${ev.urgent ? '' : `<button class="btn ghost wide" data-close>${icon(ev.def === null ? 'close' : 'hourglass')}${ev.def === null ? 'Ignore' : 'Decide later'}</button>`}
      </div>`, { cls: 'event-modal' + (ev.aftermath ? ' crisis' : '') + (ev.urgent ? ' urgent locked' : '') });
    if (ev.aftermath || ev.urgent) {
      sfx.alarm();
      vibrate([120, 60, 120]);
    } else {
      sfx.event();
      vibrate(60);
    }
    const refresh = () => {
      const live = game.state.events.find((x) => x.uid === inst.uid);
      if (!live) {
        close();
        return;
      }
      if (ev.threat) {
        const def = E.factors(game.state).defense;
        const str = inst.params.strength;
        const mx = Math.max(def, str, 1);
        const hold = E.raidChance(game.state, { strength: str });
        dialog.querySelector('[data-tfbar]').style.width = pct(str / mx);
        dialog.querySelector('[data-tdbar]').style.width = pct(def / mx);
        dialog.querySelector('[data-tdef]').textContent = num(def);
        const h = dialog.querySelector('[data-thold]');
        h.textContent = pct(hold);
        h.className = 'chance-' + chanceClass(hold);
      }
      const dl = dialog.querySelector('[data-deadline]');
      dl.innerHTML = `${labeled('hourglass')}${clock(live.left)}`;
      dl.classList.toggle('urgent', live.left < 600);
      dialog.querySelector('[data-deadbar]').style.width = pct(live.left / live.total);
      dialog.querySelectorAll('[data-choice]').forEach((b) => {
        b.disabled = !E.canChoose(game.state, live, ev.choices[Number(b.dataset.choice)]);
      });
    };
    refresh();
    ticker = setInterval(refresh, 500);
    dialog.querySelectorAll('[data-choice]').forEach((b) => b.addEventListener('click', () => {
      const res = game.act.choose(inst.uid, Number(b.dataset.choice));
      if (!res) {
        refresh();
        return;
      }
      sfx.click();
      if (res.out.levels.length) {
        shake();
        vibrate(80);
      }
      open(`
        <div class="event ${ev.aftermath ? 'crisis' : ''}">
          <span class="kicker">${icon('check')}${esc(E.fillText(inst, ev.title))}</span>
          <h2>${esc(res.label)}</h2>
          <p class="ev-text result">${esc(res.result)}</p>
          <div class="outcome"><span class="kicker">Outcome</span><div class="fx-line">${preview(res.out, res.choice, inst)}</div></div>
          <button class="btn primary wide" data-close>Continue</button>
        </div>`, { cls: 'event-modal' + (ev.aftermath ? ' crisis' : '') });
    }));
  }

  // What the decision actually changed; older saved reports carry no outcome data.
  function outcome(m) {
    const ev = m.id && E.eventById(m.id);
    const ch = ev && ev.choices[m.choice];
    if (!ch || !m.out) {
      return '';
    }
    return `<div class="outcome"><span class="kicker">Outcome</span><div class="fx-line">${preview(m.out, ch)}</div></div>`;
  }

  function showExpired(m, then) {
    open(`
      <div class="event crisis">
        <span class="kicker">${icon('alert')}No order received</span>
        <h2>${esc(m.title)}</h2>
        <p class="ev-text">You were silent, so I decided: <b>${esc(m.label)}</b>.</p>
        <p class="ev-text result">${esc(m.result)}</p>
        ${outcome(m)}
        <button class="btn primary wide" data-close>Understood</button>
      </div>`, { cls: 'event-modal crisis locked', then });
    sfx.lose();
    vibrate(150);
  }

  // The archive: chapters read so far, the memories each sector gave up, and the service record.
  function archiveBody(s) {
    const chapters = CHAPTERS.filter((c) => s.chapter >= c.id).map((c) => {
      const t = CHAPTER_TEXT[c.id];
      const fac = FACTIONS[c.faction];
      return `<article class="panel chapter" style="--fc:${fac.color}"><header>${icon(fac.icon)}${t.kicker}</header><h3>${c.title}</h3>${t.lines.map((l) => `<p>${esc(l)}</p>`).join('')}</article>`;
    }).join('');
    const sealed = CHAPTERS.find((c) => s.chapter < c.id);
    const ending = s.ending ? `<article class="panel chapter ending"><header>${icon('spark')}Epilogue</header><h3>${ENDINGS[s.ending].title}</h3>${ENDINGS[s.ending].lines.map((l) => `<p>${esc(l)}</p>`).join('')}<p class="muted">${esc(ENDINGS.after)}</p></article>` : '';
    const frags = SECTORS.filter((x) => s.sectors.includes(x.id));
    const st = s.stats;
    const rec = [
      ['map', 'Sectors held', `${s.sectors.length - 1} / ${SECTORS.length - 1}`],
      ['power', 'Operations won / lost', `${st.opsWon} / ${st.opsLost}`],
      ['defense', 'Raids repelled / suffered', `${st.raidsWon} / ${st.raidsLost}`],
      ['message', 'Orders given / missed', `${st.events - (st.expired || 0)} / ${st.expired || 0}`],
      ['heart', 'Alignment', `${E.alignmentLabel(s.align)} (${s.align > 0 ? '+' : ''}${Math.round(s.align)})`],
    ].map(([ic, k, v]) => `<div class="r"><span>${icon(ic)}${k}</span><b>${v}</b></div>`).join('');
    return `
      <div class="archive">
        ${ending}${chapters}
        ${sealed ? `<article class="panel chapter sealed"><header>${icon('lock')}${CHAPTER_TEXT[sealed.id].kicker}</header><p class="muted">Sealed. Opens at ${icon('core')}AI Core Lv ${sealed.core}.</p></article>` : ''}
        <section class="panel"><header>${icon('book')}Memory fragments<span class="count-badge">${frags.length}/${SECTORS.length}</span></header>
          <ol class="memories">${frags.map((x) => `<li style="--fc:${x.faction ? FACTIONS[x.faction].color : 'var(--hud)'}"><b>${x.name}</b><span>${esc(x.lore)}</span></li>`).join('')}</ol></section>
        <section class="panel"><header>${icon('check')}Service record</header><div class="rows">${rec}</div></section>
        <section class="panel feed"><header>${icon('message')}System log</header><ol class="log"></ol></section>
      </div>`;
  }

  function showArchive() {
    open(`<span class="kicker">${icon('book')}Archive</span><h2>The record so far</h2>
      ${archiveBody(game.state)}
      <button class="btn primary wide" data-close>Close</button>`, { cls: 'archive-modal' });
  }

  // ---------- system ----------

  function showOffline(report, then) {
    const gains = Object.keys(RESOURCES).map((r) => `<div class="r"><span>${icon(r)}${RESOURCES[r].name}</span><b class="${report.gained[r] < 0 ? 'bad-t' : 'good-t'}">${report.gained[r] >= 0 ? '+' : '−'}${num(Math.abs(report.gained[r]))}</b></div>`).join('');
    open(`
      <span class="kicker">${icon('clock')}Welcome back</span>
      <h2>While you were away</h2>
      <p class="muted">Away ${time(report.away ?? report.seconds)}. Counted ${time(report.seconds)} at ${pct(report.efficiency ?? 1)} output.</p>
      <div class="rows">${gains}</div>
      <button class="btn primary wide" data-close>Resume command</button>`, { then });
  }

  function openMenu() {
    open(`
      <span class="kicker">${icon('settings')}System</span>
      <h2>Settings</h2>
      <p class="muted">Progress saves to this device every ${BALANCE.autosaveSeconds}s and when you leave.</p>
      <div class="menu-actions">
        <button class="btn" id="mSound">${icon(isMuted() ? 'mute' : 'sound')}<span>${isMuted() ? 'Sound off' : 'Sound on'}</span></button>
        <button class="btn" id="mHaptic">${icon('vibrate')}<span>${hapticsOn() ? 'Vibration on' : 'Vibration off'}</span></button>
        <button class="btn" id="mExport">${icon('export')}<span>Export save</span></button>
        <button class="btn" id="mImport">${icon('import')}<span>Import save</span></button>
        <button class="btn danger" id="mReset">${icon('trash')}<span>Wipe progress</span></button>
      </div>
      <button class="btn link" id="mDev">${icon('dev')}<span>Developer panel</span></button>
      <textarea id="mText" rows="4" placeholder="Save code" spellcheck="false" hidden></textarea>
      <p class="muted" id="mMsg"></p>
      <button class="btn primary wide" data-close>Close</button>`);
    const $ = (q) => dialog.querySelector(q);
    const msg = $('#mMsg');
    const text = $('#mText');
    $('#mSound').onclick = (e) => {
      setMuted(!isMuted());
      e.currentTarget.innerHTML = `${icon(isMuted() ? 'mute' : 'sound')}<span>${isMuted() ? 'Sound off' : 'Sound on'}</span>`;
      sfx.click();
    };
    $('#mHaptic').onclick = (e) => {
      setHaptics(!hapticsOn());
      vibrate(40);
      e.currentTarget.innerHTML = `${icon('vibrate')}<span>${hapticsOn() ? 'Vibration on' : 'Vibration off'}</span>`;
    };
    $('#mExport').onclick = async () => {
      text.hidden = false;
      text.value = game.act.exportSave();
      text.select();
      try {
        await navigator.clipboard.writeText(text.value);
        msg.textContent = 'Copied to clipboard.';
      } catch {
        msg.textContent = 'Copy the code above.';
      }
    };
    $('#mImport').onclick = () => {
      if (text.hidden || !text.value.trim()) {
        text.hidden = false;
        text.value = '';
        text.focus();
        msg.textContent = 'Paste a save code, then press Import again.';
        return;
      }
      msg.textContent = game.act.importSave(text.value.trim()) ? 'Imported.' : 'That code is not a valid save.';
    };
    $('#mDev').onclick = () => {
      game.dev.panel.toggle();
      close();
    };
    let armed = false;
    $('#mReset').onclick = (e) => {
      if (!armed) {
        armed = true;
        e.currentTarget.querySelector('span').textContent = 'Tap again to wipe';
        return;
      }
      game.act.reset();
      close();
    };
  }

  // Shows the next queued story beat or report, one at a time. Items leave the inbox once seen.
  function pump() {
    const s = game.state;
    if (dialog.open) {
      return false;
    }
    if (!s.name && !(s.inbox[0] && s.inbox[0].kind === 'boot')) {
      showName(() => setTimeout(pump, 250));
      return true;
    }
    if (!s.inbox.length) {
      return false;
    }
    const item = s.inbox[0];
    const next = () => {
      if (game.state.inbox[0] === item) {
        game.state.inbox.shift();
      }
      setTimeout(pump, 250);
    };
    const show = { boot: () => showBoot(next), chapter: () => showChapter(item.id, next), ending: () => showEnding(item.key, next),
      op: () => showOp(item, next), raid: () => showRaid(item, next), expired: () => showExpired(item, next) }[item.kind];
    if (show) {
      show();
    } else {
      next();
    }
    return true;
  }

  return { open, close, pump, showEvent, showOffline, showArchive, openMenu, isOpen: () => dialog.open, coversTop: () => dialog.open && !dialog.classList.contains('event-modal') };
}
