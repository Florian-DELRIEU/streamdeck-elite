// Property inspector shared by the generic actions (Value; State and Alarm in L4) - see docs/L3-valeur.md.
// Needs sdtools.common.js and catalog.js (generated at build time by Elite.CatalogGen).
// The real setting is the "source" input (class sdProperty); the category / search / field controls only help to fill it.
// Keep this file ASCII-only (non-ASCII characters are written as \u escapes).

var GENERIC_ACTION_KINDS = {
    'com.mhwlng.elite.value': 'value'
};

var GENERIC_TYPE_LABELS = {
    'bool': 'oui/non',
    'number': 'nombre',
    'text': 'texte',
    'date': 'date/heure',
    'enum': 'liste de valeurs'
};

var GENERIC_MAX_RESULTS = 100;

var genericIndex = null; // built once from ELITE_CATALOG

// ---------- catalog index (pure functions) ----------

function genericBuildIndex(catalog) {
    var index = { categories: [], categoryById: {}, groupsByCategory: {}, byKey: {}, entries: [], enums: catalog.enums || {} };

    catalog.categories.forEach(function (category, order) {
        category.order = order;
        index.categories.push(category);
        index.categoryById[category.id] = category;
        index.groupsByCategory[category.id] = [];
    });

    catalog.groups.forEach(function (group) {
        if (!index.groupsByCategory[group.category])
            return;
        index.groupsByCategory[group.category].push(group);
        group.entries = group.fields.map(function (field) {
            var entry = {
                key: group.prefix + '.' + field[0],
                path: field[0],
                type: field[1],
                enumName: field[2] || null,
                group: group,
                category: group.category
            };
            index.byKey[entry.key.toLowerCase()] = entry;
            index.entries.push(entry);
            return entry;
        });
    });

    return index;
}

function genericFindEntry(index, key) {
    if (!index || !key)
        return null;
    return index.byKey[String(key).trim().toLowerCase()] || null;
}

// every word of the text must appear in the key or in the group label (case-insensitive)
function genericSearch(index, text, max) {
    var words = String(text || '').toLowerCase().split(/\s+/).filter(function (w) { return w.length > 0; });
    if (words.length === 0)
        return { entries: [], total: 0 };

    var matches = index.entries.filter(function (entry) {
        var haystack = (entry.key + ' ' + entry.group.label).toLowerCase();
        return words.every(function (w) { return haystack.indexOf(w) >= 0; });
    });

    // category, then group, then field: entries of a group stay together in the list
    matches.sort(function (a, b) {
        var ca = index.categoryById[a.category].order, cb = index.categoryById[b.category].order;
        if (ca !== cb)
            return ca - cb;
        return genericCompare(a.group.label, b.group.label) || genericCompare(a.group.id, b.group.id) || genericCompare(a.path, b.path);
    });

    return { entries: matches.slice(0, max), total: matches.length };
}

function genericCompare(a, b) {
    a = String(a).toLowerCase();
    b = String(b).toLowerCase();
    return a < b ? -1 : a > b ? 1 : 0;
}

function genericGroupLabel(group) {
    return group.label + (group.odyssey ? ' (Odyssey)' : '');
}

function genericDescribe(index, key) {
    if (!key)
        return { text: 'Choisissez une donnée, ou tapez une clé.', warning: false };

    var entry = genericFindEntry(index, key);
    if (!entry)
        return { text: '⚠ Clé hors catalogue (saisie manuelle) : elle fonctionnera si le jeu l\'écrit.', warning: true };

    var text = 'Type : ' + (GENERIC_TYPE_LABELS[entry.type] || entry.type) + ' · ' + genericGroupLabel(entry.group);
    if (entry.type === 'enum' && index.enums[entry.enumName]) {
        var values = index.enums[entry.enumName];
        text += ' · valeurs : ' + values.slice(0, 6).join(', ') + (values.length > 6 ? ', …' : '');
    }
    return { text: text, warning: false };
}

// ---------- user interface ----------

function genericElement(id) {
    return document.getElementById(id);
}

function genericInit() {
    if (genericIndex || typeof ELITE_CATALOG === 'undefined' || !genericElement('genericCategory'))
        return;

    genericIndex = genericBuildIndex(ELITE_CATALOG);

    var select = genericElement('genericCategory');
    select.innerHTML = '';
    select.appendChild(genericOption('', '— choisir une catégorie —'));
    genericIndex.categories.forEach(function (category) {
        var count = genericIndex.groupsByCategory[category.id].reduce(function (n, g) { return n + g.entries.length; }, 0);
        select.appendChild(genericOption(category.id, category.emoji + ' ' + category.label + ' (' + count + ')'));
    });

    genericFillFields(null);
    genericShowSections();
    genericSyncFromSource();
}

function genericOption(value, text) {
    var option = document.createElement('option');
    option.value = value;
    option.textContent = text;
    return option;
}

function genericFieldOption(entry) {
    return genericOption(entry.key, entry.path + ' — ' + (GENERIC_TYPE_LABELS[entry.type] || entry.type));
}

// entries: list of catalog entries (grouped in the list by event / status part), or null for an empty list
function genericFillFields(entries, placeholder) {
    var select = genericElement('genericField');
    select.innerHTML = '';
    select.appendChild(genericOption('', placeholder || '— choisir une donnée —'));
    if (!entries)
        return;

    var currentGroup = null, optgroup = null;
    entries.forEach(function (entry) {
        if (entry.group !== currentGroup) {
            currentGroup = entry.group;
            optgroup = document.createElement('optgroup');
            optgroup.label = genericGroupLabel(entry.group);
            select.appendChild(optgroup);
        }
        optgroup.appendChild(genericFieldOption(entry));
    });
}

function genericEntriesOfCategory(categoryId) {
    var entries = [];
    (genericIndex.groupsByCategory[categoryId] || []).forEach(function (group) {
        entries = entries.concat(group.entries);
    });
    return entries;
}

function genericSelectField(key) {
    var select = genericElement('genericField');
    var wanted = String(key || '').toLowerCase();
    for (var i = 0; i < select.options.length; i++) {
        if (select.options[i].value.toLowerCase() === wanted && wanted !== '') {
            select.selectedIndex = i;
            return;
        }
    }
    select.selectedIndex = 0;
}

function genericUpdateKeyInfo() {
    var info = genericDescribe(genericIndex, genericElement('source').value);
    var element = genericElement('genericKeyInfo');
    element.textContent = info.text;
    element.className = 'sdpi-item-value generic-info' + (info.warning ? ' generic-warning' : '');
}

// category and field lists follow the saved key (called when the settings are loaded)
function genericSyncFromSource() {
    if (!genericIndex)
        return;

    var key = genericElement('source').value;
    var entry = genericFindEntry(genericIndex, key);
    genericElement('genericSearch').value = '';
    if (entry) {
        genericElement('genericCategory').value = entry.category;
        genericFillFields(genericEntriesOfCategory(entry.category));
        genericSelectField(entry.key);
    } else {
        genericElement('genericCategory').value = '';
        genericFillFields(null);
    }
    genericUpdateKeyInfo();
}

function genericCategoryChanged() {
    genericElement('genericSearch').value = '';
    var categoryId = genericElement('genericCategory').value;
    genericFillFields(categoryId ? genericEntriesOfCategory(categoryId) : null);
    genericSelectField(genericElement('source').value);
}

function genericSearchChanged() {
    var text = genericElement('genericSearch').value;
    if (text.trim().length < 2) {
        genericCategoryChanged();
        return;
    }

    genericElement('genericCategory').value = '';
    var result = genericSearch(genericIndex, text, GENERIC_MAX_RESULTS);
    var placeholder = result.total === 0
        ? '— aucun résultat —'
        : '— ' + result.total + ' résultat(s)' + (result.total > GENERIC_MAX_RESULTS ? ', ' + GENERIC_MAX_RESULTS + ' affichés : précisez' : '') + ' —';
    genericFillFields(result.entries, placeholder);
    genericSelectField(genericElement('source').value);
}

function genericFieldChanged() {
    var key = genericElement('genericField').value;
    if (!key)
        return;
    genericElement('source').value = key;
    genericUpdateKeyInfo();
    setSettings();
}

// manual entry of a key (any key, even outside the catalog)
function genericSourceTyped() {
    genericUpdateKeyInfo();
    setSettings();
}

// sections marked data-actions="value state alarm" are shown for these actions only
function genericShowSections() {
    var kind = (typeof actionInfo !== 'undefined' && actionInfo && actionInfo.action) ? GENERIC_ACTION_KINDS[actionInfo.action] : null;
    var sections = document.querySelectorAll('[data-actions]');
    Array.prototype.forEach.call(sections, function (section) {
        var kinds = section.getAttribute('data-actions').split(' ');
        section.style.display = (!kind || kinds.indexOf(kind) >= 0) ? '' : 'none';
    });
}

// sdtools.common.js calls loadConfiguration when the settings arrive: resynchronise the lists afterwards
(function () {
    var baseLoadConfiguration = window.loadConfiguration;
    window.loadConfiguration = function (payload) {
        baseLoadConfiguration(payload);
        try {
            genericInit();
            genericShowSections();
            if (payload && Object.prototype.hasOwnProperty.call(payload, 'source'))
                genericSyncFromSource();
        } catch (err) {
            console.log('generic.js: ' + err);
        }
    };
})();

document.addEventListener('DOMContentLoaded', genericInit);
