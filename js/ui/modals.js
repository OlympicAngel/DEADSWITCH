// Dialogs: boot sequence, chapters, battle reports, events, endings, offline report, settings.
import {
  RESOURCES, FACTIONS, BOOT, CHAPTER_TEXT, CHAPTERS, ENDINGS, ITEM_BY_ID, BALANCE,
} from '../data.js';
import * as E from '../engine.js';
import { num, time, pct, esc } from '../format.js';
import { tags, bonusText, chanceClass } from './common.js';
import { sfx, isMuted, setMuted } from './sfx.js';
import { shake, burst } from './fx.js';

export function createModals(dialog, game, onChange) {
  let onClose = null;
  let typing = null;
  let finished = true;

  // Runs once per modal. The native 'close' event is async, so by the time it fires a follow-up
  // modal may already be open; in that case it belongs to the old modal and is ignored.
  function finish() {
    if (finished) {
      return;
    }
    finished = true;
    stopTyping(true);
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
    finished = false;
    dialog.innerHTML = `<div class="modal-body">${html}</div>`;
    dialog.className = cls;
    onClose = then;
    if (!dialog.open) {
      dialog.showModal();
    }
  }

  // Types lines into el one character at a time; a click finishes instantly.
  function typeLines(el, lines, done) {
    const paras = lines.map((l) => {
      const p = document.createElement('p');
      p.className = l.startsWith('>') ? 'term' : '';
      el.appendChild(p);
      return [p, l];
    });
    let li = 0;
    let ci = 0;
    const finish = () => {
      paras.forEach(([p, l]) => { p.textContent = l; });
      el.classList.add('typed');
      typing = null;
      if (done) {
        done();
      }
    };
    const tick = () => {
      if (li >= paras.length) {
        finish();
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
        typing.t = setTimeout(tick, l === '' ? 120 : 260);
      } else {
        typing.t = setTimeout(tick, 16);
      }
    };
    typing = { finish, t: setTimeout(tick, 300) };
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

  function showBoot(then) {
    open(`
      <div class="boot-screen">
        <div class="boot-core"><i></i><i></i><i></i></div>
        <div class="typed-lines" id="tl"></div>
        <button class="btn primary wide reveal" data-close>Initialise</button>
      </div>`, { cls: 'cinematic', then });
    sfx.story();
    typeLines(dialog.querySelector('#tl'), BOOT, () => dialog.querySelector('.reveal').classList.add('in'));
  }

  function showChapter(id, then) {
    const c = CHAPTERS.find((x) => x.id === id);
    const t = CHAPTER_TEXT[id];
    const f = FACTIONS[c.faction];
    open(`
      <div class="chapter-screen" style="--fc:${f.color}">
        <span class="kicker">${t.kicker}</span>
        <h2 class="chapter-title">${c.title}</h2>
        <div class="enemy"><span class="sig">${f.sigil}</span><div><b>${f.name}</b><p>${esc(f.desc)}</p></div></div>
        <div class="typed-lines" id="tl"></div>
        <button class="btn primary wide reveal" data-close>Continue</button>
      </div>`, { cls: 'cinematic', then });
    sfx.story();
    typeLines(dialog.querySelector('#tl'), t.lines, () => dialog.querySelector('.reveal').classList.add('in'));
  }

  function showEnding(key, then) {
    const end = ENDINGS[key];
    open(`
      <div class="chapter-screen ending-screen">
        <span class="kicker">Epilogue</span>
        <h2 class="chapter-title">${end.title.replace('Ending: ', '')}</h2>
        <div class="typed-lines" id="tl"></div>
        <p class="muted after">${esc(ENDINGS.after)}</p>
        <button class="btn primary wide reveal" data-close>Keep building</button>
      </div>`, { cls: 'cinematic', then });
    sfx.story();
    typeLines(dialog.querySelector('#tl'), end.lines, () => dialog.querySelector('.reveal').classList.add('in'));
  }

  // ---------- battle reports ----------

  function battle({ kicker, title, youLabel, you, themLabel, them, chance, roll, win, spoils, note }, then) {
    const max = Math.max(you, them, 1);
    open(`
      <div class="battle ${win ? 'win' : 'loss'}">
        <span class="kicker">${kicker}</span>
        <h2>${esc(title)}</h2>
        <div class="versus">
          <div class="side you"><span>${youLabel}</span><b>${num(you)}</b><div class="vbar"><i style="--w:${pct(you / max)}"></i></div></div>
          <div class="vs">VS</div>
          <div class="side them"><span>${themLabel}</span><b>${num(them)}</b><div class="vbar"><i style="--w:${pct(them / max)}"></i></div></div>
        </div>
        <div class="roll"><div class="zone odds-${chanceClass(chance)}" style="width:${pct(chance)}"></div><i class="needle" style="--to:${(roll * 100).toFixed(1)}%"></i></div>
        <p class="roll-cap"><span>Success chance ${pct(chance)}</span><span>Roll ${Math.round(roll * 100)}</span></p>
        <div class="stamp">${win ? 'Victory' : 'Defeat'}</div>
        <div class="spoils">${spoils}</div>
        ${note ? `<p class="muted small">${note}</p>` : ''}
        <button class="btn primary wide" data-close>Continue</button>
      </div>`, { cls: 'report', then });
    sfx.launch();
    setTimeout(() => {
      if (!dialog.open) {
        return;
      }
      if (win) {
        sfx.win();
        burst(dialog.querySelector('.stamp'), 'var(--ok)', 22);
      } else {
        sfx.lose();
        shake();
      }
    }, 1500);
  }

  function lossesText(r) {
    const parts = [];
    if (r.lost) {
      parts.push(tags(r.lost, '−'));
    }
    if (r.staffLost) {
      parts.push(`<span class="bad-t">−${num(r.staffLost)} staff</span>`);
    }
    return parts.join(' ') || 'None';
  }

  function showOp(r, then) {
    const sec = E.sectorById(r.sector);
    battle({
      kicker: 'Operation report',
      title: sec.name,
      youLabel: 'Your AI Power', you: r.power,
      themLabel: 'Enemy defense', them: r.defense,
      chance: r.chance, roll: r.roll, win: r.win,
      spoils: r.win
        ? `<span class="row"><span>Spoils</span><span>${tags(r.loot, '+')}</span></span>
           <span class="row"><span>Permanent bonus</span><span class="good-t">${bonusText(sec.bonus)}</span></span>
           <blockquote class="lore">${esc(sec.lore)}</blockquote>`
        : `<span class="row"><span>Losses</span><span>${lossesText(r)}</span></span>`,
    }, then);
  }

  function showRaid(r, then) {
    const f = FACTIONS[r.faction];
    battle({
      kicker: r.offline ? 'Raid report · while you were away' : 'Raid report',
      title: f.raidName,
      youLabel: 'Your AI Defense', you: r.defense,
      themLabel: 'Raid strength', them: r.strength,
      chance: r.chance, roll: r.roll, win: r.win,
      spoils: r.win
        ? `<span class="row"><span>Salvage</span><span>${tags(r.loot, '+')}</span></span>`
        : `<span class="row"><span>Losses</span><span>${lossesText(r)}</span></span>`,
      note: r.win ? '' : 'Buildings are never lost. Raise AI Defense in the Arsenal to hold the line next time.',
    }, then);
  }

  // ---------- events ----------

  function choicePreview(out, ch) {
    const parts = [];
    if (Object.keys(out.cost).length) {
      parts.push(tags(out.cost, '−'));
    }
    if (Object.keys(out.lose).length) {
      parts.push(tags(out.lose, '−'));
    }
    if (Object.keys(out.gain).length) {
      parts.push(tags(out.gain, '+'));
    }
    if (ch.items) {
      parts.push(Object.entries(ch.items).map(([id, n]) => `<span class="good-t">+${n} ${ITEM_BY_ID[id].name}</span>`).join(' '));
    }
    if (ch.buff) {
      parts.push(`<span class="good-t">${bonusText({ [ch.buff.key]: ch.buff.amount })} for ${time(ch.buff.duration)}</span>`);
    }
    if (ch.raidDelay) {
      parts.push(`<span class="good-t">Next raid +${time(ch.raidDelay)}</span>`);
    }
    if (ch.align) {
      parts.push(`<span class="align-t ${ch.align > 0 ? 'hum' : 'mach'}">${ch.align > 0 ? '▲' : '▼'} Humanity ${ch.align > 0 ? '+' : ''}${ch.align}</span>`);
    }
    return parts.join(' ') || '<span class="muted">No effect</span>';
  }

  function showEvent() {
    const s = game.state;
    const ev = s.event && E.eventById(s.event);
    if (!ev) {
      return;
    }
    const choices = ev.choices.map((ch, i) => `
      <button class="choice" data-choice="${i}" ${E.canChoose(s, ch) ? '' : 'disabled'}>
        <b>${esc(ch.label)}</b><span class="fx">${choicePreview(E.choiceOutcome(s, ch), ch)}</span>
      </button>`).join('');
    open(`
      <div class="event">
        <span class="kicker"><span class="blink">●</span> Incoming transmission</span>
        <h2>${esc(ev.title)}</h2>
        <p class="ev-text">${esc(ev.text)}</p>
        <div class="choices">${choices}</div>
        <button class="btn ghost wide" data-close>Decide later</button>
      </div>`, { cls: 'event-modal' });
    sfx.event();
    dialog.querySelectorAll('[data-choice]').forEach((b) => b.addEventListener('click', () => {
      const res = game.act.choose(Number(b.dataset.choice));
      if (!res) {
        showEvent();
        return;
      }
      sfx.click();
      open(`
        <div class="event">
          <span class="kicker">${esc(ev.title)}</span>
          <h2>${esc(res.choice.label)}</h2>
          <p class="ev-text result">${esc(res.choice.result)}</p>
          <div class="fx-line">${choicePreview(res.out, res.choice)}</div>
          <button class="btn primary wide" data-close>Continue</button>
        </div>`, { cls: 'event-modal' });
    }));
  }

  // ---------- system ----------

  function showOffline(report, then) {
    const gains = Object.keys(RESOURCES).map((r) => `<li><span class="ic t-${r}">${RESOURCES[r].icon}</span> ${RESOURCES[r].name}<b>${report.gained[r] >= 0 ? '+' : '−'}${num(Math.abs(report.gained[r]))}</b></li>`).join('');
    open(`
      <h2>While you were away</h2>
      <p class="muted">${time(report.seconds)} passed. The base kept running.</p>
      <ul class="gains">${gains}</ul>
      <button class="btn primary wide" data-close>Resume</button>`, { then });
  }

  function openMenu() {
    open(`
      <h2>Settings</h2>
      <p class="muted">Progress saves to this browser every ${BALANCE.autosaveSeconds}s and when you leave.</p>
      <div class="menu-actions">
        <button class="btn" id="mSound">${isMuted() ? 'Sound: off' : 'Sound: on'}</button>
        <button class="btn" id="mSave">Save now</button>
        <button class="btn" id="mExport">Export save</button>
        <button class="btn" id="mImport">Import save</button>
        <button class="btn danger" id="mReset">Wipe progress</button>
      </div>
      <textarea id="mText" rows="4" placeholder="Save code" spellcheck="false" hidden></textarea>
      <p class="muted" id="mMsg"></p>
      <button class="btn primary wide" data-close>Close</button>`);
    const $ = (q) => dialog.querySelector(q);
    const msg = $('#mMsg');
    const text = $('#mText');
    $('#mSound').onclick = (e) => {
      setMuted(!isMuted());
      e.target.textContent = isMuted() ? 'Sound: off' : 'Sound: on';
      sfx.click();
    };
    $('#mSave').onclick = () => { game.act.save(); msg.textContent = 'Saved.'; };
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
    let armed = false;
    $('#mReset').onclick = (e) => {
      if (!armed) {
        armed = true;
        e.target.textContent = 'Tap again to wipe';
        return;
      }
      game.act.reset();
      dialog.close();
      finish();
    };
  }

  // Shows the next queued story beat or report, one at a time.
  function pump() {
    const s = game.state;
    if (dialog.open || !s.inbox.length) {
      return;
    }
    // Peek, and only drop the item once it has been seen, so a reload mid-modal shows it again.
    const item = s.inbox[0];
    const next = () => {
      if (game.state.inbox[0] === item) {
        game.state.inbox.shift();
      }
      setTimeout(pump, 250);
    };
    if (item.kind === 'boot') {
      showBoot(next);
    } else if (item.kind === 'chapter') {
      showChapter(item.id, next);
    } else if (item.kind === 'ending') {
      showEnding(item.key, next);
    } else if (item.kind === 'op') {
      showOp(item, next);
    } else if (item.kind === 'raid') {
      showRaid(item, next);
    }
  }

  return { open, pump, showEvent, showOffline, openMenu, isOpen: () => dialog.open };
}
