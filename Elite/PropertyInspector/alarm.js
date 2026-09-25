// Property inspector of the "Alarme" action (com.mhwlng.elite.eventalarm) - see docs/L8-alarme.md.
// Needs sdtools.common.js, catalog.js, commands.js and generic.js (catalog index, command search, free shortcut).
// The real settings are the sdProperty elements (event, filterField, filterOp, filterValue, duration, idleImage,
// activeImage, alarmSound, pressCommand, pressHotkey, pressHotkeyText, clickSound); the category / search / event lists
// only help to fill "event". generic.js calls alarmInit, alarmBeforeLoad, alarmSettingsLoaded and alarmShowValue.
// Keep this file ASCII-only (non-ASCII characters are written as \u escapes).

var ALARM_PREFIX = 'journal.';
var alarmEvents = null;    // built once from the journal groups of the catalog (genericIndex)
var alarmInfoOpen = false; // "i" panel shown

// ---------- events of the catalog (pure functions) ----------

function alarmBuildEvents(index) {
    var events = { list: [], byName: {}, byCategory: {}, categories: [] };
    index.categories.forEach(function (category) {
        var list = [];
        index.groupsByCategory[category.id].forEach(function (group) {
            if (group.prefix.indexOf(ALARM_PREFIX) !== 0)
                return;
            var name = group.prefix.substring(ALARM_PREFIX.length);
            var description = index.info[group.prefix] || '';
            // the descriptions of its fields help too ("attaque" finds UnderAttack through "Qui est attaque" of Target)
            var fieldsText = group.entries.map(function (entry) { return entry.path + ' ' + (index.info[entry.key] || ''); }).join(' ');
            var event = {
                name: name,
                group: group,
                category: category,
                description: description,
                ownText: genericNormalize(name + ' ' + description),
                allText: genericNormalize(name + ' ' + description + ' ' + category.label + ' ' + fieldsText)
            };
            list.push(event);
            events.list.push(event);
            events.byName[name.toLowerCase()] = event;
        });
        if (list.length > 0) {
            events.byCategory[category.id] = list;
            events.categories.push(category);
        }
    });
    return events;
}

// every word in the name, the description, the category or the fields (case and accents ignored); name or description first
function alarmSearchEvents(events, text) {
    var words = genericNormalize(text).split(/\s+/).filter(function (w) { return w.length > 0; });
    if (words.length === 0)
        return [];

    var matches = [];
    events.list.forEach(function (event) {
        if (words.every(function (w) { return event.allText.indexOf(w) >= 0; }))
            matches.push({ event: event, score: words.every(function (w) { return event.ownText.indexOf(w) >= 0; }) ? 0 : 1 });
    });
    matches.sort(function (a, b) {
        return (a.score - b.score) || (a.event.category.order - b.event.category.order) || genericCompare(a.event.name, b.event.name);
    });
    return matches.map(function (m) { return m.event; });
}

// "journal.UnderAttack" typed by hand is read as "UnderAttack" (as Elite/Generic/AlarmConfig.cs does)
function alarmEventNameOf(text) {
    var name = String(text || '').trim();
    return name.toLowerCase().indexOf(ALARM_PREFIX) === 0 ? name.substring(ALARM_PREFIX.length) : name;
}

function alarmFindEvent(name) {
    return alarmEvents && name ? alarmEvents.byName[alarmEventNameOf(name).toLowerCase()] || null : null;
}

function alarmFieldKey(eventName, field) {
    return ALARM_PREFIX + eventName + '.' + field;
}

function alarmEventLabel(event) {
    return event.name + (event.group.odyssey ? ' (Odyssey)' : '');
}

// ---------- user interface ----------

function alarmCurrentEvent() {
    return alarmEventNameOf(genericElement('event').value);
}

function alarmInit() {
    if (!genericIndex || alarmEvents)
        return;

    alarmEvents = alarmBuildEvents(genericIndex);
    var select = genericElement('alarmCategory');
    select.innerHTML = '';
    select.appendChild(genericOption('', '\u2014 choisir une cat\u00e9gorie \u2014'));
    alarmEvents.categories.forEach(function (category) {
        select.appendChild(genericOption(category.id, category.emoji + ' ' + category.label + ' (' + alarmEvents.byCategory[category.id].length + ')'));
    });
    alarmFillEvents(null);
    alarmFillFilterFields(alarmCurrentEvent(), genericElement('filterField').value, true);
    alarmSyncFromEvent();
}

function alarmFillEvents(list) {
    var select = genericElement('alarmEventList');
    select.innerHTML = '';
    select.appendChild(genericOption('', '\u2014 choisir un \u00e9v\u00e9nement \u2014'));
    (list || []).forEach(function (event) {
        select.appendChild(genericOption(event.name, alarmEventLabel(event)));
    });
}

// category and event lists positioned on an event (does not touch the search)
function alarmShowEvent(event) {
    genericElement('alarmCategory').value = event ? event.category.id : '';
    alarmFillEvents(event ? alarmEvents.byCategory[event.category.id] : null);
    genericSelectOption(genericElement('alarmEventList'), event ? event.name : '');
}

// fields of the event (except timestamp) in the filter list. A saved field is kept when keepUnknown (loading) or for
// an event outside the catalog; otherwise a field that the new event does not have is dropped (no filter).
function alarmFillFilterFields(eventName, current, keepUnknown) {
    var select = genericElement('filterField');
    var event = alarmFindEvent(eventName);
    var wanted = String(current || '');
    var found = false;
    select.innerHTML = '';
    select.appendChild(genericOption('', '\u2014 aucun filtre \u2014'));
    if (event) {
        event.group.entries.forEach(function (entry) {
            if (entry.path === 'timestamp')
                return;
            select.appendChild(genericOption(entry.path, entry.path + ' \u2014 ' + genericTypeLabel(entry)));
            if (wanted && entry.path.toLowerCase() === wanted.toLowerCase())
                found = true;
        });
    }
    if (wanted && !found && (keepUnknown || !event)) {
        select.appendChild(genericOption(wanted, wanted + ' (hors catalogue)'));
        found = true;
    }
    genericSelectOption(select, found ? wanted : '');
}

function alarmHideResults() {
    genericElement('alarmResultsRow').style.display = 'none';
    genericElement('alarmSearchInfoRow').style.display = 'none';
}

// the event changed: its description, the filter helpers and the "i" panel follow
function alarmEventChanged() {
    var name = alarmCurrentEvent();
    var event = alarmFindEvent(name);
    var info = genericElement('alarmEventInfo');
    info.className = 'sdpi-item-value generic-info';
    if (!name)
        info.textContent = 'Choisis un \u00e9v\u00e9nement (cat\u00e9gorie puis \u00e9v\u00e9nement, ou recherche).';
    else if (!event) {
        info.textContent = '\u26a0 \u00c9v\u00e9nement hors catalogue (saisie manuelle) : l\'alarme fonctionnera si le jeu l\'\u00e9crit sous ce nom exact.';
        info.className += ' generic-warning';
    } else
        info.textContent = event.description || 'Pas de description.';

    alarmUpdateFilterHelpers();
    if (alarmInfoOpen)
        alarmRenderInfo();
}

// called when the settings are loaded
function alarmSyncFromEvent() {
    genericElement('alarmSearch').value = '';
    alarmHideResults();
    alarmShowEvent(alarmFindEvent(alarmCurrentEvent()));
    alarmEventChanged();
}

function alarmChooseEvent(name) {
    if (!name)
        return;
    genericElement('event').value = name;
    alarmShowEvent(alarmFindEvent(name));
    alarmFillFilterFields(name, genericElement('filterField').value, false);
    alarmEventChanged();
    setSettings();
}

// chosen by the user: the search is cleared
function alarmCategoryChanged() {
    genericElement('alarmSearch').value = '';
    alarmHideResults();
    var id = genericElement('alarmCategory').value;
    alarmFillEvents(id ? alarmEvents.byCategory[id] : null);
    genericSelectOption(genericElement('alarmEventList'), alarmCurrentEvent());
}

// the search never clears what is being typed; no native tooltip on the results (a click on a result showing its
// tooltip did nothing in the Stream Deck software, docs/L7-retours-d6.md)
function alarmSearchChanged() {
    var text = genericElement('alarmSearch').value;
    if (!alarmEvents || genericNormalize(text).trim().length < GENERIC_MIN_SEARCH) {
        alarmHideResults();
        return;
    }

    var found = alarmSearchEvents(alarmEvents, text);
    var info = genericElement('alarmSearchInfo');
    genericElement('alarmSearchInfoRow').style.display = '';
    if (found.length === 0) {
        info.textContent = 'Aucun \u00e9v\u00e9nement : essaie un autre mot (fran\u00e7ais ou anglais).';
        genericElement('alarmResultsRow').style.display = 'none';
        return;
    }

    info.textContent = found.length + ' \u00e9v\u00e9nement(s) \u2014 clique pour choisir.';
    var results = genericElement('alarmResults');
    results.innerHTML = '';
    var currentCategory = null, optgroup = null;
    found.forEach(function (event) {
        if (event.category !== currentCategory) {
            currentCategory = event.category;
            optgroup = document.createElement('optgroup');
            optgroup.label = event.category.emoji + ' ' + event.category.label;
            results.appendChild(optgroup);
        }
        optgroup.appendChild(genericOption(event.name, alarmEventLabel(event)));
    });
    genericSelectOption(results, alarmCurrentEvent());
    genericElement('alarmResultsRow').style.display = '';
}

function alarmResultChosen() {
    var name = genericElement('alarmResults').value;
    if (name && name.toLowerCase() !== alarmCurrentEvent().toLowerCase())
        alarmChooseEvent(name);
}

function alarmEventListChanged() {
    alarmChooseEvent(genericElement('alarmEventList').value);
}

// manual entry of the event name (any name, even outside the catalog)
function alarmEventTyped() {
    alarmShowEvent(alarmFindEvent(alarmCurrentEvent()));
    alarmFillFilterFields(alarmCurrentEvent(), genericElement('filterField').value, false);
    alarmEventChanged();
    setSettings();
}

// ---------- filter ----------

function alarmFilterEntry() {
    var field = genericElement('filterField').value;
    return field && genericIndex ? genericFindEntry(genericIndex, alarmFieldKey(alarmCurrentEvent(), field)) : null;
}

// tests offered according to the type of the field, values of an enumeration proposed, meaning of the field
function alarmUpdateFilterHelpers() {
    var field = genericElement('filterField').value;
    var entry = alarmFilterEntry();
    var allowed = entry ? GENERIC_RULE_OPERATORS[entry.type] : null;
    if (entry && /#count$/.test(entry.path))
        allowed = GENERIC_NUMBER_OPERATORS;
    var select = genericElement('filterOp');
    Array.prototype.forEach.call(select.options, function (option) {
        // never disable the current choice, so that a saved filter stays visible
        option.disabled = option.value !== '' && allowed !== null && allowed.indexOf(option.value) < 0 && option.value !== select.value;
    });

    var list = genericElement('alarmEnumValues');
    list.innerHTML = '';
    var values = [];
    if (entry && entry.type === 'enum' && genericIndex.enums[entry.enumName])
        values = genericIndex.enums[entry.enumName];
    else if (entry && entry.type === 'bool')
        values = ['oui', 'non'];
    values.forEach(function (value) {
        list.appendChild(genericOption(value, value));
    });

    var info = genericElement('alarmFilterInfo');
    if (!field)
        info.textContent = '';
    else if (!entry)
        info.textContent = 'Champ hors catalogue : le test porte sur la valeur \u00e9crite par le jeu.';
    else
        info.textContent = (genericIndex.info[entry.key] || 'Pas de description d\u00e9taill\u00e9e pour ce champ.') + ' (' + genericTypeLabel(entry) + ')';
}

function alarmFilterFieldChanged() {
    alarmUpdateFilterHelpers();
    setSettings();
}

// ---------- "i" panel, test button ----------

function alarmToggleInfo() {
    alarmInfoOpen = !alarmInfoOpen;
    genericElement('alarmInfoRow').style.display = alarmInfoOpen ? '' : 'none';
    if (alarmInfoOpen)
        alarmRenderInfo();
}

function alarmRenderInfo() {
    var panel = genericElement('alarmInfoPanel');
    panel.innerHTML = '';
    var name = alarmCurrentEvent();
    if (!name) {
        genericInfoLine(panel, null, 'Choisis d\'abord un \u00e9v\u00e9nement.');
        return;
    }

    var event = alarmFindEvent(name);
    genericInfoLine(panel, '\u00c9v\u00e9nement', name);
    if (!event)
        genericInfoLine(panel, null, '\u00c9v\u00e9nement hors catalogue (saisie manuelle) : pas de description.');
    else {
        genericInfoLine(panel, 'Cat\u00e9gorie', event.category.emoji + ' ' + event.category.label + (event.group.odyssey ? ' (Odyssey)' : ''));
        genericInfoLine(panel, 'Quand', event.description || 'pas de description.');
        var fields = event.group.entries.filter(function (e) { return e.path !== 'timestamp'; }).map(function (e) { return e.path; });
        genericInfoLine(panel, 'Champs (filtre)', fields.length > 0 ? fields.join(', ') : 'aucun');
    }
    genericInfoLine(panel, 'Dernier re\u00e7u', '\u2026', 'genericCurrentValue');
    genericRequestValue(alarmFieldKey(name, 'timestamp'));
}

// reply of the plugin to genericRequestValue: time of the last occurrence of the event
function alarmShowValue(reply) {
    var target = genericElement('genericCurrentValue');
    if (!target || String(reply.genericValueKey).toLowerCase() !== alarmFieldKey(alarmCurrentEvent(), 'timestamp').toLowerCase())
        return;

    target.textContent = reply.genericValuePresent
        ? '\u00e0 ' + reply.genericValueText + ' (le journal relu au lancement compte aussi, mais il ne d\u00e9clenche jamais l\'alarme).'
        : 'pas encore re\u00e7u depuis le lancement du plugin.';
}

// asks the plugin to trigger the alert as a live event would
function alarmTest() {
    var info = genericElement('alarmTestInfo');
    genericElement('alarmTestRow').style.display = '';
    if (typeof websocket === 'undefined' || !websocket || websocket.readyState !== 1) {
        info.textContent = 'Disponible seulement dans le logiciel Stream Deck.';
        return;
    }

    websocket.send(JSON.stringify({
        action: actionInfo.action,
        event: 'sendToPlugin',
        context: uuid,
        payload: { alarmTest: true }
    }));
    info.textContent = 'Envoy\u00e9 : la touche passe en alerte (image d\'alerte ou \u26a0, son d\'alerte) pour la dur\u00e9e r\u00e9gl\u00e9e ; dur\u00e9e 0 = jusqu\'\u00e0 un appui.';
}

// ---------- settings (called by generic.js) ----------

// before the settings are loaded: the filter list must already hold the saved field, or its value would be lost
function alarmBeforeLoad(settings) {
    if (!alarmEvents || !settings)
        return;
    var has = function (name) { return Object.prototype.hasOwnProperty.call(settings, name); };
    alarmFillFilterFields(has('event') ? alarmEventNameOf(settings.event) : alarmCurrentEvent(),
        has('filterField') ? settings.filterField : genericElement('filterField').value, true);
}

function alarmSettingsLoaded() {
    alarmSyncFromEvent();
}
